using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockAnalyzer.Core.Interfaces;
using StockAnalyzer.Core.Services;
using StockAnalyzer.Infrastructure.Data;
using StockAnalyzer.Infrastructure.External;
using StockAnalyzer.Infrastructure.Repositories;

namespace StockAnalyzer.Infrastructure;

/// <summary>
/// 依赖注入扩展
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 配置数据库
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Data Source=stockanalyzer.db";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // 注册仓储
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        services.AddScoped<IAnalysisLogRepository, AnalysisLogRepository>();

        // 注册服务
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddScoped<IStatisticsService, StatisticsService>();

        // 配置 AI 服务
        var aiSettings = configuration.GetSection(AiSettings.SectionName).Get<AiSettings>() ?? new AiSettings();
        services.Configure<AiSettings>(configuration.GetSection(AiSettings.SectionName));

        if (aiSettings.Provider.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IAiService, MockAiService>();
        }
        else
        {
            // 配置 HttpClient for OpenAI
            services.AddHttpClient<IAiService, OpenAiService>(client =>
            {
                client.BaseAddress = new Uri(aiSettings.BaseUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {aiSettings.ApiKey}");
                client.Timeout = TimeSpan.FromSeconds(aiSettings.TimeoutSeconds);
            });
        }

        // 数据库初始化器
        services.AddScoped<DbInitializer>();

        return services;
    }
}
