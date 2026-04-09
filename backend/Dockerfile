# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src

# 复制项目文件并还原依赖
COPY src/StockAnalyzer.Core/StockAnalyzer.Core.csproj src/StockAnalyzer.Core/
COPY src/StockAnalyzer.Infrastructure/StockAnalyzer.Infrastructure.csproj src/StockAnalyzer.Infrastructure/
COPY src/StockAnalyzer.Api/StockAnalyzer.Api.csproj src/StockAnalyzer.Api/

RUN dotnet restore src/StockAnalyzer.Api/StockAnalyzer.Api.csproj -a $TARGETARCH

# 复制所有源代码
COPY src/ src/

# 编译发布
RUN dotnet publish src/StockAnalyzer.Api/StockAnalyzer.Api.csproj \
    -c Release \
    -a $TARGETARCH \
    -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 安装 curl 用于健康检查
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# 创建非 root 用户
RUN adduser --disabled-password --gecos '' appuser

# 创建数据目录
RUN mkdir -p /app/data /app/logs && chown -R appuser:appuser /app

# 复制发布文件
COPY --from=build /app/publish .

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 切换到非 root 用户
USER appuser

# 健康检查
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -sf http://localhost:8080/api/health || exit 1

EXPOSE 8080

ENTRYPOINT ["dotnet", "StockAnalyzer.Api.dll"]
