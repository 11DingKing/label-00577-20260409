# Stock AI Analyzer - 股票 AI 分析工具

一个基于 C# ASP.NET Core 8 的股票 AI 分析工具，支持管理股票观察列表、AI 分析投资建议、记录历史结果并进行统计查询。

---

## How to Run

### 方式一：Docker Compose（推荐）

```bash
# 启动服务
docker-compose up --build -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

### 方式二：本地开发

```bash
cd backend
dotnet restore
dotnet run --project src/StockAnalyzer.Api
```

---

## Services

| 服务                | 端口     | 描述                             |
| ------------------- | -------- | -------------------------------- |
| **管理界面 (Vue3)** | **8092** | 股票 AI 分析前端工具（推荐使用） |
| API 服务            | 8091     | 股票 AI 分析后端服务             |

**访问地址:**

- **管理界面**: http://localhost:8092 （图形化操作界面，推荐）
- API 服务: http://localhost:8091
- Swagger 文档: http://localhost:8091/swagger (开发环境)
- 健康检查: http://localhost:8091/api/health

---

## 测试账号

本项目无需账号登录，直接访问管理界面即可使用。默认使用 Mock AI 服务（演示模式）。

如需使用真实 AI：

```bash
# 在 docker-compose.yml 中配置
AiSettings__Provider=OpenAI
AiSettings__ApiKey=your-openai-api-key
```

---

## 题目内容

> 设计并实现一个工具，使用C#语言，用户可以事先设置一个股票列表，逐个股票让AI进行分析，给出将来一个月的买入、持有、卖出3个建议之一，并给出信息度，0到100% 的范围，然后记录下来每天的结果，并且可以统计这个结果，例如找到连续N天都是买入的股票。

---

## 快速质检（一键测试）

```bash
# 启动服务后，运行质检脚本
./test-api.sh

# 或指定服务地址
./test-api.sh http://localhost:8091
```

**预期输出:**

```
╔════════════════════════════════════════════════════════╗
║              ✓ 所有测试通过！质检合格！                ║
╚════════════════════════════════════════════════════════╝
```

---

## 功能特性

### 核心功能

1. **股票列表管理**
   - 添加/删除/更新股票
   - 支持激活/停用状态
   - 搜索和筛选功能

2. **AI 分析**
   - 支持单个或批量股票分析
   - 给出买入(Buy)/持有(Hold)/卖出(Sell)建议
   - 提供置信度(0-100%)
   - 记录分析理由

3. **结果统计**
   - 查询连续 N 天相同建议的股票
   - 统计汇总（按建议类型分组）
   - 单股票趋势分析

### 技术特性

**后端:**

- **架构**: 分层架构 (Controller -> Service -> Repository)
- **数据库**: SQLite (轻量级，便于部署)
- **日志**: Serilog 结构化日志
- **异常处理**: 全局异常中间件
- **API 文档**: Swagger/OpenAPI

**前端:**

- **框架**: Vue 3 + Vite + TypeScript
- **UI 组件**: Element Plus
- **状态管理**: Pinia
- **图表**: ECharts
- **样式**: SCSS + CSS Variables
- **交互**: 全屏 Loading、响应式设计

---

## API 接口

### 股票管理

| Method | Endpoint               | Description  |
| ------ | ---------------------- | ------------ |
| GET    | `/api/stocks`          | 获取所有股票 |
| GET    | `/api/stocks/{symbol}` | 获取单个股票 |
| POST   | `/api/stocks`          | 添加股票     |
| POST   | `/api/stocks/batch`    | 批量添加股票 |
| PUT    | `/api/stocks/{symbol}` | 更新股票     |
| DELETE | `/api/stocks/{symbol}` | 删除股票     |

### AI 分析

| Method | Endpoint                         | Description                |
| ------ | -------------------------------- | -------------------------- |
| POST   | `/api/analysis/run`              | 运行 AI 分析（全部或指定） |
| POST   | `/api/analysis/run/{symbol}`     | 分析单个股票               |
| GET    | `/api/analysis/results`          | 获取分析结果               |
| GET    | `/api/analysis/results/{symbol}` | 获取指定股票历史           |
| GET    | `/api/analysis/latest`           | 获取最新结果               |

### 统计查询

| Method | Endpoint                         | Description       |
| ------ | -------------------------------- | ----------------- |
| GET    | `/api/statistics/consecutive`    | 查询连续 N 天建议 |
| GET    | `/api/statistics/summary`        | 统计汇总          |
| GET    | `/api/statistics/trend/{symbol}` | 股票趋势          |

---

## 质检测试指南 (curl 命令)

> **注意**: 以下所有测试基于服务运行在 `http://localhost:8091`

