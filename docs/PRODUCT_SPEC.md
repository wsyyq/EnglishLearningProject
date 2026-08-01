---
title: "GameLexicon：基于 Godot 的游戏英语学习工具策划与 Codex 执行规格"
document_type: "product_and_implementation_spec"
language: "zh-CN"
target_engine: "Godot 4.7.1 .NET"
target_platform_mvp: "Windows 10/11 x64"
architecture: "Godot desktop app + Windows CaptureBridge + local OCR + SQLite"
status: "implementation_ready"
version: "0.1.0"
updated_at: "2026-08-01"
---

# GameLexicon：基于 Godot 的游戏英语学习工具策划与 Codex 执行规格

> 本文件既是产品策划案，也是提供给 Codex 的工程实施说明。  
> Codex 应严格按照“里程碑、任务、验收标准”的顺序执行，不应一次性重写全部工程。

---

## 0. 给 Codex 的执行指令

### 0.1 项目目标

创建一款桌面英语学习工具，使用户在游玩英文游戏时能够：

1. 使用全局快捷键截取游戏画面。
2. 对截图或用户框选区域执行英文 OCR。
3. 查看并校正 OCR 结果。
4. 从游戏原句中选择单词或连续短语。
5. 保存单词/短语、释义、游戏原句、截图上下文与来源游戏。
6. 播放单词、短语和完整例句的英文发音。
7. 使用间隔复习和多种题型复习已保存内容。
8. 导出或备份自己的学习数据。

### 0.2 Codex 工作规则

Codex 必须遵守以下规则：

- 使用 **Godot 4.7.1 .NET 版**，主要业务代码使用 **C#**。
- MVP 只要求支持 **Windows 10/11 x64**。
- MVP 必须可以完全离线使用。
- 不向游戏进程注入 DLL，不读取游戏内存，不使用绕过反作弊的技术。
- 截图、OCR、词库和复习记录默认只保存在本机。
- 所有外部能力均通过接口隔离：
  - `IScreenCaptureService`
  - `IOcrService`
  - `ITextToSpeechService`
  - `IDictionaryProvider`
  - `IVocabularyRepository`
  - `IReviewRepository`
- 不允许在 UI 脚本中直接执行 SQL、OCR 命令或 Win32 调用。
- 每完成一个里程碑：
  1. 编译工程。
  2. 运行自动化测试。
  3. 执行该里程碑的人工验收。
  4. 更新 `docs/IMPLEMENTATION_STATUS.md`。
- 未完成当前里程碑的验收前，不进入下一个里程碑。
- 新增数据库字段必须通过迁移实现，禁止直接修改用户现有数据库。
- API 密钥不得提交到 Git。
- 对无法确认的实现细节，优先实现最小、可测试、可替换的版本，不扩大范围。

### 0.3 Codex 首次执行时应创建的文件

```text
AGENTS.md
README.md
LICENSE
.gitignore
docs/
  PRODUCT_SPEC.md
  ARCHITECTURE.md
  DATA_MODEL.md
  IMPLEMENTATION_STATUS.md
  MANUAL_TEST_CHECKLIST.md
```

将本文件复制为：

```text
docs/PRODUCT_SPEC.md
```

`AGENTS.md` 应提取本节中的 Codex 工作规则，并要求每次修改前先阅读：

```text
docs/PRODUCT_SPEC.md
docs/IMPLEMENTATION_STATUS.md
```

---

# 1. 产品定义

## 1.1 暂定名称

**GameLexicon**

可选中文名：

- 游戏语境词库
- 游戏英语拾词本
- 原句英语助手
- 游戏短语收藏夹

工程内部统一使用：

```text
GameLexicon
```

## 1.2 产品定位

GameLexicon 不是普通的截图翻译工具，也不是只保存孤立单词的生词本。

它的核心价值是：

> 将用户在游戏中真实遇到的英文内容，转化为带有原句、截图上下文、来源游戏和复习计划的个人短语词库。

用户真正学习的最小单位优先为：

1. 固定搭配。
2. 动词短语。
3. 介词短语。
4. 常用表达。
5. 句型片段。
6. 必要时才是单独单词。

## 1.3 目标用户

主要用户：

- 使用英语游玩 RPG、视觉小说、策略游戏、模拟经营游戏的中文用户。
- 能理解基础英语，但经常被生词、短语和不熟悉表达打断。
- 已使用欧路词典、Anki 或其他生词本，但缺少游戏原句上下文。
- 希望建立属于自己的游戏英语语料库。

次要用户：

- 通过影视字幕、网页漫画、电子书或软件界面学习英语的用户。
- 希望从任何屏幕内容中建立上下文词库的用户。

## 1.4 产品原则

### 原则 A：尽量不打断游戏

默认工作流应允许用户：

1. 在游戏中按快捷键。
2. 截图进入待处理队列。
3. 继续游戏。
4. 在游戏结束后批量整理 OCR 内容。

### 原则 B：保存语境，而不是只保存词义

每个词条至少保存：

- 单词或短语。
- 游戏中的完整原句。
- 原句内目标短语的位置。
- 来源游戏。
- 截图或截图裁剪。
- 创建时间。

### 原则 C：OCR 结果必须可人工校正

游戏字体、描边、发光、半透明背景和低分辨率都会导致 OCR 错误。  
软件不能把 OCR 结果当作绝对正确的数据。

### 原则 D：离线优先

MVP 中以下功能必须离线可用：

- 截图。
- OCR。
- 建立词条。
- 本地发音。
- 复习。
- 备份和导出。

### 原则 E：服务可替换

后续可以添加在线 OCR、在线词典、LLM 短语分析和云端 TTS，但不得把核心流程锁死在某一家服务中。

---

# 2. 用户问题与解决方案

## 2.1 当前问题

用户当前使用欧路词典截图翻译时，主要存在以下问题：

1. 容易只保存单个单词。
2. 单词脱离了游戏原句。
3. 无法回忆当时角色、任务或剧情上下文。
4. 难以学习动词短语和固定搭配。
5. 截图、查词、编辑生词本的操作链较长。
6. 缺少针对原句的填空、听力和短语复习。
7. 同一短语在多个游戏场景中的重复出现无法聚合。
8. 游戏过程中整理词条会打断体验。

## 2.2 解决方案概要

GameLexicon 提供两种工作流。

### 工作流 A：即时整理

```text
游戏中按快捷键
→ 截图
→ 自动打开轻量浮层
→ 框选字幕或文本区域
→ OCR
→ 校正文段
→ 选择单词/短语
→ 保存
→ 返回游戏
```

### 工作流 B：稍后批量整理

```text
游戏中多次按快捷键
→ 截图依次进入收件箱
→ 用户继续游戏
→ 游戏结束后打开 GameLexicon
→ 逐张框选、OCR、校正和建词条
```

MVP 应优先保证工作流 B 稳定，再完善工作流 A。

---

# 3. 功能范围

## 3.1 MVP 必须实现

### F01：全局快捷键截图

- 默认快捷键：`Ctrl + Shift + E`。
- 用户可在设置中修改。
- GameLexicon 未获得焦点时也能触发。
- 支持截取：
  - 当前显示器。
  - 用户预先选择的显示器。
  - 后续可增加指定窗口。
- 截图保存到本地收件箱。
- 截图完成后显示轻量通知，不能强制切走游戏焦点。

### F02：截图收件箱

每条截图记录显示：

- 缩略图。
- 截图时间。
- 来源窗口标题。
- 推测的游戏名称。
- 状态：
  - 待处理。
  - OCR 中。
  - 待校正。
  - 已完成。
  - 失败。
- 删除按钮。
- 批量删除和批量标记功能。

### F03：区域框选

- 打开截图后可拖拽选择文本区域。
- 支持重新框选。
- 支持缩放和平移。
- 支持保存一个或多个文本区域。
- MVP 可以先限制为一次处理一个区域。

### F04：英文 OCR

- 本地调用 Tesseract OCR。
- 使用 `eng` 语言数据。
- 返回：
  - 原始全文。
  - 单词文本。
  - 单词位置框。
  - 置信度。
  - 行号。
- OCR 失败时显示可理解的错误。
- 用户可以直接手工输入文本，绕过 OCR。

### F05：OCR 校正

- 显示截图和 OCR 文本。
- 点击截图中的词框时，文本区同步定位。
- 低置信度词使用明显标记。
- 用户可以编辑整段文本。
- 保留：
  - OCR 原始文本。
  - 用户校正文本。
- 支持重新运行 OCR。

### F06：单词和短语选择

- 用户可以在校正后的句子中选择：
  - 单个单词。
  - 连续多个单词。
- 选中后创建候选词条。
- 自动去除首尾多余标点。
- 保留原始大小写，但同时生成规范化键。
- 可以一次从同一句中添加多个词条。
- 支持手工输入目标短语。

### F07：词条编辑

字段包括：

- 表达：`headword`
- 类型：
  - 单词
  - 短语
  - 固定表达
  - 句型
- 英文释义，可空。
- 中文释义，可空。
- 音标，可空。
- 词性，可空。
- 用户笔记。
- 标签。
- 来源游戏。
- 原句。
- 截图裁剪。
- 是否设为主要例句。
- 是否加入复习。

MVP 不强制依赖在线词典。用户至少可以手工录入释义。

### F08：重复词条处理

当用户保存已有表达时：

- 显示已有词条。
- 提供：
  - 合并为新例句。
  - 创建独立词条。
  - 取消。
- 默认推荐合并为新例句。
- 一个词条可以拥有多个游戏原句和多个来源游戏。

### F09：词条库

支持：

- 搜索。
- 按游戏筛选。
- 按标签筛选。
- 按类型筛选。
- 按复习状态筛选。
- 查看词条详情。
- 编辑。
- 归档。
- 删除。
- 查看所有游戏例句。

### F10：英文发音

支持播放：

- 单词。
- 短语。
- 完整原句。

MVP 使用 Godot `DisplayServer` 的系统 TTS。

设置项：

- 英语语音。
- 语速。
- 音高。
- 音量。
- 是否打断当前播放。

### F11：复习队列

首页显示：

- 今日待复习数量。
- 新词数量。
- 逾期数量。
- 最近七天学习数量。
- “开始复习”按钮。

### F12：复习模式

MVP 至少实现以下四种。

