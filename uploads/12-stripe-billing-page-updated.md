# PART 12: STRIPE BILLING PAGE (UPDATED)

```
URL: /{workspaceSlug}/settings/billing

┌─────────────────────────────────────────────────────────┐
│  Billing                                                │
├─────────────────────────────────────────────────────────┤
│  CURRENT PLAN (if on paid plan):                        │
│  ┌───────────────────────────────────────────────────┐  │
│  │ Business Plan  $90/month                          │  │
│  │ Jun 1, 2026 - Jul 1, 2026                        │  │
│  │ [Manage Subscription] [View Invoices]             │  │
│  │ Next billing: $90 on Jul 1, 2026                 │  │
│  │ [Cancel Plan]                                     │  │
│  └───────────────────────────────────────────────────┘  │
│                                                         │
│  CURRENT PLAN (if FREE):                                │
│  Free Plan                                              │
│  [Upgrade Plan]                                         │
│                                                         │
│  USAGE CARDS (same as Phase 1)                          │
│                                                         │
│  INVOICES TABLE:                                        │
│  Date | Invoice # | Amount | Status | [PDF]            │
│  Jun 1, 2026 | INV-001 | $90 | Paid ✅ | [Download]   │
└─────────────────────────────────────────────────────────┘

UPGRADE PAGE: /{workspaceSlug}/settings/billing/upgrade

Toggle: [Monthly] [Yearly -20%]

Plans:
┌────────────┬────────────────┬─────────────────────┐
│ Pro        │ Business POPULAR│ Advanced            │
│ $30/mo     │ $90/mo         │ $300/mo             │
│ ($24/mo/yr)│ ($72/mo/yr)    │ ($240/mo/yr)        │
├────────────┼────────────────┼─────────────────────┤
│[Upgrade]   │[Upgrade]       │[Upgrade]            │
└────────────┴────────────────┴─────────────────────┘
→ Clicking Upgrade → Stripe Checkout page
→ On success → return to billing page with success msg
```

---
