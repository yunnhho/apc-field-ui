# A/B 시연 — B(줄인 문서) · D-fatester (FATester)

> Sonnet 서브에이전트 보고를 메인 세션이 저장. 사용량: 토큰 108,267 · 도구 20회 · 138초.

## 지표

| 화면 | 처음 render 점검 | render 횟수 | 마지막 점검 |
|---|---|---|---|
| MainForm | 0건 (`참고:` 1줄) | 2 | 0건 |
| EquipmentDetailDialog | 1건 | 2 | 0건 |

- 빌드 경고 0개, 오류 0개.
- 읽은 템플릿: `EquipmentView`, `EquipmentDetailDialog`, `JudgeView`(카드·집계 참고). `docs/api.md`.

## 처음 render 점검 원문

- MainForm: `참고: MainForm > MainView > Panel > ApcGrid: 표 아래가 빔(행 8, 13행까지 들어감) — 이력·목록이면 샘플 행을 13개로, 설비 수처럼 정해진 목록이면 그대로` / `점검 0건`
- EquipmentDetailDialog: `점검: EquipmentDetailDialog > ApcValueCard: 글자 '프로브 접촉 불량 (2026-11-20 09:12:40)' 말줄임(…)으로 잘림(필요 304px > 폭 271px) — 문구를 줄이거나 칸을 넓힌다` / `점검 1건`

## 진행 요약

- 점검 메시지로 고친 것: 불량 분류 샘플 8 → 13행, 상세 dialog 폭 640 → 800(잘림 해소).
- 눈으로 보고 고친 것: 없음. 상세 dialog 카드 3개는 Footer가 비어 아래가 조금 빔(4번째 카드에 Footer가 있어 줄 높이가 같아져 그대로 둠).
- 구성: 채널 타일 4 → 진행률 카드 4 → 저항 카드 4 → 누설전류 카드 4 → 불량 분류 표. 타일 누르면 상세 dialog(상태 줄 + 카드 4). 헤더 "전체 · 알람 1 · 경고 1 · 대기 1", 알람바는 CH3 오류만.
- 막힌 곳: 없음.
- 문서가 모호했던 곳
  - mΩ·nA 소수 자리: 4-1에 없음, "사용자에게 묻는다"인데 질문 금지 → `0.0`.
  - 진행률 전용 컨트롤 없음 → `ApcValueCard` 정수 %.
  - "완료" 채널을 헤더 집계에서 정상으로 빼는지 명시 없음.
  - 4b에 "내용 폭이 부족하면 폭을 늘린다" 안내 없음(B 문서에서 뺀 문장) → 점검 메시지로 해결.

## 결과 파일

- 처음: `out/first/MainForm.png`, `out/first/EquipmentDetailDialog.png`
- 마지막: `out/MainForm.png`, `out/EquipmentDetailDialog.png`

## 정한 기준값

- 저항(mΩ) 상한 50.0 / 경고 45.0, 누설전류(nA) 상한 100.0 / 경고 90.0, 표시 `0.0`.
- 진행률 정수 %, 누적 사이클 `#,0`, 온도·습도 `0.0`.
- 시험중 채널만 판정, 완료는 Run, 오류·대기는 Stop(판정 안 함).
- 샘플: CH1 시험중 45.0 경계 경고 / CH2 완료 / CH3 오류 E-231 / CH4 대기(빈칸). 불량 분류 13건.
