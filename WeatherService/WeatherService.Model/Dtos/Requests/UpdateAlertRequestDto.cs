using WeatherService.Model.Enums;

namespace WeatherService.Model.Dtos.Requests
{
    public class UpdateAlertRequestDto
    {
        public double? ThresholdValue { get; set; }
        public ThresholdOperatorEnum? Operator { get; set; }
        public bool? IsActive { get; set; }
        public string? WebhookUrl { get; set; }
    }
}
