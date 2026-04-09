import { createRouter, createWebHistory } from "vue-router";

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      redirect: "/dashboard",
    },
    {
      path: "/dashboard",
      name: "Dashboard",
      component: () => import("@/views/Dashboard.vue"),
      meta: { title: "仪表盘" },
    },
    {
      path: "/stocks",
      name: "Stocks",
      component: () => import("@/views/Stocks.vue"),
      meta: { title: "股票管理" },
    },
    {
      path: "/analysis",
      name: "Analysis",
      component: () => import("@/views/Analysis.vue"),
      meta: { title: "AI 分析" },
    },
    {
      path: "/statistics",
      name: "Statistics",
      component: () => import("@/views/Statistics.vue"),
      meta: { title: "统计报表" },
    },
    {
      path: "/settings",
      name: "Settings",
      component: () => import("@/views/Settings.vue"),
      meta: { title: "系统设置" },
    },
  ],
});

router.beforeEach((to, _from, next) => {
  document.title = `${to.meta.title || "Stock AI Analyzer"} - 股票AI分析工具`;
  next();
});

export default router;
