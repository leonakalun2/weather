namespace WeatherService.Model.Dtos.Responses
{
    public class AlertResponseDto
    {
        public Guid Id { get; set; }
        public string SubscriberEmail { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ConditionType { get; set; } = string.Empty;
        public string ConditionUnit { get; set; } = string.Empty;
        public double ThresholdValue { get; set; }
        public string Operator { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastTriggeredAt { get; set; }
        public int TriggerCount { get; set; }
        public string WebhookUrl { get; set; } = string.Empty;
    }
}
