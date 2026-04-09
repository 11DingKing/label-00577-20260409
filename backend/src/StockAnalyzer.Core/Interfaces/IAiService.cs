using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Core.Interfaces;

/// <summary>
/// AI分析服务接口
/// </summary>
public interface IAiService
{
    /// <summary>
    /// 分析单只股票
    /// </summary>
    /// <param name="stock">股票信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>AI分析响应</returns>
    Task<AiAnalysisResponse> AnalyzeStockAsync(Stock stock, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 批量分析股票
    /// </summary>
    /// <param name="stocks">股票列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析结果字典</returns>
    Task<Dictionary<string, AiAnalysisResponse>> AnalyzeStocksAsync(
        IEnumerable<Stock> stocks, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    Task<bool> IsAvailableAsync();
}