#### 模式 1：英译中回忆

正面：

- 单词或短语。
- 可选播放发音。

背面：

- 中文释义。
- 英文释义。
- 游戏原句。
- 截图上下文。

#### 模式 2：原句填空

正面：

- 将目标词或短语从游戏原句中替换为下划线。

示例：

```text
We need to _____ before the guards return.
```

答案：

```text
get out of here
```

#### 模式 3：四选一

- 显示表达，选择正确中文释义。
- 或显示原句填空，选择正确短语。
- 干扰项来自相同类型或相近长度的词条。
- 干扰项不足时退化为普通翻卡，不生成质量很差的选项。

#### 模式 4：听力识别

- 播放单词、短语或原句。
- 用户选择或输入答案。
- 必须提供“再次播放”。

### F13：复习评分

用户每题结束后选择：

- Again：完全不会。
- Hard：勉强想起。
- Good：正常想起。
- Easy：非常熟悉。

评分后更新下次复习时间。

### F14：备份与导出

支持导出：

- 完整 JSON 备份。
- CSV 词条。
- Anki 可导入 TSV。

导出内容至少包括：

- 表达。
- 中文释义。
- 英文释义。
- 原句。
- 来源游戏。
- 标签。
- 创建时间。
- 复习信息。

截图可选择：

- 不导出。
- 复制到媒体文件夹并写入相对路径。

---

## 3.2 V1.1 建议功能

- 多区域 OCR。
- OCR 图像预处理调节。
- 快捷键只入队、不打开主界面。
- 游戏自动识别与封面管理。
- 自动检测句子边界。
- 简单短语候选推荐。
- 同一表达的多例句轮换复习。
- 拼写输入题。
- 短语排序题。
- 词条批量编辑。
- 学习统计。
- 每日目标。
- 欧路词典 CSV 导入。
- Anki 导出模板。
- 截图自动清理策略。
- 数据库自动备份。

## 3.3 V1.2 及以后

- macOS `ScreenCaptureKit` 适配。
- Linux XDG Desktop Portal 适配。
- 在线词典 Provider。
- 在线 OCR Provider。
- 云端 TTS 和可缓存音频。
- LLM 辅助：
  - 识别值得学习的搭配。
  - 解释短语在当前句子的含义。
  - 生成英文释义。
  - 生成难度相近的干扰项。
- 多设备同步。
- 移动端只读复习客户端。
- 浏览器扩展。
- 视频字幕连续捕获。
- Steam 游戏库关联。
- 自动识别角色名、任务名和章节。

## 3.4 明确不属于 MVP 的功能

- 实时翻译整场游戏。
- 覆盖在游戏上的持续字幕翻译。
- 注入游戏进程。
- 读取游戏内存。
- 绕过 DRM。
- 绕过反作弊。
- 自动调用未授权的词典网页接口。
- 账户系统。
- 云同步。
- 移动端编辑。
- 多人协作词库。

---

# 4. 核心用户流程

## 4.1 首次启动

```text
启动应用
→ 欢迎页
→ 检查 Tesseract
→ 检查英文语言包
→ 检查系统英文 TTS
→ 设置截图快捷键
→ 选择默认显示器
→ 执行测试截图
→ 执行测试 OCR
→ 完成
```

如果依赖缺失：

- 显示准确的缺失项。
- 显示本地安装路径设置。
- 允许暂时跳过 OCR，使用手工输入模式。
- 不允许静默失败。

## 4.2 游戏中快速捕获

```text
用户按 Ctrl+Shift+E
→ CaptureBridge 收到快捷键
→ 捕获目标屏幕
→ 写入 PNG
→ 写入 manifest.json
→ Godot 收件箱检测到新任务
→ 更新角标
→ 可选显示系统通知
```

## 4.3 整理截图

```text
打开截图
→ 框选字幕区域
→ OCR
→ 查看词框和全文
→ 修正 OCR
→ 选择一句话
→ 选择单词/短语
→ 编辑释义和标签
→ 保存
→ 继续处理下一张
```

## 4.4 复习

```text
进入今日复习
→ 系统选择到期卡片
→ 混合题型
→ 用户作答
→ 显示答案与原截图
→ 用户评分
→ 更新排期
→ 显示本次结果
```

---

# 5. 信息架构与界面

## 5.1 主导航

桌面端左侧导航：

```text
首页
截图收件箱
词条库
今日复习
统计
设置
```

窗口建议：

- 默认尺寸：`1280 × 800`。
- 最小尺寸：`960 × 640`。
- 支持高 DPI。
- 支持浅色和深色主题。
- MVP 默认深色主题，减少从游戏切换后的视觉刺激。

## 5.2 首页 Dashboard

包含：

- 今日待复习。
- 待整理截图。
- 本周新增词条。
- 连续学习天数。
- 最近学习的游戏。
- 快捷操作：
  - 测试截图。
  - 导入图片。
  - 手工添加词条。
  - 开始复习。

## 5.3 截图收件箱 Capture Inbox

布局：

```text
┌ 筛选与搜索 ───────────────────────────┐
│ 状态 | 游戏 | 日期 | 批量操作          │
├─────────────┬─────────────────────────┤
│ 截图列表     │ 选中截图预览与元数据      │
│ 缩略图       │                         │
│ 时间         │ [处理截图] [删除]        │
└─────────────┴─────────────────────────┘
```

## 5.4 OCR 工作台

布局：

```text
┌────────────────────────────────────────────┐
│ 返回 | 来源游戏 | 重新 OCR | 保存草稿       │
├──────────────────────┬─────────────────────┤
│ 截图画布              │ OCR 文本编辑区       │
│ 缩放/平移             │ 低置信度提示         │
│ OCR 单词框            │ 句子列表             │
│ 框选区域              │ 候选词条列表         │
├──────────────────────┴─────────────────────┤
│ [添加单词/短语] [标记完成] [下一张]          │
└────────────────────────────────────────────┘
```

关键交互：

- 鼠标拖拽：框选 OCR 区域。
- `Ctrl + 鼠标滚轮`：缩放。
- 空格 + 拖拽：平移。
- 点击词框：选中 OCR token。
- `Shift + 点击`：扩展为连续短语。
- 文本区选中文字：创建词条。
- 双击低置信度词：直接编辑。

## 5.5 词条编辑器

分区：

### 基本信息

- 表达。
- 类型。
- 词性。
- 中文释义。
- 英文释义。
- 音标。
- 笔记。
- 标签。

### 游戏语境

- 原句。
- 目标短语高亮。
- 截图裁剪预览。
- 来源游戏。
- 来源窗口标题。
- 捕获时间。

### 发音

- 播放表达。
- 播放原句。
- 语音选择。

### 重复检测

- 显示可能重复的词条。
- 显示已有例句数量。
- “合并为新例句”。

## 5.6 词条详情

显示：

- 词头。
- 释义。
- 标签。
- 学习状态。
- 下次复习时间。
- 多条例句卡片。
- 每条例句的截图。
- 每条例句的来源游戏。
- 复习历史。
- 编辑、归档、删除。

## 5.7 复习界面

布局：

```text
┌────────────────────────────────────────────┐
│ 进度 12/30 | 本题类型 | 退出                │
├────────────────────────────────────────────┤
│                                            │
│              题目区域                       │
│                                            │
│              输入/选项区域                  │
│                                            │
├────────────────────────────────────────────┤
│ 显示答案后：Again | Hard | Good | Easy      │
└────────────────────────────────────────────┘
```

显示答案时必须展示：

- 正确答案。
- 释义。
- 游戏原句。
- 目标表达高亮。
- 截图上下文。
- 发音按钮。

---

# 6. 技术选型

## 6.1 基线版本

- Godot：`4.7.1 .NET`
- 语言：C#，可在极少数纯 UI 动画中使用 GDScript，但默认不使用。
- .NET：采用 Godot 4.7.1 .NET 发行版所要求的受支持版本。
- 数据库：SQLite。
- SQLite 驱动：`Microsoft.Data.Sqlite`。
- OCR：Tesseract CLI。
- OCR 语言：`eng`。
- 截图：Windows Graphics Capture。
- 全局快捷键：Windows `RegisterHotKey`，由独立 CaptureBridge 处理。
- TTS：Godot `DisplayServer.tts_*`。
- 配置：JSON。
- 日志：本地滚动日志。
- 测试：xUnit 或 NUnit；选择一种并全项目统一。

## 6.2 为什么采用 Godot .NET

本项目涉及：

- Win32 全局快捷键。
- Windows 屏幕捕获 API。
- SQLite。
- 子进程管理。
- 文件系统监听。
- 后续跨平台原生适配。

C# 比纯 GDScript 更适合作为该项目的主要语言。  
Godot 仍负责：

- 场景树。
- UI。
- 输入。
- 动画。
- 资源管理。
- 主循环。
- TTS 接口。

## 6.3 为什么需要 CaptureBridge

当 Godot 应用不在前台时，普通 Godot 输入事件不能可靠接收全局快捷键。  
外部游戏画面的捕获也不应依赖 Godot 自身 Viewport 截图。

因此 MVP 使用独立进程：

```text
GameLexicon.CaptureBridge.exe
```

职责仅包括：

1. 注册全局快捷键。
2. 识别当前前台窗口和显示器。
3. 调用 Windows Graphics Capture。
4. 保存截图。
5. 写入捕获任务清单。
6. 向主程序发出新截图事件。
7. 不执行 OCR。
8. 不访问数据库。
9. 不注入目标游戏。

## 6.4 通信方式

MVP 建议采用“文件收件箱 + JSON manifest”，而不是先实现复杂 IPC。

目录：

```text
%APPDATA%/GameLexicon/capture_inbox/
```

每次捕获生成：

```text
capture_inbox/
  20260801_153000_123/
    capture.png
    manifest.json
```

`manifest.json`：

```json
{
  "schema_version": 1,
  "capture_id": "a7e85964-1940-4c09-a679-fab0a171bc38",
  "captured_at_utc": "2026-08-01T07:30:00.123Z",
  "source_window_title": "Example Game",
  "source_process_name": "examplegame.exe",
  "display_id": "DISPLAY1",
  "image_file": "capture.png",
  "pixel_width": 2560,
  "pixel_height": 1440,
  "status": "ready"
}
```

