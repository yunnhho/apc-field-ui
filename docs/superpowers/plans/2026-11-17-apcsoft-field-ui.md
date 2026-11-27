# APCSOFT Field UI Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** a raised-style control library `APC.UI.dll` that WinForms field-PC programs use just by referencing it + replaceable theme/image sets + a demo gallery per function.

**Architecture:** pure logic (theme load/validation, image path resolution, scan detection) is split into WinForms-independent classes verified by console self-checks (`APC.UI.Checks`). Controls sit thinly on top; all art comes from `themes/<name>/assets/*.png` (rendered from SVG masters), colors and fonts from `theme.json`.

**Tech Stack:** C# 7.3, .NET Framework 4.0, WinForms, old-style (non-SDK) csproj, VS2019 MSBuild; asset rendering with Node 24 + `@resvg/resvg-js` (tools only).

**Spec:** `docs/superpowers/specs/2026-11-17-apcsoft-field-ui-design.md`

## Global Constraints

- `<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>`, `<LangVersion>7.3</LangVersion>`. No `async/await`, `System.Text.Json`, `Task.Run`, or per-monitor DPI APIs.
- Zero NuGet dependencies in the library. JSON = `System.Runtime.Serialization.Json.DataContractJsonSerializer`.
- Old-style csproj (this PC has no .NET SDK). Build command: `"/c/Program Files (x86)/Microsoft Visual Studio/2019/Community/MSBuild/Current/Bin/MSBuild.exe" APC.UI.sln -p:Configuration=Debug -v:m` (below: `$MSBUILD`).
- Image names: `{kind}_{name}_{state}@{scale}.png`, kind ∈ `btn|lamp|win|icon|logo`, state ∈ `normal|hover|pressed|disabled` (lamps and logo omit state: `lamp_run@1x.png`, `logo_apcsoft@1x.png`), scale ∈ `1x|1.5x|2x`.
- Fixed status color meaning: run=green, warn=yellow, alarm=red, stop=gray, info=blue. Themes may change lightness only.
- Minimum control height: 28px (at 96dpi, multiplied by the DPI scale).
- Scan defaults: key interval ≤ 50ms, minimum length 4, terminator Enter.
- Target is the work PC only (1920×1080, resizable window). No kiosk/touch features (size sets, full-screen lock, number pad).
- Brand red only in the logo image. No brand color token in code or theme.
- `Resources/` is read-only (reference originals).
- This folder is not a git repository → the "checkpoint" at the end of each Task means build + self-checks pass; commit only when the user asks.

## Review Focus

1. **Broken theme.json** (missing file, JSON syntax error, missing field, changed status hue) → `ThemeException` naming the file and field. A Korean `fontFamily` ("맑은 고딕") loads without mojibake. → Task 1 tests.
2. **Missing image at a given scale** → falls back to `@1x`; if `@1x` is missing too, `FileNotFoundException` with the expected path (never silently an empty button). → Task 3 tests.
3. **125% scale (96×1.25=120dpi)** → between 1x and 1.5x pick the larger (1.5x) to avoid blur. → Task 3 tests.
4. **Short fast human input / input with one slow key in the middle / empty Enter** → never mistaken for a scan. → Task 6 tests.
5. **Designer sets button height to 20px** → raised to 28px (×DPI scale). → Task 4 tests.

---

### Task 1: Solution skeleton + theme load/validation

**Files:**
- Create: `APC.UI.sln`
- Create: `src/APC.UI/APC.UI.csproj` (Class Library, net40, references: System, System.Drawing, System.Windows.Forms, System.Runtime.Serialization)
- Create: `src/APC.UI/Theming/ThemeData.cs`
- Create: `src/APC.UI/Theming/ThemeLoader.cs`
- Create: `src/APC.UI/Theming/ThemeException.cs`
- Create: `src/APC.UI/Theming/StatusKind.cs`
- Create: `src/APC.UI.Checks/APC.UI.Checks.csproj` (Console, net40, references: APC.UI, System.Drawing, System.Windows.Forms)
- Create: `src/APC.UI.Checks/Program.cs`
- Create: `themes/default/theme.json`
- Create: `src/APC.UI.Checks/fixtures/` (bad JSON fixtures, copied to build output)

