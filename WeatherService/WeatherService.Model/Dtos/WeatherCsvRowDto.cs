namespace WeatherService.Model.Dtos
{
    public class WeatherCsvRow
    {
        public string Timestamp { get; set; } = "";
        public string Location { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Temperature_C { get; set; }
        public double Humidity_Pct { get; set; }
        public double Rainfall_mm { get; set; }
        public double WindSpeed_ms { get; set; }
        public double WindDirection_deg { get; set; }
        public string WindDirection_text { get; set; } = "";
        public string Condition { get; set; } = "";
        public string ConditionDetail { get; set; } = "";
        public double PM25_ugm3 { get; set; }
        public double PM10_ugm3 { get; set; }
        public double PSI { get; set; }
        public string PSI_Category { get; set; } = "";
        public double CO_ugm3 { get; set; }
        public double NO2_ugm3 { get; set; }
        public double O3_ugm3 { get; set; }
        public double SO2_ugm3 { get; set; }
        public double UV_Index { get; set; }
        public string UV_Category { get; set; } = "";
    }
}
