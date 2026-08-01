# M1-T01 Codex 执行指令

## 任务名称

```text
M1-T01：SQLite 连接和迁移
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T01_CODEX_INSTRUCTION.md
```

本任务只实现 SQLite 连接、迁移基础设施和首版数据库结构。

本任务不实现：

- 文本规范化。
- Repository。
- 词条增删改查。
- 手工添加词条 UI。
- 搜索。
- OCR。
- 截图。
- 复习。
- M1-T02。

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
65f846f164a0bbce33d30dae021a06cc4a9bb0cb
```

当前已知状态：

- Git 工作区干净。
- M0-T04 提交内容完整。
- M0-T04 为 `Done`。
- M1-T01 为 `Not Started`。
- 当前无 Godot 编辑器或残留 Godot 进程。
- 根解决方案包含 8 个项目。
- Godot、Application、Domain、Infrastructure 为 `net8.0`。
- 测试项目和 CaptureBridge 维持既有 `net10.0` 基线。
- Restore 成功。
- Build 成功，0 错误。
- Test 成功，21/21 通过。
- 当前存在因 NuGet 漏洞数据源不可达产生的 `NU1900` 警告。

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. 产品规格目标

M1-T01 必须完成：

```text
SqliteConnectionFactory
MigrationRunner
Migration001_Initial
```

验收：

```text
首次启动建库
第二次启动不重复迁移
测试数据库可以删除
```

数据库逻辑路径：

```text
user://data/gamelexicon.db
```

迁移流程：

1. 打开数据库。
2. 创建 `schema_migrations`。
3. 按版本升序执行未应用迁移。
4. 每个迁移独立事务。
5. 迁移失败时回滚并停止启动。
6. 不允许跳过失败迁移。

数据库约束：

- 启用外键。
- MVP 启用 WAL。
- 时间使用 UTC ISO 8601。
- GUID 后续统一保存为小写字符串。
- 所有写操作后续使用事务。

---

# 3. 固定路径

## 3.1 仓库根目录

```text
D:\UGit\EnglishLearningProject
```

## 3.2 Godot 工程目录

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

## 3.3 根解决方案

```text
D:\UGit\EnglishLearningProject\GameLexicon.sln
```

## 3.4 Godot 项目

```text
D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj
```

## 3.5 Godot 主脚本

```text
D:\UGit\EnglishLearningProject\english-learning-project\scripts\AppRoot.cs
```

## 3.6 AppServices

```text
D:\UGit\EnglishLearningProject\english-learning-project\scripts\AppServices.cs
```

## 3.7 Infrastructure 项目

```text
D:\UGit\EnglishLearningProject\src\GameLexicon.Infrastructure\GameLexicon.Infrastructure.csproj
```

## 3.8 Infrastructure 测试项目

```text
D:\UGit\EnglishLearningProject\tests\GameLexicon.Infrastructure.Tests\GameLexicon.Infrastructure.Tests.csproj
```

## 3.9 Godot .NET 主程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe
```

## 3.10 Godot .NET 控制台程序

```text
E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe
```

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
docs/MT_INSTRUCTION/M1-T01_CODEX_INSTRUCTION.md
```

重点读取 `PRODUCT_SPEC.md` 中：

```text
SQLite 数据模型
数据库约束
迁移
日志
错误处理
推荐的首批 Codex 任务
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
Infrastructure / Persistence

Primary writer:
主协调 Agent

Supporting agents:
- godot_specialist：只读审查 Godot user:// 与启动集成
- milestone_architect：只读审查任务范围和数据模型
- skill_curator：仅在收尾 Skill 影响审查需要时调用
```

主协调 Agent 是同一工作区唯一默认写入者。

---

# 5. 阶段 0：重新核验基线

## 5.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git log -3 --oneline
git show --stat --oneline 65f846f164a0bbce33d30dae021a06cc4a9bb0cb
git diff --check
```

必须确认：

- 工作区干净。
- 提交存在。
- 提交包含 M0-T04 配置、日志、测试、Godot UI 和状态文档。
- 没有未确认的用户修改。

工作区不干净时立即停止，不恢复、不覆盖、不暂存、不提交。

## 5.2 状态

确认：

```text
M0-T04 = Done
M1-T01 = Not Started
```

状态不一致时停止。

## 5.3 Godot 进程

执行：

```powershell
Get-Process -ErrorAction SilentlyContinue |
  Where-Object { $_.ProcessName -match "godot" } |
  Select-Object ProcessName, Id, Path, MainWindowTitle
```

如 Godot 正在打开当前项目：

- 停止修改。
- 要求用户保存并关闭。
- 不直接结束用途不明的进程。

## 5.4 解决方案和框架

执行：

```powershell
dotnet sln "D:\UGit\EnglishLearningProject\GameLexicon.sln" list

Get-ChildItem `
  "D:\UGit\EnglishLearningProject" `
  -Recurse `
  -Filter *.csproj |
  ForEach-Object {
    Select-String -Path $_.FullName -Pattern "<TargetFramework>" |
      ForEach-Object {
        [PSCustomObject]@{
          Project = $_.Path
          TargetFramework = $_.Line.Trim()
        }
      }
  }
