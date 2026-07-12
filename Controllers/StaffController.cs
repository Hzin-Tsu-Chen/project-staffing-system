using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StaffController : ControllerBase
{
    private const int MonthlyCapacity = 160;   // 每人每月標準工時（超過視為超載）

    private readonly AppDbContext _db;
    public StaffController(AppDbContext db) => _db = db;

    /// <summary>
    /// 查全部人員，並帶出各人的專案數、總工時與「負荷率」。
    /// 負荷率 = 總工時 / 每月標準工時，超過 100% 代表人力超載。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var staff = await _db.Staffs
            .Select(s => new
            {
                s.Id, s.Name, s.Role, s.Skill, s.Email, s.Phone, s.IsActive,
                ProjectCount = s.Assignments.Count,
                TotalHours = s.Assignments.Sum(a => (int?)a.Hours) ?? 0,
            })
            .ToListAsync();

        // 負荷率在記憶體計算（避免資料庫端浮點運算差異）
        var result = staff.Select(s => new
        {
            s.Id, s.Name, s.Role, s.Skill, s.Email, s.Phone, s.IsActive, s.ProjectCount, s.TotalHours,
            LoadPercent = (int)Math.Round(s.TotalHours * 100.0 / MonthlyCapacity),
            IsOverloaded = s.TotalHours > MonthlyCapacity,
        }).OrderByDescending(s => s.TotalHours);

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<Staff>> Create(Staff s)
    {
        _db.Staffs.Add(s);
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Staff updated)
    {
        var s = await _db.Staffs.FindAsync(id);
        if (s is null) return NotFound();

        s.Name = updated.Name;
        s.Role = updated.Role;
        s.Skill = updated.Skill;
        s.Email = updated.Email;
        s.Phone = updated.Phone;
        s.IsActive = updated.IsActive;
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var s = await _db.Staffs.FindAsync(id);
        if (s is null) return NotFound();
        _db.Staffs.Remove(s);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