**Interfaces:**
- Produces:
  - `enum StatusKind { Run, Warn, Alarm, Stop, Info }`
  - `[DataContract] class ThemeData` — members (JSON keys camelCase): `string Name; string FontFamily; int FontSize; ThemeColors Colors; StatusColors Status; int[] ButtonSlice; ScanSettings Scan {int MaxKeyIntervalMs; int MinLength}; string Logo;`
  - `ThemeColors`: `string[] Background, Header, GridHeader` (each length 2 = gradient start/end), `string Border, Text, TextMuted, TextInverse, GridAltRow, GridSelected`.
  - `StatusColors`: `string Run, Warn, Alarm, Stop, Info`.
  - `static class ThemeLoader { static ThemeData Load(string themeDir); static Color ParseColor(string hex); static Color GetStatusColor(ThemeData t, StatusKind k); }`
  - `class ThemeException : Exception` — message includes the file path and field name.
  - Checks runner: `static void Check(bool ok, string name)` — counts failures, prints `FAILED n` at the end and exits with code n. Later Tasks add `Run<Area>Checks()` methods to `Program.cs`.

- [ ] **Step 1: Write `themes/default/theme.json` (spec values as is)**

```json
{
  "name": "APCSOFT Default",
  "fontFamily": "맑은 고딕",
  "fontSize": 10,
  "colors": {
    "background": ["#E6E6E6", "#CFCFCF"],
    "header": ["#5A6A7E", "#2F3B4A"],
    "gridHeader": ["#FAFAFA", "#D9DDE2"],
    "border": "#888888", "text": "#1F2937", "textMuted": "#6B7280", "textInverse": "#FFFFFF",
    "gridAltRow": "#F3F4F6", "gridSelected": "#CFE0F5"
  },
  "status": { "run": "#16A34A", "warn": "#EAB308", "alarm": "#DC2626", "stop": "#9CA3AF", "info": "#2563EB" },
  "buttonSlice": [8, 8, 8, 8],
  "scan": { "maxKeyIntervalMs": 50, "minLength": 4 },
  "logo": "logo_apcsoft"
}
```
Save as UTF-8 (no BOM).

- [ ] **Step 2: Write failing checks (`RunThemeChecks`)**

```csharp
var t = ThemeLoader.Load(Path.Combine(Root, "themes/default"));
Check(t.FontFamily == "맑은 고딕", "theme: korean fontFamily round-trips");
Check(t.Scan.MaxKeyIntervalMs == 50 && t.Scan.MinLength == 4, "theme: scan defaults");
Check(ThemeLoader.GetStatusColor(t, StatusKind.Alarm) == ThemeLoader.ParseColor("#DC2626"), "theme: alarm color");
Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("missing")), "theme.json"), "theme: missing file names file");
Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("syntax")), "theme.json"), "theme: bad json");
Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("no-status")), "status"), "theme: missing field named");
Check(Throws<ThemeException>(() => ThemeLoader.Load(Fx("alarm-blue")), "status.alarm"), "theme: alarm hue changed rejected");
Check(!Throws<ThemeException>(() => ThemeLoader.Load(Fx("alarm-dark")), ""), "theme: darker alarm red accepted");
```
`Root` = repository root (search upward from the executable for `APC.UI.sln`), `Fx(n)` = fixtures folder, `Throws<T>(action, mustContain)` = true if T is thrown and its message contains the string. Fixtures: `missing/` (empty folder), `syntax/` (a single `{`), `no-status/` (status key deleted), `alarm-blue/` (alarm `#2563EB`), `alarm-dark/` (alarm `#991B1B`).

