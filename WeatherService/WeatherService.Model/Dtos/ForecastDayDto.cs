using WeatherService.Model.Dtos.Responses;

namespace WeatherService.Model.Dtos
{
    public class ForecastDayDto
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public double HumidityMin { get; set; }
        public double HumidityMax { get; set; }
        public ForecastWindDto Wind { get; set; } = new();
        public string ForecastText { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public double PrecipitationChancePct { get; set; }
    }
}