```

确认：

- 解决方案仍有 8 个项目。
- Godot、Domain、Application、Infrastructure 为 `net8.0`。
- 测试和 CaptureBridge 保持既有框架。
- 本任务不统一或修改所有目标框架。

## 5.5 基线构建

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet restore GameLexicon.sln
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

允许记录已知 `NU1900`，但：

- 不得关闭 NuGet Audit。
- 不得全局抑制 `NU1900`。
- 不得把网络警告误报为代码错误。
- 若包恢复实际失败，则停止。

---

# 6. SQLite Provider 选择

使用官方轻量 ADO.NET Provider：

```text
Microsoft.Data.Sqlite
```

只添加到：

```text
GameLexicon.Infrastructure
```

不得添加：

- Entity Framework Core。
- Dapper。
- sqlite-net。
- System.Data.SQLite。
- 其他 ORM。
- 重复 SQLite Provider。

## 6.1 版本选择规则

由于 Infrastructure 为 `net8.0`：

1. 查询当前 NuGet 可用版本。
2. 选择最新稳定的 `8.0.x`。
3. 不使用 Preview。
4. 不因系统安装了 .NET 10 而选择只面向 .NET 10 的主版本。
5. 在最终报告记录精确版本。
6. 将精确版本写入 `.csproj`，不使用浮动版本。

可执行：

```powershell
dotnet package search Microsoft.Data.Sqlite `
  --exact-match `
  --source https://api.nuget.org/v3/index.json
```

然后：

```powershell
dotnet add `
  "D:\UGit\EnglishLearningProject\src\GameLexicon.Infrastructure\GameLexicon.Infrastructure.csproj" `
  package Microsoft.Data.Sqlite `
  --version <实际选定的最新稳定8.0.x>
```

当前官方 NuGet 已存在兼容 `net8.0` 的 `8.0.x` 版本；执行时仍须以 NuGet 实际结果为准。

## 6.2 网络停止条件

出现以下情况时停止：

- 无法访问 NuGet package source。
- 无法确定实际包版本。
- Package restore 失败。
- 包安装需要修改目标框架。
- 只能使用 Preview。
- 发生包降级或不可解决的依赖冲突。

不得通过：

- 禁用 NuGet 安全检查。
- 手工复制 DLL。
- 从未知来源下载原生 SQLite DLL。
- 把包文件提交仓库。

---

# 7. 目录结构

建议创建：

```text
src/GameLexicon.Infrastructure/
└─ Persistence/
   ├─ SqliteConnectionFactory.cs
   ├─ DatabaseOptions.cs
   ├─ DatabaseInitializer.cs（可选）
   └─ Migrations/
      ├─ IDatabaseMigration.cs
      ├─ MigrationRunner.cs
      └─ Migration001_Initial.cs

tests/GameLexicon.Infrastructure.Tests/
└─ Persistence/
   ├─ SqliteConnectionFactoryTests.cs
   ├─ MigrationRunnerTests.cs
   └─ Migration001InitialTests.cs
```

实际命名可按既有代码风格小幅调整，但必须保留核心类型：

```text
SqliteConnectionFactory
MigrationRunner
Migration001_Initial
```

---

# 8. DatabaseOptions

建议创建：

```text
src/GameLexicon.Infrastructure/Persistence/DatabaseOptions.cs
```

最小字段：

```csharp
public sealed class DatabaseOptions
{
    public required string DatabasePath { get; init; }
    public bool EnableWriteAheadLogging { get; init; } = true;
    public int BusyTimeoutMilliseconds { get; init; } = 5000;
}
```

要求：

- `DatabasePath` 必须是调用层传入的物理路径。
- Infrastructure 不依赖 Godot。
- 不在 Infrastructure 中调用 `ProjectSettings.GlobalizePath`。
- 测试可以注入临时路径。
- 不允许空路径或目录路径冒充数据库文件。

---

# 9. SqliteConnectionFactory

创建：

```text
src/GameLexicon.Infrastructure/Persistence/SqliteConnectionFactory.cs
```

职责：

1. 保存经过验证的数据库选项。
2. 确保数据库父目录存在。
3. 创建新的 `SqliteConnection`。
4. 打开连接。
5. 每次连接启用外键。
6. 设置合理的 Busy Timeout。
7. 初始化时启用 WAL。
8. 不长期共享一个全局连接。
9. 不把连接暴露给 Godot View。

建议 API：

```csharp
public sealed class SqliteConnectionFactory
{
    public string DatabasePath { get; }

    public Task<SqliteConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default);
}
```

连接字符串要求：

- 使用 `SqliteConnectionStringBuilder`。
- `DataSource` 为实际数据库路径。
- 模式允许读写和创建。
- `ForeignKeys = true`。
- 不包含密钥。
- 不记录完整连接字符串。

打开后确认：

```sql
PRAGMA foreign_keys = ON;
PRAGMA busy_timeout = 5000;
```

WAL：

```sql
PRAGMA journal_mode = WAL;
```

说明：

- `journal_mode` 是数据库级设置，不应对每条业务查询重复执行。
- 在数据库初始化或迁移前确保设置成功。
- 测试使用真实临时文件；不要仅靠 `:memory:` 验证 WAL 和文件删除。

## 9.1 错误信息

打开失败时：

