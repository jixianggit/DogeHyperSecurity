using System;
using System.IO;
using System.Text.Json;

namespace DogeHyperSecurity.Services;

internal sealed class LoginSettings
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DogeHyperSecurity");

    private static readonly string FilePath = Path.Combine(FolderPath, "login.json");

    public bool AutoLoginAccount { get; set; }

    public bool RememberAccount { get; set; }

    public string Account { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool AutoLoginCard { get; set; }

    public bool RememberCard { get; set; }

    public string CardKey { get; set; } = string.Empty;

    public string LoginMode { get; set; } = "card";

    public static LoginSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new LoginSettings();
            }

            return JsonSerializer.Deserialize<LoginSettings>(File.ReadAllText(FilePath))
                   ?? new LoginSettings();
        }
        catch
        {
            return new LoginSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(FolderPath);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch
        {
            // 保存失败不影响登录流程
        }
    }
}
