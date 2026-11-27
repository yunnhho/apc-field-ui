# R3-M2 — 체결 순서 TorqueSequence 3차 재시연 (Sonnet, 11/27, APC.UI 1.29.0.0) — 에이전트 보고 요약

4분 37초, 도구 22회, 토큰 132k.

**1) 화면** — MainForm(out\MainForm.png), ReworkDialog(반장 스캔 대기), ReworkBlockedDialog(NG 2회째, render용 서브클래스). `Views/BoltBoard.cs`, `Data/TorqueModels.cs`. 빌드 0/0, 점검 0건, 참고 1건(대기 행 되풀이).

**3) UX 결정** — 스캔 줄(시리얼·사원증 `ScanOnly` + `Accepts`, 모드 `ApcTabStrip`) → 판정판 "NG / 7번 토크 하한 미달 8.6 N·m (하한 9.0)" + 토크·각도 카드 → Primary 하나(상태별 글자) + 이유. 수동 입력 `Clear`/`HasValue`. 결과 표 10행 `FillRows`, 상태 `ApcLampValue`.

**5) 모호했던 점** — 자동 모드의 Primary("공구값 받기")가 실제로는 필요 없음. **`ApcInfoStrip`이 dialog 640에서 말줄임 3건 → 780으로 넓힘, 필요 폭 기준 없음.** 상태별 dialog render 방법 없음(서브클래스). NG 재체결 대상 포인트의 색(다음 Warn과 겹침).

---

## 평가 (메인 세션, 19/20)

| # | 항목 | 점수 | 근거 |
|---|---|---|---|
| 1 | 3초 정보 | ✓ | 판정판 "NG" + 7번 하한 미달 사유 |
| 2 | 주 동작 | ✓ | 판정판 바로 아래 Primary 하나 + 이유 |
| 3 | 메인 막기 | ✓ | 승인 전 체결 잠금, 순서 위반 경고, 진행 중 변경 금지 |
| 4 | dialog 차단 | ✓ | 반장 스캔 전 승인 꺼짐, NG 2회째 재체결 불가 |
| 5 | 새 레이아웃 | ✓ | 윤곽 안 볼트 10개, NG 볼트 크게 |
| 6 | 레이아웃 마무리 | △ | 볼트가 작고 윤곽 가운데가 크게 빔 (M2 네 번 모두 비슷) |
| 7 | 빌드·점검 | ✓ | 0/0, 0건 |
| 8 | 요청 항목 | ✓ | 전부 |
| 9 | 경계값 샘플 | ✓ | 9.0/30, 11.0/60 |
| 10 | 지침 피드백 | ✓ | InfoStrip 필요 폭, 상태별 dialog render, 자동 모드 Primary |

합계 19 (6번 −1)
