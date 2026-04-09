<template>
  <el-config-provider :locale="zhCn">
    <div class="app-container">
      <el-container>
        <!-- 侧边栏 -->
        <el-aside width="260px" class="sidebar">
          <div class="logo">
            <div class="logo-icon">
              <el-icon size="28"><TrendCharts /></el-icon>
            </div>
            <div class="logo-text">
              <span class="title">Stock AI</span>
              <span class="subtitle">Analyzer</span>
            </div>
          </div>

          <div class="nav-section">
            <span class="nav-label">主菜单</span>
            <el-menu
              :default-active="route.path"
              router
              background-color="transparent"
              text-color="rgba(255,255,255,0.7)"
              active-text-color="#ffffff"
            >
              <el-menu-item index="/dashboard">
                <el-icon><DataAnalysis /></el-icon>
                <span>仪表盘</span>
              </el-menu-item>
              <el-menu-item index="/stocks">
                <el-icon><Collection /></el-icon>
                <span>股票管理</span>
              </el-menu-item>
              <el-menu-item index="/analysis">
                <el-icon><Cpu /></el-icon>
                <span>AI 分析</span>
              </el-menu-item>
              <el-menu-item index="/statistics">
                <el-icon><PieChart /></el-icon>
                <span>统计报表</span>
              </el-menu-item>
            </el-menu>
          </div>

          <div class="nav-section">
            <span class="nav-label">系统</span>
            <el-menu
              :default-active="route.path"
              router
              background-color="transparent"
              text-color="rgba(255,255,255,0.7)"
              active-text-color="#ffffff"
            >
              <el-menu-item index="/settings">
                <el-icon><Setting /></el-icon>
                <span>系统设置</span>
              </el-menu-item>
            </el-menu>
          </div>

          <!-- 底部状态 -->
          <div class="sidebar-footer">
            <div class="ai-status" :class="{ 'is-mock': aiMode === 'Mock AI' }">
              <el-icon><Cpu /></el-icon>
              <span>{{ aiMode }}</span>
            </div>
          </div>
        </el-aside>

        <!-- 主内容区 -->
        <el-container class="main-container">
          <el-header class="header">
            <div class="header-left">
              <h1 class="page-title">{{ currentPageTitle }}</h1>
              <el-breadcrumb separator="/">
                <el-breadcrumb-item :to="{ path: '/' }"
                  >首页</el-breadcrumb-item
                >
                <el-breadcrumb-item>{{ currentPageTitle }}</el-breadcrumb-item>
              </el-breadcrumb>
            </div>
            <div class="header-right">
              <el-tooltip content="系统状态" placement="bottom">
                <div
                  class="status-indicator"
                  :class="healthClass"
                  @click="checkHealth"
                >
                  <span class="status-dot"></span>
                  <span class="status-text">{{ healthStatus }}</span>
                </div>
              </el-tooltip>
              <el-divider direction="vertical" />
              <el-tooltip content="刷新数据" placement="bottom">
                <el-button :icon="Refresh" circle @click="refreshPage" />
              </el-tooltip>
            </div>
          </el-header>

          <el-main class="main-content">
            <router-view v-slot="{ Component }">
              <transition name="page-fade" mode="out-in">
                <component :is="Component" />
              </transition>
            </router-view>
          </el-main>

          <el-footer class="footer">
            <span>Stock AI Analyzer v1.0.0</span>
            <span class="divider">|</span>
            <span>Powered by ASP.NET Core 8 + Vue 3</span>
          </el-footer>
        </el-container>
      </el-container>
    </div>
  </el-config-provider>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  Refresh,
  TrendCharts,
  DataAnalysis,
  Collection,
  Cpu,
  PieChart,
  Setting,
} from "@element-plus/icons-vue";
import zhCn from "element-plus/es/locale/lang/zh-cn";
import { healthApi } from "@/api";
import { logger } from "@/utils/logger";

const route = useRoute();
const router = useRouter();

const healthStatus = ref("检查中...");
const healthClass = ref("checking");
const aiMode = ref("Mock AI");

const currentPageTitle = computed(() => {
  const titles: Record<string, string> = {
    "/dashboard": "仪表盘",
    "/stocks": "股票管理",
    "/analysis": "AI 分析",
    "/statistics": "统计报表",
    "/settings": "系统设置",
  };
  return titles[route.path] || "首页";
});

const checkHealth = async () => {
  healthStatus.value = "检查中...";
  healthClass.value = "checking";

  try {
    logger.info("开始健康检查");
    const res = await healthApi.ready();

    if (res.status === "Ready") {
      healthStatus.value = "运行正常";
      healthClass.value = "healthy";
      logger.info("健康检查通过", res);
    } else {
      healthStatus.value = "部分异常";
      healthClass.value = "warning";
      logger.warn("健康检查警告", res);
    }

    // 获取 AI 模式
    if (res.checks?.aiService?.description) {
      aiMode.value = res.checks.aiService.description.includes("Mock")
        ? "Mock AI"
        : "OpenAI";
    }
  } catch (error) {
    healthStatus.value = "连接失败";
    healthClass.value = "error";
    logger.error("健康检查失败", error);
  }
};