Godot 主程序每 500 毫秒轮询一次，或者使用 `.NET FileSystemWatcher` 监听新增目录。

为了防止读取到尚未写完的文件：

1. CaptureBridge 先写 `capture.tmp.png`。
2. 写完后重命名为 `capture.png`。
3. 先写 `manifest.tmp.json`。
4. 最后原子重命名为 `manifest.json`。
5. 主程序只处理存在 `manifest.json` 的目录。

后续可以改为 Named Pipe，但不能影响上层接口。

---

# 7. 分层架构

```text
┌─────────────────────────────────────────────┐
│ Godot Presentation                         │
│ Scenes / Controls / ViewModels              │
├─────────────────────────────────────────────┤
│ Application                                 │
│ CaptureWorkflow / EntryWorkflow / Review    │
├─────────────────────────────────────────────┤
│ Domain                                      │
│ Entities / Value Objects / Scheduling       │
├─────────────────────────────────────────────┤
│ Infrastructure                              │
│ SQLite / Tesseract / TTS / File Storage     │
├─────────────────────────────────────────────┤
│ Platform                                    │
│ Windows CaptureBridge / Global Hotkey       │
└─────────────────────────────────────────────┘
```

## 7.1 Presentation 层

职责：

- 场景。
- 控件。
- 用户交互。
- 状态展示。
- 调用 Application 层。
- 不直接访问 SQLite。
- 不直接执行 OCR。
- 不直接调用 Win32。

## 7.2 Application 层

主要用例：

- `ImportCaptureUseCase`
- `CreateOcrRegionUseCase`
- `RunOcrUseCase`
- `CorrectOcrTextUseCase`
- `CreateEntryFromSelectionUseCase`
- `MergeEntryExampleUseCase`
- `BuildReviewSessionUseCase`
- `SubmitReviewGradeUseCase`
- `ExportLibraryUseCase`
- `BackupDatabaseUseCase`

## 7.3 Domain 层

包含：

- 实体。
- 值对象。
- 领域规则。
- 复习算法。
- 文本规范化。
- 重复检测。
- 题目生成。

不得依赖 Godot API、Windows API 或数据库实现。

## 7.4 Infrastructure 层

实现：

- SQLite Repository。
- Tesseract OCR。
- Godot 系统 TTS。
- JSON 配置。
- 截图文件存储。
- 日志。
- 导入导出。

## 7.5 Platform 层

Windows 专用内容：

- `RegisterHotKey`。
- 前台窗口信息。
- 显示器枚举。
- Windows Graphics Capture。
- 系统通知。
- CaptureBridge 生命周期。

---

# 8. 建议工程目录

```text
GameLexicon/
├─ AGENTS.md
├─ README.md
├─ LICENSE
├─ .gitignore
├─ global.json
├─ GameLexicon.sln
├─ docs/
│  ├─ PRODUCT_SPEC.md
│  ├─ ARCHITECTURE.md
│  ├─ DATA_MODEL.md
│  ├─ IMPLEMENTATION_STATUS.md
│  └─ MANUAL_TEST_CHECKLIST.md
├─ app/
│  └─ GameLexicon.Godot/
│     ├─ project.godot
│     ├─ GameLexicon.Godot.csproj
│     ├─ assets/
│     │  ├─ icons/
│     │  ├─ fonts/
│     │  └─ themes/
│     ├─ scenes/
│     │  ├─ App.tscn
│     │  ├─ onboarding/
│     │  ├─ dashboard/
│     │  ├─ capture_inbox/
│     │  ├─ ocr_workspace/
│     │  ├─ entry_editor/
│     │  ├─ library/
│     │  ├─ review/
│     │  ├─ statistics/
│     │  ├─ settings/
│     │  └─ shared/
│     ├─ scripts/
│     │  ├─ AppRoot.cs
│     │  ├─ NavigationService.cs
│     │  ├─ AppServices.cs
│     │  ├─ ViewModelBase.cs
│     │  └─ UI/
│     └─ addons/
├─ src/
│  ├─ GameLexicon.Domain/
│  │  ├─ GameLexicon.Domain.csproj
│  │  ├─ Entities/
│  │  ├─ ValueObjects/
│  │  ├─ Review/
│  │  └─ Text/
│  ├─ GameLexicon.Application/
│  │  ├─ GameLexicon.Application.csproj
│  │  ├─ Abstractions/
│  │  ├─ DTOs/
│  │  └─ UseCases/
│  └─ GameLexicon.Infrastructure/
│     ├─ GameLexicon.Infrastructure.csproj
│     ├─ Persistence/
│     ├─ OCR/
│     ├─ TTS/
│     ├─ Export/
│     ├─ Configuration/
│     └─ Logging/
├─ tools/
│  └─ GameLexicon.CaptureBridge/
│     ├─ GameLexicon.CaptureBridge.csproj
│     ├─ Program.cs
│     ├─ Hotkey/
│     ├─ Capture/
│     ├─ ForegroundWindow/
│     └─ Manifest/
├─ tests/
│  ├─ GameLexicon.Domain.Tests/
│  ├─ GameLexicon.Application.Tests/
│  └─ GameLexicon.Infrastructure.Tests/
├─ scripts/
│  ├─ bootstrap.ps1
│  ├─ run-dev.ps1
│  ├─ run-tests.ps1
│  └─ package-windows.ps1
└─ third_party/
   └─ README.md
```

---

# 9. Godot 场景设计

## 9.1 `App.tscn`

节点建议：

```text
AppRoot (Control)
├─ Background (ColorRect)
├─ AppLayout (HBoxContainer)
│  ├─ Sidebar (PanelContainer)
│  │  └─ NavigationList (VBoxContainer)
│  └─ ContentHost (MarginContainer)
│     └─ RouteHost (Control)
├─ ToastLayer (CanvasLayer)
├─ ModalLayer (CanvasLayer)
├─ GlobalLoadingOverlay (CanvasLayer)
└─ InboxPollTimer (Timer)
```

`AppRoot.cs` 职责：

- 初始化服务。
- 执行数据库迁移。
- 检查依赖。
- 启动 CaptureBridge。
- 监听截图收件箱。
- 恢复上次页面。
- 保存窗口状态。
- 退出时优雅关闭桥接进程。

## 9.2 `DashboardView.tscn`

```text
DashboardView (ScrollContainer)
└─ Content (VBoxContainer)
   ├─ Header
   ├─ MetricCards (GridContainer)
   ├─ PrimaryActions (HBoxContainer)
   ├─ RecentGames
   └─ RecentEntries
```

## 9.3 `CaptureInboxView.tscn`

```text
CaptureInboxView (HSplitContainer)
├─ LeftPanel (VBoxContainer)
│  ├─ FilterBar
│  └─ CaptureList (ItemList)
└─ RightPanel (VBoxContainer)
   ├─ ScreenshotPreview (TextureRect)
   ├─ MetadataPanel
   └─ ActionButtons
```

## 9.4 `OcrWorkspaceView.tscn`

```text
OcrWorkspaceView (VBoxContainer)
├─ Toolbar
├─ MainSplit (HSplitContainer)
│  ├─ ScreenshotPanel
│  │  └─ ScreenshotCanvas (Control)
│  │     ├─ ScreenshotTexture (TextureRect)
│  │     ├─ SelectionOverlay (Control)
│  │     └─ TokenOverlay (Control)
│  └─ TextPanel
│     ├─ OcrStatusBar
│     ├─ CorrectedTextEdit (TextEdit)
│     ├─ SentenceList
│     └─ CandidateEntryList
└─ BottomActions
```

建议创建自定义控件：

```text
ScreenshotCanvas.cs
TokenOverlay.cs
RegionSelectionOverlay.cs
```

### `ScreenshotCanvas.cs`

负责：

- 图片适配。
- 缩放。
- 平移。
- 屏幕坐标与图片像素坐标转换。
- 发送区域选择事件。

必须提供方法：

```csharp
Vector2 ScreenToImage(Vector2 screenPoint);
Vector2 ImageToScreen(Vector2 imagePoint);
Rect2 ScreenRectToImageRect(Rect2 screenRect);
Rect2 ImageRectToScreenRect(Rect2 imageRect);
```

### `TokenOverlay.cs`

负责：

- 绘制 OCR token 框。
- 按置信度决定线型或透明度。
- 命中检测。
- 连续选择。
- 高亮当前短语。

## 9.5 `EntryEditorView.tscn`

```text
EntryEditorView (ScrollContainer)
└─ Form (VBoxContainer)
   ├─ ExpressionSection
   ├─ DefinitionSection
   ├─ ContextSection
   ├─ ScreenshotSection
   ├─ TagSection
   ├─ DuplicateSection
   └─ SaveActions
```

## 9.6 `LibraryView.tscn`

```text
LibraryView (VBoxContainer)
├─ SearchFilterBar
├─ MainSplit
│  ├─ EntryList
│  └─ EntryDetails
└─ PaginationOrVirtualListFooter
```

当词条较多时应使用虚拟化列表或分页，避免一次创建数千个复杂节点。

## 9.7 `ReviewSessionView.tscn`

```text
ReviewSessionView (VBoxContainer)
├─ SessionHeader
├─ QuestionHost
├─ AnswerHost
├─ RevealButton
├─ GradeButtons
└─ SessionProgress
```

题型使用策略模式：

```csharp
public interface IReviewQuestionPresenter
{
    ReviewCardType SupportedType { get; }
    void Bind(ReviewQuestionDto question);
    bool CanSubmit { get; }
    ReviewAnswerDto GetAnswer();
    void Reveal(ReviewResultDto result);
}
```

---

# 10. 领域模型

## 10.1 Capture

```csharp
public sealed class Capture
{
    public Guid Id { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public string SourceWindowTitle { get; set; } = "";
    public string SourceProcessName { get; set; } = "";
    public string? GameTitle { get; set; }
    public string ImagePath { get; init; } = "";
    public int PixelWidth { get; init; }
    public int PixelHeight { get; init; }
    public CaptureStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}
```

状态：

```csharp
public enum CaptureStatus
{
    Pending,
    Processing,
    NeedsCorrection,
    Completed,
    Failed,
    Archived
}
```

## 10.2 OcrRegion

