# M0-T04 Codex 执行指令

## 任务名称

```text
M0-T04：配置与日志
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M0-T04_CODEX_INSTRUCTION.md
```

本任务只实现：

- JSON 配置。
- 日志目录。
- 日志滚动。
- 开发模式开关。

验收核心：

- 重启应用后配置仍然保留。
- 日志中不出现敏感文本。

本任务不执行 M1-T01，不实现数据库、OCR、截图、TTS 或其他业务功能。

---

# 1. 已确认的前置基线

用户已完成并确认：

```text
M0-T03 commit:
483dfe7206bfa4c8944b87f3bc9dc809253ccabc
```

当前状态：

- Git 工作区干净。
- M0-T03 提交内容完整。
- M0-T03 状态为 `Done`。
- M0-T04 状态为 `Not Started`。
- 根解决方案包含 8 个项目。
- 基线 restore 成功。
- 基线 build 成功，0 警告、0 错误。
- 基线 test 成功，3/3 通过。
- 当前无 Godot 编辑器或残留 Godot 进程。

Codex 仍须在开始时重新核验，不得只依赖本文件中的描述。

---

# 2. 固定路径

## 2.1 仓库根目录

```text
D:\UGit\EnglishLearningProject
```

## 2.2 Godot 工程目录

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

## 2.3 根解决方案

```text
D:\UGit\EnglishLearningProject\GameLexicon.sln
```

## 2.4 Godot 项目

```text
D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj
```

## 2.5 Godot 主场景

```text
D:\UGit\EnglishLearningProject\english-learning-project\scenes\App.tscn
```

## 2.6 Godot 主脚本

```text
D:\UGit\EnglishLearningProject\english-learning-project\scripts\AppRoot.cs
```

## 2.7 设置页面

```text
D:\UGit\EnglishLearningProject\english-learning-project\scenes\settings\SettingsView.tscn
```

## 2.8 Godot .NET 主程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe
```

## 2.9 Godot .NET 控制台程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe
```

---

# 3. 产品规格约束

M0-T04 必须完成：

```text
JSON 配置
日志目录
日志滚动
开发模式开关
```

产品规格中的日志要求：

```text
日志目录：
user://logs/

主程序日志：
gamelexicon-YYYYMMDD.log

CaptureBridge 日志：
capturebridge-YYYYMMDD.log

默认保留：
14 天

单文件最大：
10 MB
```

当前 M0-T04 只实现主程序日志：

```text
gamelexicon-YYYYMMDD.log
```

不得为了满足文件名清单而提前实现 CaptureBridge 日志；CaptureBridge 尚未进入对应里程碑。

默认允许记录的事件类型：

- 应用启动。
- 应用版本。
- 开发模式状态。
- 配置加载与保存结果。
- 配置回退到默认值。
- 日志轮换和旧日志清理结果。
- 应用正常关闭。
- 导航名称等不含用户内容的技术事件。

默认禁止记录：

- 截图像素。
- 完整 OCR 文本。
- 完整释义。
- 用户 API 密钥。
- Token。
- Authorization Header。
- 密码。
- Cookie。
- 用户输入的学习文本。
- 原句、词条、例句或笔记正文。
- 配置对象的完整序列化内容。

开发模式可以增加技术诊断日志，但不能取消上述敏感信息禁令。

---

# 4. 必须阅读

开始前完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M0-T04_CODEX_INSTRUCTION.md
```

如存在以下文件，也必须读取：

```text
docs/AGENT_SYSTEM.md
docs/SKILLS_CATALOG.md
.agents/skills/project-routing/SKILL.md
.agents/skills/godot-workflow/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Infrastructure / Godot composition root

Primary writer:
主协调 Agent

Supporting agents:
- godot_specialist
- milestone_architect
- skill_curator（收尾时按需调用）
```

专业 Agent 默认只读。主协调 Agent是同一工作区的唯一默认写入者。

---

# 5. 阶段 0：重新核验基线

## 5.1 Git 状态

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git log -3 --oneline
git show --stat --oneline 483dfe7206bfa4c8944b87f3bc9dc809253ccabc
git diff --check
```

必须确认：

- 工作区干净。
- 提交 `483dfe7...` 存在。
- 提交包含 M0-T03 的导航代码、六个占位页面和状态文档。
- 没有未确认修改。

