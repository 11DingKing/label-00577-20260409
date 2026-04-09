using Microsoft.Extensions.Logging;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Enums;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Infrastructure.External;

/// <summary>
/// Mock AI 服务（用于测试和演示）
/// </summary>
public class MockAiService : IAiService
{
    private readonly ILogger<MockAiService> _logger;
    private readonly Random _random = new();

    public MockAiService(ILogger<MockAiService> logger)
    {
        _logger = logger;
    }

    public async Task<AiAnalysisResponse> AnalyzeStockAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        // 模拟 API 调用延迟
        await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(100, 500)), cancellationToken);

        // 基于股票代码的哈希生成相对稳定的结果
        var hash = stock.Symbol.GetHashCode();
        var dayOfYear = DateTime.UtcNow.DayOfYear;
        var seed = Math.Abs(hash + dayOfYear);
        var rand = new Random(seed);

        var recommendationValue = rand.Next(1, 4);
        var recommendation = recommendationValue switch
        {
            1 => Recommendation.Buy,
            2 => Recommendation.Hold,
            _ => Recommendation.Sell
        };

        var confidence = rand.Next(40, 95);
        
        var reasoning = GenerateReasoning(stock, recommendation, confidence);

        _logger.LogInformation("Mock AI 分析完成: {Symbol} - {Recommendation} ({Confidence}%)",
            stock.Symbol, recommendation, confidence);

        return new AiAnalysisResponse
        {
            Recommendation = recommendation,
            Confidence = confidence,
            Reasoning = reasoning,
            RawResponse = $"{{\"recommendation\":\"{recommendation}\",\"confidence\":{confidence},\"reasoning\":\"{reasoning}\"}}"
        };
    }

    public async Task<Dictionary<string, AiAnalysisResponse>> AnalyzeStocksAsync(
        IEnumerable<Stock> stocks,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, AiAnalysisResponse>();

        foreach (var stock in stocks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var response = await AnalyzeStockAsync(stock, cancellationToken);
            results[stock.Symbol] = response;
        }

        return results;
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(true);
    }

    private string GenerateReasoning(Stock stock, Recommendation recommendation, int confidence)
    {
        var reasons = recommendation switch
        {
            Recommendation.Buy => new[]
            {
                $"{stock.Symbol} 技术面呈现强劲上升趋势，成交量放大，MACD金叉形成，建议积极买入并持有。",
                $"{stock.Name} 基本面表现优秀，营收和利润持续增长，相对同行业估值偏低，具备较高的投资价值。",
                $"{stock.Symbol} 近期突破关键阻力位，RSI 和 MACD 指标均确认上涨趋势，短期有望继续走强。",
                $"{stock.Name} 行业景气度持续提升，公司竞争优势明显，市场份额稳步扩大，长期看好。"
            },
            Recommendation.Hold => new[]
            {
                $"{stock.Name} 目前处于震荡整理阶段，暂无明显的上涨或下跌信号，建议持有观望。",
                $"{stock.Symbol} 技术指标呈现中性走势，建议维持现有仓位，等待更明确的市场信号。",
                $"{stock.Name} 当前估值处于合理区间，短期缺乏明显催化剂，建议耐心持有等待时机。",
                $"{stock.Symbol} 基本面稳健但增速放缓，市场情绪偏谨慎，建议保持仓位观察后续走势。"
            },
            Recommendation.Sell => new[]
            {
                $"{stock.Name} 技术面出现顶背离信号，上涨动能不足，风险收益比不佳，建议减仓。",
                $"{stock.Symbol} 跌破重要支撑位，短期可能面临进一步下行压力，建议降低仓位规避风险。",
                $"{stock.Name} 近期出现利空消息，机构资金持续流出，基本面存在隐忧，建议及时止盈或止损。",
                $"{stock.Symbol} 估值偏高且增长预期下调，行业竞争加剧，建议逢高减持锁定收益。"
            },
            _ => new[] { "分析结果不确定，请结合其他信息综合判断。" }
        };

        var rand = new Random(stock.Symbol.GetHashCode() + DateTime.UtcNow.DayOfYear);
        return reasons[rand.Next(reasons.Length)];
    }
}
