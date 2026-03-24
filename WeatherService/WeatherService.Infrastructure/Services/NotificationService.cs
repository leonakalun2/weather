using Microsoft.Extensions.Logging;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Entities;

namespace WeatherService.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ILogger logger)
        {
            _logger = _logger;
        }

        public Task SendAlertEmailAsync(WeatherAlertEntity alert, WeatherRecordEntity currentWeather, string message)
        {
            _logger.LogInformation(
                $"ALERT EMAIL → Email: {alert.SubscriberEmail} | Message: {message} | Temp: {currentWeather.Temperature}°C | PSI: {currentWeather.Psi}");
            return Task.CompletedTask;
        }

        public Task SendWebhookAsync(string webhookUrl, object payload)
        {
            _logger.LogInformation($"Webhook triggered Url: {webhookUrl}");
            return Task.CompletedTask;
        }
    }
}