如工作区不干净：

1. 立即停止。
2. 列出修改和未跟踪文件。
3. 不恢复、不覆盖、不暂存、不提交。

## 5.2 状态文档

确认：

```text
M0-T03 = Done
M0-T04 = Not Started
```

状态不一致时停止，不自行猜测或覆盖。

## 5.3 Godot 进程

执行：

```powershell
Get-Process -ErrorAction SilentlyContinue |
  Where-Object { $_.ProcessName -match "godot" } |
  Select-Object ProcessName, Id, Path, MainWindowTitle
```

如 Godot 正在打开当前项目：

1. 停止。
2. 要求用户保存并关闭编辑器。
3. 不结束用途不明的进程。

## 5.4 解决方案与目标框架

执行：

```powershell
dotnet sln "D:\UGit\EnglishLearningProject\GameLexicon.sln" list

Select-String `
  -Path "D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj" `
  -Pattern "Project Sdk|TargetFramework"
```

必须确认：

- 根解决方案仍为 8 个项目。
- Godot SDK 为 `Godot.NET.Sdk/4.7.1`。
- Godot TargetFramework 为 `net8.0`。
- 生产类库目标框架仍为 `net8.0`。

不得在 M0-T04 修改目标框架或项目引用结构。

## 5.5 基线构建与测试

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet restore GameLexicon.sln
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

基线失败时停止，不把既有故障混入 M0-T04。

---

# 6. 实现边界与架构

配置和日志属于：

```text
GameLexicon.Infrastructure
```

接口、设置模型和日志抽象属于：

```text
GameLexicon.Application
```

Godot 工程只负责组合和 UI：

```text
AppRoot
→ 初始化 AppServices
→ AppServices 创建 Infrastructure 实现
→ SettingsView 使用设置服务
```

依赖方向保持：

```text
Godot UI
├─ Application
└─ Infrastructure

Infrastructure
└─ Application

Application
└─ Domain（现有引用保持不变）
```

禁止：

```text
Application → Infrastructure
Domain → Infrastructure
Infrastructure → Godot
```

不新增第三方 NuGet 包。优先使用：

```text
System.Text.Json
System.IO
TimeProvider
```

如 Codex认为必须引入第三方日志包，必须停止并报告理由，不得自行添加。

---

# 7. 配置设计

## 7.1 配置文件位置

逻辑路径：

```text
user://config/settings.json
```

物理路径由 Godot 在运行时解析，不得硬编码 Windows 用户目录。

推荐组合方式：

```csharp
string userDataPath = ProjectSettings.GlobalizePath("user://");
```

然后由 Infrastructure 使用：

```text
<userDataPath>/config/settings.json
```

运行时代码不得硬编码：

```text
C:\Users\<name>\...
D:\UGit\...
E:\SteamLibrary\...
```

## 7.2 设置模型

建议创建：

```text
src/GameLexicon.Application/Configuration/AppSettings.cs
src/GameLexicon.Application/Configuration/LoggingSettings.cs
```

最小模型：

```csharp
public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool DevelopmentMode { get; set; }
    public LoggingSettings Logging { get; set; } = new();
}

public sealed class LoggingSettings
{
    public int RetentionDays { get; set; } = 14;
    public int MaxFileSizeMb { get; set; } = 10;
}
```

JSON 建议使用 `snake_case`：

```json
{
  "schema_version": 1,
  "development_mode": false,
  "logging": {
    "retention_days": 14,
    "max_file_size_mb": 10
  }
}
```

要求：

- 默认 `DevelopmentMode = false`。
- 默认保留 14 天。
- 默认单文件最大 10 MB。
- 模型可向后扩展。
- 当前不得提前加入 OCR、截图、TTS、数据库等完整设置集合。
- 不把 API Key 放入普通 JSON 设置文件。
- 配置类不得依赖 Godot。

## 7.3 设置服务接口

建议创建：

```text
src/GameLexicon.Application/Abstractions/IAppSettingsService.cs
```

最小职责：

```csharp
public interface IAppSettingsService
{
    AppSettings Current { get; }
    AppSettings Load();
    void Save(AppSettings settings);
}
```

可使用等价 API，但必须：

