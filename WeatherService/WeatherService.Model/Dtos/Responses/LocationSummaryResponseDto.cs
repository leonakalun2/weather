namespace WeatherService.Model.Dtos.Responses
{
    public class LocationSummaryResponseDto
    {
        public string Location { get; set; } = string.Empty;
        public CoordinatesDto Coordinates { get; set; } = new();
        public double TemperatureC { get; set; }
        public double HumidityPct { get; set; }
        public double RainfallMm { get; set; }
        public double Pm25 { get; set; }
        public double Psi { get; set; }
        public string PsiCategory { get; set; } = string.Empty;
        public double UvIndex { get; set; }
        public string UvCategory { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public int ActiveAlerts { get; set; }
    }
}
