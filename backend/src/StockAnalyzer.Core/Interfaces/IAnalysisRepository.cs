using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Core.Interfaces;

/// <summary>
/// 分析结果仓储接口
/// </summary>
public interface IAnalysisRepository
{
    /// <summary>
    /// 添加分析结果
    /// </summary>
    Task<AnalysisResult> AddAsync(AnalysisResult result);
    
    /// <summary>
    /// 更新分析结果
    /// </summary>
    Task<AnalysisResult> UpdateAsync(AnalysisResult result);
    
    /// <summary>
    /// 批量添加分析结果
    /// </summary>
    Task<List<AnalysisResult>> AddRangeAsync(IEnumerable<AnalysisResult> results);
    
    /// <summary>
    /// 获取指定股票的分析结果
    /// </summary>
    Task<List<AnalysisResult>> GetByStockIdAsync(int stockId, int limit = 30);
    
    /// <summary>
    /// 获取指定股票在指定日期的分析结果
    /// </summary>
    Task<AnalysisResult?> GetByStockAndDateAsync(int stockId, DateOnly date);
    
    /// <summary>
    /// 获取指定日期范围的分析结果
    /// </summary>
    Task<List<AnalysisResult>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate);
    
    /// <summary>
    /// 获取最新分析结果
    /// </summary>
    Task<List<AnalysisResult>> GetLatestAsync(int count = 50);
    
    /// <summary>
    /// 分页查询分析结果
    /// </summary>
    Task<(List<AnalysisResult> Items, int Total)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? symbol = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Recommendation? recommendation = null);
    
    /// <summary>
    /// 获取指定股票指定日期范围内连续相同建议的记录
    /// </summary>
    Task<List<AnalysisResult>> GetConsecutiveResultsAsync(
        int stockId, 
        Recommendation recommendation, 
        DateOnly endDate, 
        int days);
    
    /// <summary>
    /// 获取所有股票在指定日期范围内的分析结果（用于统计）
    /// </summary>
    Task<List<AnalysisResult>> GetAllForStatisticsAsync(DateOnly startDate, DateOnly endDate);
    
    /// <summary>
    /// 获取统计信息
    /// </summary>
    Task<(int Total, DateOnly? FirstDate, DateOnly? LastDate)> GetStatisticsInfoAsync();
    
    /// <summary>
    /// 获取按建议类型分组的统计
    /// </summary>
    Task<Dictionary<Recommendation, (int Count, decimal AvgConfidence)>> GetRecommendationStatsAsync();
    
    /// <summary>
    /// 检查指定股票在指定日期是否已有分析结果
    /// </summary>
    Task<bool> ExistsAsync(int stockId, DateOnly date);
}