- [ ] **Step 3: Build → confirm failure**

Run: `$MSBUILD APC.UI.sln ... && src/APC.UI.Checks/bin/Debug/APC.UI.Checks.exe`
Expected: compile error (no ThemeLoader) or FAILED > 0.

- [ ] **Step 4: Implement `ThemeLoader.Load`**

Read with `File.ReadAllBytes` → `MemoryStream` → `DataContractJsonSerializer` (encoding auto-detection keeps UTF-8 Korean). Wrap `SerializationException`/`IOException` in `ThemeException(path + cause)`. Null checks on required fields put the JSON key path (`status`, `colors.header` …) in the message. Status color validation (ranges via `Color.GetHue()`/`GetSaturation()`):

| Key | Allowed |
|---|---|
| run | hue 90–160 |
| warn | hue 40–65 |
| alarm | hue ≥ 345 or ≤ 15 |
| info | hue 200–245 |
| stop | saturation ≤ 0.15 |

A gradient array length ≠ 2 or `buttonSlice` length ≠ 4 also throws `ThemeException`.

- [ ] **Step 5: Build + confirm checks pass**

Expected: output ends with `ALL PASSED`, exit 0.

- [ ] **Step 6: Checkpoint** — 0 build warnings (Nullable etc. irrelevant), checks pass.

---

### Task 2: Asset pipeline (SVG masters → PNG sets) + migrating existing images

**Files:**
- Create: `tools/package.json` (devDependency `@resvg/resvg-js`), `tools/render-assets.mjs`
- Create: `assets-src/*.svg` (list below)
- Create (generated): `themes/default/assets/*.png`
- Create: `themes/default/assets/logo_apcsoft@1x.png` (← copy of `Resources/APCSOFT.png`)

**Interfaces:**
- Produces: all files below in `themes/default/assets/`. Base (@1x) sizes:
  - `btn_{primary,normal,danger}_{normal,hover,pressed,disabled}` — 48×48, corners and gloss fit within the 8px 9-slice margins.
  - `win_{close,minimize}_{normal,hover,pressed,disabled}` — 46×32.
  - `lamp_{run,warn,alarm,stop,info}` — 24×24.
  - `logo_apcsoft@1x` — 155×50 (only a raster original exists, so 1x only; 1.5x/2x get added once the company's vector original is available → Task 3's fallback rule handles it).

- [ ] **Step 1: Write SVG masters** — style reference: `Resources/` originals and mockup B (the `.B` rules in `.superpowers/brainstorm/735-1794878105/content/visual-style.html`):
  - Lamps: like `1~4.png`, a radialGradient (highlight at 35%/30% → main color at 60% → dark rim) + 1px dark outline. Main color = theme.json status value. info is newly drawn.
  - Buttons: vertical linearGradient (normal `#FDFDFD→#D4D8DE→#BFC5CD`), 1px `#7A7A7A` border, radius 5, 1px white highlight on top. hover = lighter + border `#5A7FB0`, pressed = reversed gradient + inner shadow, disabled = 50% opacity gray. primary = blue-gray family (`#7FA3D1→#3E6CA8`, contrast with white text), danger = red family (`#F05252→#B91C1C`).
  - Window buttons: shaped like `btn_close.jpg`/`btn_minimize.jpg` (light blue-gray rounded square + dark ✕/—). hover **clearly distinct from before** (brighter, border `#5A7FB0`); close hover turns only the icon `#B91C1C`.
- [ ] **Step 2: Write `render-assets.mjs`** — render each `assets-src/*.svg` to `{basename}@1x/1.5x/2x.png` with `fitTo: {mode:'width', value: round(svgWidth*scale)}`, output to `themes/default/assets/`. `--out <dir>` writes another theme; `--override <dir>` replaces SVGs with the same basename from that folder (for swapping only some images per customer).
- [ ] **Step 3: Run**

