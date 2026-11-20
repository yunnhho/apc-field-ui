# A/B 시연 — A(현재 문서) · A-injection (InjectionMonitor)

> Sonnet 서브에이전트 보고를 메인 세션이 저장. 사용량: 토큰 121,397 · 도구 21회 · 204초.

## 지표

| 화면 | 처음 render 점검 | render 횟수 | 마지막 점검 |
|---|---|---|---|
| MainForm | 1 | 2 | 0 |
| RecipeDialog | 0 | 1 | 0 |

- 빌드 경고 0, 오류 0. 읽은 템플릿: `EquipmentView`, `RecipeDialog`, `TempHumiView`. `docs/api.md`.

## 처음 render 점검 원문

- MainForm: `점검: MainForm > MainView > Panel > ApcGrid: 표 세로 스크롤바(행 13 중 11행 보임) — 샘플 행을 11개로 줄이거나 표를 키운다` / `점검 1건`
- RecipeDialog: `점검 0건`

## 진행 요약

- 점검 메시지로 고친 것: 이력 13 → 11행.
- 눈으로 보고 고친 것: 없음.
- 구성: 타일 6(선택 IM-01) → 카드 3 → 금형 온도 추이 → 알람 이력. 헤더 "전체 · 알람 1 · 경고 1 · 정지 1 · 대기 1". 레시피 저장 시 재판정.
- 문서가 모호했던 곳: 대기 문구 사유 표기, 표 행 수를 render 전에 알 수 없음, dialog 여는 버튼 위치(→ 추이 제목 줄에 "레시피 편집"), 기본 선택 설비.

## 결과 파일

- 처음: `out/first/MainForm.png`, `out/first/RecipeDialog.png` / 마지막: `out/MainForm.png`(RecipeDialog는 처음과 같음)

## 정한 기준값

- 금형 온도 설정 60.0 ℃, 알람 ±5.0(65.0/55.0), 경고 ±4.0. 사출 압력 상한 140.0 bar, 경고 126.0. 사이클 목표 25 s, 상한 28.0, 경고 27.0 — `Data/Judge.cs`.
- 추이 17점 5분 간격, 마지막 점 = 현재값.
