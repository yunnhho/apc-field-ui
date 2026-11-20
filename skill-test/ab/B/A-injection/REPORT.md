# A/B 시연 — B(줄인 문서) · A-injection (InjectionMonitor)

> Sonnet 서브에이전트 보고를 메인 세션이 저장. 사용량: 토큰 119,229 · 도구 23회 · 211초.

## 지표

| 화면 | 처음 render 점검 | render 횟수 | 마지막 점검 |
|---|---|---|---|
| MainForm | 1 | 2 | 0 |
| RecipeDialog | 0 | 1 | 0 |

- 빌드 경고 0, 오류 0. 읽은 템플릿: `EquipmentView`, `RecipeDialog`(구조만 참고). `docs/api.md`.

## 처음 render 점검 원문

- MainForm: `점검: MainForm > MainView > Panel > ApcGrid: 표 세로 스크롤바(행 12 중 11행 보임) — 샘플 행을 11개로 줄이거나 표를 키운다` / `점검 1건`
- RecipeDialog: `점검 0건`

## 진행 요약

- 점검 메시지로 고친 것: 이력 12 → 11행.
- 눈으로 보고 고친 것: 이력 표 상태 열이 좁아 "알.."로 잘림 — **render가 못 잡음** → 열 폭 `S(90)`.
- 문서가 모호했던 곳: 레시피 편집이 "설정"인지(→ 현재값 구획 제목 줄에 일반 버튼), 압력·사이클 경고 시작값, 정지·대기 사유 문구, 이력 행 상태 문구 목록.

## 결과 파일

- 처음: `out/first/MainForm.png`, `out/first/RecipeDialog.png` / 마지막: `out/MainForm.png`

## 정한 기준값

- 금형 온도 55.0~65.0 ℃(경고 63.0/57.0), 설정 60.0. 사출 압력 상한 150.0 bar(경고 140.0). 사이클 목표 30 s, 상한 35.0, 경고 33.0 — `Data\Judge.cs`.
- 샘플: IM-01·03 정상, IM-02 경고, IM-04 알람(152.3 bar), IM-05 정지(41.2 ℃ 미판정), IM-06 대기(빈칸). 첫 선택 IM-04.
