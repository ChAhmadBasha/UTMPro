# PART 17: PHASE 2 NUGET PACKAGES (Additional)

```xml
<!-- UTMPro.Web/UTMPro.Web.csproj - ADD to Phase 1 packages -->
<ItemGroup>
  <!-- Stripe -->
  <PackageReference Include="Stripe.net" Version="46.3.0" />
  
  <!-- SignalR (built-in, just reference) -->
  <!-- Microsoft.AspNetCore.SignalR is included in ASP.NET Core -->
  
  <!-- SAML -->
  <PackageReference Include="ITfoxtec.Identity.Saml2" 
                    Version="4.9.0" />
  
  <!-- PDF Generation for invoices -->
  <PackageReference Include="QuestPDF" Version="2024.10.5" />
  
  <!-- Rate limiting -->
  <!-- Built into .NET 7+ - Microsoft.AspNetCore.RateLimiting -->
</ItemGroup>
```

---
