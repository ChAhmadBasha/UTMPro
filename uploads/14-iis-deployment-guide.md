# PART 14: IIS DEPLOYMENT GUIDE

```
STEP 1: Install .NET 9.0 Hosting Bundle on Windows Server

STEP 2: Create IIS Sites

Site 1: UTMPro Main App
  Physical path: C:\inetpub\utmpro\main\
  Bindings:
    https | utmpro.co       | port 443 | SNI cert
    https | app.utmpro.co   | port 443 | SNI cert
  App Pool: UTMProMain
    .NET CLR: No Managed Code
    Pipeline: Integrated
    Identity: ApplicationPoolIdentity

Site 2: UTMPro Admin
  Physical path: C:\inetpub\utmpro\admin\
  Bindings:
    https | admin.utmpro.co | port 443 | SNI cert
  App Pool: UTMProAdmin
    .NET CLR: No Managed Code

Site 3: UTMPro Redirect Engine  
  Physical path: C:\inetpub\utmpro\redirect\
  Bindings:
    https | go.utmpro.co    | port 443 | SNI cert
    https | *.utmpro.co     | port 443 | wildcard cert
    http  | *               | port 80  | (for custom domains)
    https | *               | port 443 | (for custom domains)
  App Pool: UTMProRedirect
    .NET CLR: No Managed Code
  NOTE: Custom domain sites need web.config URL rewrite

STEP 3: web.config for Redirect Engine
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" 
           path="*" verb="*"
           modules="AspNetCoreModuleV2"
           resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\UTMPro.RedirectEngine.dll"
                stdoutLogEnabled="false"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable 
          name="ASPNETCORE_ENVIRONMENT" 
          value="Production" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>

STEP 4: GeoLite2 Database
  Download from: maxmind.com (free account required)
  File: GeoLite2-City.mmdb
  Place at: C:\GeoLite2\GeoLite2-City.mmdb
  Update SystemSettings: GeoLite2DbPath

STEP 5: SQL Server
  Create database: UTMProDB
  Run: 001_Schema.sql
  Run: 002_SeedData.sql
  Run: 003_StoredProcedures.sql

STEP 6: Connection String
  Use Windows Auth or SQL Auth
  Test: sqlcmd -S . -d UTMProDB -E -Q "SELECT 1"

STEP 7: First Admin User
  After deployment, register normally at utmpro.co/register
  Then run SQL:
  UPDATE Users SET IsSuperAdmin = 1 
  WHERE Email = 'admin@utmpro.co';
```

---
