import { defineStore } from "pinia";
import { ref, computed } from "vue";
import {
  stockApi,
  analysisApi,
  statisticsApi,
  type Stock,
  type AnalysisResult,
  type StatisticsSummary,
} from "@/api";

// 股票 Store
export const useStockStore = defineStore("stock", () => {
  const stocks = ref<Stock[]>([]);
  const loading = ref(false);
  const total = computed(() => stocks.value.length);
  const activeCount = computed(
    () => stocks.value.filter((s) => s.isActive).length,
  );

  const fetchStocks = async (includeInactive = false) => {
    loading.value = true;
    try {
      const res = await stockApi.getAll(includeInactive);
      stocks.value = res.data.stocks;
    } finally {
      loading.value = false;
    }
  };

  const addStock = async (symbol: string, name: string) => {
    const res = await stockApi.add({ symbol, name });
    stocks.value.unshift(res.data);
    return res.data;
  };

  const batchAddStocks = async (list: { symbol: string; name: string }[]) => {
    const res = await stockApi.batchAdd(list);
    await fetchStocks();
    return res.data;
  };

  const updateStock = async (
    symbol: string,
    data: { name?: string; isActive?: boolean },
  ) => {
    const res = await stockApi.update(symbol, data);
    const index = stocks.value.findIndex((s) => s.symbol === symbol);
    if (index !== -1) {
      stocks.value[index] = res.data;
    }
    return res.data;
  };

  const deleteStock = async (symbol: string) => {
    await stockApi.delete(symbol);
    stocks.value = stocks.value.filter((s) => s.symbol !== symbol);
  };

  return {
    stocks,
    loading,
    total,
    activeCount,
    fetchStocks,
    addStock,
    batchAddStocks,
    updateStock,
    deleteStock,
  };
});

// 分析 Store
export const useAnalysisStore = defineStore("analysis", () => {
  const results = ref<AnalysisResult[]>([]);
  const latestResults = ref<AnalysisResult[]>([]);
  const loading = ref(false);
  const analyzing = ref(false);

  const runAnalysis = async (symbols?: string[], forceRerun = false) => {
    analyzing.value = true;
    try {
      const res = await analysisApi.run(symbols, forceRerun);
      await fetchLatest();
      return res.data;
    } finally {
      analyzing.value = false;
    }
  };

  const runSingleAnalysis = async (symbol: string) => {
    analyzing.value = true;
    try {
      const res = await analysisApi.runSingle(symbol);
      await fetchLatest();
      return res.data;
    } finally {
      analyzing.value = false;
    }
  };

  const fetchResults = async (params: {
    page?: number;
    pageSize?: number;
    symbol?: string;
    startDate?: string;
    endDate?: string;
    recommendation?: number;
  }) => {
    loading.value = true;
    try {
      const res = await analysisApi.getResults(params);
      results.value = res.data.results;
      return res.data;
    } finally {
      loading.value = false;
    }
  };

  const fetchLatest = async (count = 20) => {
    const res = await analysisApi.getLatest(count);
    latestResults.value = res.data;
    return res.data;
  };

  return {
    results,
    latestResults,
    loading,
    analyzing,
    runAnalysis,
    runSingleAnalysis,
    fetchResults,
    fetchLatest,
  };
});

// 统计 Store
export const useStatisticsStore = defineStore("statistics", () => {
  const summary = ref<StatisticsSummary | null>(null);
  const loading = ref(false);

  const fetchSummary = async () => {
    loading.value = true;
    try {
      const res = await statisticsApi.getSummary();
      summary.value = res.data;
      return res.data;
    } finally {
      loading.value = false;
    }
  };

  const getConsecutive = async (days: number, recommendation: number) => {
    const res = await statisticsApi.getConsecutive(days, recommendation);
    return res.data;
  };

  const getTrend = async (symbol: string, days = 30) => {
    const res = await statisticsApi.getTrend(symbol, days);
    return res.data;
  };

  return {
    summary,
    loading,
    fetchSummary,
    getConsecutive,
    getTrend,
  };
});
