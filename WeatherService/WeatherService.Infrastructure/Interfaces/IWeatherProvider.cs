using WeatherService.Model.Dtos;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface IWeatherProvider
    {
        Task<WeatherDataDto?> GetCurrentConditionsAsync();
        Task<AirQualityDataDto?> GetAirQualityAsync();
        Task<UvDataDto?> GetUvIndexAsync();
        Task<IList<ForecastDayDto>> GetFourDayForecastAsync();
    }
}
