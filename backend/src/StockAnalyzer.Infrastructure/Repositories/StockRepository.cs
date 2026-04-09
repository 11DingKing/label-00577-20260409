using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;
using StockAnalyzer.Infrastructure.Data;

namespace StockAnalyzer.Infrastructure.Repositories;

/// <summary>
/// 股票仓储实现
/// </summary>
public class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Stock>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Stocks.AsQueryable();
        
        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query.OrderBy(s => s.Symbol).ToListAsync();
    }

    public async Task<Stock?> GetByIdAsync(int id)
    {
        return await _context.Stocks.FindAsync(id);
    }

    public async Task<Stock?> GetBySymbolAsync(string symbol)
    {
        return await _context.Stocks
            .FirstOrDefaultAsync(s => s.Symbol == symbol);
    }

    public async Task<List<Stock>> GetBySymbolsAsync(IEnumerable<string> symbols)
    {
        var normalizedSymbols = symbols.Select(s => s.ToUpperInvariant()).ToList();
        return await _context.Stocks
            .Where(s => normalizedSymbols.Contains(s.Symbol) && s.IsActive)
            .ToListAsync();
    }

    public async Task<Stock> AddAsync(Stock stock)
    {
        _context.Stocks.Add(stock);
        await _context.SaveChangesAsync();
        return stock;
    }

    public async Task<List<Stock>> AddRangeAsync(IEnumerable<Stock> stocks)
    {
        var stockList = stocks.ToList();
        _context.Stocks.AddRange(stockList);
        await _context.SaveChangesAsync();
        return stockList;
    }

    public async Task<Stock> UpdateAsync(Stock stock)
    {
        _context.Stocks.Update(stock);
        await _context.SaveChangesAsync();
        return stock;
    }

    public async Task<bool> DeleteAsync(string symbol)
    {
        var stock = await GetBySymbolAsync(symbol);
        if (stock == null)
            return false;

        _context.Stocks.Remove(stock);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string symbol)
    {
        return await _context.Stocks.AnyAsync(s => s.Symbol == symbol);
    }

    public async Task<int> GetActiveCountAsync()
    {
        return await _context.Stocks.CountAsync(s => s.IsActive);
    }
}
