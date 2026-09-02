using System.Text.Json;

namespace DeskBoxIo;

/// <summary>
/// deskbox-io —— DeskBox 随记/待办双向 CLI 接口。
/// 用法：
///   deskbox-io status
///   deskbox-io widgets
///   deskbox-io read todo [--widget &lt;id&gt;]
///   deskbox-io read notes [--include-recent]
///   deskbox-io add todo --text "..." [--due &lt;ISO&gt;] [--important] [--color red] [--notes "..."] [--widget &lt;id&gt;] [--restart] [--force]
///   deskbox-io add note --text "..." [--title "..."] [--markdown] [--pin] [--restart] [--force]
/// 约定：stdout 输出机器可读 JSON，stderr 输出给人看的提示；退出码 0=成功 2=参数/策略错误。
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0) { PrintHelp(); return 2; }

        switch (args[0].ToLowerInvariant())
        {
            case "status": return Status();
            case "widgets": return TodoService.Widgets();
            case "read": return DispatchRead(args.Skip(1).ToArray());
            case "add": return DispatchAdd(args.Skip(1).ToArray());
            case "help":
            case "-h":
            case "--help":
                PrintHelp();
                return 0;
            default:
                Console.Error.WriteLine($"未知命令：{args[0]}");
                PrintHelp();
                return 2;
        }
    }

    // ---------- 子命令分发 ----------

    private static int DispatchRead(string[] rest)
    {
        if (rest.Length == 0) { Console.Error.WriteLine("用法：deskbox-io read todo|notes"); return 2; }
        var a = Args.Parse(rest);
        return a.Positionals[0].ToLowerInvariant() switch
        {
            "todo" => TodoService.Read(a.Get("widget")),
            "notes" => NotesService.Read(a.Has("include-recent")),
            _ => Fail($"未知读取对象：{a.Positionals[0]}（支持 todo / notes）"),
        };
    }

    private static int DispatchAdd(string[] rest)
    {
        if (rest.Length == 0) { Console.Error.WriteLine("用法：deskbox-io add todo|note [选项]"); return 2; }
        var a = Args.Parse(rest);
        return a.Positionals[0].ToLowerInvariant() switch
        {
            "todo" => TodoService.Add(
                a.Get("text") ?? "",
                a.Get("due"),
                a.Has("important"),
                a.Get("color"),
                a.Get("notes"),
                a.Get("widget"),
                a.Has("restart"),
                a.Has("force")),
            "note" or "notes" => NotesService.Add(
                a.Get("text") ?? "",
                a.Get("title"),
                a.Has("markdown"),
                a.Has("pin"),
                a.Has("restart"),
                a.Has("force")),
            _ => Fail($"未知添加对象：{a.Positionals[0]}（支持 todo / note）"),
        };
    }

    // ---------- status ----------

    private static int Status()
    {
        Console.WriteLine($"数据目录: {DeskBoxEnv.DataDir}");
        Console.WriteLine($"settings.json: {(File.Exists(DeskBoxEnv.SettingsPath) ? "存在" : "缺失")}");
        Console.WriteLine($"DeskBox 运行中: {(DeskBoxEnv.IsDeskBoxRunning() ? "是" : "否")}");

        var widgets = DeskBoxEnv.FindTodoWidgets();
        Console.WriteLine($"待办组件: {widgets.Count} 个");
        foreach (var w in widgets)
        {
            var exists = File.Exists(w.JsonPath);
            var count = exists ? CountItems(w.JsonPath) : "文件不存在";
            Console.WriteLine($"  - {w.Name} ({w.Id}) : {count} 条");
        }

        var qc = Path.Combine(DeskBoxEnv.DataDir, "quick-capture", "quick-capture.json");
        Console.WriteLine($"随记文件: {(File.Exists(qc) ? "存在" : "缺失")}");
        if (File.Exists(qc))
        {
            try
            {
                var data = JsonSerializer.Deserialize<QuickCaptureStoreData>(File.ReadAllText(qc), JsonOpts.Pretty);
                Console.WriteLine($"  记录 {data?.Items.Count ?? 0} 条, 最近 {data?.RecentItems.Count ?? 0} 条");
            }
            catch { Console.WriteLine("  (无法解析)"); }
        }
        return 0;
    }

    private static string CountItems(string path)
    {
        try
        {
            var d = JsonSerializer.Deserialize<TodoWidgetData>(File.ReadAllText(path), JsonOpts.Pretty);
            return d?.Items.Count.ToString() ?? "0";
        }
        catch { return "解析失败"; }
    }

    private static int Fail(string msg) { Console.Error.WriteLine("错误：" + msg); return 2; }

    private static void PrintHelp()
    {
        Console.WriteLine("""
        deskbox-io —— DeskBox 随记/待办双向接口

        命令:
          status                        查看数据目录/运行状态/组件概况
          widgets                       列出所有待办组件
          read todo [--widget <id>]     读取待办(JSON)
          read notes [--include-recent] 读取随记(JSON)
          add todo --text "..." [--due <ISO时间>] [--important] [--color red|orange|yellow|green|blue|purple|teal|pink] [--notes "..."] [--widget <id>]
          add note --text "..." [--title "..."] [--markdown] [--pin]

        通用开关:
          --restart  写完后自动重启 DeskBox，让新数据立即显示
          --force    DeskBox 运行中强制写（可能被 App 下次保存覆盖）
          --widget <id>  指定待办组件（默认第一个）

        输出约定: stdout 为 JSON，stderr 为人读提示；退出码 0=成功 2=参数/策略错误
        """);
    }
}

/// <summary>极简参数解析：--key value / --key=value / 布尔开关 --key / 位置参数。</summary>
public readonly record struct Args(Dictionary<string, string> Flags, List<string> Positionals)
{
    public static Args Parse(string[] args)
    {
        // 这些参数后面跟一个值（--due 2026-09-01 或 --due=2026-09-01 都支持）
        var valueFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "text", "due", "color", "notes", "widget", "title",
        };
        var flags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var positionals = new List<string>();
        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a.StartsWith("--", StringComparison.Ordinal))
            {
                var body = a[2..];
                var eq = body.IndexOf('=');
                if (eq >= 0)
                {
                    flags[body[..eq]] = body[(eq + 1)..];
                }
                else if (valueFlags.Contains(body) && i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    flags[body] = args[++i]; // 消费下一个参数作为值
                }
                else
                {
                    flags[body] = "true"; // 布尔开关
                }
            }
            else
            {
                positionals.Add(a);
            }
        }
        return new Args(flags, positionals);
    }

    public bool Has(string key) => Flags.TryGetValue(key, out var v) && v != "false";
    public string? Get(string key) => Flags.TryGetValue(key, out var v) ? v : null;
}