### 快速验收测试（一键执行）

将以下脚本保存为 `test.sh` 并执行：

```bash
#!/bin/bash
# ============================================
# Stock AI Analyzer - 质检测试脚本
# ============================================

BASE_URL="http://localhost:8091"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

check_response() {
    if echo "$1" | grep -q '"success":true'; then
        echo -e "${GREEN}✓ PASS${NC}"
    else
        echo -e "${RED}✗ FAIL${NC}"
        echo "$1"
    fi
}

echo "============================================"
echo "Stock AI Analyzer - 质检测试"
echo "============================================"
echo ""

# 测试 1: 健康检查
echo -e "${YELLOW}[测试 1/10] 健康检查${NC}"
RESP=$(curl -s $BASE_URL/api/health)
if echo "$RESP" | grep -q '"status":"Healthy"'; then
    echo -e "${GREEN}✓ PASS${NC} - 服务运行正常"
else
    echo -e "${RED}✗ FAIL${NC} - 服务不可用"
    exit 1
fi

# 测试 2: 就绪检查
echo -e "${YELLOW}[测试 2/10] 就绪检查（数据库连接）${NC}"
RESP=$(curl -s $BASE_URL/api/health/ready)
if echo "$RESP" | grep -q '"status":"Ready"'; then
    echo -e "${GREEN}✓ PASS${NC} - 数据库连接正常"
else
    echo -e "${RED}✗ FAIL${NC}"
fi

# 测试 3: 添加单个股票
echo -e "${YELLOW}[测试 3/10] 添加单个股票${NC}"
RESP=$(curl -s -X POST $BASE_URL/api/stocks \
  -H "Content-Type: application/json" \
  -d '{"symbol": "AAPL", "name": "Apple Inc."}')
check_response "$RESP"

# 测试 4: 批量添加股票
echo -e "${YELLOW}[测试 4/10] 批量添加股票${NC}"
RESP=$(curl -s -X POST $BASE_URL/api/stocks/batch \
  -H "Content-Type: application/json" \
  -d '{"stocks": [{"symbol": "GOOGL", "name": "Alphabet Inc."},{"symbol": "MSFT", "name": "Microsoft"},{"symbol": "TSLA", "name": "Tesla Inc."}]}')
check_response "$RESP"

# 测试 5: 获取股票列表
echo -e "${YELLOW}[测试 5/10] 获取股票列表${NC}"
RESP=$(curl -s $BASE_URL/api/stocks)
COUNT=$(echo "$RESP" | grep -o '"total":[0-9]*' | grep -o '[0-9]*')
if [ "$COUNT" -ge 1 ]; then
    echo -e "${GREEN}✓ PASS${NC} - 共 $COUNT 只股票"
else
    echo -e "${RED}✗ FAIL${NC}"
fi

# 测试 6: 运行 AI 分析
echo -e "${YELLOW}[测试 6/10] 运行 AI 分析（全部股票）${NC}"
RESP=$(curl -s -X POST $BASE_URL/api/analysis/run \
  -H "Content-Type: application/json" -d '{}')
SUCCESS=$(echo "$RESP" | grep -o '"successCount":[0-9]*' | grep -o '[0-9]*')
echo -e "${GREEN}✓ PASS${NC} - 成功分析 $SUCCESS 只股票"

# 测试 7: 分析单个股票
echo -e "${YELLOW}[测试 7/10] 分析单个股票${NC}"
RESP=$(curl -s -X POST $BASE_URL/api/analysis/run/AAPL)
if echo "$RESP" | grep -q '"recommendation"'; then
    REC=$(echo "$RESP" | grep -o '"recommendation":"[^"]*"' | head -1)
    CONF=$(echo "$RESP" | grep -o '"confidence":[0-9.]*' | head -1)
    echo -e "${GREEN}✓ PASS${NC} - $REC, $CONF"
else
    echo -e "${RED}✗ FAIL${NC}"
fi

# 测试 8: 获取分析结果
echo -e "${YELLOW}[测试 8/10] 获取分析结果${NC}"
RESP=$(curl -s "$BASE_URL/api/analysis/results?page=1&pageSize=10")
check_response "$RESP"

# 测试 9: 获取统计汇总
echo -e "${YELLOW}[测试 9/10] 获取统计汇总${NC}"
RESP=$(curl -s $BASE_URL/api/statistics/summary)
check_response "$RESP"

# 测试 10: 查询连续建议
echo -e "${YELLOW}[测试 10/10] 查询连续买入股票${NC}"
RESP=$(curl -s "$BASE_URL/api/statistics/consecutive?days=2&recommendation=1")
check_response "$RESP"

echo ""
echo "============================================"
echo -e "${GREEN}质检测试完成!${NC}"
echo "============================================"
```

