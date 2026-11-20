# A/B 시연 — B(줄인 문서) · C-cleanroom (CleanroomMonitor)

> Sonnet 서브에이전트 보고를 메인 세션이 저장. 사용량: 토큰 120,722 · 도구 18회 · 195초.

## 지표

| 화면 | 처음 render 점검 | render 횟수 | 마지막 점검 |
|---|---|---|---|
| MainForm | 0 | 1 | 0 |
| SettingsDialog | 0 | 1 | 0 |
| AdminPasswordDialog | 0 | 1 | 0 |

- 빌드: 처음 경고 1(CS0108 `MainView.Text`가 `UserControl.Text`를 가림) → 이름 변경 후 0/0.
- 읽은 템플릿: `TempHumiView`, `EquipmentView`, `LotInputDialog`, `AdminPasswordDialog`(+ `FailureAnalysisView.OpenSettings` grep). `docs/api.md`.

## 처음 render 점검 원문

- 세 화면 모두 `점검 0건`(`점검:` 줄 없음).

## 진행 요약

- 만든 것: 메인, 설정 dialog, 설정 진입 전 관리자 확인 dialog(4a 열기 순서를 따름, 요청에는 없음).
- 메인: 8구역 표(상태 칸 `ApcLampValue`), 이탈 셀 상태색+굵게, 선택 구역(A1) 온도·습도 추이 2개, 헤더 "전체 · 정상 4 · 알람 1 · 경고 2 · 통신 끊김 1", 알람바 A3만.
- 설정: 구역별 입력 표 + 저장 주기 `ApcNumericBox`. 오류 `Warn`, 저장 전 `Confirm("저장")`. 설정은 타이틀바 톱니.
- 점검 메시지로 고친 것: 없음. 눈으로 보고 고친 것: 없음.
- PNG에서 보고 둔 것
  - 상태 칸 램프+문구가 가운데 정렬이라 행마다 램프 위치가 다름(컨트롤 한계로 판단).
  - 설정 dialog 저장 주기 입력칸이 폭 전체로 길게 펼쳐짐(`AddFieldRow` 기본 동작).
- 문서가 모호했던 곳: "통신 끊김"/"정지" 구분 문구, 통신 끊김 구역 추이 처리, 파티클 소수 자리(→ `#,0`), 상태 칸 램프 정렬 기준 없음.

## 결과 파일

- 처음 = 마지막: `out/first/MainForm.png`, `out/first/SettingsDialog.png`, `out/first/AdminPasswordDialog.png`

## 정한 기준값

- 온도 20.0~24.0 ℃(여유 0.5), 습도 40.0~60.0 %RH(여유 3.0), 파티클 상한 A구역 100 / B구역 1,000 개/ft³(80%부터 경고), 차압 하한 10.0 Pa(11.0 이하 경고) — `ZoneJudge`.
- 저장 주기 10 s(1~3600). 소수 온도·습도·차압 `0.0`, 파티클 `#,0`. 추이 5분 간격 13점.
- 샘플: 정상 A1·B1·B2·B3, 경고 A2·A4(습도 60.0 = 상한), 알람 A3, 통신 끊김 B4.
