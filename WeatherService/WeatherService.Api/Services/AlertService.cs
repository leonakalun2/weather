using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos.Requests;
using WeatherService.Model.Entities;
using WeatherService.Model.Enums;

namespace WeatherService.Api.Services
{
    public class AlertService : IAlertService
    {
        private readonly IAlertRepository _alertRepo;
        private readonly IWeatherRepository _weatherRepo;
        private readonly IGeocodingProvider _geocoding;
        private readonly INotificationService _notification;
        private readonly ILogger<AlertService> _logger;

        public AlertService(IAlertRepository alertRepo, IWeatherRepository weatherRepo, IGeocodingProvider geocoding,
            INotificationService notification, ILogger<AlertService> logger)
        {
            _alertRepo = alertRepo;
            _weatherRepo = weatherRepo;
            _geocoding = geocoding;
            _notification = notification;
            _logger = logger;
        }

        public async Task<WeatherAlertEntity> CreateAlertAsync(CreateAlertRequestDto req)
        {
            var geo = await _geocoding.GeocodeAsync(req.Location);

            var alert = new WeatherAlertEntity
            {
                SubscriberEmail = req.SubscriberEmail.Trim().ToLower(),
                SubscriberName = req.SubscriberName,
                Location = geo?.FormattedName ?? req.Location,
                Latitude = geo?.Lat ?? 0,
                Longitude = geo?.Lon ?? 0,
                ConditionType = req.ConditionType,
                ThresholdValue = req.ThresholdValue,
                Operator = req.Operator,
                WebhookUrl = req.WebhookUrl
            };

            return await _alertRepo.CreateAsync(alert);
        }

        public async Task<WeatherAlertEntity?> GetAlertByIdAsync(Guid id)
        {
            return await _alertRepo.GetByIdAsync(id);
        }

        public async Task<IList<WeatherAlertEntity>> GetAlertsByEmailAsync(string email)
        {
            return await _alertRepo.GetByEmailAsync(email.Trim().ToLower());
        }

        public async Task<WeatherAlertEntity> UpdateAlertAsync(Guid id, UpdateAlertRequestDto req)
        {
            var alert = await _alertRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Alert ID: {id} not found");

            if (req.ThresholdValue.HasValue)
                alert.ThresholdValue = req.ThresholdValue.Value;

            if (req.Operator.HasValue)
                alert.Operator = req.Operator.Value;

            if (req.IsActive.HasValue)
                alert.IsActive = req.IsActive.Value;

            if (!string.IsNullOrEmpty(req.WebhookUrl))
                alert.WebhookUrl = req.WebhookUrl;

            await _alertRepo.UpdateAsync(alert);
            return alert;
        }

        public Task DeleteAlertAsync(Guid id) => _alertRepo.DeleteAsync(id);

        public async Task ProcessAlertsAsync()
        {
            var alerts = await _alertRepo.GetActiveAlertsAsync();
            _logger.LogDebug("Processing {Count} active alerts", alerts.Count());

            foreach (var alert in alerts)
            {
                try
                {
                    var weather = await _weatherRepo.GetLatestByLocationAsync(alert.Location);
                    if (weather == null)
                    {
                        _logger.LogDebug("No weather data for alert location '{Location}'", alert.Location);
                        continue;
                    }

                    var value = GetMetricValue(weather, alert.ConditionType);
                    var triggered = Evaluate(value, alert.ThresholdValue, alert.Operator);

                    if (!triggered)
                        continue;

                    var msg = BuildMessage(alert, value);
                    await _notification.SendAlertEmailAsync(alert, weather, msg);

                    if (!string.IsNullOrEmpty(alert.WebhookUrl))
                    {
                        await _notification.SendWebhookAsync(alert.WebhookUrl, new
                        {
                            alertId = alert.Id,
                            location = alert.Location,
                            conditionType = alert.ConditionType.ToString(),
                            currentValue = value,
                            threshold = alert.ThresholdValue,
                            @operator = alert.Operator.ToString(),
                            message = msg,
                            timestamp = DateTime.UtcNow
                        });
                    }

                    alert.LastTriggeredAt = DateTime.UtcNow;
                    alert.TriggerCount++;
                    await _alertRepo.UpdateAsync(alert);

                    _logger.LogInformation($"Alert Id: {alert.Id} triggered for Email: {alert.SubscriberEmail} — Type: {alert.ConditionType} - Value: {value}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing alert Id: {alert.Id}");
                }
            }
        }


        #region private methods
        private static double GetMetricValue(WeatherRecordEntity r, ConditionTypeEnum type) =>
        type switch
        {
            ConditionTypeEnum.Temperature => r.Temperature,
            ConditionTypeEnum.Humidity => r.Humidity,
            ConditionTypeEnum.RainfallMm => r.RainfallMm,
            ConditionTypeEnum.WindSpeedMs => r.WindSpeedMs,
            ConditionTypeEnum.Pm25 => r.Pm25,
            ConditionTypeEnum.Psi => r.Psi,
            ConditionTypeEnum.UvIndex => r.UvIndex,
            _ => 0
        };

        private static bool Evaluate(double value, double threshold, ThresholdOperatorEnum op) =>
            op switch
            {
                ThresholdOperatorEnum.GreaterThan => value > threshold,
                ThresholdOperatorEnum.LessThan => value < threshold,
                ThresholdOperatorEnum.GreaterThanOrEqual => value >= threshold,
                ThresholdOperatorEnum.LessThanOrEqual => value <= threshold,
                _ => false
            };

        private static string BuildMessage(WeatherAlertEntity a, double value)
        {
            var (unit, label) = a.ConditionType switch
            {
                ConditionTypeEnum.Temperature => ("°C", "Temperature"),
                ConditionTypeEnum.Humidity => ("%", "Humidity"),
                ConditionTypeEnum.RainfallMm => ("mm", "Rainfall"),
                ConditionTypeEnum.WindSpeedMs => ("m/s", "Wind speed"),
                ConditionTypeEnum.Pm25 => ("µg/m³", "PM2.5"),
                ConditionTypeEnum.Psi => ("", "PSI"),
                ConditionTypeEnum.UvIndex => ("", "UV index"),
                _ => ("", a.ConditionType.ToString())
            };

            return $"Alert for {a.Location}: {label} is {value}{unit} " +
                   $"({a.Operator} threshold of {a.ThresholdValue}{unit})";
        }
        #endregion
    }
}
