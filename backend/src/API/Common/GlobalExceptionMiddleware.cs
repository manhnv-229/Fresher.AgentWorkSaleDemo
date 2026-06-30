using System.Text.Json;

namespace Demo.Api.Common;

/// <summary>
/// Xử lý exception chưa được bắt trong HTTP pipeline và trả về phản hồi lỗi an toàn.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Chạy middleware kế tiếp và chặn các exception chưa được xử lý.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception after response started for {Method} {Path}. TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.TraceIdentifier);
                throw;
            }

            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse("server_error", "An unexpected server error occurred.");
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, SerializerOptions));
        }
    }
}
