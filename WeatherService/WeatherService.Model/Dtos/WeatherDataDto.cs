namespace WeatherService.Model.Dtos
{
    public class WeatherDataDto
    {
        public double TemperatureC { get; set; }
        public double HumidityPct { get; set; }
        public double RainfallMm { get; set; }
        public double WindSpeedKmh { get; set; }
        public double WindDirectionDeg { get; set; }
        //public string WindDirectionText { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
