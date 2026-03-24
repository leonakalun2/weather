namespace WeatherService.Model.Dtos
{
    public class AirQualityDataDto
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
