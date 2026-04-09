<template>
  <div class="statistics-page">
    <!-- 连续建议查询 -->
    <div class="page-card">
      <div class="card-header">
        <span class="card-title">
          <el-icon><Search /></el-icon>
          连续建议查询
        </span>
        <el-tag type="info" effect="plain">核心统计功能</el-tag>
      </div>

      <div class="query-section">
        <div class="query-form">
          <div class="query-item">
            <label class="query-label">连续天数</label>
            <el-input-number
              v-model="queryDays"
              :min="2"
              :max="30"
              controls-position="right"
              class="query-input-number"
            />
          </div>
          <div class="query-item">
            <label class="query-label">建议类型</label>
            <el-select v-model="queryRecommendation" class="query-select">
              <el-option label="买入" :value="1">
                <el-tag type="success" effect="dark" size="small">买入</el-tag>
              </el-option>
              <el-option label="持有" :value="2">
                <el-tag type="warning" effect="dark" size="small">持有</el-tag>
              </el-option>
              <el-option label="卖出" :value="3">
                <el-tag type="danger" effect="dark" size="small">卖出</el-tag>
              </el-option>
            </el-select>
          </div>
          <div class="query-item query-action">
            <el-button
              type="primary"
              @click="searchConsecutive"
              class="query-btn"
            >
              <el-icon><Search /></el-icon>
              查询
            </el-button>
          </div>
        </div>

        <transition name="fade">
          <div v-if="consecutiveResult" class="result-banner">
            <el-icon class="result-icon"><Trophy /></el-icon>
            <div class="result-text">
              找到
              <strong>{{ consecutiveResult.totalFound }}</strong> 只股票连续
              <strong>{{ consecutiveResult.days }}</strong> 天以上
              <el-tag
                :type="getRecommendationType(consecutiveResult.recommendation)"
                effect="dark"
                round
              >
                {{ getRecommendationText(consecutiveResult.recommendation) }}
              </el-tag>
            </div>
          </div>
        </transition>
      </div>

      <el-table
        :data="consecutiveResult?.stocks || []"
        stripe
        :row-class-name="getRowClass"
        class="consecutive-table"
      >
        <el-table-column prop="symbol" label="股票" width="200">
          <template #default="{ row }">
            <div class="stock-cell">
              <span class="stock-symbol">{{ row.symbol }}</span>
              <span class="stock-name">{{ row.name }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column
          prop="consecutiveDays"
          label="连续天数"
          width="130"
          align="center"
        >
          <template #default="{ row }">
            <div class="days-badge">
              <el-icon><Calendar /></el-icon>
              <span>{{ row.consecutiveDays }} 天</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column
          prop="averageConfidence"
          label="平均置信度"
          width="200"
        >
          <template #default="{ row }">
            <div class="confidence-bar">
              <el-progress
                :percentage="Math.round(row.averageConfidence)"
                :stroke-width="10"
                :show-text="false"
                :color="getConfidenceColor(row.averageConfidence)"
              />
              <span class="confidence-value"
                >{{ row.averageConfidence?.toFixed(1) }}%</span
              >
            </div>
          </template>
        </el-table-column>
        <el-table-column label="分析周期" min-width="200">
          <template #default="{ row }">
            <div class="date-range">
              <span class="date">{{ row.startDate }}</span>
              <el-icon class="arrow"><Right /></el-icon>
              <span class="date">{{ row.endDate }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="80" fixed="right" align="center">
          <template #default="{ row }">
            <span class="trend-text" @click="viewTrend(row.symbol)">趋势</span>
          </template>
        </el-table-column>

        <template #empty>
          <div class="empty-state">
            <el-icon><Search /></el-icon>
            <h3>暂无数据</h3>
            <p>设置查询条件后点击"查询"</p>
          </div>
        </template>
      </el-table>
    </div>

    <!-- 统计汇总 & 趋势分析 -->
    <el-row :gutter="24" class="stats-row">
      <el-col :xs="24" :lg="12">
        <div class="page-card stats-card">
          <div class="card-header">
            <span class="card-title">
              <el-icon><PieChart /></el-icon>
              建议分布统计
            </span>
          </div>

          <div class="stats-grid">
            <div class="stat-item buy">
              <div class="stat-header">
                <el-icon><Top /></el-icon>
                <span>买入</span>
              </div>
              <div class="stat-body">
                <div class="stat-number">
                  {{ summary?.buySummary?.count || 0 }}
                </div>
                <div class="stat-meta">
                  <span class="percentage"
                    >{{
                      summary?.buySummary?.percentage?.toFixed(1) || 0
                    }}%</span
                  >
                  <span class="confidence"
                    >平均置信度
                    {{
                      summary?.buySummary?.averageConfidence?.toFixed(1) || 0
                    }}%</span
                  >
                </div>
              </div>
            </div>

            <div class="stat-item hold">
              <div class="stat-header">
                <el-icon><Minus /></el-icon>
                <span>持有</span>
              </div>
              <div class="stat-body">
                <div class="stat-number">
                  {{ summary?.holdSummary?.count || 0 }}
                </div>
                <div class="stat-meta">
                  <span class="percentage"
                    >{{
                      summary?.holdSummary?.percentage?.toFixed(1) || 0
                    }}%</span
                  >
                  <span class="confidence"
                    >平均置信度
                    {{
                      summary?.holdSummary?.averageConfidence?.toFixed(1) || 0
                    }}%</span
                  >
                </div>
              </div>
            </div>

            <div class="stat-item sell">
              <div class="stat-header">
                <el-icon><Bottom /></el-icon>
                <span>卖出</span>
              </div>
              <div class="stat-body">
                <div class="stat-number">
                  {{ summary?.sellSummary?.count || 0 }}
                </div>
                <div class="stat-meta">
                  <span class="percentage"
                    >{{
                      summary?.sellSummary?.percentage?.toFixed(1) || 0
                    }}%</span
                  >
                  <span class="confidence"
                    >平均置信度
                    {{
                      summary?.sellSummary?.averageConfidence?.toFixed(1) || 0
                    }}%</span
                  >
                </div>
              </div>
            </div>
          </div>
        </div>
      </el-col>

      <el-col :xs="24" :lg="12">
        <div class="page-card trend-card">
          <div class="card-header">
            <span class="card-title">
              <el-icon><TrendCharts /></el-icon>
              股票趋势分析
            </span>
          </div>

          <div class="trend-content">
            <div class="trend-form">
              <el-select
                v-model="trendSymbol"
                placeholder="选择股票查看趋势"
                filterable
                class="trend-select"
              >
                <el-option
                  v-for="stock in stocks"
                  :key="stock.symbol"
                  :label="`${stock.symbol} - ${stock.name}`"
                  :value="stock.symbol"
                />
              </el-select>
              <el-button
                type="primary"
                @click="fetchTrend"
                :disabled="!trendSymbol"
                class="trend-btn"
              >
                <el-icon><View /></el-icon>
                查看趋势
              </el-button>
            </div>

            <div class="trend-content-area">
              <div v-if="trendData" class="trend-result">
                <div class="trend-header">
                  <div class="trend-title">
                    <span class="trend-symbol">{{ trendData.symbol }}</span>
                    <span class="trend-name">{{ trendData.name }}</span>
                  </div>
                  <el-tag
                    :type="
                      getRecommendationType(
                        trendData.summary?.dominantRecommendation,
                      )
                    "
                    effect="dark"
                    round
                    size="large"
                  >
                    {{
                      getRecommendationText(
                        trendData.summary?.dominantRecommendation,
                      )
                    }}
                  </el-tag>
                </div>

                <div class="trend-stats">
                  <div class="trend-stat">
                    <span class="stat-value">{{
                      trendData.summary?.totalDays || 0
                    }}</span>
                    <span class="stat-label">分析天数</span>
                  </div>
                  <div class="trend-stat buy">
                    <span class="stat-value">{{
                      trendData.summary?.buyDays || 0
                    }}</span>
                    <span class="stat-label">买入</span>
                  </div>
                  <div class="trend-stat hold">
                    <span class="stat-value">{{
                      trendData.summary?.holdDays || 0
                    }}</span>
                    <span class="stat-label">持有</span>
                  </div>
                  <div class="trend-stat sell">
                    <span class="stat-value">{{
                      trendData.summary?.sellDays || 0
                    }}</span>
                    <span class="stat-label">卖出</span>
                  </div>
                  <div class="trend-stat confidence">
                    <span class="stat-value"
                      >{{ trendData.summary?.averageConfidence || 0 }}%</span
                    >
                    <span class="stat-label">平均置信度</span>
                  </div>
                </div>

                <div class="chart-container" ref="trendChartRef"></div>
              </div>

              <div v-else class="empty-trend">
                <el-icon><TrendCharts /></el-icon>
                <p>选择股票查看趋势分析图表</p>
              </div>
            </div>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from "vue";
import { ElMessage, ElLoading } from "element-plus";
import {
  Search,
  PieChart,
  TrendCharts,
  Trophy,
  Calendar,
  Top,
  Minus,
  Bottom,
  View,
  Right,
} from "@element-plus/icons-vue";
import * as echarts from "echarts";
import { useStatisticsStore, useStockStore } from "@/stores";
import { logger } from "@/utils/logger";

const statisticsStore = useStatisticsStore();
const stockStore = useStockStore();

const summary = ref(statisticsStore.summary);
const stocks = ref(stockStore.stocks);
const queryDays = ref(3);
const queryRecommendation = ref(1);
const consecutiveResult = ref<any>(null);
const trendSymbol = ref("");
const trendData = ref<any>(null);
const trendChartRef = ref<HTMLElement>();

const getRecommendationType = (rec: string) => {
  const map: Record<string, "success" | "warning" | "danger"> = {
    Buy: "success",
    Hold: "warning",
    Sell: "danger",
  };
  return map[rec] || "info";
};

const getRecommendationText = (rec: string) => {
  const map: Record<string, string> = {
    Buy: "买入",
    Hold: "持有",
    Sell: "卖出",
  };
  return map[rec] || rec;
};

const getConfidenceColor = (confidence: number) => {
  if (confidence >= 80) return "#10b981";
  if (confidence >= 60) return "#f59e0b";
  return "#ef4444";
};

const getRowClass = ({ rowIndex }: { rowIndex: number }) => {
  return rowIndex === 0 ? "highlight-row" : "";
};

const searchConsecutive = async () => {
  logger.userAction("查询连续建议", {
    days: queryDays.value,
    recommendation: queryRecommendation.value,
  });

  const loading = ElLoading.service({
    lock: true,
    text: "查询中...",
    background: "rgba(255, 255, 255, 0.9)",
  });

  try {
    consecutiveResult.value = await statisticsStore.getConsecutive(
      queryDays.value,
      queryRecommendation.value,
    );
    logger.info("连续建议查询完成", {
      found: consecutiveResult.value.totalFound,
    });
  } catch (error) {
    logger.error("查询失败", error);
    ElMessage.error("查询失败");
  } finally {
    loading.close();
  }
};

const viewTrend = async (symbol: string) => {
  trendSymbol.value = symbol;
  const loading = ElLoading.service({
    lock: true,
    text: "加载趋势数据中...",
    background: "rgba(255, 255, 255, 0.9)",
  });
  try {
    await fetchTrend();
  } finally {
    loading.close();
  }
};

const fetchTrend = async () => {
  if (!trendSymbol.value) return;
  logger.userAction("查看趋势", { symbol: trendSymbol.value });

  const loading = ElLoading.service({
    lock: true,
    text: "加载趋势数据中...",
    background: "rgba(255, 255, 255, 0.9)",
  });

  try {
    // 添加最小延迟以显示 loading 状态
    const [result] = await Promise.all([
      statisticsStore.getTrend(trendSymbol.value, 30),
      new Promise((resolve) => setTimeout(resolve, 400)),
    ]);
    trendData.value = result;
    await nextTick();
    initTrendChart();
  } catch (error) {
    logger.error("获取趋势失败", error);
    ElMessage.error("获取趋势失败");
  } finally {
    loading.close();
  }
};

const initTrendChart = () => {
  if (!trendChartRef.value || !trendData.value?.trendData) return;

  const chart = echarts.init(trendChartRef.value);
  const data = trendData.value.trendData;

  chart.setOption({
    tooltip: {
      trigger: "axis",
      backgroundColor: "rgba(255, 255, 255, 0.95)",
      borderColor: "#e5e7eb",
      borderWidth: 1,
      textStyle: { color: "#1f2937" },
    },
    grid: { top: 40, right: 60, bottom: 60, left: 60 },
    xAxis: {
      type: "category",
      data: data.map((d: any) => d.date),
      axisLabel: { rotate: 45, fontSize: 11 },
    },
    yAxis: [
      {
        type: "category",
        data: ["卖出", "持有", "买入"],
        position: "left",
        axisLabel: { fontSize: 12, fontWeight: "bold" },
      },
      {
        type: "value",
        min: 0,
        max: 100,
        position: "right",
        name: "置信度",
        axisLabel: { formatter: "{value}%" },
      },
    ],
    series: [
      {
        name: "建议",
        type: "scatter",
        yAxisIndex: 0,
        symbolSize: 18,
        data: data.map((d: any) => {
          const y =
            d.recommendation === "Buy"
              ? "买入"
              : d.recommendation === "Hold"
                ? "持有"
                : "卖出";
          return {
            value: [d.date, y],
            itemStyle: {
              color:
                d.recommendation === "Buy"
                  ? "#10b981"
                  : d.recommendation === "Hold"
                    ? "#f59e0b"
                    : "#ef4444",
              shadowBlur: 8,
              shadowColor: "rgba(0,0,0,0.15)",
            },
          };
        }),
      },
      {
        name: "置信度",
        type: "line",
        yAxisIndex: 1,
        smooth: true,
        data: data.map((d: any) => d.confidence),
        lineStyle: { color: "#3b82f6", width: 3 },
        itemStyle: { color: "#3b82f6" },
        areaStyle: {
          color: {
            type: "linear",
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              { offset: 0, color: "rgba(59, 130, 246, 0.3)" },
              { offset: 1, color: "rgba(59, 130, 246, 0)" },
            ],
          },
        },
      },
    ],
  });

  window.addEventListener("resize", () => chart.resize());
};

onMounted(async () => {
  logger.info("Statistics 页面加载");
  await Promise.all([statisticsStore.fetchSummary(), stockStore.fetchStocks()]);
  summary.value = statisticsStore.summary;
  stocks.value = stockStore.stocks;
});
</script>

<style lang="scss" scoped>
.statistics-page {
  .query-section {
    margin-bottom: 24px;
  }

  .query-form {
    display: flex;
    align-items: center;
    gap: 24px;
    margin-bottom: 20px;
    padding: 20px 24px;
    background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
    border-radius: var(--radius-lg);
    flex-wrap: wrap;
  }

  .query-item {
    display: flex;
    align-items: center;
    gap: 12px;
  }

  .query-label {
    font-size: 14px;
    font-weight: 500;
    color: var(--text-secondary);
    white-space: nowrap;
  }

  .query-input-number {
    width: 130px;

    :deep(.el-input__wrapper) {
      border-radius: var(--radius-md);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
      background: white;
    }
  }

  .query-select {
    width: 140px;

    :deep(.el-input__wrapper) {
      border-radius: var(--radius-md);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.06);
      background: white;
    }
  }

  .query-action {
    margin-left: auto;
  }

  .query-btn {
    min-width: 100px;
    height: 40px;
    font-size: 14px;
    font-weight: 600;
    border-radius: var(--radius-md);
    background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
    border: none;
    box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);
    transition: all 0.2s ease;

    &:hover:not(:disabled) {
      background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
      transform: translateY(-1px);
      box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
    }

    &:active:not(:disabled) {
      transform: translateY(0);
    }

    .el-icon {
      margin-right: 6px;
    }
  }

  // 趋势文字
  .trend-text {
    color: #3b82f6;
    cursor: pointer;

    &:hover {
      color: #2563eb;
    }
  }

  .result-banner {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 16px 20px;
    background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
    border-radius: var(--radius-md);
    border: 1px solid #bae6fd;

    .result-icon {
      font-size: 28px;
      color: #0284c7;
    }

    .result-text {
      font-size: 15px;
      color: #0c4a6e;

      strong {
        color: #0369a1;
      }
    }
  }

  // 统计行高度一致
  .stats-row {
    align-items: stretch;

    .el-col {
      margin-bottom: 24px;
      display: flex;
    }
  }

  .stats-card,
  .trend-card {
    flex: 1;
    min-height: 520px;
    display: flex;
    flex-direction: column;

    .card-header {
      flex-shrink: 0;
    }
  }

  .stats-card {
    .stats-grid {
      flex: 1;
      display: flex;
      flex-direction: column;
      justify-content: center;
    }
  }

  .trend-card {
    .trend-content {
      flex: 1;
      display: flex;
      flex-direction: column;
    }
  }

  .stats-grid {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 16px;

    .stat-item {
      background: #fff;
      border-radius: var(--radius-md);
      padding: 20px;
      border: 1px solid var(--border-light);
      transition: all var(--transition-normal);

      &:hover {
        transform: translateY(-2px);
        box-shadow: var(--shadow-md);
      }

      &.buy {
        border-left: 4px solid #10b981;
        .stat-header {
          color: #047857;
        }
        .stat-number {
          color: #10b981;
        }
      }

      &.hold {
        border-left: 4px solid #f59e0b;
        .stat-header {
          color: #b45309;
        }
        .stat-number {
          color: #f59e0b;
        }
      }

      &.sell {
        border-left: 4px solid #ef4444;
        .stat-header {
          color: #dc2626;
        }
        .stat-number {
          color: #ef4444;
        }
      }

      .stat-header {
        display: flex;
        align-items: center;
        gap: 8px;
        font-weight: 600;
        font-size: 14px;
        margin-bottom: 12px;
      }

      .stat-body {
        .stat-number {
          font-size: 32px;
          font-weight: 800;
          line-height: 1;
          margin-bottom: 8px;
        }

        .stat-meta {
          display: flex;
          flex-direction: column;
          gap: 4px;
          font-size: 12px;
          color: var(--text-secondary);

          .percentage {
            font-weight: 600;
          }
        }
      }
    }
  }

  .trend-form {
    display: flex;
    gap: 12px;

    .trend-select {
      flex: 1;

      :deep(.el-input__wrapper) {
        height: 40px;
      }
    }

    .trend-btn {
      min-width: 110px;
      height: 40px;
    }
  }

  .trend-card {
    .trend-content {
      display: flex;
      flex-direction: column;
      height: 100%;
    }

    .trend-form {
      margin-bottom: 20px;
      flex-shrink: 0;
    }

    .trend-content-area {
      flex: 1;
      display: flex;
      flex-direction: column;
      min-height: 350px;
    }
  }

  .trend-result {
    flex: 1;
    display: flex;
    flex-direction: column;
    min-height: 350px;

    .trend-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 16px;
      padding-bottom: 16px;
      border-bottom: 1px solid #f1f5f9;

      .trend-title {
        display: flex;
        align-items: baseline;
        gap: 12px;
      }

      .trend-symbol {
        font-size: 22px;
        font-weight: 800;
        color: var(--primary-color);
        letter-spacing: -0.02em;
      }

      .trend-name {
        color: var(--text-secondary);
        font-size: 14px;
      }
    }

    .trend-stats {
      display: flex;
      gap: 12px;
      margin-bottom: 20px;

      .trend-stat {
        flex: 1;
        text-align: center;
        padding: 12px 8px;
        background: #f8fafc;
        border-radius: 10px;
        border: 1px solid #e2e8f0;

        .stat-value {
          display: block;
          font-size: 20px;
          font-weight: 700;
          color: #334155;
          margin-bottom: 4px;
        }

        .stat-label {
          font-size: 11px;
          color: #94a3b8;
          font-weight: 500;
        }

        &.buy {
          background: #ecfdf5;
          border-color: #a7f3d0;
          .stat-value {
            color: #059669;
          }
        }

        &.hold {
          background: #fffbeb;
          border-color: #fde68a;
          .stat-value {
            color: #d97706;
          }
        }

        &.sell {
          background: #fef2f2;
          border-color: #fecaca;
          .stat-value {
            color: #dc2626;
          }
        }

        &.confidence {
          background: #eff6ff;
          border-color: #bfdbfe;
          .stat-value {
            color: #2563eb;
          }
        }
      }
    }

    .trend-summary {
      margin-bottom: 20px;

      .text-success {
        color: #10b981;
        font-weight: 600;
      }
      .text-warning {
        color: #f59e0b;
        font-weight: 600;
      }
      .text-danger {
        color: #ef4444;
        font-weight: 600;
      }
    }
  }

  .empty-trend {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    text-align: center;
    padding: 40px 20px;
    color: var(--text-secondary);
    min-height: 350px;

    .el-icon {
      font-size: 48px;
      margin-bottom: 12px;
      color: var(--text-muted);
    }
  }

  .chart-container {
    flex: 1;
    min-height: 250px;
  }

  :deep(.highlight-row) {
    background-color: #f0fdf4 !important;
  }

  // 连续建议表格优化
  .consecutive-table {
    .stock-cell {
      display: flex;
      flex-direction: column;
      gap: 4px;

      .stock-symbol {
        display: inline-block;
        font-weight: 700;
        color: var(--primary-color);
        font-family: "SF Mono", "Monaco", "Consolas", monospace;
        letter-spacing: 0.5px;
        padding: 4px 10px;
        background: linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%);
        border-radius: var(--radius-sm);
        font-size: 13px;
        width: fit-content;
      }

      .stock-name {
        font-size: 13px;
        color: var(--text-secondary);
        padding-left: 2px;
      }
    }

    .days-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 8px 14px;
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: white;
      border-radius: 20px;
      font-weight: 700;
      font-size: 14px;
      box-shadow: 0 2px 8px rgba(59, 130, 246, 0.3);

      .el-icon {
        font-size: 14px;
      }
    }

    .date-range {
      display: flex;
      align-items: center;
      gap: 8px;

      .date {
        padding: 6px 12px;
        background: #f8fafc;
        border-radius: 6px;
        font-size: 13px;
        font-weight: 500;
        color: #475569;
        border: 1px solid #e2e8f0;
      }

      .arrow {
        color: #94a3b8;
        font-size: 14px;
      }
    }
  }
}

.fade-enter-active,
.fade-leave-active {
  transition: all 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
