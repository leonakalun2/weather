using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WeatherService.Api.Services;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos;
using WeatherService.Model.Entities;
using WeatherService.Model.Enums;
using WeatherService.Test.Helpers;

namespace WeatherService.Test
{
    public class WeatherServiceTest : IClassFixture<WeatherApiFactory>
    {
        private readonly Mock<IWeatherRepository> _weatherRepo = new();
        private readonly Mock<IForecastRepository> _forecastRepo = new();
        private readonly Mock<IWeatherProvider> _provider = new();
        private readonly Mock<IGeocodingProvider> _geocoding = new();
        private readonly Mock<IExportService> _export = new();

        private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        private WeatherServices CreateSut() => new(
            _weatherRepo.Object,
            _forecastRepo.Object,
            _provider.Object,
            _geocoding.Object,
            _export.Object,
            _cache,
            NullLogger<WeatherServices>.Instance);

        [Fact]
        public async Task GetCurrentWeather_ReturnFreshRecord_WhenDataIsRecent()
        {
            //arrange
            _geocoding.Setup(g => g.GeocodeAsync("singapore"))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            var record = MappingTestHelper.MakeRecord("Singapore, SG", DateTime.UtcNow.AddMinutes(-5));
            _weatherRepo.Setup(r => r.GetLatestByLocationAsync("Singapore, SG"))
                .ReturnsAsync(record);

            //act
            var result = await CreateSut().GetCurrentWeatherAsync("singapore");

            //asset
            result.Should().NotBeNull();
            result!.Location.Should().Be("Singapore, SG");
        }

        [Fact]
        public async Task GetCurrentWeather_SecondCall_UseMemoryCache()
        {
            //arrange
            _geocoding.Setup(g => g.GeocodeAsync("singapore"))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            var record = MappingTestHelper.MakeRecord("Singapore, SG", DateTime.UtcNow.AddMinutes(-5));
            _weatherRepo.Setup(r => r.GetLatestByLocationAsync("Singapore, SG"))
                .ReturnsAsync(record);

            var sut = CreateSut();

            //act - call twice
            await sut.GetCurrentWeatherAsync("singapore");
            await sut.GetCurrentWeatherAsync("singapore");

            //assert
            _geocoding.Verify(g => g.GeocodeAsync("singapore"), Times.Once);
            _weatherRepo.Verify(r => r.GetLatestByLocationAsync("Singapore, SG"), Times.Once);
        }

        [Fact]
        public async Task GetCurrentWeather_CallsProvider_WhenDbDataIsStale()
        {
            // Arrange — stale data (20 min old, above the 15 min threshold)
            _geocoding
                .Setup(g => g.GeocodeAsync("singapore"))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            var stale = MappingTestHelper.MakeRecord("Singapore, SG", DateTime.UtcNow.AddMinutes(-20));
            var fresh = MappingTestHelper.MakeRecord("Singapore, SG", DateTime.UtcNow);

            // First call returns stale, second call (after refresh) returns fresh
            _weatherRepo
                .SetupSequence(r => r.GetLatestByLocationAsync("Singapore, SG"))
                .ReturnsAsync(stale)
                .ReturnsAsync(fresh);

            _provider
                .Setup(p => p.GetCurrentConditionsAsync())
                .ReturnsAsync(MappingTestHelper.MakeCurrentWeatherData());
            _provider
                .Setup(p => p.GetAirQualityAsync())
                .ReturnsAsync(MappingTestHelper.MakeAirQuality());
            _provider
                .Setup(p => p.GetUvIndexAsync())
                .ReturnsAsync(new UvDataDto { UvIndex = 6, UvCategory = "High" });

            _weatherRepo
                .Setup(r => r.SaveAsync(It.IsAny<WeatherRecordEntity>()))
                .ReturnsAsync((WeatherRecordEntity r) => r);

            // Act
            var result = await CreateSut().GetCurrentWeatherAsync("singapore");

            // Assert — provider was called to get fresh data
            result.Should().NotBeNull();
            _provider.Verify(p => p.GetCurrentConditionsAsync(), Times.Once);
            _provider.Verify(p => p.GetAirQualityAsync(), Times.Once);
            _provider.Verify(p => p.GetUvIndexAsync(), Times.Once);
        }

        [Fact]
        public async Task GetCurrentWeather_ReturnsNull_WhenGeocodingFails()
        {
            // Arrange — geocoder returns null (unknown location)
            _geocoding
                .Setup(g => g.GeocodeAsync(It.IsAny<string>()))
                .ReturnsAsync((ValueTuple<double, double, string>?)null);

            // Act
            var result = await CreateSut().GetCurrentWeatherAsync("unknownplace");

            // Assert
            result.Should().BeNull();
            _provider.Verify(p => p.GetCurrentConditionsAsync(), Times.Never);
        }

