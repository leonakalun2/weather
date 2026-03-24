namespace WeatherService.Model.Dtos.Responses
{
    public class ForecastResponseDto
    {
        public string Location { get; set; } = string.Empty;
        public CoordinatesDto Coordinates { get; set; } = new();
        public IEnumerable<ForecastDayDto> Days { get; set; } = [];
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class ForecastWindDto
    {
        public double MinSpeedMs { get; set; }
        public double MaxSpeedMs { get; set; }
        public string DirectionText { get; set; } = string.Empty;
    }
    public class WindDto
    {
        public double SpeedMs { get; set; }
        public double DirectionDeg { get; set; }
    }

    public class CoordinatesDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}