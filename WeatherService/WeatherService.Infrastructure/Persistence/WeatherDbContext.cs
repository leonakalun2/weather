using Microsoft.EntityFrameworkCore;
using WeatherService.Model.Dtos;

namespace WeatherService.Infrastructure.Persistence
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

        public DbSet<WeatherRecordEntity> WeatherRecords => Set<WeatherRecordEntity>();
        public DbSet<ForecastRecordEntity> ForecastRecords => Set<ForecastRecordEntity>();
        public DbSet<WeatherAlertEntity> WeatherAlerts => Set<WeatherAlertEntity>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            mb.Entity<WeatherRecordEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.Location, x.Timestamp });
                e.HasIndex(x => x.Timestamp);
                e.Property(x => x.Location).HasMaxLength(200).IsRequired();
                e.Property(x => x.Condition).HasMaxLength(100);
                e.Property(x => x.ConditionDetail).HasMaxLength(300);
                //e.Property(x => x.WindDirectionText).HasMaxLength(10);
                e.Property(x => x.PsiCategory).HasMaxLength(50);
                e.Property(x => x.UvCategory).HasMaxLength(50);
                e.Property(x => x.RecordType).HasConversion<string>();
            });

            mb.Entity<ForecastRecordEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.Location, x.ForecastDate });
                e.Property(x => x.Location).HasMaxLength(200).IsRequired();
                e.Property(x => x.ForecastText).HasMaxLength(200);
                e.Property(x => x.Condition).HasMaxLength(100);
                //e.Property(x => x.WindDirectionText).HasMaxLength(10);
            });

            mb.Entity<WeatherAlertEntity>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.SubscriberEmail);
                e.HasIndex(x => x.IsActive);
                e.Property(x => x.SubscriberEmail).HasMaxLength(256).IsRequired();
                e.Property(x => x.SubscriberName).HasMaxLength(200);
                e.Property(x => x.Location).HasMaxLength(200).IsRequired();
                e.Property(x => x.WebhookUrl).HasMaxLength(2000);
                e.Property(x => x.ConditionType).HasConversion<string>();
                e.Property(x => x.Operator).HasConversion<string>();
            });

            base.OnModelCreating(mb);
        }
    }
}
