using WeatherService.Model.Dtos;
using WeatherService.Model.Dtos.Responses;
using WeatherService.Model.Enums;

namespace WeatherService.Api.Helpers
{
    public static class ResponseMapperHelper
    {
        public static WeatherResponseDto ToWeatherResponse(WeatherRecordEntity r)
        {
            return new WeatherResponseDto
            {
                Location = r.Location,
                Coordinates = new CoordinatesDto { Latitude = r.Latitude, Longitude = r.Longitude },
                TemperatureC = r.Temperature,
                HumidityPct = r.Humidity,
                RainfallMm = r.RainfallMm,
                Wind = new WindDto
                {
                    SpeedMs = r.WindSpeedMs,
                    DirectionDeg = r.WindDirectionDeg
                },
                Condition = r.Condition,
                ConditionDetail = r.ConditionDetail,
                AirQuality = new AirQualityDto
                {
                    Pm25 = r.Pm25,
                    Pm10 = r.Pm10,
                    Psi = r.Psi,
                    PsiCategory = r.PsiCategory,
                    Co = r.Co,
                    No2 = r.No2,
                    O3 = r.O3,
                    So2 = r.So2
                },
                Uv = new UvDataDto { UvIndex = r.UvIndex, UvCategory = r.UvCategory },
                Timestamp = r.Timestamp
            };
        }

        public static ForecastResponseDto ToForecastResponse(IList<ForecastRecordEntity> records, string location) 
        {
            return new ForecastResponseDto
            {
                Location = records.Any() ? records[0].Location : location,
                Coordinates = records.Any()
                ? new CoordinatesDto
                {
                    Latitude = records[0].Latitude,
                    Longitude = records[0].Longitude
                }
                : new CoordinatesDto(),
                Days = records.Select(r => new ForecastDayDto
                {
                    Date = r.ForecastDate,
                    DayOfWeek = r.ForecastDate.DayOfWeek.ToString(),
                    TempMin = r.TempMin,
                    TempMax = r.TempMax,
                    HumidityMin = r.HumidityMin,
                    HumidityMax = r.HumidityMax,
                    Wind = new ForecastWindDto
                    {
                        MinSpeedMs = r.WindSpeedMinMs,
                        MaxSpeedMs = r.WindSpeedMaxMs,
                        DirectionText = r.WindDirectionText
                    },
                    ForecastText = r.ForecastText,
                    Condition = r.Condition,
                    PrecipitationChancePct = r.PrecipitationChancePct
                })
            };
        }

        public static HistoricalResponseDto ToHistoricalResponse(IList<WeatherRecordEntity> records, string location, DateTime from, DateTime to)
        {
            return new HistoricalResponseDto
            {
                Location = location,
                From = from,
                To = to,
                RecordCount = records.Count,
                Records = records.Select(ToWeatherResponse),
                Stats = records.Count > 0 ? new HistoricalStatsDto
                {
                    AvgTemperatureC = Math.Round(records.Average(r => r.Temperature), 2),
                    MinTemperatureC = records.Min(r => r.Temperature),
                    MaxTemperatureC = records.Max(r => r.Temperature),
                    AvgHumidityPct = Math.Round(records.Average(r => r.Humidity), 2),
                    AvgWindSpeedMs = Math.Round(records.Average(r => r.WindSpeedMs), 2),
                    TotalRainfallMm = Math.Round(records.Sum(r => r.RainfallMm), 2),
                    AvgPm25 = Math.Round(records.Average(r => r.Pm25), 2),
                    AvgPsi = Math.Round(records.Average(r => r.Psi), 2),
                    MaxUvIndex = records.Max(r => r.UvIndex)
                } : new HistoricalStatsDto()
            };
        }

        public static AlertResponseDto ToAlertResponse(WeatherAlertEntity alert)
        {
            var unit = alert.ConditionType switch
            {
                ConditionTypeEnum.Temperature => "°C",
                ConditionTypeEnum.Humidity => "%",
                ConditionTypeEnum.RainfallMm => "mm",
                ConditionTypeEnum.WindSpeedMs => "m/s",
                ConditionTypeEnum.Pm25 => "µg/m³",
                ConditionTypeEnum.Psi => "",
                ConditionTypeEnum.UvIndex => "",
                _ => ""
            };

            return new AlertResponseDto
            {
                Id = alert.Id,
                SubscriberEmail = alert.SubscriberEmail,
                SubscriberName = alert.SubscriberName,
                Location = alert.Location,
                ConditionType = alert.ConditionType.ToString(),
                ConditionUnit = unit,
                ThresholdValue = alert.ThresholdValue,
                Operator = alert.Operator.ToString(),
                IsActive = alert.IsActive,
                WebhookUrl = alert.WebhookUrl,
                CreatedAt = alert.CreatedAt,
                LastTriggeredAt = alert.LastTriggeredAt,
                TriggerCount = alert.TriggerCount
            };
        }

        public static IList<AlertResponseDto> ToAlertListResponse(IList<WeatherAlertEntity> alerts)
        {
            if (alerts == null || alerts.Count == 0)
                return null;

            return alerts.Select(i => ToAlertResponse(i)).ToList();
        }

        public static LocationSummaryResponseDto ToLocationSummaryResponse(WeatherRecordEntity record, int locationAlerts)
        {
            return new LocationSummaryResponseDto
            {
                Location = record.Location,
                Coordinates = new CoordinatesDto
                {
                    Latitude = record.Latitude,
                    Longitude = record.Longitude
                },
                TemperatureC = record.Temperature,
                HumidityPct = record.Humidity,
                RainfallMm = record.RainfallMm,
                Pm25 = record.Pm25,
                Psi = record.Psi,
                PsiCategory = record.PsiCategory,
                UvIndex = record.UvIndex,
                UvCategory = record.UvCategory,
                Condition = record.Condition,
                LastUpdated = record.Timestamp,
                ActiveAlerts = locationAlerts
            };
        }
    }
}
