using Microsoft.Extensions.Logging;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Interfaces;

namespace StockAnalyzer.Core.Services;

/// <summary>
/// 统计服务实现
/// </summary>
public class StatisticsService : IStatisticsService
{
    private readonly IStockRepository _stockRepository;
    private readonly IAnalysisRepository _analysisRepository;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(
        IStockRepository stockRepository,
        IAnalysisRepository analysisRepository,
        ILogger<StatisticsService> logger)
    {
        _stockRepository = stockRepository;
        _analysisRepository = analysisRepository;
        _logger = logger;
    }

    public async Task<ConsecutiveQueryResponse> GetConsecutiveStocksAsync(ConsecutiveQueryRequest request)
    {
        var endDate = request.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddDays(-(request.Days - 1));
        
        _logger.LogInformation("查询连续 {Days} 天 {Recommendation} 的股票, 日期范围: {Start} - {End}",
            request.Days, request.Recommendation, startDate, endDate);

        var stocks = await _stockRepository.GetAllAsync(false);
        var consecutiveStocks = new List<ConsecutiveStockResponse>();

        foreach (var stock in stocks)
        {
            var results = await _analysisRepository.GetConsecutiveResultsAsync(
                stock.Id, 
                request.Recommendation, 
                endDate, 
                request.Days);

            // 检查是否有连续的记录
            if (results.Count >= request.Days)
            {
                // 验证是否真的连续（按日期排序后检查）
                var sortedResults = results.OrderByDescending(r => r.AnalysisDate).ToList();
                var consecutiveDays = CountConsecutiveDays(sortedResults, request.Recommendation);
                
                if (consecutiveDays >= request.Days)
                {
                    var avgConfidence = sortedResults
                        .Where(r => r.Recommendation == request.Recommendation)
                        .Average(r => r.Confidence);

                    var recentAnalysis = sortedResults
                        .Take(request.Days)
                        .Select(r => new AnalysisResultResponse
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

                    consecutiveStocks.Add(new ConsecutiveStockResponse
                    {
                        Symbol = stock.Symbol,
                        Name = stock.Name,
                        ConsecutiveDays = consecutiveDays,
                        Recommendation = request.Recommendation,
                        AverageConfidence = Math.Round(avgConfidence, 2),
                        StartDate = sortedResults.Last().AnalysisDate,
                        EndDate = sortedResults.First().AnalysisDate,
                        RecentAnalysis = recentAnalysis
                    });

                    _logger.LogDebug("找到符合条件的股票: {Symbol}, 连续 {Days} 天 {Recommendation}",
                        stock.Symbol, consecutiveDays, request.Recommendation);
                }
            }
        }

        return new ConsecutiveQueryResponse
        {
            Days = request.Days,
            Recommendation = request.Recommendation.ToString(),
            Stocks = consecutiveStocks.OrderByDescending(s => s.ConsecutiveDays)
                                     .ThenByDescending(s => s.AverageConfidence)
                                     .ToList(),
            TotalFound = consecutiveStocks.Count
        };
    }

    public async Task<StatisticsSummaryResponse> GetSummaryAsync()
    {
        var stockCount = await _stockRepository.GetActiveCountAsync();
        var (totalAnalysis, firstDate, lastDate) = await _analysisRepository.GetStatisticsInfoAsync();
        var recommendationStats = await _analysisRepository.GetRecommendationStatsAsync();

        var total = recommendationStats.Values.Sum(v => v.Count);
        
        RecommendationSummary GetSummary(Recommendation rec)
        {
            if (!recommendationStats.TryGetValue(rec, out var stats))
            {
                return new RecommendationSummary { Count = 0, Percentage = 0, AverageConfidence = 0 };
            }
            
            return new RecommendationSummary
            {
                Count = stats.Count,
                Percentage = total > 0 ? Math.Round((decimal)stats.Count / total * 100, 2) : 0,
                AverageConfidence = Math.Round(stats.AvgConfidence, 2)
            };
        }

        return new StatisticsSummaryResponse
        {
            TotalStocks = stockCount,
            TotalAnalysis = totalAnalysis,
            FirstAnalysisDate = firstDate,
            LastAnalysisDate = lastDate,
            BuySummary = GetSummary(Recommendation.Buy),
            HoldSummary = GetSummary(Recommendation.Hold),
            SellSummary = GetSummary(Recommendation.Sell)
        };
    }

    public async Task<StockTrendResponse?> GetStockTrendAsync(string symbol, int days = 30)
    {
        var stock = await _stockRepository.GetBySymbolAsync(symbol.ToUpperInvariant());
        if (stock == null)
        {
            return null;
        }

        var results = await _analysisRepository.GetByStockIdAsync(stock.Id, days);
        
        if (results.Count == 0)
        {
            return new StockTrendResponse
            {
                Symbol = stock.Symbol,
                Name = stock.Name,
                TrendData = new List<TrendDataPoint>(),
                Summary = new TrendSummary
                {
                    TotalDays = 0,
                    BuyDays = 0,
                    HoldDays = 0,
                    SellDays = 0,
                    DominantRecommendation = "N/A",
                    AverageConfidence = 0
                }
            };
        }

        var trendData = results
            .OrderBy(r => r.AnalysisDate)
            .Select(r => new TrendDataPoint
            {
                Date = r.AnalysisDate,
                Recommendation = r.Recommendation,
                Confidence = r.Confidence
            }).ToList();

        var buyDays = results.Count(r => r.Recommendation == Recommendation.Buy);
        var holdDays = results.Count(r => r.Recommendation == Recommendation.Hold);
        var sellDays = results.Count(r => r.Recommendation == Recommendation.Sell);
        
        var dominant = (buyDays >= holdDays && buyDays >= sellDays) ? "Buy" :
                       (holdDays >= buyDays && holdDays >= sellDays) ? "Hold" : "Sell";

        return new StockTrendResponse
        {
            Symbol = stock.Symbol,
            Name = stock.Name,
            TrendData = trendData,
            Summary = new TrendSummary
            {
                TotalDays = results.Count,
                BuyDays = buyDays,
                HoldDays = holdDays,
                SellDays = sellDays,
                DominantRecommendation = dominant,
                AverageConfidence = Math.Round(results.Average(r => r.Confidence), 2)
            }
        };
    }

    private int CountConsecutiveDays(List<Models.AnalysisResult> sortedResults, Recommendation targetRecommendation)
    {
        if (sortedResults.Count == 0)
            return 0;

        var count = 0;
        DateOnly? previousDate = null;

        foreach (var result in sortedResults)
        {
            if (result.Recommendation != targetRecommendation)
                break;

            if (previousDate.HasValue)
            {
                // 检查日期是否连续（允许周末间隔）
                var daysDiff = previousDate.Value.DayNumber - result.AnalysisDate.DayNumber;
                if (daysDiff > 3) // 超过3天间隔认为不连续
                    break;
            }

            count++;
            previousDate = result.AnalysisDate;
        }

        return count;
    }
}
