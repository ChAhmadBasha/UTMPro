using UTMPro.Data.Helpers;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Services;

public interface IPartnerService
{
    Task<ServiceResult<PartnerProgram>> CreateProgramAsync(long workspaceId, CreateProgramRequest request);
    Task<ServiceResult<Partner>> RegisterPartnerAsync(long programId, RegisterPartnerRequest request);
    Task<ServiceResult<Partner>> ApprovePartnerAsync(long partnerId, long approvedBy);
    Task<ServiceResult<Partner>> RejectPartnerAsync(long partnerId, string reason, long rejectedBy);
    Task<ServiceResult<PartnerSale>> RecordSaleAsync(RecordSaleRequest request);
    Task<ServiceResult<PartnerPayout>> CreatePayoutAsync(long partnerId, long programId, long workspaceId, decimal amount, string method, long processedBy);
}

public class CreateProgramRequest
{
    public string ProgramName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CommissionType { get; set; } = "Percentage";
    public decimal CommissionValue { get; set; } = 20;
    public string CommissionDuration { get; set; } = "Lifetime";
    public int? CommissionDurationMonths { get; set; }
    public decimal PayoutThreshold { get; set; } = 50;
    public string PayoutFrequency { get; set; } = "Monthly";
    public string PayoutMethod { get; set; } = "Stripe";
    public int CookieDays { get; set; } = 90;
    public bool RequireApplication { get; set; }
    public bool AutoApprove { get; set; } = true;
    public bool IsPublic { get; set; } = true;
}

public class RegisterPartnerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? DestinationUrl { get; set; }
    public string? ApplicationData { get; set; }
}

public class RecordSaleRequest
{
    public string ReferralCode { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public decimal SaleAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ExternalOrderId { get; set; }
    public string? StripeChargeId { get; set; }
}

public class PartnerService : IPartnerService
{
    private readonly IPartnerRepository _partnerRepo;
    private readonly ISystemSettingsRepository _settingsRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<PartnerService> _logger;

