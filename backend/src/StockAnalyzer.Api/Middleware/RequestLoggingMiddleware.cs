using System.Diagnostics;
using System.Text;

namespace StockAnalyzer.Api.Middleware;

/// <summary>
/// 请求/响应日志中间件
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = context.TraceIdentifier;
        
        // 记录请求信息
        var request = context.Request;
        var requestBody = await ReadRequestBodyAsync(request);
        
        _logger.LogInformation(
            "[{RequestId}] HTTP {Method} {Path}{QueryString} started - Body: {Body}",
            requestId,
            request.Method,
            request.Path,
            request.QueryString,
            TruncateBody(requestBody));

        // 包装响应流以捕获响应体
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // 读取响应体
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            
            // 复制响应到原始流
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                           statusCode >= 400 ? LogLevel.Warning :
                           LogLevel.Information;

            _logger.Log(logLevel,
                "[{RequestId}] HTTP {Method} {Path} completed with {StatusCode} in {ElapsedMs}ms - Response: {Response}",
                requestId,
                request.Method,
                request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds,
                TruncateBody(responseText));
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.ContentLength.HasValue || request.ContentLength == 0)
            return string.Empty;

        request.EnableBuffering();
        
        using var reader = new StreamReader(
            request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);
        
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        
        return body;
    }

    private static string TruncateBody(string body, int maxLength = 500)
    {
        if (string.IsNullOrEmpty(body))
            return "(empty)";
        
        // 移除换行符使日志更紧凑
        body = body.Replace("\n", " ").Replace("\r", "").Trim();
        
        if (body.Length <= maxLength)
            return body;
        
        return body.Substring(0, maxLength) + "...(truncated)";
    }
}
