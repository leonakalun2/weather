using Microsoft.EntityFrameworkCore;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Infrastructure.Persistence;
using WeatherService.Model.Dtos;

namespace WeatherService.Infrastructure.Repositories
{
    public class ForecastRepository : IForecastRepository
    {
        public readonly WeatherDbContext _dbContext;

        public ForecastRepository(WeatherDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IList<ForecastRecordEntity>> GetForecastAsync(string location, int days = 4)
        {
            var ret = await _dbContext.ForecastRecords
                    .Where(i => i.Location == location && i.ForecastDate >= DateTime.UtcNow.Date)
                    .Take(days)
                    .ToListAsync();

            return ret;
        }

        public async Task SaveForecastAsync(IList<ForecastRecordEntity> records)
        {
            _dbContext.ForecastRecords.AddRange(records);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteOldForecastsAsync(string location)
        {
            var oldRecords = await _dbContext.ForecastRecords
                .Where(i => i.Location == location)
                .ToListAsync();

            _dbContext.ForecastRecords.RemoveRange(oldRecords);
            await _dbContext.SaveChangesAsync();
        }
    }
}
