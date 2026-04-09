import { describe, it, expect } from "vitest";

// 工具函数测试
describe("工具函数", () => {
  describe("推荐类型映射", () => {
    const getRecommendationType = (rec: string) => {
      const map: Record<string, "success" | "warning" | "danger"> = {
        Buy: "success",
        Hold: "warning",
        Sell: "danger",
      };
      return map[rec] || "info";
    };

    it("Buy 应返回 success", () => {
      expect(getRecommendationType("Buy")).toBe("success");
    });

    it("Hold 应返回 warning", () => {
      expect(getRecommendationType("Hold")).toBe("warning");
    });

    it("Sell 应返回 danger", () => {
      expect(getRecommendationType("Sell")).toBe("danger");
    });

    it("未知值应返回 info", () => {
      expect(getRecommendationType("Unknown")).toBe("info");
    });
  });

  describe("推荐文本映射", () => {
    const getRecommendationText = (rec: string) => {
      const map: Record<string, string> = {
        Buy: "买入",
        Hold: "持有",
        Sell: "卖出",
      };
      return map[rec] || rec;
    };

    it("Buy 应返回 买入", () => {
      expect(getRecommendationText("Buy")).toBe("买入");
    });

    it("Hold 应返回 持有", () => {
      expect(getRecommendationText("Hold")).toBe("持有");
    });

    it("Sell 应返回 卖出", () => {
      expect(getRecommendationText("Sell")).toBe("卖出");
    });

    it("未知值应返回原值", () => {
      expect(getRecommendationText("Unknown")).toBe("Unknown");
    });
  });

  describe("置信度颜色", () => {
    const getConfidenceColor = (confidence: number) => {
      if (confidence >= 80) return "#10b981";
      if (confidence >= 60) return "#f59e0b";
      return "#ef4444";
    };

    it("高置信度 (>=80) 应返回绿色", () => {
      expect(getConfidenceColor(80)).toBe("#10b981");
      expect(getConfidenceColor(95)).toBe("#10b981");
    });

    it("中等置信度 (60-79) 应返回橙色", () => {
      expect(getConfidenceColor(60)).toBe("#f59e0b");
      expect(getConfidenceColor(79)).toBe("#f59e0b");
    });

    it("低置信度 (<60) 应返回红色", () => {
      expect(getConfidenceColor(59)).toBe("#ef4444");
      expect(getConfidenceColor(30)).toBe("#ef4444");
    });
  });

  describe("股票代码验证", () => {
    const isValidSymbol = (symbol: string) => {
      // 股票代码: 1-10 个大写字母或数字
      return /^[A-Z0-9]{1,10}$/.test(symbol.toUpperCase());
    };

    it("有效的股票代码", () => {
      expect(isValidSymbol("AAPL")).toBe(true);
      expect(isValidSymbol("GOOGL")).toBe(true);
      expect(isValidSymbol("A")).toBe(true);
      expect(isValidSymbol("000001")).toBe(true);
    });

    it("无效的股票代码", () => {
      expect(isValidSymbol("")).toBe(false);
      expect(isValidSymbol("VERYLONGSYMBOL123")).toBe(false);
      expect(isValidSymbol("AAP L")).toBe(false);
      expect(isValidSymbol("aapl!")).toBe(false);
    });
  });

  describe("批量导入解析", () => {
    const parseBatchInput = (text: string) => {
      const lines = text
        .trim()
        .split("\n")
        .filter((l) => l.trim());
      return lines
        .map((line) => {
          const [symbol, name] = line.split(",").map((s) => s.trim());
          return {
            symbol: symbol?.toUpperCase() || "",
            name: name || symbol || "",
          };
        })
        .filter((s) => s.symbol);
    };

    it("应该正确解析标准格式", () => {
      const input = `AAPL,Apple Inc.
GOOGL,Alphabet Inc.
MSFT,Microsoft`;
      const result = parseBatchInput(input);

      expect(result.length).toBe(3);
      expect(result[0]).toEqual({ symbol: "AAPL", name: "Apple Inc." });
      expect(result[1]).toEqual({ symbol: "GOOGL", name: "Alphabet Inc." });
    });

    it("应该处理只有代码没有名称的情况", () => {
      const input = `AAPL
GOOGL`;
      const result = parseBatchInput(input);

      expect(result[0]).toEqual({ symbol: "AAPL", name: "AAPL" });
    });

    it("应该忽略空行", () => {
      const input = `AAPL,Apple

GOOGL,Google

`;
      const result = parseBatchInput(input);

      expect(result.length).toBe(2);
    });

    it("应该转换为大写", () => {
      const input = `aapl,Apple`;
      const result = parseBatchInput(input);

      expect(result[0].symbol).toBe("AAPL");
    });
  });

  describe("日期格式化", () => {
    const formatDate = (dateStr: string) => {
      const date = new Date(dateStr);
      const year = date.getFullYear();
      const month = String(date.getMonth() + 1).padStart(2, "0");
      const day = String(date.getDate()).padStart(2, "0");
      const hours = String(date.getHours()).padStart(2, "0");
      const minutes = String(date.getMinutes()).padStart(2, "0");
      return `${year}-${month}-${day} ${hours}:${minutes}`;
    };

    it("应该格式化 ISO 日期字符串", () => {
      const result = formatDate("2024-01-15T10:30:00Z");
      expect(result).toMatch(/2024-01-15/);
    });
  });
});
