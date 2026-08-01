# GameLexicon 实施状态

更新日期：2026-08-01

> 本文件记录当前仓库状态、已完成任务、正在等待执行的任务、环境限制与后续前置条件。  
> Codex 每次开始工作前必须先阅读：
>
> - `AGENTS.md`
> - `docs/PRODUCT_SPEC.md`
> - `docs/IMPLEMENTATION_STATUS.md`
>
> 每次只执行一个任务。未完成当前任务的构建、测试、人工验收和状态更新前，不得进入下一任务。

---

## 1. 仓库基线

### 1.1 仓库根目录

```text
D:\UGit\EnglishLearningProject
```

### 1.2 现有 Godot 工程目录

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

约束：

- `english-learning-project/` 是当前已有的 Godot 工程目录。
- 未经明确任务授权，不得移动或重命名该目录。
- 不得在 Godot 编辑器安装目录中创建项目文件。
- 不得创建第二个重复的 Godot 工程。
- 当前 Git 仓库根目录为外层 `EnglishLearningProject/`。

---

## 2. 最近完成任务

### PREP-01：项目现状检查与实施文档初始化

- Task ID：`PREP-01`
- 名称：项目现状检查与实施文档初始化
- 状态：`Done`
- 完成日期：2026-08-01

### 2.1 已完成内容

- 已确认 Git 仓库根目录。
- 已确认现有 Godot 工程目录。
- 已检查仓库目录结构。
- 已检查是否存在嵌套 Git 仓库。
- 已记录当前 Godot 工程状态。
- 已记录本机可用的 `.NET SDK` 与 Git 版本。
- 已创建或确认以下项目文档：
  - `AGENTS.md`
  - `docs/PRODUCT_SPEC.md`
  - `docs/IMPLEMENTATION_STATUS.md`
- 未安装第三方依赖。
- 未实现任何业务功能。
- 未移动或重命名现有 Godot 工程。

### 2.2 自动化测试

不适用。

本任务仅用于环境检查和文档初始化，当前仓库尚无可构建的解决方案和测试项目。

### 2.3 人工验收

已完成：

- [x] 确认仓库根目录为 `D:\UGit\EnglishLearningProject`
- [x] 确认 Godot 工程位于 `english-learning-project/`
- [x] 确认未发现嵌套 `.git` 仓库
- [x] 确认当前工程尚无 `.sln`
- [x] 确认当前工程尚无 `.csproj`
- [x] 确认当前工程尚无 C# 脚本
- [x] 确认当前工程尚无 GDScript 脚本
- [x] 记录 `.NET SDK` 版本
- [x] 记录 Git 版本
- [x] 记录 Godot 工程声明的版本特性和渲染配置

### 2.4 已知限制

- Godot 命令当前不在 `PATH`。
- 尚未通过命令行确认 Godot 编辑器的精确版本。
- 尚未确认当前使用的 Godot 编辑器是否为要求的 Godot 4.7.1 .NET 版本。
- 当前运行进程名为 `godot.windows.opt.tools.64`，但尚未确认其完整可执行路径和文件版本。
- 当前 Godot 工程尚未生成 `.csproj`。
- 当前 Godot 工程尚未生成 `.sln`。
- 当前 Godot 工程尚未初始化 C# 脚本环境。
- 当前仓库尚无可构建的根解决方案。
- 当前仓库尚无自动化测试项目。

### 2.5 后续任务

```text
M0-T01：创建解决方案与基础分层项目
```

---

## 3. 当前任务

### M0-T01：创建解决方案与基础分层项目

- Task ID：`M0-T01`
- 名称：创建解决方案与基础分层项目
- 状态：`Not Started`
- 开始时间：未开始
- 负责人：Codex
- 前置任务：`PREP-01`
- 阻塞状态：无
- 是否允许执行后续任务：否

### 3.1 本任务目标

在仓库根目录创建 GameLexicon 的基础 C# 解决方案结构，但不初始化 Godot C# 工程，不实现任何业务功能。

应创建：

```text
GameLexicon.sln

src/
├─ GameLexicon.Domain/
├─ GameLexicon.Application/
└─ GameLexicon.Infrastructure/

tools/
└─ GameLexicon.CaptureBridge/

tests/
├─ GameLexicon.Domain.Tests/
├─ GameLexicon.Application.Tests/
└─ GameLexicon.Infrastructure.Tests/
```

