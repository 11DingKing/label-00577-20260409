<template>
  <div class="settings-page">
    <el-row :gutter="24" class="settings-row">
      <!-- 左列：AI 服务配置 -->
      <el-col :xs="24" :lg="12">
        <div class="page-card config-card">
          <div class="card-header">
            <span class="card-title">
              <el-icon><Setting /></el-icon>
              AI 服务配置
            </span>
          </div>

          <el-alert type="warning" :closable="false" class="config-alert">
            <template #title>
              <strong>当前模式：Mock AI（演示模式）</strong>
            </template>
            <p>
              系统当前使用模拟 AI 服务生成分析结果。如需使用真实 AI
              分析，请按以下步骤配置：
            </p>
          </el-alert>

          <div class="config-section">
            <h4>切换到 OpenAI</h4>
            <p>修改 <code>docker-compose.yml</code> 中的环境变量：</p>
            <el-input
              type="textarea"
              :rows="6"
              readonly
              :value="openaiConfig"
            />
            <el-button
              type="primary"
              size="small"
              @click="copyConfig"
              class="copy-btn"
            >
              <el-icon><DocumentCopy /></el-icon>
              复制配置
            </el-button>
          </div>

          <div class="config-section">
            <h4>支持的 AI 提供商</h4>
            <el-descriptions :column="1" border>
              <el-descriptions-item label="Mock">
                模拟 AI，用于演示和测试
              </el-descriptions-item>
              <el-descriptions-item label="OpenAI">
                GPT-4, GPT-3.5-turbo
              </el-descriptions-item>
            </el-descriptions>
          </div>
        </div>
      </el-col>

      <!-- 右列：系统状态 + 关于 -->
      <el-col :xs="24" :lg="12">
        <div class="right-column">
          <div class="page-card status-card">
            <div class="card-header">
              <span class="card-title">
                <el-icon><Monitor /></el-icon>
                系统状态
              </span>
            </div>

            <el-descriptions :column="1" border class="status-desc">
              <el-descriptions-item label="API 服务">
                <el-tag
                  :type="healthStatus === 'Healthy' ? 'success' : 'danger'"
                  effect="light"
                >
                  {{ healthStatus === "Healthy" ? "Ready" : healthStatus }}
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="数据库">
                <el-tag
                  :type="dbStatus === 'Connected' ? 'success' : 'danger'"
                  effect="light"
                >
                  {{ dbStatus }}
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="AI 服务">
                <el-tag
                  :type="aiStatus === 'Available' ? 'success' : 'warning'"
                  effect="light"
                >
                  {{ aiStatus }}
                </el-tag>
              </el-descriptions-item>
            </el-descriptions>

            <el-button @click="checkHealth" class="refresh-btn">
              <el-icon><Refresh /></el-icon>
              刷新状态
            </el-button>
          </div>

          <div class="page-card about-card">
            <div class="card-header">
              <span class="card-title">
                <el-icon><InfoFilled /></el-icon>
                关于
              </span>
            </div>

            <el-descriptions :column="1" border class="about-desc">
              <el-descriptions-item label="系统名称">
                Stock AI Analyzer
              </el-descriptions-item>
              <el-descriptions-item label="版本">
                <el-tag type="info" effect="plain" size="small">v1.0.0</el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="技术栈">
                <div class="tech-tags">
                  <el-tag size="small">C# / ASP.NET Core 8</el-tag>
                  <el-tag size="small">Vue 3</el-tag>
                  <el-tag size="small">Element Plus</el-tag>
                  <el-tag size="small">SQLite</el-tag>
                </div>
              </el-descriptions-item>
              <el-descriptions-item label="功能">
                <ul class="feature-list">
                  <li>股票列表管理</li>
                  <li>AI 投资建议分析</li>
                  <li>历史结果记录</li>
                  <li>连续建议统计</li>
                </ul>
              </el-descriptions-item>
            </el-descriptions>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";
import { ElMessage, ElLoading } from "element-plus";
import {
  Setting,
  Monitor,
  Refresh,
  InfoFilled,
  DocumentCopy,
} from "@element-plus/icons-vue";
import { healthApi } from "@/api";

const healthStatus = ref("Checking...");
const dbStatus = ref("Checking...");
const aiStatus = ref("Checking...");

const openaiConfig = `environment:
  - AiSettings__Provider=OpenAI
  - AiSettings__ApiKey=sk-your-api-key-here
  - AiSettings__Model=gpt-4
  - AiSettings__MaxTokens=500
  - AiSettings__Temperature=0.3`;

const copyConfig = () => {
  navigator.clipboard.writeText(openaiConfig);
  ElMessage.success("配置已复制到剪贴板");
};

const checkHealth = async () => {
  const loading = ElLoading.service({
    lock: true,
    text: "刷新状态中...",
    background: "rgba(255, 255, 255, 0.9)",
  });

  try {
    const res = await healthApi.ready();
    healthStatus.value = res.status || "Unknown";
    dbStatus.value = res.checks?.database?.status || "Unknown";
    aiStatus.value = res.checks?.aiService?.status || "Unknown";
  } catch {
    healthStatus.value = "Error";
    dbStatus.value = "Error";
    aiStatus.value = "Error";
  } finally {
    loading.close();
  }
};

onMounted(() => {
  checkHealth();
});
</script>

<style lang="scss" scoped>
.settings-page {
  .settings-row {
    align-items: stretch;

    > .el-col {
      margin-bottom: 24px;
    }
  }

  // 左列配置卡片
  .config-card {
    height: 100%;
    min-height: 520px;
  }

  // 右列容器
  .right-column {
    display: flex;
    flex-direction: column;
    gap: 24px;
    height: 100%;
  }

  // 状态卡片
  .status-card {
    flex: 0 0 auto;

    .status-desc {
      :deep(.el-descriptions__label) {
        width: 100px;
        font-weight: 500;
      }
    }

    .refresh-btn {
      margin-top: 16px;
    }
  }

  // 关于卡片
  .about-card {
    flex: 1;

    .about-desc {
      :deep(.el-descriptions__label) {
        width: 100px;
        font-weight: 500;
      }
    }

    .tech-tags {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }

    .feature-list {
      margin: 0;
      padding-left: 18px;

      li {
        line-height: 1.8;
        color: var(--text-secondary);
      }
    }
  }

  // 配置提示
  .config-alert {
    margin-bottom: 20px;

    p {
      margin: 8px 0 0 0;
      font-size: 13px;
    }
  }

  // 配置区块
  .config-section {
    margin-top: 24px;

    h4 {
      margin: 0 0 12px 0;
      font-size: 14px;
      font-weight: 600;
      color: var(--text-primary);
    }

    p {
      margin: 0 0 8px 0;
      color: var(--text-secondary);
      font-size: 13px;
    }

    code {
      background: #f5f7fa;
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 13px;
      color: var(--primary-color);
    }

    .copy-btn {
      margin-top: 12px;
    }
  }
}

// 响应式
@media (max-width: 1200px) {
  .settings-page {
    .config-card {
      min-height: auto;
    }

    .right-column {
      height: auto;
    }
  }
}
</style>
