# M1-T08 Codex 执行指令

## 任务名称

```text
M1-T08：SQLite 词条 Repository 写侧
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T08_CODEX_INSTRUCTION.md
```

本任务只实现：

```text
SqliteVocabularyRepository 写侧骨架
SaveAsync(VocabularyEntry, CancellationToken)
词条新增
词条更新
归档状态持久化
活动词头唯一约束验证
时间单调性与创建时间保护
关联数据保护
Infrastructure 自动化测试
```

本任务不实现：

- `FindByNormalizedHeadwordAsync`
- `GetDetailsAsync`
- `SearchAsync`
- 词条详情查询
- 搜索、筛选、排序或分页 SQL
- 永久删除
- Application UseCase
- Godot 接线或 UI
- Migration003
- M1-T09 或任何后续任务

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
bdb8a3ebc05762c5a0f52088e90246f20fd2739d
```

当前状态：

- 当前分支：`main`
- Git 工作区干净
- M1-T07 提交完整
- M1-T07 = `Done`
- M1-T08 = `Not Started`
- 当前无 Godot 编辑器或残留进程
- 根解决方案包含 8 个项目
- 目标框架：
  - Godot 桌面：`net8.0`
  - Godot Android 条件目标：`net9.0`
  - Domain、Application、Infrastructure：`net8.0`
  - 三个测试项目、CaptureBridge：`net10.0`
- 构建成功，0 警告、0 错误
- 测试 246/246 通过：
  - Domain：111
  - Application：61
  - Infrastructure：74
- Migration001 哈希：
  `1fd5546081fe87c479ebd21d52e26f7d1dfaa636`
- Migration002 哈希：
  `d8ce250e24442ece38c231e3ae8286a4d0def4c5`
- `IVocabularyRepository` 仍严格包含四个方法
- `SaveAsync` 精确签名：

```csharp
Task SaveAsync(
    VocabularyEntry entry,
    CancellationToken cancellationToken);
```

- `SqliteSentenceExampleRepository` 已提交且无修改
- `SqliteTagRepository` 已提交且无修改
- Application 公共 API 未泄漏 SQLite、Godot、Infrastructure 或 `IQueryable`
- 数据库、WAL/SHM、日志、备份和构建产物未进入 Git

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. 任务拆分边界

`IVocabularyRepository` 有四个方法，但 M1-T08 只实现写侧：

```text
M1-T08：
SaveAsync

M1-T09：
FindByNormalizedHeadwordAsync
GetDetailsAsync
SearchAsync
生命周期与查询侧收尾
```

## 2.1 禁止未实现占位

本任务不得为了提前声明：

```csharp
: IVocabularyRepository
```

而让三个查询方法：

- 抛 `NotImplementedException`
- 抛 `NotSupportedException`
- 返回 `null`
- 返回空结果
- 返回伪造数据

## 2.2 推荐类型形态

创建：

```csharp
public sealed partial class SqliteVocabularyRepository
```

本任务暂时不声明实现 `IVocabularyRepository`，但 `SaveAsync` 必须与接口方法拥有完全一致的公开签名：

```csharp
public Task SaveAsync(
    VocabularyEntry entry,
    CancellationToken cancellationToken);
```

M1-T09 在补齐其他三个方法后，再让组合后的 partial 类型正式实现：

```csharp
IVocabularyRepository
```

要求：

- 类型名必须是 `SqliteVocabularyRepository`
- 类型必须 `public sealed partial`
- 本任务不得新增单独的 `IVocabularyWriter`
- 不得修改现有 Application 接口
- 不得创建运行时不可用的查询方法占位

这是阶段拆分策略，不构成新的长期架构接口。

---

# 3. 现有领域结构

当前 `VocabularyEntry` 构造参数：

```text
id
headword
normalizedHeadword
entryType
partOfSpeech
phonetic
definitionEnglish
translationChinese
notes
isArchived
createdAt
updatedAt
```

不可变字段：

```text
Id
CreatedAt
```

可变字段：

```text
Headword
NormalizedHeadword
EntryType
PartOfSpeech
Phonetic
DefinitionEnglish
TranslationChinese
Notes
IsArchived
UpdatedAt
```

领域时间规则：

```text
CreatedAt 必须为 UTC
UpdatedAt 必须为 UTC
UpdatedAt >= CreatedAt
同一聚合正常修改时 UpdatedAt 不得倒退
```

归档：

```text
IsArchived
```

EntryType：

```text
Word = 0
Phrase = 1
Expression = 2
SentencePattern = 3
```

规范化词头：

```text
NormalizedHeadword
```

Repository 不负责生成或重新计算该值。

---

# 4. 当前数据库结构

```sql
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
```

活动词条唯一索引：

```sql
CREATE UNIQUE INDEX ux_vocabulary_entries_normalized_active
ON vocabulary_entries(normalized_headword)
WHERE is_archived = 0;
```

Migration002 还包含查询索引，但本任务不修改它们。

---

# 5. 必须阅读

开始前完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M1-T08_CODEX_INSTRUCTION.md
```