- 能加载默认设置。
- 能保存。
- 保存后更新 `Current`。
- 可在单元测试中使用临时目录。
- 不依赖 Godot API。

## 7.4 JSON 实现

建议创建：

```text
src/GameLexicon.Infrastructure/Configuration/JsonAppSettingsService.cs
```

要求：

1. 首次启动：
   - 创建 `config/`。
   - 创建默认 `settings.json`。
   - 返回默认设置。

2. 正常启动：
   - 读取 JSON。
   - 验证范围。
   - 返回并保存为 `Current`。

3. 保存：
   - 先写临时文件。
   - 完成后原子替换或安全移动为正式文件。
   - 不留下半写入 JSON。
   - JSON 使用 UTF-8。
   - 格式化输出，便于人工查看。

4. 无效范围：
   - `RetentionDays` 小于 1 或过大时回退到安全范围。
   - `MaxFileSizeMb` 小于 1 或过大时回退到安全范围。
   - 建议范围：
     - `RetentionDays`: 1–365
     - `MaxFileSizeMb`: 1–1024

5. 损坏 JSON：
   - 不崩溃。
   - 将损坏文件重命名为带时间戳的备份，例如：
     `settings.corrupt-YYYYMMDD-HHmmss.json`
   - 使用默认配置重新创建正式文件。
   - 返回可理解的加载结果或允许启动层记录警告。
   - 不把损坏 JSON 原文写入日志。

6. 未知字段：
   - 应允许忽略，保证未来兼容。
   - 当前不实现复杂迁移系统。

---

# 8. 日志抽象

## 8.1 日志级别

建议创建：

```text
src/GameLexicon.Application/Logging/AppLogLevel.cs
```

例如：

```csharp
public enum AppLogLevel
{
    Debug,
    Information,
    Warning,
    Error
}
```

## 8.2 日志接口

建议创建：

```text
src/GameLexicon.Application/Abstractions/IAppLogger.cs
```

最小 API：

```csharp
public interface IAppLogger : IDisposable
{
    bool DevelopmentMode { get; }
    void SetDevelopmentMode(bool enabled);

    void Debug(string category, string eventName, string message);
    void Information(string category, string eventName, string message);
    void Warning(string category, string eventName, string message);
    void Error(string category, string eventName, string message, Exception? exception = null);
}
```

可采用等价 API，但必须：

- 生产代码不依赖具体文件日志器。
- 默认关闭 Debug 级别。
- 开发模式开启后允许 Debug。
- 运行时可切换开发模式。
- 支持安全关闭和释放文件资源。
- 不要求当前实现复杂结构化日志框架。

---

# 9. 滚动文件日志

## 9.1 建议文件

```text
src/GameLexicon.Infrastructure/Logging/RollingFileLogger.cs
src/GameLexicon.Infrastructure/Logging/RollingFileLoggerOptions.cs
src/GameLexicon.Infrastructure/Logging/SensitiveDataRedactor.cs
```

## 9.2 日志目录与文件名

逻辑目录：

```text
user://logs/
```

主文件：

```text
gamelexicon-YYYYMMDD.log
```

当日文件达到最大大小后，使用序号滚动，例如：

```text
gamelexicon-20260801.log
gamelexicon-20260801.1.log
gamelexicon-20260801.2.log
```

要求：

- 默认最大单文件 10 MB。
- 单元测试可以注入更小字节数，不得在测试中写满真实 10 MB。
- 新的一天自动使用新日期文件。
- 写入使用 UTF-8。
- 每条记录一行。
- 多线程写入必须避免行内容互相穿插。
- 日志器可被安全 Dispose。

建议格式：

```text
2026-08-01T11:08:00.123Z [Information] App/Startup Application started.
```

不得在日志中序列化整个设置对象。

## 9.3 保留策略

默认保留：

```text
14 天
```

要求：

- 初始化日志器或启动时清理过期日志。
- 只清理符合本应用日志命名规则的文件。
- 不删除日志目录中的任意陌生文件。
- 保留天数使用配置值。
- 清理失败不应导致应用无法启动。
- 清理失败只能记录安全的错误摘要。

## 9.4 敏感信息保护

`SensitiveDataRedactor` 至少处理常见键值形式：

