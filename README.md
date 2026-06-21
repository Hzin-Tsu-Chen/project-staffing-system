# 員工管理 Web API（C# / .NET + SQL Server）

用 ASP.NET Core 開發的 RESTful Web API，透過 Entity Framework Core 連接 SQL Server，提供員工資料的完整 CRUD 功能。

## 技術棧

- **C# / ASP.NET Core** — Web API 框架
- **Entity Framework Core** — ORM，用 C# 操作資料庫
- **SQL Server** — 資料庫（Docker 部署）

## 功能（完整 CRUD）

| 方法 | 路徑 | 功能 |
|---|---|---|
| GET | `/api/employee` | 查詢全部員工 |
| GET | `/api/employee/{id}` | 查詢單一員工 |
| POST | `/api/employee` | 新增員工 |
| PUT | `/api/employee/{id}` | 修改員工 |
| DELETE | `/api/employee/{id}` | 刪除員工 |

## 架構

```
瀏覽器 / 前端
    │  HTTP 請求（GET/POST/PUT/DELETE）
    ▼
EmployeeController（接收請求、回傳 JSON）
    │
    ▼
AppDbContext（Entity Framework，C# 與資料庫的橋樑）
    │
    ▼
SQL Server（DemoDb 資料庫，資料永久保存）
```

## 重點

- 資料存在 **SQL Server**，重啟服務後資料仍在（非記憶體暫存）
- 採用 **非同步**（async/await）寫法，符合實務
- 啟動時自動建立資料庫與表（`EnsureCreated`）並放入種子資料

## 如何執行

1. 啟動 SQL Server（Docker）：
   ```bash
   docker run -d --name sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=MyPass@1234" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
   ```
2. 執行 API：
   ```bash
   dotnet run --launch-profile http
   ```
3. 瀏覽器開 `http://localhost:5020/api/employee` 查看結果

## 檔案說明

| 檔案 | 說明 |
|---|---|
| `Models/Employee.cs` | 員工資料模型 |
| `Data/AppDbContext.cs` | EF Core 資料庫橋樑 |
| `Controllers/EmployeeController.cs` | API 端點（CRUD）|
| `Program.cs` | 程式進入點、資料庫連線設定 |
