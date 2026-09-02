# AGENTS.md — 给在 deskbox-io 仓库里工作的 AI 代理的指引

本仓库是 **deskbox-io**：DeskBox 随记/待办的双向 CLI 接口（独立工具，非官方）。

## 构建与自测

```bat
dotnet build deskbox-io.csproj
bin\Debug\net10.0\deskbox-io.exe status     # 应输出数据目录/运行状态
```

注意：本机若在 DSH 沙箱内构建，先设置 `$env:DOTNET_CLI_HOME="<工作目录>\.dotnet-home"; $env:NuGetAudit="false"`（本项目零 NuGet 依赖，离线可构建）。

## 工具契约（改代码前先遵守）

- 命令：`status` / `widgets` / `read todo [--widget <id>]` / `read notes [--include-recent]`
  / `add todo --text ... [--due <ISO>] [--important] [--color red|orange|yellow|green|blue|purple|teal|pink] [--notes ...]`
  / `add note --text ... [--title ...] [--markdown] [--pin]`
- 通用开关：`--restart`（写后重启 DeskBox）、`--force`（App 运行时强写，慎用）、`--widget <id>`
- 输出约定：stdout 是机器可读 JSON，stderr 是人读提示；退出码 0=成功，2=参数/策略错误
- 数据位置：`%LOCALAPPDATA%\DeskBox\data\`（待办 = `widgets\{id}\todo.json`，随记 = `quick-capture\quick-capture.json`）

## 代码结构

- `Program.cs` — 入口：子命令分发、参数解析、status
- `Models.cs` — 与 DeskBox 磁盘 JSON 同构的数据模型 + 序列化配置
- `DeskBoxEnv.cs` — 环境层：数据目录、组件发现、进程检测、写锁、原子写+备份、重启
- `TodoService.cs` — 待办命令
- `NotesService.cs` — 随记命令

## 重要规则

1. App 运行时写入默认拒绝（策略在工具内）；不要绕过工具手改 DeskBox 的 JSON。
2. 字段必须与 DeskBox 磁盘格式同构（camelCase、32 位无连字符 GUID、带时区时间）——改模型前先对照
   `docs/data-format.md` 与 DeskBox 实际产生的文件。
3. 提交前不要包含 `bin/`、`obj/`、`backups/`（含真实数据）、`publish/`（大文件走 Release 资产）。
