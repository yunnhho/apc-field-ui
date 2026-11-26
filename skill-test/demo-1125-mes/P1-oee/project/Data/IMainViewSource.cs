using System;
using System.Collections.Generic;

namespace PressLineOee.Data
{
    internal enum StopKind { Unplanned, Planned }

    /// <summary>프레스 한 대의 금일(생산일자 기준) 생산 집계와 이론 CT.</summary>
    internal sealed class MachineInfo
    {
        public MachineInfo(string id, double theoreticalCtMin, int produced, int good)
        {
            Id = id; TheoreticalCtMin = theoreticalCtMin; Produced = produced; Good = good;
        }

        public string Id { get; private set; }
        /// <summary>이론(표준) CT, 분/개.</summary>
        public double TheoreticalCtMin { get; private set; }
        public int Produced { get; private set; }
        public int Good { get; private set; }
    }

    /// <summary>비가동(정지) 한 건. End가 null이면 지금도 정지 중.</summary>
    internal sealed class StopEvent
    {
        public StopEvent(string id, string machine, StopKind kind, DateTime start, DateTime? end,
            string category, string detail, string note, string user)
        {
            Id = id; Machine = machine; Kind = kind; Start = start; End = end;
            Category = category; Detail = detail; Note = note; User = user;
        }

        public string Id { get; private set; }
        public string Machine { get; private set; }
        public StopKind Kind { get; private set; }
        public DateTime Start { get; private set; }
        public DateTime? End { get; private set; }
        public string Category { get; private set; }
        public string Detail { get; private set; }
        public string Note { get; private set; }
        public string User { get; private set; }

        public StopEvent WithReason(string category, string detail, string note, string user)
        {
            return new StopEvent(Id, Machine, Kind, Start, End, category, detail, note, user);
        }
    }

    /// <summary>
    /// 메인 화면 데이터 자리. 실제 연결(PLC, MES DB)은 이 인터페이스를 구현해 넣는다.
    /// </summary>
    internal interface IMainViewSource
    {
        DateTime Now { get; }
        /// <summary>사유를 입력하는 로그인 사용자(입력자 열).</summary>
        string CurrentUser { get; }
        IList<MachineInfo> GetMachines();
        /// <summary>금일(생산일자 기준) 비가동 이력, 최근 시작 순.</summary>
        IList<StopEvent> GetStopEvents();
        void SaveReason(string eventId, string category, string detail, string note);
    }
}