Run: `cd tools && npm install && node render-assets.mjs`
Expected: `ls themes/default/assets/*.png | wc -l` → **76** (btn 36 + win 24 + lamp 15 + logo 1).
- [ ] **Step 4: Visual check** — open `lamp_*@2x.png`, `win_close_hover@2x.png`, `btn_primary_pressed@2x.png`; confirm the tone continues from the Resources originals and backgrounds are transparent.
- [ ] **Step 5: Checkpoint.**

---

### Task 3: Image resolution + theme facade

**Files:**
- Create: `src/APC.UI/Theming/AssetResolver.cs`
- Create: `src/APC.UI/Theming/ApcTheme.cs`
- Modify: `src/APC.UI.Checks/Program.cs` (`RunAssetChecks`)

**Interfaces:**
- Consumes: `ThemeLoader.Load`, `ThemeData` (Task 1)
- Produces:
  - `static class AssetResolver { static string PickScale(float dpi); static string Resolve(string assetsDir, string baseName, float dpi); }` — `baseName` e.g. `"btn_primary_hover"`, `"lamp_run"`. Returns the full path of a file that exists.
  - `static class ApcTheme { static ThemeData Current; static string ThemeDir; static float Dpi; static void Load(string themeDir); static Image GetImage(string baseName); static Font GetFont(FontStyle style = Regular); static Color Status(StatusKind k); static Color C(string hex); static int MinHeight { get; } static event EventHandler Changed; }` — `MinHeight` = `28 × Dpi/96`.

- [ ] **Step 1: Failing checks**

```csharp
Check(AssetResolver.PickScale(96f) == "1x", "scale 100%");
Check(AssetResolver.PickScale(120f) == "1.5x", "scale 125% rounds up");
Check(AssetResolver.PickScale(144f) == "1.5x", "scale 150%");
Check(AssetResolver.PickScale(168f) == "2x", "scale 175% rounds up");
Check(AssetResolver.PickScale(240f) == "2x", "scale 250% caps at 2x");
var dir = Path.Combine(Root, "themes/default/assets");
Check(AssetResolver.Resolve(dir, "lamp_run", 192f).EndsWith("lamp_run@2x.png"), "resolve 2x");
Check(AssetResolver.Resolve(dir, "logo_apcsoft", 192f).EndsWith("logo_apcsoft@1x.png"), "fallback to 1x");
Check(Throws<FileNotFoundException>(() => AssetResolver.Resolve(dir, "btn_nope_normal", 96f), "btn_nope_normal@1x.png"), "missing names expected path");
```
- [ ] **Step 2: Build and run → confirm FAIL.**
- [ ] **Step 3: Implement** — `PickScale`: ratio r=dpi/96; r<1.25→1x, r<1.75→1.5x, otherwise 2x (boundaries pick the larger). `ApcTheme.GetImage` caches per path in a `Dictionary<string, Image>` (read with `Image.FromStream` to avoid file locks); `Load` disposes and clears the cache, then raises `Changed`. `Dpi` is `Graphics.FromHwnd(IntPtr.Zero).DpiX` at `Load`.
- [ ] **Step 4: Confirm pass.**
- [ ] **Step 5: Checkpoint.**

---

### Task 4: 9-slice drawing + `ApcButton`

**Files:**
- Create: `src/APC.UI/Drawing/NineSlice.cs`
- Create: `src/APC.UI/Controls/ApcButton.cs`
- Modify: `src/APC.UI.Checks/Program.cs` (`RunButtonChecks`)

**Interfaces:**
- Consumes: `ApcTheme.GetImage`, `ApcTheme.MinHeight`, `ApcTheme.GetFont`, `ThemeData.ButtonSlice`
- Produces:
  - `static class NineSlice { static void Draw(Graphics g, Image img, Rectangle dest, Padding slice, float scale); }` — slice is in @1x px, multiplied by the image scale to source coordinates.
  - `enum ApcButtonVariant { Normal, Primary, Danger }`
  - `class ApcButton : Button { ApcButtonVariant Variant; }` — image name `btn_{variant}_{state}`. state: `!Enabled`→disabled, mouse down→pressed, mouse over→hover, otherwise normal. Text color: Primary/Danger = `textInverse`, Normal = `text`.

