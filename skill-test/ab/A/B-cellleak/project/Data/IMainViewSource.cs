using System;
using System.Collections.Generic;
using APC.UI.Theming;

namespace CellLeakTester.Data
{
    /// <summary>검사 한 건(새 DTO: readonly 필드 + 생성자).</summary>
    internal sealed class LeakRecord
    {
        public readonly DateTime Time;
        public readonly string Barcode;
        public readonly double Leak;

        public LeakRecord(DateTime time, string barcode, double leak)
        {
            Time = time;
            Barcode = barcode;
            Leak = leak;
        }
    }

    /// <summary>금일 집계.</summary>
    internal sealed class TodaySummary
    {
        public readonly int Total;
        public readonly int Ng;

        public TodaySummary(int total, int ng)
        {
            Total = total;
            Ng = ng;
        }
    }

    /// <summary>누설량 판정 기준과 판정 함수(화면 전체가 이것 하나만 쓴다).</summary>
    internal static class LeakJudge
    {
        /// <summary>정한 기준값: 누설량 상한(Pa/s). 같으면 OK, 초과부터 NG.</summary>
        public const double Limit = 5.0;

        public static StatusKind Of(double leak) { return leak > Limit ? StatusKind.Alarm : StatusKind.Run; }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, DB, 시리얼)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        /// <summary>최근 검사 이력(최신이 맨 앞).</summary>
        IList<LeakRecord> GetHistory();
        TodaySummary GetToday();
        /// <summary>현재 LOT 번호·모델·작업자.</summary>
        string[] GetLot();
    }
}