- 抛出清晰异常。
- 日志只记录安全摘要。
- 不记录连接字符串。
- 不记录用户数据内容。
- 可以记录逻辑事件：
  `Database/OpenFailed`
- 物理路径是否写入日志应遵循现有日志安全策略；默认避免输出完整个人目录。

---

# 10. IDatabaseMigration

产品规格示例为：

```csharp
public interface IDatabaseMigration
{
    int Version { get; }
    Task ApplyAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken);
}
```

为确保每条命令明确加入 `MigrationRunner` 创建的事务，允许采用最小扩展：

```csharp
public interface IDatabaseMigration
{
    int Version { get; }

    Task ApplyAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken cancellationToken);
}
```

规则：

- 只能在需要显式事务归属时采用扩展签名。
- 必须在最终报告说明与规格示例的差异及原因。
- 不得让 Migration 自己提交 Runner 的事务。
- Version 必须大于 0。
- Migration 不提供 Down/回滚到旧版本功能。
- 当前不实现迁移降级。

---

# 11. MigrationRunner

创建：

```text
src/GameLexicon.Infrastructure/Persistence/Migrations/MigrationRunner.cs
```

职责：

1. 接收 `SqliteConnectionFactory`。
2. 接收迁移集合。
3. 校验迁移版本。
4. 打开数据库连接。
5. 确保 `schema_migrations` 存在。
6. 读取已应用版本。
7. 按 Version 升序执行未应用迁移。
8. 每个迁移使用独立事务。
9. 同一事务中：
   - 执行迁移。
   - 插入迁移记录。
10. 成功后提交。
11. 失败后回滚并停止。
12. 第二次执行不重复应用版本 1。

建议 API：

```csharp
public sealed class MigrationRunner
{
    public Task<MigrationResult> RunAsync(
        CancellationToken cancellationToken = default);
}
```

`MigrationResult` 可包含：

```csharp
public sealed record MigrationResult(
    int CurrentVersion,
    IReadOnlyList<int> AppliedVersions);
```

也可使用等价结果，但不得返回数据库内容。

## 11.1 版本校验

启动前验证：

- 迁移列表不为空时，版本必须唯一。
- 版本必须大于 0。
- 按升序执行。
- 当前首版必须包含 Version 1。
- 同一版本重复注册时立即失败。
- 数据库存在高于程序最高版本的迁移时停止：
  - 不尝试降级。
  - 不删除迁移记录。
  - 不继续运行应用。

## 11.2 `schema_migrations`

建立：

```sql
CREATE TABLE IF NOT EXISTS schema_migrations (
    version INTEGER PRIMARY KEY,
    applied_at_utc TEXT NOT NULL
);
```

记录时间：

- UTC。
- ISO 8601。
- 推荐使用固定、可排序格式，例如：
  `yyyy-MM-ddTHH:mm:ss.fffffffZ`

不得依赖本地时区。

## 11.3 事务规则

每个迁移独立事务：

```text
Begin transaction
→ Apply migration
→ Insert schema_migrations record
→ Commit
```

失败时：

```text
Rollback
→ 不插入版本记录
→ 停止后续迁移
→ 启动失败
```

不得：

- 捕获异常后继续下一迁移。
- 只记录失败但仍启动主应用。
- 使用 `INSERT OR IGNORE` 隐藏版本冲突。
- 在迁移失败后把数据库标记为已迁移。

---

# 12. Migration001_Initial

创建：

```text
src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs
```

Version：

```text
1
```

首版迁移应建立产品规格中的完整 MVP 表结构，但本任务不实现对应 Repository 或业务逻辑。

## 12.1 表结构

迁移前 `schema_migrations` 已由 Runner 建立。

Migration001 建立：

