---
name: deskbox-io
description: 'DeskBox 随记/待办双向接口。当用户要求读取、添加或修改 DeskBox（桌面格子应用）的随记（QuickCapture）或待办（Todo）内容时使用。典型触发："给 DeskBox 加一条待办/随记"、"读一下我的待办"、"往随记里记一条"、"用 deskbox-io 添加…"。'
---

# DeskBox 随记/待办接口（deskbox-io）

读写 DeskBox 桌面临时应用（格子/小组件）的**随记**（QuickCapture）与**待办**（Todo）数据。
实现是独立的 C# CLI 工具，直接读写 DeskBox 的 JSON 数据文件——不碰 DeskBox 进程内部状态。

## 第一步：定位工具（跨机器通用，不要假设路径存在）

按顺序查找，找到即用；不要报错瞎跑：

1. `Get-Command deskbox-io -ErrorAction SilentlyContinue`（已加入 PATH 就直接用）
2. 环境变量 `$env:DESKBOX_IO` 若已设置（指向 deskbox-io.exe 或 .cmd）
3. 用户明确给出的路径（用户说"工具在 xxx"，直接用）
4. 已知候选位置（仅作本机提示，不写死）：`D:\DeskBox\io\deskbox-io.cmd`、`D:\DeskBox\io\bin\Debug\net10.0\deskbox-io.exe`、`D:\DeskBox\io\bin\Release\net10.0\win-x64\publish\deskbox-io.exe`（免安装自包含单文件版，可拷到无 .NET 的机器直接用）；或从**当前工作目录**出发向上逐级、再在当前工作区里递归查找 `deskbox-io.cmd` / `deskbox-io*.exe` / `deskbox-io.csproj`
5. 找到 `deskbox-io.csproj` 源码但没有 exe → 用 `dotnet build` 构建（构建需先设置 `$env:DOTNET_CLI_HOME="<某处>\.dotnet-home"; $env:NuGetAudit="false"`，且目标机需装有 .NET SDK）
6. 全部找不到 → **直接问用户**"deskbox-io 的 io 文件夹放在哪台机器的哪个路径？"，不要伪造结果；同时说明：新机器需要 `D:\DeskBox\io` 整个文件夹 + .NET 10 运行时（见文末"部署到新机器"）

工具路径可能因环境不同而变化，**每次使用前先定位**。

## 数据位置（运行时自动解析，无需写死）

- 根目录：`%LOCALAPPDATA%\DeskBox\data`
- 待办：`data\widgets\{widgetId}\todo.json`（每个待办组件一个文件，可能有多个；用 `widgets` 命令列出）
- 随记：`data\quick-capture\quick-capture.json`（全局一份）

## 命令

```
deskbox-io status                         # 数据目录/进程状态/组件概况
deskbox-io widgets                        # 列出待办组件 id/名称/条数
deskbox-io read todo [--widget <id>]      # 读待办（JSON，不指定=全部组件）
deskbox-io read notes [--include-recent]  # 读随记（JSON）
deskbox-io add todo --text "..." [--due <ISO时间>] [--important] [--color red|orange|yellow|green|blue|purple|teal|pink] [--notes "..."] [--widget <id>]
deskbox-io add note --text "..." [--title "..."] [--markdown] [--pin]
```

通用开关：`--restart`（写完后自动重启 DeskBox 让数据立即生效）、`--force`（App 运行时强写，可能被覆盖，慎用）。

示例：
```
deskbox-io read todo
deskbox-io add todo --text "买牛奶" --due 2026-09-02T09:00:00+08:00 --important --color blue --restart
deskbox-io add note --text "记得喝水" --markdown --restart
```

## 必须遵守的规则

1. **输出约定**：stdout 是机器可读 JSON，stderr 是人读提示；退出码 0=成功，2=参数/策略错误。向用户汇报时读 JSON 里的字段（如 `itemId`），不要原样贴整段 JSON。
2. **运行态策略**：DeskBox 正在运行时，`add` **默认会被拒绝**（退出码 2，防数据被 App 内存覆盖）。此时：
   - 用户想让数据立即生效 → 加 `--restart`（会重启 DeskBox，属预期行为，先告知用户）；
   - 或加 `--force`（不推荐，下次 App 保存可能覆盖）。
3. **DSH 沙箱**：在 DSH 里通过 pwsh 执行 `add` 会写 `%LOCALAPPDATA%`（工作区之外），沙箱会拦截并需要升级权限（`sandbox_permissions: "danger-full-access"` + justification），会弹审批给用户。`read`/`status`/`widgets` 不需要。用户自己在命令行运行则无此限制。
4. **写入安全由工具保证**：写前自动备份 `.bak`、先写临时文件再原子替换、字段与 App 磁盘格式完全同构（camelCase、32 位无连字符 GUID、带时区时间）。不要手工改 JSON 文件，一律用工具。
5. **多组件**：待办可能有多个组件。`read` 默认返回全部；`add` 默认写第一个（可用 `--widget <id>` 指定）；不确定时先跑 `widgets` 确认。
6. **--text 含特殊字符**：在 pwsh 里用双引号包裹；含换行的 Markdown 建议改用 `--notes` 或多条消息说明。

## 部署到新机器（完整清单）

要把这套功能复刻到另一台机器，需要：

1. **`io` 文件夹**（`deskbox-io.csproj` + 源码 + `bin\Debug\net10.0\` 已构建产物 + `deskbox-io.cmd`）整体拷贝过去；
2. **目标机装 .NET 10 运行时**（当前构建是框架依赖版；DeskBox 1.4.8 是 Native AOT、自带运行时，**不会**提供 .NET）。可用 `winget install Microsoft.DotNet.Runtime.10` 或从 dot.net 下载；只有装了运行时，`deskbox-io.exe`/`deskbox-io.cmd` 才能跑（有 .NET SDK 才能现场构建）。若想要免安装的单文件版：在**有网**的普通终端运行 `D:\DeskBox\io\publish-selfcontained.ps1`（或手动 `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`），把产物单文件拷过去即可；
3. **本 SKILL.md** 放到新机器 DSH 的技能目录（用户级 `C:\Users\<用户>\.agents\skills\deskbox-io\SKILL.md`，或项目级 `<工作区>\.agents\skills\deskbox-io\SKILL.md`）——放好即可被新对话自动发现；
4. 数据无需迁移：工具在每台机器上读写**本机**的 `%LOCALAPPDATA%\DeskBox\data`（DeskBox 装好即自动适配）。若想把现有随记/待办带过去，单独拷贝 `data` 目录即可。

## 常见任务速查

- "看看我有什么待办" → `read todo`
- "记一条随记" → `add note --text "..." --restart`
- "加个明天到期的蓝色重要待办" → `add todo --text "..." --due <明天日期>T09:00:00+08:00 --important --color blue --restart`
- "待办去哪了" → `status` / `widgets`
