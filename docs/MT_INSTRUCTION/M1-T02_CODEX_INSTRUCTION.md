# M1-T02 Codex 执行指令

## 任务名称

```text
M1-T02：文本规范化
```

建议保存为：

```text
D:\UGit\EnglishLearningProject\docs\MT_INSTRUCTION\M1-T02_CODEX_INSTRUCTION.md
```

本任务只实现英文表达的规范化服务：

```text
ITextNormalizer
EnglishExpressionNormalizer
Domain 单元测试
```

本任务不实现：

- 句子切分。
- 目标范围重新定位。
- Repository。
- CRUD。
- 数据库存取。
- 手工添加词条 UI。
- 搜索。
- OCR。
- 截图。
- 复习。
- M1-T03 或其他后续任务。

---

# 1. 已确认的前置基线

用户已确认最新提交：

```text
8849f987c919faa09d52c2413b9ccd9a221627c9
```

当前已知状态：

- 当前分支：`main`
- Git 工作区干净。
- M1-T01 提交内容完整。
- M1-T01 为 `Done`。
- M1-T02 为 `Not Started`。
- 当前无 Godot 编辑器或残留 Godot 进程。
- 根解决方案包含 8 个项目。
- Godot、Domain、Application、Infrastructure 为 `net8.0`。
- 测试项目和 CaptureBridge 保持既有 `net10.0`。
- 基线 Restore 成功。
- 基线 Build 成功，0 警告、0 错误。
- 基线 Test 成功，35/35 通过。
- NuGet Audit 保持启用。
- 沙箱内首次 Restore 曾因网络权限出现 `NU1301`，获准在沙箱外重试后成功。
- 数据库、日志、`.godot/`、`bin/`、`obj/` 未进入 Git。

Codex 开始时仍须重新核验，不得只依赖本文件。

---

# 2. 产品规格依据

产品规格将 M1-T02 定义为：

```text
完成：
- ITextNormalizer
- EnglishExpressionNormalizer

验收：
- 通过产品规格第 27 节中的规范化测试
```

用于重复检测的 `NormalizedHeadword` 必须遵循：

1. Unicode 规范化为 Form KC。
2. 转为小写。
3. 首尾 Trim。
4. 连续空白折叠为一个空格。
5. 将弯引号统一为直引号。
6. 去除首尾不属于表达的句读符号。
7. 保留内部撇号和连字符。
8. 不做词干化。
9. 不自动把短语拆成单词。

规格中的最低测试：

```text
" Get   Out! " → "get out"
"Don't"        → "don't"
"well-known"   → "well-known"
```

本任务只实现“英文表达规范化”，不把产品规格第 11.2 节的句子切分或第 11.3 节的短语范围重定位提前并入本任务。

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

## 3.3 Domain 项目

```text
D:\UGit\EnglishLearningProject\src\GameLexicon.Domain\GameLexicon.Domain.csproj
```

## 3.4 Domain 测试项目

```text
D:\UGit\EnglishLearningProject\tests\GameLexicon.Domain.Tests\GameLexicon.Domain.Tests.csproj
```

## 3.5 Godot 工程

```text
D:\UGit\EnglishLearningProject\english-learning-project
```

M1-T02 正常情况下不修改 Godot 工程。

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
docs/MT_INSTRUCTION/M1-T02_CODEX_INSTRUCTION.md
```

重点读取：

```text
PRODUCT_SPEC.md
- 第 3.1 节 F06：单词和短语选择
- 第 7.3 节 Domain 层职责
- 第 10 节领域模型
- 第 11.1 节词头规范化
- 第 27.1 节文本规范化测试
- 第 31 节 M1-T02
```

如存在以下文件，也必须读取：

```text
docs/AGENT_SYSTEM.md
docs/SKILLS_CATALOG.md
.agents/skills/project-routing/SKILL.md
.agents/skills/milestone-workflow/SKILL.md
.agents/skills/skill-maintenance/SKILL.md
```

任务路由：

```text
Primary domain:
Domain / Text

Primary writer:
主协调 Agent

Supporting agents:
- milestone_architect：只读审查规格范围和验收
- skill_curator：仅在收尾 Skill 影响审查需要时调用
```

本任务通常不需要 `godot_specialist`，因为不修改 Godot 文件、不调用 Godot API。

---

# 5. 阶段 0：重新核验基线

## 5.1 Git

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git branch --show-current
git log -3 --oneline
git show --stat --oneline 8849f987c919faa09d52c2413b9ccd9a221627c9
git diff --check
```

