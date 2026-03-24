namespace WeatherService.Model.Dtos.Responses
{
    public class HistoricalResponseDto
    {
        public string Location { get; set; } = string.Empty;
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int RecordCount { get; set; }
        public IEnumerable<WeatherResponseDto> Records { get; set; } = [];
        public HistoricalStatsDto Stats { get; set; } = new();
    }

    public class HistoricalStatsDto
    {
        public double AvgTemperatureC { get; set; }
        public double MinTemperatureC { get; set; }
        public double MaxTemperatureC { get; set; }
        public double AvgHumidityPct { get; set; }
        public double AvgWindSpeedMs { get; set; }
        public double TotalRainfallMm { get; set; }
        public double AvgPm25 { get; set; }
        public double AvgPsi { get; set; }
        public double MaxUvIndex { get; set; }
    }
}
