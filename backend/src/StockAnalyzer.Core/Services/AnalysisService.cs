using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Core.Services;

/// <summary>
/// 分析服务实现
/// </summary>
public class AnalysisService : IAnalysisService
{
    private readonly IStockRepository _stockRepository;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly IAnalysisLogRepository _logRepository;
    private readonly IAiService _aiService;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(
        IStockRepository stockRepository,
        IAnalysisRepository analysisRepository,
        IAnalysisLogRepository logRepository,
        IAiService aiService,
        ILogger<AnalysisService> logger)
    {
        _stockRepository = stockRepository;
        _analysisRepository = analysisRepository;
        _logRepository = logRepository;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<RunAnalysisResponse> RunAnalysisAsync(RunAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // 获取要分析的股票
        List<Stock> stocks;
        if (request.Symbols != null && request.Symbols.Count > 0)
        {
            stocks = await _stockRepository.GetBySymbolsAsync(request.Symbols);
            if (stocks.Count == 0)
            {
                throw new InvalidOperationException("未找到指定的股票");
            }
        }
        else
        {
            stocks = await _stockRepository.GetAllAsync(false);
            if (stocks.Count == 0)
            {
                throw new InvalidOperationException("股票列表为空，请先添加股票");
            }
        }

        _logger.LogInformation("开始分析 {Count} 只股票", stocks.Count);

        var results = new List<AnalysisResultResponse>();
        var errors = new List<AnalysisErrorDetail>();
        var skippedCount = 0;

        foreach (var stock in stocks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // 检查今日是否已有分析记录
                var existingResult = await _analysisRepository.GetByStockAndDateAsync(stock.Id, today);
                
                // 如果已存在且不强制重新分析，则跳过
                if (existingResult != null && !request.ForceRerun)
                {
                    _logger.LogDebug("股票 {Symbol} 今日已分析，跳过", stock.Symbol);
                    skippedCount++;
                    continue;
                }

                var aiResponse = await _aiService.AnalyzeStockAsync(stock, cancellationToken);
                
                AnalysisResult analysisResult;
                
                // Bug Fix: 实现 Upsert 逻辑 - 存在则更新，不存在则插入
                if (existingResult != null)
                {
                    // 更新现有记录
                    existingResult.Recommendation = aiResponse.Recommendation;
                    existingResult.Confidence = aiResponse.Confidence;
                    existingResult.Reasoning = aiResponse.Reasoning;
                    existingResult.RawAiResponse = aiResponse.RawResponse;
                    analysisResult = await _analysisRepository.UpdateAsync(existingResult);
                    _logger.LogDebug("更新已有分析结果: {Symbol}", stock.Symbol);
                }
                else
                {
                    // 插入新记录
                    analysisResult = new AnalysisResult
                    {
                        StockId = stock.Id,
                        AnalysisDate = today,
                        Recommendation = aiResponse.Recommendation,
                        Confidence = aiResponse.Confidence,
                        Reasoning = aiResponse.Reasoning,
                        RawAiResponse = aiResponse.RawResponse,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _analysisRepository.AddAsync(analysisResult);
                }
                
                results.Add(new AnalysisResultResponse
                {
                    Id = analysisResult.Id,
                    Symbol = stock.Symbol,
                    StockName = stock.Name,
                    AnalysisDate = analysisResult.AnalysisDate,
                    Recommendation = analysisResult.Recommendation.ToString(),
                    Confidence = analysisResult.Confidence,
                    Reasoning = analysisResult.Reasoning,
                    CreatedAt = analysisResult.CreatedAt
                });

                _logger.LogInformation("分析完成: {Symbol} - {Recommendation} ({Confidence}%)", 
                    stock.Symbol, aiResponse.Recommendation, aiResponse.Confidence);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "分析股票 {Symbol} 失败", stock.Symbol);
                errors.Add(new AnalysisErrorDetail
                {
                    Symbol = stock.Symbol,
                    ErrorMessage = ex.Message
                });
            }
        }

        stopwatch.Stop();

        // 记录日志
        var log = new AnalysisLog
        {
            ExecutedAt = DateTime.UtcNow,
            TotalStocks = stocks.Count,
            SuccessCount = results.Count,
            FailureCount = errors.Count,
            ErrorDetails = errors.Count > 0 ? JsonSerializer.Serialize(errors) : null,
            DurationMs = stopwatch.ElapsedMilliseconds
        };
        await _logRepository.AddAsync(log);

        _logger.LogInformation("分析任务完成: 总计 {Total}, 成功 {Success}, 失败 {Failure}, 跳过 {Skipped}, 耗时 {Duration}ms",
            stocks.Count, results.Count, errors.Count, skippedCount, stopwatch.ElapsedMilliseconds);

        return new RunAnalysisResponse
        {
            TotalStocks = stocks.Count,
            SuccessCount = results.Count,
            FailureCount = errors.Count,
            SkippedCount = skippedCount,
            DurationMs = stopwatch.ElapsedMilliseconds,
            Results = results,
            Errors = errors.Count > 0 ? errors : null
        };
    }

