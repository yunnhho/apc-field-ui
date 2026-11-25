## 보고: FinalTestYieldMonitor (S2-yield-en, 영문 SKILL.md)

**1) 만든 화면과 PNG**
- 메인 화면 `MainForm` → `out\MainForm.png`
- dialog `OcapDialog`(수율 기준 미달 Lot 처리) → `out\OcapDialog.png`
- 프로젝트 파일: `Views\MainView.cs`, `Views\OcapDialog.cs`, `Data\IMainViewSource.cs`, `Data\SampleMainViewSource.cs`

**2) 마지막 verify.sh 결과**
- 모든 형식 render 점검 0건.
- 빌드 경고 0, 오류 0.

**3) UX 결정**
- 맨 위 큰 판정판(`ApcJudgePanel`)에 "T04 수율 기준 미달 / 최종 수율 94.1 % · 기준 95.0 이상"을 띄운다. 오른쪽에는 최초·최종 수율 카드가 있고, 최종 수율 카드는 알람 테두리다.
- 그다음 테스터 타일 6개("T04  94.1 %", 대기는 "T03  대기")를 둔다. 타일을 누르면 현재 Lot, 수율 카드, Hard/Soft Bin 표, 추이 그래프가 그 테스터로 바뀐다. 처음에는 기준 미달 테스터가 선택돼 있다.
- Soft Bin 1위 비율을 다른 테스터와 비교한 줄을 표 아래에 둔다("Soft Bin 23 비율 T04 1.3 % · 그 외 0.2 %").
- 주 동작은 `미달 Lot 처리` 버튼(Primary) 하나다. 선택 테스터가 기준 이상이거나 Lot이 없으면 버튼이 꺼지고 옆에 이유가 뜬다.
- dialog는 마우스만으로 처리된다. 판정과 사유는 콤보에서 고른다.
- 원인 확인 전에는 Release를 화면이 막는다: Release를 고르면 `판정 확정`이 꺼지고 빨간 띠에 이유가 뜨며, 사유를 고르고 `원인 확인 완료`를 눌러야 풀린다.
- 진행 표에서 OCAP 단계 상태를 램프와 글자로 같이 보인다. Scrap은 빨간 확인창을 한 번 더 거친다.
- 모니터링 화면이라 스캔 입력은 만들지 않았다.

**4) 정한 기준값**
- 최종 수율 95.0 % 이상 통과, 미만 기준 미달(Alarm, Lot Hold). `YieldRule.FinalYieldCriterion` 상수.
- 판정은 0.0으로 반올림한 값으로 한다. 경계값 행: T02 13:48:52 정확히 95.0 → 통과.
- 최초 수율은 기준이 없어 Run. 경고 단계 없음(요구에 없음).
- 샘플 Lot 데이터(고객 코드 ACM·BTX·CRL 등)와 불량 분배는 직접 정했다. Bin 합 = 테스트 수량(T04: 투입 10,000, 진행 7,420, Bin1 6,982 → 최종 94.1 %, 최초 92.4 %).
- Release 선행 조건은 "원인 확인 완료" 하나로 단순화했다.
- 판정은 4개(Hold 유지·추가 검사·Release·Scrap), Rework 제외.
- 이력의 기준 미달 행은 T03 10:12:02 한 건.

**5) 스킬 지침에서 막히거나 모호했던 점**
- `Titled` 제목 줄 높이가 지침에 없어 `S(40) + HeightForRows(8)`로 가늠했다.
- 고정 행 수 표 두 개(Hard Bin 8, Soft Bin 5)를 한 줄에 둘 때 높이 맞추는 방법이 모호했다.
- dialog 푸터에는 비활성 버튼 옆 `Caption`을 둘 자리가 없어 `ApcNoticeBar`로 대체했다.
- 대기 테스터 선택 시 `ApcTrendChart.SetValues`에 빈 배열을 넘겨도 되는지 문서에 없다.
- 모니터링 화면에 스캔 칸을 둘지 판단 기준이 없었다.
- `Control` 이름 충돌을 피하려 `Refresh2`로 지었는데, `Reload` 계열이 낫다.
