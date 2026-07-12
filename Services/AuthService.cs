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
    private readonly SymmetricSecurityKey _signingKey;

    public AuthService(IConfiguration config)
    {
        _signingKey = BuildSigningKey(config);
    }

    /// <summary>
    /// 由設定值產生 JWT 簽章金鑰。
    ///
    /// HMAC-SHA512 要求金鑰至少 512 bits（64 bytes），但雲端平台自動產生的
    /// 環境變數長度不一定足夠（長度不足會拋 IDX10720）。
    /// 故一律以 SHA-512 將設定值雜湊為固定 64 bytes，任何長度的輸入皆可安全使用。
    /// </summary>
    public static SymmetricSecurityKey BuildSigningKey(IConfiguration config)
    {
        var secret = config["Jwt:Key"]
                     ?? "dev-only-secret-please-set-Jwt__Key-in-production";
        var keyBytes = SHA512.HashData(Encoding.UTF8.GetBytes(secret));   // 固定 64 bytes
        return new SymmetricSecurityKey(keyBytes);
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

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),   // Token 8 小時後過期
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
