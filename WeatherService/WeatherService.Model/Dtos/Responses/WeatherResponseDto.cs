namespace WeatherService.Model.Dtos.Responses
{
    public class WeatherResponseDto
    {
        public string Location { get; set; } = string.Empty;
        public CoordinatesDto Coordinates { get; set; } = new();
        public double TemperatureC { get; set; }
        public double HumidityPct { get; set; }

        public double RainfallMm { get; set; }

        public WindDto Wind { get; set; } = new();

        public string Condition { get; set; } = string.Empty;
        public string ConditionDetail { get; set; } = string.Empty;
        public AirQualityDto AirQuality { get; set; } = new();
        public UvDataDto Uv { get; set; } = new();
        public DateTime Timestamp { get; set; }
    }

    public class AirQualityDto
    {
        public double Pm25 { get; set; }
        public double Pm10 { get; set; }
        public double Psi { get; set; }
        public string PsiCategory { get; set; } = string.Empty;
        public double Co { get; set; }
        public double No2 { get; set; }
        public double O3 { get; set; }
        public double So2 { get; set; }
    }
}
