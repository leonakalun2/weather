using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Entities;

namespace WeatherService.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly HttpClient _httpClient;

        public NotificationService(ILogger<NotificationService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public Task SendAlertEmailAsync(WeatherAlertEntity alert, WeatherRecordEntity currentWeather, string message)
        {
            //pending implmentation
            _logger.LogInformation(
                $"ALERT EMAIL → Email: {alert.SubscriberEmail} | Message: {message} | Temp: {currentWeather.Temperature}°C | PSI: {currentWeather.Psi}");
            return Task.CompletedTask;
        }

        public async Task SendWebhookAsync(string webhookUrl, object payload)
        {
            _logger.LogInformation($"Webhook triggered Url: {webhookUrl}");
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(webhookUrl, content);

            response.EnsureSuccessStatusCode();
            return;
        }
    }
}
