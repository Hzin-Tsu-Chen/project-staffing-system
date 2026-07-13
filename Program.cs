using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyApi.Data;
using MyApi.Models;
using MyApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 雲端平台（如 Render）以 PORT 環境變數指定埠號；本地開發則沿用預設值
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<AuthService>();

// ── 資料庫 ──
// 雲端部署用 SQLite（檔案型，免另外架資料庫伺服器）
// 本地開發可改用 SQL Server —— Entity Framework 讓兩者可無縫切換
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=app.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// ── JWT 驗證 ──
// 簽發（AuthService）與驗證（此處）必須使用同一把金鑰，故共用 BuildSigningKey
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = AuthService.BuildSigningKey(builder.Configuration),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

// ── CORS：允許前端網頁呼叫這個 API（前後端分離必要設定）──
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// 啟動時自動建立資料庫與表，並放入種子資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // 預設 demo 帳號（方便直接登入試用）
    if (!db.Users.Any())
    {
        AuthService.CreatePasswordHash("demo1234", out var hash, out var salt);
        db.Users.Add(new User { Username = "demo", PasswordHash = hash, PasswordSalt = salt });
        db.SaveChanges();
    }

    // 種子資料：模擬工程顧問公司的專案與人力
    if (!db.Projects.Any())
    {
        var projects = new List<Project>
        {
            new() { Name = "台61線橋梁耐震評估", Client = "交通部公路局", Status = "進行中",
                    Budget = 4800, StartDate = new(2026, 1, 15), EndDate = new(2026, 12, 31) },
            new() { Name = "桃園捷運綠線BIM建模", Client = "桃園市政府捷運工程局", Status = "進行中",
                    Budget = 12000, StartDate = new(2025, 9, 1), EndDate = new(2027, 3, 31) },
            new() { Name = "彰化縣淹水潛勢圖資更新", Client = "經濟部水利署", Status = "規劃中",
                    Budget = 2600, StartDate = new(2026, 8, 1), EndDate = new(2027, 1, 31) },
            new() { Name = "台中港區地理資訊系統建置", Client = "台灣港務公司", Status = "已完成",
                    Budget = 3500, StartDate = new(2025, 3, 1), EndDate = new(2026, 2, 28) },
        };
        db.Projects.AddRange(projects);

        var staffs = new List<Staff>
        {
            new() { Name = "陳志明", Role = "專案經理", Skill = "橋梁工程、專案管理",
                    Email = "cm.chen@example-eng.com.tw", Phone = "04-2358-1001" },
            new() { Name = "林佳蓉", Role = "工程師", Skill = "結構分析、耐震評估",
                    Email = "jr.lin@example-eng.com.tw", Phone = "04-2358-1002" },
            new() { Name = "王大維", Role = "工程師", Skill = "BIM、Revit",
                    Email = "dw.wang@example-eng.com.tw", Phone = "04-2358-1003" },
            new() { Name = "李思妤", Role = "工程師", Skill = "GIS、空間資料分析",
                    Email = "sy.lee@example-eng.com.tw", Phone = "04-2358-1004" },
            new() { Name = "張俊傑", Role = "繪圖員", Skill = "AutoCAD、圖資繪製",
                    Email = "cc.chang@example-eng.com.tw", Phone = "04-2358-1005" },
            new() { Name = "黃美玲", Role = "測量員", Skill = "測量、無人機空拍",
                    Email = "ml.huang@example-eng.com.tw", Phone = "04-2358-1006" },
        };
        db.Staffs.AddRange(staffs);
        db.SaveChanges();

        // 派工：建立專案與人員的多對多關聯（含負責工作項目與工時）
        db.Assignments.AddRange(
            // 台61線橋梁耐震評估
            new Assignment { ProjectId = projects[0].Id, StaffId = staffs[0].Id, RoleInProject = "專案負責人／業主界面協調", Hours = 60 },
            new Assignment { ProjectId = projects[0].Id, StaffId = staffs[1].Id, RoleInProject = "橋梁結構耐震分析、SAP2000 建模", Hours = 120 },
            new Assignment { ProjectId = projects[0].Id, StaffId = staffs[4].Id, RoleInProject = "補強設計圖繪製（AutoCAD）", Hours = 80 },
            new Assignment { ProjectId = projects[0].Id, StaffId = staffs[5].Id, RoleInProject = "橋梁現地檢測與空拍建檔", Hours = 50 },

            // 桃園捷運綠線BIM建模
            new Assignment { ProjectId = projects[1].Id, StaffId = staffs[0].Id, RoleInProject = "專案督導／進度與預算控管", Hours = 40 },
            new Assignment { ProjectId = projects[1].Id, StaffId = staffs[2].Id, RoleInProject = "車站 BIM 建模（Revit）、碰撞檢核", Hours = 150 },
            new Assignment { ProjectId = projects[1].Id, StaffId = staffs[4].Id, RoleInProject = "施工圖出圖與圖說套繪", Hours = 100 },
            new Assignment { ProjectId = projects[1].Id, StaffId = staffs[3].Id, RoleInProject = "路線周邊地形圖資整合", Hours = 45 },

            // 彰化縣淹水潛勢圖資更新
            new Assignment { ProjectId = projects[2].Id, StaffId = staffs[3].Id, RoleInProject = "淹水潛勢空間分析（QGIS）", Hours = 90 },
            new Assignment { ProjectId = projects[2].Id, StaffId = staffs[5].Id, RoleInProject = "現地測量與地形點位補測", Hours = 70 },
            new Assignment { ProjectId = projects[2].Id, StaffId = staffs[1].Id, RoleInProject = "水理模式參數校正", Hours = 55 },

            // 台中港區地理資訊系統建置
            new Assignment { ProjectId = projects[3].Id, StaffId = staffs[3].Id, RoleInProject = "GIS 系統建置與圖臺開發", Hours = 60 },
            new Assignment { ProjectId = projects[3].Id, StaffId = staffs[2].Id, RoleInProject = "港區設施 3D 模型整合", Hours = 65 },
            new Assignment { ProjectId = projects[3].Id, StaffId = staffs[0].Id, RoleInProject = "專案結案與成果驗收", Hours = 30 }
        );

        // 工作階段：甘特圖的分段依據。
        // 階段劃分依「機關委託技術服務廠商評選及計費辦法」第 4~9 條的技術服務類型
        // （可行性研究／規劃／設計／監造／專案管理），再依各案性質細分。
        // 每個階段都附一句白話說明 —— 甘特圖是要給業主、主管、非工程背景的人看的，
        // 只寫「細部設計」四個字，看的人並不知道那到底在做什麼。
        void Phase(int pid, int seq, string name, string note, DateTime s, DateTime e) =>
            db.ProjectPhases.Add(new ProjectPhase
            { ProjectId = pid, Seq = seq, Name = name, Note = note, StartDate = s, EndDate = e });

        // ① 台61線橋梁耐震評估（2026/1/15 ~ 2026/12/31）—— 規劃 + 設計
        Phase(projects[0].Id, 1, "開案與資料蒐集",
              "向公路局調閱這座橋當年的設計圖與歷年檢測報告，先弄清楚它是怎麼蓋的、過去修過哪些地方。",
              new(2026, 1, 15), new(2026, 3, 15));
        Phase(projects[0].Id, 2, "現地調查與檢測",
              "派人到橋上實地量測、拍照、鑽取混凝土樣本，確認這座橋「現在」的真實狀況，而不是只看幾十年前的圖紙。",
              new(2026, 3, 16), new(2026, 5, 31));
        Phase(projects[0].Id, 3, "耐震能力詳細評估",
              "用電腦模擬大地震來襲，算出這座橋會不會倒、哪個橋墩會先撐不住。這是整個案子的核心。",
              new(2026, 6, 1), new(2026, 8, 31));
        Phase(projects[0].Id, 4, "補強方案基本設計",
              "針對算出來會出問題的地方，提出幾種補強做法（例如加鋼板、擴大橋墩），連同大概費用一起給業主挑。",
              new(2026, 9, 1), new(2026, 10, 31));
        Phase(projects[0].Id, 5, "細部設計與成本估算",
              "把業主選定的做法畫成施工廠商看得懂、可以直接發包動工的圖，並精算需要多少錢、多少材料。",
              new(2026, 11, 1), new(2026, 12, 15));
        Phase(projects[0].Id, 6, "期末審查與結案",
              "向業主簡報成果，依審查意見修正報告後結案交件。",
              new(2026, 12, 16), new(2026, 12, 31));

        // ② 桃園捷運綠線BIM建模（2025/9/1 ~ 2027/3/31）
        Phase(projects[1].Id, 1, "開案與需求訪談",
              "跟捷運局坐下來談清楚：這套模型將來要拿來做什麼？要細到看得見一根螺絲，還是看得到牆和樑就好？",
              new(2025, 9, 1), new(2025, 11, 30));
        Phase(projects[1].Id, 2, "BIM執行計畫書研擬",
              "先把規矩訂好——檔案怎麼命名、模型要多精細、十幾個人怎麼同時改同一份模型不會打架。沒訂好，做到一半就會各做各的。",
              new(2025, 12, 1), new(2026, 2, 28));
        Phase(projects[1].Id, 3, "既有圖說數化與建模",
              "把原本平面的 2D 設計圖，一張一張建成可以旋轉、可以走進去看的 3D 立體模型。這是最花人力的一段。",
              new(2026, 3, 1), new(2026, 9, 30));
        Phase(projects[1].Id, 4, "模型整合與碰撞檢核",
              "把結構、水電、空調的模型疊在一起，讓電腦自動抓出「水管剛好穿過大樑」這類衝突——在動工前發現，比在工地打掉重做便宜太多。",
              new(2026, 10, 1), new(2027, 1, 15));
        Phase(projects[1].Id, 5, "施工圖說產出",
              "從 3D 模型自動產生工地師傅實際要照著施工的平面圖。",
              new(2027, 1, 16), new(2027, 2, 28));
        Phase(projects[1].Id, 6, "成果交付與教育訓練",
              "把模型交給捷運局，並教他們的人怎麼開啟、查詢、後續自行維護。",
              new(2027, 3, 1), new(2027, 3, 31));

        // ③ 彰化縣淹水潛勢圖資更新（2026/8/1 ~ 2027/1/31）
        Phase(projects[2].Id, 1, "開案與資料蒐集",
              "蒐集彰化的地形高低、河川、排水溝與過去幾次颱風的實際淹水紀錄。",
              new(2026, 8, 1), new(2026, 9, 15));
        Phase(projects[2].Id, 2, "地形測量與補測",
              "舊的地形資料可能過時（例如新蓋了道路、堤防），派測量員到現場補測地勢高低。",
              new(2026, 9, 16), new(2026, 10, 31));
        Phase(projects[2].Id, 3, "水理模式建置與校正",
              "建立一套模擬雨水怎麼流的電腦模型，再用「過去真的淹過的那幾次」去驗證：模型算出來的淹水範圍，跟當年實際淹的一不一樣？不一樣就調參數，直到算得準。",
              new(2026, 11, 1), new(2026, 12, 15));
        Phase(projects[2].Id, 4, "淹水潛勢模擬分析",
              "用校正好的模型去問：如果下 24 小時 500 毫米的雨，彰化哪裡會淹？會淹多深？",
              new(2026, 12, 16), new(2027, 1, 15));
        Phase(projects[2].Id, 5, "圖資製作與成果審查",
              "把結果畫成一張防災地圖（讓民眾與消防隊知道哪裡要先撤離），送水利署審查。",
              new(2027, 1, 16), new(2027, 1, 31));

        // ④ 台中港區地理資訊系統建置（2025/3/1 ~ 2026/2/28，已完成）
        Phase(projects[3].Id, 1, "需求訪談與系統規劃",
              "了解港務公司想用這套系統管什麼：碼頭、倉庫、地下管線、還是船席調度？",
              new(2025, 3, 1), new(2025, 5, 31));
        Phase(projects[3].Id, 2, "圖資整理與資料庫建置",
              "把散在各部門、各種格式的圖檔與設施清冊，整理進一個統一的空間資料庫，讓每樣東西都有座標、查得到。",
              new(2025, 6, 1), new(2025, 9, 30));
        Phase(projects[3].Id, 3, "圖臺系統開發",
              "開發一個網站，讓港務人員用瀏覽器就能看地圖、點一下設施就查得到它的資料。",
              new(2025, 10, 1), new(2025, 12, 31));
        Phase(projects[3].Id, 4, "系統測試與上線",
              "找實際使用者試用、修掉錯誤，然後正式啟用。",
              new(2026, 1, 1), new(2026, 1, 31));
        Phase(projects[3].Id, 5, "教育訓練與驗收結案",
              "教港務公司同仁操作，業主驗收無誤後結案。",
              new(2026, 2, 1), new(2026, 2, 28));

        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 健康檢查（免驗證）。
// Render 免費方案在閒置 15 分鐘後會讓服務休眠，下次造訪需等 30~60 秒冷啟動；
// 由外部監控服務（UptimeRobot）每 10 分鐘打這個端點，服務就不會睡著，
// 面試官點連結進來即為秒開。
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    time = DateTime.UtcNow,
}));

app.UseDefaultFiles();     // 讓 wwwroot/index.html 成為首頁
app.UseStaticFiles();      // 提供前端靜態檔案
app.UseCors();
app.UseAuthentication();   // 先驗證身分
app.UseAuthorization();    // 再檢查權限
app.MapControllers();
app.Run();