```sql
CREATE TABLE captures (
    id TEXT PRIMARY KEY,
    captured_at_utc TEXT NOT NULL,
    source_window_title TEXT NOT NULL DEFAULT '',
    source_process_name TEXT NOT NULL DEFAULT '',
    game_title TEXT,
    image_path TEXT NOT NULL,
    pixel_width INTEGER NOT NULL,
    pixel_height INTEGER NOT NULL,
    status INTEGER NOT NULL,
    error_message TEXT
);

CREATE TABLE ocr_regions (
    id TEXT PRIMARY KEY,
    capture_id TEXT NOT NULL,
    x INTEGER NOT NULL,
    y INTEGER NOT NULL,
    width INTEGER NOT NULL,
    height INTEGER NOT NULL,
    raw_text TEXT NOT NULL DEFAULT '',
    corrected_text TEXT NOT NULL DEFAULT '',
    created_at_utc TEXT NOT NULL,
    FOREIGN KEY (capture_id)
        REFERENCES captures(id)
        ON DELETE CASCADE
);

CREATE TABLE ocr_tokens (
    id TEXT PRIMARY KEY,
    ocr_region_id TEXT NOT NULL,
    text TEXT NOT NULL,
    confidence REAL NOT NULL,
    x INTEGER NOT NULL,
    y INTEGER NOT NULL,
    width INTEGER NOT NULL,
    height INTEGER NOT NULL,
    block_index INTEGER NOT NULL,
    paragraph_index INTEGER NOT NULL,
    line_index INTEGER NOT NULL,
    word_index INTEGER NOT NULL,
    FOREIGN KEY (ocr_region_id)
        REFERENCES ocr_regions(id)
        ON DELETE CASCADE
);

CREATE TABLE sentence_examples (
    id TEXT PRIMARY KEY,
    capture_id TEXT NOT NULL,
    ocr_region_id TEXT,
    sentence_text TEXT NOT NULL,
    normalized_sentence TEXT NOT NULL,
    target_start INTEGER NOT NULL,
    target_length INTEGER NOT NULL,
    screenshot_crop_path TEXT NOT NULL DEFAULT '',
    game_title TEXT,
    created_at_utc TEXT NOT NULL,
    FOREIGN KEY (capture_id)
        REFERENCES captures(id)
        ON DELETE RESTRICT,
    FOREIGN KEY (ocr_region_id)
        REFERENCES ocr_regions(id)
        ON DELETE SET NULL
);

CREATE TABLE vocabulary_entries (
    id TEXT PRIMARY KEY,
    headword TEXT NOT NULL,
    normalized_headword TEXT NOT NULL,
    entry_type INTEGER NOT NULL,
    part_of_speech TEXT,
    phonetic TEXT,
    definition_english TEXT,
    translation_chinese TEXT,
    notes TEXT,
    is_archived INTEGER NOT NULL DEFAULT 0,
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL
);

CREATE UNIQUE INDEX ux_vocabulary_entries_normalized_active
ON vocabulary_entries(normalized_headword)
WHERE is_archived = 0;

CREATE TABLE entry_examples (
    entry_id TEXT NOT NULL,
    example_id TEXT NOT NULL,
    is_primary INTEGER NOT NULL DEFAULT 0,
    sort_order INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (entry_id, example_id),
    FOREIGN KEY (entry_id)
        REFERENCES vocabulary_entries(id)
        ON DELETE CASCADE,
    FOREIGN KEY (example_id)
        REFERENCES sentence_examples(id)
        ON DELETE CASCADE
);

CREATE TABLE tags (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    normalized_name TEXT NOT NULL UNIQUE
);

CREATE TABLE entry_tags (
    entry_id TEXT NOT NULL,
    tag_id TEXT NOT NULL,
    PRIMARY KEY (entry_id, tag_id),
    FOREIGN KEY (entry_id)
        REFERENCES vocabulary_entries(id)
        ON DELETE CASCADE,
    FOREIGN KEY (tag_id)
        REFERENCES tags(id)
        ON DELETE CASCADE
);

CREATE TABLE review_cards (
    id TEXT PRIMARY KEY,
    entry_id TEXT NOT NULL,
    card_type INTEGER NOT NULL,
    due_at_utc TEXT NOT NULL,
    repetition INTEGER NOT NULL DEFAULT 0,
    interval_days REAL NOT NULL DEFAULT 0,
    ease_factor REAL NOT NULL DEFAULT 2.5,
    lapse_count INTEGER NOT NULL DEFAULT 0,
    last_reviewed_at_utc TEXT,
    is_suspended INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (entry_id)
        REFERENCES vocabulary_entries(id)
        ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_review_cards_entry_type
ON review_cards(entry_id, card_type);

CREATE INDEX ix_review_cards_due
ON review_cards(is_suspended, due_at_utc);

CREATE TABLE review_logs (
    id TEXT PRIMARY KEY,
    review_card_id TEXT NOT NULL,
    reviewed_at_utc TEXT NOT NULL,
    grade INTEGER NOT NULL,
    previous_interval_days REAL NOT NULL,
    new_interval_days REAL NOT NULL,
    previous_ease_factor REAL NOT NULL,
    new_ease_factor REAL NOT NULL,
    response_milliseconds INTEGER,
    FOREIGN KEY (review_card_id)
        REFERENCES review_cards(id)
        ON DELETE CASCADE
);

CREATE TABLE app_settings (
    key TEXT PRIMARY KEY,
    value_json TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL
);
```

## 12.2 `app_settings` 与 JSON 配置

M0-T04 已建立：

```text
user://config/settings.json
```

因此在 M1-T01：

- 按产品数据库规格创建 `app_settings` 表。
- 不把它接入当前设置服务。
- 不从 JSON 双写到 SQLite。
- 不改变 JSON 配置的权威来源。
- 在文档中标记该表为“预留，当前未使用”。

不得在本任务制造两个配置源之间的同步逻辑。

## 12.3 SQL 执行规则

- 所有建表和建索引命令在 Version 1 事务中执行。
- 可使用单个 SQL batch，前提是 Provider 正确执行全部语句。
- 或逐条执行，便于定位失败。
- 不使用字符串拼接插入用户数据。
- 当前 Migration SQL 没有用户输入。
- 表和索引名称必须与规格一致。
- 不额外创造未批准业务列。
- 如发现规格 SQL 在实际 SQLite 上失败，停止并报告，不静默改动数据模型。

---

# 13. Godot AppServices 集成

数据库逻辑路径：

```text
user://data/gamelexicon.db
```

Godot 负责将逻辑路径转换为物理路径，例如：

```csharp
string databasePath = ProjectSettings.GlobalizePath(
    "user://data/gamelexicon.db");
```

然后传入 Infrastructure。

不得在 Infrastructure 引用 Godot。

## 13.1 启动顺序

M0-T04 当前顺序大致为：

