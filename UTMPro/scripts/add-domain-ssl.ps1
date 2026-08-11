<#
.SYNOPSIS
    Issues a Let's Encrypt SSL certificate for a custom domain and binds it to the IIS RedirectEngine site.

.DESCRIPTION
    This script uses win-acme (wacs.exe) to issue a free Let's Encrypt certificate
    for a custom domain and automatically adds the HTTPS binding to your IIS site.
    
    Run this script EVERY TIME you add a new custom domain in UTMPro admin.

.PARAMETER Domain
    The custom domain to issue a certificate for (e.g., link.client.com)

.PARAMETER SiteName
    The IIS site name for the Redirect Engine (default: RedirectEngine)

.PARAMETER WacsPath
    Path to wacs.exe (default: C:\win-acme\wacs.exe)

.PARAMETER Email
    Email for Let's Encrypt notifications (default: admin@utmpro.link)

.EXAMPLE
    .\add-domain-ssl.ps1 -Domain "link.client2.com"
    .\add-domain-ssl.ps1 -Domain "go.mybrand.com" -SiteName "UTMProRedirect"

.NOTES
    Requirements:
    - Run as Administrator
    - win-acme installed at $WacsPath
    - Port 80 open for HTTP-01 validation
    - DNS A record must point to this server BEFORE running
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Domain,

    [string]$SiteName = "RedirectEngine",

    [string]$WacsPath = "C:\win-acme\wacs.exe",

    [string]$Email = "admin@utmpro.link"
)

# Check if running as admin
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERROR: Run this script as Administrator!" -ForegroundColor Red
    exit 1
}

# Check if win-acme exists
if (-not (Test-Path $WacsPath)) {
    Write-Host "ERROR: win-acme not found at $WacsPath" -ForegroundColor Red
    Write-Host "Download from: https://www.win-acme.com/" -ForegroundColor Yellow
    exit 1
}

# Check if IIS site exists
Import-Module WebAdministration -ErrorAction SilentlyContinue
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
if (-not $site) {
    Write-Host "ERROR: IIS site '$SiteName' not found." -ForegroundColor Red
    Write-Host "Available sites:" -ForegroundColor Yellow
    Get-Website | ForEach-Object { Write-Host "  - $($_.Name) (ID: $($_.ID))" }
    exit 1
}

$siteId = $site.ID
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host " UTMPro - SSL Certificate Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Domain:    $Domain" -ForegroundColor White
Write-Host "IIS Site:  $SiteName (ID: $siteId)" -ForegroundColor White
Write-Host "Email:     $Email" -ForegroundColor White
Write-Host ""

# Test DNS first
Write-Host "Step 1: Checking DNS for $Domain..." -ForegroundColor Yellow
try {
    $dns = Resolve-DnsName -Name $Domain -Type A -ErrorAction Stop
    $resolvedIp = $dns | Where-Object { $_.QueryType -eq 'A' } | Select-Object -First 1 -ExpandProperty IPAddress
    Write-Host "  DNS resolves to: $resolvedIp" -ForegroundColor Green
} catch {
    Write-Host "  WARNING: DNS not resolving yet. Let's Encrypt validation may fail." -ForegroundColor Yellow
    Write-Host "  Make sure the A record points to your server IP." -ForegroundColor Yellow
}

# Issue certificate
Write-Host ""
Write-Host "Step 2: Issuing Let's Encrypt certificate..." -ForegroundColor Yellow
Write-Host "  Running: $WacsPath --target manual --host $Domain --installation iis --siteid $siteId --store certificatestore --accepttos --emailaddress $Email" -ForegroundColor Gray

& $WacsPath --target manual --host $Domain --installation iis --siteid $siteId --store certificatestore --accepttos --emailaddress $Email

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SUCCESS! SSL certificate issued and IIS binding added." -ForegroundColor Green
    Write-Host ""
    Write-Host "Verify:" -ForegroundColor Cyan
    Write-Host "  1. Open https://$Domain/ in your browser" -ForegroundColor White
    Write-Host "  2. Check IIS Manager -> $SiteName -> Bindings" -ForegroundColor White
    Write-Host "  3. Certificate auto-renews via Task Scheduler" -ForegroundColor White
} else {
    Write-Host ""
    Write-Host "FAILED! Certificate issuance failed." -ForegroundColor Red
    Write-Host ""
    Write-Host "Common fixes:" -ForegroundColor Yellow
    Write-Host "  1. Ensure DNS A record points to THIS server" -ForegroundColor White
    Write-Host "  2. Ensure port 80 is open (HTTP-01 validation)" -ForegroundColor White
    Write-Host "  3. Ensure http://$Domain/ is reachable from internet" -ForegroundColor White
    Write-Host "  4. Wait 15-30 min for DNS propagation and retry" -ForegroundColor White
}