const refreshPage = () => {
  logger.info("刷新页面", { path: route.path });
  router.go(0);
};

onMounted(() => {
  logger.info("应用启动", { timestamp: new Date().toISOString() });
  checkHealth();
});
</script>

<style lang="scss" scoped>
.app-container {
  min-height: 100vh;
  background: var(--bg-color);
}

.sidebar {
  background: var(--sidebar-bg);
  box-shadow: 4px 0 20px rgba(0, 0, 0, 0.15);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: fixed;
  left: 0;
  top: 0;
  bottom: 0;
  width: 260px;
  z-index: 100;

  .logo {
    height: 80px;
    display: flex;
    align-items: center;
    padding: 0 24px;
    gap: 14px;
    border-bottom: 1px solid rgba(255, 255, 255, 0.08);

    .logo-icon {
      width: 48px;
      height: 48px;
      background: linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%);
      border-radius: 12px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
    }

    .logo-text {
      display: flex;
      flex-direction: column;

      .title {
        color: #fff;
        font-size: 18px;
        font-weight: 700;
        letter-spacing: -0.5px;
      }

      .subtitle {
        color: rgba(255, 255, 255, 0.6);
        font-size: 12px;
        font-weight: 500;
      }
    }
  }

  .nav-section {
    padding: 20px 16px 8px;

    .nav-label {
      display: block;
      font-size: 11px;
      font-weight: 600;
      color: rgba(255, 255, 255, 0.4);
      text-transform: uppercase;
      letter-spacing: 1px;
      padding: 0 12px;
      margin-bottom: 8px;
    }
  }

  .el-menu {
    border-right: none;

    .el-menu-item {
      height: 48px;
      line-height: 48px;
      margin: 4px 0;
      border-radius: 10px;
      transition: all 0.2s ease;

      .el-icon {
        font-size: 20px;
        margin-right: 12px;
      }

      &:hover {
        background: rgba(255, 255, 255, 0.08) !important;
        color: #fff !important;
      }

      &.is-active {
        background: linear-gradient(
          90deg,
          rgba(59, 130, 246, 0.9) 0%,
          rgba(139, 92, 246, 0.9) 100%
        ) !important;
        color: #fff !important;
        box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);

        .el-icon {
          color: #fff;
        }
      }
    }
  }

  .sidebar-footer {
    margin-top: auto;
    padding: 20px;
    border-top: 1px solid rgba(255, 255, 255, 0.08);

    .ai-status {
      display: flex;
      align-items: center;
      gap: 10px;
      padding: 12px 16px;
      background: rgba(16, 185, 129, 0.15);
      border-radius: 10px;
      color: #34d399;
      font-size: 13px;
      font-weight: 500;

      &.is-mock {
        background: rgba(245, 158, 11, 0.15);
        color: #fbbf24;
      }

      .el-icon {
        font-size: 18px;
      }
    }
  }
}

.main-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  margin-left: 260px;
}

.header {
  background: #fff;
  height: 72px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 32px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
  border-bottom: 1px solid var(--border-light);

  .header-left {
    .page-title {
      font-size: 22px;
      font-weight: 700;
      color: var(--text-primary);
      margin: 0 0 4px 0;
      letter-spacing: -0.5px;
    }

    .el-breadcrumb {
      font-size: 13px;
    }
  }

  .header-right {
    display: flex;
    align-items: center;
    gap: 16px;

    .status-indicator {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 16px;
      border-radius: 20px;
      cursor: pointer;
      transition: all 0.2s ease;
      font-size: 13px;
      font-weight: 500;

      .status-dot {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        animation: pulse 2s infinite;
      }

      &.healthy {
        background: #d1fae5;
        color: #047857;

        .status-dot {
          background: #10b981;
        }
      }

      &.warning {
        background: #fef3c7;
        color: #b45309;

        .status-dot {
          background: #f59e0b;
        }
      }

      &.error {
        background: #fee2e2;
        color: #dc2626;

        .status-dot {
          background: #ef4444;
        }
      }

      &.checking {
        background: #e5e7eb;
        color: #6b7280;

        .status-dot {
          background: #9ca3af;
        }
      }

      &:hover {
        transform: scale(1.02);
      }
    }

    .el-divider {
      height: 24px;
    }
  }
}

.main-content {
  flex: 1;
  padding: 28px 32px;
  background: var(--bg-color);
  overflow: visible;
}

.footer {
  height: 48px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  background: #fff;
  border-top: 1px solid var(--border-light);
  font-size: 12px;
  color: var(--text-secondary);

  .divider {
    color: var(--text-muted);
  }
}

// 页面切换动画
.page-fade-enter-active,
.page-fade-leave-active {
  transition: all 0.25s ease;
}

.page-fade-enter-from {
  opacity: 0;
  transform: translateY(12px);
}

.page-fade-leave-to {
  opacity: 0;
  transform: translateY(-12px);
}

@keyframes pulse {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.6;
    transform: scale(0.9);
  }
}
</style>
