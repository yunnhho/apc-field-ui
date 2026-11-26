점검: MainForm 0건, TrackOutDialog 0건 · 빌드 경고 0 / 오류 0 (중간 "표 아래 빔" 참고는 샘플 행을 늘려 해소)

UX 결정
- Track-in 판정판이 가장 크게(왼쪽 맨 위, 가능 초록 / 불가 빨강 + 막힌 사유 한 줄), 옆에 HSMS·GEM·장비 카드 3개.
- 흐름은 스캔 → 검증 → 버튼. 버튼은 Primary 하나(Track-in).
- Track-in은 검증 NG면 Enabled=false + 옆에 사유. Track-out은 Process End 수신 전 비활성 + "공정 종료(Process End) 수신 후 가능". 검증은 MES → EAP → RMS 순서로 첫 NG에서 멈추고 NG 행만 빨강 굵게, 나머지 "미검증".
- Track-out 수량 차이가 기준 이상이면 판정판이 HOLD 필요, 사유 콤보는 그때만 켜짐, 사유 없으면 확정 불가, Hold 확정은 Danger 확인창.
- 대기 Lot 표: Hold·Q-time 초과 Lot은 순위·점수 비우고 빨강 램프 + 사유 글. Q-time 90분 이하는 경고색 굵게.

정한 기준값: Q-time 한도 4:00, 경고 90분 이하, Track-out Hold 기준 차이 2 Strip 이상(|작업자 − 장비 양품|과 |장비 양품+불량 − 이전 공정| 중 큰 값, 한 함수), RTD는 요구서 공식 + 0~1 샘플 입력(Q-time 임박도 = 1 − 남은분/240, 0.0 표시), LGA-64는 WB-03 자격 없음, 샘플 WB-03이 L2611-0471 처리 중(62.0%) → 다음 L2611-0482 스캔 시 EAP "장비 Idle" NG, Track-out 샘플 이전 400 / 양품 394 / 불량 6 / 작업자 391(차이 3 → HOLD 필요), 창 제목 "Wire Bond Track-in/out", 기능명 "Lot 스캔 · Track-in".

스킬 지침 개선점
- 한 화면에 표 3개일 때 표 높이 정하는 지침 없음(개수 정해진 체크리스트는 HeightForRows 고정, 나머지 Fill).
- Columns() 두 번 쌓을 때 줄 간격이 헬퍼 몫인지 문서에 없음(실제로 헬퍼가 줌).
- 버튼 비활성 이유 글의 색 규칙 없음(StatusText Alarm 직접 결정).
- 샘플 상태에서 Track-out이 비활성이라 dialog는 render로만 확인.
- 스캔·Track-in/out 클릭은 실행해 보지 않음.
