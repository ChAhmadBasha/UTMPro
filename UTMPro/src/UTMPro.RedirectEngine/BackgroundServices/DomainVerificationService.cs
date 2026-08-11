using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Data.SqlClient;
using UTMPro.Data;

namespace UTMPro.RedirectEngine.BackgroundServices;

/// <summary>
/// Background service that:
///   1. Checks DNS records for unverified custom domains (every 5 minutes)
///   2. After DNS verification, automatically issues Let's Encrypt SSL certificates
///      via win-acme (wacs.exe) and adds IIS HTTPS bindings
///   3. Tracks SSL status (SSLIssued, SSLIssuedAt, SSLError, SSLExpiresAt) in DB
/// </summary>
public class DomainVerificationService : BackgroundService
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly ILogger<DomainVerificationService> _logger;

    public DomainVerificationService(IDbConnectionFactory db, IConfiguration config,
        ILogger<DomainVerificationService> logger)
    {
        _db = db; _config = config; _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Wait for app startup
        await Task.Delay(TimeSpan.FromSeconds(15), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await VerifyDomainsAsync(ct);
                await IssuePendingSSLCertificatesAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Domain verification/SSL error");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }

    // ═══════════════════════════════════════════════════
    // STEP 1: DNS Verification
    // ═══════════════════════════════════════════════════
    private async Task VerifyDomainsAsync(CancellationToken ct)
    {
        const string sql = @"
            SELECT Id, Domain, DNSType, DNSValue 
            FROM Domains 
            WHERE IsVerified = 0 AND IsSystemDomain = 0 AND IsActive = 1 AND IsArchived = 0";

        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);

        var domainsToCheck = new List<(long Id, string Domain, string DnsType, string DnsValue)>();
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                domainsToCheck.Add((
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3)));
            }
        }

        foreach (var (id, domain, dnsType, dnsValue) in domainsToCheck)
        {
            try
            {
                var matched = IsIpAddress(dnsValue)
                    ? await IpMatchesAsync(domain, dnsValue, ct)
                    : await CnameMatchesAsync(domain, dnsValue, ct);

                if (matched)
                {
                    await using var updateCmd = new SqlCommand(
                        "UPDATE Domains SET IsVerified = 1, VerifiedAt = GETUTCDATE(), UpdatedAt = GETUTCDATE() WHERE Id = @Id", conn);
                    updateCmd.Parameters.AddWithValue("@Id", id);
                    await updateCmd.ExecuteNonQueryAsync(ct);
                    _logger.LogInformation("Domain DNS verified: {Domain} ({DnsType} -> {Value})", domain, dnsType, dnsValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("DNS lookup failed for {Domain}: {Msg}", domain, ex.Message);
            }
        }
    }

    // A-record verification: the domain's resolved IP addresses must contain
    // the expected address.
    private static async Task<bool> IpMatchesAsync(string domain, string expectedIp, CancellationToken ct)
    {
        var addresses = await Dns.GetHostAddressesAsync(domain, ct);
        return addresses.Any(a => a.ToString() == expectedIp);
    }

    // CNAME verification: the domain must resolve to the expected target
    // hostname (canonical CNAME target), or its resolved addresses must match
    // the target's resolved addresses. The origin server IP is never required.
    private static async Task<bool> CnameMatchesAsync(string domain, string expectedTarget, CancellationToken ct)
    {
        expectedTarget = expectedTarget.Trim().TrimEnd('.');

        try
        {
            var entry = await Dns.GetHostEntryAsync(domain, ct);
            var canonical = (entry.HostName ?? "").Trim().TrimEnd('.');

            if (string.Equals(canonical, expectedTarget, StringComparison.OrdinalIgnoreCase))
                return true;
            if (entry.Aliases.Any(a => string.Equals((a ?? "").Trim().TrimEnd('.'), expectedTarget, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        catch
        {
            // Fall through to address comparison below.
        }

        // Fallback: the domain and the target must resolve to the same IPs.
        try
        {
            var domainAddresses = await Dns.GetHostAddressesAsync(domain, ct);
            var targetAddresses = await Dns.GetHostAddressesAsync(expectedTarget, ct);
            var targetSet = targetAddresses.Select(a => a.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return domainAddresses.Any(a => targetSet.Contains(a.ToString()));
        }
        catch
        {
            return false;
        }
    }

    private static bool IsIpAddress(string value)
        => IPAddress.TryParse(value?.Trim(), out _);

    // ═══════════════════════════════════════════════════
    // STEP 2: Auto SSL Certificate Issuance
    // ═══════════════════════════════════════════════════
    private async Task IssuePendingSSLCertificatesAsync(CancellationToken ct)
    {
        // Only run on Windows (IIS + win-acme)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Read settings from DB
        await using var conn = await _db.CreateOpenConnectionAsync();

        var autoSSL = await GetSettingAsync(conn, "AutoSSLEnabled");
        if (autoSSL != "true")
            return;

        var wacsPath = await GetSettingAsync(conn, "WinAcmePath") ?? @"C:\win-acme\wacs.exe";
        var siteName = await GetSettingAsync(conn, "IISSiteName") ?? "RedirectEngine";
        var email = await GetSettingAsync(conn, "SSLContactEmail") ?? "admin@utmpro.link";

        // Check if win-acme exists
        if (!File.Exists(wacsPath))
        {
            _logger.LogWarning("win-acme not found at {Path}. Auto-SSL disabled. Download from https://www.win-acme.com/", wacsPath);
            return;
        }

        // Find domains that are verified but don't have SSL yet
        const string sql = @"
            SELECT Id, Domain 
            FROM Domains 
            WHERE IsVerified = 1 
              AND IsSystemDomain = 0 
              AND IsActive = 1 
              AND IsArchived = 0 
              AND (SSLIssued = 0 OR SSLIssued IS NULL)
              AND (SSLError IS NULL OR SSLError = '')";

        var pendingDomains = new List<(long Id, string Domain)>();
        await using (var cmd = new SqlCommand(sql, conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                pendingDomains.Add((reader.GetInt64(0), reader.GetString(1)));
            }
        }

        foreach (var (id, domain) in pendingDomains)
        {
            if (ct.IsCancellationRequested) break;

            _logger.LogInformation("Auto-SSL: Issuing certificate for {Domain}...", domain);

            try
            {
                var (success, output) = await RunWinAcmeAsync(wacsPath, domain, siteName, email, ct);

                if (success)
                {
                    // Mark as SSL issued
                    await using var updateCmd = new SqlCommand(@"
                        UPDATE Domains 
                        SET SSLIssued = 1, 
                            SSLIssuedAt = GETUTCDATE(), 
                            SSLError = NULL,
                            SSLExpiresAt = DATEADD(DAY, 90, GETUTCDATE()),
                            UpdatedAt = GETUTCDATE() 
                        WHERE Id = @Id", conn);
                    updateCmd.Parameters.AddWithValue("@Id", id);
                    await updateCmd.ExecuteNonQueryAsync(ct);

                    _logger.LogInformation("Auto-SSL: Certificate issued successfully for {Domain}", domain);
                }
                else
                {
                    // Record the error so we don't retry every 5 minutes forever
                    var errorMsg = output.Length > 450 ? output.Substring(0, 450) : output;
                    await using var errorCmd = new SqlCommand(@"
                        UPDATE Domains 
                        SET SSLError = @Error, UpdatedAt = GETUTCDATE() 
                        WHERE Id = @Id", conn);
                    errorCmd.Parameters.AddWithValue("@Id", id);
                    errorCmd.Parameters.AddWithValue("@Error", errorMsg);
                    await errorCmd.ExecuteNonQueryAsync(ct);

                    _logger.LogWarning("Auto-SSL: Failed for {Domain}: {Error}", domain, errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Auto-SSL: Exception issuing cert for {Domain}", domain);

                await using var errorCmd = new SqlCommand(
                    "UPDATE Domains SET SSLError = @Error, UpdatedAt = GETUTCDATE() WHERE Id = @Id", conn);
                errorCmd.Parameters.AddWithValue("@Id", id);
                errorCmd.Parameters.AddWithValue("@Error", ex.Message.Length > 450 ? ex.Message.Substring(0, 450) : ex.Message);
                await errorCmd.ExecuteNonQueryAsync(ct);
            }

            // Wait between domains to avoid rate limiting
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }

    /// <summary>
    /// Runs win-acme (wacs.exe) to issue a Let's Encrypt certificate and bind to IIS.
    /// Returns (success, output).
    /// </summary>
    private static async Task<(bool Success, string Output)> RunWinAcmeAsync(
        string wacsPath, string domain, string siteName, string email, CancellationToken ct)
    {
        // win-acme command line:
        // wacs.exe --target manual --host DOMAIN --installation iis 
        //          --websiteid SITE_ID --store certificatestore 
        //          --accepttos --emailaddress EMAIL --closeonfinish
        var args = string.Join(" ",
            "--target", "manual",
            "--host", domain,
            "--installation", "iis",
            "--installationsiteid", GetIISSiteId(siteName).ToString(),
            "--store", "certificatestore",
            "--accepttos",
            "--emailaddress", email,
            "--closeonfinish"
        );

        var psi = new ProcessStartInfo
        {
            FileName = wacsPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(wacsPath) ?? @"C:\win-acme"
        };

        using var process = new Process { StartInfo = psi };
        var output = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) output.AppendLine("[ERR] " + e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait max 2 minutes for certificate issuance
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(2));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { }
            return (false, "Process timed out after 2 minutes");
        }

        var exitCode = process.ExitCode;
        var outputStr = output.ToString();

        // win-acme exit code 0 = success
        var success = exitCode == 0 && !outputStr.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase);

        return (success, outputStr);
    }

    /// <summary>
    /// Gets the IIS site ID by name using PowerShell.
    /// Fallback to 1 if lookup fails.
    /// </summary>
    private static int GetIISSiteId(string siteName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-Command \"(Get-Website -Name '{siteName}').ID\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return 1;

            var result = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);

            return int.TryParse(result, out var id) ? id : 1;
        }
        catch
        {
            return 1;
        }
    }

    private static async Task<string?> GetSettingAsync(SqlConnection conn, string key)
    {
        await using var cmd = new SqlCommand(
            "SELECT SettingValue FROM SystemSettings WHERE SettingKey = @Key", conn);
        cmd.Parameters.AddWithValue("@Key", key);
        var result = await cmd.ExecuteScalarAsync();
        return result as string;
    }
}
