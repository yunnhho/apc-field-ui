# APC.UI — Agent Guide

UI reference repository for APCSOFT field-PC programs (WinForms). Usage and folder layout: `docs/README.md`. Design baseline: `docs/superpowers/specs/2026-11-17-apcsoft-field-ui-design.md`.

## Constraints
- .NET Framework 4.0 / C# 7.3. No newer syntax (`record`, `init`, nullable reference types, etc.) and no NuGet packages.
- Done means `./build.sh` prints `ALL PASSED`. Never finish in a failing state.

## Do not touch
- `Resources/`, `references/` — source material. Read only.
- `themes/*/assets/*.png` — generated. Edit the SVGs in `assets-src/`, then regenerate with `node tools/render-assets.mjs` (first time: `cd tools && npm install`).
- `theme.json` status colors (run/warn/alarm/stop/info) may change lightness only. Changing the hue makes `ThemeLoader` reject the theme.
- Customer customization lives only in `themes/<customer>/`. `themes/default` is the shared baseline for all customers.

## Adding screens and controls
- Before building something new, check the existing controls in `src/APC.UI/Controls/` and the templates in `src/APC.UI.Demo/Templates/`, and reuse them.
- New controls go in `APC.UI`; add an example to the gallery (`src/APC.UI.Demo/GalleryForm.cs`), and if they contain logic, add a check in `src/APC.UI.Checks/`.

## Skill
- `skills/apc-field-ui/SKILL.md` quotes the arguments of `tools/new-project.sh`, `APC.UI.Tools` (api/render/attach) and `render-assets.mjs`, and the demo template names, verbatim. When you change any of these, update SKILL.md and reinstall with `bash tools/install-skill.sh`.
- Never edit the installed copy (`~/.claude/skills/apc-field-ui`). The repository is the source.

## Documentation language
- Documents read by agents (this file, `skills/`, `docs/superpowers/`, XML doc comments → `docs/api.md`) are written in English.
- Documents for the user stay in Korean: `docs/README.md`, decision/to-do logs (`docs/MMDD.md`), demo reports (`skill-test/**/REPORT.md`, `report.html`). Final reports to the user are in Korean.
- Korean UI strings (labels, dialog text, sample values) stay Korean inside English text.

## Git
- Never commit directly to `main`. Create a branch per task.
- Commit messages: `<type>: <description>` (feat, fix, refactor, docs, chore …).
- When the public API of `APC.UI` changes, bump the version in `src/APC.UI/Properties/AssemblyInfo.cs` (to tell apart dlls copied into customer projects).
