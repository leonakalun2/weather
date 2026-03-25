using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos;
using WeatherService.Model.Dtos.Responses;
using static System.Net.WebRequestMethods;

namespace WeatherService.Infrastructure.ExternalProviders
{
    public class DataGovSgProvider : IWeatherProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DataGovSgProvider> _logger;
        private readonly string _apiKey;

        private const string _baseUrl = "https://api-open.data.gov.sg/v2/real-time/api";

        public DataGovSgProvider(HttpClient httpClient, IConfiguration config, ILogger<DataGovSgProvider> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["WeatherProviders:DataGovSg:ApiKey"];

            if (!string.IsNullOrEmpty(_apiKey))
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", _apiKey);
        }

        public async Task<WeatherDataDto?> GetCurrentConditionsAsync()
        {
            try
            {
                var tempTask = FetchDocAsync("air-temperature");
                var rhTask = FetchDocAsync("relative-humidity");
                var rainTask = FetchDocAsync("rainfall");
                var wsTask = FetchDocAsync("wind-speed");
                var wdTask = FetchDocAsync("wind-direction");

                await Task.WhenAll(tempTask, rhTask, rainTask, wsTask, wdTask);

                var tempDoc = await tempTask;
                if (tempDoc == null)
                {
                    _logger.LogWarning("No air-temperature return from Data Gov API");
                    return null;
                }

                var temperature = AverageStations(tempDoc);
                var humidity = AverageStations(await rhTask);
                var rainfall = AverageStations(await rainTask);
                var windKmh = AverageStations(await wsTask);
                var windDeg = AverageStations(await wdTask);

                return new WeatherDataDto
                {
                    TemperatureC = Math.Round(temperature, 1),
                    HumidityPct = Math.Round(humidity, 1),
                    RainfallMm = Math.Round(rainfall, 2),
                    WindSpeedKmh = Math.Round(windKmh, 1),
                    WindDirectionDeg = Math.Round(windDeg, 1),
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch current conditions from data.gov.sg");
                return null;
            }

        }

        public async Task<AirQualityDataDto?> GetAirQualityAsync()
        {
            var pm25Task = FetchStringAsync($"{_baseUrl}/pm25");
            var psiTask = FetchStringAsync($"{_baseUrl}/psi");
            await Task.WhenAll(pm25Task, psiTask);
            double pm25 = 0, pm10 = 0, co = 0, so2 = 0, no2 = 0, o3 = 0, psi = 0;

            var pm25Json = await pm25Task;
            if (pm25Json != null)
            {
                var doc = JsonDocument.Parse(pm25Json);
                pm25 = ParseNationalReading(doc.RootElement, "pm25_one_hourly");
            }

            var psiJson = await psiTask;
            if (psiJson != null)
            {
                var doc = JsonDocument.Parse(psiJson);
                pm10 = ParseNationalReading(doc.RootElement, "pm10_twenty_four_hourly");
                co = ParseNationalReading(doc.RootElement, "co_eight_hour_max");
                so2 = ParseNationalReading(doc.RootElement, "so2_twenty_four_hourly");
                no2 = ParseNationalReading(doc.RootElement, "no2_one_hour_max");
                o3 = ParseNationalReading(doc.RootElement, "o3_eight_hour_max");
                psi = ParseNationalReading(doc.RootElement, "psi_twenty_four_hourly");
            }

            return new AirQualityDataDto
            {
                Pm25 = Math.Round(pm25, 1),
                Pm10 = Math.Round(pm10, 1),
                Psi = Math.Round(psi, 1),
                PsiCategory = PsiToCategory(psi),
                Co = Math.Round(co, 1),
                No2 = Math.Round(no2, 1),
                O3 = Math.Round(o3, 1),
                So2 = Math.Round(so2, 1)
            };
        }

        public async Task<UvDataDto?> GetUvIndexAsync()
        {
            try
            {
                var json = await FetchStringAsync($"{_baseUrl}/uv-index");
                if (json is null) return new UvDataDto { UvIndex = 0, UvCategory = "N/A" };

                var root = JsonDocument.Parse(json).RootElement;
                var uv = ParseUvIndex(root);

                return new UvDataDto
                {
                    UvIndex = uv,
                    UvCategory = UvToCategory(uv)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch UV index from data.gov.sg");
                return null;
            }
        }

        public async Task<IList<ForecastDayDto>> GetFourDayForecastAsync()
        {
            try
            {
                var json = await FetchStringAsync($"{_baseUrl}/four-day-outlook");
                if (json is null) return [];

                return ParseFourDayForecast(JsonDocument.Parse(json).RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch 4-day forecast from data.gov.sg");
                return [];
            }
        }

        #region Helper methods
        public static string DeriveConditionFromCurrentReadings(double rainfallMm, double humidityPct) =>
            rainfallMm switch
        {
            > 5 => "Thunderstorm",
            > 1 => "Rain",
            > 0.2 => "Drizzle",
            _ when humidityPct > 90 => "Overcast",
            _ when humidityPct > 75 => "Cloudy",
            _ => "Fair"
        };

        public static string DeriveConditionDetailFromCurrentReadings(double rainfallMm, double humidityPct) =>
            rainfallMm switch
            {
                > 5 => "heavy thundery showers",
                > 1 => "moderate rain",
                > 0.2 => "light drizzle",
                _ when humidityPct > 90 => "overcast sky",
                _ when humidityPct > 75 => "partly cloudy",
                _ => "fair conditions"
            };
        #endregion

        #region Private methods
        private async Task<JsonDocument?> FetchDocAsync(string url)
        {
            var json = await AsyncHelper($"{_baseUrl}/{url}");
            return json != null ? JsonDocument.Parse(json) : null;
        }

        private async Task<string?> FetchStringAsync(string url)
        {
            try
            {
                var resp = await _httpClient.GetAsync(url);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP GET failed: {Url}", url);
                return null;
            }
        }

        private async Task<string?> AsyncHelper(string url)
        {
            try
            {
                var res = await _httpClient.GetAsync(url);
                res.EnsureSuccessStatusCode();
                return await res.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"HTTP Get Failed Url: {url}");
                return null;
            }
        }

        private static double AverageStations(JsonDocument? doc)
        {
            if (doc == null)
                return 0;

            try
            {
                var readings = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("readings");

                if (readings.ValueKind != JsonValueKind.Array
                    || readings.GetArrayLength() == 0)
                    return 0;

                var stations = readings[0].GetProperty("data");
                if (stations.ValueKind != JsonValueKind.Array
                    || stations.GetArrayLength() == 0)
                    return 0;

                var values = stations.EnumerateArray()
                            .Select(i => i.GetProperty("value").GetDouble())
                            .Where(b => b >= 0)
                            .ToList();

                return values.Count > 0 ? values.Average() : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static double ParseNationalReading(JsonElement root, string key)
        {
            try
            {
                var items = root.GetProperty("data").GetProperty("items");
                if (items.ValueKind != JsonValueKind.Array
                    || items.GetArrayLength() == 0) return 0;

                var readings = items[0].GetProperty("readings");
                if (!readings.TryGetProperty(key, out var r)) return 0;

                return r.TryGetProperty("national", out var n) ? n.GetDouble() : 0;
            }
            catch 
            { 
                return 0; 
            }
        }

        private static double ParseUvIndex(JsonElement root)
        {
            try
            {
                var records = root.GetProperty("data").GetProperty("records");
                if (records.ValueKind != JsonValueKind.Array
                    || records.GetArrayLength() == 0) return 0;

                var index = records[0].GetProperty("index");
                if (index.ValueKind != JsonValueKind.Array
                    || index.GetArrayLength() == 0) return 0;

                return index[0].GetProperty("value").GetDouble();
            }
            catch { return 0; }
        }

        private static IList<ForecastDayDto> ParseFourDayForecast(JsonElement root)
        {
            var ret = new List<ForecastDayDto>();
            try
            {
                var records = root.GetProperty("data").GetProperty("records");
                if (records.ValueKind != JsonValueKind.Array
                    || records.GetArrayLength() == 0) return [];

                var forecasts = records[0].GetProperty("forecasts");
                if (forecasts.ValueKind != JsonValueKind.Array) return [];

                ret = forecasts.EnumerateArray().Take(4).Select((day, i) =>
                {
                    var text = day.TryGetProperty("object", out var f)
                        ? f.GetString() ?? "" : "";

                    double tLow = 25, tHigh = 33;
                    double hLow = 65, hHigh = 90;
                    double wsLow = 0, wsHigh = 0;
                    string wdText = "";

                    if (day.TryGetProperty("temperature", out var t))
                    {
                        tLow = t.GetProperty("low").GetDouble();
                        tHigh = t.GetProperty("high").GetDouble();
                    }

                    if (day.TryGetProperty("relativeHumidity", out var rh))
                    {
                        hLow = rh.GetProperty("low").GetDouble();
                        hHigh = rh.GetProperty("high").GetDouble();
                    }

                    if (day.TryGetProperty("wind", out var w))
                    {
                        if (w.TryGetProperty("speed", out var spd))
                        {
                            wsLow = spd.GetProperty("low").GetDouble();
                            wsHigh = spd.GetProperty("high").GetDouble();
                        }
                        if (w.TryGetProperty("direction", out var dir))
                            wdText = dir.GetString() ?? "";
                    }

                    return new ForecastDayDto
                    {
                        Date = DateTime.UtcNow.AddDays(i + 1).Date,
                        TempMin = tLow,
                        TempMax = tHigh,
                        HumidityMin = hLow,
                        HumidityMax = hHigh,
                        Wind = new ForecastWindDto
                        {
                            MinSpeedMs = wsLow,
                            MaxSpeedMs = wsHigh,
                            DirectionText = wdText
                        },
                        ForecastText = text,
                        Condition = text,
                        PrecipitationChancePct = EstimatePrecipChance(text)
                    };
                }).ToList();

                return ret;
            }
            catch {
                return ret;
            }
        }

        private static string PsiToCategory(double psi) => psi switch
        {
            <= 50 => "Good",
            <= 100 => "Moderate",
            <= 200 => "Unhealthy",
            <= 300 => "Very Unhealthy",
            _ => "Hazardous"
        };

        private static string UvToCategory(double uv) => uv switch
        {
            < 3 => "Low",
            < 6 => "Moderate",
            < 8 => "High",
            < 11 => "Very High",
            _ => "Extreme"
        };

        private static double EstimatePrecipChance(string text)
        {
            var l = text.ToLower();
            if (l.Contains("thunder")) return 80;
            if (l.Contains("heavy")) return 90;
            if (l.Contains("shower")) return 70;
            if (l.Contains("rain")) return 60;
            if (l.Contains("drizzle")) return 40;
            if (l.Contains("cloudy")) return 20;
            return 5;
        }


        #endregion
    }
}