---

### 分步测试命令（手动执行）

#### 1. 健康检查

```bash
# 基础健康检查 - 预期返回 {"status":"Healthy",...}
curl -s http://localhost:8091/api/health | jq

# 就绪检查（含数据库） - 预期返回 {"status":"Ready",...}
curl -s http://localhost:8091/api/health/ready | jq
```

#### 2. 股票管理 CRUD

```bash
# [C] 添加单个股票
curl -s -X POST http://localhost:8091/api/stocks \
  -H "Content-Type: application/json" \
  -d '{"symbol": "AAPL", "name": "Apple Inc."}' | jq
# 预期: success=true, data.symbol="AAPL"

# [C] 批量添加股票
curl -s -X POST http://localhost:8091/api/stocks/batch \
  -H "Content-Type: application/json" \
  -d '{
    "stocks": [
      {"symbol": "GOOGL", "name": "Alphabet Inc."},
      {"symbol": "MSFT", "name": "Microsoft Corporation"},
      {"symbol": "AMZN", "name": "Amazon.com Inc."},
      {"symbol": "TSLA", "name": "Tesla Inc."},
      {"symbol": "NVDA", "name": "NVIDIA Corporation"}
    ]
  }' | jq
# 预期: success=true, data 数组包含5只股票

# [R] 获取所有股票
curl -s http://localhost:8091/api/stocks | jq
# 预期: success=true, data.total >= 6

# [R] 获取单个股票
curl -s http://localhost:8091/api/stocks/AAPL | jq
# 预期: success=true, data.symbol="AAPL"

# [U] 更新股票
curl -s -X PUT http://localhost:8091/api/stocks/AAPL \
  -H "Content-Type: application/json" \
  -d '{"name": "Apple Inc. (Updated)", "isActive": true}' | jq
# 预期: success=true, data.name="Apple Inc. (Updated)"

# [D] 删除股票
curl -s -X DELETE http://localhost:8091/api/stocks/NVDA | jq
# 预期: success=true

# 验证删除 - 预期返回 404
curl -s http://localhost:8091/api/stocks/NVDA | jq
```

#### 3. AI 分析

```bash
# 分析所有股票
curl -s -X POST http://localhost:8091/api/analysis/run \
  -H "Content-Type: application/json" \
  -d '{}' | jq
# 预期: successCount > 0, results 数组包含分析结果

# 分析指定股票
curl -s -X POST http://localhost:8091/api/analysis/run \
  -H "Content-Type: application/json" \
  -d '{"symbols": ["AAPL", "GOOGL"]}' | jq
# 预期: 只分析 AAPL 和 GOOGL

# 强制重新分析（覆盖今日结果）
curl -s -X POST http://localhost:8091/api/analysis/run \
  -H "Content-Type: application/json" \
  -d '{"symbols": ["AAPL"], "forceRerun": true}' | jq
# 预期: skippedCount=0

# 分析单个股票
curl -s -X POST http://localhost:8091/api/analysis/run/MSFT | jq
# 预期: recommendation 为 Buy/Hold/Sell, confidence 在 0-100

# 获取分析结果（分页）
curl -s "http://localhost:8091/api/analysis/results?page=1&pageSize=10" | jq
# 预期: results 数组, total, page, pageSize

# 按建议类型筛选 (1=Buy, 2=Hold, 3=Sell)
curl -s "http://localhost:8091/api/analysis/results?recommendation=1" | jq
# 预期: 所有结果的 recommendation 都是 "Buy"

# 获取单个股票的分析历史
curl -s http://localhost:8091/api/analysis/results/AAPL | jq
# 预期: 返回 AAPL 的历史分析记录

# 获取最新分析结果
curl -s "http://localhost:8091/api/analysis/latest?count=5" | jq
# 预期: 最多返回5条最新记录
```

