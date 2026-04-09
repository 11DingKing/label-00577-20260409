import { describe, it, expect, beforeEach, vi } from "vitest";
import { logger, LogLevel } from "@/utils/logger";

describe("Logger", () => {
  beforeEach(() => {
    // 清空日志
    logger.clearLogs();
    // Mock console
    vi.spyOn(console, "log").mockImplementation(() => {});
    vi.spyOn(console, "error").mockImplementation(() => {});
    vi.spyOn(console, "groupCollapsed").mockImplementation(() => {});
    vi.spyOn(console, "groupEnd").mockImplementation(() => {});
  });

  describe("基本日志功能", () => {
    it("应该记录 info 级别日志", () => {
      logger.info("测试信息");
      const logs = logger.getLogs();

      expect(logs.length).toBe(1);
      expect(logs[0].level).toBe("INFO");
      expect(logs[0].message).toBe("测试信息");
    });

    it("应该记录带数据的日志", () => {
      logger.info("测试信息", { key: "value" });
      const logs = logger.getLogs();

      expect(logs[0].data).toEqual({ key: "value" });
    });

    it("应该记录 error 级别日志", () => {
      const error = new Error("测试错误");
      logger.error("发生错误", error);
      const logs = logger.getLogs();

      expect(logs[0].level).toBe("ERROR");
      expect(logs[0].message).toBe("发生错误");
    });

    it("应该记录 warn 级别日志", () => {
      logger.warn("警告信息");
      const logs = logger.getLogs();

      expect(logs[0].level).toBe("WARN");
    });

    it("应该记录 debug 级别日志", () => {
      logger.debug("调试信息");
      const logs = logger.getLogs();

      expect(logs[0].level).toBe("DEBUG");
    });
  });

  describe("日志级别过滤", () => {
    it("应该根据级别过滤日志", () => {
      logger.setLevel(LogLevel.WARN);
      logger.debug("调试");
      logger.info("信息");
      logger.warn("警告");
      logger.error("错误", "test error");

      const logs = logger.getLogs();
      // DEBUG 和 INFO 应该被过滤
      expect(logs.length).toBe(2);
      expect(logs[0].level).toBe("WARN");
      expect(logs[1].level).toBe("ERROR");

      // 恢复默认级别
      logger.setLevel(LogLevel.DEBUG);
    });
  });

  describe("API 日志", () => {
    it("应该记录 API 请求", () => {
      logger.apiRequest("GET", "/api/stocks", { page: 1 });
      const logs = logger.getLogs();

      expect(logs[0].message).toContain("API Request");
      expect(logs[0].message).toContain("GET");
      expect(logs[0].message).toContain("/api/stocks");
    });

    it("应该记录 API 响应", () => {
      logger.apiResponse("GET", "/api/stocks", 200, { data: [] });
      const logs = logger.getLogs();

      expect(logs[0].message).toContain("API Response");
      expect(logs[0].message).toContain("200");
    });

    it("应该记录 API 错误", () => {
      logger.apiError("POST", "/api/analysis", new Error("网络错误"));
      const logs = logger.getLogs();

      expect(logs[0].level).toBe("ERROR");
      expect(logs[0].message).toContain("API Error");
    });
  });

  describe("用户操作日志", () => {
    it("应该记录用户操作", () => {
      logger.userAction("点击按钮", { buttonId: "submit" });
      const logs = logger.getLogs();

      expect(logs[0].message).toContain("User Action");
      expect(logs[0].message).toContain("点击按钮");
    });
  });

  describe("日志导出", () => {
    it("应该导出日志为 JSON", () => {
      logger.info("测试1");
      logger.info("测试2");

      const exported = logger.exportLogs();
      const parsed = JSON.parse(exported);

      expect(Array.isArray(parsed)).toBe(true);
      expect(parsed.length).toBe(2);
    });
  });

  describe("日志清理", () => {
    it("应该清空所有日志", () => {
      logger.info("测试1");
      logger.info("测试2");

      expect(logger.getLogs().length).toBe(2);

      logger.clearLogs();

      expect(logger.getLogs().length).toBe(0);
    });
  });

  describe("按级别获取日志", () => {
    it("应该按级别过滤日志", () => {
      logger.info("信息1");
      logger.warn("警告1");
      logger.info("信息2");
      logger.error("错误1", "test");

      const infoLogs = logger.getLogsByLevel(LogLevel.INFO);
      const warnLogs = logger.getLogsByLevel(LogLevel.WARN);

      expect(infoLogs.length).toBe(2);
      expect(warnLogs.length).toBe(1);
    });
  });
});
