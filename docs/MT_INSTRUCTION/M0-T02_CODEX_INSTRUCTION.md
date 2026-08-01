# M0-T02 Codex 执行指令

## 任务名称

```text
M0-T02：初始化现有 Godot 4.7.1 .NET/C# 工程与基础主场景
```

---

## 0. 当前环境说明

Godot 4.7.1 .NET 发行包已经完整放入以下目录：

```text
E:\SteamLibrary\steamapps\common\Godot Engine
```

当前目录应至少包含：

```text
E:\SteamLibrary\steamapps\common\Godot Engine\
├─ GodotSharp\
├─ godot.windows.opt.tools.64.exe
├─ Godot_v4.7.1-stable_mono_win64.exe
└─ Godot_v4.7.1-stable_mono_win64_console.exe
```

本任务中优先使用以下明确的 .NET 主程序：

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe
```

Steam 启动兼容文件：

```text
E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe
```

Codex 不得修改 Godot 安装目录中的任何文件。

---

# 1. 固定路径

## 1.1 仓库根目录

```text
D:\UGit\EnglishLearningProject
```

## 1.2 现有 Godot 工程目录

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

## 1.3 Godot 4.7.1 .NET 主程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe
```

## 1.4 Godot 4.7.1 .NET 控制台程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe
```

## 1.5 Steam 启动兼容程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe
```

## 1.6 根解决方案

```text
D:\UGit\EnglishLearningProject\GameLexicon.sln
```

---

# 2. 开始前必须完成的检查

开始实施前，必须执行：

1. 阅读：

   ```text
   AGENTS.md
   docs/PRODUCT_SPEC.md
   docs/IMPLEMENTATION_STATUS.md
   ```

2. 检查当前 Git 状态：

   ```powershell
   Set-Location "D:\UGit\EnglishLearningProject"
   git status
   ```

3. 确认 `M0-T01` 已完成。

4. 确认 `M0-T01` 已有本地 Git 提交；若尚未提交，停止并报告，不继续修改 Godot 工程。

5. 确认当前仓库根目录为：

   ```text
   D:\UGit\EnglishLearningProject
   ```

6. 确认现有 Godot 工程仍位于：

   ```text
   D:\UGit\EnglishLearningProject\english-learning-project
   ```

7. 确认仓库中只有一个 `project.godot`。

8. 确认不存在会被覆盖的用户未提交修改。

9. 确认当前没有另一个 Godot 编辑器实例正在打开该项目。

10. 不移动或重命名：

   ```text
   english-learning-project/
   ```

11. 不创建第二个 Godot 工程。

12. 不修改：

   ```text
   E:\SteamLibrary\steamapps\common\Godot Engine
   ```

---

# 3. 第一阶段：验证 Godot 4.7.1 .NET 与 .NET SDK

## 3.1 验证文件存在

执行：

```powershell
$GodotDir = "E:\SteamLibrary\steamapps\common\Godot Engine"
$GodotExe = Join-Path $GodotDir "Godot_v4.7.1-stable_mono_win64.exe"
$GodotConsoleExe = Join-Path $GodotDir "Godot_v4.7.1-stable_mono_win64_console.exe"
$SteamGodotExe = Join-Path $GodotDir "godot.windows.opt.tools.64.exe"
$GodotSharpDir = Join-Path $GodotDir "GodotSharp"

Test-Path $GodotExe
Test-Path $GodotConsoleExe
Test-Path $SteamGodotExe
Test-Path $GodotSharpDir
```

四项都必须返回：

```text
True
```

## 3.2 验证 Godot 版本

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" --version

& "E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe" --version
```

记录两条完整输出。

必须确认：

- 两个可执行文件均可以启动。
- 两个版本均为 Godot 4.7.1。
- 主程序输出包含 `.mono`、`.NET` 或其他可明确证明 C# 发行版的标识。
- `GodotSharp/` 目录存在。
- 两个可执行文件的文件大小、文件版本和 ProductVersion 与当前 .NET 发行版相符。

## 3.3 验证 GodotSharp 内容

检查：

```powershell
Get-ChildItem `
  "E:\SteamLibrary\steamapps\common\Godot Engine\GodotSharp" `
  -Force

Get-ChildItem `
  "E:\SteamLibrary\steamapps\common\Godot Engine\GodotSharp" `
  -Recurse `
  -File |
  Select-Object -First 50 FullName
```

