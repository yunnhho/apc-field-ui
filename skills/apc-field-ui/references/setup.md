# Project detection and setup

Follow the one row matching the work folder.

| Work folder | Do |
|---|---|
| No csproj | `bash /c/apc-field-ui/tools/new-project.sh <folder> <ProjectName> <customer> [--framework v4.x]` (default v4.0, v4.0–v4.8.1; creates the folder if missing) |
| Old-style csproj, `TargetFrameworkVersion` v4.x, WinForms, no APC.UI | Same command with `--into-existing` (adds dll, theme, references only; rerun replaces only the dll). Do every `할 일:` line in its output yourself |
| Already references APC.UI (`lib/APC.UI.dll`; editing screens, adding dialogs) | `bash /c/apc-field-ui/tools/new-project.sh <folder> --check-version` (changes nothing). `같음` → proceed; `다름` → do what the output says |
| SDK-style (`<Project Sdk=`), net6/8, WPF, web | **Stop.** Tell the user APC.UI supports only .NET Framework 4.x WinForms |

- If the customer's theme is not in the repository, the script prints `알림: ... themes/default 를 복사했습니다` — relay it to the user.
- Follow **that project's** `LangVersion`/`TargetFrameworkVersion` (v4.x → C# 7.3: no `record`, `init`, `string?`, switch expressions, target-typed `new()`).
