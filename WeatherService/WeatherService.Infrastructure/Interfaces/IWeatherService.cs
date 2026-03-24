using WeatherService.Model.Dtos;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherRecordEntity?> GetCurrentWeatherAsync(string location);
        //Task<WeatherRecordEntity?> GetCurrentWeatherByCoordinatesAsync(double lat, double lon);
        Task<IList<ForecastRecordEntity>> GetForecastAsync(string location, int days = 7);
        Task<IList<WeatherRecordEntity>> GetHistoricalAsync(string location, DateTime from, DateTime to);
        Task<byte[]> ExportToCsvAsync(string location, DateTime from, DateTime to);
        Task RefreshWeatherDataAsync(string location);
    }
}
