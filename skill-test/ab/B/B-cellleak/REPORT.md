# A/B 시연 — B(줄인 문서) · B-cellleak (CellLeakTester)

> Sonnet 서브에이전트 보고를 메인 세션이 저장. 사용량: 토큰 113,159 · 도구 32회 · 166초.

## 지표

| 화면 | 처음 render 점검 | render 횟수 | 마지막 점검 |
|---|---|---|---|
| MainForm | 1 | 4 | 0 |
| LotEntryDialog | 0 | 2 | 0 |
| AdminPasswordDialog | 0 | 2 | 0 |

- 빌드 경고 0, 오류 0. 읽은 템플릿: `JudgeView`, `RecipeDialog`, `AdminPasswordDialog`. `docs/api.md`.

## 처음 render 점검 원문

- MainForm
  - `점검: MainForm > MainView > Panel: Label '최근 검사 이력'(0,0 116x27)와(과) FlowLayoutPanel(16,16 1886x47) 겹침`
  - `참고: MainForm > MainView > Panel > ApcGrid: 표 아래가 빔(행 12, 18행까지 들어감) — 이력·목록이면 샘플 행을 18개로, 설비 수처럼 정해진 목록이면 그대로`
  - `점검 1건`
- LotEntryDialog / AdminPasswordDialog: `점검 0건`

## 진행 요약

- 점검 메시지로 고친 것
  - `최근 검사 이력` 제목 Label에 Dock이 없어 겹침 → `Dock = Top`. (**B 문서에서 뺀 규칙** "SectionTitle을 Body에 넣을 때 Dock=Top"에 해당)
  - `참고`대로 12 → 18행 → "행 18 중 17행 보임" 스크롤바 점검 → 17행. `참고`의 수가 1 많았다(render 버그).
- 눈으로 보고 고친 것
  - 판정 열(`ApcLampColumn` + 문구)이 좁아 "OK"가 "O…"로 잘림. render는 점검 0건으로 통과 → 열 폭 `S(110)`. (render가 램프 열 글자 잘림을 못 잡음)
- 막힌 곳: 없음.
- 문서가 모호했던 곳
  - 램프 열에 문구를 넣을 때 열 폭 기준 없음.
  - 설정 화면 없이 관리자 비밀번호 dialog를 어디서 여는지 없음 → "관리자 확인" 버튼.
  - 샘플 비밀번호 확인 방식 없음 → `VerifyAdmin`을 인터페이스에.
  - 현재 바코드를 스캔칸으로 할지 읽기 전용으로 할지 → 읽기 전용.

## 결과 파일

- 처음: `out/first/MainForm.png`, `out/first/LotEntryDialog.png`, `out/first/AdminPasswordDialog.png`
- 마지막: `out/MainForm.png`, `out/LotEntryDialog.png`, `out/AdminPasswordDialog.png`

## 정한 기준값

- 누설량 상한 5.0 Pa/s(같으면 OK), 경고 구간 없음, 소수 `0.0`, 수량 `#,0`, 수율 `0.0` — `Data/LeakJudge.cs`.
- 집계 생산 428 / NG 9 / OK 419 / 수율 97.9%. 이력 17행(NG 2, 경계 5.0 OK 1).
- 수량·수율 카드 Run, NG 카드 NG>0이면 Alarm.