必须确认：

- 当前分支为 `main`。
- 工作区干净。
- 提交 `8849f987...` 存在。
- 提交包含 M1-T01 的 SQLite 连接、迁移、测试和状态文档。
- 没有未确认的用户修改。

工作区不干净时：

1. 立即停止。
2. 列出所有修改和未跟踪文件。
3. 不恢复。
4. 不覆盖。
5. 不暂存。
6. 不提交。

## 5.2 状态文档

确认：

```text
M1-T01 = Done
M1-T02 = Not Started
```

状态不一致时停止。

## 5.3 解决方案和框架

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

- 根解决方案仍有 8 个项目。
- Domain 为 `net8.0`。
- Domain.Tests 保持既有 `net10.0`。
- 本任务不修改任一目标框架。

## 5.4 基线构建与测试

优先执行无需网络的验证：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

仅当依赖资产确实缺失时才执行：

```powershell
dotnet restore GameLexicon.sln
```

规则：

- 本任务不新增 NuGet 包，通常不需要网络。
- 不禁用 NuGet Audit。
- 沙箱网络限制导致 Restore 失败时，不应误判为文本规范化代码失败。
- 获准后可在沙箱外重试 Restore。
- 基线 Build 或 Test 失败时停止，不把既有问题混入 M1-T02。

---

# 6. 分层与放置规则

文本规范化属于纯领域规则，应放在：

```text
GameLexicon.Domain
```

建议目录：

```text
src/GameLexicon.Domain/
└─ Text/
   ├─ ITextNormalizer.cs
   └─ EnglishExpressionNormalizer.cs
```

测试：

```text
tests/GameLexicon.Domain.Tests/
└─ Text/
   └─ EnglishExpressionNormalizerTests.cs
```

禁止放在：

```text
GameLexicon.Infrastructure
GameLexicon.Application
Godot scripts
```

理由：

- 规则不依赖数据库。
- 规则不依赖 Godot。
- 规则不依赖 Windows。
- 后续 Repository 和 UseCase 只消费规范化结果。

不得为本任务新增项目引用。

---

# 7. `ITextNormalizer`

创建：

```text
src/GameLexicon.Domain/Text/ITextNormalizer.cs
```

建议最小接口：

```csharp
namespace GameLexicon.Domain.Text;

public interface ITextNormalizer
{
    string Normalize(string value);
}
```

要求：

- 输入为英文单词、短语或固定表达。
- 返回用于比较、去重和索引的规范化键。
- 不修改调用方原字符串。
- 不包含异步 API。
- 不依赖文化区设置。
- 不依赖 Godot、SQLite、Regex 第三方包或外部服务。

## 7.1 Null 和空白约定

统一采用：

```text
null → ArgumentNullException
"" → ""
仅空白 → ""
```

如果现有项目明确采用其他空值约定，Codex 必须先报告证据；没有证据时使用上述约定。

不得将 `null` 静默变成空字符串。

---

# 8. `EnglishExpressionNormalizer`

创建：

```text
src/GameLexicon.Domain/Text/EnglishExpressionNormalizer.cs
```

建议：

```csharp
namespace GameLexicon.Domain.Text;

public sealed class EnglishExpressionNormalizer : ITextNormalizer
{
    public string Normalize(string value)
    {
        // Minimal deterministic implementation.
    }
}
```

要求：

- 无可变全局状态。
- 可重复调用。
- 线程安全。
- 相同输入始终得到相同输出。
- 不访问文件、数据库、日志或网络。
- 不记录输入文本，避免后续把学习文本写入日志。
- 不使用当前系统文化执行大小写转换。

---

# 9. 规范化管线与执行顺序

使用以下确定顺序：

```text
1. 检查 null
2. Unicode Form KC
3. 统一弯单引号为 ASCII '
4. 使用 Invariant 小写
5. 折叠连续 Unicode 空白
6. Trim 首尾空白
7. 去除首尾 Unicode 句读符号
8. 再次 Trim 首尾空白
9. 返回结果
```

去除首尾标点后再次 Trim，是为了处理：

