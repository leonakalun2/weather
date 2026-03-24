using WeatherService.Model.Enums;

namespace WeatherService.Model.Dtos
{
    public class WeatherAlertEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string SubscriberEmail { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;

        // Stored as canonical geocoded name e.g. "Singapore, SG"
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public ConditionTypeEnum ConditionType { get; set; }
        public double ThresholdValue { get; set; }
        public ThresholdOperatorEnum Operator { get; set; }

        public bool IsActive { get; set; } = true;
        public string WebhookUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastTriggeredAt { get; set; }
        public int TriggerCount { get; set; } = 0;
    }
}
