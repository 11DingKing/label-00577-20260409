using Microsoft.AspNetCore.Mvc.Filters;

namespace StockAnalyzer.Api.Filters;

/// <summary>
/// 操作日志 Filter（AOP 风格）
/// 记录所有 API 操作的详细日志
/// </summary>
public class OperationLogFilter : IAsyncActionFilter
{
    private readonly ILogger<OperationLogFilter> _logger;

    public OperationLogFilter(ILogger<OperationLogFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        var actionName = context.RouteData.Values["action"]?.ToString();
        var httpMethod = context.HttpContext.Request.Method;
        var traceId = context.HttpContext.TraceIdentifier;

        // 获取操作参数（排除敏感信息）
        var parameters = context.ActionArguments
            .Where(kvp => !IsSensitiveParameter(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => SanitizeValue(kvp.Value));

        _logger.LogInformation(
            "[{TraceId}] Action executing: {Controller}.{Action} ({Method}) with parameters: {@Parameters}",
            traceId,
            controllerName,
            actionName,
            httpMethod,
            parameters);

        var executedContext = await next();

        if (executedContext.Exception != null)
        {
            _logger.LogError(
                executedContext.Exception,
                "[{TraceId}] Action failed: {Controller}.{Action} ({Method})",
                traceId,
                controllerName,
                actionName,
                httpMethod);
        }
        else
        {
            var resultType = executedContext.Result?.GetType().Name ?? "null";
            _logger.LogInformation(
                "[{TraceId}] Action completed: {Controller}.{Action} ({Method}) -> {ResultType}",
                traceId,
                controllerName,
                actionName,
                httpMethod,
                resultType);
        }
    }

    private static bool IsSensitiveParameter(string parameterName)
    {
        var sensitiveNames = new[] { "password", "token", "apikey", "secret", "credential" };
        return sensitiveNames.Any(s => parameterName.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private static object? SanitizeValue(object? value)
    {
        if (value == null)
            return null;

        // 对于复杂对象，只返回类型名
        var type = value.GetType();
        if (!type.IsPrimitive && type != typeof(string) && !type.IsEnum)
        {
            return $"[{type.Name}]";
        }

        return value;
    }
}

/// <summary>
/// 标记需要详细日志记录的操作
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class AuditLogAttribute : Attribute
{
    public string? Description { get; set; }
    
    public AuditLogAttribute(string? description = null)
    {
        Description = description;
    }
}
