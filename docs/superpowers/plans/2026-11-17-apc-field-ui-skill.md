# APC Field UI Skill Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** build repository tools (skeleton, API summary, render, csproj attach) and a personal skill `apc-field-ui` so that, in a new .NET Framework 4.x WinForms project, an agent builds a "main screen + dialogs" shell to the APC.UI baseline.

**Architecture:** repetitive work is done by deterministic repository tools: a new .NET 4.0 console `APC.UI.Tools` (commands `api` / `render` / `attach`) and `tools/new-project.sh` + `tools/project-template/`. The skill is a short procedure; for the API it only points to `docs/api.md`, regenerated on every build. `build.sh` checks skeleton creation → build → render, so if a tool breaks, `ALL PASSED` is not printed.

**Tech Stack:** C# 7.3, .NET Framework 4.0, WinForms, old-style csproj, VS2019 MSBuild, Git Bash (sh), Node + `@resvg/resvg-js` (existing).

**Spec:** `docs/superpowers/specs/2026-11-17-apc-field-ui-skill-design.md`

## Global Constraints

- Repository code: `<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>`, `<LangVersion>7.3</LangVersion>`, old-style csproj, no NuGet additions.
- Done when: `./build.sh` → last line `ALL PASSED (n)`. Never finish in a failing state.
- Add checks to the existing assert runner (`src/APC.UI.Checks`, `Check(bool, name)`). No test framework.
- `Resources/` and `references/` are read-only. `themes/*/assets/*.png` are regenerated only with `render-assets.mjs`. Never change status color hues.
- Generated new projects target .NET Framework 4.x WinForms only. For anything else the tools exit non-zero with a reason.
- New project code follows that project's `TargetFrameworkVersion`/`LangVersion`. Template defaults are `v4.0` / `7.3`.
- Screens are laid out in code only (no designer support). Composition: one main screen + dialogs.
- MSBuild path: `/c/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin/MSBuild.exe`. Scripts use `$MSBUILD` from `tools/env.sh`.
- Branch: `feat/apc-field-ui-skill`. Commit messages: `<type>: <description>`.

## Review Focus

1. **Invalid names**: a project name that is not a C# identifier (`1st-app`, spaces), or a customer name with spaces or Korean → `new-project.sh` rejects the project name and handles the customer folder correctly as a quoted path. → Task 6 checks.
2. **New-project mode run where a csproj already exists** → does not overwrite; stops and points to `--into-existing`. → Task 6 checks.
3. **`render` type name typo / constructor with parameters** → error listing similarly named candidate types, exit code 2. → Task 3 checks.
4. **Wrong theme path for `render`** → prints the `ThemeException` message (with file and item) as is, exit code 2. Never writes an empty PNG. → Task 3 checks.
5. **An existing project already references APC.UI at another path (HintPath)** → `attach` does not add a duplicate reference and rewrites the existing `HintPath` to `lib\APC.UI.dll`. → Task 4 checks.

---

### Task 1: Move `ApcTemplateView` into the dll + version rule

