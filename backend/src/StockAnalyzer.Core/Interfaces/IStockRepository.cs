using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Core.Interfaces;

/// <summary>
/// 股票仓储接口
/// </summary>
public interface IStockRepository
{
    /// <summary>
    /// 获取所有股票
    /// </summary>
    Task<List<Stock>> GetAllAsync(bool includeInactive = false);
    
    /// <summary>
    /// 根据ID获取股票
    /// </summary>
    Task<Stock?> GetByIdAsync(int id);
    
    /// <summary>
    /// 根据代码获取股票
    /// </summary>
    Task<Stock?> GetBySymbolAsync(string symbol);
    
    /// <summary>
    /// 根据代码列表获取股票
    /// </summary>
    Task<List<Stock>> GetBySymbolsAsync(IEnumerable<string> symbols);
    
    /// <summary>
    /// 添加股票
    /// </summary>
    Task<Stock> AddAsync(Stock stock);
    
    /// <summary>
    /// 批量添加股票
    /// </summary>
    Task<List<Stock>> AddRangeAsync(IEnumerable<Stock> stocks);
    
    /// <summary>
    /// 更新股票
    /// </summary>
    Task<Stock> UpdateAsync(Stock stock);
    
    /// <summary>
    /// 删除股票
    /// </summary>
    Task<bool> DeleteAsync(string symbol);
    
    /// <summary>
    /// 检查股票是否存在
    /// </summary>
    Task<bool> ExistsAsync(string symbol);
    
    /// <summary>
    /// 获取活跃股票数量
    /// </summary>
    Task<int> GetActiveCountAsync();
}
