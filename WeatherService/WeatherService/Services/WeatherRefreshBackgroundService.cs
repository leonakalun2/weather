using WeatherService.Infrastructure.Interfaces;

namespace WeatherService.Api.Services
{
    public class WeatherRefreshBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scope;
        private readonly ILogger<WeatherRefreshBackgroundService> _logger;

        public WeatherRefreshBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<WeatherRefreshBackgroundService> logger)
        {
            _scope = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Weather refresh service started");

            // Stagger start so it doesn't run immediately at startup
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scope.CreateScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IWeatherRepository>();
                    var weatherSvc = scope.ServiceProvider.GetRequiredService<IWeatherService>();
                    var locations = await repo.GetAllLocationsAsync();

                    foreach (var location in locations)
                    {
                        if (stoppingToken.IsCancellationRequested) break;
                        await weatherSvc.RefreshWeatherDataAsync(location);
                        // Small delay between locations to avoid hitting rate limits
                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    }

                    _logger.LogInformation("Refreshed {Count} locations", locations.Count);
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in weather refresh cycle");
                }

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }
    }
}
