using Microsoft.AspNetCore.Mvc;

namespace UTMPro.Web.Controllers;

[Route("docs")]
public class DocsController : Controller
{
    // ── Getting Started ───────────────────────────────────
    [HttpGet("")] public IActionResult Index() => View("~/Views/Docs/Index.cshtml");
    [HttpGet("getting-started")] public IActionResult GettingStarted() => View("~/Views/Docs/GettingStarted.cshtml");
    [HttpGet("create-account")] public IActionResult CreateAccount() => View("~/Views/Docs/CreateAccount.cshtml");
    [HttpGet("workspace-setup")] public IActionResult WorkspaceSetup() => View("~/Views/Docs/WorkspaceSetup.cshtml");

    // ── Links ────────────────────────────────────────────
    [HttpGet("links")] public IActionResult Links() => View("~/Views/Docs/Links.cshtml");
    [HttpGet("create-short-link")] public IActionResult CreateShortLink() => View("~/Views/Docs/CreateShortLink.cshtml");
    [HttpGet("link-redirects")] public IActionResult LinkRedirects() => View("~/Views/Docs/LinkRedirects.cshtml");
    [HttpGet("password-protected-links")] public IActionResult PasswordLinks() => View("~/Views/Docs/PasswordLinks.cshtml");
    [HttpGet("link-expiration")] public IActionResult LinkExpiration() => View("~/Views/Docs/LinkExpiration.cshtml");
    [HttpGet("link-cloaking")] public IActionResult LinkCloaking() => View("~/Views/Docs/LinkCloaking.cshtml");
    [HttpGet("ab-testing")] public IActionResult ABTesting() => View("~/Views/Docs/ABTesting.cshtml");
    [HttpGet("link-in-bio")] public IActionResult LinkInBio() => View("~/Views/Docs/LinkInBio.cshtml");
    [HttpGet("bulk-import-export")] public IActionResult BulkImportExport() => View("~/Views/Docs/BulkImportExport.cshtml");

    // ── UTM & Tracking ───────────────────────────────────
    [HttpGet("utm-builder")] public IActionResult UTMBuilder() => View("~/Views/Docs/UTMBuilder.cshtml");
    [HttpGet("utm-parameters-explained")] public IActionResult UTMExplained() => View("~/Views/Docs/UTMExplained.cshtml");
    [HttpGet("utm-templates")] public IActionResult UTMTemplates() => View("~/Views/Docs/UTMTemplates.cshtml");
    [HttpGet("conversion-tracking")] public IActionResult ConversionTracking() => View("~/Views/Docs/ConversionTracking.cshtml");

    // ── Analytics ────────────────────────────────────────
    [HttpGet("analytics")] public IActionResult Analytics() => View("~/Views/Docs/Analytics.cshtml");
    [HttpGet("click-analytics")] public IActionResult ClickAnalytics() => View("~/Views/Docs/ClickAnalytics.cshtml");
    [HttpGet("geo-analytics")] public IActionResult GeoAnalytics() => View("~/Views/Docs/GeoAnalytics.cshtml");
    [HttpGet("device-analytics")] public IActionResult DeviceAnalytics() => View("~/Views/Docs/DeviceAnalytics.cshtml");

    // ── Domains & QR ─────────────────────────────────────
    [HttpGet("custom-domains")] public IActionResult CustomDomains() => View("~/Views/Docs/CustomDomains.cshtml");
    [HttpGet("domain-verification")] public IActionResult DomainVerification() => View("~/Views/Docs/DomainVerification.cshtml");
    [HttpGet("qr-codes")] public IActionResult QRCodes() => View("~/Views/Docs/QRCodes.cshtml");

    // ── Team & Workspace ─────────────────────────────────
    [HttpGet("team-management")] public IActionResult TeamManagement() => View("~/Views/Docs/TeamManagement.cshtml");
    [HttpGet("roles-permissions")] public IActionResult RolesPermissions() => View("~/Views/Docs/RolesPermissions.cshtml");

    // ── Billing & Plans ──────────────────────────────────
    [HttpGet("billing")] public IActionResult Billing() => View("~/Views/Docs/Billing.cshtml");
    [HttpGet("free-trial")] public IActionResult FreeTrial() => View("~/Views/Docs/FreeTrial.cshtml");

    // ── Advanced ─────────────────────────────────────────
    [HttpGet("webhooks")] public IActionResult Webhooks() => View("~/Views/Docs/Webhooks.cshtml");
    [HttpGet("partner-program")] public IActionResult PartnerProgram() => View("~/Views/Docs/PartnerProgram.cshtml");
    [HttpGet("integrations")] public IActionResult Integrations() => View("~/Views/Docs/Integrations.cshtml");
    [HttpGet("sso-saml")] public IActionResult SSOSAML() => View("~/Views/Docs/SSOSAML.cshtml");
    [HttpGet("api-reference")] public IActionResult APIReference() => View("~/Views/Docs/APIReference.cshtml");
    [HttpGet("browser-extension")] public IActionResult BrowserExtension() => View("~/Views/Docs/BrowserExtension.cshtml");
    [HttpGet("social-link-previews")] public IActionResult SocialLinkPreviews() => View("~/Views/Docs/SocialLinkPreviews.cshtml");

    // ── FAQ ──────────────────────────────────────────────
    [HttpGet("faq")] public IActionResult FAQ() => View("~/Views/Docs/FAQ.cshtml");
}
