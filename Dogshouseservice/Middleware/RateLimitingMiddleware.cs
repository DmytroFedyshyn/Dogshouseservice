using Dogshouseservice.Constants;

namespace Dogshouseservice.Middleware
{
    public class RateLimitingMiddleware
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(10, 10); // Limit: 10 requests per second

        private readonly RequestDelegate _next;

        public RateLimitingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!await _semaphore.WaitAsync(100))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync(ResponseMessages.TooManyRequests);
            }
            else
            {
                try { await _next(context); }
                finally { _semaphore.Release(); }
            }
        }
    }
}
