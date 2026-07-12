using Microsoft.EntityFrameworkCore;
using MyApi.Models;

namespace MyApi.Data;

/// <summary>
/// 資料庫橋樑（Entity Framework Core）。
/// 資料表設計：Project（專案）、Staff（人員）為主體，
/// Assignment（派工）為兩者的多對多關聯表，並帶有關聯屬性（角色、工時）。
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // 外鍵關聯：刪除專案時，一併刪除其派工紀錄（避免孤兒資料）
        b.Entity<Assignment>()
            .HasOne(a => a.Project)
            .WithMany(p => p.Assignments)
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<Assignment>()
            .HasOne(a => a.Staff)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        // 同一人在同一專案不可重複派工（唯一約束）
        b.Entity<Assignment>()
            .HasIndex(a => new { a.ProjectId, a.StaffId })
            .IsUnique();
    }
}
