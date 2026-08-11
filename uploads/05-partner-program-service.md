# PART 5: PARTNER PROGRAM SERVICE

```csharp
// ============================================================
// File: UTMPro.Web/Services/Phase2/PartnerService.cs
// ============================================================
namespace UTMPro.Web.Services;

public interface IPartnerService
{
    Task<ServiceResult<PartnerProgram>> CreateProgramAsync(
        long workspaceId, CreateProgramRequest request, 
        long createdBy);

    Task<ServiceResult<Partner>> RegisterPartnerAsync(
        long programId, RegisterPartnerRequest request);

    Task<ServiceResult<Partner>> ApprovePartnerAsync(
        long partnerId, long approvedBy);

    Task<ServiceResult<PartnerSale>> RecordSaleAsync(
        RecordSaleRequest request);

    Task<ServiceResult<PartnerPayout>> ProcessPayoutAsync(
        long partnerId, decimal amount, 
        string method, long processedBy);

    Task<string> GenerateReferralCodeAsync();
    Task<string> GenerateReferralUrlAsync(
        string programSlug, string referralCode, 
        string destinationUrl);
    Task AttributeClickAsync(
        string referralCode, long clickEventId, string ip);
    Task DetectFraudAsync(long partnerId, long programId);
}

public class PartnerService : IPartnerService
{
    private readonly IPartnerRepository _partnerRepo;
    private readonly IWebhookService _webhookService;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PartnerService> _logger;

    public async Task<ServiceResult<PartnerProgram>> 
        CreateProgramAsync(
            long workspaceId, 
            CreateProgramRequest request,
            long createdBy)
    {
        // Validate workspace doesn't already have a program
        var existing = await _partnerRepo
            .GetProgramByWorkspaceAsync(workspaceId);
        if (existing != null)
            return ServiceResult<PartnerProgram>.Fail(
                "Workspace already has a partner program");

        // Generate unique slug
        var slug = await GenerateUniqueProgramSlugAsync(
            request.ProgramName);

        var program = new PartnerProgram
        {
            WorkspaceId = workspaceId,
            ProgramName = request.ProgramName,
            Slug = slug,
            CommissionType = request.CommissionType,
            CommissionValue = request.CommissionValue,
            CommissionDuration = request.CommissionDuration,
            CommissionDurationMonths = 
                request.CommissionDurationMonths,
            PayoutThreshold = request.PayoutThreshold,
            PayoutFrequency = request.PayoutFrequency,
            PayoutMethod = request.PayoutMethod,
            CookieDays = request.CookieDays,
            RequireApplication = request.RequireApplication,
            AutoApprove = request.AutoApprove,
            IsPublic = request.IsPublic,
            IsActive = true
        };

        var id = await _partnerRepo.CreateProgramAsync(program);
        program.Id = id;

        return ServiceResult<PartnerProgram>.Ok(program);
    }

    public async Task<ServiceResult<Partner>> 
        RegisterPartnerAsync(
            long programId, 
            RegisterPartnerRequest request)
    {
        var program = await _partnerRepo
            .GetProgramByIdAsync(programId);
        if (program == null || !program.IsActive)
            return ServiceResult<Partner>.Fail(
                "Partner program not found or inactive");

        // Check if already registered
        var existing = await _partnerRepo
            .GetPartnerByEmailAndProgramAsync(
                request.Email, programId);
        if (existing != null)
            return ServiceResult<Partner>.Fail(
                "Email already registered in this program");

        var referralCode = await GenerateReferralCodeAsync();
        var referralUrl = await GenerateReferralUrlAsync(
            program.Slug, referralCode, 
            request.DestinationUrl ?? "https://utmpro.co");

        var status = program.RequireApplication
            ? (program.AutoApprove ? "Approved" : "Pending")
            : "Approved";

        var partner = new Partner
        {
            ExternalId = GenerateExternalId("prt"),
            ProgramId = programId,
            WorkspaceId = program.WorkspaceId,
            Name = request.Name,
            Email = request.Email,
            Country = request.Country,
            CountryCode = request.CountryCode,
            ReferralCode = referralCode,
            ReferralUrl = referralUrl,
            ApplicationStatus = status,
            ApplicationData = request.ApplicationData,
            IsActive = true
        };

        if (status == "Approved")
            partner.ApprovedAt = DateTime.UtcNow;

        var id = await _partnerRepo.CreatePartnerAsync(partner);
        partner.Id = id;

        // Send welcome email
        await _emailService.SendPartnerWelcomeAsync(
            partner.Email, partner.Name, 
            program.ProgramName, referralUrl);

        // Notify workspace owner if pending
        if (status == "Pending")
        {
            await _notificationService.SendAsync(
                program.WorkspaceId, "NewPartnerApplication",
                $"New partner application from {partner.Name}");
        }

        // Fire webhook
        await _webhookService.FireAsync(
            program.WorkspaceId, "partner.joined",
            new { partner.Id, partner.Name, partner.Email,
                  partner.ReferralCode, status });

        return ServiceResult<Partner>.Ok(partner);
    }

    public async Task<ServiceResult<PartnerSale>> 
        RecordSaleAsync(RecordSaleRequest request)
    {
        // Find partner by referral code
        var partner = await _partnerRepo
            .GetPartnerByReferralCodeAsync(request.ReferralCode);
        if (partner == null)
            return ServiceResult<PartnerSale>.Fail(
                "Referral code not found");

        // Check cookie window (attribution)
        var program = await _partnerRepo
            .GetProgramByIdAsync(partner.ProgramId);
        if (program == null)
            return ServiceResult<PartnerSale>.Fail(
                "Program not found");

        // Self-referral check
        var selfReferralEnabled = await GetSettingAsync(
            "SelfReferralDetection");
        if (selfReferralEnabled == "true" &&
            request.CustomerEmail == partner.Email)
        {
            await LogFraudAsync(partner.Id, program.Id,
                "SelfReferral", partner.Email, "High");
            return ServiceResult<PartnerSale>.Fail(
                "Self-referral detected");
        }

        // Calculate commission
        var commission = CalculateCommission(
            program, request.SaleAmount);

        var sale = new PartnerSale
        {
            ExternalId = GenerateExternalId("ps"),
            PartnerId = partner.Id,
            ProgramId = partner.ProgramId,
            WorkspaceId = partner.WorkspaceId,
            CustomerEmail = request.CustomerEmail,
            SaleAmount = request.SaleAmount,
            Currency = request.Currency,
            CommissionType = program.CommissionType,
            CommissionRate = program.CommissionValue,
            CommissionAmount = commission,
            Status = "Pending",
            ReferralCode = request.ReferralCode,
            ExternalOrderId = request.ExternalOrderId,
            StripeChargeId = request.StripeChargeId,
            SaleDate = DateTime.UtcNow
        };

        var id = await _partnerRepo.CreateSaleAsync(sale);
        sale.Id = id;

        // Update partner stats
        await _partnerRepo.UpdatePartnerStatsAsync(
            partner.Id, 
            clicks: 0, 
            leads: 0, 
            sales: 1,
            revenue: request.SaleAmount,
            commission: commission,
            pendingBalance: commission);

        // Fire webhook
        await _webhookService.FireAsync(
            partner.WorkspaceId, "partner.sale",
            new { sale.Id, sale.ExternalId, 
                  sale.SaleAmount, sale.CommissionAmount,
                  sale.Currency, partner.Name, 
                  partner.Email });

        // Notify workspace owner
        await _notificationService.SendAsync(
            partner.WorkspaceId, "NewPartnerSale",
            $"New sale of ${sale.SaleAmount} from " +
            $"{partner.Name}");

        return ServiceResult<PartnerSale>.Ok(sale);
    }

    private decimal CalculateCommission(
        PartnerProgram program, decimal saleAmount)
    {
        return program.CommissionType switch
        {
            "Percentage" => 
                Math.Round(saleAmount * program.CommissionValue 
                           / 100, 2),
            "FlatRate"   => program.CommissionValue,
            _            => 0
        };
    }

    public async Task<string> GenerateReferralCodeAsync()
    {
        const string chars = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string code;
        bool exists;
        do
        {
            code = new string(
                Enumerable.Repeat(chars, 8)
                    .Select(s => s[Random.Shared.Next(s.Length)])
                    .ToArray());
            exists = await _partnerRepo
                .ReferralCodeExistsAsync(code);
        } while (exists);

        return code;
    }

    private string GenerateExternalId(string prefix)
    {
        const string chars = 
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new string(
            Enumerable.Repeat(chars, 20)
                .Select(s => s[Random.Shared.Next(s.Length)])
                .ToArray());
        return $"{prefix}_{random}";
    }
}
```

---
