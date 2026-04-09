import { describe, it, expect, beforeEach, vi } from "vitest";
import { setActivePinia, createPinia } from "pinia";
import { useStockStore, useAnalysisStore, useStatisticsStore } from "@/stores";

// Mock API
vi.mock("@/api", () => ({
  stockApi: {
    getAll: vi.fn().mockResolvedValue({
      data: {
        stocks: [
          {
            id: 1,
            symbol: "AAPL",
            name: "Apple Inc.",
            isActive: true,
            analysisCount: 5,
          },
          {
            id: 2,
            symbol: "GOOGL",
            name: "Alphabet",
            isActive: true,
            analysisCount: 3,
          },
        ],
        total: 2,
      },
    }),
    add: vi.fn().mockResolvedValue({
      data: {
        id: 3,
        symbol: "MSFT",
        name: "Microsoft",
        isActive: true,
        analysisCount: 0,
      },
    }),
    update: vi.fn().mockResolvedValue({
      data: {
        id: 1,
        symbol: "AAPL",
        name: "Apple Inc. Updated",
        isActive: true,
        analysisCount: 5,
      },
    }),
    delete: vi.fn().mockResolvedValue({ data: true }),
    batchAdd: vi.fn().mockResolvedValue({
      data: [
        { id: 4, symbol: "TSLA", name: "Tesla", isActive: true },
        { id: 5, symbol: "AMZN", name: "Amazon", isActive: true },
      ],
    }),
  },
  analysisApi: {
    run: vi.fn().mockResolvedValue({
      data: {
        totalStocks: 2,
        successCount: 2,
        failureCount: 0,
        skippedCount: 0,
        durationMs: 1500,
        results: [
          { id: 1, symbol: "AAPL", recommendation: "Buy", confidence: 75 },
          { id: 2, symbol: "GOOGL", recommendation: "Hold", confidence: 60 },
        ],
      },
    }),
    runSingle: vi.fn().mockResolvedValue({
      data: { id: 10, symbol: "AAPL", recommendation: "Buy", confidence: 80 },
    }),
    getResults: vi.fn().mockResolvedValue({
      data: {
        results: [
          { id: 1, symbol: "AAPL", recommendation: "Buy", confidence: 75 },
        ],
        total: 1,
        page: 1,
        pageSize: 20,
      },
    }),
    getLatest: vi.fn().mockResolvedValue({
      data: [{ id: 1, symbol: "AAPL", recommendation: "Buy", confidence: 75 }],
    }),
  },
  statisticsApi: {
    getSummary: vi.fn().mockResolvedValue({
      data: {
        totalStocks: 10,
        totalAnalysis: 100,
        buySummary: { count: 40, percentage: 40, averageConfidence: 72 },
        holdSummary: { count: 35, percentage: 35, averageConfidence: 65 },
        sellSummary: { count: 25, percentage: 25, averageConfidence: 70 },
      },
    }),
    getConsecutive: vi.fn().mockResolvedValue({
      data: {
        days: 3,
        recommendation: "Buy",
        stocks: [{ symbol: "AAPL", consecutiveDays: 5 }],
        totalFound: 1,
      },
    }),
    getTrend: vi.fn().mockResolvedValue({
      data: {
        symbol: "AAPL",
        name: "Apple",
        trendData: [],
        summary: { totalDays: 30, buyDays: 15, holdDays: 10, sellDays: 5 },
      },
    }),
  },
}));

describe("Stock Store", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("应该获取股票列表", async () => {
    const store = useStockStore();
    await store.fetchStocks();

    expect(store.stocks.length).toBe(2);
    expect(store.stocks[0].symbol).toBe("AAPL");
  });

  it("应该计算总数", async () => {
    const store = useStockStore();
    await store.fetchStocks();

    expect(store.total).toBe(2);
  });

  it("应该添加股票", async () => {
    const store = useStockStore();
    const stock = await store.addStock("MSFT", "Microsoft");

    expect(stock.symbol).toBe("MSFT");
    expect(store.stocks[0].symbol).toBe("MSFT");
  });

  it("应该更新股票", async () => {
    const store = useStockStore();
    await store.fetchStocks();

    const updated = await store.updateStock("AAPL", {
      name: "Apple Inc. Updated",
    });

    expect(updated.name).toBe("Apple Inc. Updated");
  });

  it("应该删除股票", async () => {
    const store = useStockStore();
    await store.fetchStocks();

    await store.deleteStock("AAPL");

    expect(store.stocks.find((s) => s.symbol === "AAPL")).toBeUndefined();
  });
});

describe("Analysis Store", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("应该运行批量分析", async () => {
    const store = useAnalysisStore();
    const result = await store.runAnalysis();

    expect(result.totalStocks).toBe(2);
    expect(result.successCount).toBe(2);
    expect(result.results.length).toBe(2);
  });

  it("应该运行单股分析", async () => {
    const store = useAnalysisStore();
    const result = await store.runSingleAnalysis("AAPL");

    expect(result.symbol).toBe("AAPL");
    expect(result.recommendation).toBe("Buy");
  });

  it("应该获取分析结果", async () => {
    const store = useAnalysisStore();
    const result = await store.fetchResults({ page: 1, pageSize: 20 });

    expect(result.results.length).toBe(1);
    expect(result.total).toBe(1);
  });

  it("应该获取最新结果", async () => {
    const store = useAnalysisStore();
    await store.fetchLatest(10);

    expect(store.latestResults.length).toBe(1);
  });
});

describe("Statistics Store", () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it("应该获取统计摘要", async () => {
    const store = useStatisticsStore();
    const summary = await store.fetchSummary();

    expect(summary.totalStocks).toBe(10);
    expect(summary.totalAnalysis).toBe(100);
    expect(summary.buySummary.count).toBe(40);
  });

  it("应该查询连续建议", async () => {
    const store = useStatisticsStore();
    const result = await store.getConsecutive(3, 1);

    expect(result.days).toBe(3);
    expect(result.stocks.length).toBe(1);
  });

  it("应该获取趋势数据", async () => {
    const store = useStatisticsStore();
    const result = await store.getTrend("AAPL", 30);

    expect(result.symbol).toBe("AAPL");
    expect(result.summary.totalDays).toBe(30);
  });
});
