using Microsoft.AspNetCore.Mvc;
using WeatherService.Api.Helpers;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos.Responses;

namespace WeatherService.Api.Controllers
{
    [ApiController]
    [Route("api/v1/locations")]
    [Produces("application/json")]
    public class LocationsController : ControllerBase
    {
        private readonly IWeatherRepository _weatherRepo;
        private readonly IAlertRepository _alertRepo;
        private readonly IWeatherService _weatherSvc;

        public LocationsController(IWeatherRepository weatherRepo, IAlertRepository alertRepo, IWeatherService weatherSvc)
        {
            _weatherRepo = weatherRepo;
            _alertRepo = alertRepo;
            _weatherSvc = weatherSvc;
        }

        /// <summary>
        /// List all locations currently tracked in the database
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(BaseApiResponseDto<IList<string>>), 200)]
        public async Task<IActionResult> GetAllLocations()
        {
            var locations = await _weatherRepo.GetAllLocationsAsync();
            return Ok(BaseApiResponseDto<IList<string>>.Ok(locations));
        }

        /// <summary>
        /// Dashboard summary — temperature, humidity, rainfall, PSI, UV, active alert count.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpGet("{location}/summary")]
        [ProducesResponseType(typeof(BaseApiResponseDto<LocationSummaryResponseDto>), 200)]
        [ProducesResponseType(typeof(BaseApiResponseDto<LocationSummaryResponseDto>), 404)]
        public async Task<IActionResult> GetSummary(string location)
        {
            var record = await _weatherSvc.GetCurrentWeatherAsync(location);
            if (record is null)
                return NotFound(BaseApiResponseDto<LocationSummaryResponseDto>.Fail($"No data for '{location}'"));

            var allAlerts = await _alertRepo.GetActiveAlertsAsync();
            var locationAlerts = allAlerts.Count(a =>
                string.Equals(a.Location, record.Location, StringComparison.OrdinalIgnoreCase));

            return Ok(BaseApiResponseDto<LocationSummaryResponseDto>.Ok(ResponseMapperHelper.ToLocationSummaryResponse(record, locationAlerts)));
        }
    }
}
