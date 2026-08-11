# UTMPro — Complete Installation Guide (IIS on Windows Server)

> **Version:** 2.0 (Phase 1 + Phase 2)  
> **Stack:** ASP.NET Core 9.0 · SQL Server 2022 · IIS 10  
> **Last Updated:** June 2026

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [SQL Server Setup](#2-sql-server-setup)
3. [Build & Publish the Application](#3-build--publish-the-application)
4. [IIS Configuration](#4-iis-configuration)
5. [SSL Certificates](#5-ssl-certificates)
6. [DNS Configuration](#6-dns-configuration)
7. [Application Configuration](#7-application-configuration)
8. [GeoIP Database Setup](#8-geoip-database-setup)
9. [Google OAuth Setup](#9-google-oauth-setup)
10. [Stripe Setup](#10-stripe-setup)
11. [First Admin User](#11-first-admin-user)
12. [Verify Installation](#12-verify-installation)
13. [Troubleshooting](#13-troubleshooting)
14. [Maintenance & Updates](#14-maintenance--updates)

---

## 1. Prerequisites

### Required Software

| Software | Version | Download |
|----------|---------|----------|
| Windows Server | 2019 or 2022 | — |
| .NET 9.0 Hosting Bundle | 9.0.x | https://dotnet.microsoft.com/download/dotnet/9.0 |
| SQL Server | 2019 or 2022 | https://www.microsoft.com/sql-server |
| IIS | 10.0 | Windows Feature |
| SSMS (optional) | 19+ | https://aka.ms/ssmsfullsetup |

### Install .NET Hosting Bundle

```
1. Download "ASP.NET Core 9.0 Runtime - Windows Hosting Bundle" from:
   https://dotnet.microsoft.com/download/dotnet/9.0

2. Run the installer (dotnet-hosting-9.0.x-win.exe)

3. RESTART IIS after installation:
   Open CMD as Administrator:
   > net stop was /y
   > net start w3svc
```

### Enable IIS Features

```
Open Server Manager → Add Roles and Features:

✅ Web Server (IIS)
  ✅ Web Server
    ✅ Common HTTP Features
      ✅ Default Document
      ✅ Directory Browsing
      ✅ HTTP Errors
      ✅ Static Content
    ✅ Health and Diagnostics
      ✅ HTTP Logging
    ✅ Performance
      ✅ Static Content Compression
      ✅ Dynamic Content Compression
    ✅ Security
      ✅ Request Filtering
      ✅ URL Authorization
  ✅ Management Tools
    ✅ IIS Management Console
```

---

## 2. SQL Server Setup

### 2.1 Create Database

Open SQL Server Management Studio (SSMS) and connect to your SQL Server instance.

```sql
-- Option A: If using the connection string from appsettings
-- Server: CYBERSPACEPCMTN\CSS19  (change to your server)
-- Auth: sa / abc123  (change to your credentials)

-- Option B: If using Windows Authentication
-- Server: .  (localhost)
-- Auth: Integrated Security
```

### 2.2 Run SQL Scripts (IN ORDER)

Execute these scripts in SSMS **one at a time, in order**:

```
Step 1:  database/001_Schema.sql         → Creates 28 core tables
Step 2:  database/002_SeedData.sql       → Inserts plans, system domains, settings
Step 3:  database/003_StoredProcedures.sql → Creates 6 stored procedures
Step 4:  database/004_Phase2_Schema.sql  → Creates 20 Phase 2 tables + ALTER existing
Step 5:  database/005_Phase2_SeedData.sql → Stripe prices, integrations, Phase 2 settings
Step 6:  database/006_Phase2_StoredProcedures.sql → Phase 2 stored procedures
Step 7:  database/007_Phase3_Additions.sql → Blog tables, site config settings
Step 8:  database/008_Sprint1_Features.sql → Link comments, audit logs, bio profiles
Step 9:  database/009_Sprint2_Features.sql → Deep links, team activity, UTM templates
Step 10: database/010_Domain_Fixes.sql   → Domain visibility, system domain updates
Step 11: database/011_Pages_Docs.sql     → Pages and documentation additions
Step 12: database/012_Email_Verification.sql → Email verification additions
Step 13: database/013_OG_Cache_Fix.sql   → Social preview fields in redirect lookup
Step 14: database/014_QuickWins.sql      → Quick-win feature updates
Step 15: database/015_EmailTemplates.sql → Email template schema/data
Step 16: database/016_Discounts_AutoAdmin_TrialPlan.sql → Discounts/admin/trial updates
Step 17: database/017_Blog_Seed_Content.sql → Blog seed content
Step 18: database/018_Admin_Analytics.sql → Admin analytics procedures
Step 19: database/019_Fix_OG_SP.sql       → Redirect lookup fixes (superseded by 021)
Step 20: database/020_Auto_SSL.sql        → Automatic SSL support
Step 21: database/021_Fix_Admin_Traffic_Rules.sql → Admin traffic redirects and counters
Step 22: database/022_Admin_Traffic_Daily_Report.sql → Daily admin traffic attribution/reporting
```

### 2.3 Verify Database

```sql
USE UTMProDB;
GO

-- Should return 48+ tables
SELECT COUNT(*) AS TableCount FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Should return 4 plans
SELECT Id, Name, Price FROM Plans;

-- Should return 2 system domains
SELECT Id, Domain, IsSystemDomain, IsPrimary FROM Domains WHERE IsSystemDomain = 1;

-- Should return 20+ settings
SELECT COUNT(*) FROM SystemSettings;
```

### 2.4 Update System Settings

```sql
-- IMPORTANT: Update these to match your actual server
-- CustomDomainTarget is the hostname users point their CNAME records at
-- (e.g. links.utmpro.link). Point it at your redirect engine host.
UPDATE SystemSettings SET SettingValue = 'links.utmpro.link' WHERE SettingKey = 'CustomDomainTarget';
UPDATE SystemSettings SET SettingValue = 'https://utmpro.link' WHERE SettingKey = 'SiteUrl';
UPDATE SystemSettings SET SettingValue = 'https://app.utmpro.link' WHERE SettingKey = 'AppUrl';
UPDATE SystemSettings SET SettingValue = 'https://go.utmpro.link' WHERE SettingKey = 'RedirectEngineUrl';

-- Update system domain DNS values to the CNAME target hostname
UPDATE Domains SET DNSValue = 'links.utmpro.link', DNSType = 'CNAME' WHERE IsSystemDomain = 1;
```

---

## 3. Build & Publish the Application

### 3.1 Install .NET SDK (Build Machine)

Download .NET 9.0 SDK from https://dotnet.microsoft.com/download/dotnet/9.0

### 3.2 Publish Projects

Open a terminal in the UTMPro solution root:

```bash
# Publish Main Web App
dotnet publish src/UTMPro.Web/UTMPro.Web.csproj -c Release -o publish/web

# Publish Redirect Engine
dotnet publish src/UTMPro.RedirectEngine/UTMPro.RedirectEngine.csproj -c Release -o publish/redirect
```

### 3.3 Copy to Server

Copy the published folders to the server:

```
C:\inetpub\utmpro\
├── web\           ← contents of publish/web/
└── redirect\      ← contents of publish/redirect/
```

You can use:
- **Remote Desktop** → copy/paste folders
- **Web Deploy** (msdeploy)
- **Robocopy** from network share:
  ```cmd
  robocopy \\buildserver\publish\web C:\inetpub\utmpro\web /MIR
  robocopy \\buildserver\publish\redirect C:\inetpub\utmpro\redirect /MIR
  ```

---

## 4. IIS Configuration

### 4.1 Create Application Pools

Open **IIS Manager** → Application Pools → Add Application Pool:

**Pool 1: UTMProWeb**
```
Name:              UTMProWeb
.NET CLR Version:  No Managed Code
Managed Pipeline:  Integrated
Start Immediately: ✅
```

**Pool 2: UTMProRedirect**
```
Name:              UTMProRedirect
.NET CLR Version:  No Managed Code
Managed Pipeline:  Integrated
Start Immediately: ✅
```

For both pools, click **Advanced Settings**:
```
Process Model:
  Identity:              ApplicationPoolIdentity  (or a custom service account)
  Idle Time-out:         0  (never idle)
  
Recycling:
  Regular Time Interval: 0  (disable automatic recycling)
```

### 4.2 Create IIS Sites

#### Site 1: UTMPro Main Web App

```
IIS Manager → Sites → Add Website:

Site name:          UTMProWeb
Application pool:   UTMProWeb
Physical path:      C:\inetpub\utmpro\web
Binding:
  Type:    https
  IP:      All Unassigned
  Port:    443
  Host:    app.utmpro.link
  SSL:     (select your SSL certificate)

Add additional binding:
  Type:    https
  Host:    utmpro.link
  Port:    443
  SSL:     (select your SSL certificate)
```

#### Site 2: UTMPro Redirect Engine

```
IIS Manager → Sites → Add Website:

Site name:          UTMProRedirect
Application pool:   UTMProRedirect
Physical path:      C:\inetpub\utmpro\redirect
Bindings:
  https | go.utmpro.link   | 443 | SSL cert
  https | *.utmpro.link    | 443 | Wildcard SSL cert
  http  | *                | 80  | (for custom domains before SSL)
  https | *                | 443 | (for custom domains with SSL)
```

> **CRITICAL**: The redirect engine MUST receive requests for ALL domains 
> (go.utmpro.link, utmpro.link, and any custom domains users add).
> It uses the `Host` header to look up the correct link.

### 4.3 Web.config Files

These should already exist in the published folders, but verify:

**C:\inetpub\utmpro\web\web.config**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" 
           modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\UTMPro.Web.dll"
                stdoutLogEnabled="true" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

**C:\inetpub\utmpro\redirect\web.config**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" 
           modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\UTMPro.RedirectEngine.dll"
                stdoutLogEnabled="true" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### 4.4 Create Log Directories

```cmd
mkdir C:\inetpub\utmpro\web\logs
mkdir C:\inetpub\utmpro\redirect\logs
mkdir C:\inetpub\utmpro\web\wwwroot\uploads\images
mkdir C:\inetpub\utmpro\web\wwwroot\uploads\logos
mkdir C:\inetpub\utmpro\web\wwwroot\uploads\avatars
mkdir C:\inetpub\utmpro\web\wwwroot\uploads\favicons
```

### 4.5 Set Folder Permissions

```cmd
icacls "C:\inetpub\utmpro\web" /grant "IIS_IUSRS:(OI)(CI)RX" /T
icacls "C:\inetpub\utmpro\web\wwwroot\uploads" /grant "IIS_IUSRS:(OI)(CI)F" /T
icacls "C:\inetpub\utmpro\web\logs" /grant "IIS_IUSRS:(OI)(CI)F" /T
icacls "C:\inetpub\utmpro\redirect" /grant "IIS_IUSRS:(OI)(CI)RX" /T
icacls "C:\inetpub\utmpro\redirect\logs" /grant "IIS_IUSRS:(OI)(CI)F" /T
```

---

## 5. SSL Certificates

### Option A: Let's Encrypt (Free)

Install **win-acme** (https://www.win-acme.com/):

```cmd
# Download and extract win-acme
# Run as Administrator:
wacs.exe

# Follow prompts:
# → Create certificate (default settings)
# → Manual input
# → Enter: utmpro.link, app.utmpro.link, go.utmpro.link
# → Validation: [http-01] Self-hosting
# → Store: Windows Certificate Store
# → Install: IIS
```

### Option B: Wildcard Certificate

If you have a wildcard certificate for `*.utmpro.link`:

```
1. Import .pfx file:
   IIS Manager → Server Certificates → Import
   
2. Assign to bindings:
   Each site → Bindings → Edit → Select certificate
```

### Option C: Self-Signed (Development Only)

```powershell
New-SelfSignedCertificate -DnsName "utmpro.link","*.utmpro.link","app.utmpro.link","go.utmpro.link" -CertStoreLocation "cert:\LocalMachine\My"
```

---

## 6. DNS Configuration

### Required DNS Records

Configure these at your domain registrar (Cloudflare, Namecheap, GoDaddy, etc.):

```
Type    Name              Value              TTL
─────   ────              ─────              ───
A       utmpro.link       YOUR_SERVER_IP     300
A       app.utmpro.link   YOUR_SERVER_IP     300
A       go.utmpro.link    YOUR_SERVER_IP     300
A       *.utmpro.link     YOUR_SERVER_IP     300  (wildcard, optional)
```

### For Custom User Domains

When users add custom domains (e.g., `links.acme.com`), they need to add:

```
Type    Name              Value              TTL
─────   ────              ─────              ───
A       links.acme.com    YOUR_SERVER_IP     300
```

The redirect engine will handle all incoming domains automatically.

---

## 7. Application Configuration

### 7.1 Main Web App — appsettings.json

Edit `C:\inetpub\utmpro\web\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "UTMProDB": "Server=YOUR_SQL_SERVER;Database=UTMProDB;TrustServerCertificate=True;user id=YOUR_USER;password=YOUR_PASSWORD;"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "App": {
    "SiteUrl": "https://utmpro.link",
    "AppUrl": "https://app.utmpro.link",
    "AdminUrl": "https://app.utmpro.link/admin",
    "RedirectEngineUrl": "https://go.utmpro.link",
    "CustomDomainTarget": "links.utmpro.link"
  },
  "SMTP": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "User": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@utmpro.link",
    "FromName": "UTMPro"
  },
  "Stripe": {
    "PublishableKey": "pk_live_xxxxx",
    "SecretKey": "sk_live_xxxxx",
    "WebhookSecret": "whsec_xxxxx",
    "ConnectClientId": "",
    "TrialDays": "14"
  },
  "SAML": {
    "Enabled": true,
    "SpBaseUrl": "https://app.utmpro.link"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 7.2 Redirect Engine — appsettings.json

Edit `C:\inetpub\utmpro\redirect\appsettings.json`:

```json
{
  "ConnectionStrings": {
    "UTMProDB": "Server=YOUR_SQL_SERVER;Database=UTMProDB;TrustServerCertificate=True;user id=YOUR_USER;password=YOUR_PASSWORD;"
  },
  "CacheTTLMinutes": "5",
  "BatchProcessorSeconds": "10",
  "BatchSizeLimit": "500",
  "CacheWarmupCount": "1000",
  "GeoLite2DbPath": "C:\\GeoLite2\\GeoLite2-City.mmdb",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "UTMPro.RedirectEngine": "Information"
    }
  }
}
```

> **IMPORTANT**: Both apps must use the **same connection string** pointing to the same database.

---

## 8. GeoIP Database Setup

### Download GeoLite2

1. Create a free MaxMind account: https://www.maxmind.com/en/geolite2/signup
2. Log in → My Account → Download Databases
3. Download **GeoLite2-City** (GeoLite2-City.mmdb)

### Install

```cmd
mkdir C:\GeoLite2
copy GeoLite2-City.mmdb C:\GeoLite2\GeoLite2-City.mmdb
```

Verify the path matches `GeoLite2DbPath` in the redirect engine's appsettings.json.

> **Note**: If you skip this step, the app still works — GeoIP data will just show as "Unknown".

---

## 9. Google OAuth Setup

### Create OAuth Credentials

1. Go to https://console.cloud.google.com/
2. Create a new project (or select existing)
3. Navigate to **APIs & Services → Credentials**
4. Click **Create Credentials → OAuth 2.0 Client IDs**
5. Application type: **Web application**
6. Name: UTMPro
7. **Authorized redirect URIs**:
   ```
   https://app.utmpro.link/signin-google
   ```
8. Copy the **Client ID** and **Client Secret** to appsettings.json

> **CRITICAL**: The redirect URI must be exactly `https://app.utmpro.link/signin-google` 
> (not `/auth/google/callback`). The `/signin-google` path is handled internally by 
> the Google OAuth middleware.

---

## 10. Stripe Setup

### 10.1 Create Stripe Account

1. Sign up at https://stripe.com
2. Complete business verification

### 10.2 Get API Keys

1. Dashboard → Developers → API Keys
2. Copy **Publishable key** (`pk_live_xxx`) and **Secret key** (`sk_live_xxx`)

### 10.3 Create Products & Prices in Stripe

Create 3 products matching your plans:

```
Product: Pro Plan        → Price: $30/month  (price_pro_monthly)
Product: Business Plan   → Price: $90/month  (price_business_monthly)
Product: Advanced Plan   → Price: $300/month (price_advanced_monthly)
```

Then update the database:
```sql
UPDATE StripePrices SET StripePriceId = 'price_ACTUAL_ID_FROM_STRIPE' 
WHERE StripePriceId = 'price_pro_monthly';
-- Repeat for all 6 prices (monthly + yearly for each plan)
```

### 10.4 Configure Webhook

1. Stripe Dashboard → Developers → Webhooks → Add endpoint
2. Endpoint URL: `https://app.utmpro.link/webhooks/stripe`
3. Events to send:
   - `checkout.session.completed`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`
   - `customer.subscription.trial_will_end`
4. Copy the **Webhook signing secret** (`whsec_xxx`) to appsettings.json

---

## 11. First Admin User

### 11.1 Register

1. Open https://app.utmpro.link/register
2. Create your admin account (name, email, password)
3. Create a workspace when prompted

### 11.2 Grant SuperAdmin

```sql
-- Replace with your email
UPDATE Users SET IsSuperAdmin = 1 WHERE Email = 'admin@utmpro.link';
```

### 11.3 Verify Admin Access

1. Log out and log back in
2. Navigate to https://app.utmpro.link/admin
3. You should see the admin dashboard

---

## 12. Verify Installation

### Checklist

```
[ ] Main web app loads:        https://app.utmpro.link
[ ] Landing page loads:        https://utmpro.link
[ ] Login works:               https://app.utmpro.link/login
[ ] Registration works:        https://app.utmpro.link/register
[ ] Google OAuth works:        https://app.utmpro.link/auth/google
[ ] Admin portal works:        https://app.utmpro.link/admin
[ ] Create a test link on go.utmpro.link domain
[ ] Click the test short link:  https://go.utmpro.link/SLUG → redirects correctly
[ ] Create a test link on utmpro.link domain
[ ] Click it:                   https://utmpro.link/SLUG → redirects correctly
[ ] Analytics show clicks:     Check after 10-15 seconds
[ ] Redirect engine health:    https://go.utmpro.link/health → {"status":"healthy"}
[ ] Blog page loads:           https://app.utmpro.link/blog
[ ] API docs load:             https://app.utmpro.link/docs/api
[ ] Billing page loads:        https://app.utmpro.link/SLUG/settings/billing
```

### Test Redirect Engine

```cmd
# From server or any machine:
curl -v https://go.utmpro.link/health
# Should return: {"status":"healthy","timestamp":"2026-..."}

# Test a redirect (replace SLUG with actual):
curl -v -L https://go.utmpro.link/test123
# Should show 302 redirect to destination URL
```

### Check Click Recording

1. Create a link
2. Click it 3 times
3. Wait 15 seconds (batch processor interval is 10s)
4. Check Analytics page — should show 3 clicks
5. If not, check redirect engine logs:
   ```cmd
   type C:\inetpub\utmpro\redirect\logs\stdout*.log
   ```

---

## 13. Troubleshooting

### Common Issues

#### "502 Bad Gateway" or "500 Internal Server Error"

```
1. Check if .NET Hosting Bundle is installed:
   C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\
   Should contain 9.0.x folder

2. Check stdout logs:
   C:\inetpub\utmpro\web\logs\stdout*.log
   C:\inetpub\utmpro\redirect\logs\stdout*.log

3. Enable stdout logging in web.config:
   stdoutLogEnabled="true"

4. Restart IIS:
   iisreset
```

#### "Links on utmpro.link don't redirect"

```
1. The redirect engine must be bound to receive utmpro.link requests
   Check IIS → UTMProRedirect → Bindings
   Must include: utmpro.link on port 443

2. The link must be created with domain "utmpro.link" (not "go.utmpro.link")
   Check in database:
   SELECT d.Domain, l.Slug FROM Links l 
   INNER JOIN Domains d ON l.DomainId = d.Id

3. Verify the redirect engine can reach the database:
   Check redirect engine logs for connection errors
```

#### "Click events not recording"

```
1. Check batch processor logs:
   Look for "Processing batch of X clicks" or errors
   
2. Verify connection string in redirect engine appsettings.json
   Must match the same database as the web app

3. Check if sp_BulkInsertClickEvents stored procedure exists:
   SELECT * FROM sys.procedures WHERE name = 'sp_BulkInsertClickEvents';

4. Manual test — insert a click directly:
   INSERT INTO ClickEvents (LinkId, WorkspaceId, ClickedAt) 
   VALUES (1, 1, GETUTCDATE());
```

#### "Google OAuth redirects to error"

```
1. Verify redirect URI in Google Console matches EXACTLY:
   https://app.utmpro.link/signin-google
   
2. Check that Google ClientId and ClientSecret are correct in appsettings.json

3. The callback path is /signin-google (handled by middleware),
   NOT /auth/google/callback
```

#### "Stripe webhook fails"

```
1. Webhook URL must be: https://app.utmpro.link/webhooks/stripe

2. Verify webhook secret (whsec_xxx) in appsettings.json

3. Test with Stripe CLI:
   stripe listen --forward-to https://app.utmpro.link/webhooks/stripe
   stripe trigger checkout.session.completed
```

### View Logs

```cmd
# Real-time web app logs
type C:\inetpub\utmpro\web\logs\stdout_*.log

# Real-time redirect engine logs
type C:\inetpub\utmpro\redirect\logs\stdout_*.log

# Windows Event Viewer → Application logs (ASP.NET Core errors)
eventvwr.msc
```

---

## 14. Maintenance & Updates

### Deploy Updates

```cmd
# 1. Build new release
dotnet publish src/UTMPro.Web/UTMPro.Web.csproj -c Release -o publish/web
dotnet publish src/UTMPro.RedirectEngine/UTMPro.RedirectEngine.csproj -c Release -o publish/redirect

# 2. Stop IIS sites
%windir%\system32\inetsrv\appcmd stop site /site.name:UTMProWeb
%windir%\system32\inetsrv\appcmd stop site /site.name:UTMProRedirect

# 3. Copy new files (preserve appsettings.json!)
robocopy publish\web C:\inetpub\utmpro\web /MIR /XF appsettings.json appsettings.Production.json
robocopy publish\redirect C:\inetpub\utmpro\redirect /MIR /XF appsettings.json appsettings.Production.json

# 4. Run any new SQL migration scripts

# 5. Start IIS sites
%windir%\system32\inetsrv\appcmd start site /site.name:UTMProWeb
%windir%\system32\inetsrv\appcmd start site /site.name:UTMProRedirect
```

### Database Backup

```sql
-- Full backup
BACKUP DATABASE UTMProDB 
TO DISK = 'C:\Backups\UTMProDB_full.bak'
WITH COMPRESSION, INIT;

-- Schedule daily via SQL Server Agent or Windows Task Scheduler
```

### GeoIP Database Update

MaxMind updates GeoLite2 weekly. Set up auto-update:

```cmd
# Download geoipupdate from MaxMind
# Configure C:\GeoLite2\GeoIP.conf with your license key
# Schedule weekly task:
schtasks /create /tn "GeoIP Update" /tr "C:\GeoLite2\geoipupdate.exe" /sc weekly /d MON
```

### Monitor Health

```cmd
# Redirect engine health check (cron or monitoring tool)
curl -s https://go.utmpro.link/health

# Check app pool status
%windir%\system32\inetsrv\appcmd list apppool /state:Stopped
```

---

## Architecture Reference

```
Internet Traffic
     │
     ▼
  [ DNS ]
     │
     ├── utmpro.link ──────────────► IIS Site: UTMProRedirect (redirect engine)
     ├── go.utmpro.link ───────────► IIS Site: UTMProRedirect (redirect engine)  
     ├── app.utmpro.link ──────────► IIS Site: UTMProWeb (main app)
     ├── *.utmpro.link ────────────► IIS Site: UTMProRedirect (custom subdomains)
     └── custom.domain.com ────────► IIS Site: UTMProRedirect (user custom domains)
                                          │
                                          ▼
                                    [ SQL Server ]
                                     UTMProDB
                                          │
                                    ┌─────┴─────┐
                                    │  28 Core  │
                                    │  Tables   │
                                    │  + 20 P2  │
                                    │  + 10 Ext │
                                    └───────────┘
```

---

## File Paths Summary

```
C:\inetpub\utmpro\
├── web\                           Main web app
│   ├── UTMPro.Web.dll
│   ├── appsettings.json          ← EDIT THIS
│   ├── web.config
│   ├── wwwroot\
│   │   ├── uploads\
│   │   │   ├── images\
│   │   │   ├── logos\
│   │   │   ├── avatars\
│   │   │   └── favicons\
│   │   └── css\
│   └── logs\
│       └── stdout_*.log
├── redirect\                      Redirect engine
│   ├── UTMPro.RedirectEngine.dll
│   ├── appsettings.json          ← EDIT THIS
│   ├── web.config
│   └── logs\
│       └── stdout_*.log
└── (backups)\

C:\GeoLite2\
└── GeoLite2-City.mmdb            GeoIP database

Database: UTMProDB on SQL Server
  Scripts: UTMPro\database\001-010_*.sql
```

---

**Installation complete!** 🎉

For support, check the application logs first, then the troubleshooting section above.
