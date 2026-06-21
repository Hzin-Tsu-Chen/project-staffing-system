using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 註冊 DbContext，連到 Docker 裡的 SQL Server（DemoDb 資料庫）
var connectionString = "Server=localhost,1433;Database=DemoDb;User Id=sa;Password=MyPass@1234;TrustServerCertificate=True";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// 啟動時自動建立資料庫和表，若是空的就放入兩筆範例資料
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Employees.Any())
    {
        db.Employees.AddRange(
            new Employee { Name = "小明", Salary = 50000 },
            new Employee { Name = "小華", Salary = 48000 });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
