using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos;
using WeatherService.Model.Entities;

namespace WeatherService.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        public async Task<byte[]> ExportWeatherToCsvAsync(IEnumerable<WeatherRecordEntity> records, string location)
        {
            using var ms = new MemoryStream();
            await using var writer = new StreamWriter(ms, new UTF8Encoding(true));
            await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            csv.WriteHeader<WeatherCsvRow>();
            await csv.NextRecordAsync();

            foreach (var r in records)
            {
                csv.WriteRecord(new WeatherCsvRow
                {
                    Timestamp = r.Timestamp.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    Location = r.Location,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Temperature_C = r.Temperature,
                    Humidity_Pct = r.Humidity,
                    Rainfall_mm = r.RainfallMm,
                    WindSpeed_ms = r.WindSpeedMs,
                    WindDirection_deg = r.WindDirectionDeg,
                    //WindDirection_text = r.WindDirectionText,
                    Condition = r.Condition,
                    ConditionDetail = r.ConditionDetail,
                    PM25_ugm3 = r.Pm25,
                    PM10_ugm3 = r.Pm10,
                    PSI = r.Psi,
                    PSI_Category = r.PsiCategory,
                    CO_ugm3 = r.Co,
                    NO2_ugm3 = r.No2,
                    O3_ugm3 = r.O3,
                    SO2_ugm3 = r.So2,
                    UV_Index = r.UvIndex,
                    UV_Category = r.UvCategory
                });
                await csv.NextRecordAsync();
            }
            await writer.FlushAsync();
            return ms.ToArray();
        }
    }
}