### 3.2 预计涉及文件

```text
GameLexicon.sln

src/GameLexicon.Domain/
├─ GameLexicon.Domain.csproj
└─ ...

src/GameLexicon.Application/
├─ GameLexicon.Application.csproj
└─ ...

src/GameLexicon.Infrastructure/
├─ GameLexicon.Infrastructure.csproj
└─ ...

tools/GameLexicon.CaptureBridge/
├─ GameLexicon.CaptureBridge.csproj
└─ ...

tests/GameLexicon.Domain.Tests/
├─ GameLexicon.Domain.Tests.csproj
└─ ...

tests/GameLexicon.Application.Tests/
├─ GameLexicon.Application.Tests.csproj
└─ ...

tests/GameLexicon.Infrastructure.Tests/
├─ GameLexicon.Infrastructure.Tests.csproj
└─ ...

docs/IMPLEMENTATION_STATUS.md
```

实际文件应在任务开始前根据仓库现状再次确认。

### 3.3 依赖

- 可用的 `.NET SDK`
- 可用的 Git
- 已存在并可读取的：
  - `AGENTS.md`
  - `docs/PRODUCT_SPEC.md`
  - `docs/IMPLEMENTATION_STATUS.md`

本任务不依赖：

- Godot C# 工程文件
- Tesseract
- SQLite
- Windows Graphics Capture
- 系统 TTS
- 第三方词典服务

### 3.4 项目引用规则

生产项目引用方向应为：

```text
GameLexicon.Domain
    ↑
GameLexicon.Application
    ↑
GameLexicon.Infrastructure
```

具体规则：

- `GameLexicon.Domain`
  - 不引用任何其他生产项目。
  - 不引用 Godot。
  - 不引用 SQLite。
  - 不引用 Windows API。
  - 不引用 OCR 实现。

- `GameLexicon.Application`
  - 可以引用 `GameLexicon.Domain`。
  - 不直接引用 Godot UI。
  - 不直接引用 Windows API。
  - 不直接引用具体 SQLite 实现。

- `GameLexicon.Infrastructure`
  - 可以引用 `GameLexicon.Application`。
  - 可以引用 `GameLexicon.Domain`。
  - 后续负责 SQLite、OCR、配置、导出等实现。
  - 本任务不添加实际基础设施依赖。

- `GameLexicon.CaptureBridge`
  - 本任务只创建基础项目。
  - 不实现全局快捷键。
  - 不实现截图。
  - 如无明确需要，不添加对其他生产项目的引用。

- 测试项目
  - 只引用各自需要测试的项目。
  - 不引用无关生产项目。

### 3.5 执行计划

1. 阅读：
   - `AGENTS.md`
   - `docs/PRODUCT_SPEC.md`
   - `docs/IMPLEMENTATION_STATUS.md`
2. 检查当前 Git 状态。
3. 确认仓库根目录。
4. 确认现有 Godot 工程仍位于 `english-learning-project/`。
5. 创建根解决方案 `GameLexicon.sln`。
6. 创建 Domain、Application、Infrastructure 项目。
7. 创建 CaptureBridge 项目。
8. 创建三个测试项目。
9. 配置项目引用。
10. 添加至少一个最小 smoke test。
11. 运行解决方案构建。
12. 运行全部当前测试。
13. 检查项目引用方向。
14. 检查 Git diff。
15. 更新本文件。
16. 将当前任务状态更新为 `Done`。
17. 将下一任务设置为 `M0-T02`。
18. 不自动执行 `M0-T02`。

### 3.6 本任务明确不做

- 不创建第二个 Godot 工程。
- 不移动 `english-learning-project/`。
- 不重命名 `english-learning-project/`。
- 不手工创建 Godot `.csproj`。
- 不将当前 Godot 工程加入根解决方案。
- 不创建 Godot 场景。
- 不创建 Godot C# 脚本。
- 不实现导航 UI。
- 不实现日志系统。
- 不实现配置系统。
- 不实现 SQLite。
- 不实现数据库迁移。
- 不实现截图。
- 不实现 CaptureBridge 平台功能。
- 不实现 OCR。
- 不实现 TTS。
- 不实现词条。
- 不实现复习。
- 不添加本任务不需要的生产依赖。
- 不修改 Godot 安装目录。
- 不删除用户已有文件。

