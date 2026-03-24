using WeatherService.Model.Dtos;
using WeatherService.Model.Enums;

namespace WeatherService.Test.Helpers
{
    public class MappingTestHelper
    {
        public static WeatherRecordEntity MakeRecord(string location, DateTime ts) => new()
        {
            Location = "Singapore, SG",
            Latitude = 1.3521,
            Longitude = 103.8198,
            Temperature = 28.5,
            Humidity = 80.0,
            RainfallMm = 0.2,
            WindSpeedMs = 2.8,
            WindDirectionDeg = 180.0,
            Condition = "Cloudy",
            ConditionDetail = "partly cloudy",
            Pm25 = 12.5,
            Pm10 = 20.0,
            Psi = 45.0,
            PsiCategory = "Good",
            Co = 200.0,
            No2 = 10.0,
            O3 = 50.0,
            So2 = 5.0,
            UvIndex = 6.0,
            UvCategory = "High",
            Timestamp = ts,
            RecordType = RecordTypeEnum.Current
        };

        public static WeatherDataDto MakeCurrentWeatherData() => new()
        {
            TemperatureC = 28.5,
            HumidityPct = 80.0,
            RainfallMm = 0.2,
            WindSpeedKmh = 10.0,    
            WindDirectionDeg = 180.0,
            Timestamp = DateTime.UtcNow
        };

        public static AirQualityDataDto MakeAirQuality() => new()
        {
            Pm25 = 12.5,
            Pm10 = 20.0,
            Psi = 45.0,
            PsiCategory = "Good",
            Co = 200.0,
            No2 = 10.0,
            O3 = 50.0,
            So2 = 5.0
        };
    }
}
