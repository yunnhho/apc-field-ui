using System.Collections.Generic;

namespace RubberWeighing.Data
{
    /// <summary>메인 화면 데이터 자리. 실제 연결(저울 시리얼, 배치 DB)은 이 인터페이스를 구현해 넣는다.</summary>
    internal interface IMainViewSource
    {
        string RecipeName { get; }
        string BatchNo { get; }
        /// <summary>레시피 원료(순서대로).</summary>
        IList<Material> GetRecipe();
        /// <summary>이 배치에서 이미 확정된 계량(순번 순).</summary>
        IList<WeighRecord> GetDoneRecords();
        /// <summary>현재 원료에 이미 스캔된 LOT(프로그램 재시작 후 이어하기). 없으면 null.</summary>
        string ActiveLot { get; }
        /// <summary>저울 현재값(kg). 화면이 주기적으로 읽는다.</summary>
        decimal ReadScale();
        /// <summary>원료 LOT 스캔이 받아들여져 계량을 시작할 때 알린다(저울 영점·목표 설정 등).</summary>
        void StartWeighing(Material material);
        /// <summary>계량 확정 저장. 허용 범위 안인 값만 온다.</summary>
        void Record(WeighRecord record);
    }
}
