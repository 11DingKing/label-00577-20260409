using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace StockAnalyzer.Infrastructure.Data;

/// <summary>
/// 数据库初始化器
/// </summary>
public class DbInitializer
{
    private readonly AppDbContext _context;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(AppDbContext context, ILogger<DbInitializer> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 确保数据库已创建并应用迁移
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
            _logger.LogInformation("数据库初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库初始化失败");
            throw;
        }
    }
}
