#!/bin/bash
# Stock AI Analyzer 测试运行脚本

set -e

echo "========================================"
echo "Stock AI Analyzer - 测试运行器"
echo "========================================"

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 检查 dotnet 是否安装
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}错误: dotnet 未安装${NC}"
    echo "请安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo -e "${GREEN}dotnet 版本:${NC}"
dotnet --version
echo ""

# 还原依赖
echo -e "${YELLOW}正在还原 NuGet 包...${NC}"
dotnet restore
echo ""

# 构建项目
echo -e "${YELLOW}正在构建项目...${NC}"
dotnet build --no-restore
echo ""

# 运行测试
echo -e "${YELLOW}正在运行测试...${NC}"
echo ""

# 单元测试
echo "----------------------------------------"
echo -e "${GREEN}运行单元测试${NC}"
echo "----------------------------------------"
dotnet test src/StockAnalyzer.Tests/StockAnalyzer.Tests.csproj \
    --filter "FullyQualifiedName~Unit" \
    --no-build \
    --verbosity normal

# 集成测试
echo ""
echo "----------------------------------------"
echo -e "${GREEN}运行集成测试${NC}"
echo "----------------------------------------"
dotnet test src/StockAnalyzer.Tests/StockAnalyzer.Tests.csproj \
    --filter "FullyQualifiedName~Integration" \
    --no-build \
    --verbosity normal

echo ""
echo "========================================"
echo -e "${GREEN}所有测试完成!${NC}"
echo "========================================"
