using System.Net;
using System.Text.Json;
using WeatherService.Model.Dtos.Responses;

namespace WeatherService.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private static readonly JsonSerializerOptions _jsonOption = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandle exception - Method: {ctx.Request.Method} - Path: {ctx.Request.Path}");
                await HandleAsync(ctx, ex);
            }
        }

        public static async Task HandleAsync(HttpContext ctx, Exception ex)
        {
            var (status, message) = ex switch
            {
                ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
                _ => (HttpStatusCode.InternalServerError,
                      "An unexpected error occurred. Please try again later.")
            };

            ctx.Response.StatusCode = (int)status;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(BaseApiResponseDto<object>.Fail(message), _jsonOption));
        }
    }
}