```text
api_key=...
apikey=...
token=...
access_token=...
authorization=...
password=...
cookie=...
secret=...
```

替换为：

```text
<redacted>
```

要求：

- 大小写不敏感。
- 覆盖 `:` 与 `=` 常见形式。
- 不把原始异常对象的全部 `Data` 或请求内容写入日志。
- 异常日志默认记录：
  - 异常类型。
  - 安全化后的异常消息。
- 默认不记录完整堆栈到文件。
- 开发模式可记录堆栈摘要，但仍需经过敏感信息清理。
- 不声称通用正则可以识别所有 OCR 或学习文本。
- 防止学习文本泄露主要依靠调用约束：业务代码不得把正文传给日志器。

## 9.5 开发模式

默认：

```text
false
```

关闭时：

- 不写 Debug 日志。
- 写 Information、Warning、Error。
- 不写用户学习正文。

开启时：

- 允许 Debug 技术日志。
- 仍不允许截图、OCR 全文、释义、API Key、Token、密码等。
- UI 必须明确提示：
  “开发模式会记录更多技术诊断信息，但不应记录学习文本或密钥。”

---

# 10. AppServices 与启动顺序

建议创建：

```text
english-learning-project/scripts/AppServices.cs
```

M0-T04 阶段只包含：

```csharp
public static IAppSettingsService SettingsService { get; }
public static IAppLogger Logger { get; }
```

或等价的只读属性。

初始化顺序必须是：

```text
1. 解析 user:// 物理目录。
2. 创建并加载 JSON 配置。
3. 使用配置初始化日志器。
4. 写入安全的启动日志。
5. 初始化 M0-T03 导航。
```

关闭顺序：

```text
1. 写入安全的正常关闭日志。
2. Flush/Dispose 日志器。
3. 清理静态状态，避免编辑器重复运行时持有旧资源。
```

建议由 `AppRoot.cs`：

- 在 `_Ready()` 中初始化 AppServices。
- 初始化成功后再创建导航。
- 在 `_ExitTree()` 中关闭 AppServices。
- 捕获配置或日志初始化错误。
- 使用 `GD.PushError` 显示安全摘要。
- 不把完整 JSON、用户路径或敏感信息写入 Godot 输出。

必须保留 M0-T03：

- 六个路由。
- 默认 Dashboard。
- 页面缓存。
- 按钮互斥选中。
- 不重新创建 AppRoot。

---

# 11. 设置页面中的开发模式开关

更新：

```text
english-learning-project/scenes/settings/SettingsView.tscn
```

建议新增：

```text
SettingsView
└─ Content
   ├─ Title
   ├─ PlaceholderMessage（可保留或更新）
   ├─ DevelopmentSection (VBoxContainer)
   │  ├─ DevelopmentModeCheckBox (CheckButton)
   │  ├─ DevelopmentModeDescription (Label)
   │  └─ SaveStatusLabel (Label)
```

建议创建：

```text
english-learning-project/scripts/UI/SettingsView.cs
```

要求：

1. 打开设置页面时：
   - 从 `AppServices.SettingsService.Current` 读取值。
   - 正确显示开发模式开关。
   - 不因绑定初始值触发重复保存。

2. 用户切换时：
   - 更新 `DevelopmentMode`。
   - 立即保存 JSON。
   - 立即调用日志器切换 Debug 级别。
   - 显示“设置已保存”。
   - 记录安全事件：
     `Development mode changed: enabled/disabled`
   - 不记录完整设置 JSON。

3. 描述文本必须提醒：
   - 会记录更多技术诊断信息。
   - 不应记录学习文本或密钥。

4. 本任务不实现其他设置分组。

5. 设置页面仍属于 M0-T04 的最小可用 UI，不实现完整设置中心。

---

# 12. 允许创建和修改的文件

建议创建：

