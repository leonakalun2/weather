using WeatherService.Model.Enums;

namespace WeatherService.Model.Dtos.Requests
{
    public class CreateAlertRequestDto
    {
        public string SubscriberEmail { get; set; } = string.Empty;
        public string SubscriberName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public ConditionTypeEnum ConditionType { get; set; }
        public double ThresholdValue { get; set; }
        public ThresholdOperatorEnum Operator { get; set; }
        public string WebhookUrl { get; set; } = string.Empty;
    }
}