必须确认该目录不是空目录，并包含 Godot C#/.NET 支持所需的程序集、SDK 或工具文件。

不得修改或重新生成该目录。

## 3.4 验证 .NET SDK

执行：

```powershell
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

记录：

- 默认 SDK。
- 已安装 SDK。
- 已安装 Runtime。
- 系统架构。

预期至少包含：

```text
8.0.423
10.0.301
```

要求：

- `.NET SDK` 架构为 x64。
- Godot 与 `.NET SDK` 架构一致。
- M0-T02 优先使用 Godot 自动生成项目所要求的目标框架。
- 不因默认 SDK 是 .NET 10 而手工把 Godot 项目改成 `net10.0`。

## 3.5 查看 Godot 命令行帮助

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" --help
```

记录与以下能力有关的实际参数：

- `--editor`
- `--path`
- `--headless`
- `--quit`
- `--build-solutions`
- C#/.NET 构建相关参数

后续命令应以该版本实际帮助输出为准。

## 3.6 强制停止条件

出现以下任意情况时，立即停止 M0-T02：

- `.NET` 主程序不存在。
- `GodotSharp/` 不存在或为空。
- 主程序无法启动。
- 版本不是 Godot 4.7.1。
- 无法确认这是支持 C# 的 .NET/mono 版本。
- Steam 启动兼容程序仍然指向标准版。
- Godot 与 `.NET SDK` 架构不一致。
- 当前项目有另一个 Godot 实例正在打开。
- Git 工作区存在可能被覆盖的用户修改。
- 仓库中出现多个 `project.godot`。
- 现有 Godot 工程路径与文档不一致。

停止时必须：

1. 不修改 Godot 工程。
2. 不创建 `.csproj`。
3. 不创建场景。
4. 不创建 C# 脚本。
5. 不修改根解决方案。
6. 不修改 Godot 安装目录。
7. 只报告：
   - 阻塞原因
   - 实际检测结果
   - 推荐解决步骤

---

# 4. 第二阶段：初始化现有 Godot C# 工程

只有第一阶段全部通过后才能继续。

## 4.1 初始化规则

必须在以下现有工程中初始化：

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

要求：

- 保留现有 `project.godot`。
- 保留现有 `icon.svg`。
- 保留现有 `icon.svg.import`。
- 不创建第二个工程。
- 不移动目录。
- 不重命名目录。
- 不手工伪造 Godot `.csproj`。
- 应由 Godot 4.7.1 .NET 初始化 C# 项目。
- 不提前把 Godot 项目目标框架改成 `net10.0`。
- 不自行替换 Godot 自动生成的 SDK、TargetFramework 或构建属性。

## 4.2 创建目录

创建：

```text
english-learning-project/scripts/
english-learning-project/scenes/
```

不得创建其他无关目录。

## 4.3 创建最小 C# 脚本

创建：

```text
english-learning-project/scripts/AppRoot.cs
```

内容：

```csharp
using Godot;

public partial class AppRoot : Control
{
    public override void _Ready()
    {
        GD.Print("GameLexicon AppRoot initialized.");
    }
}
```

要求：

- 文件名与类名匹配。
- 类继承 `Control`。
- 使用 `partial`。
- 当前只实现最小启动逻辑。
- 不添加业务功能。
- 不访问数据库。
- 不启动 OCR。
- 不调用 Windows API。
- 不添加第三方包。

## 4.4 初始化 Godot C# 项目文件

