#!/bin/sh
# 연기 점검: new-project.sh로 뼈대 생성 → 빌드 → render, 잘못된 입력 거부, --into-existing 연결, override 새 이름 렌더.
# build.sh가 부른다. 실패하면 "SMOKE FAILED: <단계>"를 출력하고 1로 끝난다.
. "$(dirname "$0")/env.sh"
cd "$(dirname "$0")/.." || exit 1
TOOLS=./src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe
T="$(mktemp -d)"
fail() { echo "SMOKE FAILED: $1"; rm -rf "$T"; exit 1; }
quiet() { "$@" > "$T/log" 2>&1 || { tail -20 "$T/log"; return 1; }; }

quiet tools/new-project.sh "$T/app" SmokeApp "샘플 고객" || fail new-project
quiet "$MSBUILD" "$(cygpath -w "$T/app/SmokeApp.sln")" -v:m -nologo -p:PreferredUILang=en-US || fail build-new-project
quiet "$TOOLS" render "$(cygpath -w "$T/app/bin/Debug/SmokeApp.exe")" MainView "$(cygpath -w "$T/app/bin/Debug/themes/샘플 고객")" "$(cygpath -w "$T/main.png")" || fail render
[ -s "$T/main.png" ] || fail render-output
quiet bash tools/verify.sh "$T/app/SmokeApp.sln" "$T/app/bin/Debug/SmokeApp.exe" "샘플 고객" "$T/verify" MainView MainForm || fail verify
[ -s "$T/verify/MainForm.png" ] || fail verify-output
tools/new-project.sh "$T/app" --check-version | grep -q "^같음" || fail check-version
quiet "$TOOLS" render "$(cygpath -w "$T/app/bin/Debug/SmokeApp.exe")" MainView "$(cygpath -w "$T/app/bin/Debug/themes/샘플 고객")" "$(cygpath -w "$T/new-dir/main.png")" || fail render-creates-output-dir

quiet "$MSBUILD" "$(cygpath -w "$T/app/SmokeApp.sln")" -v:m -nologo -p:PreferredUILang=en-US -p:PlatformTarget=x86 "-p:OutputPath=bin\x86\\" "-p:IntermediateOutputPath=obj\x86\\" || fail build-x86
quiet ./src/APC.UI.Tools/bin/Debug-x86/APC.UI.Tools.exe render "$(cygpath -w "$T/app/bin/x86/SmokeApp.exe")" MainView "$(cygpath -w "$T/app/bin/x86/themes/샘플 고객")" "$(cygpath -w "$T/x86.png")" || fail render-x86
[ -s "$T/x86.png" ] || fail render-x86-output

tools/new-project.sh "$T/bad" 1bad x > /dev/null 2>&1 && fail reject-bad-name
tools/new-project.sh "$T/app" SmokeApp x > /dev/null 2>&1 && fail reject-existing-csproj
tools/new-project.sh "$T/dots" Dots ".." > /dev/null 2>&1 && fail reject-dot-customer
tools/new-project.sh "$T/kw" class x > /dev/null 2>&1 && fail reject-keyword-name
tools/new-project.sh "$T/fw" FwApp x --framework v4.9 > /dev/null 2>&1 && fail reject-unknown-framework
mkdir -p "$T/broken/Program.cs"
tools/new-project.sh "$T/broken" Broken x > /dev/null 2>&1 && fail template-copy-failure-reported
mkdir -p "$T/sdk"
cp src/APC.UI.Checks/fixtures/csproj/sdk.csproj "$T/sdk/Field.csproj"
tools/new-project.sh "$T/sdk" Field x --into-existing > /dev/null 2>&1 && fail reject-sdk-into-existing
[ -e "$T/sdk/lib" ] || [ -e "$T/sdk/themes" ] && fail rejected-into-existing-left-files
[ -e "$T/dots/.git" ] && fail dot-customer-copied-repo
"$TOOLS" render "$(cygpath -w "$T/app/bin/Debug/SmokeApp.exe")" MainVeiw "$(cygpath -w "$T/app/bin/Debug/themes/샘플 고객")" "$(cygpath -w "$T/x.png")" 2>&1 | grep -q "형식을 찾을 수 없습니다" || fail tools-utf8-message

mkdir -p "$T/old"
sed 's/v4\.8/v4.0/' src/APC.UI.Checks/fixtures/csproj/old-winforms.csproj > "$T/old/Field.csproj"
printf 'static class P { [System.STAThread] static void Main() { new APC.UI.Controls.ApcButton(); } }\n' > "$T/old/Program.cs"
quiet tools/new-project.sh "$T/old" Field "샘플 고객" --into-existing || fail into-existing
grep -q "할 일: Main에서" "$T/log" || fail into-existing-theme-load-hint
quiet "$MSBUILD" "$(cygpath -w "$T/old/Field.csproj")" -v:m -nologo -p:PreferredUILang=en-US || fail build-into-existing
[ -f "$T/old/bin/Debug/themes/샘플 고객/theme.json" ] || fail into-existing-theme-copy

mkdir -p "$T/ov"
cp assets-src/lamp_run.svg "$T/ov/icon_smoke_normal.svg"
quiet node tools/render-assets.mjs --out "$T/ov-out" --override "$T/ov" || fail render-assets
[ -f "$T/ov-out/icon_smoke_normal@1x.png" ] || fail render-assets-new-name

quiet bash tools/install-skill.sh "$T/skills" || fail install-skill
grep -q "^name: apc-field-ui" "$T/skills/apc-field-ui/SKILL.md" || fail install-skill-content
grep -q "AddCaptionRow" "$T/skills/apc-field-ui/references/dialog.md" || fail install-skill-references

rm -rf "$T"
echo "SMOKE OK"
