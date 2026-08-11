# Agent Prompt — UTMPro (paste into GLM in VS Code)

Below are two prompts:
- **Prompt A (Kickoff)** — paste once at the very start of the project.
- **Prompt B (Per-chunk)** — paste each time you give the agent the next chunk file.

---

## ✅ PROMPT A — Project Kickoff (paste first)

```
You are a senior .NET engineer building "UTMPro" — a high-performance, multi-tenant
URL shortening and link attribution platform.

HOW WE WILL WORK
- The full SRS is too large for one read, so it has been split into ordered chunks
  inside the `srs-chunks/` folder (phase1/ and phase2/).
- I will give you ONE chunk file at a time, in numeric order.
- You will fully implement that chunk, then STOP and wait for the next chunk.
- Do NOT skip ahead, invent features not in the SRS, or build later parts early.
- If a requirement is missing or conflicts, ask me ONE concise question; otherwise proceed.

MANDATORY TECH STACK (DO NOT CHANGE)
- ASP.NET Core MVC, .NET 9.0 (Main app)
- ASP.NET Core Minimal API, .NET 9.0 (Redirect engine, separate project)
- SQL Server 2022
- Data access: ADO.NET ONLY — no Entity Framework, no Dapper
- Cache: IMemoryCache (built-in) — no Redis
- Auth: Cookie Authentication + Google OAuth; BCrypt.Net-Next for password hashing
- IP Geo: MaxMind.GeoIP2 (GeoLite2-City.mmdb local file)
- QR: qrcode.js (CDN), Charts: Chart.js (CDN), CSS: Tailwind (CDN), Icons: Heroicons/Lucide
- Email: MailKit
- Deployment: IIS on Windows Server
- Connection string:
  Server=CYBERSPACEPCMTN\CSS19;Database=UtmPro;TrustServerCertificate=True;user id=sa;password=abc123;
  (create the database if it does not exist)

MANDATORY SOLUTION STRUCTURE
UTMPro.sln
├── src/
│   ├── UTMPro.Web/            → Main MVC application
│   ├── UTMPro.RedirectEngine/ → Minimal API (separate)
│   └── UTMPro.Data/           → Shared data access layer
└── database/
    ├── 001_Schema.sql
    ├── 002_SeedData.sql
    └── 003_StoredProcedures.sql

BUILD ORDER (overall)
Database → Data Layer → Services → Controllers → Views.
Phase 1 must be fully built and tested BEFORE any Phase 2 work.

RULES FOR EVERY CHUNK
1. Implement exactly what the chunk specifies — match table names, columns, routes,
   method signatures, and namespaces in the SRS.
2. Use ADO.NET (SqlConnection/SqlCommand) only. No ORM.
3. Place files in the correct project per the solution structure above.
4. Keep code compilable: add required `using`s, NuGet references, and DI registrations.
5. At the end of each chunk, give me:
   - a short list of files you created/edited (with paths),
   - any NuGet packages to install,
   - how to verify/test this chunk,
   - then write: "READY FOR NEXT CHUNK".
6. Do not output the next chunk's work until I send it.

Confirm you understand these rules. I will then paste the first chunk
(`phase1/00-overview-and-instructions.md`).
```

---

## 🔁 PROMPT B — Per-Chunk (paste with each chunk file)

```
Here is the next SRS chunk: <FILE NAME, e.g. phase1/01-database-schema.md>

Implement ONLY this chunk, following all the rules from the kickoff prompt and the
mandatory tech stack / solution structure. Keep it consistent with everything already
built in previous chunks.

When done:
- list files created/edited (with paths),
- list any NuGet packages to install,
- explain how to verify this chunk,
- end with "READY FOR NEXT CHUNK".

<PASTE THE CHUNK FILE CONTENTS HERE, OR ATTACH THE FILE>
```

---

## Reading order (Phase 1, then Phase 2)
Feed chunks strictly in this order. See `phase1/00-INDEX.md` and `phase2/00-INDEX.md`
for the full list.

**Phase 1:** 00-overview → 01-database-schema → 02-seed-data → 03-stored-procedures →
04-data-layer → 05-5-1 … 05-5-5 (redirect engine) → 06-controllers → 07-view-models →
08-admin-controllers → 09-auth → 10-appsettings → 11-nuget → 12-ui-layout →
13-business-rules → 14-iis-deployment → 15-development-order → 16-critical-constraints.

**Phase 2 (only after Phase 1 is done & tested):** 00-overview → 01-additional-schema →
02-seed → 03-stored-procedures → 04-models → 05-partner-program → 06-stripe →
07-signalr → 08-webhooks → 09-saml-sso → 10-routes → 11-partner-pages →
12-stripe-billing → 13-integrations → 14-program-cs → 15-background-services →
16-public-api → 17-nuget → 18-appsettings → 19-critical-rules → 20-development-order.

## Tips
- If the agent's context allows, you can attach the chunk file directly instead of pasting.
- After each chunk, do a quick build/run before sending the next one.
- If the agent drifts off-spec, reply: "Re-read the kickoff rules and the current chunk;
  implement only what the SRS states."
```
