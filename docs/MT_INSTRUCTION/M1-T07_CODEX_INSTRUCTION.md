# M1-T07 Codex 执行指令

## 任务名称

```text
M1-T07：SQLite 标签 Repository
```

保存到：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T07_CODEX_INSTRUCTION.md
```

本任务只实现：

```text
SqliteTagRepository
ITagRepository 四个既有方法
标签精确查找
并发安全的 GetOrCreate
按词条读取标签
词条标签集合原子替换
Infrastructure 自动化测试
```

本任务不实现：

- `SqliteVocabularyRepository`
- 标签重命名、删除、合并或搜索
- Application UseCase
- Godot 接线或 UI
- Migration003
- M1-T08 或任何后续任务

---

# 1. 已确认基线

最新提交：

```text
8c9233ae23b3249f0b6ac7dec8dbae56bf54e92c
```

已知状态：

- 分支：`main`
- 工作区干净
- M1-T06 = `Done`
- M1-T07 = `Not Started`
- 无 Godot 残留进程
- 根解决方案 8 个项目
- Godot 桌面 `net8.0`，Android 条件目标 `net9.0`
- Domain、Application、Infrastructure：`net8.0`
- 三个测试项目和 CaptureBridge：`net10.0`
- Build：0 警告、0 错误
- Tests：230/230
  - Domain 111
  - Application 61
  - Infrastructure 58
- Migration001 哈希：
  `1fd5546081fe87c479ebd21d52e26f7d1dfaa636`
- Migration002 哈希：
  `d8ce250e24442ece38c231e3ae8286a4d0def4c5`
- `ITagRepository` 四个方法完整
- Application 公共 API 未泄漏 SQLite、Godot、Infrastructure 或 `IQueryable`
- 数据库、WAL/SHM、日志、备份及构建产物未进入 Git

开始时仍须重新核验。

---

# 2. 固定接口契约

不得修改现有接口：

```csharp
public interface ITagRepository
{
    Task<Tag?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken);

    Task<Tag> GetOrCreateAsync(
        Tag candidate,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Tag>> GetForEntryAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    Task SetForEntryAsync(
        Guid entryId,
        IReadOnlyList<Guid> tagIds,
        CancellationToken cancellationToken);
}
```

既定语义：

- `FindByNormalizedNameAsync`：调用方已经规范化；Repository 精确查找，不再规范化；找不到返回 `null`。
- `GetOrCreateAsync`：同一 `NormalizedName` 已存在时返回已有 Tag，不覆盖已有 Name；不存在时保存 candidate；并发冲突后仍返回唯一 Tag。
- `GetForEntryAsync`：无关联返回空列表；按 `NormalizedName ASC`、`Id ASC` 稳定排序。
- `SetForEntryAsync`：原子替换整个关联集合；空列表清空；重复 ID、`Guid.Empty`、不存在的 Entry 或 Tag 必须失败；不创建 Tag，不删除未使用 Tag。

---

# 3. 当前数据库结构

必须使用现有表：

```sql
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
```

现有索引：

```sql
CREATE INDEX ix_entry_tags_tag_entry
ON entry_tags(tag_id, entry_id);
```

不得新增 Migration、字段、索引、FTS、别名表或孤儿清理表。

---

# 4. 必须阅读

完整阅读：

```text
AGENTS.md
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
docs/ENVIRONMENT.md
docs/DECISIONS.md
docs/AGENT_HANDOFF.md
docs/MT_INSTRUCTION/M1-T07_CODEX_INSTRUCTION.md

