using Microsoft.AspNetCore.Mvc;
using UTMPro.Data.Helpers;
using UTMPro.Data.Models;
using UTMPro.Data.Repositories;

namespace UTMPro.Web.Controllers;

[Route("scim/{workspaceSlug}/v2")]
[ApiController]
public class SCIMController : ControllerBase
{
    private readonly IWorkspaceRepository _wsRepo;
    private readonly IUserRepository _userRepo;

    public SCIMController(IWorkspaceRepository wsRepo, IUserRepository userRepo)
    {
        _wsRepo = wsRepo; _userRepo = userRepo;
    }

    [HttpGet("Users")]
    public async Task<IActionResult> ListUsers(string workspaceSlug)
    {
        var ws = await _wsRepo.GetBySlugAsync(workspaceSlug);
        if (ws == null) return NotFound();
        var members = await _wsRepo.GetMembersAsync(ws.Id);
        var resources = members.Select(m => new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
            id = m.UserId.ToString(), userName = m.UserEmail, displayName = m.UserName,
            active = m.IsActive, meta = new { resourceType = "User" }
        });
        return Ok(new { schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" }, totalResults = members.Count, Resources = resources });
    }

    [HttpGet("Users/{id}")]
    public async Task<IActionResult> GetUser(string workspaceSlug, long id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return NotFound(new { schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:Error" }, detail = "User not found", status = 404 });
        return Ok(new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" }, id = user.ExternalId, userName = user.Email, displayName = user.Name, active = user.IsActive });
    }

    [HttpPost("Users")]
    public async Task<IActionResult> CreateUser(string workspaceSlug, [FromBody] SCIMUserRequest request)
    {
        var ws = await _wsRepo.GetBySlugAsync(workspaceSlug);
        if (ws == null) return NotFound();

        var existing = await _userRepo.GetByEmailAsync(request.UserName);
        if (existing != null)
        {
            // Add to workspace if not already member
            var member = await _wsRepo.GetMemberAsync(ws.Id, existing.Id);
            if (member == null) await _wsRepo.AddMemberAsync(ws.Id, existing.Id, "Member", null);
            return Ok(new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" }, id = existing.ExternalId, userName = existing.Email, displayName = existing.Name, active = existing.IsActive });
        }

        var user = new User
        {
            ExternalId = IdGenerator.NewExternalId("user_"), Name = request.DisplayName ?? request.UserName,
            Email = request.UserName.ToLower(), IsActive = true, EmailVerified = true
        };
        user.Id = await _userRepo.CreateAsync(user);
        await _wsRepo.AddMemberAsync(ws.Id, user.Id, "Member", null);

        return Created($"/scim/{workspaceSlug}/v2/Users/{user.ExternalId}",
            new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" }, id = user.ExternalId, userName = user.Email, displayName = user.Name, active = true });
    }

    [HttpPut("Users/{id}")]
    public async Task<IActionResult> UpdateUser(string workspaceSlug, string id, [FromBody] SCIMUserRequest request)
    {
        var user = await _userRepo.GetByExternalIdAsync(id);
        if (user == null) return NotFound();
        user.Name = request.DisplayName ?? user.Name;
        user.IsActive = request.Active;
        await _userRepo.UpdateAsync(user);
        return Ok(new { schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:User" }, id = user.ExternalId, userName = user.Email, displayName = user.Name, active = user.IsActive });
    }

    [HttpDelete("Users/{id}")]
    public async Task<IActionResult> DeleteUser(string workspaceSlug, string id)
    {
        var user = await _userRepo.GetByExternalIdAsync(id);
        if (user == null) return NotFound();
        // Deprovision = disable, not delete
        var ws = await _wsRepo.GetBySlugAsync(workspaceSlug);
        if (ws != null) await _wsRepo.RemoveMemberAsync(ws.Id, user.Id);
        return NoContent();
    }

    [HttpGet("ServiceProviderConfig")]
    public IActionResult GetConfig(string workspaceSlug)
    {
        return Ok(new
        {
            schemas = new[] { "urn:ietf:params:scim:schemas:core:2.0:ServiceProviderConfig" },
            patch = new { supported = false }, bulk = new { supported = false },
            filter = new { supported = false }, changePassword = new { supported = false },
            sort = new { supported = false }, etag = new { supported = false },
            authenticationSchemes = new[] { new { type = "httpbasic", name = "HTTP Basic" } }
        });
    }

    [HttpGet("Groups")]
    public IActionResult ListGroups(string workspaceSlug)
    {
        return Ok(new { schemas = new[] { "urn:ietf:params:scim:api:messages:2.0:ListResponse" }, totalResults = 0, Resources = Array.Empty<object>() });
    }
}

public class SCIMUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public bool Active { get; set; } = true;
}
