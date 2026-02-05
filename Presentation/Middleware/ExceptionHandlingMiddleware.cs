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
            context.Response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                ArgumentNullException ex => (StatusCodes.Status400BadRequest, ex.Message),
                ArgumentException ex => (StatusCodes.Status400BadRequest, ex.Message),
                InvalidOperationException ex => (StatusCodes.Status409Conflict, ex.Message),
                KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
                UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Internal server error")
            };

            context.Response.StatusCode = statusCode;

            var response = new ErrorResponse
            {
                Message = message
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
