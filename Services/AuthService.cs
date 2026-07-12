using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MyApi.Models;

namespace MyApi.Services;

/// <summary>
/// 認證服務：負責密碼的「加鹽雜湊」與 JWT Token 的簽發。
/// </summary>
public class AuthService
{
    private readonly string _jwtKey;

    public AuthService(IConfiguration config)
    {
        // JWT 簽章金鑰從設定讀取（正式環境放環境變數，不寫死在程式碼）
        // 註：HMAC-SHA512 要求金鑰至少 512 bits（64 字元），太短會拋 IDX10720
        _jwtKey = config["Jwt:Key"]
                  ?? "dev-only-secret-key-please-change-in-production-at-least-64-characters-long!!";
    }

    /// <summary>
    /// 產生密碼雜湊。每個使用者用不同的隨機「鹽（salt）」，
    /// 因此即使兩人密碼相同，資料庫裡的雜湊值也不同 —— 可抵禦彩虹表攻擊。
    /// </summary>
    public static void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();
        salt = hmac.Key;                                              // 隨機鹽
        hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));    // 加鹽後的雜湊
    }

    /// <summary>驗證密碼：用同一把鹽重算雜湊，比對是否一致。</summary>
    public static bool VerifyPassword(string password, byte[] hash, byte[] salt)
    {
        using var hmac = new HMACSHA512(salt);
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computed.SequenceEqual(hash);
    }

    /// <summary>簽發 JWT Token。前端拿到後放在 Header，之後每次呼叫 API 都要帶。</summary>
    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),   // Token 8 小時後過期
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
