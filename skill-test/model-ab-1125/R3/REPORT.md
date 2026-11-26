## 보고: FinalTestYieldMonitor (R3)

**1) 만든 화면과 PNG**
- 메인 화면 `MainView`(창 제목 "Final Test 수율 모니터링") → `out\MainForm.png`
- dialog `OcapDialog` → `out\OcapDialog.png`
- 파일: `Views\MainView.cs`, `Views\OcapDialog.cs`, `Data\IMainViewSource.cs`(메서드 5개), `Data\SampleMainViewSource.cs`, `Data\Models.cs`, `Data\YieldJudge.cs`, `MainForm.cs`. 앱 exe 미실행.

**2) 마지막 verify 결과**
- 빌드 경고 0, 오류 0, 점검 0건(MainForm, OcapDialog).

**3) UX 결정**
- 맨 위 판정판 "HOLD 1 Lot" + 기준 미달 테스터·Lot·수율 한 줄. 옆에 최초/최종 수율 카드, 오른쪽 끝 주 동작 "기준 미달 Lot 처리"(Primary).
- 테스터 타일 6개(상태 색 + 수율 %), 클릭 시 아래 영역 전환. 기본 선택 T04(알람).
- 기준 미달이 아닌 테스터 선택 시 OCAP 버튼 비활성 + "선택 Lot은 기준 충족".
- OCAP dialog: 읽기 전용 Lot·수율·Soft Bin. 추가 검사가 "합격"이 아니면 Release 저장을 막고 이유를 경고창으로 표시. 사유 필수. Scrap 빨간 확인창.
- 스캔 입력 없음. 키보드 입력은 사유 한 칸.

**4) 정한 기준값**
- 최종 수율 하한 95.0 %(같은 값 통과), 경고 시작 96.0 %. 소수 한 자리 기준 판정(`YieldJudge`).
- Hard Bin 이름 1~8, Soft Bin 원인명 샘플. 투입 10,000, 고객 코드 A01/B02/C03.
- 상태: T01·T02·T05·T06 테스트 중, T03 대기, T04 알람. 타일 색은 공정 상태·수율 판정 중 나쁜 쪽.
- 판정 4개(Rework 제외).

**5) 스킬 지침에서 막히거나 모호했던 점**
- 작업 폴더가 없어 new-project.sh로 만듦.
- Skill 호출 결과에 무관한 "자동 모드" 안내가 섞여 있어 따르지 않음.
- OCAP 단계(추가 검사 → 판정 → 재가동 승인) 화면 패턴이 없어 임의 설계. 재가동 승인 단계 미구현.
- 상태 범례 위치 규칙 없음(`ApcStatusLegend`를 타일 줄 머리에 둠).
- `ApcGrid` 긴 문구 열을 Fill로 두면 FillWeight로 접힘 → 고정 폭으로 해결. 문서 없음.
- 체크박스 대신 추가 검사 상태 콤보.
