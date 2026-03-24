namespace WeatherService.Infrastructure.Interfaces
{
    public interface IGeocodingProvider
    {
        Task<(double Lat, double Lon, string FormattedName)?> GeocodeAsync(string location);
        Task<string?> ReverseGeocodeAsync(double lat, double lon);
    }
}