优先使用控制台程序进行可观察的命令行操作：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --editor `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

如需要 GUI 初始化，可使用：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" `
  --editor `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

确保由 Godot 生成：

```text
english-learning-project/*.csproj
```

如果 Godot 同时生成本地 `.sln`：

- 可以保留。
- 根解决方案仍以：

  ```text
  D:\UGit\EnglishLearningProject\GameLexicon.sln
  ```

  为主。

记录：

- Godot 自动生成的 `.csproj` 文件名。
- `TargetFramework`。
- Godot SDK 配置。
- `RootNamespace`。
- `EnableDynamicLoading` 等关键构建属性。
- 是否生成本地 `.sln`。
- 是否生成 `.godot/mono/`。

## 4.5 目标框架规则

当前普通 C# 项目暂时使用：

```text
net10.0
```

Godot 项目的目标框架必须以 Godot 自动生成结果为准。

不得：

- 将 Godot 项目手工修改为 `net10.0`。
- 在未验证兼容性前统一修改所有项目。
- 忽略 TargetFramework 不兼容错误。
- 删除或覆盖 Godot 自动生成的 SDK 设置。

如果 Godot 项目与普通类库目标框架不兼容：

1. 停止后续集成。
2. 报告：
   - Godot 项目实际 TargetFramework
   - 普通类库实际 TargetFramework
   - 完整构建错误
   - 推荐调整方案
3. 不自行批量修改所有项目。
4. 等待用户确认后再调整目标框架。

---

# 5. 第三阶段：创建基础主场景

## 5.1 创建场景

创建：

```text
english-learning-project/scenes/App.tscn
```

基础节点结构：

```text
AppRoot (Control)
├─ Background (ColorRect)
├─ AppLayout (HBoxContainer)
│  ├─ Sidebar (PanelContainer)
│  └─ ContentHost (MarginContainer)
├─ ToastLayer (CanvasLayer)
├─ ModalLayer (CanvasLayer)
└─ GlobalLoadingOverlay (CanvasLayer)
```

## 5.2 节点要求

### AppRoot

- 类型：`Control`
- 脚本：

  ```text
  res://scripts/AppRoot.cs
  ```

- Layout：Full Rect
- 作为根节点

### Background

- 类型：`ColorRect`
- Layout：Full Rect
- 作为应用背景
- 当前不要求正式主题设计

### AppLayout

- 类型：`HBoxContainer`
- Layout：Full Rect
- 用于后续承载侧栏和内容区

### Sidebar

- 类型：`PanelContainer`
- 只创建占位结构
- 建议最小宽度约 220 像素
- 当前不实现正式导航按钮

### ContentHost

- 类型：`MarginContainer`
- 只创建占位内容区
- 应填满剩余空间

### ToastLayer

- 类型：`CanvasLayer`
- 当前只创建空层

### ModalLayer

- 类型：`CanvasLayer`
- 当前只创建空层

### GlobalLoadingOverlay

- 类型：`CanvasLayer`
- 当前只创建空层

## 5.3 设置主场景

将：

```text
res://scenes/App.tscn
```

设置为 Godot 主场景。

只允许对 `project.godot` 做以下必要修改：

- 设置主场景。
- 记录 C#/.NET 工程所需配置。
- 由 Godot 自动写入的必要设置。

不得修改与本任务无关的：

- 渲染器。
- 物理引擎。
- 窗口行为。
- 输入映射。
- 插件配置。

## 5.4 当前界面要求

工程运行后只需显示：

- 一个基础窗口。
- 一个背景。
- 一个左侧占位区域。
- 一个主内容占位区域。

不要求：

- 正式视觉设计。
- 动画。
- 页面路由。
- 截图收件箱。
- OCR 界面。
- 词条库。
- 复习界面。

---

# 6. 第四阶段：将 Godot 项目加入根解决方案

## 6.1 加入根解决方案

将 Godot 自动生成的 `.csproj` 加入：

```text
D:\UGit\EnglishLearningProject\GameLexicon.sln
```

使用实际生成的文件名，例如：

```powershell
dotnet sln "D:\UGit\EnglishLearningProject\GameLexicon.sln" add `
  "D:\UGit\EnglishLearningProject\english-learning-project\<实际文件名>.csproj"
```

不得猜测文件名。

## 6.2 配置 Godot 项目引用

Godot 项目可以引用：

```text
GameLexicon.Domain
GameLexicon.Application
GameLexicon.Infrastructure
```

引用方向：

```text
Godot UI
  ├─ Application
  ├─ Domain
  └─ Infrastructure
```

禁止：

```text
Domain → Godot
Application → Godot
Infrastructure → Godot
```

## 6.3 循环依赖检查

必须确认：

- Domain 无生产项目引用。
- Application 只引用 Domain。
- Infrastructure 引用 Application 和 Domain。
- Godot 项目可以引用三层项目。
- 不存在循环引用。

## 6.4 解决方案项目数量

M0-T01 完成后根解决方案有 7 个项目。

M0-T02 完成后应有 8 个项目：

```text
1. GameLexicon.Domain
2. GameLexicon.Application
3. GameLexicon.Infrastructure
4. GameLexicon.CaptureBridge
5. GameLexicon.Domain.Tests
6. GameLexicon.Application.Tests
7. GameLexicon.Infrastructure.Tests
8. Godot C# 项目
```

---

# 7. 本任务明确不做

M0-T02 不实现：

- 正式导航系统。
- ViewModel 框架。
- 日志系统。
- 配置系统。
- SQLite。
- 数据库迁移。
- CaptureBridge 功能。
- 全局快捷键。
- Windows Graphics Capture。
- OCR。
- Tesseract。
- TTS。
- 词条编辑。
- 词条库。
- 复习系统。
- 导入导出。
- 云服务。
- 在线词典。
- LLM 功能。
- Godot 安装目录修改。
- Steam 更新配置修改。

---

# 8. 验证要求

## 8.1 构建 Godot C# 项目

优先使用 Godot 控制台程序：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --editor `
  --quit
```

如该命令不能触发 C# 解决方案构建，应根据 `--help` 输出使用 Godot 4.7.1 实际支持的等价构建参数。

必要时执行：

```powershell
dotnet build `
  "D:\UGit\EnglishLearningProject\english-learning-project\<实际文件名>.csproj"
```

要求：

- Godot C# 项目构建成功。
- `AppRoot.cs` 编译成功。
- 不存在 GodotSharp SDK 缺失错误。

## 8.2 构建根解决方案

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet restore GameLexicon.sln
dotnet build GameLexicon.sln --no-restore
```

要求：

- 构建成功。
- 记录警告数。
- 记录错误数。
- 不允许忽略失败项目。

## 8.3 运行测试

执行：

```powershell
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 现有测试全部通过。
- 不得跳过失败测试。
- 记录每个测试项目结果。

## 8.4 Godot headless 验证

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --editor `
  --quit
```

要求：

- Godot 能加载项目。
- C# 脚本可解析。
- 主场景可加载。
- 无脚本编译错误。
- 无场景资源错误。

如果命令参数不适用于该版本：

1. 查看 `--help`。
2. 使用等价的最小 headless 验证命令。
3. 记录最终实际命令。
4. 不通过删除脚本或场景规避错误。

## 8.5 Godot 运行验证

使用明确的 .NET 主程序运行工程：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

确认：

- 应用窗口可以启动。
- 主场景正确加载。
- 左侧占位区可见。
- 主内容占位区可见。
- 输出中出现：

  ```text
  GameLexicon AppRoot initialized.
  ```

- 没有 C# 异常。
- 没有资源加载错误。

如 Codex 无法观察 GUI：

- 完成命令行和 headless 验证。
- 将 GUI 运行列为用户人工验收项。
- 不虚假声称已经看到窗口。

## 8.6 验证 Steam 启动兼容文件

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe" --version
```

确认该文件当前也是 Godot 4.7.1 .NET/mono 版本。

本任务不修改 Steam 设置，也不验证 Steam 客户端更新行为。

## 8.7 Git 验证

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff
git diff --check
```

确认：

- 未创建第二个 `project.godot`。
- 未移动 `english-learning-project/`。
- 未修改 Godot 安装目录。
- `.godot/`、`bin/` 和 `obj/` 被忽略。
- `icon.svg.import` 没有被错误忽略。
- `project.godot` 修改仅限必要配置。
- 变更范围只属于 M0-T02。

---

# 9. 自动化验收清单

- [ ] `.NET` 主程序存在
- [ ] `.NET` 控制台程序存在
- [ ] Steam 启动兼容程序存在
- [ ] `GodotSharp/` 存在且非空
- [ ] Godot 版本确认为 4.7.1
- [ ] 主程序确认为 .NET/C# 版本
- [ ] Steam 启动兼容程序确认为 .NET/C# 版本
- [ ] `.NET SDK` 架构为 x64
- [ ] Godot 与 `.NET SDK` 架构一致
- [ ] 现有 Godot 工程已生成 `.csproj`
- [ ] `.csproj` 由 Godot 生成，不是手工伪造
- [ ] 已记录 Godot 项目 TargetFramework
- [ ] 已创建 `scripts/AppRoot.cs`
- [ ] 已创建 `scenes/App.tscn`
- [ ] `App.tscn` 已设置为主场景
- [ ] Godot 项目已加入根解决方案
- [ ] 根解决方案包含 8 个项目
- [ ] 项目引用方向正确
- [ ] 不存在循环引用
- [ ] Godot C# 项目构建成功
- [ ] 根解决方案构建成功
- [ ] 全部现有测试通过
- [ ] Godot headless 验证通过
- [ ] 未创建第二个 Godot 工程
- [ ] 未修改 Godot 安装目录
- [ ] 未提前实现后续功能

---

# 10. 人工验收清单

- [ ] `english-learning-project/` 路径未变化
- [ ] 仓库中仍只有一个 `project.godot`
- [ ] Godot 安装目录未被 Codex 修改
- [ ] 根解决方案仍位于仓库根目录
- [ ] Godot 主场景能在 .NET 编辑器中打开
- [ ] 应用启动后显示基础窗口
- [ ] 左侧占位区域可见
- [ ] 主内容占位区域可见
- [ ] Godot 输出中出现 AppRoot 初始化消息
- [ ] 没有 C# 编译错误
- [ ] 没有场景资源错误
- [ ] 没有同时使用标准版和 .NET 版编辑该项目
- [ ] 没有实现 OCR、SQLite、截图、TTS、词条或复习功能
- [ ] `docs/IMPLEMENTATION_STATUS.md` 与实际结果一致

---

# 11. 完成后的文档更新

M0-T02 完成后，更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

必须记录：

- Task ID：`M0-T02`
- 名称
- 状态：`Done`
- 开始时间
- 完成时间
- Godot 安装目录：

  ```text
  E:\SteamLibrary\steamapps\common\Godot Engine
  ```

- Godot 主程序路径：

  ```text
  E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe
  ```

- Steam 启动兼容程序路径：

  ```text
  E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe
  ```

- Godot 完整版本输出
- `GodotSharp/` 验证结果
- 是否确认为 .NET 版
- Godot 项目的 `.csproj` 文件名
- Godot 项目的 `TargetFramework`
- 已安装的 `.NET SDK`
- 创建的文件
- 修改的文件
- 执行的命令
- Godot C# 项目构建结果
- 根解决方案构建结果
- 测试结果
- Godot headless 验证结果
- Godot GUI 人工验收状态
- Git diff 概况
- 已知限制

将下一任务设置为：

```text
M0-T03：实现基础导航
```

状态：

```text
Not Started
```

不得自动执行 M0-T03。

---

# 12. Codex 最终报告格式

最终报告必须包含：

```markdown
## 任务结果

- Task ID:
- 名称:
- 状态:

## 环境验证

- Godot 安装目录:
- Godot 主程序:
- Steam 启动兼容程序:
- Godot 版本:
- Godot .NET 支持:
- GodotSharp:
- .NET SDK:
- 系统架构:

## Godot 项目

- 生成的 .csproj:
- TargetFramework:
- 主场景:
- 主脚本:

## 创建的文件

- ...

## 修改的文件

- ...

## 项目引用

```text
...
```

## 执行的命令

```text
...
```

## 构建结果

```text
...
```

## 测试结果

```text
...
```

## Godot 验证结果

- C# 项目构建:
- Headless:
- GUI 人工验收:

## Git diff 概况

```text
...
```

## 人工验收

- ...

## 警告和已知限制

- ...

## 下一任务

- M0-T03
- 不自动执行
```

---

# 13. Codex 可直接执行的总指令

请执行：

```text
M0-T02：初始化现有 Godot 4.7.1 .NET/C# 工程与基础主场景
```

固定路径：

```text
仓库根目录：
D:\UGit\EnglishLearningProject

Godot 工程目录：
D:\UGit\EnglishLearningProject\english-learning-project

Godot .NET 安装目录：
E:\SteamLibrary\steamapps\common\Godot Engine

Godot .NET 主程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe

Godot .NET 控制台程序：
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe

Steam 启动兼容程序：
E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe

根解决方案：
D:\UGit\EnglishLearningProject\GameLexicon.sln
```

严格按照本文件的阶段、强制停止条件、任务边界和验收要求执行。

本轮只完成 M0-T02：

- 不执行 M0-T03。
- 不实现后续业务功能。
- 不修改 Godot 安装目录。
- 不修改 Steam 设置。
