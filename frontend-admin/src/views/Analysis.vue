<template>
  <div class="analysis-page">
    <!-- 操作面板 -->
    <div class="page-card">
      <div class="card-header">
        <span class="card-title">
          <el-icon><Cpu /></el-icon>
          AI 分析控制台
        </span>
      </div>

      <el-row :gutter="24">
        <el-col :xs="24" :lg="12">
          <div class="action-panel batch">
            <div class="panel-header">
              <el-icon class="panel-icon"><VideoPlay /></el-icon>
              <div>
                <h4>批量分析</h4>
                <p class="desc">对所有活跃股票执行 AI 分析</p>
              </div>
            </div>
            <div class="action-controls">
              <el-checkbox v-model="forceRerun" class="force-checkbox">
                <span class="checkbox-label">强制重新分析</span>
                <span class="checkbox-hint">（覆盖今日已有结果）</span>
              </el-checkbox>
              <el-button
                type="primary"
                size="large"
                :loading="analyzing"
                @click="runBatchAnalysis"
                class="run-btn"
              >
                <el-icon><VideoPlay /></el-icon>
                开始分析所有股票
              </el-button>
            </div>
          </div>
        </el-col>
        <el-col :xs="24" :lg="12">
          <div class="action-panel single">
            <div class="panel-header">
              <el-icon class="panel-icon"><Promotion /></el-icon>
              <div>
                <h4>单股分析</h4>
                <p class="desc">分析指定股票</p>
              </div>
            </div>
            <div class="action-controls">
              <el-select
                v-model="selectedSymbol"
                placeholder="选择股票"
                filterable
                class="stock-select"
              >
                <el-option
                  v-for="stock in stocks"
                  :key="stock.symbol"
                  :label="`${stock.symbol} - ${stock.name}`"
                  :value="stock.symbol"
                >
                  <span class="stock-option">
                    <span class="symbol">{{ stock.symbol }}</span>
                    <span class="name">{{ stock.name }}</span>
                  </span>
                </el-option>
              </el-select>
              <el-button
                type="success"
                size="large"
                :loading="analyzing"
                :disabled="!selectedSymbol"
                @click="runSingleAnalysis"
                class="run-btn"
              >
                <el-icon><Promotion /></el-icon>
                分析该股票
              </el-button>
            </div>
          </div>
        </el-col>
      </el-row>

      <!-- 分析结果摘要 -->
      <transition name="fade">
        <div v-if="lastRunResult" class="result-summary">
          <el-alert
            :type="lastRunResult.failureCount > 0 ? 'warning' : 'success'"
            :closable="false"
            show-icon
          >
            <template #title>
              <div class="summary-content">
                <span class="summary-text">
                  分析完成！共
                  <strong>{{ lastRunResult.totalStocks }}</strong> 只股票
                </span>
                <div class="summary-stats">
                  <el-tag type="success" effect="plain"
                    >成功 {{ lastRunResult.successCount }}</el-tag
                  >
                  <el-tag
                    v-if="lastRunResult.failureCount > 0"
                    type="danger"
                    effect="plain"
                    >失败 {{ lastRunResult.failureCount }}</el-tag
                  >
                  <el-tag
                    v-if="lastRunResult.skippedCount > 0"
                    type="info"
                    effect="plain"
                    >跳过 {{ lastRunResult.skippedCount }}</el-tag
                  >
                  <span class="duration-badge">
                    <el-icon><Timer /></el-icon>
                    {{ formatDuration(lastRunResult.durationMs) }}
                  </span>
                </div>
              </div>
            </template>
          </el-alert>
        </div>
      </transition>
    </div>

    <!-- 分析结果列表 -->
    <div class="page-card">
      <div class="card-header">
        <span class="card-title">
          <el-icon><Document /></el-icon>
          分析结果历史
          <el-tag
            type="info"
            effect="plain"
            size="small"
            style="margin-left: 12px"
          >
            共 {{ total }} 条
          </el-tag>
        </span>
      </div>

      <!-- 筛选 -->
      <div class="search-bar">
        <el-select v-model="filterSymbol" placeholder="股票筛选" clearable>
          <el-option
            v-for="stock in stocks"
            :key="stock.symbol"
            :label="stock.symbol"
            :value="stock.symbol"
          />
        </el-select>
        <el-select
          v-model="filterRecommendation"
          placeholder="建议类型"
          clearable
        >
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
        <el-button type="primary" @click="fetchResults" :icon="Search">
          查询
        </el-button>
        <el-button @click="resetFilters" :icon="Refresh"> 重置 </el-button>
      </div>

      <el-table :data="results" v-loading="loading" stripe>
        <el-table-column prop="symbol" label="股票代码" width="130">
          <template #default="{ row }">
            <span class="stock-symbol">{{ row.symbol }}</span>
          </template>
        </el-table-column>
        <el-table-column
          prop="stockName"
          label="股票名称"
          width="180"
          show-overflow-tooltip
        />
        <el-table-column
          prop="analysisDate"
          label="分析日期"
          width="130"
          align="center"
        >
          <template #default="{ row }">
            <span class="date-cell">{{ row.analysisDate }}</span>
          </template>
        </el-table-column>
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
        <el-table-column prop="confidence" label="置信度" width="170">
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
        <el-table-column prop="reasoning" label="分析理由" min-width="300">
          <template #default="{ row }">
            <el-tooltip
              :content="row.reasoning"
              placement="top-start"
              :show-after="300"
            >
              <span class="reasoning-text">{{ row.reasoning }}</span>
            </el-tooltip>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="创建时间" width="170">
          <template #default="{ row }">
            <span class="time-text">{{ formatDate(row.createdAt) }}</span>
          </template>
        </el-table-column>

        <template #empty>
          <div class="empty-state">
            <el-icon><Document /></el-icon>
            <h3>暂无分析数据</h3>
            <p>点击上方"开始分析"执行 AI 分析</p>
          </div>
        </template>
      </el-table>

      <el-pagination
        v-model:current-page="currentPage"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[10, 20, 50, 100]"
        layout="total, sizes, prev, pager, next, jumper"
        @change="fetchResults"
        class="pagination"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElMessage } from "element-plus";
