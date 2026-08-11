using Microsoft.Data.SqlClient;
using UTMPro.Data.Models;

namespace UTMPro.Data.Repositories;

public class PartnerRepository : IPartnerRepository
{
    private readonly IDbConnectionFactory _db;
    public PartnerRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PartnerProgram?> GetProgramByIdAsync(long id)
    {
        const string sql = "SELECT * FROM PartnerPrograms WHERE Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapProgram(r) : null;
    }

    public async Task<PartnerProgram?> GetProgramByWorkspaceAsync(long workspaceId)
    {
        const string sql = "SELECT * FROM PartnerPrograms WHERE WorkspaceId = @WsId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", workspaceId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapProgram(r) : null;
    }

    public async Task<PartnerProgram?> GetProgramBySlugAsync(string slug)
    {
        const string sql = "SELECT * FROM PartnerPrograms WHERE Slug = @Slug AND IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Slug", slug);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapProgram(r) : null;
    }

    public async Task<long> CreateProgramAsync(PartnerProgram p)
    {
        const string sql = @"INSERT INTO PartnerPrograms (WorkspaceId,ProgramName,Slug,LogoUrl,BrandColor,Description,CommissionType,CommissionValue,CommissionDuration,CommissionDurationMonths,PayoutThreshold,PayoutFrequency,PayoutMethod,CookieDays,RequireApplication,AutoApprove,ApplicationQuestions,TermsUrl,TermsText,IsPublic,IsActive,CreatedAt,UpdatedAt)
            VALUES (@WsId,@Name,@Slug,@Logo,@Color,@Desc,@CType,@CVal,@CDur,@CDurM,@POT,@POF,@POM,@CD,@RA,@AA,@AQ,@TU,@TT,@Pub,1,GETUTCDATE(),GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@WsId", p.WorkspaceId);
        cmd.Parameters.AddWithValue("@Name", p.ProgramName);
        cmd.Parameters.AddWithValue("@Slug", p.Slug);
        cmd.Parameters.AddWithValue("@Logo", (object?)p.LogoUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Color", p.BrandColor);
        cmd.Parameters.AddWithValue("@Desc", (object?)p.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CType", p.CommissionType);
        cmd.Parameters.AddWithValue("@CVal", p.CommissionValue);
        cmd.Parameters.AddWithValue("@CDur", p.CommissionDuration);
        cmd.Parameters.AddWithValue("@CDurM", (object?)p.CommissionDurationMonths ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@POT", p.PayoutThreshold);
        cmd.Parameters.AddWithValue("@POF", p.PayoutFrequency);
        cmd.Parameters.AddWithValue("@POM", p.PayoutMethod);
        cmd.Parameters.AddWithValue("@CD", p.CookieDays);
        cmd.Parameters.AddWithValue("@RA", p.RequireApplication);
        cmd.Parameters.AddWithValue("@AA", p.AutoApprove);
        cmd.Parameters.AddWithValue("@AQ", (object?)p.ApplicationQuestions ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TU", (object?)p.TermsUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TT", (object?)p.TermsText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Pub", p.IsPublic);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdateProgramAsync(PartnerProgram p)
    {
        const string sql = @"UPDATE PartnerPrograms SET ProgramName=@Name,Description=@Desc,CommissionType=@CType,CommissionValue=@CVal,CommissionDuration=@CDur,CommissionDurationMonths=@CDurM,PayoutThreshold=@POT,PayoutFrequency=@POF,CookieDays=@CD,RequireApplication=@RA,AutoApprove=@AA,IsPublic=@Pub,IsActive=@Active,UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", p.Id);
        cmd.Parameters.AddWithValue("@Name", p.ProgramName);
        cmd.Parameters.AddWithValue("@Desc", (object?)p.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CType", p.CommissionType);
        cmd.Parameters.AddWithValue("@CVal", p.CommissionValue);
        cmd.Parameters.AddWithValue("@CDur", p.CommissionDuration);
        cmd.Parameters.AddWithValue("@CDurM", (object?)p.CommissionDurationMonths ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@POT", p.PayoutThreshold);
        cmd.Parameters.AddWithValue("@POF", p.PayoutFrequency);
        cmd.Parameters.AddWithValue("@CD", p.CookieDays);
        cmd.Parameters.AddWithValue("@RA", p.RequireApplication);
        cmd.Parameters.AddWithValue("@AA", p.AutoApprove);
        cmd.Parameters.AddWithValue("@Pub", p.IsPublic);
        cmd.Parameters.AddWithValue("@Active", p.IsActive);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<PartnerProgram>> GetAllProgramsAsync()
    {
        const string sql = "SELECT pp.*, w.Name AS WorkspaceName FROM PartnerPrograms pp INNER JOIN Workspaces w ON pp.WorkspaceId = w.Id ORDER BY pp.CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        var list = new List<PartnerProgram>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) { var p = MapProgram(r); try { p.WorkspaceName = r.GetString(r.GetOrdinal("WorkspaceName")); } catch { } list.Add(p); }
        return list;
    }

    // Partners
    public async Task<Partner?> GetPartnerByIdAsync(long id)
    {
        const string sql = "SELECT p.*, pp.ProgramName FROM Partners p INNER JOIN PartnerPrograms pp ON p.ProgramId = pp.Id WHERE p.Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapPartner(r) : null;
    }

    public async Task<Partner?> GetPartnerByEmailAndProgramAsync(string email, long programId)
    {
        const string sql = "SELECT * FROM Partners WHERE Email = @Email AND ProgramId = @PId";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@PId", programId);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapPartner(r) : null;
    }

    public async Task<Partner?> GetPartnerByReferralCodeAsync(string code)
    {
        const string sql = "SELECT * FROM Partners WHERE ReferralCode = @Code AND IsActive = 1";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Code", code);
        await using var r = await cmd.ExecuteReaderAsync();
        return await r.ReadAsync() ? MapPartner(r) : null;
    }

    public async Task<long> CreatePartnerAsync(Partner p)
    {
        const string sql = @"INSERT INTO Partners (ExternalId,ProgramId,WorkspaceId,UserId,Name,Email,AvatarUrl,Country,CountryCode,ReferralCode,ReferralUrl,ApplicationStatus,ApplicationData,ApprovedAt,ApprovedBy,IsActive,CreatedAt,UpdatedAt)
            VALUES (@Eid,@Pid,@Wid,@Uid,@Name,@Email,@Avatar,@Country,@CC,@RC,@RU,@AS,@AD,@ApAt,@ApBy,1,GETUTCDATE(),GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Eid", p.ExternalId);
        cmd.Parameters.AddWithValue("@Pid", p.ProgramId);
        cmd.Parameters.AddWithValue("@Wid", p.WorkspaceId);
        cmd.Parameters.AddWithValue("@Uid", (object?)p.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Name", p.Name);
        cmd.Parameters.AddWithValue("@Email", p.Email);
        cmd.Parameters.AddWithValue("@Avatar", (object?)p.AvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Country", (object?)p.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CC", (object?)p.CountryCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RC", p.ReferralCode);
        cmd.Parameters.AddWithValue("@RU", p.ReferralUrl);
        cmd.Parameters.AddWithValue("@AS", p.ApplicationStatus);
        cmd.Parameters.AddWithValue("@AD", (object?)p.ApplicationData ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApAt", (object?)p.ApprovedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApBy", (object?)p.ApprovedBy ?? DBNull.Value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdatePartnerAsync(Partner p)
    {
        const string sql = "UPDATE Partners SET ApplicationStatus=@AS,ApprovedAt=@ApAt,ApprovedBy=@ApBy,RejectedAt=@RjAt,RejectionReason=@RjR,IsActive=@Active,IsFlagged=@Flag,FraudScore=@FS,UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", p.Id);
        cmd.Parameters.AddWithValue("@AS", p.ApplicationStatus);
        cmd.Parameters.AddWithValue("@ApAt", (object?)p.ApprovedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ApBy", (object?)p.ApprovedBy ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RjAt", (object?)p.RejectedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@RjR", (object?)p.RejectionReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Active", p.IsActive);
        cmd.Parameters.AddWithValue("@Flag", p.IsFlagged);
        cmd.Parameters.AddWithValue("@FS", p.FraudScore);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Partner>> GetPartnersByProgramAsync(long programId, string? status, int page, int pageSize)
    {
        const string sql = @"SELECT * FROM Partners WHERE ProgramId = @Pid AND (@Status IS NULL OR ApplicationStatus = @Status) ORDER BY CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", programId);
        cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<Partner>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapPartner(r));
        return list;
    }

    public async Task<int> GetPartnerCountByProgramAsync(long programId, string? status)
    {
        const string sql = "SELECT COUNT(*) FROM Partners WHERE ProgramId = @Pid AND (@Status IS NULL OR ApplicationStatus = @Status)";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", programId);
        cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task UpdatePartnerStatsAsync(long partnerId, long clicks, int leads, int sales, decimal revenue, decimal commission, decimal pendingBalance)
    {
        const string sql = "UPDATE Partners SET TotalClicks=TotalClicks+@C,TotalLeads=TotalLeads+@L,TotalSales=TotalSales+@S,TotalRevenue=TotalRevenue+@R,TotalCommission=TotalCommission+@Co,PendingBalance=PendingBalance+@PB,UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", partnerId);
        cmd.Parameters.AddWithValue("@C", clicks);
        cmd.Parameters.AddWithValue("@L", leads);
        cmd.Parameters.AddWithValue("@S", sales);
        cmd.Parameters.AddWithValue("@R", revenue);
        cmd.Parameters.AddWithValue("@Co", commission);
        cmd.Parameters.AddWithValue("@PB", pendingBalance);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ReferralCodeExistsAsync(string code)
    {
        const string sql = "SELECT COUNT(*) FROM Partners WHERE ReferralCode = @Code";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Code", code);
        return (int)(await cmd.ExecuteScalarAsync())! > 0;
    }

    // Sales
    public async Task<long> CreateSaleAsync(PartnerSale s)
    {
        const string sql = @"INSERT INTO PartnerSales (ExternalId,PartnerId,ProgramId,WorkspaceId,CustomerEmail,CustomerId,SaleAmount,Currency,CommissionType,CommissionRate,CommissionAmount,Status,ReferralCode,ClickId,StripeChargeId,ExternalOrderId,SaleDate,CreatedAt)
            VALUES (@Eid,@Pid,@Pgid,@Wid,@CE,@Cid,@SA,@Cur,@CT,@CR,@CA,@St,@RC,@CkId,@SCI,@EOI,@SD,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Eid", s.ExternalId);
        cmd.Parameters.AddWithValue("@Pid", s.PartnerId);
        cmd.Parameters.AddWithValue("@Pgid", s.ProgramId);
        cmd.Parameters.AddWithValue("@Wid", s.WorkspaceId);
        cmd.Parameters.AddWithValue("@CE", (object?)s.CustomerEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Cid", (object?)s.CustomerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SA", s.SaleAmount);
        cmd.Parameters.AddWithValue("@Cur", s.Currency);
        cmd.Parameters.AddWithValue("@CT", s.CommissionType);
        cmd.Parameters.AddWithValue("@CR", s.CommissionRate);
        cmd.Parameters.AddWithValue("@CA", s.CommissionAmount);
        cmd.Parameters.AddWithValue("@St", s.Status);
        cmd.Parameters.AddWithValue("@RC", (object?)s.ReferralCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CkId", (object?)s.ClickId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SCI", (object?)s.StripeChargeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@EOI", (object?)s.ExternalOrderId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@SD", s.SaleDate);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<PartnerSale?> GetSaleByIdAsync(long id)
    {
        const string sql = "SELECT ps.*, p.Name AS PartnerName, p.Email AS PartnerEmail FROM PartnerSales ps INNER JOIN Partners p ON ps.PartnerId = p.Id WHERE ps.Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new PartnerSale { Id = r.GetInt64(r.GetOrdinal("Id")), ExternalId = r.GetString(r.GetOrdinal("ExternalId")), PartnerId = r.GetInt64(r.GetOrdinal("PartnerId")), SaleAmount = r.GetDecimal(r.GetOrdinal("SaleAmount")), CommissionAmount = r.GetDecimal(r.GetOrdinal("CommissionAmount")), Status = r.GetString(r.GetOrdinal("Status")), SaleDate = r.GetDateTime(r.GetOrdinal("SaleDate")), PartnerName = r.GetString(r.GetOrdinal("PartnerName")), PartnerEmail = r.GetString(r.GetOrdinal("PartnerEmail")) };
    }

    public async Task UpdateSaleStatusAsync(long id, string status)
    {
        var sql = $"UPDATE PartnerSales SET Status=@St, {(status == "Approved" ? "ApprovedAt" : status == "Paid" ? "PaidAt" : "ReversedAt")}=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@St", status);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<PartnerSale>> GetSalesByProgramAsync(long programId, string? status, int page, int pageSize)
    {
        const string sql = @"SELECT ps.*, p.Name AS PartnerName, p.Email AS PartnerEmail FROM PartnerSales ps INNER JOIN Partners p ON ps.PartnerId = p.Id WHERE ps.ProgramId = @Pid AND (@St IS NULL OR ps.Status = @St) ORDER BY ps.SaleDate DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", programId);
        cmd.Parameters.AddWithValue("@St", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<PartnerSale>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerSale { Id = r.GetInt64(r.GetOrdinal("Id")), ExternalId = r.GetString(r.GetOrdinal("ExternalId")), PartnerId = r.GetInt64(r.GetOrdinal("PartnerId")), SaleAmount = r.GetDecimal(r.GetOrdinal("SaleAmount")), CommissionAmount = r.GetDecimal(r.GetOrdinal("CommissionAmount")), Status = r.GetString(r.GetOrdinal("Status")), SaleDate = r.GetDateTime(r.GetOrdinal("SaleDate")), PartnerName = r.GetString(r.GetOrdinal("PartnerName")), PartnerEmail = r.GetString(r.GetOrdinal("PartnerEmail")), Currency = r.GetString(r.GetOrdinal("Currency")) });
        return list;
    }

    public async Task<List<PartnerSale>> GetSalesByPartnerAsync(long partnerId, int page, int pageSize)
    {
        const string sql = "SELECT * FROM PartnerSales WHERE PartnerId = @Pid ORDER BY SaleDate DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", partnerId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<PartnerSale>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerSale { Id = r.GetInt64(r.GetOrdinal("Id")), ExternalId = r.GetString(r.GetOrdinal("ExternalId")), SaleAmount = r.GetDecimal(r.GetOrdinal("SaleAmount")), CommissionAmount = r.GetDecimal(r.GetOrdinal("CommissionAmount")), Status = r.GetString(r.GetOrdinal("Status")), SaleDate = r.GetDateTime(r.GetOrdinal("SaleDate")) });
        return list;
    }

    // Payouts
    public async Task<long> CreatePayoutAsync(PartnerPayout p)
    {
        const string sql = @"INSERT INTO PartnerPayouts (ExternalId,PartnerId,ProgramId,WorkspaceId,Amount,Currency,PayoutMethod,Status,PeriodStart,PeriodEnd,Notes,CreatedAt,UpdatedAt) VALUES (@Eid,@Pid,@Pgid,@Wid,@Amt,@Cur,@PM,'Pending',@PS,@PE,@Notes,GETUTCDATE(),GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Eid", p.ExternalId);
        cmd.Parameters.AddWithValue("@Pid", p.PartnerId);
        cmd.Parameters.AddWithValue("@Pgid", p.ProgramId);
        cmd.Parameters.AddWithValue("@Wid", p.WorkspaceId);
        cmd.Parameters.AddWithValue("@Amt", p.Amount);
        cmd.Parameters.AddWithValue("@Cur", p.Currency);
        cmd.Parameters.AddWithValue("@PM", p.PayoutMethod);
        cmd.Parameters.AddWithValue("@PS", (object?)p.PeriodStart ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PE", (object?)p.PeriodEnd ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes", (object?)p.Notes ?? DBNull.Value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<PartnerPayout?> GetPayoutByIdAsync(long id)
    {
        const string sql = "SELECT pp.*, p.Name AS PartnerName, p.Email AS PartnerEmail FROM PartnerPayouts pp INNER JOIN Partners p ON pp.PartnerId = p.Id WHERE pp.Id = @Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new PartnerPayout { Id = r.GetInt64(r.GetOrdinal("Id")), ExternalId = r.GetString(r.GetOrdinal("ExternalId")), Amount = r.GetDecimal(r.GetOrdinal("Amount")), Status = r.GetString(r.GetOrdinal("Status")), PayoutMethod = r.GetString(r.GetOrdinal("PayoutMethod")), PartnerName = r.GetString(r.GetOrdinal("PartnerName")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")) };
    }

    public async Task UpdatePayoutStatusAsync(long id, string status, string? failureReason)
    {
        const string sql = "UPDATE PartnerPayouts SET Status=@St,FailureReason=@FR,ProcessedAt=GETUTCDATE(),UpdatedAt=GETUTCDATE() WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@St", status);
        cmd.Parameters.AddWithValue("@FR", (object?)failureReason ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<PartnerPayout>> GetPayoutsByProgramAsync(long programId, int page, int pageSize)
    {
        const string sql = "SELECT pp.*, p.Name AS PartnerName, p.Email AS PartnerEmail FROM PartnerPayouts pp INNER JOIN Partners p ON pp.PartnerId = p.Id WHERE pp.ProgramId = @Pid ORDER BY pp.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", programId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<PartnerPayout>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerPayout { Id = r.GetInt64(r.GetOrdinal("Id")), ExternalId = r.GetString(r.GetOrdinal("ExternalId")), Amount = r.GetDecimal(r.GetOrdinal("Amount")), Status = r.GetString(r.GetOrdinal("Status")), PayoutMethod = r.GetString(r.GetOrdinal("PayoutMethod")), PartnerName = r.GetString(r.GetOrdinal("PartnerName")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")) });
        return list;
    }

    public async Task<List<PartnerPayout>> GetPayoutsByPartnerAsync(long partnerId, int page, int pageSize)
    {
        const string sql = "SELECT * FROM PartnerPayouts WHERE PartnerId = @Pid ORDER BY CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", partnerId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<PartnerPayout>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerPayout { Id = r.GetInt64(r.GetOrdinal("Id")), Amount = r.GetDecimal(r.GetOrdinal("Amount")), Status = r.GetString(r.GetOrdinal("Status")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")) });
        return list;
    }

    // Bounties
    public async Task<long> CreateBountyAsync(PartnerBounty b)
    {
        const string sql = "INSERT INTO PartnerBounties (ProgramId,Title,Description,BountyAmount,Currency,BountyType,MaxClaims,IsActive,ExpiresAt,CreatedAt) VALUES (@Pid,@T,@D,@A,@C,@BT,@MC,1,@E,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", b.ProgramId);
        cmd.Parameters.AddWithValue("@T", b.Title);
        cmd.Parameters.AddWithValue("@D", (object?)b.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@A", b.BountyAmount);
        cmd.Parameters.AddWithValue("@C", b.Currency);
        cmd.Parameters.AddWithValue("@BT", b.BountyType);
        cmd.Parameters.AddWithValue("@MC", (object?)b.MaxClaims ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@E", (object?)b.ExpiresAt ?? DBNull.Value);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<List<PartnerBounty>> GetBountiesByProgramAsync(long programId)
    {
        const string sql = "SELECT * FROM PartnerBounties WHERE ProgramId = @Pid ORDER BY CreatedAt DESC";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", programId);
        var list = new List<PartnerBounty>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerBounty { Id = r.GetInt64(r.GetOrdinal("Id")), ProgramId = r.GetInt64(r.GetOrdinal("ProgramId")), Title = r.GetString(r.GetOrdinal("Title")), BountyAmount = r.GetDecimal(r.GetOrdinal("BountyAmount")), BountyType = r.GetString(r.GetOrdinal("BountyType")), TotalClaims = r.GetInt32(r.GetOrdinal("TotalClaims")), IsActive = r.GetBoolean(r.GetOrdinal("IsActive")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")) });
        return list;
    }

    // Messages
    public async Task<long> CreateMessageAsync(PartnerMessage m)
    {
        const string sql = "INSERT INTO PartnerMessages (ProgramId,PartnerId,SenderId,Subject,Body,CreatedAt) VALUES (@Pid,@Ptid,@Sid,@Sub,@Body,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", m.ProgramId);
        cmd.Parameters.AddWithValue("@Ptid", (object?)m.PartnerId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Sid", m.SenderId);
        cmd.Parameters.AddWithValue("@Sub", m.Subject);
        cmd.Parameters.AddWithValue("@Body", m.Body);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<List<PartnerMessage>> GetMessagesByProgramAsync(long programId, int page, int pageSize)
    {
        const string sql = "SELECT pm.*, u.Name AS SenderName FROM PartnerMessages pm INNER JOIN Users u ON pm.SenderId = u.Id WHERE pm.ProgramId = @Pid ORDER BY pm.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", programId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<PartnerMessage>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerMessage { Id = r.GetInt64(r.GetOrdinal("Id")), Subject = r.GetString(r.GetOrdinal("Subject")), Body = r.GetString(r.GetOrdinal("Body")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")), SenderName = r.GetString(r.GetOrdinal("SenderName")) });
        return list;
    }

    public async Task<List<PartnerMessage>> GetMessagesByPartnerAsync(long partnerId, int page, int pageSize)
    {
        const string sql = "SELECT pm.*, u.Name AS SenderName FROM PartnerMessages pm INNER JOIN Users u ON pm.SenderId = u.Id WHERE (pm.PartnerId = @Pid OR pm.PartnerId IS NULL) AND pm.ProgramId IN (SELECT ProgramId FROM Partners WHERE Id = @Pid) ORDER BY pm.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", partnerId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<PartnerMessage>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerMessage { Id = r.GetInt64(r.GetOrdinal("Id")), Subject = r.GetString(r.GetOrdinal("Subject")), Body = r.GetString(r.GetOrdinal("Body")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")), SenderName = r.GetString(r.GetOrdinal("SenderName")) });
        return list;
    }

    // Fraud
    public async Task<long> CreateFraudEventAsync(PartnerFraudEvent e)
    {
        const string sql = "INSERT INTO PartnerFraudEvents (PartnerId,ProgramId,FraudType,Description,Severity,CreatedAt) VALUES (@Pid,@Pgid,@FT,@Desc,@Sev,GETUTCDATE()); SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", e.PartnerId);
        cmd.Parameters.AddWithValue("@Pgid", e.ProgramId);
        cmd.Parameters.AddWithValue("@FT", e.FraudType);
        cmd.Parameters.AddWithValue("@Desc", (object?)e.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Sev", e.Severity);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<List<PartnerFraudEvent>> GetFraudEventsByProgramAsync(long programId, int page, int pageSize)
    {
        const string sql = "SELECT pfe.*, p.Name AS PartnerName, p.Email AS PartnerEmail FROM PartnerFraudEvents pfe INNER JOIN Partners p ON pfe.PartnerId = p.Id WHERE pfe.ProgramId = @Pid ORDER BY pfe.CreatedAt DESC OFFSET @Off ROWS FETCH NEXT @PS ROWS ONLY";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Pid", programId);
        cmd.Parameters.AddWithValue("@Off", (page - 1) * pageSize);
        cmd.Parameters.AddWithValue("@PS", pageSize);
        var list = new List<PartnerFraudEvent>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(new PartnerFraudEvent { Id = r.GetInt64(r.GetOrdinal("Id")), FraudType = r.GetString(r.GetOrdinal("FraudType")), Severity = r.GetString(r.GetOrdinal("Severity")), IsResolved = r.GetBoolean(r.GetOrdinal("IsResolved")), CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")), PartnerName = r.GetString(r.GetOrdinal("PartnerName")), PartnerEmail = r.GetString(r.GetOrdinal("PartnerEmail")), Description = r.IsDBNull(r.GetOrdinal("Description")) ? null : r.GetString(r.GetOrdinal("Description")) });
        return list;
    }

    public async Task ResolveFraudEventAsync(long id, string resolution, long resolvedBy)
    {
        const string sql = "UPDATE PartnerFraudEvents SET IsResolved=1,ResolvedAt=GETUTCDATE(),ResolvedBy=@RBy,Resolution=@Res WHERE Id=@Id";
        await using var conn = await _db.CreateOpenConnectionAsync();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@RBy", resolvedBy);
        cmd.Parameters.AddWithValue("@Res", resolution);
        await cmd.ExecuteNonQueryAsync();
    }

    private static PartnerProgram MapProgram(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        ProgramName = r.GetString(r.GetOrdinal("ProgramName")),
        Slug = r.GetString(r.GetOrdinal("Slug")),
        CommissionType = r.GetString(r.GetOrdinal("CommissionType")),
        CommissionValue = r.GetDecimal(r.GetOrdinal("CommissionValue")),
        CommissionDuration = r.GetString(r.GetOrdinal("CommissionDuration")),
        CookieDays = r.GetInt32(r.GetOrdinal("CookieDays")),
        PayoutThreshold = r.GetDecimal(r.GetOrdinal("PayoutThreshold")),
        RequireApplication = r.GetBoolean(r.GetOrdinal("RequireApplication")),
        AutoApprove = r.GetBoolean(r.GetOrdinal("AutoApprove")),
        IsPublic = r.GetBoolean(r.GetOrdinal("IsPublic")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        TotalPartners = r.GetInt32(r.GetOrdinal("TotalPartners")),
        TotalRevenue = r.GetDecimal(r.GetOrdinal("TotalRevenue")),
        TotalPayouts = r.GetDecimal(r.GetOrdinal("TotalPayouts")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        UpdatedAt = r.GetDateTime(r.GetOrdinal("UpdatedAt")),
    };

    private static Partner MapPartner(SqlDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        ExternalId = r.GetString(r.GetOrdinal("ExternalId")),
        ProgramId = r.GetInt64(r.GetOrdinal("ProgramId")),
        WorkspaceId = r.GetInt64(r.GetOrdinal("WorkspaceId")),
        Name = r.GetString(r.GetOrdinal("Name")),
        Email = r.GetString(r.GetOrdinal("Email")),
        ReferralCode = r.GetString(r.GetOrdinal("ReferralCode")),
        ReferralUrl = r.GetString(r.GetOrdinal("ReferralUrl")),
        ApplicationStatus = r.GetString(r.GetOrdinal("ApplicationStatus")),
        TotalClicks = r.GetInt64(r.GetOrdinal("TotalClicks")),
        TotalLeads = r.GetInt32(r.GetOrdinal("TotalLeads")),
        TotalSales = r.GetInt32(r.GetOrdinal("TotalSales")),
        TotalRevenue = r.GetDecimal(r.GetOrdinal("TotalRevenue")),
        TotalCommission = r.GetDecimal(r.GetOrdinal("TotalCommission")),
        TotalPaid = r.GetDecimal(r.GetOrdinal("TotalPaid")),
        PendingBalance = r.GetDecimal(r.GetOrdinal("PendingBalance")),
        FraudScore = r.GetInt32(r.GetOrdinal("FraudScore")),
        IsFlagged = r.GetBoolean(r.GetOrdinal("IsFlagged")),
        IsActive = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
    };
}
