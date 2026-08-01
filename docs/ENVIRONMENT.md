# Environment

Last reviewed: 2026-08-01 (`M0-T02`, version, .NET identity, architecture, C# generation, build, headless, and GUI verified)

## Repository

- Root: `D:\UGit\EnglishLearningProject`
- Godot project: `D:\UGit\EnglishLearningProject\english-learning-project`
- Root solution: `D:\UGit\EnglishLearningProject\GameLexicon.sln`

## Godot 4.7.1 .NET paths

- Installation directory: `E:\SteamLibrary\steamapps\common\Godot Engine`
- .NET main executable: `E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64.exe`
- Console/headless executable: `E:\SteamLibrary\steamapps\common\Godot Engine\Godot_v4.7.1-stable_mono_win64_console.exe`
- Steam compatibility executable: `E:\SteamLibrary\steamapps\common\Godot Engine\godot.windows.opt.tools.64.exe`
- GodotSharp: `E:\SteamLibrary\steamapps\common\Godot Engine\GodotSharp`

Verified during M0-T02:

- All three executables report `4.7.1.stable.mono.official.a13da4feb`.
- File and product metadata identify the Mono/.NET distribution.
- Godot executables and the .NET host are x64.
- `GodotSharp/Api` and `GodotSharp/Tools` exist and are nonempty.
- The console executable supports `--editor`, `--path`, `--headless`, `--quit`, and `--build-solutions`.
- Headless editor build and main-scene loading succeeded.
- GUI startup and the M0-T02 placeholder layout were verified manually by the user.

## Toolchain

- .NET SDKs: `8.0.423`, `10.0.301`
- Git: `2.55.0.windows.3`
- MVP host target: Windows 10/11 x64

## Godot C# project

- Project: `D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.csproj`
- Local solution: `D:\UGit\EnglishLearningProject\english-learning-project\EnglishLearningProject.sln`
- Project SDK: `Godot.NET.Sdk/4.7.1`
- Desktop target framework: `net8.0`
- Android conditional target framework: `net9.0`
- Domain, Application, and Infrastructure target framework: `net8.0`
- Test projects and CaptureBridge remain `net10.0`; .NET 10 tests successfully reference and test the net8.0 production libraries.
- Generated `.godot/mono/` content is ignored by the Godot project `.gitignore`.

## Maintenance

These are machine-local paths. Agents and Skills must read this file instead of relying on chat history or duplicating absolute paths. When a path/version changes: verify it read-only, update this file, review the affected workflow Skill and active milestone instruction, and record a Skill change only if reusable workflow changed.

Never record passwords, tokens, private proxy credentials, API keys, or other secrets here.