import {
  Cpu,
  VideoPlay,
  Promotion,
  Document,
  Search,
  Refresh,
  Timer,
} from "@element-plus/icons-vue";
import { useStockStore, useAnalysisStore } from "@/stores";
import type { AnalysisResult, RunAnalysisResponse } from "@/api";
import { logger } from "@/utils/logger";
import dayjs from "dayjs";

const stockStore = useStockStore();
const analysisStore = useAnalysisStore();

const stocks = ref(stockStore.stocks);
const results = ref<AnalysisResult[]>([]);
const loading = ref(false);
const analyzing = ref(false);
const forceRerun = ref(false);
const selectedSymbol = ref("");
const filterSymbol = ref("");
const filterRecommendation = ref<number | "">("");
const currentPage = ref(1);
const pageSize = ref(20);
const total = ref(0);
const lastRunResult = ref<RunAnalysisResponse | null>(null);

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

const formatDate = (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm");

const formatDuration = (ms: number) => {
  if (ms < 1000) return `${ms}ms`;
  const seconds = (ms / 1000).toFixed(1);
  return `${seconds}s`;
};

const fetchStocks = async () => {
  await stockStore.fetchStocks();
  stocks.value = stockStore.stocks;
};

const fetchResults = async () => {
  loading.value = true;
  logger.info("查询分析结果", {
    page: currentPage.value,
    symbol: filterSymbol.value,
  });

  try {
    const res = await analysisStore.fetchResults({
      page: currentPage.value,
      pageSize: pageSize.value,
      symbol: filterSymbol.value || undefined,
      recommendation: filterRecommendation.value || undefined,
    });
    results.value = res.results;
    total.value = res.total;
  } finally {
    loading.value = false;
  }
};

const resetFilters = () => {
  filterSymbol.value = "";
  filterRecommendation.value = "";
  currentPage.value = 1;
  fetchResults();
};

const runBatchAnalysis = async () => {
  analyzing.value = true;
  logger.userAction("执行批量分析", { forceRerun: forceRerun.value });

  try {
    lastRunResult.value = await analysisStore.runAnalysis(
      undefined,
      forceRerun.value,
    );
    ElMessage.success({
      message: `分析完成！成功 ${lastRunResult.value.successCount} 只`,
      duration: 4000,
    });
    await fetchResults();
  } catch (error) {
    logger.error("批量分析失败", error);
    ElMessage.error("分析失败，请检查后端服务");
  } finally {
    analyzing.value = false;
  }
};

const runSingleAnalysis = async () => {
  if (!selectedSymbol.value) return;
  analyzing.value = true;
  logger.userAction("执行单股分析", { symbol: selectedSymbol.value });

  try {
    const result = await analysisStore.runSingleAnalysis(selectedSymbol.value);
    ElMessage.success({
      message: `${result.symbol}: ${getRecommendationText(result.recommendation)} (${result.confidence}%)`,
      duration: 4000,
    });
    await fetchResults();
  } catch (error) {
    logger.error("单股分析失败", error);
    ElMessage.error("分析失败");
  } finally {
    analyzing.value = false;
  }
};

onMounted(async () => {
  logger.info("Analysis 页面加载");
  await fetchStocks();
  await fetchResults();
});
</script>

<style lang="scss" scoped>
.analysis-page {
  .action-panel {
    background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
    border-radius: var(--radius-lg);
    padding: 24px;
    border: 1px solid var(--border-light);
    height: 100%;

    .panel-header {
      display: flex;
      align-items: flex-start;
      gap: 16px;
      margin-bottom: 20px;

      .panel-icon {
        font-size: 28px;
        padding: 14px;
        border-radius: 14px;
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
      }

      h4 {
        margin: 0 0 4px 0;
        font-size: 18px;
        font-weight: 700;
        color: var(--text-primary);
      }

      .desc {
        margin: 0;
        color: var(--text-secondary);
        font-size: 13px;
      }
    }

    &.batch .panel-icon {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: white;
      box-shadow: 0 4px 14px rgba(59, 130, 246, 0.4);
    }

    &.single .panel-icon {
      background: linear-gradient(135deg, #10b981 0%, #059669 100%);
      color: white;
      box-shadow: 0 4px 14px rgba(16, 185, 129, 0.4);
    }

    .action-controls {
      display: flex;
      flex-direction: column;
      gap: 16px;

      .force-checkbox {
        .checkbox-label {
          font-weight: 500;
        }

        .checkbox-hint {
          color: var(--text-secondary);
          font-size: 12px;
          margin-left: 4px;
        }
      }

      .stock-select {
        width: 100%;

        .stock-option {
          display: flex;
          justify-content: space-between;
          align-items: center;

          .symbol {
            font-weight: 600;
            color: var(--primary-color);
          }

          .name {
            color: var(--text-secondary);
            font-size: 12px;
          }
        }
      }

      .run-btn {
        width: 100%;
        height: 48px;
        font-size: 15px;
        font-weight: 600;
        border-radius: var(--radius-md);
      }
    }
  }

  .result-summary {
    margin-top: 24px;

    .summary-content {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 16px;

      .summary-text {
        font-size: 15px;
      }

      .summary-stats {
        display: flex;
        align-items: center;
        gap: 8px;

        .duration-badge {
          display: inline-flex;
          align-items: center;
          gap: 4px;
          padding: 4px 12px;
          background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
          border-radius: 20px;
          font-size: 13px;
          font-weight: 600;
          color: #475569;
          border: 1px solid #cbd5e1;

          .el-icon {
            font-size: 14px;
            color: #64748b;
          }
        }
      }
    }
  }

  .date-cell {
    display: inline-block;
    padding: 4px 10px;
    background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
    border-radius: 6px;
    font-size: 13px;
    font-weight: 500;
    color: #475569;
    white-space: nowrap;
    border: 1px solid #e2e8f0;
  }

  .reasoning-text {
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    line-height: 1.5;
    color: var(--text-regular);
  }

  .time-text {
    color: var(--text-secondary);
    font-size: 13px;
  }

  .pagination {
    margin-top: 24px;
    justify-content: flex-end;
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
