using System.Text.Json;

namespace DeskBoxIo;

/// <summary>待办（Todo）命令：read todo / widgets / add todo。</summary>
public static class TodoService
{
    /// <summary>列出所有待办组件：id、名称、路径、条数。</summary>
    public static int Widgets()
    {
        var widgets = DeskBoxEnv.FindTodoWidgets();
        var list = widgets.Select(w => new
        {
            w.Id,
            w.Name,
            path = w.JsonPath,
            itemCount = File.Exists(w.JsonPath)
                ? (TryLoad(w.JsonPath)?.Items.Count ?? 0)
                : 0,
        }).ToList();
        Console.WriteLine(JsonSerializer.Serialize(list, JsonOpts.Pretty));
        return list.Count == 0 ? 1 : 0; // 没有待办组件时返回非 0，方便脚本判断
    }

    /// <summary>读取待办：全部组件或指定 --widget id。</summary>
    public static int Read(string? widgetId)
    {
        var widgets = PickWidgets(widgetId);
        if (widgets is null) return 2;

        var result = widgets.Select(w => new
        {
            w.Id,
            w.Name,
            path = w.JsonPath,
            data = File.Exists(w.JsonPath) ? TryLoad(w.JsonPath) : new TodoWidgetData(),
        }).ToList();

        Console.WriteLine(JsonSerializer.Serialize(result, JsonOpts.Pretty));
        return 0;
    }

    /// <summary>添加一条待办。</summary>
    public static int Add(
        string text, string? due, bool important, string? color, string? notes,
        string? widgetId, bool restart, bool force)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.Error.WriteLine("错误：--text 不能为空。");
            return 2;
        }

        DateTimeOffset? dueDate = null;
        if (due != null)
        {
            if (!DateTimeOffset.TryParse(due, out var d))
            {
                Console.Error.WriteLine($"错误：无法解析 --due 时间 '{due}'（需要 ISO 8601，如 2026-09-01T09:00:00+08:00）。");
                return 2;
            }
            dueDate = d;
        }

        string? colorMarker = null;
        if (color != null)
        {
            var c = color.Trim().ToLowerInvariant();
            var supported = new[] { "red", "orange", "yellow", "green", "blue", "purple", "teal", "pink" };
            if (!supported.Contains(c))
            {
                Console.Error.WriteLine($"错误：--color 仅支持 {string.Join("/", supported)}。");
                return 2;
            }
            colorMarker = c;
        }

        // 运行态策略：App 开着时默认拒绝，除非 --force 或 --restart
        if (DeskBoxEnv.IsDeskBoxRunning() && !force && !restart)
        {
            Console.Error.WriteLine("DeskBox 正在运行。为避免写入被 App 的内存数据覆盖：");
            Console.Error.WriteLine("  加 --restart 写完后自动重启 DeskBox，或加 --force 强制写（下次 App 保存时可能被覆盖）。");
            return 2;
        }

        var widgets = PickWidgets(widgetId);
        if (widgets is null || widgets.Count == 0)
        {
            Console.Error.WriteLine("错误：找不到待办组件（settings.json 里没有 widgetKind=Todo 的格子）。");
            return 2;
        }
        var w = widgets[0]; // 默认写第一个待办组件

        var data = File.Exists(w.JsonPath) ? TryLoad(w.JsonPath) ?? new TodoWidgetData() : new TodoWidgetData();
        var now = DateTimeOffset.UtcNow;
        var item = new TodoItem
        {
            Id = Guid.NewGuid().ToString("N"),      // 32 位无连字符 GUID，和 App 一致
            Text = text,
            IsCompleted = false,
            IsImportant = important,
            ColorMarker = colorMarker,
            DueDate = dueDate,
            Recurrence = null,
            Steps = new List<TodoStep>(),
            Notes = notes ?? null,                  // App 对空备注写 null，不是 ""
            Attachments = new List<TodoAttachment>(),
            CompletedAt = null,
            ReminderLastNotifiedAt = null,
            ReminderDismissedForDueDate = null,
            ReminderOffsetMinutes = null,
            SnoozedUntil = null,
            SnoozeLastNotifiedAt = null,
            RecurrenceSeriesId = null,
            GeneratedNextItemId = null,
            SortOrder = data.Items.Count == 0 ? 0 : data.Items.Max(i => i.SortOrder) + 1,
            CreatedAt = now,
            UpdatedAt = now,
        };
        data.Items.Add(item);

        DeskBoxEnv.WriteJsonAtomic(w.JsonPath, JsonSerializer.Serialize(data, JsonOpts.Pretty));

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            ok = true,
            widgetId = w.Id,
            widgetName = w.Name,
            itemId = item.Id,
            itemText = item.Text,
            file = w.JsonPath,
        }, JsonOpts.Pretty));

        if (restart) DeskBoxEnv.RestartDeskBox();
        return 0;
    }

    /// <summary>按 --widget 过滤组件；没指定就全部（读）或第一个（写）。</summary>
    private static List<DeskBoxEnv.TodoWidget>? PickWidgets(string? widgetId)
    {
        var all = DeskBoxEnv.FindTodoWidgets();
        if (widgetId == null) return all;
        var hit = all.Where(w => w.Id.Equals(widgetId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (hit.Count == 0)
        {
            Console.Error.WriteLine($"错误：找不到 id 为 '{widgetId}' 的待办组件。可用：deskbox-io widgets");
            return null;
        }
        return hit;
    }

    /// <summary>读文件并解析；文件损坏时返回 null 而不是抛异常。</summary>
    private static TodoWidgetData? TryLoad(string path)
    {
        try { return JsonSerializer.Deserialize<TodoWidgetData>(File.ReadAllText(path), JsonOpts.Pretty); }
        catch { return null; }
    }
}
