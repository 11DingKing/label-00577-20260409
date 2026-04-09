using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Core.DTOs;
using StockAnalyzer.Core.Services;

namespace StockAnalyzer.Api.Controllers;

/// <summary>
/// 股票管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class StocksController : ControllerBase
{
    private readonly IStockService _stockService;
    private readonly ILogger<StocksController> _logger;

    public StocksController(IStockService stockService, ILogger<StocksController> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有股票
    /// </summary>
    /// <param name="includeInactive">是否包含非活跃股票</param>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<StockListResponse>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _stockService.GetAllStocksAsync(includeInactive);
        return Ok(ApiResponse<StockListResponse>.Ok(result));
    }

    /// <summary>
    /// 获取单个股票
    /// </summary>
    /// <param name="symbol">股票代码</param>
    [HttpGet("{symbol}")]
    public async Task<ActionResult<ApiResponse<StockResponse>>> GetBySymbol(string symbol)
    {
        var result = await _stockService.GetStockBySymbolAsync(symbol);
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "STOCK_NOT_FOUND",
                Message = $"股票 {symbol.ToUpperInvariant()} 不存在"
            });
        }
        return Ok(ApiResponse<StockResponse>.Ok(result));
    }

    /// <summary>
    /// 添加股票
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<StockResponse>>> Add([FromBody] AddStockRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "请求参数验证失败",
                ValidationErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>())
            });
        }

        try
        {
            var result = await _stockService.AddStockAsync(request);
            _logger.LogInformation("添加股票: {Symbol}", result.Symbol);
            return CreatedAtAction(nameof(GetBySymbol), new { symbol = result.Symbol }, 
                ApiResponse<StockResponse>.Ok(result, "股票添加成功"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse
            {
                Code = "STOCK_EXISTS",
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// 批量添加股票
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<List<StockResponse>>>> AddBatch([FromBody] BatchAddStocksRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "请求参数验证失败"
            });
        }

        var results = await _stockService.AddStocksAsync(request);
        _logger.LogInformation("批量添加股票: {Count} 只", results.Count);
        return Ok(ApiResponse<List<StockResponse>>.Ok(results, $"成功添加 {results.Count} 只股票"));
    }

    /// <summary>
    /// 更新股票
    /// </summary>
    /// <param name="symbol">股票代码</param>
    /// <param name="request">更新请求</param>
    [HttpPut("{symbol}")]
    public async Task<ActionResult<ApiResponse<StockResponse>>> Update(string symbol, [FromBody] UpdateStockRequest request)
    {
        var result = await _stockService.UpdateStockAsync(symbol, request);
        if (result == null)
        {
            return NotFound(new ErrorResponse
            {
                Code = "STOCK_NOT_FOUND",
                Message = $"股票 {symbol.ToUpperInvariant()} 不存在"
            });
        }
        
        _logger.LogInformation("更新股票: {Symbol}", symbol);
        return Ok(ApiResponse<StockResponse>.Ok(result, "股票更新成功"));
    }

    /// <summary>
    /// 删除股票
    /// </summary>
    /// <param name="symbol">股票代码</param>
    [HttpDelete("{symbol}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(string symbol)
    {
        var result = await _stockService.DeleteStockAsync(symbol);
        if (!result)
        {
            return NotFound(new ErrorResponse
            {
                Code = "STOCK_NOT_FOUND",
                Message = $"股票 {symbol.ToUpperInvariant()} 不存在"
            });
        }
        
        _logger.LogInformation("删除股票: {Symbol}", symbol);
        return Ok(ApiResponse<bool>.Ok(true, "股票删除成功"));
    }
}
