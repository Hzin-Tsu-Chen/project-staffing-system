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
            new() { Name = "陳志明", Role = "專案經理", Skill = "橋梁工程、專案管理" },
            new() { Name = "林佳蓉", Role = "工程師", Skill = "結構分析、耐震評估" },
            new() { Name = "王大維", Role = "工程師", Skill = "BIM、Revit" },
            new() { Name = "李思妤", Role = "工程師", Skill = "GIS、空間資料分析" },
            new() { Name = "張俊傑", Role = "繪圖員", Skill = "AutoCAD、圖資繪製" },
            new() { Name = "黃美玲", Role = "測量員", Skill = "測量、無人機空拍" },
        };
        db.Staffs.AddRange(staffs);
        db.SaveChanges();

        // 派工：建立專案與人員的多對多關聯（含角色與工時）
        db.Assignments.AddRange(
            new Assignment { ProjectId = projects[0].Id, StaffId = staffs[0].Id, RoleInProject = "專案負責人", Hours = 60 },
            new Assignment { ProjectId = projects[0].Id, StaffId = staffs[1].Id, RoleInProject = "結構分析", Hours = 120 },
            new Assignment { ProjectId = projects[0].Id, StaffId = staffs[4].Id, RoleInProject = "圖資繪製", Hours = 80 },
            new Assignment { ProjectId = projects[1].Id, StaffId = staffs[0].Id, RoleInProject = "專案督導", Hours = 40 },
            new Assignment { ProjectId = projects[1].Id, StaffId = staffs[2].Id, RoleInProject = "BIM 建模", Hours = 150 },
            new Assignment { ProjectId = projects[1].Id, StaffId = staffs[4].Id, RoleInProject = "施工圖繪製", Hours = 100 },
            new Assignment { ProjectId = projects[2].Id, StaffId = staffs[3].Id, RoleInProject = "GIS 分析", Hours = 90 },
            new Assignment { ProjectId = projects[2].Id, StaffId = staffs[5].Id, RoleInProject = "現地測量", Hours = 70 },
            new Assignment { ProjectId = projects[3].Id, StaffId = staffs[3].Id, RoleInProject = "系統建置", Hours = 60 }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();     // 讓 wwwroot/index.html 成為首頁
app.UseStaticFiles();      // 提供前端靜態檔案
app.UseCors();
app.UseAuthentication();   // 先驗證身分
app.UseAuthorization();    // 再檢查權限
app.MapControllers();
app.Run();
