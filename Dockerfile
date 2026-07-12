# ── 建置階段：編譯 .NET 專案 ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MyApi.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

# ── 執行階段：只帶編譯後的產物，映像檔更小 ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Render 會以 PORT 環境變數指定埠號
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

ENTRYPOINT ["dotnet", "MyApi.dll"]