```text
"  ( Get out! )  "
```

标点与表达之间可能存在空格的情况。

实现必须是幂等的：

```text
Normalize(Normalize(value)) == Normalize(value)
```

---

# 10. Unicode Form KC

必须使用：

```csharp
value.Normalize(NormalizationForm.FormKC)
```

作用包括统一兼容字符，例如：

```text
Ｇｅｔ　Ｏｕｔ
→ Get Out
```

要求：

- Form KC 在小写和空白处理之前执行。
- 不改用 Form D、Form C 或 Form KD。
- 不自行删除变音符号。
- 不进行 ASCII 音译。
- 不将非英语字母强行丢弃。

---

# 11. 大小写规则

必须使用文化无关的小写：

```csharp
ToLowerInvariant()
```

不得使用：

```csharp
ToLower()
CurrentCulture
CurrentUICulture
```

测试必须在临时切换到土耳其语等文化时验证结果不变，例如：

```text
"TITLE" → "title"
```

测试结束必须恢复原文化，避免污染其他测试。

---

# 12. 空白折叠

连续 Unicode 空白统一为一个 ASCII 空格：

```text
"get   out"
"get\tout"
"get\r\nout"
"get\u00A0out"
→ "get out"
```

要求：

- 使用 `char.IsWhiteSpace` 或等价 Unicode 规则。
- 输出内部空白统一为普通空格 U+0020。
- 首尾不保留空格。
- 不在单词内部无空白处插入空格。
- 不拆分 camelCase。
- 不拆分连字符表达。

实现可使用：

- 单次字符扫描与 `StringBuilder`；或
- 清晰、经过测试的标准库实现。

不需要引入正则表达式包。

---

# 13. 弯引号统一

至少将以下常见单引号变体统一为 ASCII 撇号：

```text
U+2018 LEFT SINGLE QUOTATION MARK   ‘
U+2019 RIGHT SINGLE QUOTATION MARK  ’
```

建议同时覆盖常见 OCR/排版变体：

```text
U+02BC MODIFIER LETTER APOSTROPHE    ʼ
U+FF07 FULLWIDTH APOSTROPHE          ＇
```

Form KC 可能已处理部分兼容字符，但仍需通过测试确认。

示例：

```text
"Don’t" → "don't"
"rock ’n’ roll" → "rock 'n' roll"
```

限制：

- 只统一单引号/撇号。
- 不在本任务中设计复杂双引号语义。
- 首尾作为引号用途的撇号可被边界标点去除。
- 单词内部撇号必须保留。

---

# 14. 首尾句读符号处理

只移除表达首尾的 Unicode `Punctuation` 类字符。

建议通过：

```csharp
CharUnicodeInfo.GetUnicodeCategory(...)
```

或等价 Unicode Category API 判断：

```text
ConnectorPunctuation
DashPunctuation
OpenPunctuation
ClosePunctuation
InitialQuotePunctuation
FinalQuotePunctuation
OtherPunctuation
```

## 14.1 必须移除的边界示例

```text
"Get out!"       → "get out"
"(Get out)"      → "get out"
"...Get out?!"   → "get out"
"“Get out.”"     → "get out"
"[well-known]"   → "well-known"
```

## 14.2 必须保留的内部内容

```text
"don't"       → "don't"
"well-known"  → "well-known"
"rock 'n' roll" → "rock 'n' roll"
```

## 14.3 连字符边界规则

产品规格要求“保留内部连字符”，但首尾连字符可能是句读或格式噪声。

因此：

```text
"-well-known-" → "well-known"
"well-known"   → "well-known"
```

仅移除位于最终边界的标点，不移除表达内部连字符。

## 14.4 不要过度清理

不得：

- 删除内部逗号、句点或符号。
- 删除字母、数字。
- 删除变音符号。
- 删除 emoji 或 Unicode Symbol，仅因它们不是字母。
- 将所有非字母数字字符全部移除。
- 删除单词内部撇号。
- 删除单词内部连字符。

本任务遵循“只清理边界句读”的最小规则。

---

# 15. 明确不做的语言处理

不得实现：

- Stemming。
- Lemmatization。
- 词形还原。
- 单复数归并。
- 时态归并。
- 拼写纠错。
- OCR 纠错。
- 同义词归并。
- 停用词删除。
- 短语拆词。
- 单词重新排序。
- 缩写展开。
- `can't → cannot`。
- `I'm → I am`。
- 美式/英式拼写统一。
- 语言检测。
- 在线词典查询。
- 大模型处理。

