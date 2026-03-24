using WeatherService.Model.Enums;

namespace WeatherService.Model.Entities
{
    public class WeatherRecordEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Location { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Temperature { get; set; } 
        public double Humidity { get; set; }
        public double RainfallMm { get; set; }
        public double WindSpeedMs { get; set; }
        public double WindDirectionDeg { get; set; }
        //public string WindDirectionText { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string ConditionDetail { get; set; } = string.Empty;
        public double Pm25 { get; set; }
        public double Pm10 { get; set; }
        public double Psi { get; set; }
        public string PsiCategory { get; set; } = string.Empty;
        public double Co { get; set; }
        public double No2 { get; set; }
        public double O3 { get; set; }
        public double So2 { get; set; }
        public double UvIndex { get; set; }
        public string UvCategory { get; set; } = string.Empty; 
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public RecordTypeEnum RecordType { get; set; } = RecordTypeEnum.Current;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
