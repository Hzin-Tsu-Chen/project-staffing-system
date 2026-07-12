# 工程專案派工管理系統

C# / ASP.NET Core 開發的前後端分離系統，模擬工程顧問公司的專案與人力調度需求。

> 🔐 **Demo 帳號**：`demo` / `demo1234`

---

## 【使用技術】

**後端**
- **C# / ASP.NET Core** — RESTful Web API
- **Entity Framework Core** — ORM，資料存取層
- **SQLite / SQL Server** — 資料庫（EF Core 可無縫切換 Provider）
- **JWT (JSON Web Token)** — 身分驗證
- **HMAC-SHA512 加鹽雜湊** — 密碼安全儲存
- **CORS** — 跨來源請求控管

**前端**
- **HTML / CSS / JavaScript** — 獨立前端，透過 REST API 與後端溝通
- **RWD 響應式設計** — 支援桌機與手機

---

## 【資料庫設計】

```
Project（專案）                     Staff（人員）
├ 專案名稱、業主、狀態               ├ 姓名、職稱、專長
├ 預算、起訖日期                     └ 是否在職
        │                                  │
        └────────┐            ┌────────────┘
                 ▼            ▼
            Assignment（派工）  ← 多對多關聯表
            ├ ProjectId（外鍵）
            ├ StaffId（外鍵）
            ├ RoleInProject（在此專案的角色）
            └ Hours（投入工時）
```

**設計重點**
- **多對多關聯**：一人可參與多專案、一專案可有多人。因需記錄「角色」「工時」等**關聯屬性**，故獨立為 `Assignment` 表（符合正規化）。
- **外鍵約束 + Cascade 刪除**：刪除專案時自動清除其派工紀錄，避免孤兒資料。
- **唯一索引**：`(ProjectId, StaffId)` 複合唯一鍵，防止同一人重複派至同一專案。

---

## 【核心功能】

| 模組 | 功能 |
|---|---|
| **身分驗證** | 註冊 / 登入 / JWT Token 簽發；密碼加鹽雜湊儲存，不存明碼 |
| **專案管理** | 專案 CRUD、狀態管理、點選查看成員明細（JOIN 查詢）|
| **人力配置** | 人員 CRUD、**負荷率計算**（總工時 ÷ 標準工時 160h）、超載視覺化 |
| **派工** | 指派人員至專案、設定角色與工時；含外鍵完整性與重複派工驗證 |
| **儀表板** | 跨表彙總：專案總數、進行中數、總預算、**人力超載警示** |

### API 端點

| 方法 | 路徑 | 說明 | 需驗證 |
|---|---|---|---|
| POST | `/api/auth/register` | 註冊 | — |
| POST | `/api/auth/login` | 登入，回傳 JWT | — |
| GET | `/api/dashboard` | 儀表板統計 | ✓ |
| GET/POST/PUT/DELETE | `/api/project` | 專案 CRUD | ✓ |
| GET | `/api/project/{id}` | 專案 + 成員明細（JOIN）| ✓ |
| GET/POST/PUT/DELETE | `/api/staff` | 人員 CRUD + 負荷率 | ✓ |
| POST/DELETE | `/api/assignment` | 派工 / 取消派工 | ✓ |

---

## 【成果說明】

- **資安實作**：密碼以 HMAC-SHA512 + 隨機鹽雜湊儲存；API 端點以 `[Authorize]` 保護，未帶有效 JWT 一律回 401。登入失敗時不透露「帳號是否存在」，避免帳號列舉攻擊。
- **業務邏輯**：自動計算人員負荷率並標示超載（>160 小時/月），提供人力調度決策依據。
- **關聯查詢**：以 LINQ 進行跨表彙總（各專案人力/工時、各人員專案數/負荷），對應 SQL 的 JOIN 與 GROUP BY。
- **可攜性**：資料庫以 EF Core 抽象，本地可用 SQL Server、雲端部署改 SQLite，僅需更換連線字串。

### 資安設計說明

JWT 簽章金鑰與資料庫連線字串皆從**設定/環境變數**讀取，不寫死於程式碼，因此不會隨原始碼推上版控。

---

## 如何執行

```bash
dotnet run --launch-profile http
```

瀏覽器開啟 `http://localhost:5020`，以 `demo` / `demo1234` 登入。

（首次啟動會自動建立資料庫、表結構，並載入模擬的專案與人力種子資料。）

## 檔案結構

| 路徑 | 說明 |
|---|---|
| `Models/` | 資料模型（Project / Staff / Assignment / User）|
| `Data/AppDbContext.cs` | EF Core 資料庫上下文、關聯與約束設定 |
| `Services/AuthService.cs` | 密碼雜湊、JWT 簽發 |
| `Controllers/` | API 端點（Auth / Project / Staff / Assignment / Dashboard）|
| `wwwroot/index.html` | 前端單頁應用（RWD）|
