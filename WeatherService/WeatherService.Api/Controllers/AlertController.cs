using Microsoft.AspNetCore.Mvc;
using WeatherService.Api.Helpers;
using WeatherService.Infrastructure.Interfaces;
using WeatherService.Model.Dtos.Requests;
using WeatherService.Model.Dtos.Responses;

namespace WeatherService.Api.Controllers
{
    [ApiController]
    [Route("api/v1/alerts")]
    [Produces("application/json")]
    public class AlertController : ControllerBase
    {
        private readonly IAlertService _alerts;
        private readonly ILogger<AlertController> _logger;

        public AlertController(IAlertService alerts, ILogger<AlertController> logger)
        {
            _alerts = alerts;
            _logger = logger;
        }

        /// <summary>
        /// Subscribe to a weather alert.
        /// Conditions: Temperature(°C), Humidity(%), RainfallMm(mm),
        /// WindSpeedMs(m/s), Pm25(µg/m³), Psi, UvIndex.
        /// Operators: GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(BaseApiResponseDto<AlertResponseDto>), 201)]
        [ProducesResponseType(typeof(BaseApiResponseDto<AlertResponseDto>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateAlertRequestDto req)
        {
            if (string.IsNullOrWhiteSpace(req.SubscriberEmail))
                return BadRequest(BaseApiResponseDto<AlertResponseDto>.Fail("Email is required"));
            if (string.IsNullOrWhiteSpace(req.Location))
                return BadRequest(BaseApiResponseDto<AlertResponseDto>.Fail("Location is required"));

            var alert = await _alerts.CreateAlertAsync(req);
            _logger.LogInformation("Alert {Id} created for {Email}", alert.Id, alert.SubscriberEmail);

            return CreatedAtAction(nameof(GetById), new { id = alert.Id },
                BaseApiResponseDto<AlertResponseDto>.Ok(ResponseMapperHelper.ToAlertResponse(alert), "Alert subscription created"));
        }

        /// <summary>
        /// Get alert by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(BaseApiResponseDto<AlertResponseDto>), 200)]
        [ProducesResponseType(typeof(BaseApiResponseDto<AlertResponseDto>), 404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var alert = await _alerts.GetAlertByIdAsync(id);
            return alert == null
                ? NotFound(BaseApiResponseDto<AlertResponseDto>.Fail($"Alert {id} not found"))
                : Ok(BaseApiResponseDto<AlertResponseDto>.Ok(ResponseMapperHelper.ToAlertResponse(alert)));
        }

        /// <summary>
        /// Get all alerts for an email address
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(BaseApiResponseDto<IList<AlertResponseDto>>), 200)]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(BaseApiResponseDto<IList<AlertResponseDto>>
                    .Fail("Email is required"));

            var alerts = await _alerts.GetAlertsByEmailAsync(email);
            return Ok(BaseApiResponseDto<IList<AlertResponseDto>>.Ok(ResponseMapperHelper.ToAlertListResponse(alerts)));
        }

        /// <summary>
        /// Update threshold, operator, or active status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="req"></param>
        /// <returns></returns>
        [HttpPatch("{id:guid}")]
        [ProducesResponseType(typeof(BaseApiResponseDto<AlertResponseDto>), 200)]
        [ProducesResponseType(typeof(BaseApiResponseDto<AlertResponseDto>), 404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAlertRequestDto req)
        {
            try
            {
                var alert = await _alerts.UpdateAlertAsync(id, req);
                return Ok(BaseApiResponseDto<AlertResponseDto>.Ok(ResponseMapperHelper.ToAlertResponse(alert), "Alert updated"));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(BaseApiResponseDto<AlertResponseDto>.Fail($"Alert {id} not found"));
            }
        }

        /// <summary>
        /// Unsubscribe — permanently deletes the alert.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(BaseApiResponseDto<object>), 200)]
        [ProducesResponseType(typeof(BaseApiResponseDto<object>), 404)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _alerts.GetAlertByIdAsync(id);
            if (existing is null)
                return NotFound(BaseApiResponseDto<object>.Fail($"Alert {id} not found"));

            await _alerts.DeleteAlertAsync(id);
            return Ok(BaseApiResponseDto<object>.Ok(null, "Alert deleted"));
        }

        /// <summary>
        /// Manually trigger alert (for testing)
        /// </summary>
        /// <returns></returns>
        [HttpPost("process")]
        [ProducesResponseType(typeof(BaseApiResponseDto<object>), 200)]
        public async Task<IActionResult> Process()
        {
            await _alerts.ProcessAlertsAsync();
            return Ok(BaseApiResponseDto<object>.Ok(null, "Alert processing triggered"));
        }
    }
}
