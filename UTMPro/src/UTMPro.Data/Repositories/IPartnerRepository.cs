using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public interface IPartnerRepository
{
    // Programs
    Task<PartnerProgram?> GetProgramByIdAsync(long id);
    Task<PartnerProgram?> GetProgramByWorkspaceAsync(long workspaceId);
    Task<PartnerProgram?> GetProgramBySlugAsync(string slug);
    Task<long> CreateProgramAsync(PartnerProgram program);
    Task UpdateProgramAsync(PartnerProgram program);
    Task<List<PartnerProgram>> GetAllProgramsAsync();
    // Partners
    Task<Partner?> GetPartnerByIdAsync(long id);
    Task<Partner?> GetPartnerByEmailAndProgramAsync(string email, long programId);
    Task<Partner?> GetPartnerByReferralCodeAsync(string code);
    Task<long> CreatePartnerAsync(Partner partner);
    Task UpdatePartnerAsync(Partner partner);
    Task<List<Partner>> GetPartnersByProgramAsync(long programId, string? status, int page, int pageSize);
    Task<int> GetPartnerCountByProgramAsync(long programId, string? status);
    Task UpdatePartnerStatsAsync(long partnerId, long clicks, int leads, int sales, decimal revenue, decimal commission, decimal pendingBalance);
    Task<bool> ReferralCodeExistsAsync(string code);
    // Sales
    Task<long> CreateSaleAsync(PartnerSale sale);
    Task<PartnerSale?> GetSaleByIdAsync(long id);
    Task UpdateSaleStatusAsync(long id, string status);
    Task<List<PartnerSale>> GetSalesByProgramAsync(long programId, string? status, int page, int pageSize);
    Task<List<PartnerSale>> GetSalesByPartnerAsync(long partnerId, int page, int pageSize);
    // Payouts
    Task<long> CreatePayoutAsync(PartnerPayout payout);
    Task<PartnerPayout?> GetPayoutByIdAsync(long id);
    Task UpdatePayoutStatusAsync(long id, string status, string? failureReason);
    Task<List<PartnerPayout>> GetPayoutsByProgramAsync(long programId, int page, int pageSize);
    Task<List<PartnerPayout>> GetPayoutsByPartnerAsync(long partnerId, int page, int pageSize);
    // Bounties
    Task<long> CreateBountyAsync(PartnerBounty bounty);
    Task<List<PartnerBounty>> GetBountiesByProgramAsync(long programId);
    // Messages
    Task<long> CreateMessageAsync(PartnerMessage message);
    Task<List<PartnerMessage>> GetMessagesByProgramAsync(long programId, int page, int pageSize);
    Task<List<PartnerMessage>> GetMessagesByPartnerAsync(long partnerId, int page, int pageSize);
    // Fraud
    Task<long> CreateFraudEventAsync(PartnerFraudEvent evt);
    Task<List<PartnerFraudEvent>> GetFraudEventsByProgramAsync(long programId, int page, int pageSize);
    Task ResolveFraudEventAsync(long id, string resolution, long resolvedBy);
}
