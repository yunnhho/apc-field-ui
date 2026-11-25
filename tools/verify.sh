#!/bin/bash
# 현장 프로그램 완료 확인: MSBuild 빌드(UTF-8 로그) → 형식마다 render PNG + 배치 점검.
#
#   bash tools/verify.sh <sln> <앱.exe> <고객사> <PNG 폴더> <형식이름...> [--compare <캡처.png>]
#     예: bash tools/verify.sh App.sln bin/Debug/App.exe default out MainForm RecipeDialog
#   --compare는 첫 형식(보통 MainForm)에만 쓴다 → <PNG 폴더>/<형식>.compare.png
#
# 빌드 로그는 sln 폴더의 build.log(콘솔 대신 파일: 한글 VS 콘솔은 CP949·UTF-8이 섞여 깨진다).
# 64비트 도구가 "어셈블리를 읽을 수 없습니다"로 실패하면(x86 앱·32비트 네이티브 DLL) 32비트 도구로 다시 그린다.
# 종료 코드: 0 = 빌드 경고·오류 0 + 모든 형식 점검 0건, 1 = 고칠 것이 있음, 2 = 사용법·환경 오류.
. "$(dirname "$0")/env.sh" || exit 2
REPO="$(cd "$(dirname "$0")/.." && pwd)"
TOOL64="$REPO/src/APC.UI.Tools/bin/Debug/APC.UI.Tools.exe"
TOOL86="$REPO/src/APC.UI.Tools/bin/Debug-x86/APC.UI.Tools.exe"

usage() { echo "사용법: bash tools/verify.sh <sln> <앱.exe> <고객사> <PNG 폴더> <형식이름...> [--compare <캡처.png>]" >&2; exit 2; }
[ $# -ge 5 ] || usage
SLN="$1"; EXE="$2"; CUSTOMER="$3"; OUT="$4"; shift 4
TYPES=(); COMPARE=""
while [ $# -gt 0 ]; do
    case "$1" in
        --compare) [ $# -ge 2 ] || usage; COMPARE="$2"; shift 2 ;;
        *) TYPES+=("$1"); shift ;;
    esac
done
[ ${#TYPES[@]} -gt 0 ] || usage
[ -f "$SLN" ] || { echo "오류: sln이 없습니다: $SLN" >&2; exit 2; }
[ -x "$TOOL64" ] || { echo "오류: 도구가 빌드되지 않았습니다. bash $REPO/build.sh 를 먼저 실행하세요." >&2; exit 2; }

# 1. 빌드 — logfile은 sln 폴더 안 상대 경로(상위 경로는 드라이브 루트로 풀려 정리가 막힌다)
SLN_DIR="$(cd "$(dirname "$SLN")" && pwd)"
(cd "$SLN_DIR" && "$MSBUILD" "$(basename "$SLN")" -v:m -nologo -noconlog "-flp:logfile=build.log;verbosity=minimal;encoding=utf-8;summary" > /dev/null)
BUILD_OK=$?
echo "== 빌드 ($SLN_DIR/build.log)"
tail -8 "$SLN_DIR/build.log"
[ $BUILD_OK -eq 0 ] || { echo "빌드 실패 — 오류를 고치고 다시 실행"; exit 1; }
# 요약 줄 "경고 N개" / "N Warning(s)"
WARN=$(tail -8 "$SLN_DIR/build.log" | grep -oE '경고 [0-9]+개|[0-9]+ Warning\(s\)' | grep -oE '[0-9]+' | head -1)
RESULT=0
[ "${WARN:-0}" -eq 0 ] || { echo "빌드 경고 $WARN개 — 경고도 고친다"; RESULT=1; }

# 2. render — 형식 이름은 네임스페이스 없이 클래스 이름만
# 상대 경로 exe는 현재 폴더에 없으면 sln 폴더 기준으로 찾는다(어디서 실행해도 같은 인자가 통하게).
[ -f "$EXE" ] || [ "${EXE#/}" != "$EXE" ] || EXE="$SLN_DIR/$EXE"
[ -f "$EXE" ] ||{ echo "오류: 빌드 결과 exe가 없습니다: $EXE" >&2; exit 2; }
THEME="$(dirname "$EXE")/themes/$CUSTOMER"
[ -d "$THEME" ] || { echo "오류: 출력 폴더에 테마가 없습니다: $THEME" >&2; exit 2; }
mkdir -p "$OUT"
render() {   # $1 도구, $2 형식, 나머지 = 추가 인자
    local tool="$1" type="$2"; shift 2
    "$tool" render "$(cygpath -w "$EXE")" "$type" "$(cygpath -w "$THEME")" "$(cygpath -w "$OUT/$type.png")" "$@" 2>&1
}
FIRST=1
for type in "${TYPES[@]}"; do
    extra=()
    [ $FIRST -eq 1 ] && [ -n "$COMPARE" ] && extra=(--compare "$(cygpath -w "$COMPARE")")
    FIRST=0
    echo "== render $type"
    text="$(render "$TOOL64" "$type" "${extra[@]}")"
    if echo "$text" | grep -q "어셈블리를 읽을 수 없습니다" && [ -x "$TOOL86" ]; then
        echo "(64비트 도구로 못 읽음 → 32비트 도구로 다시)"
        text="$(render "$TOOL86" "$type" "${extra[@]}")"
    fi
    echo "$text"
    echo "$text" | grep -q "^점검 0건" || RESULT=1
done
[ $RESULT -eq 0 ] && echo "== 완료: 빌드 경고 0 · 오류 0, 모든 형식 점검 0건" || echo "== 고칠 것이 남음(위 점검:·경고 줄)"
exit $RESULT
