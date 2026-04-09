<template>
  <div class="stocks-page">
    <div class="page-card">
      <div class="card-header">
        <span class="card-title">
          <el-icon><Collection /></el-icon>
          股票列表
          <el-tag
            type="info"
            effect="plain"
            size="small"
            style="margin-left: 12px"
          >
            共 {{ filteredStocks.length }} 只
          </el-tag>
        </span>
        <el-button type="primary" @click="showAddDialog = true">
          <el-icon><Plus /></el-icon>
          添加股票
        </el-button>
      </div>

      <!-- 搜索栏 -->
      <div class="search-bar">
        <el-input
          v-model="searchText"
          placeholder="搜索股票代码或名称..."
          clearable
          :prefix-icon="Search"
          class="search-input"
        />
        <el-checkbox v-model="showInactive" border> 显示已停用 </el-checkbox>
        <el-button @click="fetchData" :loading="loading" :icon="Refresh">
          刷新
        </el-button>
      </div>

      <!-- 表格 -->
      <el-table
        :data="filteredStocks"
        v-loading="loading"
        stripe
        :row-class-name="tableRowClassName"
      >
        <el-table-column prop="symbol" label="股票代码" width="140">
          <template #default="{ row }">
            <span class="stock-symbol">{{ row.symbol }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="name" label="股票名称" min-width="200" />
        <el-table-column
          prop="isActive"
          label="状态"
          width="100"
          align="center"
        >
          <template #default="{ row }">
            <div
              class="status-badge"
              :class="{ active: row.isActive, inactive: !row.isActive }"
            >
              <span class="status-dot"></span>
              <span class="status-text">{{
                row.isActive ? "活跃" : "停用"
              }}</span>
            </div>
          </template>
        </el-table-column>
        <el-table-column
          prop="analysisCount"
          label="分析次数"
          width="110"
          align="center"
        >
          <template #default="{ row }">
            <span class="analysis-count">
              <span class="count-number">{{ row.analysisCount }}</span>
              <span class="count-unit">次</span>
            </span>
          </template>
        </el-table-column>
        <el-table-column label="最新建议" width="120" align="center">
          <template #default="{ row }">
            <el-tag
              v-if="row.latestAnalysis"
              :type="getRecommendationType(row.latestAnalysis.recommendation)"
              effect="dark"
              round
            >
              {{ getRecommendationText(row.latestAnalysis.recommendation) }}
            </el-tag>
            <span v-else class="no-data">-</span>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="添加时间" width="180">
          <template #default="{ row }">
            <span class="time-text">{{ formatDate(row.createdAt) }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right" align="center">
          <template #default="{ row }">
            <div class="action-buttons">
              <el-tooltip content="AI 分析" placement="top">
                <button
                  class="action-btn action-btn-primary"
                  @click="analyzeStock(row)"
                  :disabled="analyzingSymbol === row.symbol"
                >
                  <el-icon v-if="analyzingSymbol !== row.symbol"
                    ><Cpu
                  /></el-icon>
                  <el-icon v-else class="is-loading"><Loading /></el-icon>
                </button>
              </el-tooltip>
              <el-tooltip content="编辑" placement="top">
                <button
                  class="action-btn action-btn-default"
                  @click="editStock(row)"
                >
                  <el-icon><Edit /></el-icon>
                </button>
              </el-tooltip>
              <el-popconfirm
                title="确定删除该股票吗？"
                confirm-button-text="确定"
                cancel-button-text="取消"
                @confirm="deleteStock(row)"
              >
                <template #reference>
                  <button class="action-btn action-btn-danger" title="删除">
                    <el-icon><Delete /></el-icon>
                  </button>
                </template>
              </el-popconfirm>
            </div>
          </template>
        </el-table-column>

        <template #empty>
          <div class="empty-state">
            <el-icon><Box /></el-icon>
            <h3>暂无股票数据</h3>
            <p>点击"添加股票"开始管理您的股票列表</p>
          </div>
        </template>
      </el-table>
    </div>

    <!-- 添加股票对话框 -->
    <el-dialog
      v-model="showAddDialog"
      title="添加股票"
      width="480px"
      :close-on-click-modal="false"
      class="stock-dialog"
    >
      <el-form
        :model="addForm"
        :rules="rules"
        ref="addFormRef"
        label-width="90px"
        class="stock-form"
      >
        <el-form-item label="股票代码" prop="symbol">
          <el-input
            v-model="addForm.symbol"
            placeholder="如 AAPL, GOOGL, MSFT"
            maxlength="20"
            show-word-limit
            class="form-input"
          />
        </el-form-item>
        <el-form-item label="股票名称" prop="name">
          <el-input
            v-model="addForm.name"
            placeholder="如 Apple Inc., Google"
            maxlength="100"
            class="form-input"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button class="btn-cancel" @click="showAddDialog = false"
            >取消</el-button
          >
          <el-button
            class="btn-submit"
            type="primary"
            @click="handleAdd"
            :loading="submitting"
          >
            <el-icon v-if="!submitting"><Check /></el-icon>
            确定添加
          </el-button>
        </div>
      </template>
    </el-dialog>

    <!-- 批量导入对话框 -->
    <el-dialog
      v-model="showBatchDialog"
      title="批量导入股票"
      width="640px"
      :close-on-click-modal="false"
    >
      <el-alert type="info" :closable="false" show-icon class="mb-4">
        <template #title>
          <strong>导入格式说明</strong>
        </template>
        请输入要导入的股票，每行一个，格式：<code>股票代码,股票名称</code>
      </el-alert>
      <el-input
        v-model="batchText"
        type="textarea"
        :rows="12"
        placeholder="AAPL,Apple Inc.
GOOGL,Alphabet Inc.
MSFT,Microsoft Corporation
TSLA,Tesla Inc.
AMZN,Amazon.com Inc."
      />
      <div class="batch-preview" v-if="parsedBatchCount > 0">
        <el-icon><InfoFilled /></el-icon>
        已识别 <strong>{{ parsedBatchCount }}</strong> 只股票
      </div>
      <template #footer>
        <el-button @click="showBatchDialog = false">取消</el-button>
        <el-button
          type="primary"
          @click="handleBatchAdd"
          :loading="submitting"
          :disabled="parsedBatchCount === 0"
        >
          <el-icon><Upload /></el-icon>
          导入 {{ parsedBatchCount }} 只股票
        </el-button>
      </template>
    </el-dialog>

    <!-- 编辑对话框 -->
    <el-dialog
      v-model="showEditDialog"
      title="编辑股票"
      width="480px"
      :close-on-click-modal="false"
      class="stock-dialog"
    >
      <el-form :model="editForm" label-width="90px" class="stock-form">
        <el-form-item label="股票代码">
          <el-input v-model="editForm.symbol" disabled class="form-input" />
        </el-form-item>
        <el-form-item label="股票名称">
          <el-input
            v-model="editForm.name"
            placeholder="股票名称"
            class="form-input"
          />
        </el-form-item>
        <el-form-item label="状态">
          <div class="status-toggle">
            <div
              class="toggle-option"
              :class="{ active: editForm.isActive }"
              @click="editForm.isActive = true"
            >
              <el-icon><CircleCheck /></el-icon>
              活跃
            </div>
            <div
              class="toggle-option"
              :class="{ active: !editForm.isActive }"
              @click="editForm.isActive = false"
            >
              <el-icon><Remove /></el-icon>
              停用
            </div>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <div class="dialog-footer">
          <el-button class="btn-cancel" @click="showEditDialog = false"
            >取消</el-button
          >
          <el-button
            class="btn-submit"
            type="primary"
            @click="handleEdit"
            :loading="submitting"
          >
            <el-icon v-if="!submitting"><Check /></el-icon>
            保存修改
          </el-button>
        </div>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { ElMessage } from "element-plus";
import {
  Collection,
  Plus,
  Upload,
  Refresh,
  Search,
  Cpu,
  Edit,
  Delete,
  Box,
  Check,
  InfoFilled,
  Loading,
  CircleCheck,
  Remove,
} from "@element-plus/icons-vue";
import { useStockStore, useAnalysisStore } from "@/stores";
import type { Stock } from "@/api";
import { logger } from "@/utils/logger";
import dayjs from "dayjs";

const stockStore = useStockStore();
const analysisStore = useAnalysisStore();

const loading = ref(false);
const submitting = ref(false);
const analyzingSymbol = ref("");
const searchText = ref("");
const showInactive = ref(false);
const showAddDialog = ref(false);
const showBatchDialog = ref(false);
const showEditDialog = ref(false);
const batchText = ref("");
const addFormRef = ref();

const addForm = ref({ symbol: "", name: "" });
const editForm = ref({ symbol: "", name: "", isActive: true });

const rules = {
  symbol: [
    { required: true, message: "请输入股票代码", trigger: "blur" },
    { min: 1, max: 20, message: "长度在 1 到 20 个字符", trigger: "blur" },
  ],
  name: [{ required: true, message: "请输入股票名称", trigger: "blur" }],
};

const filteredStocks = computed(() => {
  let list = stockStore.stocks;
  if (!showInactive.value) {
    list = list.filter((s) => s.isActive);
  }
  if (searchText.value) {
    const keyword = searchText.value.toLowerCase();
    list = list.filter(
      (s) =>
        s.symbol.toLowerCase().includes(keyword) ||
        s.name.toLowerCase().includes(keyword),
    );
  }
  return list;
});

const parsedBatchCount = computed(() => {
  if (!batchText.value.trim()) return 0;
  const lines = batchText.value
    .trim()
    .split("\n")
    .filter((l) => l.trim());
  return lines.filter((line) => {
    const [symbol] = line.split(",").map((s) => s.trim());
    return symbol && symbol.length > 0;
  }).length;
});

const tableRowClassName = ({ row }: { row: Stock }) => {
  return row.isActive ? "" : "inactive-row";
};

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

const formatDate = (date: string) => dayjs(date).format("YYYY-MM-DD HH:mm");

const fetchData = async () => {
  loading.value = true;
  logger.info("加载股票列表");
  try {
    await stockStore.fetchStocks(true);
    logger.info("股票列表加载完成", { count: stockStore.stocks.length });
  } catch (error) {
    logger.error("加载股票列表失败", error);
  } finally {
    loading.value = false;
  }
};

const handleAdd = async () => {
  await addFormRef.value?.validate();
  submitting.value = true;
  logger.userAction("添加股票", addForm.value);

  try {
    // 添加最小延迟以显示 loading 状态
    await Promise.all([
      stockStore.addStock(
        addForm.value.symbol.toUpperCase(),
        addForm.value.name,
      ),
      new Promise((resolve) => setTimeout(resolve, 600)),
    ]);
    ElMessage.success("添加成功");
    showAddDialog.value = false;
    addForm.value = { symbol: "", name: "" };
  } catch (error) {
    logger.error("添加股票失败", error);
  } finally {
    submitting.value = false;
  }
};

const handleBatchAdd = async () => {
  const lines = batchText.value
    .trim()
    .split("\n")
    .filter((l) => l.trim());
  if (lines.length === 0) {
    ElMessage.warning("请输入要导入的股票");
    return;
  }

  const stocks = lines
    .map((line) => {
      const [symbol, name] = line.split(",").map((s) => s.trim());
      return { symbol: symbol.toUpperCase(), name: name || symbol };
    })
    .filter((s) => s.symbol);

  if (stocks.length === 0) {
    ElMessage.warning("没有有效的股票数据");
    return;
  }

  submitting.value = true;
  logger.userAction("批量导入股票", { count: stocks.length });

  try {
    const result = await stockStore.batchAddStocks(stocks);
    ElMessage.success(`成功导入 ${result.length} 只股票`);
    showBatchDialog.value = false;
    batchText.value = "";
  } catch (error) {
    logger.error("批量导入失败", error);
  } finally {
    submitting.value = false;
  }
};

const editStock = (stock: Stock) => {
  editForm.value = {
    symbol: stock.symbol,
    name: stock.name,
    isActive: stock.isActive,
  };
  showEditDialog.value = true;
  logger.userAction("打开编辑对话框", { symbol: stock.symbol });
};

const handleEdit = async () => {
  submitting.value = true;
  logger.userAction("保存股票修改", editForm.value);

  try {
    // 添加最小延迟以显示 loading 状态
    await Promise.all([
      stockStore.updateStock(editForm.value.symbol, {
        name: editForm.value.name,
        isActive: editForm.value.isActive,
      }),
      new Promise((resolve) => setTimeout(resolve, 600)),
    ]);
    ElMessage.success("保存成功");
    showEditDialog.value = false;
  } catch (error) {
    logger.error("保存失败", error);
  } finally {
    submitting.value = false;
  }
};

const deleteStock = async (stock: Stock) => {
  logger.userAction("删除股票", { symbol: stock.symbol });
  try {
    await stockStore.deleteStock(stock.symbol);
    ElMessage.success("删除成功");
  } catch (error) {
    logger.error("删除失败", error);
    ElMessage.error("删除失败");
  }
};

const analyzeStock = async (stock: Stock) => {
  analyzingSymbol.value = stock.symbol;
  logger.userAction("分析单只股票", { symbol: stock.symbol });

  try {
    const result = await analysisStore.runSingleAnalysis(stock.symbol);
    ElMessage.success({
      message: `${stock.symbol}: ${result.recommendation === "Buy" ? "买入" : result.recommendation === "Hold" ? "持有" : "卖出"} (${result.confidence}%)`,
      duration: 4000,
    });
    await fetchData();
  } catch (error) {
    logger.error("分析失败", error);
    ElMessage.error("分析失败");
  } finally {
    analyzingSymbol.value = "";
  }
};

onMounted(() => {
  logger.info("Stocks 页面加载");
  fetchData();
});
</script>

<style lang="scss" scoped>
.stocks-page {
  .search-input {
    width: 280px;
  }

  .no-data {
    color: var(--text-muted);
    font-style: italic;
  }

  .time-text {
    color: var(--text-secondary);
    font-size: 13px;
  }

  // 状态徽章
  .status-badge {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 6px;
    padding: 6px 14px;
    border-radius: 20px;
    font-size: 13px;
    font-weight: 600;
    white-space: nowrap;
    min-width: 70px;

    .status-dot {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .status-text {
      line-height: 1;
    }

    &.active {
      background: linear-gradient(135deg, #d1fae5 0%, #a7f3d0 100%);
      color: #047857;
      box-shadow: 0 2px 6px rgba(16, 185, 129, 0.2);

      .status-dot {
        background: #10b981;
        box-shadow: 0 0 6px rgba(16, 185, 129, 0.6);
      }
    }

    &.inactive {
      background: linear-gradient(135deg, #f3f4f6 0%, #e5e7eb 100%);
      color: #6b7280;

      .status-dot {
        background: #9ca3af;
      }
    }
  }

  // 分析次数
  .analysis-count {
    display: inline-flex;
    align-items: baseline;
    gap: 2px;
    padding: 4px 10px;
    background: #f8fafc;
    border-radius: 6px;
    border: 1px solid #e2e8f0;

    .count-number {
      font-size: 14px;
      font-weight: 600;
      color: #334155;
    }

    .count-unit {
      font-size: 11px;
      color: #94a3b8;
    }
  }

  // 操作按钮组
  .action-buttons {
    display: flex;
    justify-content: center;
    gap: 8px;
  }

  .action-btn {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 34px;
    height: 34px;
    border-radius: 8px;
    border: none;
    cursor: pointer;
    transition: all 0.2s ease;
    font-size: 16px;

    &:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }

    .el-icon {
      font-size: 16px;
    }

    .is-loading {
      animation: rotate 1s linear infinite;
    }

    &-primary {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: white;
      box-shadow: 0 2px 6px rgba(59, 130, 246, 0.3);

      &:hover:not(:disabled) {
        background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
      }

      &:active:not(:disabled) {
        transform: translateY(0);
      }
    }

    &-default {
      background: #f8fafc;
      color: #64748b;
      border: 1px solid #e2e8f0;

      &:hover {
        background: #f1f5f9;
        color: #475569;
        border-color: #cbd5e1;
      }
    }

    &-danger {
      background: #fef2f2;
      color: #dc2626;
      border: 1px solid #fecaca;

      &:hover {
        background: #fee2e2;
        color: #b91c1c;
        border-color: #fca5a5;
      }
    }
  }

  @keyframes rotate {
    from {
      transform: rotate(0deg);
    }
    to {
      transform: rotate(360deg);
    }
  }

  .batch-preview {
    margin-top: 16px;
    padding: 12px 16px;
    background: #f0f9ff;
    border-radius: var(--radius-sm);
    color: #0369a1;
    font-size: 14px;
    display: flex;
    align-items: center;
    gap: 8px;

    strong {
      color: #0c4a6e;
    }
  }

  :deep(.inactive-row) {
    opacity: 0.6;
    background-color: #fafafa !important;
  }

  // 对话框底部按钮
  .dialog-footer {
    display: flex;
    justify-content: flex-end;
    gap: 12px;

    .btn-cancel,
    .btn-submit {
      min-width: 110px;
      height: 44px;
      font-size: 15px;
      font-weight: 600;
      border-radius: 12px;
      transition: all 0.2s ease;
    }

    .btn-cancel {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      color: #64748b;

      &:hover {
        background: #f8fafc;
        border-color: #cbd5e1;
        color: #475569;
      }
    }

    .btn-submit {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      border: none;
      box-shadow: 0 4px 14px rgba(59, 130, 246, 0.35);

      &:hover:not(:disabled) {
        background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
        box-shadow: 0 6px 20px rgba(59, 130, 246, 0.45);
        transform: translateY(-1px);
      }

      :deep(.el-icon) {
        margin-right: 6px;
      }
    }
  }
}
</style>

<!-- 对话框全局样式 -->
<style lang="scss">
.stock-dialog {
  // 对话框整体
  &.el-dialog {
    border-radius: 20px;
    overflow: hidden;
    box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
    padding: 0 !important;
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    margin: 0 !important;
  }

  // 标题区域
  .el-dialog__header {
    background: #ffffff;
    padding: 24px 28px 20px;
    border-bottom: 1px solid #f1f5f9;
    margin: 0;

    .el-dialog__title {
      font-size: 20px;
      font-weight: 700;
      color: #1e293b;
      letter-spacing: -0.02em;
    }

    .el-dialog__headerbtn {
      top: 24px;
      right: 28px;
      width: 36px;
      height: 36px;
      border-radius: 10px;
      transition: all 0.2s ease;

      &:hover {
        background: #f1f5f9;
      }

      .el-dialog__close {
        font-size: 18px;
        color: #94a3b8;
      }
    }
  }

  // 内容区域
  .el-dialog__body {
    padding: 28px;
    background: #ffffff;
  }

  // 底部区域
  .el-dialog__footer {
    padding: 20px 28px;
    background: #fafbfc;
    border-top: 1px solid #f1f5f9;
  }

  // 表单样式
  .stock-form {
    .el-form-item {
      margin-bottom: 24px;

      &:last-child {
        margin-bottom: 0;
      }

      .el-form-item__label {
        font-size: 14px;
        font-weight: 600;
        color: #64748b;
        padding-right: 16px;
      }

      .el-form-item__content {
        .el-input {
          .el-input__wrapper {
            border-radius: 12px;
            padding: 6px 16px;
            box-shadow: 0 0 0 1px #e2e8f0 inset;
            background: #fafbfc;
            transition: all 0.2s ease;

            &:hover {
              box-shadow: 0 0 0 1px #cbd5e1 inset;
              background: #ffffff;
            }

            &.is-focus {
              box-shadow: 0 0 0 2px #3b82f6 inset;
              background: #ffffff;
            }

            .el-input__inner {
              height: 40px;
              font-size: 15px;
              color: #334155;

              &::placeholder {
                color: #94a3b8;
              }
            }
          }

          &.is-disabled {
            .el-input__wrapper {
              background: #f8fafc;
              box-shadow: 0 0 0 1px #e2e8f0 inset;

              .el-input__inner {
                color: #64748b;
                -webkit-text-fill-color: #64748b;
              }
            }
          }
        }

        // 状态切换器
        .status-toggle {
          display: flex;
          gap: 12px;

          .toggle-option {
            display: flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            padding: 12px 20px;
            border-radius: 12px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s ease;
            border: 2px solid #e2e8f0;
            background: #fafbfc;
            color: #94a3b8;

            .el-icon {
              font-size: 18px;
            }

            &:hover:not(.active) {
              border-color: #cbd5e1;
              background: #f1f5f9;
              color: #64748b;
            }
          }

          // 活跃状态选中
          .toggle-option:first-child.active {
            border-color: #10b981;
            background: #ecfdf5;
            color: #059669;

            .el-icon {
              color: #10b981;
            }
          }

          // 停用状态选中
          .toggle-option:last-child.active {
            border-color: #ef4444;
            background: #fef2f2;
            color: #dc2626;

            .el-icon {
              color: #ef4444;
            }
          }
        }
      }
    }
  }
}
</style>
