# PART 10: APPSETTINGS CONFIGURATION

```json
// File: UTMPro.Web/appsettings.json
{
  "ConnectionStrings": {
    "UTMProDB": "Server=.;Database=UTMProDB;
     Integrated Security=true;
     TrustServerCertificate=true;
     MultipleActiveResultSets=true;"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "App": {
    "SiteUrl": "https://utmpro.co",
    "AppUrl": "https://app.utmpro.co",
    "AdminUrl": "https://admin.utmpro.co",
    "RedirectEngineUrl": "https://go.utmpro.co",
    "ServerIP": "76.76.21.21"
  },
  "GeoLite2": {
    "DbPath": "C:\\GeoLite2\\GeoLite2-City.mmdb"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}

// File: UTMPro.RedirectEngine/appsettings.json
{
  "ConnectionStrings": {
    "UTMProDB": "Server=.;Database=UTMProDB;
     Integrated Security=true;
     TrustServerCertificate=true;"
  },
  "CacheTTLMinutes": "5",
  "BatchProcessorSeconds": "30",
  "BatchSizeLimit": "500",
  "CacheWarmupCount": "1000",
  "GeoLite2DbPath": "C:\\GeoLite2\\GeoLite2-City.mmdb",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "UTMPro.RedirectEngine": "Debug"
    }
  }
}
```

---