示例：

```text
"runs" 不得变为 "run"
"children" 不得变为 "child"
"can't" 不得变为 "cannot"
"get out of here" 不得拆成四个独立键
```

---

# 16. 性能与安全要求

## 16.1 性能

目标：

- 典型短语规范化为 O(n)。
- 不为每个字符创建大量短字符串。
- 不做网络、I/O 或数据库操作。
- 不需要缓存；避免为用户文本建立无限增长缓存。

## 16.2 安全与隐私

规范化服务处理的是用户学习文本，因此：

- 不记录输入。
- 不记录输出。
- 不把内容写入异常消息。
- 不写文件。
- 不发送网络。
- 不使用静态集合保存历史输入。

抛出空值异常时仅包含参数名，不包含用户文本。

---

# 17. 必须新增的单元测试

创建：

```text
tests/GameLexicon.Domain.Tests/Text/EnglishExpressionNormalizerTests.cs
```

建议使用现有 xUnit 风格，不切换测试框架。

## 17.1 产品规格最低测试

必须精确覆盖：

```text
" Get   Out! " → "get out"
"Don't"        → "don't"
"well-known"   → "well-known"
```

注意：规格第 11.1 节还给出更长示例：

```text
"  Get   Out of Here! " → "get out of here"
```

也应覆盖。

## 17.2 Unicode Form KC

至少覆盖：

```text
"Ｇｅｔ　Ｏｕｔ！" → "get out"
```

以及一个兼容字符案例，确保实际使用 Form KC。

## 17.3 弯引号

至少覆盖：

```text
"Don’t"          → "don't"
"‘Don’t’"        → "don't"
"rock ’n’ roll"  → "rock 'n' roll"
```

## 17.4 Unicode 空白

至少覆盖：

```text
"get\tout"           → "get out"
"get\r\nout"         → "get out"
"get\u00A0out"       → "get out"
"  get \t  out  "    → "get out"
```

## 17.5 边界标点

至少覆盖：

```text
"(Get out!)"       → "get out"
"...Get out?!"     → "get out"
"“Get out.”"       → "get out"
"[well-known]"     → "well-known"
"-well-known-"     → "well-known"
```

## 17.6 内部标点保留

至少覆盖：

```text
"don't"          → "don't"
"well-known"     → "well-known"
"rock 'n' roll"  → "rock 'n' roll"
```

增加一个内部标点案例，确认实现没有采用“删除全部标点”的错误做法。

## 17.7 不做词干化或拆分

至少覆盖：

```text
"Running Games" → "running games"
"children"      → "children"
"get out"       → "get out"
```

## 17.8 Null、空和空白

至少覆盖：

```text
null → ArgumentNullException
"" → ""
"   " → ""
"\t\r\n" → ""
```

## 17.9 幂等

用多组数据验证：

```text
Normalize(Normalize(input)) == Normalize(input)
```

至少包含：

- 普通短语。
- 弯引号。
- 全角字符。
- 连字符。
- 边界标点。

## 17.10 文化无关

临时设置：

```text
tr-TR
```

验证：

```text
"TITLE" → "title"
```

测试结束后必须在 `finally` 中恢复原文化。

## 17.11 输入不变

验证调用后原始字符串变量仍保持原内容。

虽然 .NET 字符串不可变，但测试可明确记录该契约。

---

# 18. 测试组织要求

建议使用参数化测试：

```csharp
[Theory]
[InlineData(...)]
```

避免为每个简单案例重复大量测试样板。

要求：

- 测试命名清晰。
- 失败信息能够看出输入与预期。
- 不依赖测试顺序。
- 不依赖系统默认文化。
- 不依赖网络。
- 不依赖文件。
- 不依赖数据库。
- 不启动 Godot。
- 不修改真实用户数据。
- 不为了测试加入生产环境开关。

---

# 19. 允许创建和修改的文件

建议创建：

```text
src/GameLexicon.Domain/Text/ITextNormalizer.cs
src/GameLexicon.Domain/Text/EnglishExpressionNormalizer.cs
tests/GameLexicon.Domain.Tests/Text/EnglishExpressionNormalizerTests.cs
```

