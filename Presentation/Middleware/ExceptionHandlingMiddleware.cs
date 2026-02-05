using System.Text.Json;

namespace Presentation.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Message = exception.Message
            };

            return context.Response.WriteAsync(
                JsonSerializer.Serialize(response)
            );
        }

        private sealed class ErrorResponse
        {
            public string Status { get; init; } = "error";
            public string Message { get; init; } = string.Empty;
        }
    }
}
