# M1-T05 Codex 执行指令

## 任务名称

```text
M1-T05：Migration002 手工例句与检索支持
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T05_CODEX_INSTRUCTION.md
```

本任务只实现：

```text
Migration002_ManualExamplesAndSearchSupport
迁移注册
SQLite schema v1 → v2 升级
sentence_examples.capture_id 可空
OCR 来源数据库约束
M1 查询与关联路径所需的最小索引
Infrastructure 自动化测试
Godot 启动迁移集成验证
```

本任务不实现：

- SQLite Repository。
- Application UseCase。
- Godot 业务 UI。
- 手工词条录入流程。
- 查询 SQL。
- CRUD。
- M1-T06 或任何后续任务。

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
e67f5cf9fc13a2a5220885310765e8653791bea0
```

当前已知状态：

- 当前分支：`main`
- Git 工作区干净。
- M1-T04 提交内容完整。
- M1-T04 为 `Done`。
- M1-T05 为 `Not Started`。
- 当前无 Godot 编辑器或残留 Godot 进程。
- 根解决方案包含 8 个项目。
- 目标框架：
  - Godot 桌面：`net8.0`
  - Godot Android 条件目标：`net9.0`
  - Domain、Application、Infrastructure：`net8.0`
  - 三个测试项目、CaptureBridge：`net10.0`
- 构建成功，0 警告、0 错误。
- 测试成功，205/205 通过：
  - Domain：111
  - Application：61
  - Infrastructure：33
- Migration001 未被 M1-T04 修改。
- Application 公共 API 未泄漏 SQLite、Godot、Infrastructure 或 `IQueryable`。
- 数据库、sidecar、日志、`.godot/`、`bin/`、`obj/` 未进入 Git。
- `user://` 数据库位于仓库外。

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. 背景与目标

## 2.1 当前数据库不兼容手工例句

`Migration001_Initial` 当前创建：

```sql
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
```

但产品要求：

```text
用户可以不经过截图和 OCR，手工创建词条及原句。
```

M1-T03 已建立领域规则：

```csharp
SentenceExample.CaptureId : Guid?
```

合法来源组合：

```text
CaptureId = null, OcrRegionId = null
→ 手工例句，合法

CaptureId != null, OcrRegionId = null
→ 截图例句，合法

CaptureId != null, OcrRegionId != null
→ OCR 例句，合法

CaptureId = null, OcrRegionId != null
→ 非法
```

该决策已记录于：

```text
docs/DECISIONS.md
ADR-007
```

## 2.2 本任务目标

新增 schema version 2：

```text
Migration002_ManualExamplesAndSearchSupport
```

完成：

1. 将 `sentence_examples.capture_id` 改为 nullable。
2. 在数据库层约束：
   `ocr_region_id IS NULL OR capture_id IS NOT NULL`。
3. 无损保留所有 v1 数据。
4. 无损保留 `entry_examples` 链接。
5. 保持现有外键和删除行为。
6. 为 M1-T04 已确定的查询和关联路径增加最小索引。
7. 注册 Migration002。
8. 首次升级执行一次。
9. 后续启动不重复执行。
10. 失败时完整回滚，schema version 不写入 2。

---

# 3. 固定路径

## 3.1 仓库根目录

```text
D:\UGit\EnglishLearningProject
```

## 3.2 根解决方案

```text
D:\UGit\EnglishLearningProject\GameLexicon.sln
```

## 3.3 Infrastructure 项目

```text
D:\UGit\EnglishLearningProject\src\GameLexicon.Infrastructure
```

## 3.4 Migration 目录

```text
D:\UGit\EnglishLearningProject\src\GameLexicon.Infrastructure\Persistence\Migrations
```

## 3.5 Infrastructure 测试

```text
D:\UGit\EnglishLearningProject\tests\GameLexicon.Infrastructure.Tests
```

## 3.6 Godot AppServices

```text
D:\UGit\EnglishLearningProject\english-learning-project\scripts\AppServices.cs
```

实际迁移注册位置必须以当前代码为准。

## 3.7 Godot .NET Console

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
docs/MT_INSTRUCTION/M1-T05_CODEX_INSTRUCTION.md
```

重点阅读：

```text
PRODUCT_SPEC.md
- F07：词条编辑
- F09：词条库
- 第 7 节分层职责
- 第 10 节领域模型
- 第 12 节 SQLite 数据模型
- 第 13 节数据库初始化和迁移
- 数据库约束和迁移规则

