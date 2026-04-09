#!/bin/bash
# ============================================
# Stock AI Analyzer - 质检测试脚本
# 用法: ./test-api.sh [BASE_URL]
# 示例: ./test-api.sh http://localhost:8091
# ============================================

BASE_URL="${1:-http://localhost:8091}"

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

PASS_COUNT=0
FAIL_COUNT=0

# 检查响应是否成功
check_success() {
    local resp="$1"
    local test_name="$2"
    if echo "$resp" | grep -q '"success":true'; then
        echo -e "  ${GREEN}✓ PASS${NC}"
        ((PASS_COUNT++))
        return 0
    else
        echo -e "  ${RED}✗ FAIL${NC}"
        echo "  响应: $resp"
        ((FAIL_COUNT++))
        return 1
    fi
}

# 检查 HTTP 状态码
check_status() {
    local status="$1"
    local expected="$2"
    local test_name="$3"
    if [ "$status" -eq "$expected" ]; then
        echo -e "  ${GREEN}✓ PASS${NC} (HTTP $status)"
        ((PASS_COUNT++))
        return 0
    else
        echo -e "  ${RED}✗ FAIL${NC} (期望 HTTP $expected, 实际 HTTP $status)"
        ((FAIL_COUNT++))
        return 1
    fi
}

echo ""
echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     Stock AI Analyzer - API 质检测试                   ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "目标服务: ${YELLOW}$BASE_URL${NC}"
echo ""

# ============================================
# 第一部分: 健康检查
# ============================================
echo -e "${BLUE}━━━ 1. 健康检查 ━━━${NC}"

echo -n "[1.1] 基础健康检查..."
RESP=$(curl -s -w "\n%{http_code}" $BASE_URL/api/health)
STATUS=$(echo "$RESP" | tail -1)
BODY=$(echo "$RESP" | head -n -1)
if echo "$BODY" | grep -q '"status":"Healthy"'; then
    echo -e "  ${GREEN}✓ PASS${NC}"
    ((PASS_COUNT++))
else
    echo -e "  ${RED}✗ FAIL${NC} - 服务不可用"
    echo "请确保服务已启动: docker-compose up -d"
    exit 1
fi

echo -n "[1.2] 就绪检查（数据库连接）..."
RESP=$(curl -s $BASE_URL/api/health/ready)
if echo "$RESP" | grep -q '"status":"Ready"'; then
    echo -e "  ${GREEN}✓ PASS${NC}"
    ((PASS_COUNT++))
else
    echo -e "  ${RED}✗ FAIL${NC}"
    ((FAIL_COUNT++))
fi

echo ""

# ============================================
# 第二部分: 股票管理 CRUD
# ============================================
echo -e "${BLUE}━━━ 2. 股票管理 CRUD ━━━${NC}"

echo -n "[2.1] 添加单个股票 (POST /api/stocks)..."
RESP=$(curl -s -X POST $BASE_URL/api/stocks \
  -H "Content-Type: application/json" \
  -d '{"symbol": "AAPL", "name": "Apple Inc."}')
check_success "$RESP" "添加股票"

echo -n "[2.2] 批量添加股票 (POST /api/stocks/batch)..."
RESP=$(curl -s -X POST $BASE_URL/api/stocks/batch \
  -H "Content-Type: application/json" \
  -d '{"stocks": [
    {"symbol": "GOOGL", "name": "Alphabet Inc."},
    {"symbol": "MSFT", "name": "Microsoft Corporation"},
    {"symbol": "AMZN", "name": "Amazon.com Inc."},
    {"symbol": "TSLA", "name": "Tesla Inc."}
  ]}')
check_success "$RESP" "批量添加"

echo -n "[2.3] 获取所有股票 (GET /api/stocks)..."
RESP=$(curl -s $BASE_URL/api/stocks)
if check_success "$RESP" "获取列表"; then
    COUNT=$(echo "$RESP" | grep -o '"total":[0-9]*' | grep -o '[0-9]*')
    echo -e "       共 ${YELLOW}$COUNT${NC} 只股票"
fi

echo -n "[2.4] 获取单个股票 (GET /api/stocks/AAPL)..."
RESP=$(curl -s $BASE_URL/api/stocks/AAPL)
check_success "$RESP" "获取单个"

echo -n "[2.5] 更新股票 (PUT /api/stocks/AAPL)..."
RESP=$(curl -s -X PUT $BASE_URL/api/stocks/AAPL \
  -H "Content-Type: application/json" \
  -d '{"name": "Apple Inc. (Updated)"}')
check_success "$RESP" "更新股票"

echo ""

# ============================================
# 第三部分: AI 分析
# ============================================
echo -e "${BLUE}━━━ 3. AI 分析 ━━━${NC}"

echo -n "[3.1] 分析所有股票 (POST /api/analysis/run)..."
RESP=$(curl -s -X POST $BASE_URL/api/analysis/run \
  -H "Content-Type: application/json" \
  -d '{}')
if check_success "$RESP" "分析全部"; then
    SUCCESS=$(echo "$RESP" | grep -o '"successCount":[0-9]*' | grep -o '[0-9]*')
    DURATION=$(echo "$RESP" | grep -o '"durationMs":[0-9]*' | grep -o '[0-9]*')
    echo -e "       成功 ${YELLOW}$SUCCESS${NC} 只, 耗时 ${YELLOW}${DURATION}ms${NC}"
fi

