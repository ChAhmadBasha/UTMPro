using MaxMind.GeoIP2;

namespace UTMPro.RedirectEngine.Services;

public class GeoIpService : IDisposable
{
    private DatabaseReader? _reader;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(IConfiguration config, ILogger<GeoIpService> logger)
    {
        _logger = logger;
        try
        {
            var path = config["GeoLite2DbPath"] ?? "C:\\GeoLite2\\GeoLite2-City.mmdb";
            if (File.Exists(path))
            {
                _reader = new DatabaseReader(path);
                _logger.LogInformation("GeoIP database loaded: {path}", path);
            }
            else
            {
                _logger.LogWarning("GeoIP database not found at: {path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("GeoIP database error: {msg}", ex.Message);
        }
    }

    public GeoResult Lookup(string? ip)
    {
        if (_reader == null || string.IsNullOrEmpty(ip) || ip == "0.0.0.0" || ip == "::1" || ip == "127.0.0.1")
            return new GeoResult();

        try
        {
            var city = _reader.City(ip);
            return new GeoResult
            {
                Country = city.Country.Name,
                CountryCode = city.Country.IsoCode,
                City = city.City.Name,
                Region = city.MostSpecificSubdivision.Name,
                Continent = city.Continent.Name,
                Latitude = (decimal?)city.Location.Latitude,
                Longitude = (decimal?)city.Location.Longitude
            };
        }
        catch
        {
            return new GeoResult();
        }
    }

    public void Dispose() => _reader?.Dispose();
}

public class GeoResult
{
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? Continent { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