### 3.7 自动化验收

必须完成：

- [ ] `dotnet build GameLexicon.sln` 成功
- [ ] 全部当前测试通过
- [ ] 至少存在一个通过的 smoke test
- [ ] Domain 项目无平台依赖
- [ ] 项目引用方向符合本节规则
- [ ] 不存在循环项目引用
- [ ] 未创建第二个 Godot 工程
- [ ] 未移动现有 Godot 工程
- [ ] 未实现后续里程碑功能

### 3.8 人工验收

必须确认：

- [ ] `GameLexicon.sln` 位于仓库根目录
- [ ] `src/`、`tools/`、`tests/` 位于仓库根目录
- [ ] `english-learning-project/` 路径未变化
- [ ] 现有 `project.godot` 未被不必要修改
- [ ] 未将生成缓存提交到 Git
- [ ] `.gitignore` 能排除 `.godot/`、`bin/` 和 `obj/`
- [ ] `docs/IMPLEMENTATION_STATUS.md` 与实际实现一致
- [ ] Codex 最终报告包含：
  - 创建文件
  - 修改文件
  - 执行命令
  - 构建结果
  - 测试结果
  - 人工验收结果
  - 已知限制
  - 建议的下一任务

---

## 4. 下一任务

### M0-T02：初始化 Godot .NET/C# 工程与基础主场景

- Task ID：`M0-T02`
- 名称：初始化 Godot .NET/C# 工程与基础主场景
- 状态：`Blocked`
- 阻塞原因：尚未确认 Godot 4.7.1 .NET 编辑器路径和 C# 工程生成能力
- 前置任务：
  - `M0-T01` 完成
  - Godot .NET 环境验证完成

### 4.1 M0-T02 前置条件

开始 M0-T02 前，必须完成：

- [ ] 确认 Godot 可执行文件的完整路径
- [ ] 确认精确版本为 Godot 4.7.1
- [ ] 确认使用支持 C# 的 Godot .NET 版本
- [ ] 确认 Godot 与 `.NET SDK` 均为 64 位
- [ ] 能使用指定 Godot 编辑器打开：
  - `english-learning-project/project.godot`
- [ ] 能在该工程中创建第一个 C# 脚本
- [ ] 能由 Godot 自动生成 `.csproj`
- [ ] 能使用 `.NET SDK` 构建 Godot 自动生成的 `.csproj`
- [ ] 确认 Godot 自动生成的目标框架
- [ ] 确认 Godot C# 项目可以加入根解决方案

### 4.2 M0-T02 预期结果

M0-T02 预计负责：

- 验证 Godot 4.7.1 .NET。
- 初始化现有 Godot 工程的 C# 项目文件。
- 保留 Godot 自动生成的 `.csproj` 配置。
- 创建基础 `App.tscn`。
- 创建基础 `AppRoot.cs`。
- 将 Godot C# 项目加入 `GameLexicon.sln`。
- 配置 Godot 项目对 Application、Domain 和 Infrastructure 的引用。
- 验证 Godot 工程可编译、可打开、可运行。

不得在 M0-T01 中提前实施以上内容。

---

## 5. 当前实际目录结构

```text
D:\UGit\EnglishLearningProject\
├─ .git\
├─ .gitignore
├─ AGENTS.md
├─ docs\
│  ├─ PRODUCT_SPEC.md
│  └─ IMPLEMENTATION_STATUS.md
└─ english-learning-project\
   ├─ .editorconfig
   ├─ .gitattributes
   ├─ .gitignore
   ├─ .godot\
   │  ├─ editor\
   │  ├─ imported\
   │  └─ shader_cache\
   ├─ icon.svg
   ├─ icon.svg.import
   └─ project.godot
```

说明：

- Git 仓库根目录为：

```text
D:\UGit\EnglishLearningProject
```

- 现有 Godot 工程位于：

```text
english-learning-project/
```