echo -n "[3.2] 分析单个股票 (POST /api/analysis/run/MSFT)..."
RESP=$(curl -s -X POST $BASE_URL/api/analysis/run/MSFT)
if check_success "$RESP" "单股分析"; then
    REC=$(echo "$RESP" | grep -o '"recommendation":"[^"]*"' | head -1 | cut -d'"' -f4)
    CONF=$(echo "$RESP" | grep -o '"confidence":[0-9.]*' | head -1 | cut -d':' -f2)
    echo -e "       建议: ${YELLOW}$REC${NC}, 置信度: ${YELLOW}$CONF%${NC}"
fi

echo -n "[3.3] 强制重新分析 (forceRerun=true)..."
RESP=$(curl -s -X POST $BASE_URL/api/analysis/run \
  -H "Content-Type: application/json" \
  -d '{"symbols": ["AAPL"], "forceRerun": true}')
if check_success "$RESP" "强制分析"; then
    SKIPPED=$(echo "$RESP" | grep -o '"skippedCount":[0-9]*' | grep -o '[0-9]*')
    echo -e "       跳过数: ${YELLOW}$SKIPPED${NC} (应为 0)"
fi

echo -n "[3.4] 获取分析结果 (GET /api/analysis/results)..."
RESP=$(curl -s "$BASE_URL/api/analysis/results?page=1&pageSize=10")
if check_success "$RESP" "分析结果"; then
    TOTAL=$(echo "$RESP" | grep -o '"total":[0-9]*' | grep -o '[0-9]*')
    echo -e "       共 ${YELLOW}$TOTAL${NC} 条记录"
fi

echo -n "[3.5] 获取最新结果 (GET /api/analysis/latest)..."
RESP=$(curl -s "$BASE_URL/api/analysis/latest?count=5")
check_success "$RESP" "最新结果"

echo -n "[3.6] 获取单股历史 (GET /api/analysis/results/AAPL)..."
RESP=$(curl -s $BASE_URL/api/analysis/results/AAPL)
check_success "$RESP" "单股历史"

echo ""

# ============================================
# 第四部分: 统计查询
# ============================================
echo -e "${BLUE}━━━ 4. 统计查询 ━━━${NC}"

echo -n "[4.1] 获取统计汇总 (GET /api/statistics/summary)..."
RESP=$(curl -s $BASE_URL/api/statistics/summary)
if check_success "$RESP" "统计汇总"; then
    TOTAL_ANALYSIS=$(echo "$RESP" | grep -o '"totalAnalysis":[0-9]*' | grep -o '[0-9]*')
    echo -e "       总分析次数: ${YELLOW}$TOTAL_ANALYSIS${NC}"
fi

echo -n "[4.2] 查询连续买入 (days=2, recommendation=1)..."
RESP=$(curl -s "$BASE_URL/api/statistics/consecutive?days=2&recommendation=1")
if check_success "$RESP" "连续买入"; then
    FOUND=$(echo "$RESP" | grep -o '"totalFound":[0-9]*' | grep -o '[0-9]*')
    echo -e "       找到 ${YELLOW}$FOUND${NC} 只符合条件"
fi

echo -n "[4.3] 获取股票趋势 (GET /api/statistics/trend/AAPL)..."
RESP=$(curl -s "$BASE_URL/api/statistics/trend/AAPL?days=30")
check_success "$RESP" "股票趋势"

echo ""

# ============================================
# 第五部分: 错误处理
# ============================================
echo -e "${BLUE}━━━ 5. 错误处理验证 ━━━${NC}"

echo -n "[5.1] 添加重复股票 (期望 409 Conflict)..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE_URL/api/stocks \
  -H "Content-Type: application/json" \
  -d '{"symbol": "AAPL", "name": "Apple"}')
check_status "$STATUS" 409 "重复添加"

echo -n "[5.2] 查询不存在的股票 (期望 404)..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" $BASE_URL/api/stocks/NOTEXIST)
check_status "$STATUS" 404 "不存在股票"

echo -n "[5.3] 分析不存在的股票 (期望 404)..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE_URL/api/analysis/run/NOTEXIST)
check_status "$STATUS" 404 "分析不存在"

echo -n "[5.4] 无效参数 (days=100, 期望 400)..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/statistics/consecutive?days=100")
check_status "$STATUS" 400 "无效参数"

echo -n "[5.5] 空请求体验证 (期望 400)..."
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -X POST $BASE_URL/api/stocks \
  -H "Content-Type: application/json" \
  -d '{}')
check_status "$STATUS" 400 "空请求体"

echo ""

# ============================================
# 第六部分: 清理测试数据（可选）
# ============================================
echo -e "${BLUE}━━━ 6. 清理测试数据 ━━━${NC}"

echo -n "[6.1] 删除测试股票 TSLA..."
RESP=$(curl -s -X DELETE $BASE_URL/api/stocks/TSLA)
check_success "$RESP" "删除股票"

echo ""

# ============================================
# 测试结果汇总
# ============================================
echo -e "${BLUE}╔════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║                    测试结果汇总                        ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "  通过: ${GREEN}$PASS_COUNT${NC}"
echo -e "  失败: ${RED}$FAIL_COUNT${NC}"
echo -e "  总计: $((PASS_COUNT + FAIL_COUNT))"
echo ""

if [ $FAIL_COUNT -eq 0 ]; then
    echo -e "${GREEN}╔════════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║              ✓ 所有测试通过！质检合格！                ║${NC}"
    echo -e "${GREEN}╚════════════════════════════════════════════════════════╝${NC}"
    exit 0
else
    echo -e "${RED}╔════════════════════════════════════════════════════════╗${NC}"
    echo -e "${RED}║              ✗ 存在失败测试，请检查！                  ║${NC}"
    echo -e "${RED}╚════════════════════════════════════════════════════════╝${NC}"
    exit 1
fi