重点阅读：

```text
PRODUCT_SPEC.md
- F07：词条编辑
- F08：重复词条处理
- F09：词条库
- 第 7 节分层职责
- 第 10 节领域模型
- 第 12 节 SQLite 数据模型
- 第 13 节 Repository
- 第 18 节重复词条与归档策略

DECISIONS.md
- 已有词条和规范化相关 ADR
```

完整阅读：

```text
src/GameLexicon.Domain/Entries/
VocabularyEntry.cs
EntryType.cs
EntryGuard.cs

src/GameLexicon.Application/Abstractions/Persistence/
IVocabularyRepository.cs

src/GameLexicon.Infrastructure/Persistence/
SqliteConnectionFactory.cs

src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteSentenceExampleRepository.cs
SqliteTagRepository.cs

src/GameLexicon.Infrastructure/Persistence/Migrations/
Migration001_Initial.cs
Migration002_ManualExamplesAndSearchSupport.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/
SqliteSentenceExampleRepositoryTests.cs
SqliteTagRepositoryTests.cs
```

阅读已有 Repository 的目的：

- 复用连接、事务、参数、GUID、UTC 和异常风格
- 不重构 M1-T06 或 M1-T07
- 不创建另一套数据库基础设施

如存在以下 Skills，也必须阅读：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Infrastructure / Persistence / Vocabulary Write Side

Primary writer:
primary coordinator

Supporting agents:
- milestone architect：只读审查写入语义、时间保护和任务边界
- skill curator：仅在 Skill Impact Review 需要时调用
```

本任务通常不需要 Godot specialist。

---

# 6. 阶段 0：重新核验基线

## 6.1 Git

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline bdb8a3ebc05762c5a0f52088e90246f20fd2739d
git diff --check
```

必须确认：

- 当前分支 `main`
- 工作区干净
- 提交存在
- M1-T07 提交内容完整
- 无未确认的用户修改

工作区不干净时停止，不得恢复或覆盖用户内容。

## 6.2 状态

```text
M1-T07 = Done
M1-T08 = Not Started
```

状态不一致时停止。

## 6.3 解决方案

```powershell
dotnet sln GameLexicon.sln list
```

必须仍为 8 个项目。

不得修改：

- 目标框架
- 项目引用
- 解决方案结构
- NuGet 包

## 6.4 迁移哈希

```powershell
git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration002_ManualExamplesAndSearchSupport.cs"
```

必须分别为：

```text
1fd5546081fe87c479ebd21d52e26f7d1dfaa636
d8ce250e24442ece38c231e3ae8286a4d0def4c5
```

本任务不得修改迁移。

## 6.5 契约

确认 `IVocabularyRepository` 四个方法未变，并记录 `SaveAsync` 精确签名。

不得修改：

```text
IVocabularyRepository
VocabularyEntry
EntryType
ITextNormalizer
Application 查询契约
```

## 6.6 基线验证

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

预期：

```text
Build: 0 warnings, 0 errors
Tests: 246/246 passed
```

---

# 7. 建议目录和文件

创建：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteVocabularyRepository.cs
```

测试：

```text
tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/
SqliteVocabularyRepositoryWriteTests.cs
```

允许最小内部 helper：

```text
VocabularyEntrySqlValues.cs
```

条件：

- `internal`
- 只处理词条 SQL 值映射
- 不形成通用 ORM
- 不重构已有 Repository
- 不泄漏到 Application
- 不引入第三方包

---

# 8. `SqliteVocabularyRepository` 写侧骨架

建议：

```csharp
public sealed partial class SqliteVocabularyRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteVocabularyRepository(
        SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory
            ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public async Task SaveAsync(
        VocabularyEntry entry,
        CancellationToken cancellationToken)
    {
        // M1-T08 implementation
    }
}
```

要求：

- 构造函数只依赖 `SqliteConnectionFactory`
- 不依赖其他 Repository
- 不依赖 Godot
- 不依赖 Logger
- 不长期持有连接
- 不缓存词条
- `SaveAsync` 与接口签名完全一致
- 不声明 `IVocabularyRepository`
- 不添加查询方法占位

---

# 9. `SaveAsync` 总体语义

```text
entry == null
→ ArgumentNullException

数据库无相同 Id
→ INSERT

