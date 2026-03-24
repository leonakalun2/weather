using Microsoft.AspNetCore.Mvc;
using WeatherService.Api.Helpers;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos.Responses;

namespace WeatherService.Api.Controllers
{
    [ApiController]
    [Route("api/v1/weather")]
    [Produces("application/json")]
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weather;
        private readonly ILogger<WeatherController> _logger;

        public WeatherController(IWeatherService weather, ILogger<WeatherController> logger)
        {
            _weather = weather;
            _logger = logger;
        }

        /// <summary>
        /// Get current weather condition for a location
        /// </summary>
        /// <returns></returns> 
        [HttpGet("currentweather")]
        [ProducesResponseType(typeof(BaseApiResponseDto<WeatherResponseDto>), 200)]
        [ProducesResponseType(typeof(BaseApiResponseDto<WeatherResponseDto>), 400)]
        public async Task<IActionResult> GetCurrentWeather([FromQuery] string location = "singapore")
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(BaseApiResponseDto<WeatherResponseDto>.Fail("Location is required"));

            var record = await _weather.GetCurrentWeatherAsync(location);
            if (record == null)
                return NotFound(BaseApiResponseDto<WeatherResponseDto>.Fail($"No weather data from location: {location}"));

            return Ok(BaseApiResponseDto<WeatherResponseDto>.Ok(ResponseMapperHelper.ToWeatherResponse(record)));

        }

        /// <summary>
        /// Get weather forecast from a location, maximum is 4 days.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        [HttpGet("forecast")]
        [ProducesResponseType(typeof(BaseApiResponseDto<ForecastResponseDto>), 200)]
        public async Task<IActionResult> GetForecast([FromQuery] string location,[FromQuery] int days)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(BaseApiResponseDto<WeatherResponseDto>.Fail("Location is required"));

            days = Math.Clamp(days, 1, 4);
            var records = await _weather.GetForecastAsync(location, days);

            if (records == null || records.Count == 0)
                return NotFound(BaseApiResponseDto<ForecastResponseDto>.Fail($"No forecast data from location: {location} for days : {days}"));

            return Ok(BaseApiResponseDto<ForecastResponseDto>.Ok(ResponseMapperHelper.ToForecastResponse(records, location)));

        }

        /// <summary>
        /// Get historical weather records with aggregate statistics (Date Time Format = yyyy-MM-dd, e.g. 2024-01-01)
        /// </summary>
        /// <param name="location"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [HttpGet("historical")]
        [ProducesResponseType(typeof(BaseApiResponseDto<HistoricalResponseDto>), 200)]
        [ProducesResponseType(typeof(BaseApiResponseDto<HistoricalResponseDto>), 400)]
        public async Task<IActionResult> GetHistorical([FromQuery] string location, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(BaseApiResponseDto<HistoricalResponseDto>.Fail("Location is required"));
            if (from >= to)
                return BadRequest(BaseApiResponseDto<HistoricalResponseDto>.Fail("'from' must be earlier than 'to'"));
            if ((to - from).TotalDays > 365)
                return BadRequest(BaseApiResponseDto<HistoricalResponseDto>.Fail("Date range cannot exceed 365 days"));

            try
            {
                var records = await _weather.GetHistoricalAsync(location, from.ToUniversalTime(), to.ToUniversalTime());

                if (records == null || records.Count == 0)
                    return NotFound(BaseApiResponseDto<HistoricalResponseDto>.Fail($"No historical data from location: {location} from: {from} to {to}"));

                return Ok(BaseApiResponseDto<HistoricalResponseDto>.Ok(ResponseMapperHelper.ToHistoricalResponse(records, location, from, to)));
            }
            catch (Exception ex)
            {
                return BadRequest(BaseApiResponseDto<HistoricalResponseDto>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// Export weather report to Csv file
        /// </summary>
        /// <param name="location"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        [HttpGet("export/csv")]
        [Produces("text/csv", "application/json")]
        public async Task<IActionResult> ExportCsv([FromQuery] string location, [FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(BaseApiResponseDto<object>.Fail("Location is required"));
            if (from >= to)
                return BadRequest(BaseApiResponseDto<object>.Fail("'from' must be earlier than 'to'"));

            try
            {
                var csv = await _weather.ExportToCsvAsync(location, from.ToUniversalTime(), to.ToUniversalTime());
                var filename = $"nea_weather_{location.Replace(" ", "_")}_{from:yyyyMMdd}_{to:yyyyMMdd}.csv";
                return File(csv, "text/csv", filename);
            }
            catch (Exception ex)
            {
                return BadRequest(BaseApiResponseDto<object>.Fail(ex.Message));
            }
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(BaseApiResponseDto<object>), 200)]
        public async Task<IActionResult> Refresh(
        [FromQuery] string location = "Singapore")
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest(BaseApiResponseDto<object>.Fail("Location is required"));

            await _weather.RefreshWeatherDataAsync(location);
            return Ok(BaseApiResponseDto<object>.Ok(null, $"Weather refreshed for '{location}'"));
        }
    }
}
