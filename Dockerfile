# ── 建置階段：編譯 .NET 專案 ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MyApi.csproj .
RUN dotnet restore -r linux-x64

COPY . .

# ReadyToRun：把 IL 預先編譯成原生機器碼，容器啟動時就不必即時 JIT。
# 這是縮短「冷啟動」的關鍵 —— Render 免費方案休眠後重新開機時，
# 使用者是實實在在在等這段時間。
RUN dotnet publish -c Release \
    -r linux-x64 --self-contained false \
    -p:PublishReadyToRun=true \
    -p:TieredPGO=true \
    -o /app

# ── 執行階段：只帶編譯後的產物，映像檔更小、拉取更快 ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# 啟動最佳化
ENV DOTNET_TieredPGO=1 \
    DOTNET_TC_QuickJitForLoops=1 \
    DOTNET_ReadyToRun=1 \
    ASPNETCORE_URLS=http://+:${PORT:-8080}

EXPOSE 8080

ENTRYPOINT ["dotnet", "MyApi.dll"]
