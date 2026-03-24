using WeatherService.Model.Dtos;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface INotificationService
    {
        Task SendAlertEmailAsync(WeatherAlertEntity alert, WeatherRecordEntity currentWeather, string message);
        Task SendWebhookAsync(string webhookUrl, object payload);
    }
}
