using WeatherService.Model.Entities;

namespace WeatherService.Infrastructure.Interfaces
{
    public interface IExportService
    {
        Task<byte[]> ExportWeatherToCsvAsync(IEnumerable<WeatherRecordEntity> records, string location);
    }
}
