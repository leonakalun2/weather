using Microsoft.Extensions.Caching.Memory;
using WeatherService.Infrastructure.ExternalProviders;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos;
using WeatherService.Model.Enums;

namespace WeatherService.Api.Services
{
    public class WeatherServices : IWeatherService
    {
        private readonly IWeatherRepository _weatherRepo;
        private readonly IForecastRepository _forecastRepo;
        private readonly IWeatherProvider _weather;
        private readonly IGeocodingProvider _geocoding;
        private readonly IExportService _export;
        private readonly IMemoryCache _cache;
        private readonly ILogger<WeatherServices> _logger;

        private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan _forecastTtl = TimeSpan.FromHours(1);
        private static readonly TimeSpan _staleThreshold = TimeSpan.FromMinutes(15);

        public WeatherServices(IWeatherRepository weatherRepo, IForecastRepository forecastRepo, IWeatherProvider weather, IGeocodingProvider geocoding,
            IExportService export, IMemoryCache cache, ILogger<WeatherServices> logger)
        {
            _weatherRepo = weatherRepo;
            _forecastRepo = forecastRepo;
            _weather = weather;
            _geocoding = geocoding;
            _export = export;
            _cache = cache;
            _logger = logger;
        }

        public async Task<WeatherRecordEntity?> GetCurrentWeatherAsync(string location)
        {
            var cacheKey = $"current:{location.ToLower()}";
            if (_cache.TryGetValue(cacheKey, out WeatherRecordEntity? weatherCache))
                return weatherCache;

            var geo = await _geocoding.GeocodeAsync(location);
            if (geo == null)
            {
                _logger.LogWarning($"Geocoding failed for location: {location}");
                return null;
            }

            var locationName = geo.Value.FormattedName; // e.g. "Singapore, SG"

            var existing = await _weatherRepo.GetLatestByLocationAsync(locationName);
            if (existing != null && DateTime.UtcNow - existing.Timestamp  < _staleThreshold)
            {
                _cache.Set(cacheKey, existing, _cacheTtl);
                return existing;
            }

            await FetchAndSaveAsync(locationName, geo.Value.Lat, geo.Value.Lon);

            var freshData = await _weatherRepo.GetLatestByLocationAsync(locationName);
            if (freshData != null)
            {
                _cache.Set(cacheKey, freshData, _cacheTtl);
            }
            return freshData;
        }

        public async Task<IList<ForecastRecordEntity>> GetForecastAsync(string location, int days = 4)
        {
            var cacheKey = $"forecast:{location.ToLower()}:{days}";
            if (_cache.TryGetValue(cacheKey, out List<ForecastRecordEntity>? cached))
                return cached!;

            // Geocode first — same pattern as GetCurrentWeatherAsync
            var geo = await _geocoding.GeocodeAsync(location);
            if (geo == null)
            {
                _logger.LogWarning($"Geocoding failed for location: {location}");
                return null;
            }

            var locationName = geo.Value.FormattedName; // e.g. "Singapore, SG"
            var existing = await _forecastRepo.GetForecastAsync(locationName, days);
            if (existing != null && existing.Count > 0 && DateTime.UtcNow - existing[0].CreatedAt > TimeSpan.FromHours(6))
            {
                _cache.Set(cacheKey, existing, _forecastTtl);
                return existing;
            }

            var fourDaysRecords = await _weather.GetFourDayForecastAsync();
            if (fourDaysRecords == null || fourDaysRecords.Count == 0)
                return null;

            await _forecastRepo.DeleteOldForecastsAsync(locationName);

            var records = fourDaysRecords.Take(days).Select(d => new ForecastRecordEntity
            {
                Location = locationName,
                Latitude = geo.Value.Lat,
                Longitude = geo.Value.Lon,
                ForecastDate = d.Date,
                TempMin = d.TempMin,
                TempMax = d.TempMax,
                HumidityMin = d.HumidityMin,
                HumidityMax = d.HumidityMax,
                WindSpeedMinMs = Math.Round(d.Wind.MinSpeedMs * 1000 / 3600, 2),
                WindSpeedMaxMs = Math.Round(d.Wind.MaxSpeedMs * 1000 / 3600, 2),
                WindDirectionText = d.Wind.DirectionText,
                ForecastText = d.ForecastText,
                Condition = d.Condition,
                PrecipitationChancePct = d.PrecipitationChancePct
            }).ToList();

            await _forecastRepo.SaveForecastAsync(records);
            _cache.Set(cacheKey, records, _forecastTtl);
            return records;
        }