```csharp
public sealed class OcrRegion
{
    public Guid Id { get; init; }
    public Guid CaptureId { get; init; }
    public PixelRect Region { get; init; }
    public string RawText { get; set; } = "";
    public string CorrectedText { get; set; } = "";
    public DateTimeOffset CreatedAt { get; init; }
}
```

## 10.3 OcrToken

```csharp
public sealed class OcrToken
{
    public Guid Id { get; init; }
    public Guid OcrRegionId { get; init; }
    public string Text { get; set; } = "";
    public float Confidence { get; set; }
    public PixelRect Bounds { get; set; }
    public int BlockIndex { get; set; }
    public int ParagraphIndex { get; set; }
    public int LineIndex { get; set; }
    public int WordIndex { get; set; }
}
```

## 10.4 SentenceExample

```csharp
public sealed class SentenceExample
{
    public Guid Id { get; init; }
    public Guid CaptureId { get; init; }
    public Guid? OcrRegionId { get; init; }
    public string SentenceText { get; set; } = "";
    public string NormalizedSentence { get; set; } = "";
    public int TargetStart { get; set; }
    public int TargetLength { get; set; }
    public string ScreenshotCropPath { get; set; } = "";
    public string? GameTitle { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}
```

`TargetStart` 和 `TargetLength` 使用 .NET 字符串的 UTF-16 索引。  
同一工程内必须统一，不允许部分代码使用字节索引、部分使用 Unicode scalar 索引。

## 10.5 VocabularyEntry

```csharp
public sealed class VocabularyEntry
{
    public Guid Id { get; init; }
    public string Headword { get; set; } = "";
    public string NormalizedHeadword { get; set; } = "";
    public EntryType EntryType { get; set; }
    public string? PartOfSpeech { get; set; }
    public string? Phonetic { get; set; }
    public string? DefinitionEnglish { get; set; }
    public string? TranslationChinese { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
```

```csharp
public enum EntryType
{
    Word,
    Phrase,
    Expression,
    SentencePattern
}
```

## 10.6 EntryExampleLink

```csharp
public sealed class EntryExampleLink
{
    public Guid EntryId { get; init; }
    public Guid ExampleId { get; init; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
```

## 10.7 ReviewCard

```csharp
public sealed class ReviewCard
{
    public Guid Id { get; init; }
    public Guid EntryId { get; init; }
    public ReviewCardType CardType { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public int Repetition { get; set; }
    public double IntervalDays { get; set; }
    public double EaseFactor { get; set; } = 2.5;
    public int LapseCount { get; set; }
    public DateTimeOffset? LastReviewedAt { get; set; }
    public bool IsSuspended { get; set; }
}
```

```csharp
public enum ReviewCardType
{
    ExpressionToMeaning,
    Cloze,
    MultipleChoice,
    Listening
}
```

## 10.8 ReviewLog

```csharp
public sealed class ReviewLog
{
    public Guid Id { get; init; }
    public Guid ReviewCardId { get; init; }
    public DateTimeOffset ReviewedAt { get; init; }
    public ReviewGrade Grade { get; init; }
    public double PreviousIntervalDays { get; init; }
    public double NewIntervalDays { get; init; }
    public double PreviousEaseFactor { get; init; }
    public double NewEaseFactor { get; init; }
    public int? ResponseMilliseconds { get; init; }
}
```

---

# 11. 文本规范化规则

## 11.1 词头规范化

用于重复检测的 `NormalizedHeadword`：

1. Unicode 规范化为 Form KC。
2. 转为小写。
3. 首尾 trim。
4. 连续空白折叠为一个空格。
5. 将弯引号统一为直引号。
6. 去除首尾不属于表达的句读符号。
7. 保留内部撇号和连字符。
8. 不做词干化。
9. 不自动把短语拆成单词。

示例：

```text
"  Get   Out of Here! " → "get out of here"
"don't"                 → "don't"
"well-known"            → "well-known"
```

## 11.2 句子切分

MVP 使用可解释的规则：

- `.`
- `?`
- `!`
- `…`
- 换行
- OCR 行边界

需要避免在以下内容中过度切分：

- `Mr.`
- `Mrs.`
- `Dr.`
- `e.g.`
- `i.e.`
- 小数。
- 常见缩写。

不能确定时允许用户手工调整句子范围。

## 11.3 短语选择

短语必须是校正文本中的连续范围。  
保存时记录：

- 原句。
- 目标开始位置。
- 目标长度。
- 规范化表达。

用户修改原句后，需要重新定位目标表达：

1. 优先使用原始索引。
2. 如果索引内容不匹配，搜索完全匹配。
3. 多处匹配时要求用户确认。
4. 无匹配时阻止保存并提示重新选中。

---

# 12. SQLite 数据模型

数据库路径建议：

```text
user://data/gamelexicon.db
```

实际 Windows 路径由 Godot `user://` 决定。  
不要硬编码 `%APPDATA%` 到业务层。

## 12.1 表结构

```sql
CREATE TABLE schema_migrations (
    version INTEGER PRIMARY KEY,
    applied_at_utc TEXT NOT NULL
);

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
    FOREIGN KEY (capture_id) REFERENCES captures(id) ON DELETE CASCADE
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
    FOREIGN KEY (ocr_region_id) REFERENCES ocr_regions(id) ON DELETE CASCADE
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
    FOREIGN KEY (capture_id) REFERENCES captures(id) ON DELETE RESTRICT,
    FOREIGN KEY (ocr_region_id) REFERENCES ocr_regions(id) ON DELETE SET NULL
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
    FOREIGN KEY (entry_id) REFERENCES vocabulary_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (example_id) REFERENCES sentence_examples(id) ON DELETE CASCADE
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
    FOREIGN KEY (entry_id) REFERENCES vocabulary_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
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
    FOREIGN KEY (entry_id) REFERENCES vocabulary_entries(id) ON DELETE CASCADE
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
    FOREIGN KEY (review_card_id) REFERENCES review_cards(id) ON DELETE CASCADE
);

CREATE TABLE app_settings (
    key TEXT PRIMARY KEY,
    value_json TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL
);
```

## 12.2 数据库约束

- 启用外键：

```sql
PRAGMA foreign_keys = ON;
```

- MVP 可启用 WAL：

```sql
PRAGMA journal_mode = WAL;
```

- 所有时间以 UTC ISO 8601 保存。
- UI 显示时转换为本地时间。
- 所有 GUID 统一保存为小写字符串。
- 所有写操作使用事务。
- 删除截图前检查引用。
- 删除词条默认先进入“归档”，永久删除必须二次确认。

## 12.3 迁移

目录：

```text
src/GameLexicon.Infrastructure/Persistence/Migrations/
  Migration001_Initial.cs
  Migration002_Example.cs
```

接口：

```csharp
public interface IDatabaseMigration
{
    int Version { get; }
    Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken);
}
```

启动流程：

1. 打开数据库。
2. 创建 `schema_migrations`。
3. 按版本升序执行未应用迁移。
4. 每个迁移独立事务。
5. 失败时停止启动并保留错误日志。
6. 不允许跳过失败迁移。

---

# 13. 服务接口

## 13.1 屏幕捕获

```csharp
public interface IScreenCaptureService
{
    Task<IReadOnlyList<CaptureSourceDto>> GetSourcesAsync(
        CancellationToken cancellationToken);

    Task<CaptureResultDto> CaptureAsync(
        CaptureRequestDto request,
        CancellationToken cancellationToken);
}
```

MVP 中，Godot 主应用不直接实现该接口。  
实现类通过 CaptureBridge 收件箱获取结果：

```csharp
public sealed class CaptureInboxService : IScreenCaptureInbox
{
    public Task<IReadOnlyList<CaptureManifest>> ScanPendingAsync(
        CancellationToken cancellationToken);
}
```

## 13.2 OCR

```csharp
public interface IOcrService
{
    Task<OcrResultDto> RecognizeAsync(
        OcrRequestDto request,
        IProgress<OcrProgressDto>? progress,
        CancellationToken cancellationToken);
}
```

```csharp
public sealed record OcrRequestDto(
    string ImagePath,
    PixelRect Region,
    string Language,
    OcrSegmentationMode SegmentationMode);
```

```csharp
public sealed record OcrResultDto(
    string RawText,
    IReadOnlyList<OcrTokenDto> Tokens,
    double MeanConfidence,
    TimeSpan Duration);
```

## 13.3 TTS

```csharp
public interface ITextToSpeechService
{
    IReadOnlyList<TtsVoiceDto> GetVoices(string languagePrefix);
    bool IsSpeaking { get; }
    void Speak(TtsRequestDto request);
    void Stop();
}
```

## 13.4 词典 Provider

```csharp
public interface IDictionaryProvider
{
    string ProviderId { get; }

    Task<DictionaryLookupResult?> LookupAsync(
        string expression,
        CancellationToken cancellationToken);
}
```

MVP 提供：

```text
ManualDictionaryProvider
```

返回空结果，允许用户手工填写。  
后续添加在线 Provider 时，必须由用户主动配置。

## 13.5 Repository

```csharp
public interface IVocabularyRepository
{
    Task<VocabularyEntry?> FindByNormalizedHeadwordAsync(
        string normalizedHeadword,
        CancellationToken cancellationToken);

    Task<VocabularyEntryDetails?> GetDetailsAsync(
        Guid entryId,
        CancellationToken cancellationToken);

    Task<PagedResult<VocabularyEntrySummary>> SearchAsync(
        VocabularySearchQuery query,
        CancellationToken cancellationToken);

    Task SaveAsync(
        VocabularyEntry aggregate,
        CancellationToken cancellationToken);
}
```

---

# 14. CaptureBridge 实现规格

## 14.1 进程行为

启动参数：

```text
GameLexicon.CaptureBridge.exe
  --inbox "<absolute path>"
  --hotkey "Ctrl+Shift+E"
  --mode "active-display"
```

退出方式：

- 主应用退出时发送退出信号。
- 或 CaptureBridge 监控父进程 ID。
- 父进程不存在后自动退出。
- 异常退出时不得留下永久占用快捷键的进程。

## 14.2 全局快捷键

使用 Win32：

```text
RegisterHotKey
UnregisterHotKey
GetMessage / PeekMessage
```

要求：

