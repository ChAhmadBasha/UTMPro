# PART 18: PHASE 2 APPSETTINGS ADDITIONS

```json
// Add to UTMPro.Web/appsettings.json
{
  "Stripe": {
    "PublishableKey": "pk_live_xxx",
    "SecretKey":      "sk_live_xxx",
    "WebhookSecret":  "whsec_xxx",
    "ConnectClientId": "ca_xxx",
    "TrialDays": 0
  },
  "Partners": {
    "PortalUrl": "https://partners.utmpro.co",
    "EnableFraudDetection": true,
    "SelfReferralDetection": true,
    "DuplicateIPWindowHours": 24,
    "MaxDuplicateIPClicks": 10,
    "FraudAutoFlagThreshold": 80
  },
  "SignalR": {
    "EnableDetailedErrors": false
  },
  "SAML": {
    "Enabled": true,
    "SpBaseUrl": "https://app.utmpro.co"
  }
}
```

---
