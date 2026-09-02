# DeskBox 数据格式与兼容性说明

> 本文档记录 deskbox-io 依赖的 DeskBox 磁盘数据格式。数据格式是"事实"，不受 DeskBox 的
> GPL-3.0 约束；本工具是独立实现，不包含任何 DeskBox 代码。

## 数据位置

根目录：`%LOCALAPPDATA%\DeskBox\data\`

| 内容 | 路径 | 说明 |
| --- | --- | --- |
| 待办 | `widgets\{widgetId}\todo.json` | 每个待办组件一个文件；组件清单在 `settings.json` 的 `widgets[]`（`widgetKind == "Todo"`） |
| 随记 | `quick-capture\quick-capture.json` | 全局一份（随记组件只是它的展示视图） |
| 设置 | `settings.json` | 全局设置 + 组件清单 |

## 序列化约定（DeskBox 自身的行为，deskbox-io 保持一致）

- camelCase 属性名，缩进输出（`WriteIndented`）
- 枚举以字符串存储（如 `type: "Text"`、`contentFormat: "Markdown"`、`appearancePreset: "Default"`）
- `version` 字段：todo.json = 3，quick-capture.json = 4
- `id`：32 位无连字符 GUID（`Guid.NewGuid().ToString("N")`）
- 时间：带时区的 ISO 8601（`DateTimeOffset`，如 `2026-09-01T15:52:28.9900805+00:00`）
- 空备注写 `null` 而非 `""`；未固定条目 `pinnedSortOrder = -1`

## todo.json（版本 3）

```jsonc
{
  "version": 3,
  "items": [
    {
      "id": "32位guid",
      "text": "任务标题",
      "isCompleted": false,
      "isImportant": false,
      "colorMarker": null,            // "red"/"orange"/"yellow"/"green"/"blue"/"purple"/"teal"/"pink" 或 null
      "dueDate": null,                // ISO 8601 或 null
      "recurrence": null,             // { "mode": "Daily|Weekly|Monthly|Weekdays", "anchorDueDate": ... } 或 null
      "steps": [],                    // 子任务: { id, text, isCompleted, sortOrder }
      "notes": null,                  // Markdown 备注
      "attachments": [],
      "completedAt": null,
      "reminderLastNotifiedAt": null,
      "reminderDismissedForDueDate": null,
      "reminderOffsetMinutes": null,
      "snoozedUntil": null,
      "snoozeLastNotifiedAt": null,
      "recurrenceSeriesId": null,
      "generatedNextItemId": null,
      "sortOrder": 0,
      "createdAt": "...",
      "updatedAt": "..."
    }
  ]
}
```

## quick-capture.json（版本 4）

```jsonc
{
  "version": 4,
  "currentView": "Records",          // Records / Pinned / Recent
  "items": [ /* 记录 */ ],
  "recentItems": [ /* 剪贴板最近 */ ]
}
```

单条记录字段：`id`、`type`（Text/Link/Image/Todo）、`body`、`contentFormat`（Text/Markdown）、
`title`、`url`、`imagePath`、`contentHash`、`attachments[]`、`isPinned`、`isRecent`、`isDeleted`、
`appearancePreset`（Default/Paper/StickyYellow/Rose/Mint/MistBlue）、`sourceKind`（Manual/Clipboard/Image/DragDrop）、
`tags[]`、`archivedAt`、`sortOrder`、`pinnedSortOrder`、`createdAt`、`updatedAt`。

## 安全写盘模式（DeskBox 的 ResilientJsonStore，deskbox-io 同样采用）

- 写前把旧文件复制为 `xxx.json.bak`
- 先写临时文件 `xxx.json.{guid}.tmp`，再改名替换（原子，防半截文件）
- 读取：主文件损坏 → 改名 `.corrupt-时间戳` 隔离 → 回退读 `.bak` → 恢复主文件；全部失败 → 空数据

## 兼容性风险

DeskBox 未来版本可能调整 schema（`version` 字段会随之变化）。deskbox-io 按已知版本读写；
升级 DeskBox 后若读写异常，先对比本文档与新版实际生成的文件，再决定是否适配。
