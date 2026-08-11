# PART 11: PARTNER PROGRAM PAGES (UI SPEC)

## 11.1 Program Overview Page

```
URL: /{workspaceSlug}/program

If no program exists:
┌─────────────────────────────────────────────────────────┐
│  Partner Program                                        │
│                                                         │
│  [Partner avatars showing: Lauren $1.8K, Elias $783..] │
│                                                         │
│  Dub Partners                                           │
│  Kickstart viral product-led growth with powerful,      │
│  branded referral and affiliate programs.               │
│                                                         │
│  [Setup Partner Program]                               │
└─────────────────────────────────────────────────────────┘

If program exists:
┌─────────────────────────────────────────────────────────┐
│  Partner Program                              [Settings]│
├─────────────────────────────────────────────────────────┤
│  SUMMARY CARDS                                          │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐  │
│  │ Partners │ │ Revenue  │ │Commission│ │ Payouts  │  │
│  │  124     │ │ $18.5K   │ │  $3.7K  │ │  $2.1K  │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘  │
├─────────────────────────────────────────────────────────┤
│  Revenue Chart (Chart.js line chart)                    │
├─────────────────────────────────────────────────────────┤
│  TOP PARTNERS                                           │
│  Avatar | Name | Country | Revenue | Commission | Paid │
│  Lauren Anderson | 🇺🇸 | $1.8K | $550 | $500           │
│  Elias Weber     | 🇩🇪 | $783  | $235 | $200           │
├─────────────────────────────────────────────────────────┤
│  PENDING ITEMS                                          │
│  • 3 applications pending approval                      │
│  • 2 payouts ready to process                           │
│  • 1 unresolved fraud event                            │
└─────────────────────────────────────────────────────────┘
```

## 11.2 Program Setup Page

```
URL: /{workspaceSlug}/program/setup

STEP 1: Basic Info
├── Program Name (required)
├── Description
├── Logo upload
├── Brand Color picker

STEP 2: Commission Structure  
├── Commission Type: [Percentage %] [Flat Rate $]
├── Commission Value: [20] %
├── Duration:
│   ├── ○ One-time (first sale only)
│   ├── ● Lifetime (all future sales)
│   └── ○ Recurring (specify months)
└── Cookie window: [90] days

STEP 3: Payouts
├── Payout threshold: $[50] minimum
├── Frequency: [Monthly ▾]
├── Method: [Stripe ▾] [PayPal] [Manual]

STEP 4: Application
├── Require application? [toggle]
├── Auto-approve? [toggle]
├── Application questions (if required):
│   ├── [+ Add question]
│   └── Questions list (drag to reorder)
└── Terms & Conditions URL

STEP 5: Review & Launch
└── Summary + [Launch Program]
```

## 11.3 Partners List Page

```
URL: /{workspaceSlug}/program/partners

Tabs: [All] [Pending] [Approved] [Suspended]

Table Columns:
Partner | Country | Revenue | Commission | Paid | 
Balance | Clicks | Joined | Status | Actions

Actions per row:
├── View detail
├── Approve/Reject (if pending)
├── Send message
├── Suspend
└── Export their data
```

## 11.4 Public Partner Portal

```
URL: partners.utmpro.co/{programSlug}

LANDING PAGE:
┌─────────────────────────────────────────────────────────┐
│  [Company Logo]                                         │
│  Join {ProgramName} Partner Program                     │
│  Earn {commission}% on every sale you refer             │
│                                                         │
│  ● {cookieDays}-day cookie window                       │
│  ● {payoutFrequency} payouts                            │
│  ● Minimum: ${payoutThreshold}                          │
│                                                         │
│  [Apply Now]  [Login if existing partner]               │
│                                                         │
│  Terms: {termsUrl}                                      │
└─────────────────────────────────────────────────────────┘

PARTNER DASHBOARD (after login):
├── Stats: Clicks | Leads | Sales | Revenue | Balance
├── Referral link + copy button
├── QR code for referral link
├── Recent sales table
├── Payout history
└── Profile settings
```

---
