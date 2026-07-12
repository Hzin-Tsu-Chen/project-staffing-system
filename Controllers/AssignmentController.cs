using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;

namespace MyApi.Controllers;

/// <summary>派工：把「人員」指派到「專案」上（多對多關聯的建立）</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentController : ControllerBase
{
    private readonly AppDbContext _db;
    public AssignmentController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<ActionResult> Assign(AssignmentRequest req)
    {
        // 驗證關聯的兩端都存在（外鍵完整性）
        if (!await _db.Projects.AnyAsync(p => p.Id == req.ProjectId))
            return BadRequest(new { message = "專案不存在" });
        if (!await _db.Staffs.AnyAsync(s => s.Id == req.StaffId))
            return BadRequest(new { message = "人員不存在" });

        // 唯一約束：同一人不可重複派到同一專案
        if (await _db.Assignments.AnyAsync(a => a.ProjectId == req.ProjectId && a.StaffId == req.StaffId))
            return BadRequest(new { message = "此人員已在該專案中" });

        var assignment = new Assignment
        {
            ProjectId = req.ProjectId,
            StaffId = req.StaffId,
            RoleInProject = req.RoleInProject,
            Hours = req.Hours,
        };
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync();
        return Ok(new { message = "派工成功" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Remove(int id)
    {
        var a = await _db.Assignments.FindAsync(id);
        if (a is null) return NotFound();
        _db.Assignments.Remove(a);
        await _db.SaveChangesAsync();
        return Ok();
    }
}

/// <summary>儀表板：跨表彙總統計</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private const int MonthlyCapacity = 160;

    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var totalProjects = await _db.Projects.CountAsync();
        var activeProjects = await _db.Projects.CountAsync(p => p.Status == "進行中");
        var totalStaff = await _db.Staffs.CountAsync(s => s.IsActive);
        var totalBudget = await _db.Projects.SumAsync(p => (int?)p.Budget) ?? 0;

        // 人力超載警示：總工時超過標準工時的人員
        var overloaded = await _db.Staffs
            .Select(s => new { s.Name, Hours = s.Assignments.Sum(a => (int?)a.Hours) ?? 0 })
            .Where(s => s.Hours > MonthlyCapacity)
            .ToListAsync();

        // 各狀態的專案數
        var byStatus = await _db.Projects
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            totalProjects,
            activeProjects,
            totalStaff,
            totalBudget,
            overloadedCount = overloaded.Count,
            overloaded,
            byStatus,
        });
    }
}
