using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace LolKey;

public enum InputMethod
{
    Telex = 0,
    VNI = 1,
    SimpleTelex = 2,
    VIQR = 3
}

public enum Charset
{
    Unicode = 0,         // Unicode dựng sẵn (Chuẩn quốc tế)
    UnicodeComposite = 1,// Unicode tổ hợp
    TCVN3 = 2,           // TCVN3 (ABC)
    VNIWindows = 3       // VNI Windows
}

public enum SwitchKeyMode
{
    CtrlShift = 0,       // Ctrl + Shift
    AltZ = 1             // Alt + Z
}

public class AppConfig
{
    // Vietnamese Engine Settings
    public bool EnableVietnamese { get; set; } = true;
    public InputMethod Method { get; set; } = InputMethod.Telex;
    public Charset EncodingCharset { get; set; } = Charset.Unicode;
    public SwitchKeyMode SwitchKey { get; set; } = SwitchKeyMode.CtrlShift;
    public bool SpellCheck { get; set; } = true;         // Tự động kiểm tra chính tả & khôi phục từ tiếng Anh (pass, fast)
    public bool ModernTonePlacement { get; set; } = true;// Đặt dấu chuẩn mới (hòa, thúy) vs Chuẩn cũ (hoà, thuỷ)
    public bool FreeMarking { get; set; } = true;        // Cho phép bỏ dấu tự do ở cuối từ (toans -> toán)

    // Anti-Toxic Engine Settings
    public bool IsEnabled { get; set; } = true;
    public int Mode { get; set; } = 0;                   // 0: Wholesome, 1: Censor, 2: Philosophy
    public int HotkeyIndex { get; set; } = 0;            // 0: Ctrl+Shift+Z, 1: F11, 2: Alt+Z
    public bool SoundEnabled { get; set; } = true;
    public int RescuedCount { get; set; } = 0;

    // System Settings
    public bool StartWithWindows { get; set; } = false;
    public bool ShowWindowOnStartup { get; set; } = true;
}

public static class ConfigManager
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppRegistryName = "AntiAnToxicAUT";
    private const string OldAppRegistryName = "LOLKeyAntiToxic";

    public static AppConfig Current { get; private set; } = new();

    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                string json = File.ReadAllText(ConfigPath, Encoding.UTF8);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json);
                if (loaded != null)
                {
                    Current = loaded;
                }
            }
        }
        catch { }

        // Sync registry startup status with Windows
        Current.StartWithWindows = IsStartupWithWindowsEnabled();
        VietnameseEngine.UpdateConfig(Current);
    }

    private static readonly object _saveLock = new();

    public static void Save()
    {
        VietnameseEngine.UpdateConfig(Current);

        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                lock (_saveLock)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(Current, options);
                    File.WriteAllText(ConfigPath, json, Encoding.UTF8);
                }
            }
            catch { }
        });
    }

    public static bool IsStartupWithWindowsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, false);
            return key?.GetValue(AppRegistryName) != null || key?.GetValue(OldAppRegistryName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartupWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
            if (key == null) return;

            string exePath = Environment.ProcessPath ?? "";
            if (enable && !string.IsNullOrEmpty(exePath))
            {
                key.SetValue(AppRegistryName, $"\"{exePath}\"");
                try { key.DeleteValue(OldAppRegistryName, false); } catch { }
                Current.StartWithWindows = true;
            }
            else
            {
                try { key.DeleteValue(AppRegistryName, false); } catch { }
                try { key.DeleteValue(OldAppRegistryName, false); } catch { }
                Current.StartWithWindows = false;
            }
        }
        catch { }
    }
}