允许修改：

```text
docs/IMPLEMENTATION_STATUS.md
docs/AGENT_HANDOFF.md
docs/SKILLS_CATALOG.md（仅 Skill 影响审查要求时）
docs/SKILL_CHANGELOG.md（仅 Skill 实际更新时）
.agents/skills/*/SKILL.md（仅可复用工作流变化时）
```

正常情况下不得修改：

```text
GameLexicon.sln
任一 .csproj
english-learning-project/
GameLexicon.Application
GameLexicon.Infrastructure
GameLexicon.CaptureBridge
Migration001_Initial
数据库文件
Godot 场景
AppServices
```

如果现有项目设置导致新 `.cs` 文件不会自动编译，先调查原因；不得无理由修改 `.csproj`。

---

# 20. 本任务明确不做

不得实现或修改：

- 句子切分器。
- 缩写判断。
- 目标表达范围验证。
- 修改句子后的目标重新定位。
- 多处匹配确认。
- `SentenceExample`。
- `VocabularyEntry` 实体。
- `NormalizedSentence` 生成。
- Repository。
- SQL。
- 数据库迁移。
- 唯一索引。
- UseCase。
- DTO。
- 手工添加词条页面。
- 词条库 UI。
- 搜索 UI。
- OCR。
- 截图。
- TTS。
- 复习。
- M1-T03。

即使这些功能未来会消费规范化服务，也不得在 M1-T02 提前实现。

---

# 21. 构建与自动验收

## 21.1 Domain 项目

执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

dotnet build `
  src/GameLexicon.Domain/GameLexicon.Domain.csproj `
  --no-restore
```

要求：

- 0 错误。
- 记录警告数。

## 21.2 Domain 测试

执行：

```powershell
dotnet build `
  tests/GameLexicon.Domain.Tests/GameLexicon.Domain.Tests.csproj `
  --no-restore

dotnet test `
  tests/GameLexicon.Domain.Tests/GameLexicon.Domain.Tests.csproj `
  --no-build `
  --no-restore
```

要求：

- 所有原有与新增 Domain 测试通过。
- 新增测试数量与最终总数必须报告。
- 不删除原测试。
- 不跳过失败测试。

## 21.3 根解决方案

执行：

```powershell
dotnet build GameLexicon.sln --no-restore
dotnet test GameLexicon.sln --no-build --no-restore
```

要求：

- 8 个项目构建成功。
- 所有测试通过。
- 0 错误。
- 不出现新增警告。

## 21.4 可选 Restore

仅在必要时：

```powershell
dotnet restore GameLexicon.sln
```

本任务不新增包，因此 Restore 结果不应改变依赖锁定状态或 `.csproj`。

沙箱网络限制引发 `NU1301` 时：

1. 明确标记为网络/权限问题。
2. 不修改代码或 NuGet 配置。
3. 获准后在沙箱外重试。
4. 不禁用 NuGet Audit。
5. 不添加离线未知包源。

---

# 22. 规范化结果自动验收

Codex 必须在最终报告中列出以下实际测试结果：

| 输入 | 预期 |
|---|---|
| `" Get   Out! "` | `"get out"` |
| `"Don't"` | `"don't"` |
| `"well-known"` | `"well-known"` |
| `"Ｇｅｔ　Ｏｕｔ！"` | `"get out"` |
| `"Don’t"` | `"don't"` |
| `"rock ’n’ roll"` | `"rock 'n' roll"` |
| `"(Get out!)"` | `"get out"` |
| `""` | `""` |
| whitespace only | `""` |

不得仅说“测试通过”，必须报告这些代表案例的结果。

---

# 23. 人工验收

本任务是纯 Domain 规则，**不需要 GUI 验收，也不需要启动 Godot**。

自动测试全部通过后，状态设置为：

```text
Awaiting Manual Verification
```

等待用户核对以下摘要：

1. 只新增：
   - `ITextNormalizer`
   - `EnglishExpressionNormalizer`
   - Domain 测试
2. 没有数据库、Godot、UI、Repository 或迁移修改。
3. 三个规格最低案例全部通过。
4. Unicode、空白、弯引号、边界标点、幂等和文化无关测试通过。
5. 没有日志记录用户输入。
6. Git diff 只属于 M1-T02。

