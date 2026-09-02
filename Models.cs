using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeskBoxIo;

/// <summary>
/// 与 DeskBox 磁盘 JSON 完全同构的数据模型（camelCase 属性名）。
/// 字段名和 .bak 备份文件里的真实数据一一对应，保证 App 能认。
/// </summary>

// ========== 待办（data\widgets\{widgetId}\todo.json） ==========

public class TodoWidgetData
{
    public int Version { get; set; } = 3;
    public List<TodoItem> Items { get; set; } = new();
}

public class TodoItem
{
    public string? Id { get; set; }
    public string? Text { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsImportant { get; set; }
    public string? ColorMarker { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public TodoRecurrence? Recurrence { get; set; }
    public List<TodoStep> Steps { get; set; } = new();
    public string? Notes { get; set; }
    public List<TodoAttachment> Attachments { get; set; } = new();
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ReminderLastNotifiedAt { get; set; }
    public DateTimeOffset? ReminderDismissedForDueDate { get; set; }
    public int? ReminderOffsetMinutes { get; set; }
    public DateTimeOffset? SnoozedUntil { get; set; }
    public DateTimeOffset? SnoozeLastNotifiedAt { get; set; }
    public string? RecurrenceSeriesId { get; set; }
    public string? GeneratedNextItemId { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class TodoStep
{
    public string? Id { get; set; }
    public string? Text { get; set; }
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }
}

public class TodoRecurrence
{
    public string? Mode { get; set; }
    public DateTimeOffset? AnchorDueDate { get; set; }
}

public class TodoAttachment
{
    public string? Id { get; set; }
    public string? FilePath { get; set; }
    public string? DisplayName { get; set; }
    public string? Type { get; set; }
    public string? StorageMode { get; set; }
    public DateTimeOffset AddedAt { get; set; }
    public bool IsManagedCopy { get; set; }
}

// ========== 随记（data\quick-capture\quick-capture.json） ==========

public class QuickCaptureStoreData
{
    public int Version { get; set; } = 4;
    public string CurrentView { get; set; } = "Records";
    public List<QuickCaptureItem> Items { get; set; } = new();
    public List<QuickCaptureItem> RecentItems { get; set; } = new();
}

public class QuickCaptureItem
{
    public string? Id { get; set; }
    // 枚举以字符串形式存储（Text / Link / Image / Todo）
    public string Type { get; set; } = "Text";
    public string? Body { get; set; }
    // Text / Markdown
    public string ContentFormat { get; set; } = "Text";
    public string? Title { get; set; }
    public string? Url { get; set; }
    public string? ImagePath { get; set; }
    public string? ContentHash { get; set; }
    public List<TodoAttachment> Attachments { get; set; } = new();
    public bool IsPinned { get; set; }
    public bool IsRecent { get; set; }
    public bool IsDeleted { get; set; }
    // Default / Paper / StickyYellow / Rose / Mint / MistBlue
    public string AppearancePreset { get; set; } = "Default";
    // Manual / Clipboard / Image / DragDrop
    public string SourceKind { get; set; } = "Manual";
    public List<string> Tags { get; set; } = new();
    public DateTimeOffset? ArchivedAt { get; set; }
    public int SortOrder { get; set; }
    public int PinnedSortOrder { get; set; } = -1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>JSON 读写共用的序列化配置，与 App 的 s_jsonOptions 一致。</summary>
public static class JsonOpts
{
    public static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,                              // 缩进输出，和 App 写盘格式一致
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // 属性名转 camelCase（IsCompleted → isCompleted）
        PropertyNameCaseInsensitive = true,                // 读取时大小写宽容
        DefaultIgnoreCondition = JsonIgnoreCondition.Never // 显式写出 null 字段（App 会写 notes: null）
    };
}
