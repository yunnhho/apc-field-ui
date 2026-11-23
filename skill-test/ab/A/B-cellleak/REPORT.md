# A/B 시연 — A(현재 문서) · B-cellleak (CellLeakTester)

> Sonnet 서브에이전트 보고를 메인 세션이 저장. 사용량: 토큰 105,786 · 도구 14회 · 116초.

## 지표

| 화면 | 처음 render 점검 건수 | render 횟수 | 마지막 점검 건수 |
|---|---|---|---|
| MainForm | 0 (참고 1줄: 표 아래가 빔) | 2 | 0 |
| LotInputDialog | 0 | 1 | 0 |
| AdminPasswordDialog | 0 | 1 | 0 |

- 빌드 경고 0개 / 오류 0개.
- 읽은 템플릿: JudgeView, RecipeDialog, AdminPasswordDialog. 문서: SKILL.md, docs/api.md.

## 처음 render 점검 원문

- MainForm: `참고: MainForm > MainView > Panel > ApcGrid: 표 아래가 빔(행 14, 17행까지 들어감) — 이력·목록이면 샘플 행을 17개로, 설비 수처럼 정해진 목록이면 그대로` / `점검 0건`
- LotInputDialog: `점검 0건`
- AdminPasswordDialog: `점검 0건`

## 진행 요약

- 점검 메시지로 고친 것: 없음. `참고:`에 따라 이력 14 → 17행.
- 눈으로 보고 고친 것: 없음(AdminPasswordDialog PNG는 열어 보지 않음).
- 막힌 곳: 없음. heredoc 안 씀, exe 실행 안 함.
- 문서가 모호했던 곳
  - 설정 화면 없이 관리자 비밀번호 dialog만 요청된 경우 여는 위치가 없다 → 메인에 "기준값 변경" 버튼.
  - RecipeDialog 템플릿 주석이 아직 "Alert"(SKILL.md는 Warn/Alert 구분) → SKILL.md를 따름.
  - 수율 소수 자리 규정 없음 → `0.0`.
  - 스캔 요청 없음 → 바코드는 `ApcTextBox { ReadOnly = true }`.

## 결과 파일

- 처음: `out/first/MainForm.png`, `out/first/LotInputDialog.png`, `out/first/AdminPasswordDialog.png`
- 마지막: `out/MainForm.png`, `out/LotInputDialog.png`, `out/AdminPasswordDialog.png`

## 정한 기준값

- 누설량 상한 5.0 Pa/s(같으면 OK, 초과부터 NG) — `LeakJudge.Limit` 한 곳.
- 소수: 누설량·수율 `0.0`, 수량 `#,0`, 시각 `HH:mm:ss`.
- 샘플 관리자 비밀번호 "1234", 모델 목록 NCM-21700/NCM-18650/LFP-32140.
- 판정 문구 "NG · 상한 초과 (상한 5.0)". 헤더: "가동중"/"NG 발생".
