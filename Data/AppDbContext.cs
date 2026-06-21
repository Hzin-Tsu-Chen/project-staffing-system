using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data;

// DbContext = C# 程式跟資料庫之間的橋樑
// 每一個 DbSet 對應資料庫裡的一張表
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 這個 Employees 對應資料庫裡的 Employees 表
    public DbSet<Employee> Employees { get; set; }
}
