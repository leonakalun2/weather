using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;
using System.Text.Json;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos;
using WeatherService.Model.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace WeatherService.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly HttpClient _httpClient;
        private readonly EmailSettings _settings;

        public NotificationService(ILogger<NotificationService> logger, HttpClient httpClient, IConfiguration config)
        {
            _logger = logger;
            _httpClient = httpClient;
            _settings = config.GetSection("Email").Get<EmailSettings>();
        }

        public async Task SendAlertEmailAsync(WeatherAlertEntity alert, WeatherRecordEntity currentWeather, string message)
        {
            try
            {
                var email = new MimeMessage();

                email.From.Add(new MailboxAddress(
                    _settings.DisplayName,
                    _settings.From));

                email.To.Add(MailboxAddress.Parse(alert.SubscriberEmail));
                email.Subject = $"Weather alert for {alert.ConditionType} at {alert.Location}";

                email.Body = new TextPart("html")
                {
                    Text = message
                };

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation(
                    $"ALERT EMAIL → Email: {alert.SubscriberEmail} | Message: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send alert email to {alert.SubscriberEmail}. Error: {ex.Message}");
            }
            return;
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
