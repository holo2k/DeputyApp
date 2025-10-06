using Microsoft.AspNetCore.Http;

namespace Shared.Middleware;

public class JwtBlacklistMiddleware(RequestDelegate next, IBlackListService blacklistService)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (token != null && blacklistService.IsTokenBlacklisted(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            var message = "У вас нет доступа к этому ресурсу.";
            await context.Response.WriteAsync(message);
            return;
        }

        await next(context);
    }
}