- [ ] **Step 1: Failing checks** (after theme load; instances only, nothing shown)

```csharp
var d = new ApcButton { Height = 20 };
Check(d.Height >= ApcTheme.MinHeight, "button clamps to 28*dpi");
var big = new ApcButton { Height = 80 };
Check(big.Height == 80, "larger height kept");
```
- [ ] **Step 2: Confirm FAIL.**
- [ ] **Step 3: Implement** — clamp the minimum height in a `SetBoundsCore` override (applies to designer and code). Font from `ApcTheme.GetFont()`. `SetStyle(UserPaint|AllPaintingInWmPaint|OptimizedDoubleBuffer|SupportsTransparentBackColor)`. Subscribe to `ApcTheme.Changed` to `Invalidate`; unsubscribe on Dispose. On focus draw a 1px `#5A7FB0` inner rectangle instead of the dotted line (for keyboard users).
- [ ] **Step 4: Confirm pass.**
- [ ] **Step 5: Checkpoint.**

---

### Task 5: Status display — `ApcLamp`, `ApcHeaderBar`, `ApcAlarmBar`

**Files:**
- Create: `src/APC.UI/Controls/ApcLamp.cs`, `ApcHeaderBar.cs`, `ApcAlarmBar.cs`
- Create: `src/APC.UI/Drawing/Gradients.cs`

**Interfaces:**
- Consumes: `ApcTheme.*`, `StatusKind`
- Produces:
  - `static class Gradients { static void Fill(Graphics g, Rectangle r, string[] pair); }` — vertical `LinearGradientBrush`.
  - `class ApcLamp : Control { StatusKind Status; bool Blink; }` — image `lamp_{status lowercase}`; when `Blink && Status==Alarm`, a 500ms `System.Windows.Forms.Timer` toggles visibility.
  - `class ApcHeaderBar : Panel { string Title; StatusKind Status; string StatusText; }` — background `colors.header`, text `textInverse`, lamp + status text on the right.
  - `class ApcAlarmBar : Panel { void Push(string message); void AcknowledgeCurrent(); event EventHandler Acknowledged; int Count; }` — alarm-family gradient background; multiple messages rotate every 3 seconds; a `확인` `ApcButton` (Normal) on the right removes the current message and raises `Acknowledged`. `Visible=false` when `Count==0`.

- [ ] **Step 1: Add checks (`RunAlarmChecks`)**

```csharp
var bar = new ApcAlarmBar();
Check(!bar.Visible, "alarm bar hidden when empty");
bar.Push("금형 온도 상한 초과"); bar.Push("원자재 부족");
Check(bar.Visible && bar.Count == 2, "alarm bar shows queued");
bar.AcknowledgeCurrent();
Check(bar.Count == 1, "ack removes one");
```
- [ ] **Step 2: Confirm FAIL → Step 3: implement → Step 4: confirm pass.**
- [ ] **Step 5: Checkpoint.**

---

### Task 6: Barcode — `ScanDetector` + `ApcScanBox`

**Files:**
- Create: `src/APC.UI/Input/ScanDetector.cs`
- Create: `src/APC.UI/Controls/ApcScanBox.cs`
- Modify: `src/APC.UI.Checks/Program.cs` (`RunScanChecks`)

**Interfaces:**
- Consumes: `ThemeData.Scan`
- Produces:
  - `class ScanDetector { ScanDetector(int maxKeyIntervalMs, int minLength); string Feed(char c, long timestampMs); void Reset(); }` — on Enter (`'\r'`) returns the string if the buffer qualifies, otherwise null. Other characters return null.
  - `class ApcScanBox : TextBox { event EventHandler<ScanEventArgs> Scanned; void Attach(Form host); void Flash(bool success); }`, `class ScanEventArgs : EventArgs { string Code; }`