Rename `TemplateView` to `ApcTemplateView`: all existing public controls use the `Apc` prefix, and this avoids name clashes in customer projects (author's call).

**Files:**
- Create: `src/APC.UI/Controls/ApcTemplateView.cs`
- Delete: `src/APC.UI.Demo/Templates/TemplateView.cs`
- Modify: `src/APC.UI.Demo/Templates/*.cs` (the 7 templates: `: TemplateView` → `: ApcTemplateView`, `using APC.UI.Controls;`)
- Modify: `src/APC.UI/Properties/AssemblyInfo.cs` (`AssemblyVersion`/`AssemblyFileVersion` → `1.1.0.0`)
- Modify: `CLAUDE.md` (one line under the Git section: "When the public API changes, bump the version in `src/APC.UI/Properties/AssemblyInfo.cs`.")
- Test: `src/APC.UI.Checks/FormChecks.cs`

**Interfaces:**
- Produces: `namespace APC.UI.Controls { public class ApcTemplateView : UserControl }`
  - `protected ApcTemplateView(string title)` (existing constructor body as is)
  - `protected readonly ApcHeaderBar Header; protected readonly ApcAlarmBar Alarm; protected readonly Panel Body;`
  - `protected static int S(int px)`, `protected static Label Caption(string text)`, `protected static ApcButton Button(string text, ApcButtonVariant variant, int width = 110)`, `protected static FlowLayoutPanel Row(params Control[] controls)`

- [ ] **Step 1: Write failing checks** — add `private sealed class ProbeView : ApcTemplateView { public ProbeView() : base("probe") {} public Control H { get { return Header; } } public Control A { get { return Alarm; } } public Control B { get { return Body; } } }` to `FormChecks.cs`, then check:
  ```csharp
  using (var v = new ProbeView())
  {
      Check(v.H.Dock == DockStyle.Top && v.A.Dock == DockStyle.Bottom && v.B.Dock == DockStyle.Fill, "template view: header top, alarm bottom, body fill");
      Check(((ApcHeaderBar)v.H).Title == "probe", "template view: title goes to header");
  }
  Check(typeof(ApcTemplateView).IsPublic, "template view is public in APC.UI");
  ```
- [ ] **Step 2: Confirm failure** — `./build.sh` → BUILD FAILED because `ApcTemplateView` is undefined.
- [ ] **Step 3: Move** — move the file, `internal` → `public`, rename the class and constructor. Change the inheritance of the 7 demos, bump the version to `1.1.0.0`, add one line to `CLAUDE.md`.
- [ ] **Step 4: Confirm pass** — `./build.sh` → `ALL PASSED`. Run `src/APC.UI.Demo/bin/Debug/APC.UI.Demo.exe` and visually confirm the 7 menu screens look as before the move.
- [ ] **Step 5: Commit** — `git commit -m "refactor: move TemplateView into APC.UI as public ApcTemplateView"`

### Task 2: `APC.UI.Tools` + `api` command + `docs/api.md`

**Files:**
- Create: `src/APC.UI.Tools/APC.UI.Tools.csproj` (Exe, v4.0, old-style, `Compile Include="**\*.cs"`, references: System, System.Drawing, System.Windows.Forms, System.Xml, System.Xml.Linq. **No APC.UI project reference**: the target assembly is loaded at runtime)
- Create: `src/APC.UI.Tools/Program.cs` (command dispatch; wrong usage prints usage and exits 1)
- Create: `src/APC.UI.Tools/ApiWriter.cs`
- Modify: `APC.UI.sln` (add the project)
- Modify: `src/APC.UI/APC.UI.csproj` (in both configurations `<DocumentationFile>bin\$(Configuration)\APC.UI.xml</DocumentationFile>`, `<NoWarn>1591</NoWarn>`)
- Modify: `src/APC.UI.Checks/APC.UI.Checks.csproj` (project reference to `APC.UI.Tools`), `src/APC.UI.Checks/Program.cs` (`Run("tools", RunToolChecks);`)
- Create: `src/APC.UI.Checks/ToolChecks.cs`
- Modify: `build.sh` (before the self-checks run `./src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe api src/APC.UI/bin/Debug/APC.UI.dll docs/api.md`; exit on failure)
- Create: `docs/api.md` (generated, committed)

**Interfaces:**
- Produces: `public static class ApiWriter { public static string Write(string dllPath); }`. Returns Markdown.
  - The first line is `# APC.UI API (v<AssemblyVersion>)`.
  - Each public type gets `## <Name>` followed by its `<summary>`, read from the adjacent `<dll name>.xml` if present.
  - Declared public/protected members (constructors, properties, methods, events) one per line, signature in C# form plus the summary if any.
  - Types and members sorted by name. Same input must give the same output.
- Produces: command `APC.UI.Tools.exe api <dll> <out.md>`. Exit code 0 (success) or 2 (missing file or load failure, message printed).
- Assemblies load with `Assembly.LoadFrom`. Dependencies are resolved from the target dll's folder via `AppDomain.CurrentDomain.AssemblyResolve` (Task 3 uses the same → put it in `AssemblyLoader.Load(string path)` next to `Program.cs`).

- [ ] **Step 1: Write failing checks** — `ToolChecks.cs`:
  ```csharp
  var md = ApiWriter.Write(Path.Combine(Root, @"src\APC.UI\bin\Debug\APC.UI.dll"));
  Check(md.StartsWith("# APC.UI API (v1.1.0.0)"), "api: header carries version");
  Check(md.Contains("## ApcTemplateView") && md.Contains("## ApcButton"), "api: lists public controls");
  Check(md.Contains("Load(string themeDir)"), "api: lists ApcTheme.Load signature");
  Check(md == ApiWriter.Write(Path.Combine(Root, @"src\APC.UI\bin\Debug\APC.UI.dll")), "api: deterministic");
  ```
- [ ] **Step 2: Confirm failure** — `./build.sh` → BUILD FAILED because `ApiWriter` is undefined.
- [ ] **Step 3: Implement** — per the Interfaces above; update sln and csproj; add the api generation step to `build.sh`.
- [ ] **Step 4: Confirm pass** — `./build.sh` → `ALL PASSED`. Confirm `docs/api.md` is generated and contains the 9 controls such as `## ApcGrid`, plus `ApcTemplateView` and `ApcTheme`.
- [ ] **Step 5: Commit** — `git commit -m "feat: APC.UI.Tools api command and generated docs/api.md"`

### Task 3: `render` command

**Files:**
- Create: `src/APC.UI.Tools/Renderer.cs`
- Modify: `src/APC.UI.Tools/Program.cs`
- Test: `src/APC.UI.Checks/ToolChecks.cs`

**Interfaces:**
- Consumes: `AssemblyLoader.Load(string path)` (Task 2)
- Produces: `public static class Renderer`
  - `public static Bitmap Render(string assemblyPath, string typeName, string themeDir, Size size)`
    - Loads the assembly.
    - Calls `APC.UI.Theming.ApcTheme.Load(themeDir)` **via reflection** on the `APC.UI` assembly the target references, so it shares the target's ApcTheme static state.
    - Finds the type (full name match, else simple name match) and creates it with a parameterless constructor (public or non-public).
    - A `Form`: set `Size`, `CreateControl()`, then `DrawToBitmap`. A `UserControl`: put it with `Dock=Fill` on a hidden `Form` of the same size, then draw.
  - `public static Bitmap SideBySide(Image left, Image right)`: scales to the taller height preserving aspect ratio and joins horizontally. Width is the sum of both widths.
  - `public sealed class RenderException : Exception`: thrown when the type is missing (lists up to 5 candidates containing the same word), when there is no parameterless constructor, and on theme load failure (carries the inner `ThemeException` message as is).
- Produces: command `APC.UI.Tools.exe render <assembly> <type> <themeDir> <out.png> [--size 1920x1080] [--compare <original.png>]`
  - With `--compare`, also writes `<out>.compare.png`.
  - Exit codes: 0 / 1 (usage) / 2 (`RenderException`, message printed, no PNG written).
- The tool exe has no dpiAware manifest, so rendering is deterministic at 96dpi.

- [ ] **Step 1: Write failing checks** — `demo` = `Path.Combine(Root, @"src\APC.UI.Demo\bin\Debug\APC.UI.Demo.exe")`, `theme` = `Path.Combine(Root, @"themes\default")`.
  ```csharp
  using (var bmp = Renderer.Render(demo, "ProductionView", theme, new Size(1920, 1080)))
  {
      Check(bmp.Width == 1920 && bmp.Height == 1080, "render: requested size");
      Check(bmp.GetPixel(10, 10) != bmp.GetPixel(960, 1070), "render: not a blank image");
  }
  Check(Throws<RenderException>(() => Renderer.Render(demo, "ProductonView", theme, new Size(800, 600)), "ProductionView"), "render: typo lists candidates");
  Check(Throws<RenderException>(() => Renderer.Render(demo, "ProductionView", Path.Combine(Root, "nope"), new Size(800, 600)), "nope"), "render: bad theme path reported");
  using (var a = new Bitmap(100, 50)) using (var b = new Bitmap(200, 100)) using (var s = Renderer.SideBySide(a, b))
      Check(s.Width == 400 && s.Height == 100, "render: side-by-side scales to taller height");
  ```
  (Without a Demo project reference in Checks, build order is not guaranteed. Add an `APC.UI.Demo` project reference to `APC.UI.Checks.csproj` with `<ReferenceOutputAssembly>false</ReferenceOutputAssembly>` just to fix the build order.)
  (`Renderer` changes the global theme via `ApcTheme.Load`, so keep `Run("tools", ...)` as the last check in `Main`.)
- [ ] **Step 2: Confirm failure** — `./build.sh` → BUILD FAILED because `Renderer` is undefined.
- [ ] **Step 3: Implement** — per Interfaces. Demo templates such as `ProductionView` are `internal`, so find the type and constructor with `BindingFlags.NonPublic`.
- [ ] **Step 4: Confirm pass** — `./build.sh` → `ALL PASSED`. Also run the command once: `APC.UI.Tools.exe render src/APC.UI.Demo/bin/Debug/APC.UI.Demo.exe ProductionView themes/default <scratch>/p.png --compare references/<any original>.png` → visually confirm the 2 PNGs.
- [ ] **Step 5: Commit** — `git commit -m "feat: APC.UI.Tools render command (screen PNG and comparison with original)"`

### Task 4: `attach` command (connect to an existing csproj)

**Files:**
- Create: `src/APC.UI.Tools/CsprojPatcher.cs`
- Modify: `src/APC.UI.Tools/Program.cs`
- Create: `src/APC.UI.Checks/fixtures/csproj/old-winforms.csproj`, `sdk.csproj`, `net48-console.csproj`, `old-with-hintpath.csproj`
- Test: `src/APC.UI.Checks/ToolChecks.cs`

**Interfaces:**
- Produces: `public static class CsprojPatcher`
  - `public static string Patch(string csprojXml, string customer)`: returns the modified XML. Does not mutate the input.
    - Root has an `Sdk` attribute → `InvalidOperationException("SDK 형식 csproj는 지원하지 않습니다 ...")`
    - `TargetFrameworkVersion` does not start with `v4.` → exception (message includes the actual value)
    - `OutputType` is not `WinExe` and there is no `System.Windows.Forms` reference → exception ("WinForms 프로젝트가 아닙니다")
    - If a `Reference Include` starting with `APC.UI` exists, rewrite its `HintPath` to `lib\APC.UI.dll`. Otherwise add `<Reference Include="APC.UI"><HintPath>lib\APC.UI.dll</HintPath></Reference>`.
    - Add `<None Include="themes\**\*"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></None>` if missing.
    - Applying twice gives the same result.
- Produces: command `APC.UI.Tools.exe attach <csproj> <customer>`. Edits the file in place. Exit codes: 0 / 2 (exception message printed, file unchanged).

- [ ] **Step 1: Write failing checks**
  ```csharp
  string old = File.ReadAllText(Fx(@"csproj\old-winforms.csproj"));
  string once = CsprojPatcher.Patch(old, "acme");
  Check(once.Contains(@"<HintPath>lib\APC.UI.dll</HintPath>") && once.Contains(@"themes\**\*"), "attach: adds reference and theme copy");
  Check(CsprojPatcher.Patch(once, "acme") == once, "attach: idempotent");
  Check(Throws<InvalidOperationException>(() => CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\sdk.csproj")), "acme"), "SDK"), "attach: rejects SDK-style");
  Check(Throws<InvalidOperationException>(() => CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\net48-console.csproj")), "acme"), "WinForms"), "attach: rejects non-WinForms");
  string hinted = CsprojPatcher.Patch(File.ReadAllText(Fx(@"csproj\old-with-hintpath.csproj")), "acme");
  Check(hinted.Split(new[] { "Include=\"APC.UI" }, StringSplitOptions.None).Length == 2 && hinted.Contains(@"lib\APC.UI.dll"), "attach: rewrites existing APC.UI HintPath without duplicating");
  ```
- [ ] **Step 2: Confirm failure** — `./build.sh` → BUILD FAILED.
- [ ] **Step 3: Implement** — with `System.Xml.Linq.XDocument`, keeping the msbuild namespace (`http://schemas.microsoft.com/developer/msbuild/2003`).
- [ ] **Step 4: Confirm pass** — `./build.sh` → `ALL PASSED`
- [ ] **Step 5: Commit** — `git commit -m "feat: APC.UI.Tools attach command (connect APC.UI to an existing csproj)"`

### Task 5: `render-assets.mjs` also renders new names from the override folder

**Files:**
- Modify: `tools/render-assets.mjs`
- Modify: `build.sh` (included in Task 6's smoke check; this Task only checks manually)

**Interfaces:**
- Produces: render targets are the union of SVG names in `src` and in `override`. On the same name, override wins (existing behavior). Names only in override are rendered too.

- [ ] **Step 1: Confirm failure** — create `<scratch>/ov/icon_smoke_normal.svg` (a copy of `assets-src/lamp_run.svg`) and run `node tools/render-assets.mjs --out <scratch>/out --override <scratch>/ov` → confirm `icon_smoke_normal@1x.png` is missing.
- [ ] **Step 2: Implement** — build the file list as `new Set([...src list, ...override list])`. Path = override if present there, else src.
- [ ] **Step 3: Confirm pass** — same command → `icon_smoke_normal@{1x,1.5x,2x}.png` appear. Re-rendering the default theme with `node tools/render-assets.mjs` (no arguments) must leave `git status themes/` clean.
- [ ] **Step 4: Commit** — `git commit -m "fix: render-assets also renders SVGs that exist only in the override folder"`

### Task 6: New project skeleton `new-project.sh` + `build.sh` smoke check

**Files:**
- Create: `tools/env.sh` (one line `MSBUILD=...`; make `build.sh` source it too)
- Create: `tools/project-template/` — placeholders `__NAME__`, `__CUSTOMER__`, `__FRAMEWORK__`.
  - `__NAME__.sln`, `__NAME__.csproj` (WinExe, `TargetFrameworkVersion=__FRAMEWORK__`, `LangVersion=7.3`, `Compile Include="**\*.cs" Exclude="bin\**;obj\**"`, `ApplicationManifest=app.manifest`, `lib\APC.UI.dll` reference, `themes\**\*` copied to output)
  - `Program.cs`: `ApcTheme.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"themes\__CUSTOMER__"))`; on `ThemeException` a `MessageBox` and exit code 1; `Application.Run(new MainForm())`
  - `MainForm.cs`: inherits `ApcForm`, `Text="__NAME__"`, puts `MainView` in with Fill.
  - `Views/MainView.cs`: inherits `ApcTemplateView`, constructors `public MainView() : this(new SampleMainViewSource())` and `public MainView(IMainViewSource source) : base("__NAME__")`, one `ApcGrid` in the body (filled from the source rows)
  - `Data/IMainViewSource.cs`: `public interface IMainViewSource { IList<string[]> GetRows(); }`
  - `Data/SampleMainViewSource.cs`: 3 sample rows
  - `app.manifest`: contents of `src/APC.UI.Demo/app.manifest`
- Create: `tools/new-project.sh`
- Modify: `build.sh`
- Modify: `docs/README.md` (one `tools/new-project.sh` line with a usage example at the start of "using it in a new customer project"; one line per tool command)

**Interfaces:**
- Consumes: `APC.UI.Tools.exe attach` (Task 4), `render` (Task 3)
- Produces: usage `tools/new-project.sh <target folder> <project name> <customer> [--framework v4.0] [--into-existing]`
  - Project name not matching `^[A-Za-z_][A-Za-z0-9_]*$` → error, exit code 1
  - New-project mode with a `*.csproj` in the target folder → "use `--into-existing`" message, exit code 1, no files written.
  - Common: run `$MSBUILD src/APC.UI/APC.UI.csproj -p:Configuration=Release` and copy to `lib/APC.UI.dll` (+`.xml`).
  - Theme: only when `themes/<customer>` is missing, copy the repository's `themes/<customer>` if it exists, otherwise `themes/default`. If present, leave it (the rerun path replaces only the dll).
  - New project: copy the template and substitute placeholders, including `__NAME__` in file names.
  - `--into-existing`: no template. Run `attach` on the target folder's csproj (exactly one required).
  - Quote every path (customer names with spaces or Korean).
- Produces: a smoke check step in `build.sh` before the self-checks. On failure print `SMOKE FAILED: <step>` and exit.
  1. `T=$(mktemp -d)` → `tools/new-project.sh "$T/app" SmokeApp "샘플 고객"` → `$MSBUILD "$T/app/SmokeApp.sln"` succeeds
  2. `APC.UI.Tools.exe render "$T/app/bin/Debug/SmokeApp.exe" MainView "$T/app/bin/Debug/themes/샘플 고객" "$T/main.png"` → file exists
  3. `tools/new-project.sh "$T/app" 1bad x` → exit code ≠ 0; `tools/new-project.sh "$T/app" SmokeApp x` (csproj exists) → exit code ≠ 0
  4. Task 5 check: put `icon_smoke_normal.svg` in an override folder and run `render-assets.mjs --out "$T/ov-out"` → `icon_smoke_normal@1x.png` exists
  5. `rm -rf "$T"`
  - Convert Git Bash paths (`/tmp/...`) with `cygpath -w` when passing them to MSBuild or .NET exes, inside `new-project.sh` too.

- [ ] **Step 1: Write the failing check** — add the smoke check above to `build.sh` first.
- [ ] **Step 2: Confirm failure** — `./build.sh` → `SMOKE FAILED: new-project` (no script yet)
- [ ] **Step 3: Implement** — create `env.sh`, the template and `new-project.sh`; update the README.
- [ ] **Step 4: Confirm pass** — `./build.sh` → `ALL PASSED`. Before deleting the generated `$T/app`, run `SmokeApp.exe` once and visually confirm the window opens (manual, once).
- [ ] **Step 5: Commit** — `git commit -m "feat: new-project.sh skeleton generation and build.sh smoke check"`

### Task 7: Personal skill `apc-field-ui`

Follow the `superpowers:writing-skills` procedure. The skill file lives outside the repository (`~/.claude/skills/`), so it is not committed. Record the trial results at the end of this plan in the repository's `docs/superpowers/plans/` instead.

**Files:**
- Create: `C:\Users\rladb\.claude\skills\apc-field-ui\SKILL.md`

**Interfaces:**
- Consumes: `tools/new-project.sh`, `APC.UI.Tools.exe render`, `docs/api.md`, `tools/render-assets.mjs`, demo template sources
- Produces: frontmatter `name: apc-field-ui`. The `description` triggers on "building APCSOFT field-PC WinForms screens (monitoring, process equipment programs) in a new project". The body follows the 7 steps of spec §5.2 and contains:
  - Repository path `C:\00P_PGDesignReference`
  - The 3 required and 3 optional inputs
  - Project detection rules (stop unless 4.x WinForms; follow the project's `LangVersion`)
  - Starting template per purpose (spec §5.2-4)
  - New art goes in the project's `assets-src/` and is passed to `render-assets.mjs` with `--override`
  - Data slot (extend `IMainViewSource`; dialogs receive values)
  - Done check (build → `render` PNGs of the main screen and each dialog → `--compare` if a capture exists)
  - The 3 anti-goals (drawing with standard controls, changing status colors, modifying `themes/default` or repository SVGs)
  - Do not write the API; just say to read `docs/api.md`. Body at most 150 lines.

- [ ] **Step 1: Baseline (no skill)** — in an empty scratch folder, ask a new agent for "equipment monitoring screen, main function: running status per line, no dialogs". Record: standard controls used or not, status color rule compliance, build or not, tokens used.
- [ ] **Step 2: Write SKILL.md** — per Produces above.
- [ ] **Step 3: Skill trial** — run the same request again in a new scratch folder. Expected: skeleton via `new-project.sh`, `EquipmentView` layout as reference, build succeeds, `main.png` generated, no direct use of standard `Button`/`DataGridView`.
- [ ] **Step 4: Two more trials**
  - Request without the required input "dialogs or not" → confirm it asks only for that.
  - Request in an SDK-style net8 project folder → confirm it stops and explains why.
- [ ] **Step 5: Record and clean up** — write the results versus baseline (compliance, tokens) in the "Trial results" section at the end of this plan and commit: `git commit -m "docs: apc-field-ui skill trial results"`. Fix gaps found in the trials in SKILL.md and rerun Step 3.

## Trial results (2026-11-17)

Same request: "equipment monitoring (per line), main function: equipment running status per line, no dialogs, .NET Framework 4.0".

| Condition | Result |
|---|---|
| No skill, folder **outside the repository** | Nothing in the global settings (`~/.claude/CLAUDE.md`, rules, skills) points to the repository or APC.UI (grep 0 hits) → the agent cannot know APC.UI exists. Baseline failure is certain, so it was not run. |
| No skill, **inside** the repository (subagent, for reference) | Read the repository `CLAUDE.md` and README, found `new-project.sh` and `EquipmentView`, and finished to the baseline. 72k tokens, 8 tool calls. → confirms the tools (option B) work on their own. |
| **Skill, outside the repository**, empty folder (`claude -p`) | Ran `new-project.sh` via the skill, used `EquipmentView` layout as reference, build succeeded, render PNG generated. Controls used: `ApcForm`/`ApcTemplateView`/`ApcButton`/`ApcLamp`/`ApcGrid`/`ApcLampColumn`; 0 standard controls or hard-coded colors. 16 turns, 81 s, $0.69. |
| Skill, required input (dialogs or not) missing | Asked only for the missing item and stopped (proposed proceeding with the `default` theme starting from `TempHumiView`). 3 turns, $0.29. |
| Skill, SDK-style net8 project | Stopped without changing files, gave the reason and options. 5 turns, $0.34. |

Gap found in the trials: `render` hit a generic GDI+ error when the output folder did not exist → fixed to create the output folder, check added to `smoke.sh` (`fix: render creates a missing output folder`).
| Skill (after the final review fixes), process equipment program + **detail dialog** | Main history list + an `ApcDialog` measurement detail dialog; the dialog got a sample constructor so both rendered. 0 standard controls; all colors via `ApcTheme.Status`/`ApcTheme.C`. 27 turns, 157 s, $1.17. |

Final review (Opus) fixes: reject customer names `.`/`..` (the whole repository was being copied), UTF-8 output from `APC.UI.Tools` (Korean error messages were garbled), SKILL additions for `--into-existing` follow-up work, dialog default constructors, and the x86 render limitation.
