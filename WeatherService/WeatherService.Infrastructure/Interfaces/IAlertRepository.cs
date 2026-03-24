using WeatherService.Model.Entities;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface IAlertRepository
    {
        Task<WeatherAlertEntity> CreateAsync(WeatherAlertEntity alert);
        Task<WeatherAlertEntity?> GetByIdAsync(Guid id);
        Task<IList<WeatherAlertEntity>> GetActiveAlertsAsync();
        Task<IList<WeatherAlertEntity>> GetByEmailAsync(string email);
        Task UpdateAsync(WeatherAlertEntity alert);
        Task DeleteAsync(Guid id);
    }
}
