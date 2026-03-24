using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Infrastructure.Persistence;
using WeatherService.Model.Dtos;
using WeatherService.Model.Dtos.Responses;
using WeatherService.Model.Enums;

namespace WeatherService.Test
{
    // WebApplicationFactory spins up a real in-memory version of your API
    // It's like running dotnet run but entirely in memory — no port, no network
    public class WeatherApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Replace SQLite with an isolated in-memory DB (fresh per test run)
                services.RemoveAll<DbContextOptions<WeatherDbContext>>();
                services.AddDbContext<WeatherDbContext>(o =>
                    o.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                // Replace real data.gov.sg HTTP client with a stub
                services.RemoveAll<IWeatherProvider>();
                services.AddSingleton<IWeatherProvider, StubWeatherProvider>();

                // Replace real Nominatim geocoding with a stub
                services.RemoveAll<IGeocodingProvider>();
                services.AddSingleton<IGeocodingProvider, StubGeocodingService>();

                // Remove background services — they would interfere with test isolation
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();

                // Seed known data so historical / location tests have something to query
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
                db.Database.EnsureCreated();
                SeedTestData(db);
            });

            builder.UseEnvironment("Development");
        }

        private static void SeedTestData(WeatherDbContext db)
        {
            db.WeatherRecords.AddRange(
                // Recent record — served straight from DB without calling the stub
                new WeatherRecordEntity
                {
                    Location = "Singapore, SG",
                    Latitude = 1.3521,
                    Longitude = 103.8198,
                    Temperature = 28.5,
                    Humidity = 80.0,
                    RainfallMm = 0.2,
                    WindSpeedMs = 2.78,
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
                    Timestamp = DateTime.UtcNow.AddMinutes(-5),
                    RecordType = RecordTypeEnum.Current
                },
                // Older record — for historical queries
                new WeatherRecordEntity
                {
                    Location = "Singapore, SG",
                    Latitude = 1.3521,
                    Longitude = 103.8198,
                    Temperature = 26.0,
                    Humidity = 88.0,
                    RainfallMm = 4.5,
                    WindSpeedMs = 1.5,
                    WindDirectionDeg = 200.0,
                    Condition = "Rain",
                    ConditionDetail = "moderate rain",
                    Pm25 = 18.0,
                    Pm10 = 30.0,
                    Psi = 72.0,
                    PsiCategory = "Moderate",
                    Co = 210.0,
                    No2 = 12.0,
                    O3 = 55.0,
                    So2 = 6.0,
                    UvIndex = 0.0,
                    UvCategory = "N/A",
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    RecordType = RecordTypeEnum.Current
                }
            );
            db.SaveChanges();
        }
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────────

    public class StubWeatherProvider : IWeatherProvider
    {
        public Task<WeatherDataDto?> GetCurrentConditionsAsync() =>
            Task.FromResult<WeatherDataDto?>(new WeatherDataDto
            {
                TemperatureC = 28.5,
                HumidityPct = 80.0,
                RainfallMm = 0.2,
                WindSpeedKmh = 10.0,
                WindDirectionDeg = 180.0,
                Timestamp = DateTime.UtcNow
            });

        public Task<AirQualityDataDto?> GetAirQualityAsync() =>
            Task.FromResult<AirQualityDataDto?>(new AirQualityDataDto
            {
                Pm25 = 12.5,
                Pm10 = 20.0,
                Psi = 45.0,
                PsiCategory = "Good",
                Co = 200.0,
                No2 = 10.0,
                O3 = 50.0,
                So2 = 5.0
            });

        public Task<UvDataDto?> GetUvIndexAsync() =>
            Task.FromResult<UvDataDto?>(new UvDataDto
            {
                UvIndex = 6.0,
                UvCategory = "High"
            });

        public Task<IList<ForecastDayDto>> GetFourDayForecastAsync() =>
            Task.FromResult<IList<ForecastDayDto>>(
            Enumerable.Range(0, 4).Select(i => new ForecastDayDto
            {
                Date = DateTime.UtcNow.AddDays(i + 1).Date,
                TempMin = 25.0,
                TempMax = 33.0,
                HumidityMin = 65.0,
                HumidityMax = 90.0,
                Wind = new ForecastWindDto
                {
                    MaxSpeedMs = 10.0,
                    MinSpeedMs = 5.0,
                },
                ForecastText = "Partly cloudy with afternoon showers",
                Condition = "Partly Cloudy",
                PrecipitationChancePct = 40.0
            }).ToList());
    }

    public class StubGeocodingService : IGeocodingProvider
    {
        public Task<(double Lat, double Lon, string FormattedName)?> GeocodeAsync(
            string location) =>
            Task.FromResult<(double, double, string)?>(
                location.ToLower() switch
                {
                    "singapore" or "singapore, sg" => (1.3521, 103.8198, "Singapore, SG"),
                    "tampines" => (1.3537, 103.9432, "Tampines, SG"),
                    "jurong" => (1.3329, 103.7436, "Jurong, SG"),
                    "changi" => (1.3644, 103.9915, "Changi, SG"),
                    _ => (1.3521, 103.8198, "Singapore, SG")
                });

        public Task<string?> ReverseGeocodeAsync(double lat, double lon) =>
            Task.FromResult<string?>("Singapore, SG");
    }
}