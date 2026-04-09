<template>
  <div class="dashboard">
    <!-- 欢迎横幅 -->
    <div class="welcome-banner animate-fade-in">
      <div class="banner-content">
        <h2>欢迎使用 Stock AI Analyzer</h2>
        <p>智能股票分析工具，基于 AI 提供投资建议</p>
      </div>
      <div class="banner-illustration">
        <el-icon size="80"><TrendCharts /></el-icon>
      </div>
    </div>

    <!-- 统计卡片 -->
    <el-row :gutter="24" class="stat-row">
      <el-col :xs="24" :sm="12" :lg="6">
        <div class="stat-card animate-fade-in" style="animation-delay: 0.1s">
          <div class="stat-content">
            <div class="stat-value">{{ summary?.totalStocks || 0 }}</div>
            <div class="stat-label">股票总数</div>
            <div class="stat-percentage">活跃监控中</div>
          </div>
          <el-icon class="stat-icon"><Collection /></el-icon>
        </div>
      </el-col>
      <el-col :xs="24" :sm="12" :lg="6">
        <div
          class="stat-card success animate-fade-in"
          style="animation-delay: 0.15s"
        >
          <div class="stat-content">
            <div class="stat-value">{{ summary?.totalAnalysis || 0 }}</div>
            <div class="stat-label">分析次数</div>
            <div class="stat-percentage">累计分析</div>
          </div>
          <el-icon class="stat-icon"><DataAnalysis /></el-icon>
        </div>
      </el-col>
      <el-col :xs="24" :sm="12" :lg="6">
        <div
          class="stat-card warning animate-fade-in"
          style="animation-delay: 0.2s"
        >
          <div class="stat-content">
            <div class="stat-value">{{ summary?.buySummary?.count || 0 }}</div>
            <div class="stat-label">买入建议</div>
            <div class="stat-percentage">
              {{ summary?.buySummary?.percentage?.toFixed(1) || 0 }}%
            </div>
          </div>
          <el-icon class="stat-icon"><Top /></el-icon>
        </div>
      </el-col>
      <el-col :xs="24" :sm="12" :lg="6">
        <div
          class="stat-card danger animate-fade-in"
          style="animation-delay: 0.25s"
        >
          <div class="stat-content">
            <div class="stat-value">{{ summary?.sellSummary?.count || 0 }}</div>
            <div class="stat-label">卖出建议</div>
            <div class="stat-percentage">
              {{ summary?.sellSummary?.percentage?.toFixed(1) || 0 }}%
            </div>
          </div>
          <el-icon class="stat-icon"><Bottom /></el-icon>
        </div>
      </el-col>
    </el-row>

    <!-- 快速操作 & 最新分析 -->
    <el-row :gutter="24" class="main-row">
      <el-col :xs="24" :lg="8">
        <div
          class="page-card quick-card animate-fade-in"
          style="animation-delay: 0.3s"
        >
          <div class="card-header">
            <span class="card-title">
              <el-icon><Operation /></el-icon>
              快速操作
            </span>
          </div>
          <div class="quick-actions">
            <el-button
              type="primary"
              size="large"
              :loading="analyzing"
              @click="runAllAnalysis"
              class="action-btn primary"
            >
              <el-icon><VideoPlay /></el-icon>
              分析所有股票
            </el-button>
            <el-button
              size="large"
              @click="$router.push('/stocks')"
              class="action-btn"
            >
              <el-icon><Plus /></el-icon>
              添加股票
            </el-button>
            <el-button
              size="large"
              @click="$router.push('/statistics')"
              class="action-btn"
            >
              <el-icon><PieChart /></el-icon>
              查看统计
            </el-button>
          </div>

          <!-- 快速状态 -->
          <div class="quick-status">
            <div class="status-grid">
              <div class="status-card">
                <div class="status-icon ai">
                  <el-icon><Cpu /></el-icon>
                </div>
                <div class="status-info">
                  <span class="status-label">AI 模式</span>
                  <span class="status-value">Mock AI</span>
                </div>
              </div>
              <div class="status-card">
                <div class="status-icon time">
                  <el-icon><Clock /></el-icon>
                </div>
                <div class="status-info">
                  <span class="status-label">最后分析</span>
                  <span class="status-value">{{ summary?.lastAnalysisDate || "暂无" }}</span>
                </div>
              </div>
              <div class="status-card">
                <div class="status-icon stock">
                  <el-icon><Collection /></el-icon>
                </div>
                <div class="status-info">
                  <span class="status-label">股票总数</span>
                  <span class="status-value">{{ summary?.totalStocks || 0 }} <small>只</small></span>
                </div>
              </div>
              <div class="status-card">
                <div class="status-icon analysis">
                  <el-icon><DataAnalysis /></el-icon>
                </div>
                <div class="status-info">
                  <span class="status-label">分析总数</span>
                  <span class="status-value">{{ summary?.totalAnalysis || 0 }} <small>次</small></span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </el-col>

      <el-col :xs="24" :lg="16">
        <div
          class="page-card result-card animate-fade-in"
          style="animation-delay: 0.35s"
        >
          <div class="card-header">
            <span class="card-title">
              <el-icon><Clock /></el-icon>
              最新分析结果
            </span>
            <el-button text type="primary" @click="$router.push('/analysis')">
              查看全部 <el-icon><ArrowRight /></el-icon>
            </el-button>
          </div>

          <el-table
            :data="latestResults"
            v-loading="loading"
            height="340"
            :show-header="true"
            stripe
          >
            <el-table-column prop="symbol" label="股票" width="130">
              <template #default="{ row }">
                <span class="stock-symbol">{{ row.symbol }}</span>
              </template>
            </el-table-column>
            <el-table-column
              prop="stockName"
              label="名称"
              min-width="150"
              show-overflow-tooltip
            />
            <el-table-column
              prop="recommendation"
              label="建议"
              width="100"
              align="center"
            >
              <template #default="{ row }">
                <el-tag
                  :type="getRecommendationType(row.recommendation)"
                  effect="dark"
                  round
                >
                  {{ getRecommendationText(row.recommendation) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="confidence" label="置信度" width="160">
              <template #default="{ row }">
                <div class="confidence-bar">
                  <el-progress
                    :percentage="row.confidence"
                    :color="getConfidenceColor(row.confidence)"
                    :stroke-width="10"
                    :show-text="false"
                  />
                  <span class="confidence-value">{{ row.confidence }}%</span>
                </div>
              </template>
            </el-table-column>
            <el-table-column prop="analysisDate" label="日期" width="120" />
          </el-table>

          <div
            v-if="latestResults.length === 0 && !loading"
            class="empty-state"
          >
            <el-icon><Document /></el-icon>
            <h3>暂无分析数据</h3>
            <p>点击"分析所有股票"开始 AI 分析</p>
          </div>
        </div>
      </el-col>
    </el-row>

    <!-- 图表区域 -->
    <el-row :gutter="24" class="chart-row">
      <el-col :xs="24" :lg="12">
        <div
          class="page-card chart-card animate-fade-in"
          style="animation-delay: 0.4s"
        >
          <div class="card-header">
            <span class="card-title">
              <el-icon><PieChart /></el-icon>
              建议分布
            </span>
          </div>
          <div class="chart-container" ref="pieChartRef"></div>
        </div>
      </el-col>
      <el-col :xs="24" :lg="12">
        <div
          class="page-card chart-card animate-fade-in"
          style="animation-delay: 0.45s"
        >
          <div class="card-header">
            <span class="card-title">
              <el-icon><InfoFilled /></el-icon>
              系统信息
            </span>
          </div>
          <div class="system-info-grid">
            <div class="info-item">
              <div class="info-icon ai">
                <el-icon><Cpu /></el-icon>
              </div>
              <div class="info-content">
                <span class="info-label">AI 模式</span>
                <span class="info-value">Mock AI</span>
                <span class="info-tag">演示模式</span>
              </div>
            </div>
            <div class="info-item">
              <div class="info-icon date">
                <el-icon><Calendar /></el-icon>
              </div>
              <div class="info-content">
                <span class="info-label">首次分析</span>
                <span class="info-value">{{ summary?.firstAnalysisDate || "-" }}</span>
              </div>
            </div>
            <div class="info-item">
              <div class="info-icon time">
                <el-icon><Timer /></el-icon>
              </div>
              <div class="info-content">
                <span class="info-label">最后分析</span>
                <span class="info-value">{{ summary?.lastAnalysisDate || "-" }}</span>
              </div>
            </div>
            <div class="info-item">
              <div class="info-icon success">
                <el-icon><Top /></el-icon>
              </div>
              <div class="info-content">
                <span class="info-label">买入平均置信度</span>
                <span class="info-value success">{{ summary?.buySummary?.averageConfidence?.toFixed(1) || 0 }}%</span>
              </div>
            </div>
            <div class="info-item">
              <div class="info-icon danger">
                <el-icon><Bottom /></el-icon>
              </div>
              <div class="info-content">
                <span class="info-label">卖出平均置信度</span>
                <span class="info-value danger">{{ summary?.sellSummary?.averageConfidence?.toFixed(1) || 0 }}%</span>
              </div>
            </div>
            <div class="info-item">
              <div class="info-icon hold">
                <el-icon><DataAnalysis /></el-icon>
              </div>
              <div class="info-content">
                <span class="info-label">持有平均置信度</span>
                <span class="info-value hold">{{ summary?.holdSummary?.averageConfidence?.toFixed(1) || 0 }}%</span>
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
import { ElMessage } from "element-plus";
import {
  Collection,
  DataAnalysis,
  Top,
  Bottom,
  Operation,
  VideoPlay,
  Plus,
  PieChart,
  Clock,
  ArrowRight,
  InfoFilled,
  Document,
  TrendCharts,
  Cpu,
  Calendar,
  Timer,
} from "@element-plus/icons-vue";
import * as echarts from "echarts";
import { useStatisticsStore, useAnalysisStore } from "@/stores";
import { logger } from "@/utils/logger";

const statisticsStore = useStatisticsStore();
const analysisStore = useAnalysisStore();

const summary = ref(statisticsStore.summary);
const latestResults = ref(analysisStore.latestResults);
const loading = ref(false);
const analyzing = ref(false);
const pieChartRef = ref<HTMLElement>();

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

const runAllAnalysis = async () => {
  analyzing.value = true;
  logger.userAction("触发全量分析");

  try {
    const result = await analysisStore.runAnalysis();
    ElMessage.success({
      message: `分析完成！成功 ${result.successCount} 只，失败 ${result.failureCount} 只`,
      duration: 5000,
    });
    logger.info("全量分析完成", result);
    await loadData();
  } catch (e) {
    logger.error("全量分析失败", e);
    ElMessage.error("分析失败，请检查后端服务");
  } finally {
    analyzing.value = false;
  }
};

const loadData = async () => {
  loading.value = true;
  logger.info("加载仪表盘数据");

  try {
    await Promise.all([
      statisticsStore.fetchSummary(),
      analysisStore.fetchLatest(10),
    ]);
    summary.value = statisticsStore.summary;
    latestResults.value = analysisStore.latestResults;

    logger.info("仪表盘数据加载完成", {
      totalStocks: summary.value?.totalStocks,
      totalAnalysis: summary.value?.totalAnalysis,
    });

    await nextTick();
    initPieChart();
  } catch (error) {
    logger.error("加载仪表盘数据失败", error);
  } finally {
    loading.value = false;
  }
};

const initPieChart = () => {
  if (!pieChartRef.value || !summary.value) return;

  const chart = echarts.init(pieChartRef.value);

  const option = {
    tooltip: {
      trigger: "item",
      formatter: "{b}: {c} ({d}%)",
      backgroundColor: "rgba(255, 255, 255, 0.95)",
      borderColor: "#e5e7eb",
      borderWidth: 1,
      textStyle: { color: "#1f2937" },
    },
    legend: {
      bottom: 20,
      itemWidth: 12,
      itemHeight: 12,
      textStyle: { color: "#6b7280", fontSize: 13 },
    },
    series: [
      {
        type: "pie",
        radius: ["45%", "75%"],
        center: ["50%", "45%"],
        avoidLabelOverlap: false,
        itemStyle: {
          borderRadius: 8,
          borderColor: "#fff",
          borderWidth: 3,
        },
        label: { show: false },
        emphasis: {
          label: {
            show: true,
            fontSize: 16,
            fontWeight: "bold",
            color: "#1f2937",
          },
          itemStyle: {
            shadowBlur: 20,
            shadowColor: "rgba(0, 0, 0, 0.2)",
          },
        },
        data: [
          {
            value: summary.value.buySummary?.count || 0,
            name: "买入",
            itemStyle: { color: "#10b981" },
          },
          {
            value: summary.value.holdSummary?.count || 0,
            name: "持有",
            itemStyle: { color: "#f59e0b" },
          },
          {
            value: summary.value.sellSummary?.count || 0,
            name: "卖出",
            itemStyle: { color: "#ef4444" },
          },
        ],
      },
    ],
  };

  chart.setOption(option);

  // 响应式
  window.addEventListener("resize", () => chart.resize());
};

onMounted(() => {
  logger.info("Dashboard 页面加载");
  loadData();
});
</script>

<style lang="scss" scoped>
.dashboard {
  .welcome-banner {
    background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
    border-radius: var(--radius-lg);
    padding: 32px 40px;
    margin-bottom: 28px;
    display: flex;
    justify-content: space-between;
    align-items: center;
    color: #fff;
    box-shadow: 0 8px 30px rgba(59, 130, 246, 0.3);
    overflow: hidden;
    position: relative;

    &::before {
      content: "";
      position: absolute;
      top: -50%;
      right: -20%;
      width: 60%;
      height: 200%;
      background: radial-gradient(
        circle,
        rgba(255, 255, 255, 0.1) 0%,
        transparent 60%
      );
    }

    .banner-content {
      h2 {
        font-size: 26px;
        font-weight: 700;
        margin: 0 0 8px 0;
        letter-spacing: -0.5px;
      }

      p {
        font-size: 15px;
        opacity: 0.9;
        margin: 0;
      }
    }

    .banner-illustration {
      opacity: 0.3;
    }
  }

  .stat-row {
    margin-bottom: 28px;
  }

  .stat-card {
    .stat-content {
      position: relative;
      z-index: 1;
    }

    .stat-percentage {
      font-size: 13px;
      opacity: 0.8;
      margin-top: 4px;
    }
  }

  .main-row {
    .el-col {
      margin-bottom: 24px;
    }
  }

  .quick-card,
  .result-card {
    height: 460px;
    display: flex;
    flex-direction: column;

    .card-header {
      flex-shrink: 0;
    }
  }

  .quick-card {
    .quick-actions {
      flex-shrink: 0;
    }

    .quick-status {
      flex: 1;
      display: flex;
      flex-direction: column;
      justify-content: flex-end;
    }
  }

  .result-card {
    :deep(.el-table) {
      flex: 1;
    }
  }

  .quick-actions {
    display: flex;
    flex-direction: column;
    gap: 12px;

    .action-btn {
      width: 100% !important;
      max-width: 100%;
      height: 48px;
      font-size: 15px;
      font-weight: 600;
      border-radius: var(--radius-md);
      transition: all 0.2s ease;
      flex-shrink: 0;
      margin: 0 !important;

      :deep(> span) {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        width: 100%;
      }

      :deep(.el-icon) {
        font-size: 18px;
        margin: 0 !important;
      }

      &.primary {
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
        border: none;
        box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);

        &:hover {
          background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
          box-shadow: 0 6px 16px rgba(59, 130, 246, 0.4);
          transform: translateY(-1px);
        }
      }

      &:not(.primary) {
        background: #ffffff;
        border: 1px solid #e2e8f0;
        color: #475569;

        &:hover {
          background: #f8fafc;
          border-color: #cbd5e1;
          color: #334155;
        }
      }
    }
  }

  .quick-status {
    margin-top: 20px;
    padding-top: 20px;
    border-top: 1px solid var(--border-light);

    .status-grid {
      display: grid;
      grid-template-columns: repeat(2, 1fr);
      gap: 12px;
    }

    .status-card {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px;
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      border-radius: 10px;
      border: 1px solid #e2e8f0;
      transition: all 0.2s ease;

      &:hover {
        background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%);
        border-color: #cbd5e1;
      }
    }

    .status-icon {
      width: 36px;
      height: 36px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;

      .el-icon {
        font-size: 18px;
        color: white;
      }

      &.ai {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
      }

      &.time {
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      }

      &.stock {
        background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      }

      &.analysis {
        background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
      }
    }

    .status-info {
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;
    }

    .status-label {
      font-size: 11px;
      color: #94a3b8;
      font-weight: 500;
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }

    .status-value {
      font-size: 14px;
      font-weight: 700;
      color: #334155;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;

      small {
        font-size: 11px;
        font-weight: 500;
        color: #64748b;
        margin-left: 2px;
      }
    }
  }

  .chart-row {
    .el-col {
      margin-bottom: 24px;
    }
  }

  .chart-card {
    height: 420px;
    display: flex;
    flex-direction: column;

    .card-header {
      flex-shrink: 0;
    }

    .chart-container {
      flex: 1;
    }

    .system-info {
      flex: 1;
    }
  }

  .chart-container {
    height: 100%;
    min-height: 320px;
  }

  .system-info-grid {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 16px;
    flex: 1;

    .info-item {
      display: flex;
      align-items: center;
      gap: 14px;
      padding: 18px;
      background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
      border-radius: 14px;
      border: 1px solid #e2e8f0;
      transition: all 0.2s ease;

      &:hover {
        background: linear-gradient(135deg, #f1f5f9 0%, #e2e8f0 100%);
        border-color: #cbd5e1;
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
      }
    }

    .info-icon {
      width: 44px;
      height: 44px;
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;

      .el-icon {
        font-size: 20px;
        color: white;
      }

      &.ai {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
      }

      &.date {
        background: linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%);
      }

      &.time {
        background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      }

      &.success {
        background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      }

      &.danger {
        background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
      }

      &.hold {
        background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
      }
    }

    .info-content {
      display: flex;
      flex-direction: column;
      gap: 4px;
      min-width: 0;
    }

    .info-label {
      font-size: 12px;
      color: #94a3b8;
      font-weight: 500;
    }

    .info-value {
      font-size: 16px;
      font-weight: 700;
      color: #334155;

      &.success {
        color: #10b981;
      }

      &.danger {
        color: #ef4444;
      }

      &.hold {
        color: #f59e0b;
      }
    }

    .info-tag {
      font-size: 11px;
      color: #f59e0b;
      background: #fef3c7;
      padding: 2px 8px;
      border-radius: 4px;
      width: fit-content;
      font-weight: 500;
    }
  }

  .empty-state {
    padding: 60px 20px;
  }
}
</style>