    public PartnerService(IPartnerRepository partnerRepo, ISystemSettingsRepository settingsRepo,
        IEmailService emailService, ILogger<PartnerService> logger)
    {
        _partnerRepo = partnerRepo;
        _settingsRepo = settingsRepo;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ServiceResult<PartnerProgram>> CreateProgramAsync(long workspaceId, CreateProgramRequest request)
    {
        var existing = await _partnerRepo.GetProgramByWorkspaceAsync(workspaceId);
        if (existing != null)
            return ServiceResult<PartnerProgram>.Fail("Workspace already has a partner program");

        var slug = request.ProgramName.ToLower().Replace(" ", "-").Replace("--", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        if (await _partnerRepo.GetProgramBySlugAsync(slug) != null)
            slug += "-" + IdGenerator.NewSlug(4).ToLower();

        var program = new PartnerProgram
        {
            WorkspaceId = workspaceId,
            ProgramName = request.ProgramName,
            Slug = slug,
            Description = request.Description,
            CommissionType = request.CommissionType,
            CommissionValue = request.CommissionValue,
            CommissionDuration = request.CommissionDuration,
            CommissionDurationMonths = request.CommissionDurationMonths,
            PayoutThreshold = request.PayoutThreshold,
            PayoutFrequency = request.PayoutFrequency,
            PayoutMethod = request.PayoutMethod,
            CookieDays = request.CookieDays,
            RequireApplication = request.RequireApplication,
            AutoApprove = request.AutoApprove,
            IsPublic = request.IsPublic,
            IsActive = true
        };

        program.Id = await _partnerRepo.CreateProgramAsync(program);
        _logger.LogInformation("Partner program created: {name} for workspace {wsId}", program.ProgramName, workspaceId);
        return ServiceResult<PartnerProgram>.Ok(program);
    }

    public async Task<ServiceResult<Partner>> RegisterPartnerAsync(long programId, RegisterPartnerRequest request)
    {
        var program = await _partnerRepo.GetProgramByIdAsync(programId);
        if (program == null || !program.IsActive)
            return ServiceResult<Partner>.Fail("Partner program not found or inactive");

        var existing = await _partnerRepo.GetPartnerByEmailAndProgramAsync(request.Email, programId);
        if (existing != null)
            return ServiceResult<Partner>.Fail("Email already registered in this program");

        string referralCode;
        do { referralCode = IdGenerator.NewSlug(8); }
        while (await _partnerRepo.ReferralCodeExistsAsync(referralCode));

        var status = program.RequireApplication
            ? (program.AutoApprove ? "Approved" : "Pending")
            : "Approved";

        var partner = new Partner
        {
            ExternalId = IdGenerator.NewExternalId("prt_"),
            ProgramId = programId,
            WorkspaceId = program.WorkspaceId,
            Name = request.Name,
            Email = request.Email.ToLower().Trim(),
            Country = request.Country,
            CountryCode = request.CountryCode,
            ReferralCode = referralCode,
            ReferralUrl = $"{request.DestinationUrl ?? "https://utmpro.link"}?ref={referralCode}",
            ApplicationStatus = status,
            ApplicationData = request.ApplicationData,
            IsActive = true
        };

        if (status == "Approved") partner.ApprovedAt = DateTime.UtcNow;
        partner.Id = await _partnerRepo.CreatePartnerAsync(partner);

        _logger.LogInformation("Partner registered: {email} in program {pid}, status={status}", partner.Email, programId, status);
        return ServiceResult<Partner>.Ok(partner);
    }

    public async Task<ServiceResult<Partner>> ApprovePartnerAsync(long partnerId, long approvedBy)
    {
        var partner = await _partnerRepo.GetPartnerByIdAsync(partnerId);
        if (partner == null) return ServiceResult<Partner>.Fail("Partner not found");

        partner.ApplicationStatus = "Approved";
        partner.ApprovedAt = DateTime.UtcNow;
        partner.ApprovedBy = approvedBy;
        await _partnerRepo.UpdatePartnerAsync(partner);

        return ServiceResult<Partner>.Ok(partner);
    }

    public async Task<ServiceResult<Partner>> RejectPartnerAsync(long partnerId, string reason, long rejectedBy)
    {
        var partner = await _partnerRepo.GetPartnerByIdAsync(partnerId);
        if (partner == null) return ServiceResult<Partner>.Fail("Partner not found");

        partner.ApplicationStatus = "Rejected";
        partner.RejectedAt = DateTime.UtcNow;
        partner.RejectionReason = reason;
        await _partnerRepo.UpdatePartnerAsync(partner);

        return ServiceResult<Partner>.Ok(partner);
    }

    public async Task<ServiceResult<PartnerSale>> RecordSaleAsync(RecordSaleRequest request)
    {
        var partner = await _partnerRepo.GetPartnerByReferralCodeAsync(request.ReferralCode);
        if (partner == null) return ServiceResult<PartnerSale>.Fail("Referral code not found");

        var program = await _partnerRepo.GetProgramByIdAsync(partner.ProgramId);
        if (program == null) return ServiceResult<PartnerSale>.Fail("Program not found");

        // Self-referral check
        var selfCheck = await _settingsRepo.GetValueAsync("SelfReferralDetection");
        if (selfCheck == "true" && request.CustomerEmail?.ToLower() == partner.Email.ToLower())
        {
            await _partnerRepo.CreateFraudEventAsync(new PartnerFraudEvent
            {
                PartnerId = partner.Id, ProgramId = program.Id,
                FraudType = "SelfReferral", Description = $"Self-referral by {partner.Email}", Severity = "High"
            });
            return ServiceResult<PartnerSale>.Fail("Self-referral detected");
        }

        var commission = program.CommissionType == "Percentage"
            ? Math.Round(request.SaleAmount * program.CommissionValue / 100, 2)
            : program.CommissionValue;

        var sale = new PartnerSale
        {
            ExternalId = IdGenerator.NewExternalId("ps_"),
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
            StripeChargeId = request.StripeChargeId,
            ExternalOrderId = request.ExternalOrderId,
            SaleDate = DateTime.UtcNow
        };

        sale.Id = await _partnerRepo.CreateSaleAsync(sale);
        await _partnerRepo.UpdatePartnerStatsAsync(partner.Id, 0, 0, 1, request.SaleAmount, commission, commission);

        _logger.LogInformation("Partner sale recorded: ${amt} from {partner}, commission=${comm}", request.SaleAmount, partner.Name, commission);
        return ServiceResult<PartnerSale>.Ok(sale);
    }

    public async Task<ServiceResult<PartnerPayout>> CreatePayoutAsync(long partnerId, long programId, long workspaceId, decimal amount, string method, long processedBy)
    {
        var partner = await _partnerRepo.GetPartnerByIdAsync(partnerId);
        if (partner == null) return ServiceResult<PartnerPayout>.Fail("Partner not found");
        if (partner.PendingBalance < amount) return ServiceResult<PartnerPayout>.Fail("Insufficient balance");

        var payout = new PartnerPayout
        {
            ExternalId = IdGenerator.NewExternalId("po_"),
            PartnerId = partnerId,
            ProgramId = programId,
            WorkspaceId = workspaceId,
            Amount = amount,
            Currency = "USD",
            PayoutMethod = method,
            Status = "Pending",
            PeriodEnd = DateTime.UtcNow,
            ProcessedBy = processedBy
        };

        payout.Id = await _partnerRepo.CreatePayoutAsync(payout);
        return ServiceResult<PartnerPayout>.Ok(payout);
    }
}