```text
配置
→ 日志
→ 导航
```

M1-T01 调整为：

```text
1. 解析 user:// 路径。
2. 加载配置。
3. 初始化日志。
4. 初始化 SQLite connection factory。
5. 执行 MigrationRunner。
6. 数据库成功后初始化导航。
```

数据库失败时：

- 不继续进入正常主界面。
- 记录安全错误摘要。
- 使用 `GD.PushError` 或现有错误入口。
- 不记录连接字符串。
- 不显示原始 SQL 全文给普通用户。
- 不删除或重建用户数据库。
- 不自动跳过失败迁移。

## 13.2 AppServices 约束

允许更新：

```text
english-learning-project/scripts/AppServices.cs
english-learning-project/scripts/AppRoot.cs
```

建议：

- AppServices 初始化改为异步，或使用等价安全方式。
- MigrationRunner 完成后再宣布 services initialized。
- 不把活跃 `SqliteConnection` 暴露给 View。
- 不创建长期全局共享连接。
- 不在 UI 线程执行明显阻塞的数据库迁移。
- 确保异常被捕获并报告，不产生未观察任务异常。

当前不创建 Repository。

---

# 14. 日志要求

M0-T04 日志已存在。

M1-T01 只记录安全迁移事件：

```text
Database initialization started.
Database migration applied: 1.
Database schema is current: 1.
Database initialization completed.
```

失败时可记录：

```text
Database open failed.
Database migration failed at version 1.
```

禁止记录：

- 完整连接字符串。
- 完整个人用户目录。
- SQL 中未来可能包含的用户数据。
- 数据库行内容。
- OCR 文本。
- 词条和例句。
- API Key、Token 或密码。

可记录：

- 迁移版本号。
- 执行时长。
- 成功/失败。
- 安全异常类型和脱敏消息。

---

# 15. 自动化测试

测试必须使用独立临时目录和真实临时 SQLite 文件。

不得：

- 写入真实 `user://data/`。
- 写入仓库目录。
- 依赖测试执行顺序。
- 留下 `.db`、`-wal`、`-shm` 文件。
- 用真实用户数据库做测试。

## 15.1 SqliteConnectionFactoryTests

至少覆盖：

1. 父目录不存在时自动创建。
2. 打开连接时创建数据库文件。
3. `PRAGMA foreign_keys` 返回 1。
4. Busy Timeout 已设置。
5. WAL 模式在文件数据库中生效。
6. 每次调用返回不同连接实例。
7. Dispose 后连接释放。
8. 数据库文件及 sidecar 可被删除。
9. 空路径或非法路径产生清晰异常。

## 15.2 MigrationRunnerTests

至少覆盖：

1. 首次运行：
   - 创建 `schema_migrations`。
   - 应用 Version 1。
   - 返回 AppliedVersions 包含 1。

2. 第二次运行：
   - 不重复应用 Version 1。
   - `schema_migrations` 中 Version 1 只有一行。
   - AppliedVersions 为空或明确表示无新增迁移。

3. 排序：
   - 输入迁移顺序混乱时仍按版本升序执行。

4. 重复版本：
   - 启动前失败。
   - 不执行任意迁移。

5. 失败回滚：
   - 测试迁移创建表后故意失败。
   - 该表或该次变更被回滚。
   - 失败版本不写入 `schema_migrations`。
   - 后续迁移不执行。

6. 数据库版本过高：
   - 数据库记录高于程序最高版本时停止。
   - 不删除高版本记录。

7. Cancellation：
   - 取消令牌能够中止。
   - 不错误记录为成功迁移。

## 15.3 Migration001InitialTests

至少覆盖：

以下表存在：

```text
schema_migrations
captures
ocr_regions
ocr_tokens
sentence_examples
vocabulary_entries
entry_examples
tags
entry_tags
review_cards
review_logs
app_settings
```

以下索引存在：

```text
ux_vocabulary_entries_normalized_active
ux_review_cards_entry_type
ix_review_cards_due
```

以下约束至少实际验证一项：

- 外键插入无父记录时失败。
- `ON DELETE CASCADE` 生效。
- `ON DELETE RESTRICT` 生效。
- `normalized_name` UNIQUE 生效。
- 活跃词条部分唯一索引生效。

测试不得在 M1-T01 实现 Repository。

## 15.4 Runtime database deletion test

必须验证：

```text
运行迁移
→ Dispose 所有连接
→ 删除 gamelexicon.db
→ 删除可能的 -wal 和 -shm
→ 临时目录删除成功
```

这对应任务验收中的：

```text
测试数据库可删除
```

---

# 16. 包与原生运行时验证

`Microsoft.Data.Sqlite` 会带入 SQLite 原生依赖。

必须验证：

1. Infrastructure 测试项目能够加载 Provider。
2. Godot .NET 项目运行时能够加载 Provider。
3. Headless 启动没有：
   - `DllNotFoundException`
   - `BadImageFormatException`
   - `TypeInitializationException`
   - SQLite provider initialization failure
4. x64 架构匹配。
5. 不手工复制原生 DLL 到 Godot 安装目录。
6. 不修改 Steam Godot 安装目录。
7. 不将 `runtimes/` 构建输出提交 Git。

---

# 17. 构建与自动验证