```text
src/GameLexicon.Application/Configuration/AppSettings.cs
src/GameLexicon.Application/Configuration/LoggingSettings.cs
src/GameLexicon.Application/Logging/AppLogLevel.cs
src/GameLexicon.Application/Abstractions/IAppSettingsService.cs
src/GameLexicon.Application/Abstractions/IAppLogger.cs

src/GameLexicon.Infrastructure/Configuration/JsonAppSettingsService.cs
src/GameLexicon.Infrastructure/Logging/RollingFileLogger.cs
src/GameLexicon.Infrastructure/Logging/RollingFileLoggerOptions.cs
src/GameLexicon.Infrastructure/Logging/SensitiveDataRedactor.cs

english-learning-project/scripts/AppServices.cs
english-learning-project/scripts/UI/SettingsView.cs

tests/GameLexicon.Infrastructure.Tests/Configuration/JsonAppSettingsServiceTests.cs
tests/GameLexicon.Infrastructure.Tests/Logging/RollingFileLoggerTests.cs
tests/GameLexicon.Infrastructure.Tests/Logging/SensitiveDataRedactorTests.cs
```

Godot 自动生成的 `.uid` 可以保留。

允许修改：

```text
english-learning-project/scripts/AppRoot.cs
english-learning-project/scenes/settings/SettingsView.tscn
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/ENVIRONMENT.md（仅当环境事实变化）
docs/SKILLS_CATALOG.md（仅当 Skill 影响审查要求）
docs/SKILL_CHANGELOG.md（仅当 Skill 实际变化）
.agents/skills/*/SKILL.md（仅当可复用工作流变化）
```

测试文件名和目录可以根据现有命名规范小幅调整。

不得修改：

- Godot 安装目录。
- Steam 设置。
- 项目目标框架。
- 根解决方案项目数量。
- 项目引用方向。
- M0-T03 导航架构，除非是启动顺序所必需的最小修改。
- `project.godot`，除非 Godot 自动产生与脚本 UID 有关的必要变化；出现时必须解释。

---

# 13. 自动化测试

## 13.1 JSON 配置测试

至少覆盖：

1. 文件不存在：
   - 返回默认值。
   - 创建配置目录。
   - 创建 `settings.json`。

2. 保存和重新加载：
   - 开发模式从 false 改为 true。
   - 新服务实例重新加载后仍为 true。

3. JSON 可读性：
   - 包含 `schema_version`。
   - 包含 `development_mode`。
   - UTF-8。
   - 格式化输出。

4. 损坏 JSON：
   - 不崩溃。
   - 损坏文件被保留为 `.corrupt-*`。
   - 默认文件重新创建。
   - 不把损坏原文写入日志或错误输出。

5. 范围验证：
   - 日志保留天数回到安全范围。
   - 最大文件大小回到安全范围。

6. 临时写入：
   - 保存结束后没有遗留 `.tmp` 文件。

测试必须使用独立临时目录，并在结束时清理。

## 13.2 日志滚动测试

至少覆盖：

1. 创建日志目录。
2. 文件名符合：
   `gamelexicon-YYYYMMDD.log`
3. 超过测试注入的小尺寸上限后生成 `.1.log`。
4. 日志行不会互相拼接。
5. Dispose 后文件可被重新打开或删除。
6. 过期日志会被清理。
7. 不符合命名规则的文件不会被删除。
8. 清理失败不会导致日志器初始化失败。

## 13.3 开发模式测试

至少覆盖：

1. 默认关闭：
   - Debug 不写入。
   - Information 写入。

2. 开启：
   - Debug 写入。

3. 运行时关闭：
   - 后续 Debug 不再写入。

4. 开发模式变化不修改：
   - 保留天数。
   - 最大文件大小。
   - SchemaVersion。

## 13.4 敏感信息测试

使用明显的测试哨兵，不使用真实密钥：

```text
api_key=TEST_SECRET_123
Authorization: Bearer TEST_TOKEN_456
password=TEST_PASSWORD_789
```

要求：

- 日志中不出现：
  - `TEST_SECRET_123`
  - `TEST_TOKEN_456`
  - `TEST_PASSWORD_789`
- 日志中出现：
  - `<redacted>`

增加一个学习文本哨兵：

```text
LEARNING_TEXT_MUST_NOT_BE_LOGGED
```

规则：

- 不通过“万能识别器”猜测学习文本。
- 生产代码和测试调用约束必须保证该文本不传入常规日志。
- 实际运行日志中不得出现该哨兵。

---

# 14. 构建与自动验证

## 14.1 构建 Infrastructure 测试目标

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj
dotnet test tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj --no-build
```

## 14.2 构建 Godot 项目

执行：

```powershell
dotnet build `
  "D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj"
```

