namespace MyApi.Models;

// 使用者帳號。密碼不存明碼，只存「雜湊值 + 鹽」
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";

    // 密碼加鹽雜湊：即使資料庫外洩，也無法還原出原始密碼
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
}

// 註冊/登入時前端傳進來的資料
public class AuthRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
