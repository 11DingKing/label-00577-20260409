import axios, { AxiosResponse, InternalAxiosRequestConfig } from "axios";
import { ElMessage } from "element-plus";
import { logger } from "@/utils/logger";

// 创建 axios 实例
const request = axios.create({
  baseURL: "/api",
  timeout: 30000,
});

// 请求拦截器
request.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    logger.apiRequest(
      config.method || "GET",
      config.url || "",
      config.params || config.data,
    );
    return config;
  },
  (error) => {
    logger.error("请求配置错误", error);
    return Promise.reject(error);
  },
);

// 响应拦截器
request.interceptors.response.use(
  (response: AxiosResponse) => {
    const { config, status, data } = response;
    logger.apiResponse(config.method || "GET", config.url || "", status, data);

    if (data.success === false) {
      logger.warn("业务逻辑错误", { url: config.url, message: data.message });
      ElMessage.error(data.message || "请求失败");
      return Promise.reject(new Error(data.message));
    }
    return data;
  },
  (error) => {
    const { config, response } = error;
    const url = config?.url || "unknown";
    const status = response?.status || 0;
    const message = response?.data?.message || error.message || "网络错误";

    logger.apiError(config?.method || "GET", url, { status, message, error });

    // 根据状态码显示不同提示
    if (status === 0) {
      ElMessage.error("网络连接失败，请检查后端服务是否启动");
    } else if (status === 404) {
      ElMessage.error("请求的资源不存在");
    } else if (status === 500) {
      ElMessage.error("服务器内部错误");
    } else {
      ElMessage.error(message);
    }

    return Promise.reject(error);
  },
);

// 类型定义
export interface Stock {
  id: number;
  symbol: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  analysisCount: number;
  latestAnalysis?: AnalysisResult;
}

export interface AnalysisResult {
  id: number;
  symbol: string;
  stockName: string;
  analysisDate: string;
  recommendation: "Buy" | "Hold" | "Sell";
  confidence: number;
  reasoning: string;
  createdAt: string;
}

export interface RunAnalysisResponse {
  totalStocks: number;
  successCount: number;
  failureCount: number;
  skippedCount: number;
  durationMs: number;
  results: AnalysisResult[];
  errors?: { symbol: string; errorMessage: string }[];
}

export interface StatisticsSummary {
  totalStocks: number;
  totalAnalysis: number;
  firstAnalysisDate?: string;
  lastAnalysisDate?: string;
  buySummary: { count: number; percentage: number; averageConfidence: number };
  holdSummary: { count: number; percentage: number; averageConfidence: number };
  sellSummary: { count: number; percentage: number; averageConfidence: number };
}

export interface ConsecutiveStock {
  symbol: string;
  name: string;
  consecutiveDays: number;
  recommendation: string;
  averageConfidence: number;
  startDate: string;
  endDate: string;
  recentAnalysis: AnalysisResult[];
}

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

// 健康检查 API
export const healthApi = {
  check: () => {
    logger.debug("调用健康检查 API");
    return request.get("/health");
  },
  ready: () => {
    logger.debug("调用就绪检查 API");
    return request.get("/health/ready").then((res) => res.data || res);
  },
};

// 股票管理 API
export const stockApi = {
  getAll: (
    includeInactive = false,
  ): Promise<ApiResponse<{ stocks: Stock[]; total: number }>> => {
    logger.info("获取股票列表", { includeInactive });
    return request.get("/stocks", { params: { includeInactive } });
  },

  getBySymbol: (symbol: string): Promise<ApiResponse<Stock>> => {
    logger.info("获取股票详情", { symbol });
    return request.get(`/stocks/${symbol}`);
  },

  add: (data: {
    symbol: string;
    name: string;
  }): Promise<ApiResponse<Stock>> => {
    logger.userAction("添加股票", data);
    return request.post("/stocks", data);
  },

  batchAdd: (
    stocks: { symbol: string; name: string }[],
  ): Promise<ApiResponse<Stock[]>> => {
    logger.userAction("批量添加股票", { count: stocks.length });
    return request.post("/stocks/batch", { stocks });
  },

  update: (
    symbol: string,
    data: { name?: string; isActive?: boolean },
  ): Promise<ApiResponse<Stock>> => {
    logger.userAction("更新股票", { symbol, ...data });
    return request.put(`/stocks/${symbol}`, data);
  },

  delete: (symbol: string): Promise<ApiResponse<boolean>> => {
    logger.userAction("删除股票", { symbol });
    return request.delete(`/stocks/${symbol}`);
  },
};

// AI 分析 API
export const analysisApi = {
  run: (
    symbols?: string[],
    forceRerun = false,
  ): Promise<ApiResponse<RunAnalysisResponse>> => {
    logger.userAction("执行批量分析", { symbols, forceRerun });
    return request.post("/analysis/run", { symbols, forceRerun });
  },

  runSingle: (symbol: string): Promise<ApiResponse<AnalysisResult>> => {
    logger.userAction("执行单股分析", { symbol });
    return request.post(`/analysis/run/${symbol}`);
  },

  getResults: (params: {
    page?: number;
    pageSize?: number;
    symbol?: string;
    startDate?: string;
    endDate?: string;
    recommendation?: number;
  }): Promise<
    ApiResponse<{
      results: AnalysisResult[];
      total: number;
      page: number;
      pageSize: number;
    }>
  > => {
    logger.debug("查询分析结果", params);
    return request.get("/analysis/results", { params });
  },

  getBySymbol: (
    symbol: string,
    limit = 30,
  ): Promise<ApiResponse<AnalysisResult[]>> => {
    logger.debug("获取股票分析历史", { symbol, limit });
    return request.get(`/analysis/results/${symbol}`, { params: { limit } });
  },

  getLatest: (count = 50): Promise<ApiResponse<AnalysisResult[]>> => {
    logger.debug("获取最新分析结果", { count });
    return request.get("/analysis/latest", { params: { count } });
  },
};

// 统计 API
export const statisticsApi = {
  getSummary: (): Promise<ApiResponse<StatisticsSummary>> => {
    logger.debug("获取统计汇总");
    return request.get("/statistics/summary");
  },

  getConsecutive: (
    days: number,
    recommendation: number,
    endDate?: string,
  ): Promise<
    ApiResponse<{
      days: number;
      recommendation: string;
      stocks: ConsecutiveStock[];
      totalFound: number;
    }>
  > => {
    logger.userAction("查询连续建议", { days, recommendation, endDate });
    return request.get("/statistics/consecutive", {
      params: { days, recommendation, endDate },
    });
  },

  getTrend: (
    symbol: string,
    days = 30,
  ): Promise<
    ApiResponse<{
      symbol: string;
      name: string;
      trendData: { date: string; recommendation: string; confidence: number }[];
      summary: {
        totalDays: number;
        buyDays: number;
        holdDays: number;
        sellDays: number;
        dominantRecommendation: string;
        averageConfidence: number;
      };
    }>
  > => {
    logger.debug("获取股票趋势", { symbol, days });
    return request.get(`/statistics/trend/${symbol}`, { params: { days } });
  },
};

export default request;
