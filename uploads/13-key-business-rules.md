# PART 13: KEY BUSINESS RULES

```
WEIGHTED REDIRECT RULES (ADDON 1 + 2):

1. PRIORITY ORDER for admin traffic %:
   Link.AdminTrafficEnabled = false → 0% (disabled for this link)
   Link.AdminTrafficEnabled = true  → Link.AdminTrafficPercent
   Link.AdminTrafficEnabled = null  → Workspace.AdminTrafficPercent
   Workspace.AdminTrafficEnabled = false → 0%
   Workspace.AdminTrafficEnabled = true  → Workspace.AdminTrafficPercent
   Global rule (if no workspace rule)

2. WEIGHT CALCULATION:
   Weights are RELATIVE not percentage
   [60, 30, 10] → total=100, each gets 60%, 30%, 10%
   [3, 2, 1]    → total=6, each gets 50%, 33%, 17%
   [100]        → 100% of traffic

3. REDIRECT MODE:
   'Single'   → One destination URL (ignore weights)
   'Weighted' → Multiple user URLs with weights
   'ABTest'   → Time-limited test mode

4. SLUG GENERATION:
   Default: 7 random characters [A-Za-z0-9]
   Characters: ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789
   Collision check: retry up to 5 times
   Custom slug: validated, URL-safe only

5. PLAN ENFORCEMENT:
   Free:     Max 25 links/month, 1K events, 30-day analytics
   Pro:      Max 1K links, 50K events, 365-day analytics
   Business: Max 10K links, 250K events, 3-year analytics
   Advanced: Max 50K links, 1M events, 5-year analytics

6. ROLE PERMISSIONS:
   Owner:  Full access including billing + delete workspace
   Admin:  All except billing + delete workspace
   Member: Create/edit links, view analytics, manage tags/folders
   Viewer: Read-only access to all data

7. DOMAIN ROUTING:
   System domains: go.utmpro.co (and others)
   Custom domains: Verified A record → server IP
   IIS must handle wildcard *.utmpro.co + all custom domains
   Host header matching in redirect engine

8. CACHE INVALIDATION:
   When link is updated → invalidate cache key
   When link is deleted → invalidate cache key
   When domain is changed → invalidate all links on that domain
   Cache key format: "link:{domain}:{slug}" (lowercase)

9. ANALYTICS RETENTION:
   Query StartDate is clamped to plan retention
   Free = 30 days, Pro = 1 year, Business = 3 years

10. ADMIN CLICK TRACKING:
    IsAdminRedirect = true on ClickEvents
    Admin clicks shown separately in analytics
    "Admin Redirects" section in workspace analytics
```

---