- 快捷键冲突时返回明确错误。
- 设置页能执行“测试注册”。
- 修改快捷键后注销旧快捷键。
- 禁止默认使用系统保留组合。
- 记录按键，但不得记录用户其他键盘输入。

## 14.3 捕获源

MVP：

- 当前鼠标所在显示器，或
- 当前前台窗口所在显示器。

设置项：

```text
CaptureMode:
- ActiveDisplay
- SelectedDisplay
```

后续：

```text
- SelectedWindow
- ActiveWindow
```

## 14.4 文件写入安全

- 每个捕获使用独立 UUID。
- 使用临时文件和原子重命名。
- PNG 写完后才生成 manifest。
- 写入失败时生成 `.error.json`。
- 日志不得包含截图像素或 OCR 文本。
- 默认最多保留 500 条待处理截图；达到上限时提示，不静默覆盖。

## 14.5 游戏兼容性

提示用户：

- 优先使用无边框窗口模式。
- 某些独占全屏、HDR、受保护视频或 DRM 内容可能得到黑屏。
- 本工具不尝试绕过保护机制。
- 如果捕获失败，允许使用系统截图后手动导入图片。

---

# 15. OCR 实现规格

## 15.1 Tesseract 调用

建议调用：

```text
tesseract "<input.png>" stdout -l eng --oem 1 --psm 6 tsv
```

注意：

- 必须异步执行。
- 不得在 Godot 主线程上使用阻塞式 `OS.execute`。
- 使用 `System.Diagnostics.Process`。
- 捕获 stdout 和 stderr。
- 设置超时，默认 30 秒。
- 支持取消。
- 进程失败后显示退出码和简化错误。
- 日志保留详细错误，但 UI 不显示难以理解的堆栈。

建议实现：

```csharp
public sealed class TesseractCliOcrService : IOcrService
{
    private readonly string _executablePath;
    private readonly string _tessdataPath;
    private readonly IOcrTsvParser _parser;
}
```

## 15.2 Tesseract 路径查找

顺序：

1. 用户设置的路径。
2. 应用随附工具目录。
3. 环境变量 `PATH`。
4. 常见 Windows 安装路径。
5. 找不到时进入依赖修复页。

不得假定 Tesseract 一定安装。

## 15.3 OCR TSV 解析

需要读取的列：

```text
level
page_num
block_num
par_num
line_num
word_num
left
top
width
height
conf
text
```

规则：

- `conf < 0` 的非单词行忽略。
- 空文本忽略。
- `confidence` 转为 `0–100`。
- 坐标转换为原始截图像素坐标。
- 保留行信息。
- 依照 block、paragraph、line、word 排序。

## 15.4 置信度显示

默认阈值：

```text
>= 85：高
60–84：中
< 60：低
```

阈值可配置。

UI：

- 高：普通框。
- 中：虚线框。
- 低：醒目框并加入“待检查”列表。

不要仅依赖颜色表达状态，应同时使用图标、线型或文字。

## 15.5 图像预处理

MVP 可先只做：

1. 裁剪。
2. 2 倍放大。
3. 保持 PNG。
4. 可选灰度。

V1.1 增加：

- 对比度。
- 二值化。
- 反色。
- 去噪。
- 锐化。
- 1×、2×、3× 缩放。
- 自动选择最佳结果。

OCR 设置：

```text
Preset:
- SubtitleLightText
- SubtitleDarkText
- UICompactText
- Raw
```

## 15.6 OCR 缓存

缓存键：

```text
SHA256(
    cropped_image_bytes
    + language
    + psm
    + preprocessing_options
)
```

相同输入不重复 OCR。

---

# 16. Godot TTS 实现

在 `project.godot` 启用文本转语音设置。

服务初始化：

1. 调用 `DisplayServer.TtsGetVoices()`。
2. 筛选语言以 `en` 开头的语音。
3. 优先用户保存的语音 ID。
4. 语音不存在时选择首个英文语音。
5. 没有英文语音时显示系统设置提示。

示例 C# 结构：

```csharp
public sealed class GodotTextToSpeechService : ITextToSpeechService
{
    public IReadOnlyList<TtsVoiceDto> GetVoices(string languagePrefix)
    {
        var voices = DisplayServer.TtsGetVoices();
        var result = new List<TtsVoiceDto>();

        foreach (var item in voices)
        {
            var dictionary = item.AsGodotDictionary();
            var id = dictionary["id"].AsString();
            var name = dictionary["name"].AsString();
            var language = dictionary["language"].AsString();

            if (language.StartsWith(
                languagePrefix,
                StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new TtsVoiceDto(id, name, language));
            }
        }

        return result;
    }

    public void Speak(TtsRequestDto request)
    {
        DisplayServer.TtsSpeak(
            request.Text,
            request.VoiceId,
            request.Volume,
            request.Pitch,
            request.Rate,
            request.UtteranceId,
            request.Interrupt);
    }

    public void Stop()
    {
        DisplayServer.TtsStop();
    }

    public bool IsSpeaking => DisplayServer.TtsIsSpeaking();
}
```

要求：

- 文本为空时不播放。
- 同一按钮快速点击不能无限堆积队列。
- 默认 `interrupt = true`。
- 原句过长时允许用户停止。
- 不将系统 TTS 结果声称为真人发音。
- MVP 不要求导出音频文件。

---

# 17. 复习算法

## 17.1 目标

MVP 使用简单、透明、可测试的间隔算法。  
不追求完整复刻任何第三方软件算法。

## 17.2 评分枚举

```csharp
public enum ReviewGrade
{
    Again = 0,
    Hard = 1,
    Good = 2,
    Easy = 3
}
```

## 17.3 更新规则

初始值：

```text
repetition = 0
interval_days = 0
ease_factor = 2.5
due_at = now
```

### Again

```text
repetition = 0
lapse_count += 1
interval_days = 10 分钟对应的天数
ease_factor = max(1.3, ease_factor - 0.20)
due_at = now + 10 分钟
```

### Hard

```text
repetition += 1
interval_days =
  如果旧间隔 < 1 天：1 天
  否则：max(1, old_interval * 1.2)
ease_factor = max(1.3, ease_factor - 0.15)
due_at = now + interval_days
```

### Good

```text
repetition += 1

如果 repetition == 1：
    interval_days = 1
否则如果 repetition == 2：
    interval_days = 3
否则：
    interval_days = max(1, old_interval * ease_factor)

ease_factor 不变
due_at = now + interval_days
```

### Easy

```text
repetition += 1

如果 repetition == 1：
    interval_days = 4
否则：
    interval_days = max(2, old_interval * ease_factor * 1.3)

ease_factor = min(3.0, ease_factor + 0.15)
due_at = now + interval_days
```

存储前将间隔保留到小数点后三位。  
显示时使用用户友好的时间。

## 17.4 同日学习步

MVP 至少支持：

- 新词第一次 Good：次日。
- Again：10 分钟后。
- 同一会话内允许再次出现 Again 卡片。
- 一次会话中 Again 卡片最多重新出现两次，防止无限循环。

## 17.5 复习队列顺序

默认：

1. 逾期卡。
2. 今日到期卡。
3. 新卡。
4. Again 重学卡。

同一表达的不同题型不应连续出现。  
同一游戏来源可打散。

## 17.6 题型生成规则

### 英译中

必须有：

- 中文释义，或
- 英文释义。

两者都为空时不生成。

### 填空题

必须满足：

- 原句包含目标表达。
- `TargetStart` 和 `TargetLength` 有效。
- 隐去后仍有足够上下文。
- 原句不能只剩目标表达。

填空文本：

```text
sentence[..start] + blank + sentence[(start + length)..]
```

空格长度不要直接暴露准确字母数。  
统一使用：

```text
_____
```

### 四选一

干扰项筛选优先级：

1. 相同 `EntryType`。
2. 相同词性。
3. 长度相近。
4. 不与正确答案规范化后相同。
5. 释义不能为空。
6. 不从同义词中随机选择造成多解。

不足 3 个合格干扰项时，不生成四选一题。

### 听力题

- 优先播放目标表达。
- 可切换播放完整原句。
- 必须允许重播。
- 没有可用英文 TTS 时跳过该题型。

---

# 18. 词条和例句策略

## 18.1 一个表达，多条例句

数据组织：

```text
VocabularyEntry
  ├─ Example A：Game 1
  ├─ Example B：Game 2
  └─ Example C：Game 1
```

复习时：

- 默认轮换例句。
- 最近连续两次尽量不使用同一例句。
- 用户可固定主要例句。
- 新添加例句不重置整个词条的学习进度。
- 可选择为新例句单独生成填空卡。

## 18.2 重复检测

精确重复：

```text
NormalizedHeadword 完全相同
```

近似重复建议：

- 连字符与空格差异。
- 单复数。
- 大小写。
- 尾部标点。
- 缩写形式。

MVP 只自动阻止精确重复。  
近似重复只提示，不自动合并。

## 18.3 游戏来源

游戏来源由以下顺序确定：

1. 用户手工选择。
2. 进程名到游戏名的用户映射。
3. 窗口标题规则。
4. 未知游戏。

映射表：

```text
source_process_name → game_title
```

用户修正一次后，下次复用。

---

# 19. 应用设置

设置分组：

## 19.1 截图

- 快捷键。
- 捕获模式。
- 默认显示器。
- 是否播放提示音。
- 是否显示通知。
- 是否只加入收件箱。
- 收件箱容量。
- CaptureBridge 自动启动。

## 19.2 OCR

- Tesseract 路径。
- tessdata 路径。
- 语言。
- PSM。
- 预处理预设。
- 低置信度阈值。
- OCR 超时。

## 19.3 发音

- 英语语音。
- 语速。
- 音高。
- 音量。
- 播放新题时是否自动发音。

## 19.4 复习

- 每日新词上限。
- 每日复习上限。
- 启用题型。
- 新词和复习卡混合比例。
- 是否显示截图。
- 是否随机例句。

## 19.5 数据

- 数据目录。
- 导出。
- 导入。
- 创建备份。
- 自动备份频率。
- 原截图保留时间。
- 删除已处理原截图。
- 清理 OCR 缓存。

## 19.6 隐私

- 本地模式状态。
- 已启用的在线 Provider。
- 在线请求前是否二次确认。
- 截图发送范围说明。
- 清除历史。

---

# 20. 错误处理

统一错误类型：