数据库有相同 Id
→ UPDATE 可变字段
```

必须保存：

```text
id
headword
normalized_headword
entry_type
part_of_speech
phonetic
definition_english
translation_chinese
notes
is_archived
created_at_utc
updated_at_utc
```

更新时：

- `id` 不变
- `created_at_utc` 不变
- 更新其他业务字段
- 更新 `is_archived`
- 更新 `updated_at_utc`

Repository 不得：

- Trim
- ToLower
- Form KC
- 重新规范化 Headword
- 根据 Headword 推导 NormalizedHeadword
- 自动更新时间
- 自动归档或恢复
- 合并重复词条
- 保存 Tags
- 保存 Examples

---

# 10. 推荐写入算法

在单一事务中：

```text
1. 打开连接
2. BeginTransaction
3. 按 Id 查询现有 created_at_utc、updated_at_utc
4. 不存在：
   - INSERT 全部字段
5. 已存在：
   - 解析已有时间
   - 验证传入 CreatedAt 与数据库 CreatedAt 相同
   - 验证传入 UpdatedAt >= 数据库 UpdatedAt
   - UPDATE 可变字段和 updated_at_utc
6. 确认影响行数
7. Commit
```

允许等价的安全 UPSERT，但必须同时满足：

- 不修改已有 `created_at_utc`
- 拒绝 UpdatedAt 倒退
- 能区分未更新原因
- 不使用 REPLACE
- 关联数据不丢失

优先采用显式查询后 INSERT/UPDATE，逻辑更清晰。

---

# 11. 创建时间保护

## 11.1 Insert

新词条：

```text
created_at_utc = entry.CreatedAt
updated_at_utc = entry.UpdatedAt
```

## 11.2 Update

数据库已有相同 Id 时：

```text
entry.CreatedAt 必须等于数据库 created_at_utc
```

不一致时：

- 抛 `InvalidOperationException` 或项目已有稳定异常
- 不修改数据库
- 不静默覆盖
- 不静默忽略传入值
- 不把它当作新词条

原因：

- `CreatedAt` 是聚合不可变身份历史
- 同一 Id 不应拥有两个创建时间

异常消息不得包含词条文本。

---

# 12. 更新时间单调性

数据库已有相同 Id 时：

```text
entry.UpdatedAt >= stored UpdatedAt
```

允许：

```text
相同 UpdatedAt
→ 幂等保存
```

拒绝：

```text
entry.UpdatedAt < stored UpdatedAt
```

拒绝时：

- 抛 `InvalidOperationException` 或项目已有稳定异常
- 完整回滚
- 所有字段保持原值
- 关联保持不变

不得：

- 自动将时间改为数据库时间
- 自动使用 `UtcNow`
- 静默忽略 stale save
- 允许时间倒退

---

# 13. SQL 规则

## 13.1 参数化

所有值必须通过参数传入。

禁止：

```text
字符串插值用户值
字符串拼接用户值
```

## 13.2 显式列

禁止：

```sql
SELECT *
```

读取已有时间时显式查询：

```sql
SELECT
    created_at_utc,
    updated_at_utc
