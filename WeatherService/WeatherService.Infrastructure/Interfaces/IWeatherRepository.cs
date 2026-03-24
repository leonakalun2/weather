using WeatherService.Model.Dtos;
using WeatherService.Model.Dtos.Responses;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface IWeatherRepository
    {
        Task<WeatherRecordEntity?> GetLatestByLocationAsync(string location);
        Task<IList<WeatherRecordEntity>> GetHistoricalAsync(string location, DateTime from, DateTime to);
        Task<WeatherRecordEntity> SaveAsync(WeatherRecordEntity record);
        Task<IList<string>> GetAllLocationsAsync();
    }
}