用户确认后才能把 M1-T02 标记为 `Done`。

---

# 24. 强制停止条件

出现以下任意情况时停止：

- 工作区不干净且变更未确认。
- 找不到提交 `8849f987...`。
- M1-T01 未标记 Done。
- M1-T02 状态不是 Not Started。
- 基线构建或测试失败。
- 解决方案不再是 8 个项目。
- 目标框架发生变化。
- 必须新增 NuGet 包才能实现。
- 必须修改数据库或迁移。
- 必须修改 Godot 工程。
- 必须修改项目引用。
- 产品规格规范化规则存在无法解释的冲突。
- 实现需要提前加入句子切分或范围重定位。
- 发现来源不明的用户文件可能被覆盖。

停止后不得：

- 自动恢复用户文件。
- `git reset --hard`。
- `git clean -fd`。
- 修改 NuGet Audit 设置。
- 自动提交。
- 自动执行后续任务。

---

# 25. Git 检查

完成自动验证后执行：

```powershell
Set-Location "D:\UGit\EnglishLearningProject"

git status --short --untracked-files=all
git diff --stat
git diff
git diff --check
```

确认：

- 只包含 M1-T02 的 Domain 代码、测试和状态文档。
- 没有 `.csproj` 修改。
- 没有数据库文件。
- 没有日志或用户设置。
- 没有 `.godot/`、`bin/`、`obj/`。
- 没有 Godot 文件。
- 没有 Infrastructure 或 Application 生产代码修改。
- 未创建 Git 提交。

额外检查：

```powershell
git diff --name-only
```

允许的主要代码路径应限于：

```text
src/GameLexicon.Domain/Text/
tests/GameLexicon.Domain.Tests/Text/
```

---

# 26. 状态与文档收尾

自动验收通过后，GUI 不适用，先更新：

```text
M1-T02 = Awaiting Manual Verification
```

用户确认人工摘要后，更新：

```text
docs/IMPLEMENTATION_STATUS.md
```

记录：

- Task ID：M1-T02
- 名称：文本规范化
- 状态：Done
- 开始和完成时间
- 接口名称
- 实现名称
- 所在项目
- Unicode Form
- 大小写规则
- 空白规则
- 引号规则
- 边界标点规则
- 明确不做的语言处理
- 新增测试数量
- Domain 测试结果
- 根解决方案测试结果
- 代表案例
- Git diff 概况
- 已知限制

更新：

```text
docs/AGENT_HANDOFF.md
```

只有环境事实变化时才更新：

```text
docs/ENVIRONMENT.md
```

正常情况下本任务不应修改 `ENVIRONMENT.md`。

下一任务必须从 `PRODUCT_SPEC.md` 和实际实施状态确定。

不得擅自猜测、编号或自动执行 M1-T03；只记录：

```text
Next task: 待 milestone_architect 根据产品规格拆分
Status: Not Started
```

如果仓库状态文档已经明确规定 M1-T03 名称，则使用该名称，但仍不得自动执行。

---

# 27. Skill Impact Review

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

- Domain 纯函数测试标准。
- Unicode 文本规范化的项目级规则。
- 任务验收模板。
- Agent 路由。

普通规范化代码和测试案例不自动构成 Skill 更新理由。

---

# 28. 自动验收清单

- [ ] 最新提交存在
- [ ] 初始工作区干净
- [ ] 当前分支为 main
- [ ] M1-T01 为 Done
- [ ] M1-T02 为 Not Started
- [ ] 基线构建成功
- [ ] 基线 35/35 或更多测试通过
- [ ] 未新增 NuGet 包
- [ ] `ITextNormalizer` 位于 Domain
- [ ] `EnglishExpressionNormalizer` 位于 Domain
- [ ] 实现不依赖 Godot
- [ ] 实现不依赖数据库
- [ ] 实现不依赖当前文化
- [ ] 使用 Unicode Form KC
- [ ] 使用 Invariant 小写
- [ ] 连续 Unicode 空白折叠
- [ ] 弯引号统一
- [ ] 首尾句读符号移除
- [ ] 内部撇号保留
- [ ] 内部连字符保留
- [ ] 不做词干化
- [ ] 不拆分短语
- [ ] Null 契约有测试
- [ ] 空字符串有测试
- [ ] 空白字符串有测试
- [ ] 幂等有测试
- [ ] 文化无关有测试
- [ ] 产品规格三个最低案例通过
- [ ] Domain 项目构建通过
- [ ] Domain 测试全部通过
- [ ] 根解决方案构建通过
- [ ] 全部测试通过
- [ ] 未修改数据库或迁移
- [ ] 未修改 Godot 工程
- [ ] 未实现 Repository
- [ ] git diff --check 通过
- [ ] 未创建 Git 提交
- [ ] Skill Impact Review 完成