FROM vocabulary_entries
WHERE id = @id;
```

## 13.3 Insert

显式列出全部字段。

## 13.4 Update

只更新：

```text
headword
normalized_headword
entry_type
part_of_speech
phonetic
definition_english
translation_chinese
notes
is_archived
updated_at_utc
```

不得更新：

```text
id
created_at_utc
```

## 13.5 禁止 REPLACE

禁止：

```sql
INSERT OR REPLACE
REPLACE INTO
```

原因：

- REPLACE 会删除旧词条再插入
- 会触发 `entry_examples`、`entry_tags` 的级联删除
- 会破坏词条关联

本任务必须有自动测试证明更新不会丢失两类关联。

---

# 14. 数据映射

## 14.1 GUID

写入：

```csharp
entry.Id.ToString("D").ToLowerInvariant()
```

或项目既有等价 helper。

不得生成新 ID。

## 14.2 EntryType

必须显式映射：

```text
Word → 0
Phrase → 1
Expression → 2
SentencePattern → 3
```

未知枚举值必须失败。

不得直接假设所有整数都有效，除非 Domain 构造已严格保证；测试仍应确认没有未知值写入路径。

## 14.3 Nullable 文本

以下可空字段：

```text
part_of_speech
phonetic
definition_english
translation_chinese
notes
```

映射：

```text
null → DBNull.Value
非 null → 原样字符串
```

不得把 null 改为空字符串，也不得 Trim。

## 14.4 布尔

```text
IsArchived false → 0
IsArchived true → 1
```

## 14.5 UTC

使用项目已有固定 UTC ISO 8601 格式。

不得：

- 使用本地时间
- 使用当前时间替换领域值
- 改变精度
- 保存非 UTC offset

读取已有时间时：

- 使用项目已有严格解析方式
- 非法或非 UTC 时间视为数据库损坏
- 不静默修复
- 异常不包含词条文本

---

# 15. 活动词头唯一约束

数据库约束：

```text
相同 normalized_headword
且 is_archived = 0
→ 最多一条
```

## 15.1 必须允许

```text
Active "quest"
Archived "quest"
```

以及：

```text
多个 Archived "quest"
```

## 15.2 必须拒绝

```text
两个不同 Id
均 Active
normalized_headword 相同
```

## 15.3 恢复冲突

已有：

```text
Entry A Active "quest"
Entry B Archived "quest"
```

尝试将 B 保存为 Active：

- 数据库唯一约束拒绝
- B 仍保持 Archived
- A 不变化
- 关联不变化
- 事务回滚

## 15.4 归档后释放唯一名

已有 A Active `"quest"`：

1. 保存 A 为 Archived
2. 再保存 B Active `"quest"`

应成功。

## 15.5 异常策略

当前 Application 尚无专用重复词条异常契约。

本任务：

- 保留 SQLite 唯一约束作为最终一致性防线
- 不新增 Application 异常
- 不修改 `IVocabularyRepository`
- 可传播 `SqliteException`
- 不捕获后伪装成功
- 不把冲突词条内容写入日志

M1-T11 的重复决策 UseCase 会使用查询和业务流程提前处理正常重复场景。

---

# 16. 关联保护

更新已有词条时必须保留：

```text
entry_examples
entry_tags
```

测试必须建立：

- 一个词条
- 至少一个例句链接
- 至少一个标签链接

然后调用 `SaveAsync` 更新词条。

验证：

- 词条字段更新
- `entry_examples` 原样存在
- `is_primary`、`sort_order` 不变
- `entry_tags` 原样存在
- 例句本体存在
- Tag 本体存在

归档或恢复也不得删除关联。

---

# 17. CancellationToken

`SaveAsync` 必须：

- 在第一次数据库 await 前检查或传播 token
- 打开连接时传递 token
- BeginTransaction 时传递 token（API 支持时）
- ExecuteReaderAsync / ExecuteNonQueryAsync 传递 token
- CommitAsync 传递 token

预取消：

```text
OperationCanceledException
```

且：

- 不插入
- 不更新
- 不归档
- 不删除关联
- 不留下部分事务

不得捕获取消后返回成功。

---

# 18. 事务与资源管理

每次调用按需：

```text
connection
transaction
commands
reader
```

正确释放。

失败时：

```text
Rollback
→ 保留原始异常
→ Dispose
```

要求：

- 回滚异常不覆盖原异常
- 不长期缓存连接
- 不留下 WAL 写锁
- 不复用已 Dispose 的 command
- 测试后 DB/WAL/SHM 可删除

---

# 19. 数据损坏处理

读取现有时间时发现：

- NULL
- 非法格式
- 非 UTC
- UpdatedAt 早于 CreatedAt

必须失败。

不得：

- 当作不存在
- 使用默认时间
- 使用 `UtcNow`
- 自动修复数据库
- 跳过保护检查

异常不得包含：

- Headword
- NormalizedHeadword
- Definition
- Translation
- Notes

---

# 20. 日志与隐私

Repository 默认不注入 Logger。

不得记录：

- Headword
- NormalizedHeadword
- PartOfSpeech
- Phonetic
- DefinitionEnglish
- TranslationChinese
- Notes
- SQL 参数值
- 数据库行内容
- 完整连接字符串

本任务优先保持无日志依赖。

---

# 21. 测试数据库准备

测试必须使用真实临时 SQLite 文件：

```text
Migration001
→ Migration002
→ SqliteVocabularyRepository
```

不得：

- 手写简化 `vocabulary_entries` 表代替迁移
- 使用真实 user:// 数据库
- 写入仓库目录
- 依赖测试顺序
- 留下 DB/WAL/SHM

例句和标签关联种子可使用测试项目内参数化 SQL helper，或已完成 Repository。

测试 helper：

- 只在测试项目
- 使用参数化 SQL
- 不进入生产代码
- 不复制待测 SaveAsync 逻辑

---

# 22. Insert 测试

至少覆盖：

## 22.1 全字段

保存包含所有字段的词条，使用原始 SQL读取并确认：

- Id
- Headword
- NormalizedHeadword
- EntryType
- PartOfSpeech
- Phonetic
- DefinitionEnglish
- TranslationChinese
- Notes
- IsArchived
- CreatedAt
- UpdatedAt

逐字段一致。

## 22.2 Null 字段

保存所有可空字段为 null 的词条：

- 数据库必须为 NULL
- 不能变成空字符串

## 22.3 四种 EntryType

分别验证数据库整数：

```text
0, 1, 2, 3
```

## 22.4 原样文本

使用包含：

- 大小写
- 前后空格
- 撇号
- Unicode
- 换行（若 Domain 允许）

的字段，确认 Repository 不 Trim、不规范化。

`NormalizedHeadword` 也按传入值原样保存。

## 22.5 null 参数

```text
entry == null
→ ArgumentNullException
```

## 22.6 预取消

- 抛取消
- 无新记录

---

# 23. Update 测试

至少覆盖：

## 23.1 更新所有可变字段

先插入，再用相同 Id 保存更新后的 Domain 对象。

验证：

- Headword 更新
- NormalizedHeadword 更新
- EntryType 更新
- 可空文本更新
- IsArchived 更新
- UpdatedAt 更新
- CreatedAt 不变
- 行数仍为 1

## 23.2 更新为 null

将可空字段从非 null 更新为 null。

数据库必须变为 NULL。

## 23.3 从 null 更新为值

数据库必须正确更新。

## 23.4 幂等保存

相同对象、相同 UpdatedAt 重复保存：

- 成功
- 只有一行
- 无字段变化
- 关联不变化

## 23.5 CreatedAt 不一致

数据库已有相同 Id。

构造相同 Id、不同 CreatedAt 的对象并保存：

- 失败
- 数据库 CreatedAt 不变
- 所有字段不变
- 关联不变

## 23.6 UpdatedAt 倒退

数据库已有较新 UpdatedAt。

保存较旧对象：

- 失败
- 所有字段保持
- 关联保持

## 23.7 损坏现有时间

测试通过原始 SQL 将已有时间改为非法值，再保存：

- Repository 失败
- 不自动修复
- 不更新业务字段

测试完成后临时数据库删除。

---

# 24. 关联保护测试

必须覆盖：

## 24.1 例句链接

1. 保存词条
2. 保存例句
3. 建立 `entry_examples`
4. 更新词条

验证链接仍存在，且：

```text
is_primary
sort_order
```

不变。

## 24.2 标签链接

1. 保存词条
2. 保存 Tag
3. 建立 `entry_tags`
4. 更新词条

验证链接仍存在。

## 24.3 同时保护

至少一个测试同时存在例句和标签关联，再更新词条。

验证所有关联保留。

## 24.4 归档

将词条从 Active 保存为 Archived：

- 关联保留

再恢复为 Active（无唯一冲突）：

- 关联仍保留

---

# 25. 唯一约束测试

至少覆盖：

## 25.1 两个 Active 冲突

不同 Id，相同 NormalizedHeadword，均 Active：

- 第二次失败
- 数据库只有第一条
- 第一条不变化

## 25.2 Active + Archived

相同 NormalizedHeadword：

- 一个 Active
- 一个 Archived

都可存在。

## 25.3 多个 Archived

相同 NormalizedHeadword 的多个 Archived 可存在。

## 25.4 恢复冲突回滚

Archived B 恢复为 Active，但 A 已占用：

- 失败
- B 仍 Archived
- B 其他字段也保持原值
- A 不变化
- B 的关联保持

## 25.5 归档释放

A Active → Archived 后，B Active 同规范化名保存成功。

---

# 26. 事务回滚测试

除唯一约束外，还需证明更新事务完整。

推荐测试：

- 已有词条及关联
- 构造更新对象：
  - 修改 Headword
  - 修改 Notes
  - 修改 IsArchived
  - 但使用冲突的 Active NormalizedHeadword
- SaveAsync 失败

验证：

- Headword 未部分更新
- Notes 未部分更新
- IsArchived 未部分更新
- UpdatedAt 未变化
- 关联未变化

SQLite 单条 UPDATE 本身原子，但事务和保护逻辑仍必须被验证。

---

# 27. 并发基础测试

本任务不实现乐观并发版本字段。

至少验证：

## 27.1 同一活动规范化名并发插入

两个 Repository 实例：

- 不同 Id
- 相同 NormalizedHeadword
- 均 Active

并发保存。

预期：

- 恰好一个成功
- 一个因唯一约束失败
- 数据库只有一条 Active
- 无锁残留

使用合理超时，不得无限等待。

## 27.2 Stale save

较新对象先保存，较旧对象后保存：

- 后者被拒绝
- 新值保持

不要求复杂重试。

---

# 28. 文件锁与临时文件

所有测试完成后必须成功删除：

```text
.db
.db-wal
.db-shm
临时目录
```

并发测试后也必须确认连接释放。

---

# 29. 边界检查

确认：

- `SqliteVocabularyRepository` 位于 Infrastructure
- 类型为 `public sealed partial`
- 本任务未声明 `IVocabularyRepository`
- `SaveAsync` 签名与接口完全一致
- 未添加查询方法占位
- Application 未修改
- Domain 未修改
- 迁移未修改
- M1-T06/M1-T07 Repository 未修改
- Godot 未修改
- 公共 API 不暴露 SQLite 连接或事务
- 无 `IQueryable`
- 无新 Application DTO
- 无通用 Repository/UnitOfWork

---

# 30. 允许创建和修改的文件

建议创建：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteVocabularyRepository.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/
SqliteVocabularyRepositoryWriteTests.cs
```

