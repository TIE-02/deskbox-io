# deskbox-io

**AI-agent & CLI interface for DeskBox notes (随记) and todos (待办)**

A small, independent command-line tool that lets AI agents (DSH / Codex / Claude Code…) and scripts
**read and add** DeskBox widgets' notes and todos by talking to DeskBox's JSON data files directly —
no modification to DeskBox itself.

[English](#english) · [中文](#中文)

---

## English

### What it does

- `read todo / notes` — list what you have
- `add todo / note` — add new items (colors, due dates, markdown, pin…)
- Safe writes: automatic `.bak` backup + atomic file replace, schema identical to DeskBox's own format
- `--restart` — restart DeskBox after writing so changes show immediately

### Compatibility

- DeskBox ≥ 1.4.x (works with the 1.4.8 Native AOT build too — this tool only touches data files, never the binary)
- Data lives in `%LOCALAPPDATA%\DeskBox\data\`
  - todos: `data\widgets\{widgetId}\todo.json` (one file per todo widget)
  - notes: `data\quick-capture\quick-capture.json` (single global file)

### Install

**Option A — no .NET needed (recommended):** download `deskbox-io.exe` (self-contained, ~70 MB) from the
[Releases](../../releases) page and run it on any x64 Windows.

**Option B — build from source (needs .NET 10 SDK):**

```bat
dotnet build deskbox-io.csproj
bin\Debug\net10.0\deskbox-io.exe status
```

### Verify your download (SHA-256)

The release `deskbox-io.exe` is **unsigned**, so Windows shows an "unknown publisher" warning — that is normal.
To make sure the file you downloaded is exactly what was published (not corrupted or tampered with), compare its
SHA-256 with the one shown on the [Releases](../../releases) page of the version you downloaded:

```powershell
Get-FileHash .\deskbox-io.exe -Algorithm SHA256
```

Both values must match. Note: SHA-256 verifies the file is **unchanged since upload**; it does not prove authorship
(that would require code signing) — always download from the official Releases page over HTTPS.

### Usage

```bat
deskbox-io status
deskbox-io widgets
deskbox-io read todo
deskbox-io read notes --include-recent
deskbox-io add todo --text "买牛奶" --due 2026-09-02T09:00:00+08:00 --important --color blue --notes "备注" --restart
deskbox-io add note --text "记得喝水" --title "备忘" --markdown --pin --restart
```

Common flags: `--restart` (restart DeskBox after writing), `--force` (write while DeskBox is running — may be
overwritten by the app's next save, use with care), `--widget <id>` (target a specific todo widget; defaults to the first).

Conventions: stdout = machine-readable JSON, stderr = human hints; exit code `0` = ok, `2` = argument/policy error.

> While DeskBox is running, `add` is **rejected by default** (to avoid the app's in-memory state overwriting your
> write). Add `--restart` to make it take effect immediately, or close DeskBox first.

### For AI agents

- [`AGENTS.md`](AGENTS.md) — auto-read by Codex / Claude Code / Cursor when working in this repo
- [`skills/deskbox-io/SKILL.md`](skills/deskbox-io/SKILL.md) — DSH / Claude-Code-style skill; install by copying the
  folder to `~/.agents/skills/` (or `<workspace>/.agents/skills/`)
- Machine-readable JSON on stdout makes it easy for any agent to parse results

### Relationship to DeskBox

**Unofficial, independent tool.** Not affiliated with the DeskBox project.
[DeskBox](https://github.com/Tianyu199509/DeskBox) is GPL-3.0; this tool contains **no DeskBox code** — it only
reads/writes the JSON files DeskBox itself produces (data formats are documented in
[docs/data-format.md](docs/data-format.md)). If DeskBox changes its storage format in the future, compatibility may
break — check the release notes before upgrading.

### License

[MIT](LICENSE)

---

## 中文

### 这是什么

一个独立的命令行小工具，让 **AI Agent（DSH / Codex / Claude Code…）和脚本**直接**读取、添加** DeskBox
随记与待办：它直接读写 DeskBox 的 JSON 数据文件，不修改 DeskBox 本体。

### 功能

- `read todo / notes`：读取现有内容
- `add todo / note`：添加新条目（支持颜色标记、截止日期、Markdown、置顶等）
- 写入安全：自动 `.bak` 备份 + 原子替换，字段与 DeskBox 磁盘格式完全同构
- `--restart`：写完后自动重启 DeskBox，让新数据立即显示

### 兼容性

- DeskBox ≥ 1.4.x（1.4.8 Native AOT 版同样适用——本工具只碰数据文件，不碰二进制）
- 数据位置：`%LOCALAPPDATA%\DeskBox\data\`
  - 待办：`data\widgets\{widgetId}\todo.json`（每个待办组件一个文件）
  - 随记：`data\quick-capture\quick-capture.json`（全局一份）

### 安装

**方式 A（推荐，无需 .NET）**：从 [Releases](../../releases) 下载自包含单文件 `deskbox-io.exe`（约 70 MB），
任意 x64 Windows 直接运行。

**方式 B（源码构建，需要 .NET 10 SDK）**：

```bat
dotnet build deskbox-io.csproj
bin\Debug\net10.0\deskbox-io.exe status
```

### 校验下载文件（SHA-256）

发布的 `deskbox-io.exe` **未签名**，Windows 会提示"未知发布者"——属正常现象。为确保下载的文件就是你发布
的那个（未被损坏或篡改），请把本地算出的 SHA-256 与对应版本的 [Releases](../../releases) 页面公布的哈希比对：

```powershell
Get-FileHash .\deskbox-io.exe -Algorithm SHA256
```

两者必须一致。注意：SHA-256 只能证明文件**上传后未被改动**，不能证明作者身份（那需要代码签名）——
请始终通过 HTTPS 从官方 Releases 页面下载。

### 用法示例

```bat
deskbox-io add todo --text "买牛奶" --due 2026-09-02T09:00:00+08:00 --important --color blue --restart
deskbox-io add note --text "记得喝水" --title "备忘" --markdown --restart
```

约定：stdout 输出机器可读 JSON，stderr 输出给人看的提示；退出码 0=成功，2=参数/策略错误。
> DeskBox 运行时 `add` 默认会被拒绝（防止被 App 内存数据覆盖），加 `--restart` 立即生效，或先关闭 DeskBox。

### 给 AI Agent 用

- [`AGENTS.md`](AGENTS.md)：Codex / Claude Code / Cursor 等在此仓库工作时会自动读取
- [`skills/deskbox-io/SKILL.md`](skills/deskbox-io/SKILL.md)：DSH / Claude Code 风格技能；把该文件夹复制到
  `~/.agents/skills/`（或 `<工作区>/.agents/skills/`）即可被新对话发现

### 与 DeskBox 的关系

**非官方、独立工具**，与 DeskBox 项目无隶属关系。
[DeskBox](https://github.com/Tianyu199509/DeskBox) 使用 GPL-3.0；本工具**不含任何 DeskBox 代码**——只读写
DeskBox 自己产生的 JSON 文件（数据格式说明见 [docs/data-format.md](docs/data-format.md)）。若 DeskBox 未来
改动存储格式，兼容性可能受影响——升级前请留意其更新日志。

### License

[MIT](LICENSE)
