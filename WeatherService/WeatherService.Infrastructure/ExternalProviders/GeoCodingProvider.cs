using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherService.Infrastructure.Interfaces;
using static System.Net.WebRequestMethods;

namespace WeatherService.Infrastructure.ExternalProviders
{
    public class GeoCodingProvider : IGeocodingProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeoCodingProvider> _logger;
        private const string _baseUrl = "https://nominatim.openstreetmap.org";
        private const double _defaultLat = 1.3521;
        private const double _defaultLon = 103.8198;

        public GeoCodingProvider(HttpClient http, ILogger<GeoCodingProvider> logger)
        {
            _httpClient = http;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "WeatherMicroservice/1.0");
        }

        public async Task<(double Lat, double Lon, string FormattedName)?> GeocodeAsync(string location)
        {
            try
            {
                var url = $"{_baseUrl}/search" +
                           $"?q={Uri.EscapeDataString(location)}" +
                           $"&format=json&limit=1&addressdetails=1";
                var resp = await _httpClient.GetAsync(url);
                resp.EnsureSuccessStatusCode();

                var results = JsonDocument.Parse(await resp.Content.ReadAsStringAsync())
                    .RootElement.EnumerateArray().ToList();

                if (!results.Any())
                {
                    _logger.LogWarning(
                        $"No geocoding result for Location: {location} — defaulting to Singapore",
                        location);
                    return null;
                }

                var first = results[0];
                var lat = double.Parse(
                    first.GetProperty("lat").GetString() ?? "1.3521",
                    System.Globalization.CultureInfo.InvariantCulture);
                var lon = double.Parse(
                    first.GetProperty("lon").GetString() ?? "103.8198",
                    System.Globalization.CultureInfo.InvariantCulture);

                var name = BuildFormattedName(first, location);

                //only support SG now
                if (!name.Contains("SG"))
                    return null;

                return (lat, lon, name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geocoding failed for '{Location}'", location);
                return (_defaultLat, _defaultLon, "Singapore, SG");
            }
        }


        Task<string?> IGeocodingProvider.ReverseGeocodeAsync(double lat, double lon)
        {
            throw new NotImplementedException();
        }

        #region private methods
        private static string BuildFormattedName(JsonElement result, string fallback)
        {
            if (!result.TryGetProperty("address", out var addr))
                return fallback;

            return BuildFormattedNameFromAddress(addr);
        }

        private static string BuildFormattedNameFromAddress(JsonElement addr)
        {
            var city = addr.TryGetProperty("suburb", out var c) ? c.GetString() :
                       addr.TryGetProperty("town", out var t) ? t.GetString() :
                       addr.TryGetProperty("county", out var co) ? co.GetString() :
                       addr.TryGetProperty("country", out var ct) ? ct.GetString() :
                       "Singapore";

            var countryCode = addr.TryGetProperty("country_code", out var cc)
                ? cc.GetString()?.ToUpper()
                : "SG";

            return $"{city}, {countryCode}";
        }
        #endregion
    }
}