要求：

- 0 错误。
- 记录警告数。

## 14.3 根解决方案

执行：

```powershell
dotnet restore GameLexicon.sln
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目全部构建成功。
- 所有测试通过。
- 新增测试必须计入总数。
- 不删除或跳过测试换取通过。

## 14.4 Godot Headless 构建

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --editor `
  --build-solutions `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit
```

## 14.5 Godot Headless 启动

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit-after 3
```

如参数不匹配，读取 `--help` 并使用等价参数。

要求：

- 配置目录初始化成功。
- 日志目录初始化成功。
- 导航仍正常初始化。
- 无脚本错误。
- 无资源错误。
- 无未处理异常。

安全输出可以包括：

```text
GameLexicon services initialized.
GameLexicon AppRoot initialized.
Navigated to: Dashboard
GameLexicon navigation initialized.
```

不得把设置 JSON 或物理用户目录完整输出到日志。

---

# 15. 运行时文件自动验收

Codex 必须解析实际 Godot `user://` 物理路径，但不得把该机器路径硬编码进源码。

检查实际生成文件：

```text
user://config/settings.json
user://logs/gamelexicon-YYYYMMDD.log
```

自动确认：

1. `settings.json` 存在。
2. JSON 可以反序列化。
3. 默认开发模式为 false。
4. 日志保留为 14 天。
5. 日志最大文件为 10 MB。
6. `logs/` 存在。
7. 当日日志存在。
8. 日志包含安全的启动事件。
9. 日志不包含：
   - 测试密钥哨兵。
   - 学习文本哨兵。
   - 完整设置 JSON。
10. Git 状态不包含运行时用户数据文件。

运行时配置和日志不得写入仓库目录，也不得提交 Git。

---

# 16. GUI 人工验收

自动验收完成后，状态必须为：

```text
Awaiting Manual Verification
```

运行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

## 16.1 导航回归

确认：

- 应用正常启动。
- 默认显示首页。
- 六个导航按钮仍可切换。
- Sidebar、RouteHost 和缓存页面行为未回归。
- 无 C# 或资源错误。

## 16.2 开发模式开启

1. 打开“设置”。
2. 找到“开发模式”开关。
3. 确认默认关闭。
4. 打开开发模式。
5. 确认界面显示“设置已保存”。
6. 关闭应用。

## 16.3 第一次重启

重新运行应用：

1. 打开“设置”。
2. 确认开发模式仍为开启。
3. 确认没有异常。
4. 将开发模式关闭。
5. 确认显示“设置已保存”。
6. 关闭应用。

## 16.4 第二次重启

再次运行：

1. 打开“设置”。
2. 确认开发模式保持关闭。
3. 确认六个导航页面仍可使用。
4. 关闭应用。

## 16.5 日志人工检查

由 Codex 或用户打开实际日志文件，确认：

- 有启动和正常关闭事件。
- 有开发模式 enabled/disabled 事件。
- 没有 API Key、Token、密码。
- 没有 OCR 文本、原句、词条、释义或笔记正文。
- 没有完整配置 JSON。
- 日志格式可读。
- 未出现明显重复刷屏。

GUI 人工验收通过前不得把 M0-T04 标记为 Done。

---

# 17. 本任务明确不做

不得实现：

- SQLite。
- 数据库迁移。
- Repository。
- CaptureBridge。
- CaptureBridge 日志实际写入。
- 全局快捷键。
- Windows Graphics Capture。
- OCR。
- Tesseract。
- TTS。
- 在线 Provider。
- API Key 存储。
- 系统凭证保险库。
- 完整设置中心。
- 截图、OCR、发音、复习、数据、隐私设置分组。
- 导入导出。
- 备份。
- 正式错误中心。
- 日志查看器 UI。
- 日志上传。
- 云端日志。
- M1-T01。

---

# 18. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且变更未确认。
- 找不到 M0-T03 提交。
- M0-T03 未标记 Done。
- Godot 正在打开当前工程。
- 基线构建或测试失败。
- 根解决方案不再是 8 个项目。
- TargetFramework 或 Godot SDK 不符合基线。
- 必须修改目标框架或项目引用方向。
- 必须新增第三方 NuGet 日志包。
- 必须修改 Godot 安装目录或 Steam 设置。
- 配置文件只能写入仓库目录才能工作。
- 日志滚动会删除不属于本应用的文件。
- 测试需要写入真实用户配置目录。
- 实现需要提前引入 SQLite、OCR 或 CaptureBridge。
- 发现来源不明的用户修改可能被覆盖。

