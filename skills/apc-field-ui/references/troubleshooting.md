# Troubleshooting

Contents: heredoc · theme load · MSBuild missing · render failure · locked files

## `.cs` files partly or not created
- Cause: writing Korean source through a Bash heredoc breaks quote parsing.
- Fix: do not trust the leftovers; rewrite every `.cs` with the Write tool, one file each. Use Edit for partial changes.

## App dies right at startup (`--into-existing` project)
- Cause: `Main` does not load the theme. Render loads the theme itself, so it cannot catch this.
- Fix: add `ApcTheme.Load(...)` as the `할 일:` line from `new-project.sh --into-existing` says.

## `env.sh` cannot find MSBuild
- Fix: relay the message to the user (install Visual Studio or Build Tools, or set the `MSBUILD` environment variable).

## Render makes no PNG
- `verify.sh` already retries x86 apps ("어셈블리를 읽을 수 없습니다") with the 32-bit tool.
- If that also fails (native DLL or device missing on this PC), confirm the build only and tell the user why render was not possible.
- "형식을 찾을 수 없습니다": pass the class name only (`MainForm`, `RecipeDialog`); screens and dialogs need a parameterless constructor.

## Next build or cleanup fails with "file in use"
- Cause: a running exe locks the `bin` folder.
- Fix: verify with render. If you ran the app, close it before finishing.
