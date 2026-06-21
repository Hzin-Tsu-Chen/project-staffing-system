using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly AppDbContext _db;

    // 透過建構子拿到資料庫橋樑（DbContext）
    public EmployeeController(AppDbContext db)
    {
        _db = db;
    }

    // 查全部：GET /api/employee
    [HttpGet]
    public async Task<List<Employee>> GetAll()
    {
        return await _db.Employees.ToListAsync();
    }

    // 查一個：GET /api/employee/1
    [HttpGet("{id}")]
    public async Task<ActionResult<Employee>> GetOne(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null) return NotFound();
        return employee;
    }

    // 新增：POST /api/employee
    [HttpPost]
    public async Task<Employee> Add(Employee newEmployee)
    {
        _db.Employees.Add(newEmployee);
        await _db.SaveChangesAsync();   // 寫進資料庫，永久保存
        return newEmployee;
    }

    // 修改：PUT /api/employee/1
    [HttpPut("{id}")]
    public async Task<ActionResult<Employee>> Update(int id, Employee updated)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null) return NotFound();
        employee.Name = updated.Name;
        employee.Salary = updated.Salary;
        await _db.SaveChangesAsync();
        return employee;
    }

    // 刪除：DELETE /api/employee/1
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var employee = await _db.Employees.FindAsync(id);
        if (employee == null) return NotFound();
        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
