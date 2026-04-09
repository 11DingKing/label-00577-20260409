using StockAnalyzer.Core.DTOs;

namespace StockAnalyzer.Core.Services;

/// <summary>
/// 股票服务接口
/// </summary>
public interface IStockService
{
    Task<StockResponse> AddStockAsync(AddStockRequest request);
    Task<List<StockResponse>> AddStocksAsync(BatchAddStocksRequest request);
    Task<StockListResponse> GetAllStocksAsync(bool includeInactive = false);
    Task<StockResponse?> GetStockBySymbolAsync(string symbol);
    Task<StockResponse?> UpdateStockAsync(string symbol, UpdateStockRequest request);
    Task<bool> DeleteStockAsync(string symbol);
}

/// <summary>
/// 分析服务接口
/// </summary>
public interface IAnalysisService
{
    Task<RunAnalysisResponse> RunAnalysisAsync(RunAnalysisRequest request, CancellationToken cancellationToken = default);
    Task<AnalysisResultResponse?> RunSingleAnalysisAsync(string symbol, CancellationToken cancellationToken = default);
    Task<AnalysisResultListResponse> GetResultsAsync(AnalysisQueryParams queryParams);
    Task<List<AnalysisResultResponse>> GetResultsBySymbolAsync(string symbol, int limit = 30);
    Task<List<AnalysisResultResponse>> GetLatestResultsAsync(int count = 50);
}

/// <summary>
/// 统计服务接口
/// </summary>
public interface IStatisticsService
{
    Task<ConsecutiveQueryResponse> GetConsecutiveStocksAsync(ConsecutiveQueryRequest request);
    Task<StatisticsSummaryResponse> GetSummaryAsync();
    Task<StockTrendResponse?> GetStockTrendAsync(string symbol, int days = 30);
}
