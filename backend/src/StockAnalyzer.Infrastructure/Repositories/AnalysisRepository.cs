using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;
using StockAnalyzer.Infrastructure.Data;

namespace StockAnalyzer.Infrastructure.Repositories;

/// <summary>
/// 分析结果仓储实现
/// </summary>
public class AnalysisRepository : IAnalysisRepository
{
    private readonly AppDbContext _context;

    public AnalysisRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisResult> AddAsync(AnalysisResult result)
    {
        _context.AnalysisResults.Add(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<AnalysisResult> UpdateAsync(AnalysisResult result)
    {
        _context.AnalysisResults.Update(result);
        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<List<AnalysisResult>> AddRangeAsync(IEnumerable<AnalysisResult> results)
    {
        var resultList = results.ToList();
        _context.AnalysisResults.AddRange(resultList);
        await _context.SaveChangesAsync();
        return resultList;
    }

    public async Task<List<AnalysisResult>> GetByStockIdAsync(int stockId, int limit = 30)
    {
        return await _context.AnalysisResults
            .Include(r => r.Stock)
            .Where(r => r.StockId == stockId)
            .OrderByDescending(r => r.AnalysisDate)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<AnalysisResult?> GetByStockAndDateAsync(int stockId, DateOnly date)
    {
        return await _context.AnalysisResults
            .Include(r => r.Stock)
            .FirstOrDefaultAsync(r => r.StockId == stockId && r.AnalysisDate == date);
    }

    public async Task<List<AnalysisResult>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _context.AnalysisResults
            .Include(r => r.Stock)
            .Where(r => r.AnalysisDate >= startDate && r.AnalysisDate <= endDate)
            .OrderByDescending(r => r.AnalysisDate)
            .ThenBy(r => r.Stock.Symbol)
            .ToListAsync();
    }

    public async Task<List<AnalysisResult>> GetLatestAsync(int count = 50)
    {
        return await _context.AnalysisResults
            .Include(r => r.Stock)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<(List<AnalysisResult> Items, int Total)> GetPagedAsync(
        int page,
        int pageSize,
        string? symbol = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        Recommendation? recommendation = null)
    {
        var query = _context.AnalysisResults
            .Include(r => r.Stock)
            .AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
        {
            query = query.Where(r => r.Stock.Symbol == symbol.ToUpperInvariant());
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.AnalysisDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.AnalysisDate <= endDate.Value);
        }

        if (recommendation.HasValue)
        {
            query = query.Where(r => r.Recommendation == recommendation.Value);
        }

        var total = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(r => r.AnalysisDate)
            .ThenBy(r => r.Stock.Symbol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<AnalysisResult>> GetConsecutiveResultsAsync(
        int stockId,
        Recommendation recommendation,
        DateOnly endDate,
        int days)
    {
        // 获取指定股票最近 days * 2 条记录（考虑周末等间隔）
        var results = await _context.AnalysisResults
            .Include(r => r.Stock)
            .Where(r => r.StockId == stockId && r.AnalysisDate <= endDate)
            .OrderByDescending(r => r.AnalysisDate)
            .Take(days * 2)
            .ToListAsync();

        return results;
    }

    public async Task<List<AnalysisResult>> GetAllForStatisticsAsync(DateOnly startDate, DateOnly endDate)
    {
        return await _context.AnalysisResults
            .Include(r => r.Stock)
            .Where(r => r.AnalysisDate >= startDate && r.AnalysisDate <= endDate)
            .ToListAsync();
    }

    public async Task<(int Total, DateOnly? FirstDate, DateOnly? LastDate)> GetStatisticsInfoAsync()
    {
        var total = await _context.AnalysisResults.CountAsync();
        
        if (total == 0)
        {
            return (0, null, null);
        }

        var firstDate = await _context.AnalysisResults.MinAsync(r => r.AnalysisDate);
        var lastDate = await _context.AnalysisResults.MaxAsync(r => r.AnalysisDate);

        return (total, firstDate, lastDate);
    }

    public async Task<Dictionary<Recommendation, (int Count, decimal AvgConfidence)>> GetRecommendationStatsAsync()
    {
        var stats = await _context.AnalysisResults
            .GroupBy(r => r.Recommendation)
            .Select(g => new
            {
                Recommendation = g.Key,
                Count = g.Count(),
                // Bug Fix: 显式转换为 decimal 以匹配返回类型
                AvgConfidence = (decimal)g.Average(r => (double)r.Confidence)
            })
            .ToListAsync();

        return stats.ToDictionary(
            s => s.Recommendation,
            s => (s.Count, s.AvgConfidence));
    }

    public async Task<bool> ExistsAsync(int stockId, DateOnly date)
    {
        return await _context.AnalysisResults
            .AnyAsync(r => r.StockId == stockId && r.AnalysisDate == date);
    }
}