- [ ] **Step 1: Failing checks**

```csharp
string Run(ScanDetector d, string s, params int[] gaps) { long t=0; string r=null; for(int i=0;i<s.Length;i++){ t+= i<gaps.Length?gaps[i]:10; r=d.Feed(s[i],t);} return r; }
var d = new ScanDetector(50, 4);
Check(Run(d, "LOT-0032\r") == "LOT-0032", "fast burst is scan");
d.Reset(); Check(Run(d, "abc\r") == null, "short fast input not scan");
d.Reset(); Check(Run(d, "LOT-0032\r", 10,10,10,200,10,10,10,10,10) == null, "one slow gap breaks scan");
d.Reset(); Check(Run(d, "\r") == null, "empty enter ignored");
d.Reset(); Run(d, "abc\r"); Check(Run(d, "LOT-0033\r") == "LOT-0033", "detector recovers after rejection");
```
- [ ] **Step 2: Confirm FAIL.**
- [ ] **Step 3: Implement** — a gap > max restarts the buffer from that character. Always clear the buffer after an Enter decision. `ApcScanBox.Attach(host)`: `host.KeyPreview = true`, `Feed` with `Environment.TickCount` in `host.KeyPress`; on a scan, `e.Handled = true` (so the barcode does not leak into another focused field), `Text = code`, raise `Scanned`, `Flash(true)`. `Flash`: background to a light run/alarm tone for 300ms plus `SystemSounds.Beep` (success) / `SystemSounds.Hand` (failure) — failure is called by the consumer when its validation fails. A 2px info-color border shows "waiting for scan".
- [ ] **Step 4: Confirm pass.**
- [ ] **Step 5: Checkpoint.**

---

### Task 7: `ApcForm` (borderless window) + confirmation dialog

**Files:**
- Create: `src/APC.UI/Controls/ApcForm.cs`
- Create: `src/APC.UI/Controls/ApcDialog.cs` — for the spec invariant "destructive actions require a confirmation step". `static bool ApcDialog.Confirm(IWin32Window owner, string message)`: modal based on `ApcForm`, `취소` (Normal, default focus, Esc) / `확인` (Danger). Default focus on cancel so Enter or a barcode Enter never confirms by mistake.

**Interfaces:**
- Consumes: `ApcTheme.GetImage("win_*")`, `ApcTheme.GetImage(Current.Logo)`, `Gradients.Fill`
- Produces: `class ApcForm : Form { string TitleText; protected Rectangle ContentBounds; }`
  - `FormBorderStyle.None`, 40px (×DPI) title bar (header gradient, logo and title on the left, `win_minimize`/`win_close` image buttons with 4 states on the right), move by dragging the title bar (`WM_NCLBUTTONDOWN`+`HTCAPTION`), double-click to maximize/restore, 6px edge resize (`WM_NCHITTEST`), 1px `border` frame. When maximized, `MaximizedBounds = Screen.FromControl(this).WorkingArea` so the taskbar stays visible.

- [ ] **Step 1: Minimal checks possible without the demo** (`RunFormChecks`) — `new ApcForm().FormBorderStyle == None`; after `WindowState=Maximized`, `Bounds` does not exceed `WorkingArea`.
- [ ] **Step 2: FAIL → implement → pass.**
- [ ] **Step 3: Checkpoint** (drag, resize and hover are checked visually in the Task 9 demo).

---

### Task 8: `ApcGrid` + `ApcLampColumn`

**Files:**
- Create: `src/APC.UI/Controls/ApcGrid.cs`, `ApcLampColumn.cs`