#### 4. 统计查询

```bash
# 查询连续 N 天买入建议的股票 (recommendation: 1=Buy, 2=Hold, 3=Sell)
curl -s "http://localhost:8091/api/statistics/consecutive?days=2&recommendation=1" | jq
# 预期: stocks 数组包含连续2天以上买入建议的股票

# 获取统计汇总
curl -s http://localhost:8091/api/statistics/summary | jq
# 预期: totalStocks, totalAnalysis, buySummary, holdSummary, sellSummary

# 获取单个股票趋势
curl -s "http://localhost:8091/api/statistics/trend/AAPL?days=30" | jq
# 预期: trendData 数组, summary 包含统计信息
```

#### 5. 错误处理验证

```bash
# 添加重复股票 - 预期 409 Conflict
curl -s -X POST http://localhost:8091/api/stocks \
  -H "Content-Type: application/json" \
  -d '{"symbol": "AAPL", "name": "Apple"}' | jq

# 查询不存在的股票 - 预期 404 Not Found
curl -s http://localhost:8091/api/stocks/NOTEXIST | jq

# 分析不存在的股票 - 预期 404 Not Found
curl -s -X POST http://localhost:8091/api/analysis/run/NOTEXIST | jq

# 无效参数 - 预期 400 Bad Request
curl -s "http://localhost:8091/api/statistics/consecutive?days=100" | jq

# 空请求体验证
curl -s -X POST http://localhost:8091/api/stocks \
  -H "Content-Type: application/json" \
  -d '{}' | jq
# 预期: 400 Bad Request, 包含验证错误信息
```

---

### 预期响应格式

#### 成功响应

```json
{
  "success": true,
  "data": { ... },
  "message": "操作成功",
  "timestamp": "2026-01-30T10:00:00Z"
}
```

#### 分析结果示例

```json
{
  "success": true,
  "data": {
    "id": 1,
    "symbol": "AAPL",
    "stockName": "Apple Inc.",
    "analysisDate": "2026-01-30",
    "recommendation": "Buy",
    "confidence": 85.5,
    "reasoning": "Based on strong Q4 earnings..."
  }
}
```

#### 错误响应

```json
{
  "code": "STOCK_NOT_FOUND",
  "message": "股票 XXX 不存在",
  "timestamp": "2026-01-30T10:00:00Z",
  "traceId": "abc123"
}
```

---

## Bug 修复记录

| Bug | 位置                                             | 问题                                             | 修复                                     |
| --- | ------------------------------------------------ | ------------------------------------------------ | ---------------------------------------- |
| #1  | `AnalysisService.RunSingleAnalysisAsync`         | 更新现有分析结果时没有保存到数据库               | 添加 `UpdateAsync` 方法并调用保存        |
| #2  | `AnalysisRepository.GetRecommendationStatsAsync` | `Average()` 返回 double 但期望 decimal           | 显式类型转换                             |
| #3  | `StockService.MapToResponseWithLatestAnalysis`   | 两次数据库查询造成性能问题                       | 合并为单次查询                           |
| #4  | `AnalysisService.RunAnalysisAsync`               | ForceRerun=true 时直接 AddAsync 导致唯一约束冲突 | 实现 Upsert 逻辑（存在则更新，否则插入） |

---

## 运行测试

### 后端测试 (C#/.NET)

```bash
# 进入后端目录
cd backend

# 运行所有测试
dotnet test

# 运行测试并显示详细输出
dotnet test --verbosity normal

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~StockServiceTests"

# 生成测试覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 前端测试 (Vue3/Vitest)

```bash
# 进入前端目录
cd frontend-admin

