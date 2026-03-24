namespace WeatherService.Api.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            var start = DateTime.UtcNow;
            await _next(ctx);
            var timeTaken = Math.Round((DateTime.UtcNow - start).TotalMilliseconds);
            _logger.LogInformation($"Method: {ctx.Request.Method} - Path: {ctx.Request.Path} - Status: {ctx.Response.StatusCode} - TimeTaken: {timeTaken}");
        }
    }
}
