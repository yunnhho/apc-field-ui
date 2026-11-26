## 보고: FinalTestYieldMonitor (R2)

**1) 만든 화면과 PNG**
- 메인 화면 `MainForm` → `out\MainForm.png`
  - 위에서부터: 판정판(T04 Lot Hold) → 테스터 타일 6개(`T01  FT26K0421  97.4 %`, 클릭 시 아래 영역 전환) → 선택 테스터 Lot 정보판 → 최초·최종 수율 카드 + Hard Bin 분포표(가로 2행) → Soft Bin 상위 5 + 최근 Lot 최종 수율 추이 → Lot 결과 이력. OCAP 처리 버튼은 이력 제목 줄.
- dialog `OcapDialog` → `out\OcapDialog.png`: 읽기 전용 Lot·수율·주요 Soft Bin → OCAP 8단계 진행표 → 판정·사유.
- 소스: `Views\MainView.cs`, `Views\OcapDialog.cs`, `Data\IMainViewSource.cs`, `Data\SampleMainViewSource.cs`, `Data\Models.cs`, `Data\YieldJudge.cs`. 앱 exe 미실행.

**2) 점검·빌드**
- render 점검 MainForm 0건, OcapDialog 0건. 빌드 경고 0, 오류 0.

**3) UX 결정**
- 판정판: "T04 Lot Hold / FT26K0417 최종 수율 기준 미달 93.6 % (기준 95.0) · 주요 SB23 VOH 하한 이탈".
- 알람 테스터 T04를 처음부터 선택 → 추이에서 T04 Lot의 SB23 증가·수율 하락이 바로 보임.
- Hold Lot 행을 처음부터 선택해 OCAP 버튼 즉시 사용 가능. Hold 아닌 행이면 버튼 꺼짐 + "Hold Lot을 선택하면 처리할 수 있습니다".
- 원인 확인 미완료면 판정 줄 옆에 "Release 불가 · 원인 확인 미완료" 상시 표시, Release 선택 시 확정 비활성.
- Scrap 빨간 확인창, 사유 비면 경고창. 키보드 입력은 사유 한 칸.

**4) 정한 기준값**
- 최종 수율 95.0 % 이상 합격, 미만 Hold(Alarm). 0.0 반올림 후 판정.
- 경고(기준 근접) 96.0 % 이하(`YieldJudge.WarnStart`) — 요청에 없어 새로 정함.
- 진행 중 Lot도 테스트 수량 기준 최종 수율로 타일 색. 최초 수율 판정 없음(Run/Stop).
- Bin 비율 분모 = 진행 수량. Release 조건: 판정 앞 단계(이탈 감지·알람·Lot Hold·장비 사용 중지·원인 확인) 모두 완료.
- 고객 코드 NXA/QMC/SLD/HMS 샘플. 경계값: FT26K0414 95.0 % 합격, FT26K0418 96.0 % 기준 근접.

**5) 스킬 지침에서 막히거나 모호했던 점**
- 고정 높이 `Titled` 칸 높이 계산 API 없음 → 자식 높이 합산 함수 직접 작성. `TitledHeight(title, contentH)` 같은 helper 희망.
- `ApcLampColumn`에 `ApcLampValue`(램프+문구) 넣으면 기본 폭 23px로 잘림 → 폭 직접 지정. 지침에 안내 없음.
- 목록 표 18행에서 세로 스크롤바가 보였는데 render `참고:` 줄이 안 나옴 → 10행으로 수동 조정.
- "타일은 이름만" 규칙과 "Lot·수율 표시" 요청이 충돌 → 한 줄에 넣음.
- dialog `SetFooter`에 꺼진 이유 Caption 자리 없음 → 판정 줄 trailing caption 사용.
