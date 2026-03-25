using Microsoft.EntityFrameworkCore;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Infrastructure.Persistence;
using WeatherService.Model.Entities;

namespace WeatherService.Infrastructure.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly WeatherDbContext _dbContext;
        public AlertRepository(WeatherDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<WeatherAlertEntity> CreateAsync(WeatherAlertEntity alert)
        {
            _dbContext.WeatherAlerts.Add(alert);
            await _dbContext.SaveChangesAsync();
            return alert;
        }

        public async Task<WeatherAlertEntity?> GetByIdAsync(Guid id)
        {
            var ret = await _dbContext.WeatherAlerts.FindAsync(id);
            return ret;
        }

        public async Task<IList<WeatherAlertEntity>> GetActiveAlertsAsync()
        {
            var ret = await _dbContext.WeatherAlerts
                .Where(i => i.IsActive)
                .ToListAsync();

            return ret;
        }

        public async Task<IList<WeatherAlertEntity>> GetByEmailAsync(string email)
        { 
            var ret = await _dbContext.WeatherAlerts
                .Where(i => i.SubscriberEmail.ToLower() == email.ToLower())
                .ToListAsync();

            return ret;
        }

        public async Task UpdateAsync(WeatherAlertEntity alert)
        {
            _dbContext.WeatherAlerts.Update(alert);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var record = await _dbContext.WeatherAlerts.FindAsync(id);
            if (record != null)
            {
                _dbContext.WeatherAlerts.Remove(record);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
