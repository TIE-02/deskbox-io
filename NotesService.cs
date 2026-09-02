using System.Text.Json;

namespace DeskBoxIo;

/// <summary>随记（QuickCapture）命令：read notes / add note。</summary>
public static class NotesService
{
    private static string StorePath => Path.Combine(DeskBoxEnv.DataDir, "quick-capture", "quick-capture.json");

    /// <summary>读取随记；--include-recent 时把剪贴板"最近"也带上。</summary>
    public static int Read(bool includeRecent)
    {
        var data = LoadOrNew();
        if (data is null) return 2; // LoadOrNew 已打印原因
        var result = new Dictionary<string, object?>
        {
            ["currentView"] = data.CurrentView,
            ["items"] = data.Items,
        };
        if (includeRecent) result["recentItems"] = data.RecentItems;
        Console.WriteLine(JsonSerializer.Serialize(result, JsonOpts.Pretty));
        return 0;
    }

    /// <summary>添加一条随记（记录 Tab）。</summary>
    public static int Add(string text, string? title, bool markdown, bool pin, bool restart, bool force)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.Error.WriteLine("错误：--text 不能为空。");
            return 2;
        }

        // 运行态策略，与待办一致
        if (DeskBoxEnv.IsDeskBoxRunning() && !force && !restart)
        {
            Console.Error.WriteLine("DeskBox 正在运行。为避免写入被 App 的内存数据覆盖：");
            Console.Error.WriteLine("  加 --restart 写完后自动重启 DeskBox，或加 --force 强制写（下次 App 保存时可能被覆盖）。");
            return 2;
        }

        var data = LoadOrNew();
        if (data is null) return 2; // LoadOrNew 已打印原因
        var now = DateTimeOffset.UtcNow;
        var item = new QuickCaptureItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Type = "Text",
            Body = text,
            ContentFormat = markdown ? "Markdown" : "Text",
            Title = title ?? null,
            Url = null,
            ImagePath = null,
            ContentHash = null,
            Attachments = new List<TodoAttachment>(),
            IsPinned = pin,
            IsRecent = false,
            IsDeleted = false,
            AppearancePreset = "Default",
            SourceKind = "Manual",
            Tags = new List<string>(),
            ArchivedAt = null,
            SortOrder = data.Items.Count == 0 ? 0 : data.Items.Max(i => i.SortOrder) + 1,
            PinnedSortOrder = pin ? 0 : -1,   // 未固定为 -1，固定后 App 会自行重排
            CreatedAt = now,
            UpdatedAt = now,
        };
        data.Items.Add(item);

        DeskBoxEnv.WriteJsonAtomic(StorePath, JsonSerializer.Serialize(data, JsonOpts.Pretty));

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            ok = true,
            item.Id,
            item.Body,
            file = StorePath,
        }, JsonOpts.Pretty));

        if (restart) DeskBoxEnv.RestartDeskBox();
        return 0;
    }

    /// <summary>读文件并解析；文件不存在时返回全新的空数据；损坏时返回 null（不覆盖原文件）。</summary>
    private static QuickCaptureStoreData? LoadOrNew()
    {
        if (!File.Exists(StorePath)) return new QuickCaptureStoreData();
        try { return JsonSerializer.Deserialize<QuickCaptureStoreData>(File.ReadAllText(StorePath), JsonOpts.Pretty); }
        catch
        {
            Console.Error.WriteLine("错误：quick-capture.json 无法解析，拒绝写入（请人工检查该文件）。");
            return null;
        }
    }
}
