# PART 19: CRITICAL RULES FOR PHASE 2

```
ABSOLUTE RULES PHASE 2 - NEVER VIOLATE:

1. STRIPE WEBHOOKS:
   Always check idempotency (StripeWebhookEvents table)
   Never process same event twice
   Always return 200 to Stripe immediately, process async
   Raw request body required for signature verification
   Never buffer/read body before Stripe middleware

2. PARTNER ATTRIBUTION:
   Cookie window MUST be respected
   Attribution = last-click wins within cookie window
   Self-referral MUST be detected and blocked
   Fraud detection MUST run before approving sales
   Commission calculation MUST match program settings exactly

3. PARTNER PAYOUTS:
   Never auto-pay without fraud check
   Minimum balance check before payout
   Always create PartnerSale records BEFORE paying
   Stripe Connect transfers: use workspace's connected account
   Failed payouts MUST notify admin AND partner

4. REAL-TIME EVENTS (SignalR):
   Only broadcast to workspace's group
   Verify membership before joining group
   Never broadcast sensitive data (passwords, API keys)
   Graceful degradation: app works without SignalR

5. SAML SSO:
   SP metadata must be publicly accessible
   Certificate validation is mandatory
   Auto-provision = create user if doesn't exist
   RequireSAML = block non-SAML logins for workspace
   Always validate email attribute presence

6. SCIM:
   Use Bearer token authentication (SCIMToken)
   Hash token (BCrypt) stored in SCIMTokenHash
   Deprovision = disable user, NOT delete
   SCIM User.id = our User.ExternalId
   Always return proper SCIM error responses

7. WEBHOOK DELIVERY:
   Max 3 retries (configurable)
   Exponential-like backoff (interval * attempt number)
   Include signature header always if secret is set
   Log ALL deliveries (success and failure)
   Timeout: 10 seconds per request

8. API RATE LIMITING:
   Free plan: 60 req/min
   Pro: 300 req/min
   Business: 1000 req/min
   Advanced: 5000 req/min
   Return 429 with Retry-After header when exceeded

9. STRIPE SUBSCRIPTION STATES:
   'active'      → Normal operation
   'trialing'    → Trial active, show trial badge
   'past_due'    → Payment failed, show warning
   'canceled'    → Downgrade to Free plan
   'incomplete'  → New sub, payment pending

10. FRAUD SCORING:
    SelfReferral detected:   +50 points
    DuplicateIP (>10):       +30 points
    ChargeBack received:     +40 points
    VPN detected:            +20 points
    Score >= 80: auto-flag   
    Score >= 100: auto-suspend
    Score resets monthly if resolved

11. DATA ISOLATION:
    Partners can ONLY see their own data
    Partner portal sessions = separate from main app sessions
    NEVER expose other partners' data
    WorkspaceId check on EVERY query involving partner data

12. COMMISSION PRECISION:
    Always use DECIMAL(10,2) for money
    Round to 2 decimal places using Math.Round(x, 2)
    Currency conversion: NOT in scope for Phase 2
    All amounts stored in original currency
```

---