## 17.1 Package restore

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet restore GameLexicon.sln
```

记录：

- 实际 `Microsoft.Data.Sqlite` 版本。
- Restore 结果。
- NU1900 是否仍为漏洞数据源网络警告。

不得抑制警告。

## 17.2 Infrastructure 测试

执行：

```powershell
dotnet build `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj

dotnet test `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-build
```

## 17.3 Godot 项目

执行：

```powershell
dotnet build `
  "D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj"
```

## 17.4 根解决方案

执行：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功。
- 所有现有与新增测试通过。
- 0 错误。
- `NU1900` 可记录为已知网络警告，但不得隐藏其他警告。

## 17.5 Godot Headless 构建

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --editor `
  --build-solutions `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit
```

## 17.6 Godot Headless 第一次启动

开始前只允许处理测试或本任务明确生成的运行时数据库。

不得删除未知现有用户数据库。

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit-after 5
```

确认：

- `user://data/` 创建。
- `gamelexicon.db` 创建。
- Migration 1 已应用。
- 应用和导航正常初始化。
- 无 Provider 或 SQLite 原生加载错误。

## 17.7 Godot Headless 第二次启动

再次执行同一命令。

确认：

- 不重复执行 Migration 1。
- `schema_migrations` 中 Version 1 仍只有一行。
- 日志显示 schema current 或无新增迁移。
- 应用正常启动。
- 不破坏已有数据库。

## 17.8 实际数据库只读检查

使用测试代码或 Microsoft.Data.Sqlite 的只读检查工具验证：

```sql
SELECT version, applied_at_utc
FROM schema_migrations
ORDER BY version;
```

必须得到：

```text
1 row
version = 1
```

检查表和索引名称。

不得要求系统安装独立 `sqlite3.exe`，也不得为此下载未知二进制。

---

# 18. 运行时数据库安全

运行时数据库：

```text
user://data/gamelexicon.db
```

不得：

- 写入仓库目录。
- 纳入 Git。
- 在测试中删除用户实际数据库。
- 在迁移失败时自动删除。
- 在启动时无条件重建。
- 把数据库内容写入日志。
- 把 `.db`、`-wal`、`-shm` 添加为源文件。

如果执行前已存在 `gamelexicon.db`：

1. 识别它是否由本任务之前的用户操作产生。
2. 不覆盖或删除。
3. 运行迁移前记录文件存在。
4. 如 schema 状态未知或不兼容，停止并报告。
5. 不把未知数据库当作空库处理。

---

# 19. GUI 人工验收

自动验收完成后状态：

```text
Awaiting Manual Verification
```

运行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe" `
  --path "D:\UGit\EnglishLearningProject\english-learning-project"
```

## 19.1 第一次人工启动

确认：

- 应用正常启动。
- 默认 Dashboard 正常。
- 六个导航页面仍可切换。
- 设置和日志功能未回归。
- 没有明显启动卡死。
- 没有 C# 异常。
- 没有 SQLite Provider 错误。
- 没有资源加载错误。

正常关闭。

## 19.2 第二次人工启动

再次运行并确认：

- 应用仍正常启动。
- 没有重复迁移错误。
- 没有“table already exists”错误。
- 导航正常。
- 设置的开发模式持久化仍正常。
- 日志中迁移版本合理。
- 正常关闭。

## 19.3 数据库文件检查

确认逻辑路径对应文件存在：

```text
user://data/gamelexicon.db
```

只检查：

- 文件存在。
- 文件大小非零。
- 未出现在 Git 状态。
- 不打开并人工编辑。

## 19.4 日志检查

确认日志出现安全事件：

```text
Database initialization started.
Database migration applied: 1.
Database initialization completed.
Database schema is current: 1.
```

允许实际措辞不同。

日志不得包含：

- 完整连接字符串。
- 完整数据库内容。
- SQL 中的用户文本。
- API Key、Token、密码。
- OCR 文本或词条内容。

GUI 人工验收通过前不得将 M1-T01 标记为 Done。

---

# 20. 本任务允许创建和修改

建议创建：

```text
src/GameLexicon.Infrastructure/Persistence/DatabaseOptions.cs
src/GameLexicon.Infrastructure/Persistence/SqliteConnectionFactory.cs
src/GameLexicon.Infrastructure/Persistence/DatabaseInitializer.cs（可选）