```csharp
public enum AppErrorCode
{
    Unknown,
    DatabaseOpenFailed,
    DatabaseMigrationFailed,
    CaptureBridgeMissing,
    CaptureBridgeLaunchFailed,
    HotkeyConflict,
    CaptureFailed,
    CaptureFileMissing,
    OcrExecutableMissing,
    OcrLanguageMissing,
    OcrTimedOut,
    OcrProcessFailed,
    OcrParseFailed,
    TtsUnavailable,
    TtsVoiceMissing,
    InvalidTextSelection,
    DuplicateEntry,
    ExportFailed,
    ImportFailed
}
```

UI 错误必须包含：

- 发生了什么。
- 用户可以做什么。
- 是否可以重试。
- “查看日志”入口。

示例：

```text
未找到 Tesseract OCR。
你仍然可以手工输入英文内容，或在“设置 → OCR”中选择 tesseract.exe。
[选择路径] [稍后处理]
```

不能只显示：

```text
Process exited with code -1
```

---

# 21. 日志

日志目录：

```text
user://logs/
```

文件：

```text
gamelexicon-YYYYMMDD.log
capturebridge-YYYYMMDD.log
```

要求：

- 默认保留 14 天。
- 单文件最大 10 MB。
- 记录：
  - 启动。
  - 版本。
  - 数据库迁移。
  - CaptureBridge 状态。
  - OCR 时长。
  - OCR 退出码。
  - 导入导出结果。
- 默认不记录：
  - 截图像素。
  - 完整 OCR 文本。
  - 完整释义。
  - 用户 API 密钥。
- 调试模式可由用户主动开启，且界面明确提示可能包含学习文本。

---

# 22. 隐私、安全与合规

## 22.1 默认本地处理

以下内容默认不离开设备：

- 截图。
- OCR 图像区域。
- OCR 文本。
- 词条。
- 例句。
- 复习记录。

## 22.2 在线 Provider

后续启用在线功能时必须：

- 用户主动选择。
- 显示将发送的数据类型。
- 允许只发送裁剪区域。
- 默认不发送整张截图。
- 明确 Provider 名称。
- 允许立即禁用。
- API Key 不写入普通日志。
- API Key 不提交 Git。

## 22.3 游戏兼容与反作弊

必须遵守：

- 不向游戏进程注入。
- 不读取游戏内存。
- 不模拟游戏操作。
- 不绕过保护机制。
- 仅使用系统截图能力。
- 对竞技游戏建议使用系统截图后手动导入模式。
- 用户需自行遵守游戏服务条款和素材版权要求。

## 22.4 截图生命周期

默认建议：

- 原始截图：保留 30 天。
- 与词条关联的裁剪图：长期保留。
- 未处理截图：不自动删除，除非达到容量阈值并征得用户同意。
- 删除关联截图时提示可能影响例句上下文。

---

# 23. 导入导出

## 23.1 完整备份

格式：

```text
GameLexiconBackup/
  backup.json
  database.sqlite
  media/
  manifest.json
```

`manifest.json`：

```json
{
  "backup_schema_version": 1,
  "app_version": "0.1.0",
  "created_at_utc": "2026-08-01T08:00:00Z",
  "includes_database": true,
  "includes_media": true
}
```

备份数据库时：

- 先执行 SQLite checkpoint。
- 使用 SQLite backup API，或在确保连接安全的情况下复制。
- 不直接复制仍处于不一致状态的 WAL 数据库。

## 23.2 CSV

建议列：

```text
headword
entry_type
part_of_speech
translation_chinese
definition_english
phonetic
notes
primary_sentence
game_title
tags
created_at
next_due_at
```

## 23.3 Anki TSV

字段：

```text
Expression
Chinese
EnglishDefinition
Sentence
ClozeSentence
Game
Tags
Image
```

图片导出：

```text
media/{example_id}.png
```

TSV 中使用：

```html
<img src="example-id.png">
```

## 23.4 欧路词典兼容

V1.1 增加 CSV 映射导入：

- 用户选择源列。
- 预览前 20 行。
- 映射表达、释义、例句、标签。
- 不假定欧路不同版本的导出格式完全一致。
- 导入前创建自动备份。

---

# 24. 可补充的产品功能

以下功能值得加入后续规划。

## 24.1 快速收集模式

游戏过程中只截图，不弹出编辑界面。  
截图通知显示：

```text
已加入待整理队列：第 12 张
```

这是减少打断感的高优先级功能。

## 24.2 语境时间线

在同一游戏内按时间查看截图和词条：

```text
章节/日期
  ├─ 截图
  ├─ 原句
  └─ 保存的短语
```

有助于回忆剧情。

## 24.3 表达出现次数

同一个表达再次出现时：

- 增加出现次数。
- 保存新的例句。
- 提示“这是你第 3 次遇到该表达”。

## 24.4 OCR 纠错词典

用户修正常见 OCR 错误后建立局部规则：

```text
l → I
rn → m
0 → O
```

规则必须由用户确认，不能全局盲目替换。

## 24.5 游戏专属词表

按游戏生成：

- 高频表达。
- 新增词条。
- 已掌握词条。
- 常见角色用语。
- 任务和战斗词汇。

## 24.6 学习统计

- 每日新增。
- 每日复习。
- 正确率。
- Again 比例。
- 最常见来源游戏。
- 最难表达。
- OCR 平均置信度。
- 从截图到完成词条的平均时间。

## 24.7 暂停复习

支持：

- 暂停某个词条。
- 暂停某种题型。
- 归档已掌握表达。
- 只复习某个游戏。

## 24.8 手工添加

不依赖截图：

- 粘贴句子。
- 选择短语。
- 添加词条。
- 导入图片。
- 从剪贴板创建。

---

# 25. 性能目标

MVP 目标：

- 冷启动：普通 SSD 上不超过 5 秒。
- 首页打开：不超过 1 秒。
- 10000 个词条搜索：普通关键词 300 毫秒内返回首屏。
- 收件箱 500 张截图：列表滚动保持可用。
- 1080p 区域 OCR：典型情况下 5 秒内完成，具体取决于硬件和区域。
- UI 主线程不得被 OCR 阻塞。
- 数据库写入失败不得丢失原始截图。
- 崩溃后已完成 manifest 的截图仍可恢复。

---

# 26. 可访问性

- 所有主要按钮支持键盘焦点。
- 不能只用颜色表示 OCR 置信度。
- 复习评分按钮支持数字键：
  - `1` Again
  - `2` Hard
  - `3` Good
  - `4` Easy
- 发音按钮有文本标签或 Tooltip。
- 字体大小可调整。
- 截图缩放支持键盘。
- 所有错误提示可复制。
- 主要控件设置 Godot accessibility 属性（在目标版本支持范围内）。

---

# 27. 自动化测试

## 27.1 Domain Tests

必须覆盖：

### 文本规范化

```text
" Get   Out! " → "get out"
"Don't" → "don't"
"well-known" 保留连字符
```

### 目标范围验证

- 范围合法。
- 范围越界。
- 修改句子后重新定位。
- 多次出现目标表达。

### 复习算法

对四种评分验证：

- repetition。
- interval。
- ease。
- lapse。
- due 时间。
- ease 上下限。

### 填空生成

- 单词。
- 多词短语。
- 句首。
- 句尾。
- 重复短语。
- 无效索引。

### 干扰项选择

- 排除正确答案。
- 排除重复。
- 不足三项时失败。
- 优先相同类型。

## 27.2 Application Tests

使用内存 Fake Repository：

- 导入截图 manifest。
- 同一 capture_id 不重复导入。
- OCR 成功。
- OCR 失败状态。
- 创建新词条。
- 合并已有词条例句。
- 创建复习卡。
- 提交复习评分。
- 导出 DTO。

## 27.3 Infrastructure Tests

使用临时目录和临时数据库：

- 初次迁移。
- 重复迁移无副作用。
- Repository CRUD。
- 外键级联。
- WAL 数据库备份。
- Tesseract TSV parser。
- manifest 原子导入。
- CSV 转义。
- JSON 备份 round-trip。

不要求 CI 环境安装真实 Tesseract。  
真实 OCR 使用单独的可选集成测试。

---

# 28. 人工验收清单

## 28.1 截图

- [ ] 主程序在后台时快捷键有效。
- [ ] 快捷键冲突有明确提示。
- [ ] 多显示器能捕获正确显示器。
- [ ] 截图不包含半写入文件。
- [ ] 截图失败后主程序不崩溃。
- [ ] 游戏焦点不会被强制抢走。
- [ ] CaptureBridge 随主程序退出。

## 28.2 OCR

- [ ] 能框选区域。
- [ ] OCR 不阻塞 UI。
- [ ] 能取消 OCR。
- [ ] 低置信度词有标记。
- [ ] 用户可修改 OCR 文本。
- [ ] 原始 OCR 文本仍保留。
- [ ] Tesseract 缺失时能手工输入。

## 28.3 词条

- [ ] 可选择单词。
- [ ] 可选择连续短语。
- [ ] 可保存完整游戏原句。
- [ ] 可保存截图裁剪。
- [ ] 重复表达可合并例句。
- [ ] 可以按游戏筛选。
- [ ] 删除和归档行为正确。

## 28.4 TTS

- [ ] 能列出英文语音。
- [ ] 能播放单词。
- [ ] 能播放短语。
- [ ] 能播放原句。
- [ ] 能停止。
- [ ] 快速点击不会无限排队。
- [ ] 无英文语音时提示明确。

## 28.5 复习

- [ ] 今日到期卡出现。
- [ ] Again 进入重学。
- [ ] 四个评分改变排期。
- [ ] 填空范围正确。
- [ ] 四选一没有重复选项。
- [ ] 显示游戏原句和截图。
- [ ] 复习历史被记录。

## 28.6 数据

- [ ] 关闭并重开后数据存在。
- [ ] 数据库迁移可重复启动。
- [ ] JSON 备份可恢复。
- [ ] CSV 正确处理逗号、引号和换行。
- [ ] 删除原图前有提示。
- [ ] 日志不包含 API Key。

---

# 29. 开发里程碑

## Milestone 0：工程骨架

### 任务

