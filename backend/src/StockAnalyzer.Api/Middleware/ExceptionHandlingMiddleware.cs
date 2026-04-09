using System.Net;
using System.Text.Json;
using StockAnalyzer.Core.DTOs;

namespace StockAnalyzer.Api.Middleware;

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        
        _logger.LogError(exception, "请求处理异常 [TraceId: {TraceId}]", traceId);

        var (statusCode, errorCode, message) = exception switch
        {
            InvalidOperationException => (HttpStatusCode.BadRequest, "INVALID_OPERATION", exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest, "INVALID_ARGUMENT", exception.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", exception.Message),
            OperationCanceledException => (HttpStatusCode.RequestTimeout, "OPERATION_CANCELLED", "操作已取消"),
            HttpRequestException => (HttpStatusCode.ServiceUnavailable, "EXTERNAL_SERVICE_ERROR", "外部服务调用失败"),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "服务器内部错误")
        };

        var errorResponse = new ErrorResponse
        {
            Code = errorCode,
            Message = message,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
