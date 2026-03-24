using WeatherService.Model.Dtos.Requests;
using WeatherService.Model.Entities;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface IAlertService
    {
        Task<WeatherAlertEntity> CreateAlertAsync(CreateAlertRequestDto request);
        Task<IList<WeatherAlertEntity>> GetAlertsByEmailAsync(string email);
        Task DeleteAlertAsync(Guid id);
        Task ProcessAlertsAsync();
        Task<WeatherAlertEntity?> GetAlertByIdAsync(Guid id);
        Task<WeatherAlertEntity> UpdateAlertAsync(Guid id, UpdateAlertRequestDto request);
    }
}