- [ ] 创建解决方案和项目目录。
- [ ] 创建 Godot 4.7.1 .NET 工程。
- [ ] 创建 Domain、Application、Infrastructure、CaptureBridge 和 Tests 项目。
- [ ] 配置项目引用。
- [ ] 创建 `App.tscn`。
- [ ] 创建侧边导航占位页。
- [ ] 创建日志服务。
- [ ] 创建配置服务。
- [ ] 创建 `IMPLEMENTATION_STATUS.md`。
- [ ] 创建 PowerShell 启动和测试脚本。

### 验收

- [ ] `dotnet build GameLexicon.sln` 成功。
- [ ] Godot 编辑器能打开工程。
- [ ] 主场景能运行。
- [ ] 页面导航可切换。
- [ ] 测试项目至少有一个通过的 smoke test。

### 不在本里程碑做

- OCR。
- 截图。
- SQLite 业务表。
- 复习。

---

## Milestone 1：数据库与词条基础

### 任务

- [ ] 实现 SQLite 连接工厂。
- [ ] 实现迁移框架。
- [ ] 创建初始表。
- [ ] 实现词条 Repository。
- [ ] 实现例句 Repository。
- [ ] 实现标签 Repository。
- [ ] 实现规范化服务。
- [ ] 创建手工添加词条页面。
- [ ] 创建词条库页面。
- [ ] 创建词条详情页。
- [ ] 添加 CRUD 测试。

### 验收

- [ ] 可手工添加短语。
- [ ] 可填写中英文释义。
- [ ] 可填写游戏原句。
- [ ] 重启后数据存在。
- [ ] 精确重复时弹出合并提示。
- [ ] 一个词条能保存两条例句。
- [ ] 可搜索和按游戏筛选。

---

## Milestone 2：CaptureBridge 与截图收件箱

### 任务

- [ ] 实现 CaptureBridge 命令行参数。
- [ ] 实现父进程监控。
- [ ] 实现全局快捷键。
- [ ] 实现显示器枚举。
- [ ] 实现 Windows Graphics Capture。
- [ ] 实现 PNG 和 manifest 原子写入。
- [ ] 实现主程序启动/关闭桥接进程。
- [ ] 实现收件箱扫描。
- [ ] 实现截图导入和去重。
- [ ] 实现截图列表与预览。
- [ ] 实现手工导入图片。

### 验收

- [ ] Godot 后台时能通过快捷键截图。
- [ ] 截图出现在收件箱。
- [ ] 同一 manifest 不重复导入。
- [ ] 主程序关闭后 CaptureBridge 自动退出。
- [ ] 捕获失败有可读错误。
- [ ] 手工导入图片可以替代截图。

---

## Milestone 3：框选与 OCR

### 任务

- [ ] 实现 ScreenshotCanvas。
- [ ] 实现缩放和平移。
- [ ] 实现区域框选。
- [ ] 保存区域坐标。
- [ ] 实现 Tesseract 路径检测。
- [ ] 实现异步 OCR 进程。
- [ ] 实现 TSV parser。
- [ ] 实现 token overlay。
- [ ] 实现置信度标记。
- [ ] 实现 OCR 文本编辑。
- [ ] 保留原始文本和校正文本。
- [ ] 实现取消和超时。
- [ ] 添加 parser 单元测试。

### 验收

- [ ] 能对游戏字幕区域 OCR。
- [ ] UI 在 OCR 时保持响应。
- [ ] 词框与截图位置基本一致。
- [ ] 低置信度词可识别。
- [ ] 用户能手工修正。
- [ ] OCR 失败后可以重试或手工输入。

---

## Milestone 4：从原句创建词条

### 任务

- [ ] 实现句子切分。
- [ ] 实现文本范围选择。
- [ ] 实现 token 连续选择。
- [ ] 实现候选词条 DTO。
- [ ] 实现目标范围验证。
- [ ] 实现截图裁剪保存。
- [ ] 实现创建词条用例。
- [ ] 实现合并已有词条例句。
- [ ] 实现一张截图添加多个词条。
- [ ] 实现“标记截图完成”。

### 验收

- [ ] 从游戏原句选择单词能保存。
- [ ] 选择多词短语能保存。
- [ ] 保存后词条包含完整原句。
- [ ] 保存后词条包含截图裁剪。
- [ ] 重复短语可新增例句。
- [ ] 重新打开词条仍能正确高亮短语。

---

## Milestone 5：TTS

### 任务

- [ ] 在项目设置启用 TTS。
- [ ] 实现 Godot TTS adapter。
- [ ] 实现语音枚举。
- [ ] 实现设置保存。
- [ ] 为词条、短语和原句添加播放按钮。
- [ ] 实现停止播放。
- [ ] 实现无语音错误状态。
- [ ] 添加快速连续点击保护。

### 验收

- [ ] 能播放英文表达。
- [ ] 能播放完整例句。
- [ ] 能切换系统英文语音。
- [ ] 语速设置重启后保留。
- [ ] 无英文语音时不崩溃。

---

## Milestone 6：复习系统

### 任务

- [ ] 创建 ReviewCard 和 ReviewLog。
- [ ] 实现复习迁移。
- [ ] 实现排期算法。
- [ ] 实现到期查询。
- [ ] 实现复习会话构建。
- [ ] 实现英译中题。
- [ ] 实现填空题。
- [ ] 实现四选一题。
- [ ] 实现听力题。
- [ ] 实现 Again/Hard/Good/Easy。
- [ ] 实现会话结果页。
- [ ] 实现首页今日数量。
- [ ] 添加算法单元测试。

### 验收

- [ ] 新词能进入复习。
- [ ] 四种题型按条件生成。
- [ ] 评分后下次时间正确。
- [ ] Again 能在同一会话再次出现。
- [ ] 复习显示原句和截图。
- [ ] 复习日志正确记录。

---

## Milestone 7：导入、导出与备份

### 任务

- [ ] 实现 JSON 完整备份。
- [ ] 实现 JSON 恢复。
- [ ] 实现 CSV 导出。
- [ ] 实现 Anki TSV 导出。
- [ ] 实现媒体文件复制。
- [ ] 实现导出预览。
- [ ] 导入前自动备份。
- [ ] 添加 round-trip 测试。

### 验收

- [ ] 备份后可恢复词条和复习记录。
- [ ] CSV 可由常见表格软件打开。
- [ ] TSV 可映射到 Anki。
- [ ] 图片相对路径正确。
- [ ] 导出失败不破坏数据库。

---

## Milestone 8：稳定性与打包

### 任务

- [ ] 完成依赖检查向导。
- [ ] 完成首次启动引导。
- [ ] 完成日志查看。
- [ ] 完成截图清理。
- [ ] 完成数据库自动备份。
- [ ] 完成窗口状态保存。
- [ ] 完成高 DPI 测试。
- [ ] 完成多显示器测试。
- [ ] 完成打包脚本。
- [ ] 完成许可证清单。
- [ ] 完成用户文档。
- [ ] 完成手工验收清单。

### 验收

- [ ] 在干净 Windows 环境可安装并启动。
- [ ] 缺少 OCR 依赖时提示正确。
- [ ] 运行一小时无明显内存持续增长。
- [ ] 1000 个词条仍可流畅搜索。
- [ ] 崩溃后收件箱截图不丢失。
- [ ] 卸载时用户数据不被意外删除。

---

# 30. Codex 任务拆分模板

Codex 每次只领取一个任务，并按以下格式记录到：

```text
docs/IMPLEMENTATION_STATUS.md
```

模板：

```markdown
## 当前任务

- Task ID: M3-T04
- 名称: 实现 Tesseract TSV Parser
- 状态: In Progress
- 开始时间:
- 涉及文件:
- 依赖:
- 计划:
  1.
  2.
  3.

## 完成记录

- 状态: Done
- 自动化测试:
- 人工验收:
- 已知限制:
- 后续任务:
```

Task ID 规则：

```text
M{里程碑编号}-T{两位任务编号}
```

示例：

```text
M0-T01
M3-T04
M6-T09
```

---

# 31. 推荐的首批 Codex 任务

## M0-T01：创建解决方案

完成：

```text
GameLexicon.sln
Domain
Application
Infrastructure
CaptureBridge
Tests
```

验收：

```text
dotnet build GameLexicon.sln
```

成功。

## M0-T02：创建 Godot .NET 工程

完成：

- `project.godot`
- `App.tscn`
- `AppRoot.cs`
- 基础窗口参数。

验收：

- Godot 可运行空主界面。

## M0-T03：实现基础导航

完成：

- Sidebar。
- RouteHost。
- 六个占位页面。
- `NavigationService`。

验收：

- 点击导航不重新创建整个 AppRoot。
- 当前页面有选中状态。

## M0-T04：配置与日志

完成：

- JSON 配置。
- 日志目录。
- 日志滚动。
- 开发模式开关。

验收：

- 重启后配置保留。
- 日志中没有敏感文本。

## M1-T01：SQLite 连接和迁移

完成：

- `SqliteConnectionFactory`
- `MigrationRunner`
- `Migration001_Initial`

验收：

- 首次启动建库。
- 第二次启动不重复迁移。
- 测试数据库可删除。

## M1-T02：文本规范化

完成：

- `ITextNormalizer`
- `EnglishExpressionNormalizer`

验收：

- 通过本文件第 27 节中的规范化测试。

---

# 32. 最小代码骨架

## 32.1 AppServices

```csharp
public sealed class AppServices
{
    public static AppServices Instance { get; private set; } = null!;

    public IVocabularyRepository VocabularyRepository { get; }
    public IReviewRepository ReviewRepository { get; }
    public IOcrService OcrService { get; }
    public ITextToSpeechService TextToSpeechService { get; }
    public IAppSettingsService SettingsService { get; }

    private AppServices(
        IVocabularyRepository vocabularyRepository,
        IReviewRepository reviewRepository,
        IOcrService ocrService,
        ITextToSpeechService textToSpeechService,
        IAppSettingsService settingsService)
    {
        VocabularyRepository = vocabularyRepository;
        ReviewRepository = reviewRepository;
        OcrService = ocrService;
        TextToSpeechService = textToSpeechService;
        SettingsService = settingsService;
    }

    public static async Task InitializeAsync(
        string userDataPath,
        CancellationToken cancellationToken)
    {
        // 1. Load configuration.
        // 2. Initialize logging.
        // 3. Open database.
        // 4. Run migrations.
        // 5. Create repositories.
        // 6. Create OCR and TTS adapters.
        // 7. Assign Instance only after successful initialization.
        await Task.CompletedTask;
    }
}
```

