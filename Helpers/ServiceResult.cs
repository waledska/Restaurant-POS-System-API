namespace WebApisApp.Helpers
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string>? Errors { get; set; }

        public static ServiceResult Ok(string message = "") => new() { Success = true, Message = message };
        public static ServiceResult Fail(string message, List<string>? errors = null) => new() { Success = false, Message = message, Errors = errors };
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        public static ServiceResult<T> Ok(T data, string message = "") => new() { Success = true, Message = message, Data = data };
        public static new ServiceResult<T> Fail(string message, List<string>? errors = null) => new() { Success = false, Message = message, Errors = errors };
    }
}
