using System.Diagnostics;
using System.Text.Json;

namespace DeskBoxIo;

/// <summary>
/// 环境层：数据目录、待办组件发现、进程检测、写入锁、原子写、重启。
/// 把"和操作系统/磁盘打交道"的细节集中在这里，业务命令只管数据。
/// </summary>
public static class DeskBoxEnv
{
    /// <summary>DeskBox 数据根目录（%LOCALAPPDATA%\DeskBox\data）。</summary>
    public static string DataDir { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeskBox", "data");

    /// <summary>settings.json 路径（App 的全局设置，含组件清单）。</summary>
    public static string SettingsPath => Path.Combine(DataDir, "settings.json");

    /// <summary>
    /// 进程级写锁：同一时刻只允许一个写入者，避免多条命令并发写坏文件。
    /// 命名互斥量由操作系统管理，进程退出会自动释放（不怕"锁死"）。
    /// </summary>
    private static readonly Mutex s_writeLock = new(false, "DeskBoxIO.WriteLock");

    /// <summary>一个待办组件（settings.json 里 widgetKind == "Todo" 的格子）。</summary>
    public sealed class TodoWidget
    {
        public required string Id { get; init; }
        public string Name { get; init; } = "待办";
        public required string JsonPath { get; init; }
    }

    /// <summary>从 settings.json 找出所有待办组件；解析失败时回退为扫描 widgets 目录。</summary>
    public static List<TodoWidget> FindTodoWidgets()
    {
        var result = new List<TodoWidget>();
        if (File.Exists(SettingsPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(SettingsPath));
                if (doc.RootElement.TryGetProperty("widgets", out var arr))
                {
                    foreach (var w in arr.EnumerateArray())
                    {
                        var kind = GetString(w, "widgetKind");
                        if (!string.Equals(kind, "Todo", StringComparison.OrdinalIgnoreCase)) continue;
                        var id = GetString(w, "id");
                        if (string.IsNullOrEmpty(id)) continue;
                        result.Add(new TodoWidget
                        {
                            Id = id,
                            Name = GetString(w, "name") ?? "待办",
                            JsonPath = Path.Combine(DataDir, "widgets", id, "todo.json"),
                        });
                    }
                }
            }
            catch
            {
                // settings.json 损坏时退回到目录扫描（下方兜底）
            }
        }
        if (result.Count == 0)
        {
            var widgetsDir = Path.Combine(DataDir, "widgets");
            if (Directory.Exists(widgetsDir))
            {
                foreach (var dir in Directory.GetDirectories(widgetsDir))
                {
                    var jp = Path.Combine(dir, "todo.json");
                    if (File.Exists(jp))
                        result.Add(new TodoWidget { Id = Path.GetFileName(dir), Name = "待办", JsonPath = jp });
                }
            }
        }
        return result;
    }

    private static string? GetString(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) ? v.ValueKind == JsonValueKind.String ? v.GetString() : null : null;

    /// <summary>DeskBox 是否正在运行。</summary>
    public static bool IsDeskBoxRunning() => Process.GetProcessesByName("DeskBox").Length > 0;

    /// <summary>
    /// 原子写入：先备份旧文件为 .bak，再写临时文件，最后改名替换。
    /// 与 DeskBox 自己的 ResilientJsonStore 行为一致，崩溃也不会留下半截文件。
    /// </summary>
    public static void WriteJsonAtomic(string path, string json)
    {
        s_writeLock.WaitOne();
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(path))
                File.Copy(path, path + ".bak", overwrite: true);

            var tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, path, overwrite: true);
        }
        finally
        {
            s_writeLock.ReleaseMutex();
        }
    }

    /// <summary>重启 DeskBox：关掉所有进程，再启动 exe，让新数据立即生效。</summary>
    public static void RestartDeskBox()
    {
        var exe = FindDeskBoxExe();
        foreach (var p in Process.GetProcessesByName("DeskBox"))
        {
            try { p.Kill(); } catch { /* 可能无权限，忽略 */ }
        }
        Thread.Sleep(1200); // 给进程一点退出时间

        if (exe != null && File.Exists(exe))
        {
            Process.Start(new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe) });
            Console.Error.WriteLine("已重启 DeskBox。");
        }
        else
        {
            Console.Error.WriteLine("警告：未找到 DeskBox.exe，请手动重启 DeskBox 使数据生效。");
        }
    }

    private static string? FindDeskBoxExe()
    {
        // 优先取正在运行的进程路径（可能装在别的盘/目录）
        var running = Process.GetProcessesByName("DeskBox").FirstOrDefault();
        try
        {
            if (running?.MainModule?.FileName is { } f && File.Exists(f)) return f;
        }
        catch { /* 访问 MainModule 可能失败 */ }

        // 兜底：工具放在 D:\DeskBox\io\，App 在上一级
        var here = Path.GetDirectoryName(Environment.ProcessPath);
        var candidate = Path.Combine(here ?? @"D:\DeskBox", "..", "DeskBox.exe");
        var full = Path.GetFullPath(candidate);
        if (File.Exists(full)) return full;
        return null;
    }
}