不要让 View 直接访问静态单例中的数据库连接。  
View 调用 UseCase 或 ViewModel。

## 32.2 OCR 进程

```csharp
public async Task<ProcessResult> RunAsync(
    string executable,
    IReadOnlyList<string> arguments,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    using var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    foreach (var argument in arguments)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }

    if (!process.Start())
    {
        throw new InvalidOperationException("Failed to start OCR process.");
    }

    var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
    var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken);
    timeoutCts.CancelAfter(timeout);

    try
    {
        await process.WaitForExitAsync(timeoutCts.Token);
    }
    catch (OperationCanceledException)
    {
        TryKillProcessTree(process);
        throw;
    }

    return new ProcessResult(
        process.ExitCode,
        await stdoutTask,
        await stderrTask);
}
```

## 32.3 复习排期

```csharp
public ReviewScheduleResult Schedule(
    ReviewCard card,
    ReviewGrade grade,
    DateTimeOffset now)
{
    var oldInterval = card.IntervalDays;
    var oldEase = card.EaseFactor;

    switch (grade)
    {
        case ReviewGrade.Again:
            card.Repetition = 0;
            card.LapseCount += 1;
            card.IntervalDays = 10.0 / 1440.0;
            card.EaseFactor = Math.Max(1.3, card.EaseFactor - 0.20);
            break;

        case ReviewGrade.Hard:
            card.Repetition += 1;
            card.IntervalDays = oldInterval < 1
                ? 1
                : Math.Max(1, oldInterval * 1.2);
            card.EaseFactor = Math.Max(1.3, card.EaseFactor - 0.15);
            break;

        case ReviewGrade.Good:
            card.Repetition += 1;
            card.IntervalDays = card.Repetition switch
            {
                1 => 1,
                2 => 3,
                _ => Math.Max(1, oldInterval * card.EaseFactor)
            };
            break;

        case ReviewGrade.Easy:
            card.Repetition += 1;
            card.IntervalDays = card.Repetition == 1
                ? 4
                : Math.Max(2, oldInterval * card.EaseFactor * 1.3);
            card.EaseFactor = Math.Min(3.0, card.EaseFactor + 0.15);
            break;

        default:
            throw new ArgumentOutOfRangeException(nameof(grade));
    }

    card.IntervalDays = Math.Round(card.IntervalDays, 3);
    card.LastReviewedAt = now;
    card.DueAt = now.AddDays(card.IntervalDays);

    return new ReviewScheduleResult(
        oldInterval,
        card.IntervalDays,
        oldEase,
        card.EaseFactor,
        card.DueAt);
}
```

## 32.4 填空生成

```csharp
public static string BuildCloze(
    string sentence,
    int start,
    int length)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(sentence);

    if (start < 0 ||
        length <= 0 ||
        start + length > sentence.Length)
    {
        throw new ArgumentOutOfRangeException(
            nameof(start),
            "Target range is outside the sentence.");
    }

    return string.Concat(
        sentence.AsSpan(0, start),
        "_____",
        sentence.AsSpan(start + length));
}
```

---

# 33. CI 建议

GitHub Actions 或其他 CI 至少执行：

```text
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
```

Godot 工程验证可使用目标版本的 headless editor：

```text
godot --headless --path app/GameLexicon.Godot --editor --quit
```

CaptureBridge 的真实截图集成测试不在普通 CI 执行。  
使用带桌面会话的 Windows 专用测试环境单独运行。

---

# 34. 打包策略

Windows 安装包应包含：

- Godot 导出程序。
- `.NET` 所需运行组件，按目标发布策略决定。
- CaptureBridge。
- 许可证文件。
- 默认配置。
- 可选 Tesseract 与 `eng.traineddata`。

如果分发 Tesseract：

- 保留其许可证。
- 记录版本。
- 不修改而不说明。
- 在“关于”页面列出第三方组件。

应用目录与用户数据目录分离：

```text
Program Files/GameLexicon/
AppData/GameLexicon/
```

用户升级时不得覆盖数据库和截图。

---

# 35. MVP 发布判定

仅当以下全部满足时，才能标记为 MVP：

- [ ] Windows 全局快捷键截图稳定。
- [ ] 截图能进入收件箱。
- [ ] 能框选并执行英文 OCR。
- [ ] OCR 可校正。
- [ ] 能选择单词和连续短语。
- [ ] 能保存游戏原句与截图。
- [ ] 重复表达能合并多个例句。
- [ ] 能播放表达与原句发音。
- [ ] 至少四种复习模式可用。
- [ ] 复习排期和日志可用。
- [ ] 能备份和导出。
- [ ] 缺失依赖时有降级方案。
- [ ] 不向游戏注入代码。
- [ ] 自动化测试通过。
- [ ] 人工验收清单通过。
- [ ] 用户文档完整。

---

# 36. 风险清单

## 风险 1：游戏截图黑屏

原因可能包括：

- 独占全屏。
- HDR。
- 受保护内容。
- 特定渲染方式。
- 系统权限。

缓解：

- 推荐无边框窗口模式。
- 允许切换捕获方式。
- 支持系统截图手工导入。
- 不尝试绕过保护。

## 风险 2：游戏字体导致 OCR 差

缓解：

- 区域框选。
- 放大。
- 多种预处理。
- 低置信度提示。
- 手工校正。
- 保存用户纠错历史。

## 风险 3：全局快捷键冲突

缓解：

- 首次启动测试。
- 可修改。
- 捕获桥接返回冲突错误。
- 提供托盘按钮和手工导入。

## 风险 4：词典版权和服务不稳定

缓解：

- MVP 不捆绑未授权词典内容。
- Provider 接口化。
- 手工释义始终可用。
- 用户自行配置合法服务。

## 风险 5：复习题多解

缓解：

- 干扰项严格筛选。
- 不足时不生成选择题。
- 用户可禁用某题型。
- 提供“题目有问题”标记。

## 风险 6：截图占用空间

缓解：

- 原图保留策略。
- 只长期保存词条裁剪。
- 容量提示。
- 清理预览。
- 备份时可排除原图。

---

# 37. 设计决策记录

## ADR-001：Windows 优先

**决定：** MVP 只支持 Windows 10/11。

**原因：**

- 游戏用户覆盖较高。
- 全局快捷键与屏幕捕获可先做稳定。
- 降低第一版跨平台复杂度。

**后果：**

- 所有平台能力必须在接口后面。
- Domain 和 Application 不得引用 Windows API。

## ADR-002：Godot UI + CaptureBridge

**决定：** 使用独立 CaptureBridge。

**原因：**

- 主程序失焦时仍需快捷键。
- 外部游戏画面不属于 Godot Viewport。
- 独立进程更容易隔离崩溃和平台代码。

## ADR-003：Tesseract CLI

**决定：** MVP 调用本地 Tesseract CLI。

**原因：**

- 离线。
- 可替换。
- 不需要在第一版维护原生 Godot 扩展。
- TSV 可提供词框和置信度。

## ADR-004：SQLite

**决定：** 使用 SQLite，而非单个 JSON 文件。

**原因：**

- 词条、例句、标签和复习记录存在关系。
- 需要查询、筛选、事务和迁移。
- 后续数据量可能较大。

## ADR-005：系统 TTS

**决定：** MVP 使用 Godot 系统 TTS。

**原因：**

- 无需网络。
- Godot 提供跨平台抽象。
- 足以完成听词和听句功能。

**限制：**

- 语音质量依赖系统安装。
- MVP 不缓存或导出音频。

---

# 38. 官方参考资料

以下链接用于实现时核对 API。Codex 应优先使用官方文档，不应依赖过时博客代码。

## Godot

- Godot 官方站点：  
  https://godotengine.org/
- Godot 4.7 稳定版文档：  
  https://docs.godotengine.org/en/stable/
- Godot C#/.NET：  
  https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/
- Godot 文本转语音：  
  https://docs.godotengine.org/en/stable/tutorials/audio/text_to_speech.html
- `DisplayServer`：  
  https://docs.godotengine.org/en/stable/classes/class_displayserver.html
- `OS` 与进程执行：  
  https://docs.godotengine.org/en/stable/classes/class_os.html
- `HTTPRequest`：  
  https://docs.godotengine.org/en/stable/classes/class_httprequest.html

## Windows

- Windows Graphics Capture：  
  https://learn.microsoft.com/windows/apps/develop/media-authoring-processing/screen-capture
- `Windows.Graphics.Capture` 命名空间：  
  https://learn.microsoft.com/uwp/api/windows.graphics.capture

## OCR

- Tesseract 官方仓库：  
  https://github.com/tesseract-ocr/tesseract
- Tesseract 文档：  
  https://github.com/tesseract-ocr/tessdoc
- Tesseract 语言数据：  
  https://github.com/tesseract-ocr/tessdata

## SQLite

- Microsoft.Data.Sqlite：  
  https://learn.microsoft.com/dotnet/standard/data/sqlite/

---

# 39. 最终实施顺序摘要

Codex 按以下顺序执行：

```text
1. 工程骨架
2. SQLite 与手工词条
3. CaptureBridge 与收件箱
4. 框选和 OCR
5. 从原句建立单词/短语
6. TTS
7. 复习系统
8. 导出与备份
9. 稳定性、打包和文档
```

最重要的第一条可用链路是：

```text
手工导入图片
→ 框选
→ OCR
→ 校正
→ 选择短语
→ 保存原句
→ 在词条库查看
```

最重要的第二条可用链路是：

```text
全局快捷键
→ 游戏截图进入收件箱
→ 批量整理
```

最重要的第三条可用链路是：

```text
到期词条
→ 原句填空
→ 查看截图
→ 评分
→ 更新下次复习
```

---

# 40. 完成定义

一个任务只有同时满足以下条件才算完成：

1. 代码已实现。
2. 编译无错误。
3. 相关自动化测试通过。
4. 人工验收步骤通过。
5. 错误状态有处理。
6. 没有把平台代码泄漏到 Domain。
7. 没有把 SQL 写进 UI。
8. 没有提交敏感配置。
9. 文档已更新。
10. 已知限制已记录。

