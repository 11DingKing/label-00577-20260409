using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Core.Interfaces;

/// <summary>
/// 分析日志仓储接口
/// </summary>
public interface IAnalysisLogRepository
{
    /// <summary>
    /// 添加日志
    /// </summary>
    Task<AnalysisLog> AddAsync(AnalysisLog log);
    
    /// <summary>
    /// 获取最近的日志
    /// </summary>
    Task<List<AnalysisLog>> GetRecentAsync(int count = 10);
    
    /// <summary>
    /// 获取指定日期范围的日志
    /// </summary>
    Task<List<AnalysisLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