DECISIONS.md
- ADR-007：SentenceExample.CaptureId 可空
```

完整阅读现有实现：

```text
src/GameLexicon.Infrastructure/Persistence/DatabaseOptions.cs
src/GameLexicon.Infrastructure/Persistence/SqliteConnectionFactory.cs
src/GameLexicon.Infrastructure/Persistence/Migrations/IDatabaseMigration.cs
src/GameLexicon.Infrastructure/Persistence/Migrations/MigrationRunner.cs
src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/**

src/GameLexicon.Application/Abstractions/Persistence/**
src/GameLexicon.Application/Entries/Queries/**

english-learning-project/scripts/AppServices.cs
english-learning-project/scripts/AppRoot.cs
```

必须确认当前迁移注册方式，不得假设。

如存在以下 Skills，也必须读取：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/godot-workflow/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Infrastructure / Persistence / Migration

Primary writer:
primary coordinator

Supporting agents:
- milestone architect：只读审查 schema v2、索引最小性和任务边界
- godot specialist：只读审查迁移注册与 headless 启动验证
- skill curator：仅在 Skill Impact Review 需要时调用
```

---

# 5. 阶段 0：重新核验基线

## 5.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline e67f5cf9fc13a2a5220885310765e8653791bea0
git diff --check
```

必须确认：

- 当前分支为 `main`。
- 工作区干净。
- 提交存在。
- 提交完整包含 M1-T04。
- 没有未确认的用户修改。

工作区不干净时立即停止：

- 不恢复。
- 不覆盖。
- 不暂存。
- 不提交。
- 不执行 `git reset --hard`。
- 不执行 `git clean -fd`。

## 5.2 状态

确认：

```text
M1-T04 = Done
M1-T05 = Not Started
```

状态不一致时停止。

## 5.3 解决方案与目标框架

执行：

```powershell
dotnet sln GameLexicon.sln list
```

确认仍为 8 个项目。

确认目标框架保持当前事实：

```text
Godot desktop conditional target       net8.0
Godot Android conditional target       net9.0
GameLexicon.Domain                     net8.0
GameLexicon.Application                net8.0
GameLexicon.Infrastructure             net8.0
GameLexicon.Domain.Tests               net10.0
GameLexicon.Application.Tests          net10.0
GameLexicon.Infrastructure.Tests       net10.0
GameLexicon.CaptureBridge              net10.0
```

不得修改任一目标框架或项目引用。

## 5.4 Migration001 完整性

执行：

```powershell
git log --oneline -- `
  "src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs"

git diff HEAD^ HEAD -- `
  "src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs"
```

确认 M1-T04 未修改 Migration001。

记录当前 Migration001 文件哈希：

```powershell
git hash-object `
  "src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs"
```

任务结束时再次比较。

Migration001 永久保持历史事实，不得修改。

## 5.5 基线构建和测试

优先执行：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

预期：

```text
Build: 0 warnings, 0 errors
Tests: 205/205 passed
```

本任务不新增 NuGet 包，通常不需要 Restore。

仅在资产文件确实缺失时执行：

```powershell
dotnet restore GameLexicon.sln
```

不得：

- 禁用 NuGet Audit。
- 更换包版本。
- 修改 NuGet 源。
- 添加 SQLite Provider。

---

# 6. 新迁移

创建：

```text
src/GameLexicon.Infrastructure/Persistence/Migrations/
Migration002_ManualExamplesAndSearchSupport.cs
```

核心类型名：

```csharp
public sealed class Migration002_ManualExamplesAndSearchSupport
    : IDatabaseMigration
```

Version：

```csharp
public int Version => 2;
```

命名可因现有风格小幅调整，但：

- 必须以 `Migration002` 开头。
- Version 必须是 2。
- 不得修改 Version 1。
- 不得创建 Version 3。
- 不提供 Down migration。

---

# 7. SQLite 表重建要求

SQLite 不应通过直接修改 Migration001 来改变历史 schema。

本任务必须在 Migration002 内重建表。

## 7.1 禁止关闭外键

迁移期间不得执行：

```sql
PRAGMA foreign_keys = OFF;
```

原因：

- MigrationRunner 已要求每个迁移处于独立事务。
- `PRAGMA foreign_keys` 在事务中无法可靠切换。
- 关闭外键可能掩盖数据损坏。
- M1-T01 已规定连接启用外键。

迁移必须在：

```text
foreign_keys = ON
```

的真实条件下通过。

## 7.2 为什么必须处理 `entry_examples`

`entry_examples.example_id` 外键引用：

```text
sentence_examples.id
```

在外键启用时直接删除旧 `sentence_examples` 可能影响子链接或使迁移失败。

因此必须将：

```text
sentence_examples
entry_examples
```

作为一个事务内的相关结构共同重建或安全迁移。

不得依赖未验证的：

```text
ALTER TABLE ... RENAME
```

自动修复全部外键引用。

## 7.3 推荐迁移算法

在 MigrationRunner 已创建的事务中执行：

```text
1. 校验当前 schema 是预期 v1 结构。
2. 创建无外键的临时备份表：
   entry_examples_m002_backup
3. 将 entry_examples 全量复制到备份表。
4. 删除原 entry_examples 子表。
5. 创建 sentence_examples_m002_new：
   - capture_id nullable
   - 其他列保持一致
   - 保持原外键行为
   - 增加 OCR/Capture CHECK
6. 全量复制原 sentence_examples 数据。
7. 校验复制行数一致。
8. 删除原 sentence_examples。
9. 将 sentence_examples_m002_new 重命名为 sentence_examples。
10. 按原结构重建 entry_examples。
11. 将备份链接全量复制回 entry_examples。
12. 校验链接行数一致。
13. 删除临时备份表。
14. 创建本任务索引。
15. 运行 foreign_key_check。
16. MigrationRunner 写入 schema version 2。
17. 提交。
```

允许采用等价且经过测试的安全算法。

不得：

- 删除用户例句。
- 删除 entry_examples 链接。
- 用 `INSERT OR IGNORE` 隐藏复制错误。
- 用 `INSERT OR REPLACE` 覆盖冲突。
- 重建无关业务表。
- 提交中间状态。
- 在 Migration 自己内部提交 Runner 的事务。

## 7.4 临时对象命名

建议：

```text
sentence_examples_m002_new
entry_examples_m002_backup
```

要求：

- 名称明确属于 Migration002。
- 正常成功后全部删除或重命名。
- 成功后 schema 不得残留临时对象。
- 如果开始时发现同名对象已存在，安全失败并报告。
- 不静默删除来源不明的临时对象。

---

# 8. schema v2 的 `sentence_examples`

迁移后的表必须等价于：

```sql
CREATE TABLE sentence_examples (
    id TEXT PRIMARY KEY,
    capture_id TEXT,
    ocr_region_id TEXT,
    sentence_text TEXT NOT NULL,
    normalized_sentence TEXT NOT NULL,
    target_start INTEGER NOT NULL,
    target_length INTEGER NOT NULL,
    screenshot_crop_path TEXT NOT NULL DEFAULT '',
    game_title TEXT,
    created_at_utc TEXT NOT NULL,

    CHECK (
        ocr_region_id IS NULL
        OR capture_id IS NOT NULL
    ),

    FOREIGN KEY (capture_id)
        REFERENCES captures(id)
        ON DELETE RESTRICT,

    FOREIGN KEY (ocr_region_id)
        REFERENCES ocr_regions(id)
        ON DELETE SET NULL
);
```

## 8.1 必须保持的列

必须保持：

```text
id
capture_id
ocr_region_id
sentence_text
normalized_sentence
target_start
target_length
screenshot_crop_path
game_title
created_at_utc
```

不得：

- 改名。
- 改类型。
- 新增业务列。
- 删除默认值。
- 修改删除行为。
- 添加 Repository 专属字段。

## 8.2 Capture/OCR 组合

数据库必须允许：

```text
capture_id NULL
ocr_region_id NULL
```

数据库必须允许：

```text
capture_id 非 NULL
ocr_region_id NULL
```

数据库必须允许：

```text
capture_id 非 NULL
ocr_region_id 非 NULL
```

数据库必须拒绝：

```text
capture_id NULL
ocr_region_id 非 NULL
```

## 8.3 外键语义

必须保持：

```text
capture_id
→ captures(id)
→ ON DELETE RESTRICT

ocr_region_id
→ ocr_regions(id)
→ ON DELETE SET NULL
```

说明：

- `capture_id` 为 NULL 时不触发外键失败。
- OCR Region 删除后，例句 `ocr_region_id` 变为 NULL。
- Capture 删除仍受已有例句引用限制。
- 本任务不增加复合外键来验证 OCR Region 属于同一个 Capture。
- Capture/OCR 一致性仍由 Domain 和后续 Repository 验证。

---

# 9. `entry_examples` 重建

重建后的结构必须保持 Migration001 语义：

```sql
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
```

不得改变：

- 复合主键。
- `is_primary` 默认值。
- `sort_order` 默认值。
- 两个外键。
- 两个级联删除行为。

所有原链接必须保留：

```text
entry_id
example_id
is_primary
sort_order
```

不得重新生成 ID 或排序值。

---

# 10. v1 数据保护

## 10.1 迁移前检查

在修改 schema 前，至少检查：

- `sentence_examples` 存在。
- `entry_examples` 存在。
- `capture_id` 当前为 `NOT NULL`。
- 关键列存在。
- 临时对象不存在。

如果 schema 不符合预期：

- 抛出清晰异常。
- 不尝试猜测修复。
- 不删除表。
- 不记录 Version 2。
- 阻止正常启动。

## 10.2 行数校验

迁移过程中记录安全的整数计数：

```text
sentence_examples 原行数
sentence_examples 新行数
entry_examples 原行数
entry_examples 新行数
```

要求：

```text
迁移前后数量必须一致
```

数量不一致时抛出异常并回滚。

日志可以记录：

```text
Migration002 copied N examples and M links.
```

计数不属于敏感学习文本。

不得记录行内容。

## 10.3 字段值保护

测试必须确认以下值逐字段保持：

- ID。
- CaptureId。
- OcrRegionId。
- SentenceText。
- NormalizedSentence。
- TargetStart。
- TargetLength。
- ScreenshotCropPath。
- GameTitle。
- CreatedAt。
- EntryId。
- ExampleId。
- IsPrimary。
- SortOrder。

## 10.4 `foreign_key_check`

迁移结束前，在同一连接执行：

```sql
PRAGMA foreign_key_check;
```

必须返回 0 行。

如返回任何行：

- 迁移失败。
- 完整回滚。
- 不写入 Version 2。
- 不继续应用。

---

# 11. 检索与关联索引

只增加直接服务于 M1-T04 已确定查询契约、详情读取和外键关联的索引。

不得添加全文搜索、FTS5 或模糊搜索结构。

## 11.1 必须新增的索引

### 词条默认列表与归档过滤

```sql
CREATE INDEX ix_vocabulary_entries_archive_updated
ON vocabulary_entries(
    is_archived,
    updated_at_utc DESC,
    id ASC
);
```

支持：

```text
ActiveOnly / ArchivedOnly
UpdatedAtDescending
稳定次级 ID
```

### 词条类型筛选

```sql
CREATE INDEX ix_vocabulary_entries_archive_type_updated
ON vocabulary_entries(
    is_archived,
    entry_type,
    updated_at_utc DESC,
    id ASC
);
```

支持：

```text
归档状态 + EntryType + 默认排序
```

### 例句详情排序

```sql
CREATE INDEX ix_entry_examples_entry_sort
ON entry_examples(
    entry_id,
    sort_order,
    example_id
);
```

支持：

```text
GetForEntryAsync
SortOrder ASC
ExampleId 稳定次级排序
```

### 例句反向关联与外键操作

```sql
CREATE INDEX ix_entry_examples_example_entry
ON entry_examples(
    example_id,
    entry_id
);
```

支持：

```text
按 Example 查链接
删除例句时的子表外键检查
```

### 标签筛选

```sql
CREATE INDEX ix_entry_tags_tag_entry
ON entry_tags(
    tag_id,
    entry_id
);
```

支持：

```text
按 TagId 筛选词条
```

### 游戏筛选

```sql
CREATE INDEX ix_sentence_examples_game_created
ON sentence_examples(
    game_title COLLATE NOCASE,
    created_at_utc DESC,
    id ASC
);
```

支持：

```text
按来源游戏筛选
稳定时间排序
```

## 11.2 可选 FK 索引

milestone architect 可基于实际 schema 和 SQLite 查询计划决定是否额外加入：

```sql
CREATE INDEX ix_sentence_examples_capture_id
ON sentence_examples(capture_id);

CREATE INDEX ix_sentence_examples_ocr_region_id
ON sentence_examples(ocr_region_id);
```

加入条件：

- 明确改善外键检查或后续读取路径。
- 不与已有索引重复。
- 测试和文档记录。

不加入也不视为失败，但必须在最终报告说明。

## 11.3 不得新增

本任务不得新增：

```text
FTS5 virtual table
LIKE '%text%' 专用伪索引
translation_chinese 普通 contains 索引
notes 索引
definition 全文索引
Review 查询索引
截图状态索引
```

原因：

- SearchText 的具体 SQL 语义留给 M1-T09。
- 普通 B-tree 无法可靠优化前导通配符 contains 查询。
- 不应在 Repository SQL 尚未实现前过度索引。

## 11.4 避免重复索引

不得重复覆盖已有：

```text
ux_vocabulary_entries_normalized_active
tags.normalized_name UNIQUE
entry_examples PRIMARY KEY(entry_id, example_id)
entry_tags PRIMARY KEY(entry_id, tag_id)
```

测试必须使用：

```text
PRAGMA index_list
PRAGMA index_info
```

核对名称和列顺序。

---

# 12. 迁移注册

必须将 Migration002 加入当前运行时迁移列表。

实际位置以代码为准，可能在：

```text
english-learning-project/scripts/AppServices.cs
```

或 Infrastructure 的迁移目录/工厂。

要求：

```text
Migration001
Migration002
```

都注册。

顺序输入可以明确为 1、2，也可以由 MigrationRunner 排序。

必须验证：

- 新数据库首次启动应用 Version 1 和 2。
- 已有 Version 1 数据库只应用 Version 2。
- Version 2 数据库不重复应用。
- 日志中 Migration002 只出现一次。
- 不注册 Version 2 两次。
- 不移除 Version 1。
- 不修改 MigrationRunner 的通用逻辑，除非发现真实缺陷。

## 12.1 允许修改 Godot 的范围

只允许为迁移注册最小修改：

```text
english-learning-project/scripts/AppServices.cs
```

如果迁移注册在其他既有组合根文件中，则修改实际文件。

不得修改：

- 场景。
- UI。
- 导航。
- `project.godot`。
- Godot 目标框架。
- 设置页面。

---

# 13. 日志与隐私

允许记录：

```text
Database migration started: 2.
Database migration applied: 2.
Database schema is current: 2.
Migration002 copied <count> examples and <count> links.
```

不得记录：

- SentenceText。
- NormalizedSentence。
- Headword。
- Definition。
- Translation。
- Notes。
- GameTitle。
- Screenshot path。
- 数据库行内容。
- SQL 参数中的用户文本。
- 完整数据库连接字符串。
- API Key、Token、密码。

失败日志只记录：

- Migration version。
- 安全异常类型。
- 脱敏错误摘要。
- 行数。

不得输出完整迁移数据。

---

# 14. 自动化测试

测试放在：

```text
tests/GameLexicon.Infrastructure.Tests/Persistence/
```

建议创建：

```text
Migration002ManualExamplesAndSearchSupportTests.cs
```

也可按现有测试结构拆分。

使用真实临时 SQLite 文件。

不得使用真实 `user://` 数据库作为单元测试数据库。

---

# 15. Migration002 schema 测试

至少覆盖：

## 15.1 Version

```text
Migration002.Version = 2
```

## 15.2 新数据库完整迁移

运行 MigrationRunner：

```text
Migration001
Migration002
```

验证：

- `schema_migrations` 有 1 和 2。
- 当前版本是 2。
- 两个版本各一行。
- 所有原始表仍存在。
- 所有新索引存在。
- 无临时表残留。

## 15.3 `capture_id` 可空

使用：

```sql
PRAGMA table_info(sentence_examples);
```

确认：

```text
capture_id notnull = 0
```

其他必填列保持 `notnull = 1`。

## 15.4 手工例句插入

插入：

```text
capture_id = NULL
ocr_region_id = NULL
```

必须成功。

读取后确认所有字段往返。

## 15.5 来源组合约束

验证：

```text
Capture NULL + OCR NULL
→ Pass

Capture value + OCR NULL
→ Pass

Capture value + OCR value
→ Pass

Capture NULL + OCR value
→ Rejected
```

最后一项必须由数据库 CHECK 或约束拒绝。

## 15.6 外键行为

验证：

- 不存在的 CaptureId 被拒绝。
- 不存在的 OcrRegionId 被拒绝。
- 删除被例句引用的 Capture 时 RESTRICT。
- 删除 OcrRegion 后 `ocr_region_id` 变为 NULL。
- 删除词条后 `entry_examples` 级联删除。
- 删除例句后 `entry_examples` 级联删除。

---

# 16. v1 → v2 无损升级测试

必须构造真实 Version 1 数据库：

```text
只应用 Migration001
→ 插入有 Capture 的例句
→ 插入 OCR 例句
→ 插入词条
→ 建立 entry_examples 链接
→ 建立 Tag 和 entry_tags
→ 执行 Migration002
```

迁移后验证：

- `schema_migrations` 有 1 和 2。
- 所有表仍存在。
- 原例句数量一致。
- 原链接数量一致。
- 每个字段值完全一致。
- Primary 和 SortOrder 完全一致。
- Tag 和 entry_tags 未变化。
- `foreign_key_check` 0 行。
- 临时对象不存在。
- 新手工例句可以插入。
- 原 Capture/OCR 例句仍可读取。
- 原外键行为仍然有效。

不得只测试空数据库升级。

---

# 17. 幂等测试

## 17.1 第二次运行

对 Version 2 数据库再次运行 MigrationRunner。

验证：

- Migration002 不再执行。
- Version 2 记录仍只有一行。
- 表和索引不重复。
- 数据不变化。
- 无临时表。
- AppliedVersions 为空或等价表示无新增版本。

## 17.2 应用重启语义

测试或 headless 运行必须确认：

```text
第一次 v1 → v2
第二次 schema current 2
```

日志中：

```text
MigrationApplied 2
```

只出现一次。

---

# 18. 失败回滚测试

必须验证 Migration002 失败时完整回滚。

## 18.1 推荐故障注入

在 Version 1 数据库中预先创建一个与 Migration002 必须创建的索引同名、但结构不同的索引，例如：

```text
ix_vocabulary_entries_archive_updated
```

然后执行 Migration002。

预期：

- Migration002 在创建索引阶段失败。
- Version 2 不写入 `schema_migrations`。
- 原 `sentence_examples.capture_id` 仍为 NOT NULL。
- 原例句仍存在。
- 原 entry_examples 链接仍存在。
- 原字段值未改变。
- 没有 Migration002 临时表残留。
- Version 1 保持可读取。
- 后续迁移不执行。

允许使用等价故障注入，但必须让失败发生在已经执行过部分 DDL 后，以真正验证事务回滚。

不得仅在迁移开始前主动抛异常冒充回滚测试。

## 18.2 取消测试

如果当前迁移框架支持在多条命令之间检查 CancellationToken：

- 在执行中途取消。
- 验证完整回滚。
- Version 2 不写入。

如果现有执行模型无法可靠注入中途取消：

- 保留现有 MigrationRunner cancellation 测试。
- 最终报告说明。
- 不为测试引入生产环境延迟或钩子。

---

# 19. 索引测试

使用：

```sql
PRAGMA index_list('<table>');
PRAGMA index_info('<index>');
```

必须验证：

```text
ix_vocabulary_entries_archive_updated
→ is_archived, updated_at_utc, id

ix_vocabulary_entries_archive_type_updated
→ is_archived, entry_type, updated_at_utc, id

ix_entry_examples_entry_sort
→ entry_id, sort_order, example_id

ix_entry_examples_example_entry
→ example_id, entry_id

ix_entry_tags_tag_entry
→ tag_id, entry_id

ix_sentence_examples_game_created
→ game_title, created_at_utc, id
```

验证：

- 索引只存在一份。
- 列顺序正确。
- 原唯一索引仍存在：
  `ux_vocabulary_entries_normalized_active`。
- tags 的唯一约束仍存在。
- 复合主键仍存在。
- 未创建 FTS 表。

对于 DESC 和 COLLATE 细节，可使用：

```text
sqlite_master.sql
PRAGMA index_xinfo
```

进行必要验证。

---

# 20. 文件锁与临时数据库删除

所有测试结束后必须：

1. Dispose connection。
2. Dispose transaction。
3. 删除临时 `.db`。
4. 删除可能的 `-wal`。
5. 删除可能的 `-shm`。
6. 删除临时目录。

验证：

```text
测试数据库及 sidecar 均可删除
```

不得因失败测试留下锁。

---

# 21. 实际 runtime 数据库验证

单元测试通过后验证 Godot 运行时注册。

## 21.1 安全前置检查

当前无 Godot 进程。

定位实际：

```text
user://data/gamelexicon.db
```

检查当前 schema version。

允许：

```text
不存在
Version 1
Version 2
```

遇到以下情况必须停止：

```text
schema_migrations 不存在但存在业务表
版本高于 2
迁移记录异常
foreign_key_check 非空
数据库无法正常打开
来源不明的临时迁移表存在
```

不得删除或重建未知数据库。

## 21.2 现有 Version 1 数据库

如果实际开发数据库为 Version 1：

1. 确认无 Godot 进程。
2. 确认 WAL 已 checkpoint 或连接已全部关闭。
3. 在 `user://data/` 外或明确备份目录创建一次性验证备份：
   - `.db`
   - 如存在则包含 `-wal`
   - 如存在则包含 `-shm`
4. 备份文件不得进入 Git。
5. 报告备份路径，但日志不记录个人学习数据。
6. 再执行应用迁移。

不得把“自动备份产品功能”提前实现到应用。

这只是开发验证保护措施，不是 M7 备份功能。

## 21.3 Godot headless 第一次启动

执行：

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit-after 5
```

确认：

- Version 1 数据库升级到 2；或
- 新数据库依次应用 1 和 2；或
- 已是 Version 2 时无新增迁移。
- 应用正常初始化。
- 导航正常初始化。
- 无 SQLite 原生错误。
- 无迁移错误。
- 无 table/index already exists。
- 无数据库锁定错误。

## 21.4 Godot headless 第二次启动

再次执行同一命令。

确认：

- Migration002 不重复执行。
- schema current = 2。
- 应用正常启动。
- 无数据变化。
- 无临时表。

## 21.5 实际 schema 检查

只读检查：

```sql
SELECT version, applied_at_utc
FROM schema_migrations
ORDER BY version;
```

预期：

```text
1
2
```

每个版本一行。

检查：

```sql
PRAGMA table_info(sentence_examples);
PRAGMA foreign_key_check;
```

预期：

```text
capture_id nullable
foreign_key_check 0 rows
```

不得输出或复制用户学习文本到报告。

---

# 22. 构建与验证命令

## 22.1 Infrastructure

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-build `
  --no-restore
```

## 22.2 Godot 项目

由于迁移注册可能修改 AppServices：

```powershell
dotnet build `
  "english-learning-project\EnglishLearningProject.csproj" `
  --no-restore
```

只验证桌面目标，不修改 Android 条件目标。

## 22.3 根解决方案

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功。
- 所有测试通过。
- 0 错误。
- 0 新增警告。

## 22.4 Godot headless build

```powershell
& "E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe" `
  --headless `
  --editor `
  --build-solutions `
  --path "D:\UGit\EnglishLearningProject\english-learning-project" `
  --quit
```

确认：

- C# build 成功。
- 迁移类型可加载。
- 无资源错误。
- 无 SQLite Provider 错误。

---

# 23. 不需要 GUI 验收

milestone architect 已将 M1-T05 定义为非 GUI 任务。

本任务：

```text
GUI verification required: No
```

需要：

- 自动化 schema 测试。
- v1 数据无损升级测试。
- 失败回滚测试。
- Godot headless 双次启动。
- 非 GUI 人工代码和 schema 审查。

不需要打开 Godot 编辑器或操作页面。

---

# 24. 允许创建和修改的文件

建议创建：

```text
src/GameLexicon.Infrastructure/Persistence/Migrations/
Migration002_ManualExamplesAndSearchSupport.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/
Migration002ManualExamplesAndSearchSupportTests.cs
```

允许修改：

```text
迁移注册所在的既有文件
通常为：
english-learning-project/scripts/AppServices.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/**
（仅复用测试 helper 或增加必要测试）

docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/DECISIONS.md（仅长期 schema 决策需要时）
docs/SKILLS_CATALOG.md（仅 Skill Impact Review 需要时）
docs/SKILL_CHANGELOG.md（仅 Skill 实际更新时）
.agents/skills/*/SKILL.md（仅可复用流程变化时）
```

正常情况下不得修改：

```text
Migration001_Initial.cs
MigrationRunner.cs
IDatabaseMigration.cs
SqliteConnectionFactory.cs
任一 .csproj
GameLexicon.sln
src/GameLexicon.Domain/**
src/GameLexicon.Application/**
Godot 场景和 UI
english-learning-project/project.godot
```

如果发现 MigrationRunner 存在阻止正确事务迁移的真实缺陷：

1. 停止。
2. 报告证据。
3. 不在未经确认时扩大范围修改 Runner。

---

# 25. 本任务明确不做

不得实现：

- `SqliteSentenceExampleRepository`。
- `SqliteTagRepository`。
- `SqliteVocabularyRepository`。
- Repository SQL。
- 查询 SQL。
- FTS5。
- 文本 contains 优化。
- 手工添加 UseCase。
- 重复词条 UseCase。
- 列表或详情 UseCase。
- Godot 业务 UI。
- Capture/OCR 业务。
- Review 查询。
- Backup 产品功能。
- Migration003。
- M1-T06。

---

# 26. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且修改未确认。
- 找不到提交 `e67f5cf9...`。
- M1-T04 未标记 Done。
- M1-T05 状态不是 Not Started。
- 基线构建或测试失败。
- 解决方案不再是 8 个项目。
- 目标框架发生变化。
- Migration001 哈希发生变化。
- 必须修改 Migration001。
- 必须关闭 foreign_keys 才能迁移。
- v1 数据无法无损保留。
- entry_examples 链接无法无损保留。
- `foreign_key_check` 失败。
- Migration002 失败后无法完整回滚。
- 必须修改 MigrationRunner 才能继续。
- 必须新增 NuGet 包。
- 必须修改 Domain 或 Application。
- 实际用户数据库版本高于 2。
- 实际用户数据库 schema 异常。
- 发现来源不明的迁移临时表。
- 用户文件可能被覆盖。

停止后不得：

- 删除用户数据库。
- 自动重建数据库。
- 修改 Version 1 迁移。
- `git reset --hard`。
- `git clean -fd`。
- 禁用 NuGet Audit。
- 自动提交。
- 自动执行 M1-T06。

---

# 27. Git 检查

完成自动验证后执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
git diff --name-only
```

再次计算 Migration001 哈希：

```powershell
git hash-object `
  "src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs"
```

必须与任务开始时一致。

确认：

- 生产代码只新增 Migration002。
- Godot 只修改最小迁移注册。
- 测试只属于 Migration002。
- 状态文档更新合理。
- 没有 Domain 修改。
- 没有 Application 修改。
- 没有 Repository 实现。
- 没有 Migration001 修改。
- 没有 `.csproj` 或解决方案修改。
- 没有数据库、备份、日志、`.godot/`、`bin/`、`obj/`。
- 没有 `-wal`、`-shm`。
- 暂存区为空。
- 未创建 Git 提交。

---

# 28. 状态与文档

自动验收通过后更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

状态：

```text
M1-T05 = Awaiting Manual Verification
M1-T06 = Not Started
```

记录：

- Task ID。
- 名称。
- Migration class。
- Version 2。
- CaptureId nullable。
- OCR/Capture CHECK。
- 表重建算法。
- entry_examples 数据保护。
- 外键保持。
- 新索引名称与列顺序。
- 可选 FK 索引决定。
- Migration001 哈希未变。
- 迁移注册位置。
- v1 → v2 测试结果。
- 新数据库 1 → 2 测试结果。
- 第二次运行幂等结果。
- 失败回滚结果。
- foreign_key_check。
- 临时表清理。
- 实际 user:// schema version。
- Godot headless 双次启动结果。
- 新增测试数量与总数。
- 构建结果。
- 已知限制。

更新：

```text
docs/AGENT_HANDOFF.md
```

只有出现长期 schema 决策时才更新：

```text
docs/DECISIONS.md
```

ADR-007 必须保留。

只有环境事实变化时才更新：

```text
docs/ENVIRONMENT.md
```

正常情况下不修改 `ENVIRONMENT.md`。

人工审查通过后：

```text
M1-T05 = Done
M1-T06 = Not Started
```

不得执行 M1-T06。

---

# 29. Skill Impact Review

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

正常预期：

```text
Skills used:
- project-routing
- milestone-workflow
- godot-workflow
- skill-maintenance

Skill update required:
No
```

仅在以下可复用工作流变化时更新 Skill：

- SQLite 外键开启状态下的父/子表重建模板。
- v1 数据无损迁移测试标准。
- 迁移故障注入和回滚验证标准。
- Godot runtime migration 验证流程。
- 数据库临时备份验证流程。
- 任务路由或验收模板。

普通 Migration002 代码和索引不自动构成 Skill 更新理由。

---

# 30. 自动验收清单

- [ ] 提交 `e67f5cf9...` 存在
- [ ] 当前分支 main
- [ ] 初始工作区干净
- [ ] M1-T04 Done
- [ ] M1-T05 Not Started
- [ ] 基线 Build 成功
- [ ] 基线 205/205 测试通过
- [ ] Migration001 哈希已记录
- [ ] Migration001 未修改
- [ ] Migration002 创建
- [ ] Migration002 Version = 2
- [ ] Migration002 已注册
- [ ] 未新增 NuGet 包
- [ ] foreign_keys 未关闭
- [ ] sentence_examples.capture_id 可空
- [ ] OCR 无 Capture 被数据库拒绝
- [ ] 手工例句无 Capture 可插入
- [ ] Capture/OCR 例句仍可插入
- [ ] capture FK RESTRICT 保持
- [ ] OCR FK SET NULL 保持
- [ ] entry_examples 结构保持
- [ ] entry_examples 数据保持
- [ ] v1 例句逐字段保持
- [ ] Tag 和 entry_tags 数据不受影响
- [ ] 迁移前后行数一致
- [ ] foreign_key_check 0 行
- [ ] 无临时表残留
- [ ] 六个必需索引存在
- [ ] 索引列顺序正确
- [ ] 原唯一索引保持
- [ ] 未创建 FTS
- [ ] 新数据库应用 Version 1 和 2
- [ ] v1 数据库只应用 Version 2
- [ ] v2 数据库第二次运行无迁移
- [ ] Version 2 记录只有一行
- [ ] 故障注入导致完整回滚
- [ ] 回滚后 Version 1 数据可读取
- [ ] 回滚后 Version 2 未记录
- [ ] 测试 DB、WAL、SHM 可删除
- [ ] Infrastructure 测试通过
- [ ] Godot 项目构建通过
- [ ] 根解决方案构建通过
- [ ] 全部测试通过
- [ ] Godot headless 第一次启动通过
- [ ] Godot headless 第二次启动通过
- [ ] 实际 schema version = 2
- [ ] 日志无用户学习文本
- [ ] git diff --check 通过
- [ ] 暂存区为空
- [ ] 未创建提交
- [ ] M1-T06 未执行
- [ ] Skill Impact Review 完成

---

# 31. 非 GUI 人工审查清单

- [ ] Migration002 Version 为 2
- [ ] Migration001 未修改
- [ ] CaptureId 只在 Version 2 变为 nullable
- [ ] OCR/Capture CHECK 正确
- [ ] 表重建不关闭 foreign_keys
- [ ] entry_examples 先备份并无损恢复
- [ ] 所有 DDL 和复制处于 Runner 事务内
- [ ] 行数校验存在
- [ ] foreign_key_check 存在
- [ ] 临时表成功后不残留
- [ ] 失败后完整回滚
- [ ] schema_migrations Version 2 不会提前写入
- [ ] 六个索引范围合理
- [ ] 没有 FTS 或过度索引
- [ ] Migration002 已加入运行时注册
- [ ] Godot 只修改最小组合根
- [ ] 无 Repository、UseCase 或 UI
- [ ] 所有测试通过
- [ ] Git diff 仅属于 M1-T05

---

# 32. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T05
- 名称: Migration002 手工例句与检索支持
- 状态:
- M1-T06 executed: No
- Git commit created: No
- GUI verification required: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M1-T04 commit:
- Branch:
- Initial Git status:
- Solution projects:
- Target frameworks:
- Baseline build:
- Baseline tests:
- Migration001 initial hash:

## Migration002

- Class:
- Version:
- Registered at:
- Migration001 modified:
- Foreign keys disabled:
- Transaction owner:

## Schema v2

- CaptureId nullable:
- OCR/Capture CHECK:
- Capture FK:
- OCR FK:
- entry_examples preserved:
- Temporary objects:
- foreign_key_check:

## 重建算法

1. ...
2. ...

## 索引

| Index | Columns | Purpose |
|---|---|---|
| ... | ... | ... |

- Optional FK indexes:
- Existing indexes preserved:
- FTS created:

## 数据迁移验证

- v1 examples before/after:
- v1 links before/after:
- Field equality:
- Tags preserved:
- Manual example insert:
- Capture example insert:
- OCR example insert:
- Invalid OCR without Capture:
- Delete behaviors:

## 幂等与回滚

- New DB applied:
- v1 upgrade applied:
- Second run applied:
- Version 2 row count:
- Failure injection:
- Rollback schema:
- Rollback data:
- Cancellation:

## 创建的文件

- ...

## 修改的文件

- ...

## 自动化测试

- Baseline total:
- Added:
- Infrastructure total:
- Root total:
- Passed:
- Failed:
- Skipped:
- Temporary database deletion:

## 构建与 Godot

- Infrastructure:
- Godot project:
- Root solution:
- Headless build:
- First headless launch:
- Second headless launch:
- Runtime schema version:
- Godot processes after verification:

## 日志与安全

- Migration events:
- User learning text absent:
- Connection string absent:
- Database row content absent:

## Git diff

```text
...
```

- Migration001 final hash:
- Migration001 hash unchanged:

## Skill Impact Review

- Skills used:
- Update required:
- Skills updated:
- Documentation updated:
- Restart required:

## 人工审查

- Awaiting user review.
- No GUI run is required.

## 已知限制

- No SQLite Repository.
- No query SQL or FTS.
- No manual-entry UseCase or UI.
- Capture/OCR cross-row ownership is not enforced by a composite FK.
- ...

## 下一任务

- M1-T06：SQLite 例句 Repository
- Status: Not Started
- Not automatically executed
```

---

# 33. 可直接执行的总指令

请执行：

```text
M1-T05：Migration002 手工例句与检索支持
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T05_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `e67f5cf9fc13a2a5220885310765e8653791bea0`。
2. 开始时 Git 工作区必须干净。
3. 记录 Migration001 初始文件哈希，结束时必须一致。
4. 新增 `Migration002_ManualExamplesAndSearchSupport`，Version = 2。
5. 不修改 Migration001。
6. 不关闭 SQLite foreign_keys。
7. 在 MigrationRunner 的事务内安全重建 sentence_examples 和 entry_examples。
8. 将 sentence_examples.capture_id 改为 nullable。
9. 数据库约束 OcrRegionId 不能在 CaptureId 为空时存在。
10. 无损保留 v1 例句、链接和所有字段值。
11. 迁移前后执行行数校验。
12. 迁移结束前执行 foreign_key_check。
13. 失败时完整回滚，不记录 Version 2。
14. 创建任务规定的六个最小索引。
15. 不创建 FTS 或 contains 搜索结构。
16. 将 Migration002 加入运行时迁移注册。
17. 使用真实临时 SQLite 文件测试空库、v1 升级、幂等和失败回滚。
18. 测试数据库和 WAL/SHM 必须可删除。
19. 执行 Godot headless 双次启动，确认 Version 2 只应用一次。
20. 不实现 Repository、查询 SQL、UseCase 或 UI。
21. 不修改 Domain、Application、项目引用或目标框架。
22. 不新增 NuGet 包。
23. 不执行 M1-T06。
24. 不创建 Git 提交。
25. 自动验收后保持 Awaiting Manual Verification。
26. 本任务不需要 GUI 验收。
27. 完成后执行 Git diff、状态文档更新和 Skill Impact Review。