**Interfaces:**
- Produces:
  - `class ApcGrid : DataGridView` — on creation `EnableHeadersVisualStyles=false`, header `gridHeader` gradient (`CellPainting`), even rows `gridAltRow`, selection `gridSelected`, row height `ApcTheme.MinHeight`, `SelectionMode=FullRowSelect`, `RowHeadersVisible=false`, `AllowUserToAddRows=false`.
  - `class ApcLampColumn : DataGridViewColumn` + `ApcLampCell` — draws a `StatusKind` cell value as the centered `lamp_*` image. null/DBNull → empty cell (no exception).

- [ ] **Step 1: Checks** (`RunGridChecks`): after creating an `ApcGrid`, `RowTemplate.Height >= ApcTheme.MinHeight`; putting `null` in an `ApcLampColumn` cell and calling `GetFormattedValue` throws nothing.
- [ ] **Step 2: FAIL → implement → pass.**
- [ ] **Step 3: Checkpoint.**

---

### Task 9: Demo gallery + sample theme

**Files:**
- Create: `src/APC.UI.Demo/APC.UI.Demo.csproj` (WinExe, net40, `<dpiAware>true</dpiAware>` in app.manifest)
- Create: `src/APC.UI.Demo/Program.cs` — argument `--theme <name>` (default `default`) → `ApcTheme.Load(Root/themes/<name>)`; on `ThemeException` show the cause in a message box and exit.
- Create: `src/APC.UI.Demo/GalleryForm.cs` — `ApcForm`, start size 1600×900. Left list: `컨트롤 카탈로그` + 5 templates. The selected screen shows on the right.
- Create: `src/APC.UI.Demo/Catalog/ControlCatalog.cs` — every control × state (normal / hover hint / disabled), the 5 lamps (alarm blinking), a button that calls `ApcDialog.Confirm`.
- Create: `src/APC.UI.Demo/Templates/*.cs` — 5 `UserControl`s exactly per the spec §5.5 table, each with an `ApcHeaderBar` on top and an `ApcAlarmBar` at the bottom:
  - `WorkStartView`, `ProductionView` (ScanBox → add Grid row → F9 save, row delete via `ApcDialog.Confirm`), `DefectView`, `EquipmentView` (LampColumn), `HistoryView` (two period `DateTimePicker`s + grid + a disabled `엑셀 내보내기` button slot)
  - Fake data in static arrays inside each template.
- Create: `themes/sample/theme.json` (copy of default; header/background/primary family changed to green-gray, alarm changed in lightness only to `#B91C1C`) + `themes/sample/assets/` (run `node tools/render-assets.mjs --out ../themes/sample/assets`, then re-render with only the primary button SVG colors changed — override rule for `assets-src/sample/btn_primary_*.svg`: `--override assets-src/sample`).

- [ ] **Step 1: Build** — Expected: all 3 projects build, `APC.UI.Checks.exe` → `ALL PASSED`.
- [ ] **Step 2: Manual check (default)** — run `APC.UI.Demo.exe`: hover/pressed changes on every catalog button, window drag/resize/maximize, in `ProductionView` add a row with a real scanner (instead of fast-pasting `LOT-0032` from Notepad; without a scanner, the `ScanDetector` checks stand in), F9 save message.
- [ ] **Step 3: Manual check (theme swap)** — `APC.UI.Demo.exe --theme sample`: colors and button images change **without code changes**. Change alarm in `themes/sample/theme.json` to `#2563EB` and run → a message naming the offending field, then exit.
- [ ] **Step 4: Manual check (DPI)** — sign out and run at Windows scale 125% / 150% / 200%; buttons, lamps and window buttons are not blurry or clipped (the logo only exists at 1x, so upscale blur is acceptable — resolved once a vector original is available).
- [ ] **Step 5: Checkpoint + write `docs/README.md`**: 5 lines on using it in a new project (reference `APC.UI.dll` → copy the `themes/<customer>` folder → `ApcTheme.Load` → inherit forms from `ApcForm` → image re-render command).
