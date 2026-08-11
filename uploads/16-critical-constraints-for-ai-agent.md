# PART 16: CRITICAL CONSTRAINTS FOR AI AGENT

```
ABSOLUTE RULES - NEVER VIOLATE:

1. NO Entity Framework. Use ADO.NET SqlCommand only.

2. NO Dapper. Raw ADO.NET only.

3. ALL SQL must use parameterized queries.
   NEVER: "WHERE Id = " + id
   ALWAYS: cmd.Parameters.AddWithValue("@Id", id)

4. The Redirect Engine (UTMPro.RedirectEngine) must:
   - Return HTTP 302 (not 301)
   - Never block on DB (use cache first)
   - Queue clicks async (never await in hot path)
   - Have NO MVC dependencies
   - Be a Minimal API only

5. Passwords must use BCrypt.Net-Next
   Hash: BCrypt.Net.BCrypt.HashPassword(password, 12)
   Verify: BCrypt.Net.BCrypt.Verify(password, hash)

6. User ExternalId format: "user_" + 20 random alphanumeric
   Workspace ExternalId format: "ws_" + 20 random alphanumeric
   Link ExternalId format: "lnk_" + 20 random alphanumeric

7. All DateTimes stored as UTC (DateTime.UtcNow)

8. QR codes generated CLIENT-SIDE using qrcode.js
   Never generate server-side QR codes

9. Analytics queries MUST respect plan retention days
   Clamp startDate to: NOW - AnalyticsRetentionDays

10. Admin traffic injection (ADDON 1) priority:
    Link override → Workspace setting → Global rule
    If any level disables it, respect that

11. Cache key format: "link:{domain}:{slug}" LOWERCASE
    Always call .ToLower() on domain and slug

12. Random slug characters only:
    ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789

13. CSRF tokens required on ALL POST/PUT/DELETE forms

14. Role checks in every controller action:
    Viewer cannot: Create, Edit, Delete, Admin actions
    Member cannot: Delete, Admin settings actions
    Admin cannot: Billing, Delete workspace
    Owner: Full access

15. All file uploads (avatars, logos):
    Max 2MB
    Allowed: .png, .jpg, .jpeg
    Store in: wwwroot/uploads/{type}/{filename}
    Serve from: /uploads/{type}/{filename}
```

---

# END OF SRS DOCUMENT
# UTMPro v1.0 - Phase 1
# Ready for AI Agent Development
# Total Tables: 28 | Total Modules: 20 | Total Routes: 60+