src/GameLexicon.Domain/Entries/Tag.cs
src/GameLexicon.Application/Abstractions/Persistence/ITagRepository.cs
src/GameLexicon.Infrastructure/Persistence/SqliteConnectionFactory.cs
src/GameLexicon.Infrastructure/Persistence/Repositories/SqliteSentenceExampleRepository.cs
src/GameLexicon.Infrastructure/Persistence/Migrations/Migration001_Initial.cs
src/GameLexicon.Infrastructure/Persistence/Migrations/Migration002_ManualExamplesAndSearchSupport.cs
tests/GameLexicon.Infrastructure.Tests/Persistence/**
```

读取现有 Skills：

```text
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain: Infrastructure / Persistence / Tags
Primary writer: primary coordinator
Supporting agent: milestone architect（只读复核）
```

---

# 5. 阶段 0：基线复核

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline 8c9233ae23b3249f0b6ac7dec8dbae56bf54e92c
git diff --check

dotnet sln GameLexicon.sln list

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration002_ManualExamplesAndSearchSupport.cs"

dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

必须确认：

- 分支 main
- 工作区干净
- 8 个项目
- 目标框架无变化
- M1-T06 Done
- M1-T07 Not Started
- 两个迁移哈希不变
- Build 0/0
- Tests 230/230

基线不满足时停止，不修改、不恢复、不提交。

---

# 6. 创建 `SqliteTagRepository`

建议路径：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteTagRepository.cs
```

必须：

```csharp
public sealed class SqliteTagRepository : ITagRepository
```

构造函数只依赖：

```csharp
SqliteConnectionFactory
```

要求：

- null factory → `ArgumentNullException`
- 不长期持有连接
- 每个方法按需打开连接
- 正确释放 connection、transaction、command、reader
- 不依赖 Godot、View、UseCase
- 不缓存标签
- 不记录标签文本

可选创建 `internal TagSqlMapper`，但不得形成通用 ORM，也不得修改 M1-T06 Repository。

---

# 7. 数据映射

GUID：

```csharp
guid.ToString("D").ToLowerInvariant()
```

读取无效 GUID：

- 视为数据库损坏
- 抛 `InvalidDataException` 或项目等价异常
- 不生成新 ID
- 不返回 `Guid.Empty`
- 异常不包含原始坏值

文本字段 `name`、`normalized_name` 必须原样读写。

Repository 不得：

```text
Trim
ToLower
Form KC
压缩空白
调用 ITextNormalizer
从 Name 推导 NormalizedName
从 NormalizedName 改写 Name
```

数据库中 NULL、空白 Name 或空白 NormalizedName 必须视为损坏，不得静默修复。

---

# 8. `FindByNormalizedNameAsync`

参数：

```text
null → ArgumentNullException
空或纯空白 → ArgumentException
```

不得 Trim。

SQL 必须显式列：

```sql
SELECT
    id,
    name,
    normalized_name
FROM tags
WHERE normalized_name = @normalized_name;
```

禁止：

```text
SELECT *
COLLATE NOCASE
LOWER(...)
TRIM(...)
LIKE
```

语义：

- 精确匹配
- 找不到返回 null
- 不启动写事务
- 传播 CancellationToken

`"quest"` 与 `"Quest"`、`" quest "` 不应被 Repository 自动视为相同。

---

# 9. `GetOrCreateAsync`

参数：

```text
candidate == null → ArgumentNullException
```

candidate 已由 Domain 验证。

必须在写事务中使用指定冲突目标：

```sql
INSERT INTO tags (
    id,
    name,
    normalized_name
)
VALUES (
    @id,
    @name,
    @normalized_name
)
ON CONFLICT(normalized_name) DO NOTHING;
```

随后在同一事务内精确读取：

```sql
SELECT
    id,
    name,
    normalized_name
FROM tags
WHERE normalized_name = @normalized_name;
```

要求：

- 不存在时插入 candidate
- 已存在时返回已有 Tag
- 不覆盖已有 Name
- 不覆盖已有 Id
- 结果不存在时抛明确异常
- Commit 后返回持久化结果

禁止：

```text
INSERT OR IGNORE
INSERT OR REPLACE
REPLACE INTO
ON CONFLICT DO UPDATE name = ...
```

原因：

- `INSERT OR IGNORE` 会隐藏主键等无关错误
- REPLACE 可能删除 Tag 并级联删除 `entry_tags`
- 覆盖 Name 违反既定契约

如果 candidate.Id 与另一个不同标签冲突：

- 必须失败
- 不生成新 ID
- 不返回错误标签
- 不把它当 normalized-name 冲突忽略

并发要求：

两个 Repository 实例，以不同 Id/Name、相同 NormalizedName 并发调用后：

- 数据库只有一行
- 两次调用都返回同一持久化 Tag
- 返回 Id、Name 一致
- 不出现未处理的 normalized-name 唯一冲突

依赖现有 WAL、busy timeout 和 UNIQUE，不增加复杂重试框架。

---

# 10. `GetForEntryAsync`

参数：

```text
entryId == Guid.Empty → ArgumentException
```

查询：

```sql
SELECT
    tags.id,
    tags.name,
    tags.normalized_name
FROM entry_tags
INNER JOIN tags
    ON tags.id = entry_tags.tag_id
WHERE entry_tags.entry_id = @entry_id
ORDER BY
    tags.normalized_name ASC,
    tags.id ASC;
```

语义：

- Entry 不存在 → 空列表
- Entry 存在但无标签 → 空列表
- 永不返回 null
- 返回只读、防御性复制的列表
- 不自动创建、删除、修复或规范化 Tag
- 不依赖默认行顺序

---

# 11. `SetForEntryAsync`

这是整个标签集合的原子替换。

## 11.1 第一次 await 前验证

必须：

1. `entryId != Guid.Empty`
2. `tagIds != null`
3. 复制 `tagIds` 形成调用时快照
4. 每个 ID 非 `Guid.Empty`
5. 不得包含重复 ID

重复 ID 必须抛参数异常。

禁止静默：

```text
Distinct()
HashSet 去重后继续
```

## 11.2 空列表

空列表表示清空：

```text
验证 Entry 存在
→ 删除该 Entry 的全部 entry_tags
→ Commit
```

Entry 不存在时仍必须失败，不能把空列表当成功。

## 11.3 原子替换算法

同一事务内：

```text
1. 验证 Entry 存在
2. 验证所有 Tag 存在
3. 删除该 Entry 的全部旧关联
4. 插入全部新关联
5. Commit
```

示例：

```text
[A, B] → [B, C]
最终必须恰好为 [B, C]
```

不得：

- 只追加
- 自动创建缺失 Tag
- 删除未使用 Tag
- 修改 Tag Name
- 影响其他 Entry
- 给 entry_tags 虚构排序语义

## 11.4 缺失 Entry 或 Tag

缺失 Entry 或任何 Tag：

- 抛 `KeyNotFoundException` 或项目已有稳定异常
- 整体失败
- 原集合保持
- 不部分插入
- 不自动创建

## 11.5 删除后插入失败的回滚

必须测试真实中途失败。

推荐测试数据库 trigger：

```sql
CREATE TRIGGER fail_m1_t07_entry_tag_insert
BEFORE INSERT ON entry_tags
BEGIN
    SELECT RAISE(ABORT, 'm1-t07-test');
END;
```

测试：

```text
原集合 [A, B]
请求 [B, C]
旧关联已删除后 INSERT 失败
事务回滚
最终仍为 [A, B]
```

Trigger 只能存在于测试临时数据库；不得加入迁移，不得为测试修改生产 API。

## 11.6 并发

不实现乐观并发版本号。

SQLite 串行化写事务后：

```text
最后一个成功提交的 SetForEntryAsync 生效
```

不自动合并并发集合。

---

# 12. SQL 参数化

所有值必须使用参数。

禁止把 GUID、Name、NormalizedName 拼进 SQL。

动态参数名只能由整数索引生成，值仍通过参数传入。

MVP 推荐：

- 逐个验证 Tag 存在
- 逐个插入 entry_tags

避免超过 SQLite 参数限制。

---

# 13. 事务、资源和取消

读取方法：

- 按需打开连接
- 不需要写事务
- 正确 Dispose command/reader

写入方法：

```text
Open connection
→ BeginTransaction
→ Execute
→ Commit
```

失败：

```text
Rollback
→ 保留原异常
→ 释放全部资源
```

回滚异常不得覆盖原异常。

所有异步调用传递 `CancellationToken`：

- OpenAsync
- BeginTransactionAsync（API 支持时）
- ExecuteReaderAsync
- ExecuteNonQueryAsync
- 读取循环检查
- CommitAsync

预取消应抛 `OperationCanceledException`，且无部分写入。

---

# 14. 日志与隐私

Repository 默认不注入 Logger。

不得记录：

- Tag.Name
- Tag.NormalizedName
- SQL 参数
- 数据库行内容
- 完整连接字符串

---

# 15. 自动化测试环境

测试必须使用真实临时 SQLite 文件：

```text
Migration001
→ Migration002
→ SqliteTagRepository
```

不得手写简化 schema 代替迁移。

不得使用真实 `user://` 数据库或仓库目录。

词条 Repository 尚未实现，允许使用测试项目内参数化 SQL helper 创建 `vocabulary_entries` 种子。

测试结束必须删除：

```text
.db
-wal
-shm
临时目录
```

---

# 16. 必测：Find

至少覆盖：

1. 存在时返回完整 Tag
2. 不存在返回 null
3. 精确大小写，不自动 NOCASE
4. 不 Trim
5. null 拒绝
6. 空字符串拒绝
7. 纯空白拒绝
8. 无效数据库 GUID 拒绝
9. 空白 Name/NormalizedName 视为损坏
10. 预取消
11. 调用后连接释放

---

# 17. 必测：GetOrCreate

至少覆盖：

1. 不存在时插入 candidate
2. 返回字段与数据库一致
3. 同 candidate 重复调用幂等
4. 相同 NormalizedName、不同 candidate 时返回已有 Tag
5. 已有 Name 不被覆盖
6. 数据库始终只有一行
7. 两个 Repository 实例真实并发，最终返回同一 Tag
8. candidate.Id 与无关标签主键冲突时失败
9. 主键冲突不创建第二标签
10. null candidate 拒绝
11. 预取消不插入
12. 事务失败无部分数据

并发测试不得用串行调用冒充。

---

# 18. 必测：GetForEntry

至少覆盖：

1. Entry 不存在返回空列表
2. Entry 无标签返回空列表
3. 多标签按 NormalizedName、Id 稳定排序
4. 完整 Tag 字段
5. 返回集合不可修改
6. 同一 Tag 可关联多个 Entry
7. 不包含其他 Entry 的标签
8. Guid.Empty 拒绝
9. 数据库坏 Guid 拒绝
10. 数据库坏 Name/NormalizedName 拒绝
11. 预取消

正常 schema 下 NormalizedName 唯一，因此二级 Id 排序可通过 SQL 审查确认，不得破坏唯一约束制造非法正常数据。

---

# 19. 必测：SetForEntry

至少覆盖：

1. `[] → [A, B]`
2. `[A, B] → [B, C]`
3. 相同集合重复设置幂等
4. 输入顺序不同但集合结果一致
5. `[A, B] → []` 清空关联
6. 清空后 tags 行仍保留
7. `[A, A]` 在修改前拒绝
8. 含 `Guid.Empty` 在修改前拒绝
9. null 列表拒绝
10. Entry 不存在整体失败，包括空列表
11. `[A, Missing]` 整体失败，原集合保持
12. Missing Tag 不被创建
13. trigger 造成删除后插入失败，原集合恢复
14. 其他 Entry 的关联不变
15. 未使用 Tag 不被删除
16. 预取消无修改
17. 输入在调用后被修改不影响快照，或通过代码审查确认快照在第一次 await 前完成

不得为中途取消测试添加生产延迟或故障钩子。

---

# 20. 数据库约束测试

确认：

- `normalized_name UNIQUE` 生效
- `entry_tags(entry_id, tag_id)` 复合主键生效
- 删除 Entry 级联删除 entry_tags，但保留 Tag
- 直接删除 Tag 级联删除对应 entry_tags

Repository 本任务不提供 DeleteTag。

---

# 21. 边界检查

确认：

- Repository 位于 Infrastructure
- 实现现有 `ITagRepository`
- Application 接口未修改
- Domain 未引用 Infrastructure
- Application 未引用 Infrastructure
- 公共 API 不暴露 SQLite 类型
- 无 Godot 类型
- 无 `IQueryable`
- 无新公共 DTO
- 无通用 `IRepository<T>`
- 无 Unit of Work

---

# 22. 允许修改范围

建议新增：

```text
src/GameLexicon.Infrastructure/Persistence/Repositories/
SqliteTagRepository.cs

tests/GameLexicon.Infrastructure.Tests/Persistence/Repositories/
SqliteTagRepositoryTests.cs
```

可选新增：

```text
internal TagSqlMapper.cs
```

允许修改：

```text
tests/GameLexicon.Infrastructure.Tests/Persistence/**
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
```

仅在确有长期决策或 Skill 变化时修改：

```text
docs/DECISIONS.md
docs/SKILLS_CATALOG.md
docs/SKILL_CHANGELOG.md
.agents/skills/*/SKILL.md
```

正常情况下不得修改：

```text
GameLexicon.sln
任一 .csproj
src/GameLexicon.Domain/**
src/GameLexicon.Application/**
SqliteSentenceExampleRepository.cs
Migration001_Initial.cs
Migration002_ManualExamplesAndSearchSupport.cs
MigrationRunner.cs
SqliteConnectionFactory.cs
english-learning-project/**
tools/GameLexicon.CaptureBridge/**
```

本任务不注册到 AppServices。

---

# 23. 明确不做

不得实现：

```text
SqliteVocabularyRepository
词条保存、搜索或分页
标签 Search/Rename/Delete/Merge
孤儿 Tag 清理
UseCase
ViewModel
Godot UI
Migration003
FTS
M1-T08
```

---

# 24. 自动验证命令

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Infrastructure.Tests/GameLexicon.Infrastructure.Tests.csproj `
  --no-build `
  --no-restore

dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功
- 所有测试通过
- 0 错误
- 0 新增警告

本任务无需 GUI 或 Godot headless；不要启动 Godot Editor。

---

# 25. 最终报告必须逐项给出

```text
Find exact normalized name → Pass
Find missing → null
Find does not trim → Confirmed
Find does not normalize case → Confirmed

GetOrCreate inserts candidate → Pass
GetOrCreate returns existing → Pass
Existing display Name preserved → Pass
Repeated call → Idempotent
Concurrent same normalized name → One row / same Tag
Unrelated primary-key conflict → Rejected

GetForEntry missing Entry → Empty
GetForEntry no tags → Empty
GetForEntry stable order → Pass

SetForEntry initial set → Pass
SetForEntry full replacement → Pass
SetForEntry empty clears links → Pass
Duplicate IDs → Rejected before mutation
Missing Tag → Full rollback
Missing Entry → Rejected
Failure after delete → Original set restored
Other entries unchanged → Pass
Unused Tag rows remain → Pass

Cancellation leaves no partial change → Pass
DB/WAL/SHM deletable → Pass
```

不得只报告“测试全部通过”。

---

# 26. 状态与人工审查

自动验收通过后：

```text
M1-T07 = Awaiting Manual Verification
M1-T08 = Not Started
```

更新：

```text
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
```

记录：

- 四个方法
- 精确查找和不规范化策略
- GetOrCreate 冲突目标和并发结果
- 已有 Name 保留
- SetForEntry 原子替换算法
- 空列表、重复 ID、Missing Entry/Tag 策略
- trigger 故障回滚结果
- CancellationToken 覆盖
- 新增测试数量
- Infrastructure 和根测试总数
- DB/WAL/SHM 删除结果
- 未修改迁移、Domain、Application、Godot
- 已知限制

人工确认前不得标记 Done。

本任务 GUI 验收不适用。

---

# 27. Skill Impact Review

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
- skill-maintenance

Skill update required: No
```

只有产生可复用的新流程时才更新 Skill，例如指定唯一冲突 GetOrCreate 模板、并发测试标准或原子集合替换故障注入模板。

---

# 28. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且修改未确认
- 找不到提交 `8c9233ae...`
- M1-T06 未 Done
- M1-T07 不是 Not Started
- 基线构建或测试失败
- 解决方案不再是 8 个项目
- 目标框架或引用变化
- Migration 哈希变化
- 必须修改 Migration、ITagRepository 或 Tag
- 必须新增 NuGet 包
- 必须修改 Godot
- 无法实现并发唯一 GetOrCreate
- 必须使用 REPLACE
- 无法保证 SetForEntry 原子回滚
- 测试数据库无法删除
- 用户文件可能被覆盖

停止后不得：

```text
删除用户数据库
修改迁移历史
git reset --hard
git clean -fd
禁用 NuGet Audit
自动提交
执行 M1-T08
```

---

# 29. Git 最终检查

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
git diff --name-only

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration001_Initial.cs"

git hash-object `
  "src\GameLexicon.Infrastructure\Persistence\Migrations\Migration002_ManualExamplesAndSearchSupport.cs"
```

必须确认：

- 两个迁移哈希不变
- 生产代码只新增标签 Repository
- 测试只属于标签 Repository
- 其余只允许状态文档
- Application、Domain、M1-T06 Repository、Migration、Godot、项目文件未修改
- 数据库、WAL/SHM、日志、备份、构建产物未进入 Git
- 暂存区为空
- 未创建提交
- M1-T08 未执行

---

# 30. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T07
- 名称: SQLite 标签 Repository
- 状态:
- M1-T08 executed: No
- Git commit created: No
- GUI verification required: No

## 前置基线

- M1-T06 commit:
- Branch:
- Initial Git status:
- Baseline build:
- Baseline tests:
- Migration001 hash:
- Migration002 hash:

## Repository 实现

- Type:
- Interface:
- Constructor:
- Connection policy:
- Transaction policy:
- Cancellation coverage:

## 方法语义

- FindByNormalizedNameAsync:
- GetOrCreateAsync:
- GetForEntryAsync:
- SetForEntryAsync:

## SQL 与映射

- Parameterized:
- SELECT * used:
- Normalization in Repository:
- Exact comparison:
- GetOrCreate conflict target:
- INSERT OR IGNORE used:
- REPLACE used:
- GUID format:
- Corrupt data behavior:

## 并发 GetOrCreate

- Test setup:
- Row count:
- Returned IDs:
- Existing Name policy:
- Result:

## 原子替换

- Input snapshot:
- Duplicate validation:
- Empty set:
- Missing entry:
- Missing tag:
- Failure injection:
- Rollback result:
- Other entries unchanged:
- Unused tags retained:

## 创建和修改的文件

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

- ITagRepository modified:
- Tag modified:
- Migrations modified:
- Sentence repository modified:
- Godot modified:
- Vocabulary repository:
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

- No tag rename/delete/merge/search.
- No orphan-tag cleanup.
- No vocabulary repository.
- No UseCase or UI.
- Last successful SetForEntry wins under concurrent writes.

## 下一任务

- M1-T08：SQLite 词条 Repository 写侧
- Status: Not Started
- Not automatically executed
```

---

# 31. 可直接执行的总指令

请执行：

```text
M1-T07：SQLite 标签 Repository
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T07_CODEX_INSTRUCTION.md
```

特别要求：

1. 先核验提交 `8c9233ae23b3249f0b6ac7dec8dbae56bf54e92c`。
2. 开始时工作区必须干净。
3. 只实现 `SqliteTagRepository` 和 Infrastructure 测试。
4. 不修改 `ITagRepository` 或 Domain `Tag`。
5. 所有 SQL 参数化，禁止 `SELECT *`。
6. Repository 不 Trim、不改大小写、不调用文本规范化。
7. Find 使用 `normalized_name = @normalized_name` 精确查找。
8. GetOrCreate 使用 `ON CONFLICT(normalized_name) DO NOTHING`。
9. 禁止 `INSERT OR IGNORE`、`INSERT OR REPLACE`、`REPLACE INTO`。
10. 已有相同规范化名时返回已有 Tag，不覆盖 Name。
11. 并发相同规范化名最终只能有一行，两个调用返回同一 Tag。
12. candidate 主键与无关标签冲突时必须失败。
13. GetForEntry 按 NormalizedName、Id 稳定排序。
14. SetForEntry 在第一次 await 前复制并验证输入。
15. 重复 Tag ID 必须拒绝，不能静默去重。
16. 空列表表示清空。
17. SetForEntry 必须验证 Entry 和全部 Tag 存在。
18. 不自动创建缺失 Tag，不删除未使用 Tag。
19. 删除旧关联后插入失败必须完整回滚。
20. 使用测试 trigger 验证中途失败回滚，不添加生产故障钩子。
21. CancellationToken 必须完整传播。
22. DB/WAL/SHM 必须可删除。
23. 不修改 Migration001、Migration002、M1-T06 Repository、Domain、Application、Godot、项目引用或目标框架。
24. 不实现词条 Repository、UseCase 或 UI。
25. 不新增 NuGet 包。
26. 不执行 M1-T08。
27. 不创建 Git 提交。
28. 自动验收后保持 Awaiting Manual Verification。
29. 本任务不需要 GUI 验收。
30. 完成后执行 Git diff、状态文档更新和 Skill Impact Review。
