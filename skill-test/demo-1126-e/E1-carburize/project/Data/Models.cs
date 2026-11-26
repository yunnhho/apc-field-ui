using System;
using System.Collections.Generic;

namespace CarburizeFurnaceMonitor.Data
{
    /// <summary>레시피 한 단계(설정값). 설정 없는 값은 null.</summary>
    internal sealed class StepInfo
    {
        public readonly string Name;
        public readonly decimal? SetTemp;      // ℃ (유냉은 없음)
        public readonly decimal? SetCp;        // % (가열·유냉은 없음)
        public readonly int SetMinutes;
        public readonly decimal? OilMin;       // 유냉 유조 온도 하한 ℃
        public readonly decimal? OilMax;       // 유냉 유조 온도 상한 ℃

        public StepInfo(string name, decimal? setTemp, decimal? setCp, int setMinutes, decimal? oilMin, decimal? oilMax)
        {
            Name = name; SetTemp = setTemp; SetCp = setCp; SetMinutes = setMinutes; OilMin = oilMin; OilMax = oilMax;
        }
    }

    /// <summary>로 한 기의 현재 상태와 현재 배치. StepIndex: -1 = 대기(배치 없음), 0~4 = 레시피 단계, 5 = 전 단계 완료.</summary>
    internal sealed class FurnaceInfo
    {
        public readonly string Id;
        public readonly int StepIndex;
        public readonly int StepElapsedMinutes;
        public readonly string BatchNo, PartNo, Recipe;
        public readonly int Quantity;
        public readonly DateTime StartTime;
        public readonly decimal? Temp, Cp, OilTemp;
        public readonly int TempOutMinutes, CpOutMinutes;   // 허용 범위를 벗어난 연속 분
        public readonly bool AlarmOccurred;                 // 이 배치 공정 중 알람이 한 번이라도 있었음
        public readonly DateTime? EndTime;                  // 전 단계 완료 시각

        public FurnaceInfo(string id, int stepIndex, int stepElapsedMinutes, string batchNo, string partNo, int quantity, string recipe,
            DateTime startTime, decimal? temp, decimal? cp, decimal? oilTemp, int tempOutMinutes, int cpOutMinutes, bool alarmOccurred, DateTime? endTime)
        {
            Id = id; StepIndex = stepIndex; StepElapsedMinutes = stepElapsedMinutes; BatchNo = batchNo; PartNo = partNo; Quantity = quantity;
            Recipe = recipe; StartTime = startTime; Temp = temp; Cp = cp; OilTemp = oilTemp;
            TempOutMinutes = tempOutMinutes; CpOutMinutes = cpOutMinutes; AlarmOccurred = alarmOccurred; EndTime = endTime;
        }

        public bool HasBatch { get { return StepIndex >= 0; } }
        public bool IsFinished { get { return StepIndex >= 5; } }
    }

    /// <summary>공정 중 발생한 알람 한 건.</summary>
    internal sealed class AlarmRecord
    {
        public readonly string Time, StepName, Text;
        public AlarmRecord(string time, string stepName, string text) { Time = time; StepName = stepName; Text = text; }
    }

    /// <summary>최근 배치 이력 한 줄.</summary>
    internal sealed class HistoryRow
    {
        public readonly string Furnace, BatchNo, PartNo, Start, End;
        public readonly bool Passed;
        public readonly decimal Hardness, Depth;

        public HistoryRow(string furnace, string batchNo, string partNo, string start, string end, bool passed, decimal hardness, decimal depth)
        {
            Furnace = furnace; BatchNo = batchNo; PartNo = partNo; Start = start; End = end; Passed = passed; Hardness = hardness; Depth = depth;
        }
    }

    /// <summary>선택 로의 이번 배치 프로파일(5분 간격 실측값, 왼쪽 = 배치 시작).</summary>
    internal sealed class ProfileData
    {
        public readonly double[] Temp, Cp;
        public ProfileData(double[] temp, double[] cp) { Temp = temp; Cp = cp; }
    }

    /// <summary>배치 종료 판정 결과(dialog가 만들어 데이터 인터페이스에 넘긴다).</summary>
    internal sealed class BatchJudgement
    {
        public readonly string FurnaceId, BatchNo;
        public readonly decimal Hardness, Depth;
        public readonly bool Passed;
        public readonly string TechnicianBadge;   // 알람 이력이 있는 합격에만 값이 있다

        public BatchJudgement(string furnaceId, string batchNo, decimal hardness, decimal depth, bool passed, string technicianBadge)
        {
            FurnaceId = furnaceId; BatchNo = batchNo; Hardness = hardness; Depth = depth; Passed = passed; TechnicianBadge = technicianBadge;
        }
    }
}
