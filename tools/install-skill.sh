#!/bin/bash
# 저장소의 skills/apc-field-ui를 Claude Code 개인 스킬 폴더에 설치한다(덮어씀).
#   bash tools/install-skill.sh [대상 skills 폴더]   (기본: ~/.claude/skills)
# .md 안의 저장소 경로(C:\apc-field-ui, /c/apc-field-ui)는 이 저장소의 실제 위치로 바꿔 넣는다.
REPO="$(cd "$(dirname "$0")/.." && pwd)"
DEST="${1:-$HOME/.claude/skills}/apc-field-ui"
SRC_DIR="$REPO/skills/apc-field-ui"
[ -f "$SRC_DIR/SKILL.md" ] || { echo "오류: $SRC_DIR/SKILL.md 없음" >&2; exit 1; }
WIN_REPO="$(cygpath -w "$REPO")"

# 예전 설치본의 지워진 참고 파일이 남지 않게 references/는 새로 만든다.
rm -rf "$DEST/references"
mkdir -p "$DEST/references" || { echo "오류: $DEST 를 만들 수 없습니다" >&2; exit 1; }

DEST="$(cd "$DEST" && pwd)"   # 상대 경로 인자도 아래 cd 뒤에 맞게
cd "$SRC_DIR" || exit 1
for rel in SKILL.md references/*.md; do
    [ -f "$SRC_DIR/$rel" ] || continue
    text="$(cat "$SRC_DIR/$rel")" || exit 1
    # 따옴표 친 패턴 = 글자 그대로 치환(백슬래시 이스케이프 불필요)
    text="${text//"/c/apc-field-ui"/"$REPO"}"
    text="${text//"C:\\apc-field-ui"/"$WIN_REPO"}"
    printf '%s\n' "$text" > "$DEST/$rel" || { echo "오류: $rel 설치 실패" >&2; exit 1; }
    echo "설치: $DEST/$rel"
done