停止后不得：

- 自动恢复用户文件。
- `git reset --hard`。
- `git clean -fd`。
- 自动提交。
- 自动执行 M1-T01。

---

# 19. Git 检查

完成代码和自动验证后执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
```

确认：

- 仅包含 M0-T04 范围内文件。
- `user://` 对应物理运行时目录不在仓库中。
- `.godot/`、`bin/`、`obj/` 未进入 Git。
- 没有真实日志文件或设置文件进入 Git。
- 没有 API Key 或测试秘密进入 Git。
- 没有目标框架和引用结构变化。
- 未创建 Git 提交。

建议额外搜索：

```powershell
git grep -n -I -E `
  "(api[_-]?key|access[_-]?token|authorization|password|cookie|secret)" `
  -- .
```

必须结合语义判断；接口、测试哨兵和脱敏规则中的关键词本身不是泄露。

---

# 20. 状态与文档收尾

GUI 验收前更新：

```text
M0-T04 = Awaiting Manual Verification
```

不得标记 Done。

GUI 验收全部通过后更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

记录：

- Task ID：M0-T04
- 名称：配置与日志
- 状态：Done
- 开始和完成时间
- 配置逻辑路径
- 日志逻辑路径
- JSON SchemaVersion
- 开发模式默认值
- 日志保留天数
- 单文件最大大小
- 滚动命名方式
- 敏感信息保护方式
- 创建与修改文件
- 测试结果
- Godot 构建与 Headless 结果
- 重启持久化人工验收
- 日志人工检查
- Git diff 概况
- 已知限制

下一任务：

```text
M1-T01：SQLite 连接和迁移
```

状态：

```text
Not Started
```

不得自动执行 M1-T01。

更新：

```text
docs/AGENT_HANDOFF.md
```

只有实际环境事实变化时才更新：

```text
docs/ENVIRONMENT.md
```

配置与日志是产品实现事实，不应把运行时用户目录误写为固定机器环境路径。

---

# 21. Skill Impact Review

任务结束后应用：

```text
skill-maintenance
```

报告：

- Primary domain
- Primary agent
- Supporting agents
- Skills used
- Skill update required
- Skills updated
- Documentation updated
- Restart required

正常情况下：

```text
Skill update required: No
```

仅在以下可复用工作流发生变化时更新 Skill：

- Godot user data 路径使用规范。
- 配置测试标准。
- 日志敏感信息审查标准。
- Headless 验证流程。
- Agent 路由规则。
- 里程碑验收模板。

普通配置类、日志器实现或 Settings UI 变化不构成 Skill 更新理由。

---

# 22. 自动验收清单

- [ ] M0-T03 提交存在
- [ ] 初始工作区干净
- [ ] M0-T03 为 Done
- [ ] M0-T04 为 Not Started
- [ ] 基线构建成功
- [ ] 基线测试全部通过
- [ ] AppSettings 存在
- [ ] LoggingSettings 存在
- [ ] IAppSettingsService 存在
- [ ] IAppLogger 存在
- [ ] JSON 配置实现位于 Infrastructure
- [ ] 日志实现位于 Infrastructure
- [ ] 配置类不依赖 Godot
- [ ] 日志类不依赖 Godot
- [ ] 首次启动创建默认 JSON
- [ ] 保存采用安全临时文件策略
- [ ] 损坏 JSON 不导致崩溃
- [ ] 损坏 JSON 有备份
- [ ] 默认开发模式关闭
- [ ] 开发模式可持久化
- [ ] 开发模式可运行时切换日志级别
- [ ] 创建 user://logs/
- [ ] 日志名符合 gamelexicon-YYYYMMDD.log
- [ ] 默认保留 14 天
- [ ] 单文件最大 10 MB
- [ ] 超限后按序号滚动
- [ ] 过期日志可清理
- [ ] 陌生文件不会被清理
- [ ] Debug 默认不写入
- [ ] 开发模式开启后 Debug 写入
- [ ] 常见密钥值被脱敏
- [ ] 实际日志无学习文本
- [ ] Infrastructure 测试通过
- [ ] Godot 项目构建通过
- [ ] 根解决方案构建通过
- [ ] 所有测试通过
- [ ] Headless 构建通过
- [ ] Headless 启动通过
- [ ] M0-T03 导航回归通过
- [ ] git diff --check 通过
- [ ] 未执行 M1-T01
- [ ] Skill Impact Review 完成