        [Fact]
        public async Task GetCurrentWeather_ReturnsNull_WhenProviderFails()
        {
            // Arrange — geocoding works but NEA API returns null
            _geocoding
                .Setup(g => g.GeocodeAsync("singapore"))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            _weatherRepo
                .Setup(r => r.GetLatestByLocationAsync("Singapore, SG"))
                .ReturnsAsync((WeatherRecordEntity?)null);

            _provider
                .Setup(p => p.GetCurrentConditionsAsync())
                .ReturnsAsync((WeatherDataDto?)null);

            // Act
            var result = await CreateSut().GetCurrentWeatherAsync("singapore");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RefreshWeatherData_SavesRecordWithCorrectFields()
        {
            // Arrange
            _geocoding
                .Setup(g => g.GeocodeAsync("Singapore"))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            var conditions = MappingTestHelper.MakeCurrentWeatherData();
            _provider.Setup(p => p.GetCurrentConditionsAsync()).ReturnsAsync(conditions);
            _provider.Setup(p => p.GetAirQualityAsync()).ReturnsAsync(MappingTestHelper.MakeAirQuality());
            _provider.Setup(p => p.GetUvIndexAsync())
                .ReturnsAsync(new UvDataDto { UvIndex = 7.2, UvCategory = "High" });

            WeatherRecordEntity? saved = null;
            _weatherRepo
                .Setup(r => r.SaveAsync(It.IsAny<WeatherRecordEntity>()))
                .Callback<WeatherRecordEntity>(r => saved = r)
                .ReturnsAsync((WeatherRecordEntity r) => r);

            // Act
            await CreateSut().RefreshWeatherDataAsync("Singapore");

            // Assert — every field on the saved record comes from NEA
            saved.Should().NotBeNull();
            saved!.Location.Should().Be("Singapore, SG");      // canonical name
            saved.Temperature.Should().Be(conditions.TemperatureC);
            saved.Humidity.Should().Be(conditions.HumidityPct);
            saved.RainfallMm.Should().Be(conditions.RainfallMm);
            // Wind speed is converted from km/h → m/s
            saved.WindSpeedMs.Should().BeApproximately(
                Math.Round(conditions.WindSpeedKmh * 1000 / 3600, 2), 0.01);
            saved.UvCategory.Should().Be("High");
            saved.RecordType.Should().Be(RecordTypeEnum.Current);
        }

        // ── GetHistoricalAsync ────────────────────────────────────────────────────

        [Fact]
        public async Task GetHistorical_ThrowsArgumentException_WhenRangeExceeds365Days()
        {
            // Geocoder must succeed for this test
            _geocoding
                .Setup(g => g.GeocodeAsync(It.IsAny<string>()))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            await CreateSut()
                .Invoking(s => s.GetHistoricalAsync(
                    "Singapore",
                    DateTime.UtcNow.AddDays(-400),
                    DateTime.UtcNow))
                .Should()
                .ThrowAsync<ArgumentException>()
                .WithMessage("*365 days*");
        }

        [Fact]
        public async Task GetHistorical_QueriesDbWithLocationName()
        {
            // Arrange — user passes "singapore", DB stores "Singapore, SG"
            _geocoding
                .Setup(g => g.GeocodeAsync("singapore"))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            var from = DateTime.UtcNow.AddDays(-7);
            var to = DateTime.UtcNow;

            _weatherRepo
                .Setup(r => r.GetHistoricalAsync("Singapore, SG", from, to))
                .ReturnsAsync(new List<WeatherRecordEntity>
                {
                MappingTestHelper.MakeRecord("Singapore, SG", from.AddDays(1)),
                MappingTestHelper.MakeRecord("Singapore, SG", from.AddDays(3))
                });

            // Act
            var result = (await CreateSut().GetHistoricalAsync("singapore", from, to)).ToList();

            // Assert — two records returned using canonical name
            result.Should().HaveCount(2);
            // Verify the DB was queried with "Singapore, SG" not "singapore"
            _weatherRepo.Verify(
                r => r.GetHistoricalAsync("Singapore, SG", from, to),
                Times.Once);
        }

        // ── ExportToCsvAsync ──────────────────────────────────────────────────────

        [Fact]
        public async Task ExportToCsv_UsesLocationNameAndDelegatesToExportService()
        {
            // Arrange
            _geocoding
                .Setup(g => g.GeocodeAsync("singapore"))
                .ReturnsAsync((1.3521, 103.8198, "Singapore, SG"));

            var from = DateTime.UtcNow.AddDays(-3);
            var to = DateTime.UtcNow;
            var records = new List<WeatherRecordEntity> { MappingTestHelper.MakeRecord("Singapore, SG", from.AddDays(1)) };
            var csvData = "Timestamp,Location\n2024-01-01,Singapore, SG\n"u8.ToArray();

            _weatherRepo
                .Setup(r => r.GetHistoricalAsync("Singapore, SG", from, to))
                .ReturnsAsync(records);
            _export
                .Setup(e => e.ExportWeatherToCsvAsync(records, "Singapore, SG"))
                .ReturnsAsync(csvData);

            // Act
            var result = await CreateSut().ExportToCsvAsync("singapore", from, to);

            // Assert
            result.Should().BeEquivalentTo(csvData);
            _export.Verify(
                e => e.ExportWeatherToCsvAsync(records, "Singapore, SG"),
                Times.Once);
        }
    }
}