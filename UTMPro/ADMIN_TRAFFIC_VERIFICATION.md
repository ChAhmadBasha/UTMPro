# Admin traffic rules: deployment and verification

## What was broken

The admin UI writes rules to `AdminTrafficRules` and destinations to
`AdminTrafficUrls`. The redirect cache originally loaded only the percentage
columns on `Links`/`Workspaces` and admin rows in `LinkDestinations`; it did not
load the admin traffic rule configured by a SuperAdmin.

The first attempted fix in `019_Fix_OG_SP.sql` was incomplete:

- SQL Server could reject it because `CREATE OR ALTER PROCEDURE` was not the
  first statement in a batch (`GO` was missing after `USE`).
- It combined URLs from every matching active rule but used the percentage from
  only the first row.
- Links with custom social/OG previews and links matched by targeting rules
  bypassed admin traffic injection.
- The OG HTML response was cacheable, which could pin many visitors to the
  first randomly selected destination.
- Rule changes did not invalidate already-cached links.
- Workspace-scoped rules could not be configured correctly in the admin UI.

Migration `database/021_Fix_Admin_Traffic_Rules.sql` and the matching redirect
engine/web changes fix these paths.

## Deploy

1. Back up the database.
2. Run `database/021_Fix_Admin_Traffic_Rules.sql` against `UTMProDB`.
3. Run `database/022_Admin_Traffic_Daily_Report.sql` against `UTMProDB`.
4. Run `database/023_Admin_Traffic_Hide_From_User_Stats.sql` against `UTMProDB`
   so admin-traffic redirects no longer count toward a link's user statistics.
5. Deploy **UTMPro.RedirectEngine**.
5. Deploy **UTMPro.Web** (this supplies workspace selection, validation,
   reporting, test redirects, and cache invalidation after rule changes).
6. Configure a strong `DiagnosticsApiKey` for the redirect engine using a
   deployment secret or environment variable; do not commit the value.
7. Configure the same strong `InternalApiKey` secret in both applications. It
   protects full-cache invalidation after a rule changes. If it is omitted,
   cached links still refresh through the one-minute TTL.
8. Restart both applications.

Run the database migration before deploying the redirect engine when possible.
The cache reader has a compatibility fallback for migration 019, but rule IDs,
scope diagnostics, and per-URL click counters require migration 021. Exact
per-rule daily attribution and the admin traffic report require migration 022.

## Verify configuration (no random sampling required)

Request the diagnostics route on the **same host as the short link**:

```bash
curl -sS \
  -H 'X-Diagnostics-Key: YOUR_DIAGNOSTICS_KEY' \
  'https://go.utmpro.link/debug/traffic/YOUR_SLUG'
```

For a working 20% global rule, check these fields:

```json
{
  "selectedRule": {
    "scope": "global",
    "percent": 20.00,
    "urls": [
      { "url": "https://target.example/page", "weight": 100 }
    ]
  },
  "effective": {
    "source": "global-rule",
    "percent": 20.00,
    "urlCount": 1,
    "ready": true,
    "issue": null
  }
}
```

`/debug/og/{slug}` remains as a compatibility alias. Both diagnostics routes
return 404 unless `DiagnosticsApiKey` is configured and the
`X-Diagnostics-Key` header matches.

Important precedence:

1. `Links.AdminTrafficEnabled = 0` explicitly disables injection for that link.
2. An enabled per-link percentage wins.
3. An enabled workspace percentage wins.
4. The most recently updated active workspace rule wins.
5. Otherwise, the most recently updated active global rule wins.

A workspace rule overrides a global rule. Multiple matching rules are never
mixed.

## Admin traffic does not pollute the original link's stats

Since migration `023`, clicks that are redirected to an admin link (via
`AdminTrafficRules`) are **not** shown in the statistics of the original short
link for ordinary users:

- They no longer increment `Links.TotalClicks`.
- The analytics summary, time series, geo/device/browser/OS/referrer charts, and
  the events list all filter out `IsAdminRedirect = 1` unless the viewer is a
  platform **SuperAdmin** (`IncludeAdmin = 1`).

Admin clicks are still recorded in `ClickEvents` (marked `IsAdminRedirect = 1`),
still increment the exact `AdminTrafficUrls.ClickCount`, and remain visible to
SuperAdmins both in the original link analytics and in the dedicated Admin
Traffic report. Public stats and the public API always hide admin traffic.

## Daily report and forced test

SuperAdmins can open `/admin/traffic-rules/report` for 7, 30, 90, or 365-day
analysis. The report includes daily total/admin/normal clicks, observed admin
percentage, rule totals, destination totals, unique admin visitors, and last
redirect timestamps. Dates are grouped in UTC.

Use the **Test** button beside a rule or URL to always open a configured admin
destination. This bypasses the percentage roll intentionally and does not add a
click event, so testing cannot distort the report. The test endpoint remains
protected by the SuperAdmin login.

## Verify observed traffic in SQL

Use a reasonable sample window and the click events recorded by the redirect
engine:

```sql
DECLARE @Domain NVARCHAR(255) = N'go.utmpro.link';
DECLARE @Slug NVARCHAR(255) = N'YOUR_SLUG';
DECLARE @Since DATETIME2 = DATEADD(HOUR, -24, GETUTCDATE());

SELECT
    COUNT_BIG(*) AS RecordedClicks,
    SUM(CASE WHEN ce.IsAdminRedirect = 1 THEN 1 ELSE 0 END) AS AdminRedirects,
    CAST(
        100.0 * SUM(CASE WHEN ce.IsAdminRedirect = 1 THEN 1 ELSE 0 END)
        / NULLIF(COUNT_BIG(*), 0)
        AS DECIMAL(6,2)
    ) AS ObservedAdminPercent
FROM ClickEvents ce
INNER JOIN Links l ON ce.LinkId = l.Id
INNER JOIN Domains d ON l.DomainId = d.Id
WHERE d.Domain = @Domain
  AND l.Slug = @Slug
  AND ce.ClickedAt >= @Since;
```

Migration 021 also updates each selected `AdminTrafficUrls.ClickCount`. Inspect
those counters with:

```sql
SELECT
    atr.Id AS RuleId,
    atr.RuleName,
    atr.TrafficPercent,
    atr.IsGlobal,
    atr.WorkspaceId,
    atu.Id AS UrlId,
    atu.Url,
    atu.Weight,
    atu.ClickCount,
    atr.IsActive AS RuleIsActive,
    atu.IsActive AS UrlIsActive
FROM AdminTrafficRules atr
INNER JOIN AdminTrafficUrls atu ON atu.RuleId = atr.Id
WHERE atr.Id = YOUR_RULE_ID;
```

## Why ten clicks are not a reliable test

For a true 20% rule, the chance of observing **zero** admin redirects in only ten
independent requests is about 10.7% (`0.8^10`). Ten requests can therefore make
a working rule look broken. Use at least 100 recorded human requests for a
basic smoke test, and expect random variation around 20% rather than exactly 20.

Do not use social crawler requests as the sample: crawler preview fetches are
not recorded as human clicks. Also ensure the client or CDN is not serving an
old cached response; fixed redirect and OG responses now send `no-store`.