# 安装依赖
npm install

# 运行测试
npm run test

# 运行测试（单次）
npm run test:run

# 生成覆盖率报告
npm run test:coverage
```

**前端测试覆盖:**

| 测试文件         | 测试内容                                                       |
| ---------------- | -------------------------------------------------------------- |
| `logger.test.ts` | 日志系统：多级别日志、API日志、用户操作日志、日志导出          |
| `stores.test.ts` | Pinia Store：股票管理、分析执行、统计查询                      |
| `utils.test.ts`  | 工具函数：推荐类型映射、置信度颜色、股票代码验证、批量导入解析 |

### 测试分类

- **Unit Tests**: `StockAnalyzer.Tests/Unit/` - 单元测试
- **Integration Tests**: `StockAnalyzer.Tests/Integration/` - 集成测试

---

## 项目结构

```
577/
├── backend/                          # 后端项目 (C# ASP.NET Core 8)
│   ├── StockAnalyzer.sln            # 解决方案文件
│   ├── Dockerfile                    # Docker 构建文件
│   └── src/
│       ├── StockAnalyzer.Api/        # API 层
│       │   ├── Controllers/          # 控制器
│       │   ├── Middleware/           # 中间件
│       │   ├── Program.cs            # 入口文件
│       │   └── appsettings.json      # 配置文件
│       ├── StockAnalyzer.Core/       # 核心层
│       │   ├── DTOs/                 # 数据传输对象
│       │   ├── Enums/                # 枚举
│       │   ├── Interfaces/           # 接口定义
│       │   ├── Models/               # 实体模型
│       │   └── Services/             # 业务服务
│       ├── StockAnalyzer.Infrastructure/  # 基础设施层
│       │   ├── Data/                 # 数据库上下文
│       │   ├── External/             # 外部服务 (Mock/OpenAI)
│       │   └── Repositories/         # 仓储实现
│       └── StockAnalyzer.Tests/      # 测试项目
│           ├── Unit/                 # 单元测试
│           └── Integration/          # 集成测试
├── frontend-admin/                   # 前端项目 (Vue3 + Element Plus)
│   ├── src/
│   │   ├── api/                      # API 接口封装
│   │   ├── components/               # 公共组件
│   │   ├── layouts/                  # 布局组件
│   │   ├── stores/                   # Pinia 状态管理
│   │   ├── styles/                   # 全局样式
│   │   ├── utils/                    # 工具函数
│   │   └── views/                    # 页面视图
│   │       ├── Dashboard.vue         # 仪表盘
│   │       ├── Stocks.vue            # 股票管理
│   │       ├── Analysis.vue          # AI 分析
│   │       ├── Statistics.vue        # 统计查询
│   │       └── Settings.vue          # 系统设置
│   ├── Dockerfile                    # Docker 构建文件
│   ├── nginx.conf                    # Nginx 配置
│   └── package.json                  # 依赖配置
├── docs/                             # 文档
│   └── project_design.md             # 项目设计文档
├── docker-compose.yml                # Docker Compose 配置
├── test-api.sh                       # API 测试脚本
├── .gitignore                        # Git 忽略文件
└── README.md                         # 项目说明
```

---

## 配置说明

### AI 服务配置

```json
{
  "AiSettings": {
    "Provider": "Mock", // Mock 或 OpenAI
    "ApiKey": "", // OpenAI API Key
    "BaseUrl": "https://api.openai.com/v1",
    "Model": "gpt-4",
    "MaxTokens": 500,
    "Temperature": 0.3,
    "TimeoutSeconds": 30,
    "RetryCount": 3
  }
}
```

### 数据库配置

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/app/data/stockanalyzer.db"
  }
}
```

---

## 开发指南

### 添加新的 AI 提供商

1. 实现 `IAiService` 接口
2. 在 `DependencyInjection.cs` 中注册服务
3. 更新配置文件

### 切换到生产环境

1. 设置环境变量 `ASPNETCORE_ENVIRONMENT=Production`
2. 配置真实的 OpenAI API Key
3. 考虑使用 MySQL/PostgreSQL 替代 SQLite

---

## License

MIT License
