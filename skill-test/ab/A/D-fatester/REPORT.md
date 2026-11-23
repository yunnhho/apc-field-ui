# A/B 시연 — A(현재 문서) · D-fatester (FATester)

> Sonnet 서브에이전트 보고를 메인 세션이 저장. 사용량: 토큰 117,449 · 도구 21회 · 148초.

## 지표

| 화면 | 처음 render 점검 | render 횟수 | 마지막 점검 |
|---|---|---|---|
| MainForm | 0 (참고 1줄) | 2 | 0 |
| EquipmentDetailDialog | 0 | 2 | 0 |

- 빌드 경고 0, 오류 0. 읽은 템플릿: `EquipmentView`, `EquipmentDetailDialog`, `FailureAnalysisView`. `docs/api.md`.

## 처음 render 점검 원문

- MainForm: `참고: MainForm > MainView > Panel > ApcGrid: 표 아래가 빔(행 12, 20행까지 들어감) — 이력·목록이면 샘플 행을 20개로, 설비 수처럼 정해진 목록이면 그대로` / `점검 0건`
- EquipmentDetailDialog: `점검 0건`

## 진행 요약

- 점검 메시지로 고친 것: 없음. `참고:`에 따라 불량 표 12 → 20행(모드·위치·시각 간격 섞음).
- 눈으로 보고 고친 것: 상세 dialog 카드 중 사이클 카드에만 Footer가 있어 들쭉날쭉 → 모든 카드에 Footer.
- 구성: CH1~4 타일 → 채널별 현황 표(시료·항목·진행률·저항·누설전류) → 불량 분류 표. 타일 클릭 → 상세 dialog. 헤더 "전체 · 알람 1 · 대기 1".
- 막힌 곳: 없음.
- 문서가 모호했던 곳
  - mΩ·nA 소수 자리 없음 + "사용자에게 묻는다" vs 질문 금지.
  - 상·하한이 요구서에 없으면 측정값을 판정하지 않음 → 경계값 샘플 행 넣을 수 없음.
  - "채널 선택 시"가 타일 Selected인지 클릭인지 불분명 → 클릭.
  - 불량 표에 램프 열을 둘지 불분명 → 요청한 4열만.

## 결과 파일

- 처음: `out/first/MainForm.png`, `out/first/EquipmentDetailDialog.png`
- 마지막: `out/MainForm.png`, `out/EquipmentDetailDialog.png`

## 정한 기준값

- 제목 "FA 시험기 모니터링". 소수: mΩ·nA·℃·%RH `0.0`, 사이클 `#,0`, 진행률 정수 %.
- 상태: 시험중·완료 Run, 오류 Alarm, 대기 Stop(`ChannelRow.Status` 하나). 측정값은 상·하한 없음 → 판정 안 함.
- 샘플: CH1 열충격 62% 42.3 mΩ 8.6 nA / CH2 완료 / CH3 오류 E-304 "인가 실패" / CH4 대기(빈칸).
