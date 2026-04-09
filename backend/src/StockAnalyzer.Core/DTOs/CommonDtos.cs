namespace StockAnalyzer.Core.DTOs;

/// <summary>
/// 统一API响应包装
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public string? TraceId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}

/// <summary>
/// 错误响应
/// </summary>
public record ErrorResponse
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? TraceId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string[]>? ValidationErrors { get; init; }
}

/// <summary>
/// 分页请求基类
/// </summary>
public record PagedRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? 20 : (value > 100 ? 100 : value);
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// 分页响应基类
/// </summary>
public record PagedResponse<T>
{
    public List<T> Items { get; init; } = new();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
