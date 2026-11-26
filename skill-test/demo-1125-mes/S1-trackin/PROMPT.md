apc-field-ui 스킬로 APCSOFT 현장 PC 프로그램 화면을 만든다. 먼저 Skill 도구로 `apc-field-ui`를 호출하고 그 지침을 끝까지 따른다.

- 작업 폴더: C:\00P_apc-field-ui\skill-test\demo-1125-mes\S1-trackin\project (Git Bash: /c/00P_apc-field-ui/skill-test/demo-1125-mes/S1-trackin/project)
- render PNG: C:\00P_apc-field-ui\skill-test\demo-1125-mes\S1-trackin\out\<형식이름>.png
- 고객사 default. 사용자에게 질문할 수 없다 — 빠진 값은 아래 도메인 정보로 정하고 "정한 기준값"으로 보고한다.
- 저장소의 다른 파일은 고치지 않는다. git 명령 금지.

UI·UX 기준: 실제 현장에 설치할 화면이다.
- 이 화면 사용자가 3초 안에 알아야 할 것이 가장 크게, 가장 위에 보이게 한다.
- 작업자는 장갑·바코드 스캐너를 쓴다. 주 동작은 스캔과 큰 버튼, 키보드 입력은 최소로.
- 막아야 하는 동작(NG·인터락·조건 미충족)은 화면이 막고, 막은 이유를 글로 보여 준다.

끝나면 보고: 1) 만든 화면·dialog와 PNG 경로 2) 마지막 render 점검 건수, 빌드 경고·오류 수 3) UX 결정 3~5줄 4) 정한 기준값 5) 스킬 지침에서 막히거나 모호했던 점.

## 화면 요청
반도체 후공정 Wire Bond 장비 Track-in/out 단말 프로그램. 프로젝트명 WireBondEapTerminal.
목적: 공정 설비 프로그램(장비 한 대 옆 단말). 사용자: 장비 오퍼레이터 — Lot 캐리어 바코드를 스캔해 Track-in, 공정이 끝나면 Track-out.
메인 화면: 장비 WB-03 상태(HSMS 연결 상태, GEM 제어 상태 Online Remote/Local, 장비 Idle/Run/Alarm), 진행 Lot(Lot ID·제품·레시피·투입 Strip 수·진행률), Lot 스캔 → Track-in 검증 체크리스트(항목별 OK/NG와 사유) → Track-in 버튼, 대기 Lot 목록(RTD 순위·점수·Q-time 남은 시간·Hold 여부), 최근 이벤트·알람 이력(S6F11 / S5F1).
dialog: Track-out 수량 확인(장비 보고 양품·불량, 작업자 확인 수량, 이전 공정 수량, 차이가 기준 이상이면 Hold 사유 입력).

## 도메인 정보 (MES 학습 경로 W11)
- W/B(Wire Bond): Strip → Strip. 관리: Capillary 수명, 와이어 LOT, Bond pull·shear 시험.
- Track-in 검증 순서: [MES] Lot 상태 WAIT, 현재 공정 = W/B, Hold 없음, Q-time 이내, 장비 자격(제품·레시피 가능) → [EAP] 장비 Online Remote·Idle·알람 없음 → [RMS] 장비 안 레시피가 승인 버전과 같은가 → PP-SELECT, START(S2F41) → Lot RUN.
- 작업자가 장비를 Online Local로 바꾸면 원격 Track-in을 중단한다.
- HSMS 연결: NOT CONNECTED → CONNECTED / NOT SELECTED → SELECTED(메시지 교환 가능). Linktest로 주기적으로 생존 확인.
- Lot 상태: WAIT → RUN → WAIT(다음 공정) … → COMPLETE. HOLD 사유: FDC 이상, SPC 이탈, Q-time 초과, 수량 불일치.
- Q-time: 플라즈마 세정 Track-out 후 W/B 시작까지 한도(예 4시간). 초과하면 Track-in을 막고 자동 Hold.
- RTD 점수 = 30 × 납기 긴급도 + 25 × Q-time 임박도 + 20 × 고객 우선순위 + 15 × (같은 레시피 연속이면 1) − 10 × 셋업 시간 정규화값. Hold·Q-time 초과 Lot은 제외.
- Track-out 수량: 장비 보고값·작업자 입력·이전 공정 수량이 다를 수 있다. 차이가 기준 이상이면 Hold(조용히 덮으면 출하 수량이 틀어진다).
- 메시지: S6F11 이벤트 리포트(Process Start/End), S5F1 알람 보고, S2F41 원격 명령(PP-SELECT, START).
- 표시 형식: 수량 #,0, Q-time 남은 시간 h:mm.