可选内部 helper：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
VocabularyEntrySqlValues.cs
```

允许修改：

```text
tests/GameLexicon.Infrastructure.Tests/Persistence/**
（仅最小测试 helper）

docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/DECISIONS.md（仅长期架构决策变化时）
docs/SKILLS_CATALOG.md（仅 Skill Impact Review 需要时）
docs/SKILL_CHANGELOG.md（仅 Skill 实际更新时）
.agents/skills/*/SKILL.md（仅可复用流程变化时）
```

正常情况下不得修改：

```text
GameLexicon.sln
任一 .csproj
src/GameLexicon.Domain/**
src/GameLexicon.Application/**
SqliteSentenceExampleRepository.cs
SqliteTagRepository.cs
Migration001_Initial.cs
Migration002_ManualExamplesAndSearchSupport.cs
MigrationRunner.cs
SqliteConnectionFactory.cs
english-learning-project/**
tools/GameLexicon.CaptureBridge/**
```

本任务不注册到 `AppServices`。

---

# 31. 明确不做

不得实现：

- `FindByNormalizedHeadwordAsync`
- `GetDetailsAsync`
- `SearchAsync`
- `IVocabularyRepository` 接口声明
- 查询方法占位
- 详情聚合查询
- 搜索和分页 SQL
- 归档筛选查询
- 永久删除
- 例句或标签集合保存
- 跨 Repository Unit of Work
- 创建词条 UseCase
- 重复词条 UseCase
- 编辑/归档/删除 UseCase
- Godot ViewModel
- Godot Scene
- AppServices 接线
- Migration003
- FTS
- M1-T09

---

# 32. 自动验证命令

## 32.1 Infrastructure

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

## 32.2 根解决方案

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功
- 所有测试通过
- 0 错误
- 0 新增警告

## 32.3 Godot

本任务不修改 Godot：

```text
GUI verification required: No
Godot headless required: No
```

不启动 Godot Editor。

---

# 33. 代表性自动验收

最终报告必须逐项报告：

```text
Insert full entry → Pass
Insert nullable fields → Pass
Four EntryType mappings → Pass
Text preserved exactly → Pass

Update mutable fields → Pass
CreatedAt preserved → Pass
Same timestamp idempotent save → Pass
CreatedAt mismatch → Rejected / rollback
UpdatedAt regression → Rejected / rollback

Update preserves entry_examples → Pass
Update preserves entry_tags → Pass
Archive preserves all links → Pass
Restore preserves all links → Pass

Two active duplicates → Rejected
Active + archived duplicate → Allowed
Multiple archived duplicates → Allowed
Restore conflict → Rejected / archived row unchanged
Archive releases active normalized name → Pass

Concurrent active duplicate insert → Exactly one success
Pre-cancelled save → No write
DB/WAL/SHM deletable → Pass
```

不得只报告“全部测试通过”。

---

# 34. 非 GUI 人工审查

自动验收后：

```text
M1-T08 = Awaiting Manual Verification
M1-T09 = Not Started
```

人工审查重点：

1. 类型位于 Infrastructure。
2. 类型是 `public sealed partial`。
3. 本任务未提前声明完整接口。
4. `SaveAsync` 签名与接口一致。
5. 没有查询方法占位。
6. 不自行规范化文本。
7. SQL 全参数化。
8. 不使用 REPLACE。
9. CreatedAt 不会被更新。
10. UpdatedAt 不会倒退。
11. 唯一约束冲突完整回滚。
12. 例句和标签关联不会丢失。
13. CancellationToken 完整传播。
14. 无连接或文件锁泄漏。
15. 未修改迁移、Domain、Application、已有 Repository 或 Godot。
16. 未实现查询侧、UseCase 或 UI。
17. 所有测试通过。

用户确认前不得将 M1-T08 标记为 Done。

---

# 35. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且修改未确认
- 找不到提交 `bdb8a3eb...`
- M1-T07 未标记 Done
- M1-T08 不是 Not Started
- 基线构建或测试失败
- 解决方案不再是 8 个项目
- 目标框架或项目引用变化
- Migration001 或 Migration002 哈希变化
- 必须修改 Migration
- 必须修改 Domain 或 Application
- 必须修改现有 Repository
- 必须添加未实现查询占位
- 必须新增 NuGet 包
- 必须修改 Godot
- 无法保护 CreatedAt
- 无法拒绝 UpdatedAt 倒退
- 更新会破坏 entry_examples 或 entry_tags
- 测试数据库无法删除
- 用户文件可能被覆盖

停止后不得：

- 删除用户数据库
- 修改迁移历史
- `git reset --hard`
- `git clean -fd`
- 禁用 NuGet Audit
- 自动提交
- 自动执行 M1-T09

---

# 36. Git 检查

完成自动验证后：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
git diff --name-only
```

再次确认迁移哈希：

```powershell
git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration002_ManualExamplesAndSearchSupport.cs"
```

必须仍为：

```text
1fd5546081fe87c479ebd21d52e26f7d1dfaa636
d8ce250e24442ece38c231e3ae8286a4d0def4c5
```

确认：

- 生产代码只新增词条 Repository 写侧
- 测试只属于写侧
- 其余只允许状态文档
- Application 未修改
- Domain 未修改
- Migration 未修改
- 例句/标签 Repository 未修改
- Godot 未修改
- `.csproj` 未修改
- 数据库、WAL、SHM、日志、备份未进入 Git
- 暂存区为空
- 未创建提交

---

# 37. 状态与文档

自动验收通过后更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

状态：

```text
M1-T08 = Awaiting Manual Verification
M1-T09 = Not Started
```

记录：

- Task ID 和名称
- `SqliteVocabularyRepository` 写侧骨架
- 暂未声明完整接口
- SaveAsync 精确签名
- Insert/Update 算法
- CreatedAt 保护
- UpdatedAt 单调性
- EntryType 映射
- null 文本映射
- 活动唯一约束结果
- 归档/恢复结果
- 关联保护结果
- 并发测试结果
- CancellationToken 覆盖
- 新增测试数量
- Infrastructure 测试结果
- 根解决方案测试结果
- DB/WAL/SHM 删除结果
- 未修改迁移、Domain、Application、已有 Repository 和 Godot
- 已知限制

更新：

```text
docs/AGENT_HANDOFF.md
```

只有长期架构决策变化时修改：

```text
docs/DECISIONS.md
```

本任务的 partial 拆分属于阶段实现策略，通常无需 ADR。

只有环境事实变化时修改：

```text
docs/ENVIRONMENT.md
```

人工审查通过后：

```text
M1-T08 = Done
M1-T09 = Not Started
```

不得执行 M1-T09。

---

# 38. Skill Impact Review

任务结束后报告：

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
- skill-maintenance

Skill update required:
No
```

只有以下可复用流程确实变化时更新 Skill：

- Repository 分阶段 partial 实现标准
- 不可变 CreatedAt 写入保护标准
- UpdatedAt 单调持久化标准
- SQLite 关联保护测试标准
- Partial unique index 并发测试标准

普通词条写侧代码不自动构成 Skill 更新理由。

---

# 39. 自动验收清单

- [ ] 提交 `bdb8a3eb...` 存在
- [ ] 当前分支 main
- [ ] 初始工作区干净
- [ ] M1-T07 Done
- [ ] M1-T08 Not Started
- [ ] 基线 Build 成功
- [ ] 基线 246/246 测试通过
- [ ] Migration001 哈希不变
- [ ] Migration002 哈希不变
- [ ] IVocabularyRepository 未修改
- [ ] VocabularyEntry 未修改
- [ ] SqliteVocabularyRepository 创建
- [ ] 类型 public sealed partial
- [ ] 本任务未声明 IVocabularyRepository
- [ ] SaveAsync 签名完全一致
- [ ] 无查询方法占位
- [ ] 所有 SQL 参数化
- [ ] 未使用 SELECT *
- [ ] 未使用 REPLACE
- [ ] Repository 不规范化文本
- [ ] Insert 全字段成功
- [ ] null 字段保持 NULL
- [ ] 四个 EntryType 映射正确
- [ ] Update 可变字段成功
- [ ] CreatedAt 不被更新
- [ ] CreatedAt 不一致被拒绝
- [ ] UpdatedAt 倒退被拒绝
- [ ] 相同时间幂等
- [ ] entry_examples 保留
- [ ] entry_tags 保留
- [ ] Archive 保留关联
- [ ] Restore 保留关联
- [ ] 两个 Active 重复被拒绝
- [ ] Active + Archived 重复允许
- [ ] 多 Archived 重复允许
- [ ] Restore 冲突完整回滚
- [ ] Archive 释放唯一名
- [ ] 并发 Active 重复恰好一个成功
- [ ] CancellationToken 传播
- [ ] 预取消无写入
- [ ] 连接和事务释放
- [ ] DB/WAL/SHM 可删除
- [ ] 未记录学习文本
- [ ] 未修改 Domain
- [ ] 未修改 Application
- [ ] 未修改 Migration
- [ ] 未修改已有 Repository
- [ ] 未修改 Godot
- [ ] 未实现查询侧
- [ ] 未实现 UseCase/UI
- [ ] Infrastructure 测试通过
- [ ] 根解决方案构建通过
- [ ] 全部测试通过
- [ ] git diff --check 通过
- [ ] 暂存区为空
- [ ] 未创建提交
- [ ] M1-T09 未执行
- [ ] Skill Impact Review 完成

---

# 40. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T08
- 名称: SQLite 词条 Repository 写侧
- 状态:
- M1-T09 executed: No
- Git commit created: No
- GUI verification required: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M1-T07 commit:
- Branch:
- Initial Git status:
- Solution projects:
- Target frameworks:
- Baseline build:
- Baseline tests:
- Migration001 hash:
- Migration002 hash:

## Repository 写侧

- Type:
- Partial:
- Implements IVocabularyRepository now:
- Constructor:
- SaveAsync signature:
- Query stubs:
- Connection policy:
- Transaction policy:
- Cancellation coverage:

## 写入语义

- Insert:
- Update:
- CreatedAt protection:
- UpdatedAt monotonicity:
- EntryType mapping:
- Nullable fields:
- Text normalization in Repository:

## SQL

- Parameterized:
- SELECT * used:
- REPLACE used:
- Insert columns:
- Updated columns:
- Id updated:
- CreatedAt updated:

## 唯一约束

- Active duplicate:
- Active + archived:
- Multiple archived:
- Restore conflict:
- Archive releases name:
- Concurrent active duplicate:

## 关联保护

- entry_examples preserved:
- is_primary/sort_order preserved:
- entry_tags preserved:
- Archive/restore links preserved:

## 代表案例

| Case | Actual | Expected | Result |
|---|---|---|---|
| ... | ... | ... | Pass |

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
- Concurrent test:
- DB/WAL/SHM deletion:

## 边界检查

- IVocabularyRepository modified:
- VocabularyEntry modified:
- Migrations modified:
- Sentence repository modified:
- Tag repository modified:
- Godot modified:
- Query side:
- UseCases/UI:

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

## 人工审查

- Awaiting user review.
- No GUI run is required.

## 已知限制

- Type does not implement the complete interface until M1-T09.
- No FindByNormalizedHeadword/GetDetails/Search.
- No aggregate save transaction for tags/examples.
- No Application duplicate-decision workflow.
- No UseCase or UI.

## 下一任务

- M1-T09：SQLite 查询与生命周期
- Status: Not Started
- Not automatically executed
```

---

# 41. 可直接执行的总指令

请执行：

```text
M1-T08：SQLite 词条 Repository 写侧
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T08_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `bdb8a3ebc05762c5a0f52088e90246f20fd2739d`。
2. 开始时 Git 工作区必须干净。
3. 创建 `public sealed partial class SqliteVocabularyRepository`。
4. 本任务暂不声明完整 `IVocabularyRepository`。
5. 只实现与接口精确一致的公开 `SaveAsync`。
6. 不添加查询方法占位或未实现异常。
7. 不修改 `IVocabularyRepository`、VocabularyEntry 或 EntryType。
8. 使用 `SqliteConnectionFactory`，不长期持有连接。
9. SQL 全参数化，禁止 `SELECT *`。
10. 禁止 `INSERT OR REPLACE` 和 `REPLACE INTO`。
11. Repository 不 Trim、不规范化、不自动更新时间。
12. Insert 保存全部字段。
13. Update 只更新可变字段和 UpdatedAt。
14. Update 不得修改 Id 或 CreatedAt。
15. 传入 CreatedAt 与数据库不一致时完整拒绝。
16. UpdatedAt 早于数据库值时完整拒绝。
17. 相同 UpdatedAt 允许幂等保存。
18. 正确映射四种 EntryType、nullable 文本和 IsArchived。
19. 依赖现有活动词头 partial unique index。
20. 活动重复冲突必须失败且完整回滚。
21. Active + Archived 和多个 Archived 同名必须允许。
22. Restore 冲突必须保留原归档状态及字段。
23. Archive 后应释放活动唯一名。
24. 更新、归档和恢复不得删除 `entry_examples` 或 `entry_tags`。
25. 必须测试 `is_primary`、`sort_order` 和标签关联保持。
26. 并发 Active 重复保存恰好一个成功。
27. CancellationToken 必须完整传播。
28. 测试 DB/WAL/SHM 必须可删除。
29. 不修改 Migration001、Migration002、现有例句/标签 Repository、Domain、Application、Godot、项目引用或目标框架。
30. 不实现查询侧、UseCase 或 UI。
31. 不新增 NuGet 包。
32. 不执行 M1-T09。
33. 不创建 Git 提交。
34. 自动验收后保持 Awaiting Manual Verification。
35. 本任务不需要 GUI 验收。
36. 完成后执行 Git diff、状态文档更新和 Skill Impact Review。
