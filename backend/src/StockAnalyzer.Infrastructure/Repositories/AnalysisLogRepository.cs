using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;
using StockAnalyzer.Infrastructure.Data;

namespace StockAnalyzer.Infrastructure.Repositories;

/// <summary>
/// 分析日志仓储实现
/// </summary>
public class AnalysisLogRepository : IAnalysisLogRepository
{
    private readonly AppDbContext _context;

    public AnalysisLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AnalysisLog> AddAsync(AnalysisLog log)
    {
        _context.AnalysisLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<List<AnalysisLog>> GetRecentAsync(int count = 10)
    {
        return await _context.AnalysisLogs
            .OrderByDescending(l => l.ExecutedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<AnalysisLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.AnalysisLogs
            .Where(l => l.ExecutedAt >= startDate && l.ExecutedAt <= endDate)
            .OrderByDescending(l => l.ExecutedAt)
            .ToListAsync();
    }
}