- 未发现嵌套 `.git` 仓库。
- 当前没有根解决方案。
- 当前没有普通 C# 类库项目。
- 当前没有测试项目。
- 当前没有场景目录。
- 当前没有脚本目录。
- 当前没有插件目录。
- 当前 Godot 工程只有初始资源和配置文件。

---

## 6. Godot 工程状态

### 6.1 工程信息

- 工程文件：

```text
english-learning-project/project.godot
```

- 工程名称：

```text
EnglishLearningProject
```

- 配置格式：

```text
config_version=5
```

- 声明的 Godot 特性：

```text
4.7
Mobile
```

- Windows 渲染驱动：

```text
d3d12
```

- 渲染方式：

```text
mobile
```

- 3D 物理引擎：

```text
Jolt Physics
```

### 6.2 脚本与项目文件状态

| 项目 | 当前状态 |
|---|---|
| Godot `.csproj` | 尚未生成 |
| 根 `.sln` | 尚未生成 |
| Godot C# 脚本 | 尚未创建 |
| GDScript 脚本 | 尚未创建 |
| 场景目录 | 尚未创建 |
| 脚本目录 | 尚未创建 |
| 插件目录 | 尚未创建 |

### 6.3 工程类型结论

当前是一个尚未添加脚本的标准 Godot 工程。

更准确的当前结论是：

- 当前工程尚未初始化 Godot C# 项目文件。
- 当前工程没有足够信息证明已成功配置为 Godot .NET/C# 工程。
- 没有 `.csproj` 并不能单独证明安装的是非 .NET 版 Godot。
- 需要使用指定的 Godot 4.7.1 .NET 编辑器创建或加载第一个 C# 脚本，才能完成 C# 工程环境验证。
- Godot C# 初始化应在 `M0-T02` 中完成，不应在 `M0-T01` 中手工模拟。

---

## 7. 本机工具状态

| 工具 | 当前状态 |
|---|---|
| Godot | 命令不在 `PATH` |
| Godot 精确版本 | 尚未确认 |
| Godot .NET 支持 | 尚未确认 |
| 当前观察到的进程名 | `godot.windows.opt.tools.64` |
| Godot 可执行文件路径 | 尚未确认 |
| `.NET SDK` | `10.0.301` |
| Git | `2.55.0.windows.3` |

补充说明：

- 已检测到 `.NET SDK 10.0.301`。
- 当前尚未通过实际 Godot C# 项目构建验证兼容性。
- 在 Godot 自动生成 `.csproj` 前，不应自行决定 Godot 项目的目标框架。
- 不应手工把 Godot 项目目标框架固定为 `net10.0`。
- M0-T02 应优先保留 Godot 自动生成的目标框架和项目设置。
- M0-T01 创建的普通 C# 项目应选择能被当前 SDK 构建、并能在后续与 Godot 工程兼容的保守目标框架；实际选择必须由 Codex 在任务开始时说明。

---

## 8. Git 与资源文件规则

### 8.1 应忽略

以下内容属于生成缓存、构建输出或用户本地数据，不应提交：

```text
.godot/
bin/
obj/
logs/
capture_inbox/
screenshots/
*.db
*.db-shm
*.db-wal
.local/
```

实际规则以根目录 `.gitignore` 为准。

### 8.2 应提交

以下内容属于项目源文件或资源导入配置，应提交：

```text
project.godot
icon.svg
icon.svg.import
AGENTS.md
docs/
*.sln
*.csproj
*.cs
*.tscn
```

特别说明：

- `.godot/` 是 Godot 生成的缓存目录，不应提交。
- `icon.svg` 是项目源资源，应提交。
- `icon.svg.import` 保存资源导入配置，应提交。
- 根目录 `.gitignore` 不应使用会排除全部 `*.import` 文件的规则。

### 8.3 当前 Git 约束

- 不覆盖用户已有未提交修改。
- 不删除现有文件。
- 不创建嵌套 Git 仓库。
- 不提交：
  - 用户数据库
  - 用户截图
  - OCR 文本缓存
  - 日志
  - API 密钥
  - 本机 Godot 路径
  - 构建输出
  - Godot 缓存
- 每个任务完成后必须检查 Git diff。

---

## 9. 项目准备约束

### 9.1 技术基线

