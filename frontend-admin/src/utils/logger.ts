/**
 * 前端日志系统
 * 支持多级别日志、格式化输出、本地存储
 */

export enum LogLevel {
  DEBUG = 0,
  INFO = 1,
  WARN = 2,
  ERROR = 3,
}

interface LogEntry {
  timestamp: string;
  level: string;
  message: string;
  data?: any;
  stack?: string;
}

class Logger {
  private level: LogLevel = LogLevel.DEBUG;
  private logs: LogEntry[] = [];
  private maxLogs: number = 500;
  private storageKey: string = "stock_analyzer_logs";

  constructor() {
    this.loadFromStorage();
  }

  setLevel(level: LogLevel) {
    this.level = level;
  }

  private formatTime(): string {
    return new Date().toISOString();
  }

  private getLogStyle(level: LogLevel): string {
    switch (level) {
      case LogLevel.DEBUG:
        return "color: #9ca3af; font-weight: normal;";
      case LogLevel.INFO:
        return "color: #3b82f6; font-weight: bold;";
      case LogLevel.WARN:
        return "color: #f59e0b; font-weight: bold;";
      case LogLevel.ERROR:
        return "color: #ef4444; font-weight: bold;";
      default:
        return "";
    }
  }

  private getLevelName(level: LogLevel): string {
    switch (level) {
      case LogLevel.DEBUG:
        return "DEBUG";
      case LogLevel.INFO:
        return "INFO";
      case LogLevel.WARN:
        return "WARN";
      case LogLevel.ERROR:
        return "ERROR";
      default:
        return "UNKNOWN";
    }
  }

  private log(level: LogLevel, message: string, data?: any) {
    if (level < this.level) return;

    const entry: LogEntry = {
      timestamp: this.formatTime(),
      level: this.getLevelName(level),
      message,
      data: data ? JSON.parse(JSON.stringify(data)) : undefined,
    };

    // 控制台输出
    const prefix = `[${entry.timestamp}] [${entry.level}]`;
    const style = this.getLogStyle(level);

    if (data) {
      console.groupCollapsed(`%c${prefix} ${message}`, style);
      console.log("Data:", data);
      console.groupEnd();
    } else {
      console.log(`%c${prefix} ${message}`, style);
    }

    // 存储日志
    this.logs.push(entry);
    if (this.logs.length > this.maxLogs) {
      this.logs = this.logs.slice(-this.maxLogs);
    }
    this.saveToStorage();
  }

  debug(message: string, data?: any) {
    this.log(LogLevel.DEBUG, message, data);
  }

  info(message: string, data?: any) {
    this.log(LogLevel.INFO, message, data);
  }

  warn(message: string, data?: any) {
    this.log(LogLevel.WARN, message, data);
  }

  error(message: string, error?: any) {
    const entry: LogEntry = {
      timestamp: this.formatTime(),
      level: "ERROR",
      message,
      data: error?.message || error,
      stack: error?.stack,
    };

    console.error(
      `%c[${entry.timestamp}] [ERROR] ${message}`,
      "color: #ef4444; font-weight: bold;",
      error,
    );

    this.logs.push(entry);
    if (this.logs.length > this.maxLogs) {
      this.logs = this.logs.slice(-this.maxLogs);
    }
    this.saveToStorage();
  }

  // API 请求日志
  apiRequest(method: string, url: string, params?: any) {
    this.info(`API Request: ${method.toUpperCase()} ${url}`, params);
  }

  apiResponse(method: string, url: string, status: number, data?: any) {
    if (status >= 200 && status < 300) {
      this.info(`API Response: ${method.toUpperCase()} ${url} [${status}]`, {
        responseData: data,
      });
    } else {
      this.warn(`API Response: ${method.toUpperCase()} ${url} [${status}]`, {
        responseData: data,
      });
    }
  }

  apiError(method: string, url: string, error: any) {
    this.error(`API Error: ${method.toUpperCase()} ${url}`, error);
  }

  // 用户操作日志
  userAction(action: string, details?: any) {
    this.info(`User Action: ${action}`, details);
  }

  // 页面导航日志
  navigation(from: string, to: string) {
    this.debug(`Navigation: ${from} -> ${to}`);
  }

  // 获取所有日志
  getLogs(): LogEntry[] {
    return [...this.logs];
  }

  // 获取特定级别的日志
  getLogsByLevel(level: LogLevel): LogEntry[] {
    const levelName = this.getLevelName(level);
    return this.logs.filter((log) => log.level === levelName);
  }

  // 导出日志
  exportLogs(): string {
    return JSON.stringify(this.logs, null, 2);
  }

  // 清空日志
  clearLogs() {
    this.logs = [];
    this.saveToStorage();
    console.log("%c[Logger] Logs cleared", "color: #9ca3af;");
  }

  // 保存到本地存储
  private saveToStorage() {
    try {
      const recentLogs = this.logs.slice(-100); // 只保存最近100条
      localStorage.setItem(this.storageKey, JSON.stringify(recentLogs));
    } catch (e) {
      // 存储可能已满，忽略错误
    }
  }

  // 从本地存储加载
  private loadFromStorage() {
    try {
      const stored = localStorage.getItem(this.storageKey);
      if (stored) {
        this.logs = JSON.parse(stored);
      }
    } catch (e) {
      this.logs = [];
    }
  }
}

// 创建单例
export const logger = new Logger();

// 设置为全局可访问（便于调试）
if (typeof window !== "undefined") {
  (window as any).__logger = logger;
}

export default logger;
