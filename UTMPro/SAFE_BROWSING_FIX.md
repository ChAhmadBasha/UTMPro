# Fix: Google Safe Browsing Flagged Your Domain

## What Happened

Google Safe Browsing flagged `pagesync.utmpro.link` as dangerous because:

1. **Microsoft OAuth redirect** — Your domain redirects to `login.microsoftonline.com` which is a pattern commonly used in phishing attacks (fake Microsoft login pages)
2. **URL shortener behavior** — New domains that perform 302 redirects are automatically suspicious to Google
3. **No established reputation** — New subdomains don't have trust history with Google

## Immediate Fix (Do These NOW)

### Step 1: Request Review from Google (5 minutes)

1. Go to: **https://safebrowsing.google.com/safebrowsing/report_error/**
2. Enter: `https://pagesync.utmpro.link`
3. In the comments, write:
   ```
   This is a legitimate SaaS subdomain used for Microsoft OneDrive/SharePoint 
   integration via OAuth 2.0. The redirect to login.microsoftonline.com is a 
   standard Microsoft OAuth authorization flow, not phishing. 
   The domain is owned by UTMPro (utmpro.link), a legitimate URL management platform.
   ```
4. Submit the report

Google typically reviews within **24-72 hours**.

### Step 2: Register in Google Search Console (10 minutes)

1. Go to: **https://search.google.com/search-console/**
2. Add property: `https://pagesync.utmpro.link`
3. Verify ownership via DNS TXT record
4. Go to **Security & Manual Actions → Security Issues**
5. If issues are listed, click **Request Review**

### Step 3: Register in Google Safe Browsing (5 minutes)

1. Go to: **https://transparencyreport.google.com/safe-browsing/search?url=pagesync.utmpro.link**
2. Check the status
3. If flagged, there will be a "Report incorrect warning" link

### Step 4: Check VirusTotal (2 minutes)

1. Go to: **https://www.virustotal.com/gui/url-search**
2. Enter: `https://pagesync.utmpro.link`
3. If flagged by multiple vendors, you have a bigger problem
4. If only Google flags it, the review request should fix it

## Prevent This From Happening Again

### A. Add Security Headers to ALL Your Sites

Add these to `web.config` in your IIS sites:

```xml
<system.webServer>
  <httpProtocol>
    <customHeaders>
      <add name="X-Content-Type-Options" value="nosniff" />
      <add name="X-Frame-Options" value="SAMEORIGIN" />
      <add name="X-XSS-Protection" value="1; mode=block" />
      <add name="Referrer-Policy" value="strict-origin-when-cross-origin" />
      <add name="Permissions-Policy" value="camera=(), microphone=(), geolocation=()" />
    </customHeaders>
  </httpProtocol>
</system.webServer>
```

### B. Don't Use Short Link Domains for OAuth Redirects

**This is the real problem.** Your OAuth redirect URL is:
```
https://pagesync.utmpro.link/signin-microsoft
```

This makes Google think `pagesync.utmpro.link` is a phishing site because:
- It redirects to Microsoft login
- It's a subdomain of a URL shortener
- It looks like a credential harvesting site

**Solution:** Use a DIFFERENT subdomain for your OAuth app:
```
https://auth.utmpro.link/signin-microsoft
    or
https://app.utmpro.link/signin-microsoft
```

The main `app.utmpro.link` domain has established reputation (it's your main site). Using it for OAuth won't trigger Safe Browsing.

### C. Add a robots.txt and sitemap

Create at `https://pagesync.utmpro.link/robots.txt`:
```
User-agent: *
Allow: /
Sitemap: https://pagesync.utmpro.link/sitemap.xml
```

Create at `https://pagesync.utmpro.link/sitemap.xml`:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>https://pagesync.utmpro.link/</loc>
    <changefreq>weekly</changefreq>
  </url>
</urlset>
```

### D. Add a Landing Page

Instead of making the root URL redirect, add a proper landing page at `https://pagesync.utmpro.link/` that explains what PageSync is. This establishes legitimacy.

### E. Monitor Link Abuse

In your Admin Panel, regularly check:
- Links redirecting to `login.microsoftonline.com`, `accounts.google.com`, or any login page
- Links with suspicious patterns (crypto, adult, phishing)
- High-volume links with low engagement (spam indicators)

## Timeline

| Action | Time to Fix |
|--------|-------------|
| Submit Google review | 24-72 hours |
| Move OAuth to app.utmpro.link | Immediate (config change) |
| Add security headers | Immediate |
| Chrome clears warning | After Google approves review |

## If Review is Rejected

If Google rejects your review:
1. Check if ANY link on your platform redirects to phishing sites
2. Remove/block those links
3. Resubmit the review with evidence of cleanup
4. Consider moving the OAuth flow to your main domain