        public async Task<IList<WeatherRecordEntity>> GetHistoricalAsync(string location, DateTime from, DateTime to)
        {
            if (to - from > TimeSpan.FromDays(365))
                throw new ArgumentException("Date range cannot exceed 365 days");

            var geo = await _geocoding.GeocodeAsync(location);
            var locationName = geo?.FormattedName ?? location;

            return await _weatherRepo.GetHistoricalAsync(locationName, from, to);
        }

        public async Task<byte[]> ExportToCsvAsync(string location, DateTime from, DateTime to)
        {
            var geo = await _geocoding.GeocodeAsync(location);
            var locationName = geo?.FormattedName ?? location;

            var records = await _weatherRepo.GetHistoricalAsync(locationName, from, to);
            return await _export.ExportWeatherToCsvAsync(records, locationName);
        }

        public async Task RefreshWeatherDataAsync(string location)
        {
            var geo = await _geocoding.GeocodeAsync(location);
            if (geo == null)
            {
                _logger.LogWarning($"Geocoding failed for Location: {location}");
                return;
            }
            await FetchAndSaveAsync(geo.Value.FormattedName, geo.Value.Lat, geo.Value.Lon);
        }


        #region Private methods
        private async Task FetchAndSaveAsync(string locationName, double lat, double lon)
        {
            // All three NEA calls in parallel
            var condTask = _weather.GetCurrentConditionsAsync();
            var aqTask = _weather.GetAirQualityAsync();
            var uvTask = _weather.GetUvIndexAsync();
            await Task.WhenAll(condTask, aqTask, uvTask);

            var conditions = await condTask;
            if (conditions is null)
            {
                _logger.LogWarning($"NEA returned no conditions data for Location: {locationName}");
                return;
            }

            var aq = await aqTask;
            var uv = await uvTask;

            var record = new WeatherRecordEntity
            {
                Location = locationName,
                Latitude = lat,
                Longitude = lon,
                Temperature = conditions.TemperatureC,
                Humidity = conditions.HumidityPct,
                RainfallMm = conditions.RainfallMm,
                WindSpeedMs = Math.Round(conditions.WindSpeedKmh * 1000 / 3600, 2),
                WindDirectionDeg = conditions.WindDirectionDeg,
                //WindDirectionText = conditions.WindDirectionText,
                Condition = DataGovSgProvider.DeriveConditionFromCurrentReadings(
                                        conditions.RainfallMm, conditions.HumidityPct),
                ConditionDetail = DataGovSgProvider.DeriveConditionDetailFromCurrentReadings(
                                        conditions.RainfallMm, conditions.HumidityPct),
                Pm25 = aq?.Pm25 ?? 0,
                Pm10 = aq?.Pm10 ?? 0,
                Psi = aq?.Psi ?? 0,
                PsiCategory = aq?.PsiCategory ?? "Unknown",
                Co = aq?.Co ?? 0,
                No2 = aq?.No2 ?? 0,
                O3 = aq?.O3 ?? 0,
                So2 = aq?.So2 ?? 0,
                UvIndex = uv?.UvIndex ?? 0,
                UvCategory = uv?.UvCategory ?? "N/A",
                Timestamp = conditions.Timestamp,
                RecordType = RecordTypeEnum.Current
            };

            await _weatherRepo.SaveAsync(record);
            _logger.LogInformation($"Saved weather record for Location: {locationName}");
        }
        #endregion
    }
}