---

# 23. 人工验收清单

- [ ] 应用正常启动
- [ ] 首页和六个导航页面仍正常
- [ ] 设置页显示开发模式开关
- [ ] 开发模式默认关闭
- [ ] 打开开关后提示已保存
- [ ] 第一次重启后仍为开启
- [ ] 关闭开关后提示已保存
- [ ] 第二次重启后仍为关闭
- [ ] settings.json 存在且可读
- [ ] logs 目录存在
- [ ] 当日日志存在
- [ ] 日志有启动事件
- [ ] 日志有正常关闭事件
- [ ] 日志有开发模式变化事件
- [ ] 日志无 API Key
- [ ] 日志无 Token
- [ ] 日志无密码
- [ ] 日志无 OCR 全文
- [ ] 日志无词条、原句、释义或笔记正文
- [ ] 日志无完整设置 JSON
- [ ] 无 C# 异常
- [ ] 无资源加载错误
- [ ] 无明显日志刷屏
- [ ] 未提前实现数据库或其他业务

---

# 24. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M0-T04
- 名称: 配置与日志
- 状态:
- 是否执行 M1-T01: No
- Git commit created: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M0-T03 commit:
- Initial Git status:
- Solution projects:
- Baseline build:
- Baseline tests:

## 配置实现

- Logical path:
- Schema version:
- Default development mode:
- Default retention days:
- Default max file size:
- Atomic save:
- Corrupt JSON handling:

## 日志实现

- Logical directory:
- File pattern:
- Size rotation:
- Retention cleanup:
- Development mode behavior:
- Sensitive-data policy:
- Redaction coverage:

## 创建的文件

- ...

## 修改的文件

- ...

## 自动化测试

- Total:
- Passed:
- Failed:
- Skipped:
- Configuration tests:
- Rolling tests:
- Sensitive-data tests:

## 构建结果

- Infrastructure tests project:
- Godot project:
- Root solution:
- Warnings:
- Errors:

## Godot 验证

- Headless build:
- Headless launch:
- Navigation regression:
- Runtime settings file:
- Runtime log file:
- GUI manual verification:

## 敏感信息验收

- Test sentinels absent:
- Learning-text sentinel absent:
- Complete JSON absent:
- Manual log review:

## Git diff

```text
...
```

## Skill Impact Review

- Skills used:
- Update required:
- Skills updated:
- Documentation updated:
- Restart required:

## 人工验收

- ...

## 已知限制

- CaptureBridge logging is not implemented yet.
- Full settings center is not implemented.
- No log viewer UI.
- ...

## 下一任务

- M1-T01：SQLite 连接和迁移
- Status: Not Started
- Not automatically executed
```

---

# 25. 可直接执行的总指令

请执行：

```text
M0-T04：配置与日志
```

严格按照：

```text
docs/MT_INSTRUCTION/M0-T04_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `483dfe7206bfa4c8944b87f3bc9dc809253ccabc`。
2. 开始时工作区必须干净。
3. 配置和日志实现放在 Infrastructure。
4. 接口和设置模型放在 Application。
5. Godot 只负责组合和开发模式 UI。
6. 配置逻辑路径使用 `user://config/settings.json`。
7. 日志逻辑路径使用 `user://logs/`。
8. 默认日志保留 14 天。
9. 单文件最大 10 MB。
10. 默认开发模式关闭。
11. 开发模式不能解除敏感信息禁令。
12. 不新增第三方 NuGet 包。
13. 不实现 SQLite、OCR、CaptureBridge 或 TTS。
14. 不执行 M1-T01。
15. 不创建 Git 提交。
16. 自动验收完成后等待 GUI 人工验收。
17. GUI 验收通过后才将 M0-T04 标记为 Done。
18. 完成后执行 Git diff 和 Skill Impact Review。
