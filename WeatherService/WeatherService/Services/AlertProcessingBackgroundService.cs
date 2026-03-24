using WeatherService.Infrastructure.Interfaces;

namespace WeatherService.Api.Services
{
    public class AlertProcessingBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scope;
        private readonly ILogger<AlertProcessingBackgroundService> _logger;

        public AlertProcessingBackgroundService(IServiceScopeFactory scope, ILogger<AlertProcessingBackgroundService> logger)
        {
            _scope = scope;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Alert processing service start");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scope.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<IAlertService>();
                    await service.ProcessAlertsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in alert processing");
                }
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