---

# 29. 人工验收清单

- [ ] 只新增 Domain 文本规范化代码
- [ ] 只新增 Domain 文本规范化测试
- [ ] `" Get   Out! "` 得到 `"get out"`
- [ ] `"Don't"` 得到 `"don't"`
- [ ] `"well-known"` 保留连字符
- [ ] 全角字符正确归一
- [ ] 弯引号正确归一
- [ ] Unicode 空白正确折叠
- [ ] 边界标点正确去除
- [ ] 内部撇号和连字符保留
- [ ] 没有词干化或短语拆分
- [ ] 没有输入文本日志
- [ ] 无数据库修改
- [ ] 无 Godot 修改
- [ ] 无 UI 修改
- [ ] 无 Repository 或 CRUD
- [ ] 所有测试通过
- [ ] Git diff 仅属于 M1-T02

---

# 30. Codex 最终报告格式

```markdown
## 任务结果

- Task ID: M1-T02
- 名称: 文本规范化
- 状态:
- 后续任务是否执行: No
- Git commit created: No
- GUI verification required: No

## 任务路由

- Primary domain:
- Primary agent:
- Supporting agents:
- Skills used:

## 前置基线

- M1-T01 commit:
- Branch:
- Initial Git status:
- Solution projects:
- Target frameworks:
- Baseline build:
- Baseline tests:
- NuGet/network status:

## 实现

- Interface:
- Implementation:
- Project:
- Unicode normalization:
- Case conversion:
- Whitespace handling:
- Apostrophe handling:
- Boundary punctuation handling:
- Null/empty contract:
- Idempotency:
- Culture independence:

## 明确未实现

- Sentence splitting:
- Target relocation:
- Stemming:
- Repository:
- Database:
- UI:

## 代表案例

| Input | Actual | Expected | Result |
|---|---|---|---|
| ... | ... | ... | Pass/Fail |

## 创建的文件

- ...

## 修改的文件

- ...

## 自动化测试

- Baseline total:
- Added:
- Final total:
- Passed:
- Failed:
- Skipped:
- Domain tests:
- Root solution tests:

## 构建结果

- Domain:
- Domain.Tests:
- Root solution:
- Warnings:
- Errors:

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

- Awaiting user review of implementation and test summary.
- No GUI run is required.

## 已知限制

- No stemming or lemmatization.
- No sentence splitting.
- No target-range relocation.
- Not yet wired into repositories or use cases.
- ...

## 下一任务

- Name:
- Status: Not Started
- Not automatically executed
```

---

# 31. 可直接执行的总指令

请执行：

```text
M1-T02：文本规范化
```

严格按照：

```text
docs/MT_INSTRUCTION/M1-T02_CODEX_INSTRUCTION.md
```

执行。

特别要求：

1. 先核验提交 `8849f987c919faa09d52c2413b9ccd9a221627c9`。
2. 开始时 Git 工作区必须干净。
3. 只创建 `ITextNormalizer`、`EnglishExpressionNormalizer` 和 Domain 测试。
4. 实现必须位于 Domain。
5. 使用 Unicode Form KC。
6. 使用 Invariant 小写。
7. 折叠连续 Unicode 空白。
8. 统一弯单引号。
9. 去除首尾句读符号。
10. 保留内部撇号和连字符。
11. 不做词干化。
12. 不拆分短语。
13. 不记录用户输入文本。
14. 不修改数据库、迁移、Godot、UI、Repository 或项目引用。
15. 不新增 NuGet 包。
16. 不执行后续任务。
17. 不创建 Git 提交。
18. 自动验收后等待用户进行非 GUI 人工审查。
19. 用户确认前将状态保持为 `Awaiting Manual Verification`。
20. 完成后执行 Git diff、文档更新和 Skill Impact Review。
