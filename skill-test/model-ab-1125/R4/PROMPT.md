apc-field-ui 스킬로 APCSOFT 현장 PC 프로그램 화면을 만든다. 먼저 Skill 도구로 apc-field-ui 스킬을 불러 그 지침을 끝까지 따른다(Skill 도구가 없을 때만 Read로 `C:\Users\rladb\.claude\skills\apc-field-ui\SKILL.md`를 읽는다). 앱 exe를 실행했으면 끝나기 전에 닫는다.

- 작업 폴더: C:\00P_apc-field-ui\skill-test\model-ab-1125\R4\project (Git Bash: /c/00P_apc-field-ui/skill-test/model-ab-1125/R4/project)
- render PNG: C:\00P_apc-field-ui\skill-test\model-ab-1125\R4\out\<형식이름>.png
- 고객사 default. 사용자에게 질문할 수 없다 — 빠진 값은 아래 도메인 정보로 정하고 "정한 기준값"으로 보고한다.
- 저장소의 다른 파일은 고치지 않는다. git 명령 금지.

UI·UX 기준: 실제 현장에 설치할 화면이다.
- 이 화면 사용자가 3초 안에 알아야 할 것이 가장 크게, 가장 위에 보이게 한다.
- 작업자는 바코드 스캐너와 마우스를 쓴다. 주 동작은 스캔과 큰 버튼, 키보드 입력은 최소로.
- 막아야 하는 동작(NG·인터락·조건 미충족)은 화면이 막고, 막은 이유를 글로 보여 준다.

끝나면 보고: 1) 만든 화면·dialog와 PNG 경로 2) 마지막 render 점검 건수, 빌드 경고·오류 수 3) UX 결정 3~5줄 4) 정한 기준값 5) 스킬 지침에서 막히거나 모호했던 점.

## 화면 요청
반도체 후공정 Final Test 수율·Bin 모니터링 프로그램. 프로젝트명 FinalTestYieldMonitor.
목적: 모니터링(테스트 구역 전체). 사용자: 테스트 공정 엔지니어·반장.
메인 화면: 테스터 6대(T01~T06) 상태 타일(테스트 중/대기/알람, 현재 Lot·수율), 선택 테스터의 현재 Lot 정보(Lot ID·고객·제품·테스트 프로그램 버전·온도·투입/진행 수량), 수율 카드(최초 수율·최종 수율), Hard Bin 분포 표, Soft Bin 상위 5, 최근 Lot 수율 추이, Lot 결과 이력 표(기준 미달 Lot 표시).
dialog: 수율 기준 미달 Lot 처리(OCAP: Lot·수율·주요 Soft Bin 읽기 전용, 판정 Hold 유지/추가 검사/Release/Scrap 선택, 사유) — 단계를 건너뛴 Release는 막는다.

## 도메인 정보 (MES 학습 경로 W11)
- Final Test: Unit → Unit(Bin별). 관리: 테스트 프로그램 버전, 온도, Bin 결과.
- Hard Bin: 핸들러가 물리적으로 분류하는 통 번호(1 양품, 2~8 불량 유형 묶음). Soft Bin: 테스트 프로그램이 정한 상세 불량 원인(수십~수백 개), 여러 Soft Bin이 하나의 Hard Bin에 매핑된다. 양품 판정은 Hard Bin, 원인 분석은 Soft Bin으로 한다.
- 수율 = 양품 Unit / 테스트 투입 Unit. 예: 투입 10,000, Bin1 9,620 → 96.2%. 재테스트가 있으면 최초 수율(first pass)과 최종 수율을 따로 기록한다.
- 기준: 최종 수율 95.0% 미만이면 기준 미달 → Lot Hold, 고객에게 Lot 처리 승인 요청.
- OCAP: 이탈 → 알람 → Lot Hold → 장비 사용 중지 → 원인 확인 → 판정(Release / 추가 검사 / Rework / Scrap) → 장비 재가동 승인 → 기록. 단계를 건너뛰고 Release하는 것을 막는다.
- 분석 예: "T04를 지난 Lot만 Soft Bin 23이 높다".
- OSAT는 여러 고객 제품을 함께 생산한다 — 화면에 고객 코드를 보여 준다.
- 표시 형식: 수율 % 0.0, 수량 #,0, 온도 ℃ 0.0.
