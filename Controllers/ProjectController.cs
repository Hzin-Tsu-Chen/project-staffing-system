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

    /// <summary>
    /// 依專案起訖日與今日，計算時程進度百分比（供甘特圖使用）。
    /// 已完成專案一律視為 100%；尚未開始為 0%。
    /// </summary>
    private static int TimeProgress(DateTime start, DateTime end, string status)
    {
        if (status == "已完成") return 100;

        var today = DateTime.UtcNow.Date;
        if (today <= start.Date) return 0;
        if (today >= end.Date) return 100;

        var total = (end.Date - start.Date).TotalDays;
        if (total <= 0) return 100;
        return (int)Math.Round((today - start.Date).TotalDays / total * 100);
    }

    /// <summary>查全部專案，並帶出各專案的派工人數與總工時（關聯彙總查詢）</summary>
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var projects = await _db.Projects
            .Select(p => new
            {
                p.Id, p.Name, p.Client, p.Status, p.Budget, p.StartDate, p.EndDate,
                StaffCount = p.Assignments.Count,                          // 派工人數
                TotalHours = p.Assignments.Sum(a => (int?)a.Hours) ?? 0,   // 總投入工時
            })
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        // 進度需依當前日期計算，故在記憶體端處理
        var result = projects.Select(p => new
        {
            p.Id, p.Name, p.Client, p.Status, p.Budget, p.StartDate, p.EndDate,
            p.StaffCount, p.TotalHours,
            Progress = TimeProgress(p.StartDate, p.EndDate, p.Status),
            DurationDays = (int)(p.EndDate.Date - p.StartDate.Date).TotalDays,
        });

        return Ok(result);
    }

    /// <summary>查單一專案，列出所有派工人員與其聯絡方式（JOIN 查詢）</summary>
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
                    StaffSkill = a.Staff.Skill,
                    StaffEmail = a.Staff.Email,      // 聯絡方式
                    StaffPhone = a.Staff.Phone,
                    a.RoleInProject,                 // 在此專案負責的工作項目
                    a.Hours,
                }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (project is null) return NotFound();

        return Ok(new
        {
            project.Id, project.Name, project.Client, project.Status, project.Budget,
            project.StartDate, project.EndDate, project.Members,
            Progress = TimeProgress(project.StartDate, project.EndDate, project.Status),
            TotalHours = project.Members.Sum(m => m.Hours),
        });
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
