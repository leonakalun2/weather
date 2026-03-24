namespace WeatherService.Model.Dtos.Responses
{
    public class BaseApiResponseDto<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public IEnumerable<string> Errors { get; set; } = [];
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static BaseApiResponseDto<T> Ok(T data, string? message = null) =>
            new() { Success = true, Data = data, Message = message };

        public static BaseApiResponseDto<T> Fail(string error) =>
            new() { Success = false, Errors = [error] };

        public static BaseApiResponseDto<T> Fail(IEnumerable<string> errors) =>
            new() { Success = false, Errors = errors };
    }
}
