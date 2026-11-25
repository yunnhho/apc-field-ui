## 보고: FinalTestYieldMonitor (S2-yield-ko, 한국어 SKILL.md)

**1) 만든 화면과 PNG**
- 메인 화면 `MainForm` / `MainView` → `out\MainForm.png` (1920x1080)
- dialog `OcapDialog`(수율 기준 미달 Lot 처리) → `out\OcapDialog.png` (980x828)
- 프로젝트 파일: `Views\MainView.cs`, `Views\OcapDialog.cs`, `Data\Models.cs`, `Data\IMainViewSource.cs`, `Data\SampleMainViewSource.cs`, `MainForm.cs`(창 제목·기능명)

**2) 마지막 verify 결과**
- `== 완료: 빌드 경고 0 · 오류 0, 모든 형식 점검 0건` (MainForm 0건, OcapDialog 0건)
- PNG를 직접 열어 확인했다. 처음에는 InfoStrip 말줄임 점검이 나서 칸 폭을 넓혀 해결했다.
- OcapDialog의 "Release 막힘" 상태는 render가 기본 상태만 그려서 코드로만 확인했다.

**3) UX 결정**
- 맨 위 왼쪽에 판정판을 가장 크게 둔다. 처음에는 기준 미달 T04를 선택해 "기준 미달 · Lot Hold"와 근거를 보인다. 정상이면 "수율 정상", 대기면 "대기".
- 선택 테스터는 타일을 누르거나 오른쪽 Lot 스캔칸에 Lot ID를 스캔해 바꾼다. 없는 Lot이면 경고창.
- 판정판 옆에 스캔칸과 Primary 버튼 "미달 Lot 처리". 미달 Lot이 없으면 버튼이 꺼지고 아래에 이유를 쓴다.
- 이력 표: 미달 Lot은 빨간 램프 + 최종 수율 굵은 알람 글자 + 판정 열 "기준 미달 · Hold/Release 승인/Scrap".
- OCAP dialog: Lot·고객·테스터·수율·주요 Soft Bin 3개 읽기 전용. 원인 미확인 + Release면 "판정 확정"이 꺼지고 `ApcNoticeBar`가 알람색으로 이유를 보인다. `Save()`에서도 한 번 더 막는다.
- 사유 필수(비면 경고창), 확정 전 확인창(Scrap은 빨간 확인).

**4) 정한 기준값**
- 수율 기준: 최종 수율 95.0 % 미만이 미달. 판정 함수와 상수는 `YieldRule`(`Limit = 95.0`) 한 곳.
- 경계값 샘플: T05 최종 수율 95.0 % 합격.
- 표시 형식: 수율 `0.0`, 수량 `#,0`, 온도 `0.0 ℃`. 표시 값(`0.0`)으로 비교한다.
- 테스터 상태: T01·T02·T05·T06 테스트 중, T03 대기(값 빈칸), T04 알람.
- 고객 ACM·BTL·CNX·DLT, 프로그램 v3.2.1 / v2.8.0 / v1.9.4, 온도 25.0 / 85.0 / 125.0 ℃.
- Hard Bin 1~8 이름과 Soft Bin→Hard Bin 매핑을 정했다. Hard Bin 합 = 테스트 Unit, Soft Bin ≤ 매핑된 Hard Bin. T04만 Soft Bin 23이 높다(170 EA).
- 최초 수율은 판정 기준이 없어 Run, 정지된 T04에서는 Stop.
- OCAP 단계 샘플: 수율 이탈·알람 / Lot Hold / 장비 사용 중지 완료, 원인 확인 미확인.
- 판정 4개(Hold 유지·추가 검사·Release·Scrap), Rework 제외.
- 이력 오늘 11행, 미달 Lot 3건에 처리 결과 표시.

**5) 스킬 지침에서 막히거나 모호했던 점**
- `verify.sh`의 exe 경로가 sln 기준이 아니라 현재 폴더 기준이라, 저장소 루트에서 실행하면 "exe가 없습니다"가 났다.
- 행 수가 다른 표 두 개(Hard Bin 8 / Soft Bin 5)를 나란히 둘 때 짧은 쪽 아래 빈 곳 처리 지침이 없다.
- `Titled` 제목 줄 높이가 문서에 없어 `S(44)`로 어림했다.
- dialog 안 `ApcInfoStrip` 폭 기준이 없다(5항목 760px에서 말줄임 → 980px).
- `Columns` 칸 안에 스캔칸과 버튼을 한 칸으로 묶는 방법이 문서에 없다.
- 대기 테스터의 판정판 문구·추이 처리 기준이 없어 임의로 정했다.
