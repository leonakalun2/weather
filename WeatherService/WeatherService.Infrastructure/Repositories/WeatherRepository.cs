using Microsoft.EntityFrameworkCore;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Infrastructure.Persistence;
using WeatherService.Model.Dtos;
using WeatherService.Model.Enums;

namespace WeatherService.Infrastructure.Repositories
{
    public class WeatherRepository : IWeatherRepository
    {
        private readonly WeatherDbContext _dbContext;

        public WeatherRepository(WeatherDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<WeatherRecordEntity?> GetLatestByLocationAsync(string location)
        {
            var ret = await _dbContext.WeatherRecords
                    .Where(r => r.Location == location && r.RecordType == RecordTypeEnum.Current)
                    .OrderByDescending(r => r.Timestamp)
                    .FirstOrDefaultAsync();

            return ret;
        }

        public async Task<IList<WeatherRecordEntity>> GetHistoricalAsync(string location, DateTime from, DateTime to)
        {
            var ret = await _dbContext.WeatherRecords
                    .Where(i => i.Location == location && i.Timestamp >= from && i.Timestamp <= to)
                    .OrderByDescending(i => i.Timestamp)
                    .ToListAsync();

            return ret;
        }

        public async Task<WeatherRecordEntity> SaveAsync(WeatherRecordEntity record)
        {
            _dbContext.WeatherRecords.Add(record);
            await _dbContext.SaveChangesAsync();
            return record;
        }

        public async Task<IList<String>> GetAllLocationsAsync()
        {
            var ret = await _dbContext.WeatherRecords
                    .Select(i => i.Location)
                    .Distinct()
                    .ToListAsync();

            return ret;
        }
    }
}
