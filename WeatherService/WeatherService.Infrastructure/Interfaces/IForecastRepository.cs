using WeatherService.Model.Dtos;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface IForecastRepository
    {
        Task<IList<ForecastRecordEntity>> GetForecastAsync(string location, int days = 4);
        Task SaveForecastAsync(IList<ForecastRecordEntity> forecasts);
        Task DeleteOldForecastsAsync(string location);
    }
}