    public async Task<AnalysisResultResponse?> RunSingleAnalysisAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var stock = await _stockRepository.GetBySymbolAsync(symbol.ToUpperInvariant());
        if (stock == null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        try
        {
            var aiResponse = await _aiService.AnalyzeStockAsync(stock, cancellationToken);
            
            // 检查是否已有今日结果，有则更新
            var existing = await _analysisRepository.GetByStockAndDateAsync(stock.Id, today);
            
            AnalysisResult analysisResult;
            if (existing != null)
            {
                // Bug Fix: 更新现有记录并保存到数据库
                existing.Recommendation = aiResponse.Recommendation;
                existing.Confidence = aiResponse.Confidence;
                existing.Reasoning = aiResponse.Reasoning;
                existing.RawAiResponse = aiResponse.RawResponse;
                analysisResult = await _analysisRepository.UpdateAsync(existing);
                _logger.LogDebug("更新已有分析结果: {Symbol}, Date: {Date}", stock.Symbol, today);
            }
            else
            {
                analysisResult = new AnalysisResult
                {
                    StockId = stock.Id,
                    AnalysisDate = today,
                    Recommendation = aiResponse.Recommendation,
                    Confidence = aiResponse.Confidence,
                    Reasoning = aiResponse.Reasoning,
                    RawAiResponse = aiResponse.RawResponse,
                    CreatedAt = DateTime.UtcNow
                };
                await _analysisRepository.AddAsync(analysisResult);
            }

            _logger.LogInformation("单股分析完成: {Symbol} - {Recommendation} ({Confidence}%)",
                stock.Symbol, aiResponse.Recommendation, aiResponse.Confidence);

            return new AnalysisResultResponse
            {
                Id = analysisResult.Id,
                Symbol = stock.Symbol,
                StockName = stock.Name,
                AnalysisDate = analysisResult.AnalysisDate,
                Recommendation = analysisResult.Recommendation.ToString(),
                Confidence = analysisResult.Confidence,
                Reasoning = analysisResult.Reasoning,
                CreatedAt = analysisResult.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "单股分析失败: {Symbol}", symbol);
            throw;
        }
    }

    public async Task<AnalysisResultListResponse> GetResultsAsync(AnalysisQueryParams queryParams)
    {
        var (items, total) = await _analysisRepository.GetPagedAsync(
            queryParams.Page,
            queryParams.PageSize,
            queryParams.Symbol,
            queryParams.StartDate,
            queryParams.EndDate,
            queryParams.Recommendation);

        var responses = items.Select(r => new AnalysisResultResponse
        {
            Id = r.Id,
            Symbol = r.Stock.Symbol,
            StockName = r.Stock.Name,
            AnalysisDate = r.AnalysisDate,
            Recommendation = r.Recommendation.ToString(),
            Confidence = r.Confidence,
            Reasoning = r.Reasoning,
            CreatedAt = r.CreatedAt
        }).ToList();

        return new AnalysisResultListResponse
        {
            Results = responses,
            Total = total,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<List<AnalysisResultResponse>> GetResultsBySymbolAsync(string symbol, int limit = 30)
    {
        var stock = await _stockRepository.GetBySymbolAsync(symbol.ToUpperInvariant());
        if (stock == null)
        {
            return new List<AnalysisResultResponse>();
        }

        var results = await _analysisRepository.GetByStockIdAsync(stock.Id, limit);
        
        return results.Select(r => new AnalysisResultResponse
        {
            Id = r.Id,
            Symbol = stock.Symbol,
            StockName = stock.Name,
            AnalysisDate = r.AnalysisDate,
            Recommendation = r.Recommendation.ToString(),
            Confidence = r.Confidence,
            Reasoning = r.Reasoning,
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<List<AnalysisResultResponse>> GetLatestResultsAsync(int count = 50)
    {
        var results = await _analysisRepository.GetLatestAsync(count);
        
        return results.Select(r => new AnalysisResultResponse
        {
            Id = r.Id,
            Symbol = r.Stock.Symbol,
            StockName = r.Stock.Name,
            AnalysisDate = r.AnalysisDate,
            Recommendation = r.Recommendation.ToString(),
            Confidence = r.Confidence,
            Reasoning = r.Reasoning,
            CreatedAt = r.CreatedAt
        }).ToList();
    }
}
