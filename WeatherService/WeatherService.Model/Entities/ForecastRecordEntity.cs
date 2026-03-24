namespace WeatherService.Model.Entities
{
    public class ForecastRecordEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime ForecastDate { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public double HumidityMin { get; set; }
        public double HumidityMax { get; set; }
        public double WindSpeedMinMs { get; set; }
        public double WindSpeedMaxMs { get; set; }
        public string WindDirectionText { get; set; } = string.Empty;
        public string ForecastText { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public double PrecipitationChancePct { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
