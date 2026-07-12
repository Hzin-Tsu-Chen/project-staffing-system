using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProjectController(AppDbContext db) => _db = db;

    /// <summary>查全部專案，並帶出各專案的派工人數與總工時（關聯彙總查詢）</summary>
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var projects = await _db.Projects
            .Select(p => new
            {
                p.Id, p.Name, p.Client, p.Status, p.Budget, p.StartDate, p.EndDate,
                StaffCount = p.Assignments.Count,                    // 派工人數
                TotalHours = p.Assignments.Sum(a => (int?)a.Hours) ?? 0,  // 總投入工時
            })
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        return Ok(projects);
    }

    /// <summary>查單一專案，並列出其所有派工人員（JOIN 查詢）</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult> GetOne(int id)
    {
        var project = await _db.Projects
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id, p.Name, p.Client, p.Status, p.Budget, p.StartDate, p.EndDate,
                Members = p.Assignments.Select(a => new
                {
                    a.Id,
                    a.StaffId,
                    StaffName = a.Staff!.Name,
                    StaffRole = a.Staff.Role,
                    a.RoleInProject,
                    a.Hours,
                }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<Project>> Create(Project p)
    {
        _db.Projects.Add(p);
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Project updated)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p is null) return NotFound();

        p.Name = updated.Name;
        p.Client = updated.Client;
        p.Status = updated.Status;
        p.Budget = updated.Budget;
        p.StartDate = updated.StartDate;
        p.EndDate = updated.EndDate;
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var p = await _db.Projects.FindAsync(id);
        if (p is null) return NotFound();
        _db.Projects.Remove(p);   // 關聯的派工紀錄會一併刪除（Cascade）
        await _db.SaveChangesAsync();
        return Ok();
    }
}