src/GameLexicon.Infrastructure/Persistence/Migrations/IDatabaseMigration.cs
src/GameLexicon.Infrastructure/Persistence/Migrations/MigrationRunner.cs
src/GameLexicon.Infrastructure/Persistence/Migrations/MigrationResult.cs（可选）
src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/SqliteConnectionFactoryTests.cs
tests/GameLexicon.Infrastructure.Tests/Persistence/MigrationRunnerTests.cs
tests/GameLexicon.Infrastructure.Tests/Persistence/Migration001InitialTests.cs
```

允许修改：

```text
src/GameLexicon.Infrastructure/GameLexicon.Infrastructure.csproj
english-learning-project/scripts/AppServices.cs
english-learning-project/scripts/AppRoot.cs（仅在初始化调用必要时）
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/ENVIRONMENT.md（仅环境事实变化时）
docs/SKILLS_CATALOG.md（仅 Skill 影响审查要求时）
docs/SKILL_CHANGELOG.md（仅 Skill 实际更新时）
.agents/skills/*/SKILL.md（仅可复用工作流变化时）
```

Godot 自动生成的 `.uid` 可以保留。

正常情况下不得修改：

```text
GameLexicon.sln
english-learning-project/project.godot
english-learning-project/EnglishLearningProject.csproj
Domain 项目
Application 项目
CaptureBridge
Godot 场景
```

如果必须修改以上文件，先确认是否属于本任务的最小必要范围；超出时停止报告。

---

# 21. 本任务明确不做

不得实现：

- M1-T02 文本规范化。
- `ITextNormalizer`。
- `EnglishExpressionNormalizer`。
- Vocabulary Repository。
- Review Repository。
- Capture Repository。
- CRUD UseCase。
- 手工添加词条。
- 词条列表。
- 搜索。
- 编辑、归档、删除。
- 数据库设置 UI。
- 数据库查看器。
- 数据库备份。
- 数据库恢复。
- Encryption。
- API Key。
- OCR。
- CaptureBridge。
- TTS。
- 复习算法。
- M1-T02 或任何后续任务。

---

# 22. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且变更未确认。
- 找不到提交 `65f846f...`。
- M0-T04 未标记 Done。
- Godot 正在打开当前工程。
- 基线构建或测试失败。
- 解决方案不再是 8 个项目。
- 目标框架与基线不一致。
- NuGet Provider 无法恢复。
- 只能使用 Preview Provider。
- 出现包降级或架构冲突。
- 必须安装 EF Core 或其他 ORM。
- 必须修改 Godot 安装目录。
- 必须手工复制 SQLite 原生 DLL。
- 必须修改项目引用方向。
- Product Spec SQL 无法在 SQLite 执行。
- 已存在未知或高版本用户数据库。
- 迁移失败但无法完整回滚。
- 第二次启动会重复迁移。
- 测试结束后数据库无法删除。
- 实现需要提前创建 Repository 或业务 UI。
- 发现用户文件可能被覆盖。

停止后不得：

- 删除用户数据库。
- 自动恢复用户文件。
- `git reset --hard`。
- `git clean -fd`。
- 禁用 NuGet Audit。
- 自动提交。
- 自动执行 M1-T02。

---

# 23. Git 检查

完成自动验证后执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
```

确认：

- 只有 M1-T01 范围的代码、测试和文档。
- `.db`、`-wal`、`-shm` 未进入 Git。
- `.godot/`、`bin/`、`obj/`、`runtimes/` 构建输出未进入 Git。
- 没有修改目标框架。
- 没有修改解决方案项目数。
- 没有修改 Godot 安装目录。
- 没有实现 Repository 或业务 UI。
- 未创建 Git 提交。

可额外检查：

```powershell
git status --ignored --short |
  Select-String -Pattern "\.db|\.db-wal|\.db-shm|bin/|obj/|runtimes/"
```

---

# 24. 状态与文档收尾

自动验收完成但 GUI 未确认时：

```text
M1-T01 = Awaiting Manual Verification
```

GUI 验收通过后，更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

记录：

- Task ID：M1-T01
- 名称：SQLite 连接和迁移
- 状态：Done
- 开始和完成时间
- Microsoft.Data.Sqlite 精确版本
- 数据库逻辑路径
- WAL 状态
- Foreign Keys 状态
- Busy Timeout
- 迁移接口形式
- 当前 schema version
- Migration001 表和索引
- 首次启动结果
- 第二次启动结果
- 测试数据库删除结果
- 新增测试数量和总测试数
- Godot Headless 和 GUI 结果
- NuGet `NU1900` 状态
- Git diff 概况
- 已知限制
- `app_settings` 表当前未接入 JSON 配置

下一任务：

```text
M1-T02：文本规范化
```

状态：

```text
Not Started
```

不得自动执行 M1-T02。

更新：

```text
docs/AGENT_HANDOFF.md
```

只有环境事实变化时更新：

```text
docs/ENVIRONMENT.md
```

SQLite 包版本和数据库架构属于实现事实，优先记录在状态或架构文档，不要误写为机器环境要求。

---

# 25. Skill Impact Review

任务结束后应用：

```text
skill-maintenance
```

必须报告：

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

只有以下可复用流程发生变化时才更新 Skill：

- SQLite 临时数据库测试标准。
- 迁移事务标准。
- Godot user:// 数据路径流程。
- NuGet 包选择流程。
- Headless 数据库验证流程。
- Agent 路由或里程碑模板变化。

普通迁移代码和首版 SQL 不自动构成 Skill 更新理由。

---

# 26. 自动验收清单

- [ ] 最新提交存在
- [ ] 初始工作区干净
- [ ] M0-T04 为 Done
- [ ] M1-T01 为 Not Started
- [ ] 无 Godot 进程
- [ ] 基线构建成功
- [ ] 基线 21/21 或更多测试通过
- [ ] 选用稳定 Microsoft.Data.Sqlite 8.0.x
- [ ] 包只添加到 Infrastructure
- [ ] 未添加 ORM
- [ ] SqliteConnectionFactory 存在
- [ ] DatabaseOptions 存在
- [ ] MigrationRunner 存在
- [ ] Migration001_Initial 存在
- [ ] schema_migrations 存在
- [ ] 数据库路径来自 user://data/gamelexicon.db
- [ ] Infrastructure 不依赖 Godot
- [ ] 每次连接启用 Foreign Keys
- [ ] WAL 生效
- [ ] Busy Timeout 生效
- [ ] 首次启动创建数据库
- [ ] 首次启动应用 Migration 1
- [ ] 第二次启动不重复 Migration 1
- [ ] Version 1 记录只有一行
- [ ] 每个迁移独立事务
- [ ] 失败迁移完整回滚
- [ ] 失败版本不写入记录
- [ ] 高于程序版本的数据库被拒绝
- [ ] 首版所有表存在
- [ ] 首版所有索引存在
- [ ] 外键约束实测生效
- [ ] 临时测试数据库及 sidecar 可删除
- [ ] Godot 项目可加载 SQLite 原生 Provider
- [ ] Godot Headless 第一次启动成功
- [ ] Godot Headless 第二次启动成功
- [ ] M0-T03 导航无回归
- [ ] M0-T04 配置和日志无回归
- [ ] 所有自动化测试通过
- [ ] git diff --check 通过
- [ ] 无数据库文件进入 Git
- [ ] 未实现 Repository
- [ ] 未执行 M1-T02
- [ ] Skill Impact Review 完成

---

# 27. 人工验收清单

- [ ] 应用第一次正常启动
- [ ] Dashboard 正常显示
- [ ] 六个导航页面正常
- [ ] 设置和日志功能正常
- [ ] 无 SQLite Provider 错误
- [ ] 无 C# 异常
- [ ] 无资源错误
- [ ] 数据库文件存在且非零
- [ ] 应用正常关闭
- [ ] 应用第二次正常启动
- [ ] 没有重复建表错误
- [ ] 没有重复迁移错误
- [ ] 导航仍正常
- [ ] 开发模式持久化仍正常
- [ ] 日志显示 Migration 1 和 schema current
- [ ] 日志无连接字符串
- [ ] 日志无数据库内容
- [ ] 日志无用户学习文本
- [ ] 当前无残留 Godot 进程
- [ ] Git 状态中无数据库文件
- [ ] 未提前实现 Repository 或业务 UI

---

# 28. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T01
- 名称: SQLite 连接和迁移
- 状态:
- 是否执行 M1-T02: No
- Git commit created: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M0-T04 commit:
- Initial Git status:
- Solution projects:
- Target frameworks:
- Baseline build:
- Baseline tests:
- NU1900:

## SQLite Provider

- Package:
- Exact version:
- Target project:
- Restore result:
- Native runtime result:

## 连接工厂

- Database logical path:
- Connection mode:
- Foreign keys:
- WAL:
- Busy timeout:
- Shared connection policy:

## 迁移实现

- Migration interface:
- Current schema version:
- Applied versions on first run:
- Applied versions on second run:
- Transaction policy:
- Failure rollback:
- Future-version protection:

## Migration001

- Tables:
- Indexes:
- app_settings status:
- Schema deviations:

## 创建的文件

- ...

## 修改的文件

- ...

## 自动化测试

- Baseline total:
- Final total:
- Passed:
- Failed:
- Skipped:
- Connection tests:
- Migration tests:
- Schema tests:
- Database deletion test:

## 构建结果

- Infrastructure:
- Godot:
- Root solution:
- Warnings:
- Errors:

## Godot 验证

- Headless build:
- First headless launch:
- Second headless launch:
- Database created:
- Migration record:
- Navigation regression:
- GUI manual verification:

## 日志与安全

- Migration events:
- Connection string absent:
- Database content absent:
- User text absent:

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

- No repositories or CRUD.
- app_settings table is reserved and not wired to JSON settings.
- No migration downgrade.
- NU1900 may persist while vulnerability source is unreachable.
- ...

## 下一任务

- M1-T02：文本规范化
- Status: Not Started
- Not automatically executed
```

---

# 29. 可直接执行的总指令

请执行：

```text
M1-T01：SQLite 连接和迁移
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T01_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `65f846f164a0bbce33d30dae021a06cc4a9bb0cb`。
2. 开始时 Git 工作区必须干净。
3. 选择最新稳定且兼容 net8.0 的 Microsoft.Data.Sqlite 8.0.x，并记录精确版本。
4. 不添加 EF Core、Dapper 或其他 ORM。
5. 实现 SqliteConnectionFactory。
6. 实现 MigrationRunner。
7. 实现 Migration001_Initial。
8. 数据库逻辑路径为 user://data/gamelexicon.db。
9. 启用 Foreign Keys、WAL 和 Busy Timeout。
10. 每个迁移独立事务。
11. 迁移失败必须回滚并阻止正常启动。
12. 首次启动创建数据库并应用 Version 1。
13. 第二次启动不得重复迁移。
14. 测试数据库和 sidecar 必须可删除。
15. 不删除或覆盖未知用户数据库。
16. 不实现 Repository、CRUD 或业务 UI。
17. 不执行 M1-T02。
18. 不创建 Git 提交。
19. 自动验收完成后等待 GUI 人工验收。
20. GUI 验收通过后才将 M1-T01 标记为 Done。
21. 完成后执行 Git diff、文档更新和 Skill Impact Review。
