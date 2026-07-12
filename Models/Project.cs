using System.Text.Json.Serialization;

namespace MyApi.Models;

/// <summary>工程專案</summary>
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";          // 專案名稱
    public string Client { get; set; } = "";        // 業主（如：交通部公路局）
    public string Status { get; set; } = "規劃中";   // 規劃中 / 進行中 / 已完成 / 暫停
    public int Budget { get; set; }                 // 預算（萬元）
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // 一對多：一個專案有多筆派工
    [JsonIgnore]
    public List<Assignment> Assignments { get; set; } = new();
}

/// <summary>人員</summary>
public class Staff
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "工程師";     // 工程師 / 專案經理 / 繪圖員 / 測量員
    public string Skill { get; set; } = "";         // 專長（如：結構、GIS、BIM）
    public bool IsActive { get; set; } = true;      // 是否在職

    [JsonIgnore]
    public List<Assignment> Assignments { get; set; } = new();
}

/// <summary>
/// 派工紀錄 —— 專案與人員的「多對多」關聯表。
/// 一個人可參與多個專案，一個專案可有多位人員，
/// 額外記錄該人在該專案的角色與投入工時（此即「關聯屬性」，需獨立成表）。
/// </summary>
public class Assignment
{
    public int Id { get; set; }

    public int ProjectId { get; set; }              // 外鍵 → Project
    public Project? Project { get; set; }

    public int StaffId { get; set; }                // 外鍵 → Staff
    public Staff? Staff { get; set; }

    public string RoleInProject { get; set; } = ""; // 在此專案擔任的角色
    public int Hours { get; set; }                  // 投入工時（小時/月）
}

// ── 前端傳入用的資料格式（避免直接暴露資料庫模型）──
public class AssignmentRequest
{
    public int ProjectId { get; set; }
    public int StaffId { get; set; }
    public string RoleInProject { get; set; } = "";
    public int Hours { get; set; }
}