- Godot：4.7.1 .NET
- 主要语言：C#
- MVP 平台：Windows 10/11 x64
- 架构：
  - Godot 桌面应用
  - Windows CaptureBridge
  - 本地 Tesseract OCR
  - SQLite
- 默认离线优先

### 9.2 安全约束

- 不向游戏进程注入代码。
- 不读取游戏进程内存。
- 不实现反作弊绕过。
- 不绕过 DRM。
- 不实现受保护内容捕获绕过。
- 不在日志中记录用户截图和完整 OCR 文本。
- 不将 API Key 提交到 Git。

### 9.3 架构约束

- UI 不直接执行 SQL。
- UI 不直接启动 OCR 子进程。
- UI 不直接调用 Win32。
- Domain 不依赖 Godot。
- Domain 不依赖 SQLite。
- Domain 不依赖 Windows API。
- 外部能力通过接口隔离。
- 每个任务只实现当前范围。
- 不提前实现后续里程碑。

### 9.4 当前阶段明确禁止

当前处于 Milestone 0 初期。

在 M0-T01 完成前，不得实现：

- OCR
- SQLite 业务表
- 截图
- 全局快捷键
- CaptureBridge 平台逻辑
- TTS
- 词条编辑
- 复习
- 导入导出
- 云服务
- 在线词典
- LLM 功能

---

## 10. 风险与待确认事项

### 10.1 Godot 编辑器版本

状态：待确认

需要确认：

- Godot 可执行文件完整路径。
- 精确版本是否为 4.7.1。
- 是否为 .NET 版本。
- 是否能创建 C# 脚本。
- 是否能自动生成 `.csproj`。

处理阶段：

```text
M0-T02 前
```

### 10.2 Godot 目标框架

状态：待确认

规则：

- 不在 Godot `.csproj` 生成前猜测目标框架。
- 不手工创建 Godot `.csproj`。
- 不手工固定为 `net10.0`。
- 以 Godot 自动生成结果为准。
- 再调整普通 C# 项目以保证兼容。

处理阶段：

```text
M0-T02
```

### 10.3 根 `.gitignore`

状态：需在 M0-T01 中检查

重点确认：

- 忽略 `.godot/`
- 忽略 `bin/`
- 忽略 `obj/`
- 不忽略 `icon.svg.import`
- 不忽略 `project.godot`
- 不忽略 `.csproj`
- 不忽略 `.sln`
- 不忽略 `docs/`

### 10.4 Godot 工程目录名称

状态：保留现状

当前名称：

```text
english-learning-project/
```

规则：

- 现阶段不移动。
- 现阶段不重命名。
- 如后续需要迁移到 `app/GameLexicon.Godot/`，必须创建独立任务并先获得明确授权。

---

## 11. 当前任务完成后的更新要求

M0-T01 完成后，Codex 必须更新本文件：

1. 将：

```text
M0-T01 状态：Not Started
```

改为：

```text
M0-T01 状态：Done
```

2. 记录：

- 开始时间
- 完成时间
- 创建文件
- 修改文件
- 执行命令
- 构建结果
- 测试结果
- 人工验收结果
- 已知限制

3. 将 `M0-T01` 移入“最近完成任务”。

4. 将当前任务更新为：

```text
M0-T02：初始化 Godot .NET/C# 工程与基础主场景
```

5. 如果 Godot 4.7.1 .NET 尚未验证，则：

```text
M0-T02 状态：Blocked
```

6. 不得自动执行 M0-T02。

---

## 12. Codex 当前执行摘要

当前允许执行的任务只有：

```text
M0-T01：创建解决方案与基础分层项目
```

当前不允许执行：

```text
M0-T02 及后续任务
```

开始 M0-T01 前，Codex 必须：

1. 阅读必需文档。
2. 检查 Git 状态。
3. 报告预计修改文件。
4. 确认不移动现有 Godot 工程。
5. 确认不初始化 Godot C# 工程。
6. 确认不添加后续功能。

完成 M0-T01 后，Codex 必须：

1. 构建解决方案。
2. 运行全部当前测试。
3. 检查项目引用。
4. 检查 Git diff。
5. 更新本文件。
6. 停止并等待下一条指令。
