# SSL Setup for Custom Domains — UTMPro

## The Problem

When you add a custom domain (e.g., `link.client2.com`) in UTMPro admin, the **DNS points to your server** and **UTMPro's redirect engine handles the routing**. But the browser requires a **valid SSL certificate** for that domain. Without it → "Your connection is not private" error.

**IIS does NOT automatically issue certificates for new domains.** You must add an HTTPS binding + certificate for each custom domain.

---

## Solution: win-acme (Let's Encrypt for IIS)

### Step 1: Install win-acme (if not already)

Download from: https://www.win-acme.com/

```
1. Download the latest release (zip)
2. Extract to C:\win-acme\
3. Run wacs.exe as Administrator
```

### Step 2: Issue Certificate for the New Domain

**Option A: Interactive (manual)**
```
1. Run C:\win-acme\wacs.exe as Administrator
2. Choose: N - Create certificate (default settings)
3. Choose: 2 - Manual input
4. Enter: link.client2.com
5. Choose: 1 - Single binding of IIS site
6. Select your RedirectEngine IIS site
7. Let it validate and issue the certificate
```

**Option B: Command line (automated)**
```powershell
cd C:\win-acme
.\wacs.exe --target manual --host link.client2.com --installation iis --siteid YOUR_SITE_ID --store certificatestore
```

Replace `YOUR_SITE_ID` with the IIS site ID of your Redirect Engine site.

### Step 3: Verify IIS Binding

After win-acme issues the certificate:

```
1. Open IIS Manager
2. Go to your RedirectEngine site
3. Click "Bindings..." in the right panel
4. You should see:
   - https  go.utmpro.link:443  (✅ existing)
   - https  link.client1.com:443  (✅ existing - works)
   - https  link.client2.com:443  (✅ NEW - just added)
```

If the binding is missing, add it manually:
```
1. Click "Add..."
2. Type: https
3. Host name: link.client2.com
4. SSL certificate: Select the Let's Encrypt cert for this domain
5. Click OK
```

### Step 4: Test

Open `https://link.client2.com/` — should redirect to utmpro.link (root redirect). No SSL error.

---

## Automating for Future Domains

Every time a new custom domain is verified in UTMPro, you need to:

1. **Issue a Let's Encrypt certificate** via win-acme
2. **Add an IIS HTTPS binding** for that domain

### Option 1: PowerShell Script (Recommended)

Create `C:\scripts\add-domain-ssl.ps1`:

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$Domain,
    
    [string]$SiteName = "RedirectEngine",
    [string]$WacsPath = "C:\win-acme\wacs.exe"
)

Write-Host "Issuing SSL certificate for $Domain..."

# Issue certificate via win-acme
& $WacsPath --target manual --host $Domain --installation iis --siteid (Get-Website $SiteName).ID --store certificatestore --accepttos --emailaddress admin@utmpro.link

Write-Host "Done! Certificate issued and IIS binding added for $Domain"
```

Usage:
```powershell
.\add-domain-ssl.ps1 -Domain "link.client2.com"
.\add-domain-ssl.ps1 -Domain "go.newclient.com"
```

### Option 2: Wildcard Certificate (if using subdomains)

If all custom domains are subdomains of one domain (e.g., `*.utmpro.link`), you can use a single wildcard cert:

```powershell
wacs.exe --target manual --host *.utmpro.link --validationmode dns-01 --installation iis
```

But this **doesn't work for completely different domains** (e.g., `link.client.com`).

### Option 3: Cloudflare Proxy (Easiest for Multiple Domains)

If you put Cloudflare in front of your server:

1. Add each custom domain to Cloudflare
2. Set DNS A record → your server IP
3. Enable Cloudflare proxy (orange cloud)
4. Cloudflare handles SSL automatically — no certificate needed on your server
5. Set SSL mode to "Full" in Cloudflare

---

## Quick Reference

| Domain | DNS | SSL Cert | IIS Binding | Status |
|--------|-----|----------|-------------|--------|
| go.utmpro.link | ✅ A → server | ✅ Let's Encrypt | ✅ https:443 | Working |
| link.client1.com | ✅ A → server | ✅ Let's Encrypt | ✅ https:443 | Working |
| link.client2.com | ✅ A → server | ❌ Missing | ❌ Missing | **SSL Error** |

**Fix:** Run `wacs.exe` to issue cert for `link.client2.com`, IIS binding will be added automatically.

---

## Troubleshooting

### "The certificate is not valid for this domain"
- win-acme issued a cert but for a different domain. Re-run with the correct hostname.

### "Let's Encrypt validation failed"
- DNS not propagated yet. Wait 15-30 minutes and retry.
- Port 80 is blocked by firewall. Let's Encrypt needs port 80 for HTTP-01 validation.
- Make sure `http://link.client2.com/.well-known/acme-challenge/` is reachable.

### "IIS binding exists but wrong certificate"
- Edit the binding → change the SSL certificate dropdown to the correct one.

### "Certificate expired"
- win-acme auto-renews via Task Scheduler. Check `Task Scheduler → win-acme` task is enabled.
- Manual renewal: `wacs.exe --renew --force`
