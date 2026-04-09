using Microsoft.Extensions.Logging;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Core.Services;

/// <summary>
/// 股票服务实现
/// </summary>
public class StockService : IStockService
{
    private readonly IStockRepository _stockRepository;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly ILogger<StockService> _logger;

    public StockService(
        IStockRepository stockRepository,
        IAnalysisRepository analysisRepository,
        ILogger<StockService> logger)
    {
        _stockRepository = stockRepository;
        _analysisRepository = analysisRepository;
        _logger = logger;
    }

    public async Task<StockResponse> AddStockAsync(AddStockRequest request)
    {
        var normalizedSymbol = request.Symbol.ToUpperInvariant().Trim();
        
        if (await _stockRepository.ExistsAsync(normalizedSymbol))
        {
            throw new InvalidOperationException($"股票 {normalizedSymbol} 已存在");
        }

        var stock = new Stock
        {
            Symbol = normalizedSymbol,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _stockRepository.AddAsync(stock);
        _logger.LogInformation("添加股票成功: {Symbol} - {Name}", created.Symbol, created.Name);
        
        return MapToResponse(created);
    }

    public async Task<List<StockResponse>> AddStocksAsync(BatchAddStocksRequest request)
    {
        var results = new List<StockResponse>();
        var errors = new List<string>();

        foreach (var stockRequest in request.Stocks)
        {
            try
            {
                var result = await AddStockAsync(stockRequest);
                results.Add(result);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add($"{stockRequest.Symbol}: {ex.Message}");
                _logger.LogWarning("批量添加股票跳过: {Symbol} - {Reason}", stockRequest.Symbol, ex.Message);
            }
        }

        if (errors.Count > 0 && results.Count == 0)
        {
            throw new InvalidOperationException($"批量添加失败: {string.Join("; ", errors)}");
        }

        _logger.LogInformation("批量添加股票完成: 成功 {SuccessCount}, 跳过 {SkipCount}", 
            results.Count, errors.Count);

        return results;
    }

    public async Task<StockListResponse> GetAllStocksAsync(bool includeInactive = false)
    {
        var stocks = await _stockRepository.GetAllAsync(includeInactive);
        var responses = new List<StockResponse>();

        foreach (var stock in stocks)
        {
            var response = await MapToResponseWithLatestAnalysis(stock);
            responses.Add(response);
        }

        return new StockListResponse
        {
            Stocks = responses,
            Total = responses.Count
        };
    }

    public async Task<StockResponse?> GetStockBySymbolAsync(string symbol)
    {
        var normalizedSymbol = symbol.ToUpperInvariant().Trim();
        var stock = await _stockRepository.GetBySymbolAsync(normalizedSymbol);
        
        if (stock == null)
            return null;

        return await MapToResponseWithLatestAnalysis(stock);
    }

    public async Task<StockResponse?> UpdateStockAsync(string symbol, UpdateStockRequest request)
    {
        var normalizedSymbol = symbol.ToUpperInvariant().Trim();
        var stock = await _stockRepository.GetBySymbolAsync(normalizedSymbol);
        
        if (stock == null)
            return null;

        if (request.Name != null)
            stock.Name = request.Name.Trim();
        
        if (request.IsActive.HasValue)
            stock.IsActive = request.IsActive.Value;

        stock.UpdatedAt = DateTime.UtcNow;
        
        var updated = await _stockRepository.UpdateAsync(stock);
        _logger.LogInformation("更新股票成功: {Symbol}", updated.Symbol);
        
        return await MapToResponseWithLatestAnalysis(updated);
    }

    public async Task<bool> DeleteStockAsync(string symbol)
    {
        var normalizedSymbol = symbol.ToUpperInvariant().Trim();
        var result = await _stockRepository.DeleteAsync(normalizedSymbol);
        
        if (result)
        {
            _logger.LogInformation("删除股票成功: {Symbol}", normalizedSymbol);
        }
        
        return result;
    }

    private StockResponse MapToResponse(Stock stock, AnalysisResultResponse? latestAnalysis = null, int analysisCount = 0)
    {
        return new StockResponse
        {
            Id = stock.Id,
            Symbol = stock.Symbol,
            Name = stock.Name,
            IsActive = stock.IsActive,
            CreatedAt = stock.CreatedAt,
            UpdatedAt = stock.UpdatedAt,
            AnalysisCount = analysisCount,
            LatestAnalysis = latestAnalysis
        };
    }

    private async Task<StockResponse> MapToResponseWithLatestAnalysis(Stock stock)
    {
        // 优化：只查询一次数据库，获取所有结果（限制合理数量）
        var allResults = await _analysisRepository.GetByStockIdAsync(stock.Id, 1000);
        var latest = allResults.FirstOrDefault();
        
        AnalysisResultResponse? latestResponse = null;
        if (latest != null)
        {
            latestResponse = new AnalysisResultResponse
            {
                Id = latest.Id,
                Symbol = stock.Symbol,
                StockName = stock.Name,
                AnalysisDate = latest.AnalysisDate,
                Recommendation = latest.Recommendation.ToString(),
                Confidence = latest.Confidence,
                Reasoning = latest.Reasoning,
                CreatedAt = latest.CreatedAt
            };
        }
        
        return MapToResponse(stock, latestResponse, allResults.Count);
    }
}
