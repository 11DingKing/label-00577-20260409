using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Core.Models;

namespace StockAnalyzer.Infrastructure.Data;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();
    public DbSet<AnalysisLog> AnalysisLogs => Set<AnalysisLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Symbol).IsUnique();
            entity.Property(e => e.Symbol).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<AnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StockId, e.AnalysisDate });
            entity.Property(e => e.Confidence).HasPrecision(5, 2);
            entity.Property(e => e.Reasoning).HasMaxLength(2000);

            entity.HasOne(e => e.Stock)
                  .WithMany(s => s.AnalysisResults)
                  .HasForeignKey(e => e.StockId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AnalysisLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExecutedAt);
        });
    }
}
