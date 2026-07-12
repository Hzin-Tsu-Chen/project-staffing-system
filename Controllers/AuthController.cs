using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.Data;
using MyApi.Models;
using MyApi.Services;

namespace MyApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public AuthController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    /// <summary>註冊：POST /api/auth/register</summary>
    [HttpPost("register")]
    public async Task<ActionResult> Register(AuthRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "此帳號已被註冊" });

        AuthService.CreatePasswordHash(request.Password, out var hash, out var salt);

        var user = new User
        {
            Username = request.Username,
            PasswordHash = hash,
            PasswordSalt = salt,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "註冊成功" });
    }

    /// <summary>登入：POST /api/auth/login → 成功時回傳 JWT Token</summary>
    [HttpPost("login")]
    public async Task<ActionResult> Login(AuthRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

        // 帳號不存在或密碼錯誤，都回傳同樣訊息 —— 避免洩漏「帳號是否存在」
        if (user is null || !AuthService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new { message = "帳號或密碼錯誤" });

        return Ok(new
        {
            token = _auth.CreateToken(user),
            username = user.Username,
        });
    }
}
