using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Infrastructure.Data;

namespace StockAnalyzer.Api.Controllers;

/// <summary>
/// 健康检查控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAiService _aiService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        AppDbContext dbContext,
        IAiService aiService,
        ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// 基础健康检查
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// 就绪检查（含数据库连接）
    /// </summary>
    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var checks = new Dictionary<string, object>();
        var isReady = true;

        // 检查数据库连接
        try
        {
            await _dbContext.Database.CanConnectAsync();
            checks["database"] = new { Status = "Connected" };
        }
        catch (Exception ex)
        {
            checks["database"] = new { Status = "Disconnected", Error = ex.Message };
            isReady = false;
            _logger.LogWarning(ex, "数据库连接检查失败");
        }

        // 检查 AI 服务
        try
        {
            var aiAvailable = await _aiService.IsAvailableAsync();
            checks["aiService"] = new { Status = aiAvailable ? "Available" : "Unavailable" };
            if (!aiAvailable)
            {
                _logger.LogWarning("AI 服务不可用");
            }
        }
        catch (Exception ex)
        {
            checks["aiService"] = new { Status = "Error", Error = ex.Message };
            _logger.LogWarning(ex, "AI 服务检查失败");
        }

        var result = new
        {
            Status = isReady ? "Ready" : "NotReady",
            Timestamp = DateTime.UtcNow,
            Checks = checks
        };

        return isReady ? Ok(result) : StatusCode(503, result);
    }
}
