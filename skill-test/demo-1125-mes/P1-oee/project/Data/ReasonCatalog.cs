using System.Collections.Generic;

namespace PressLineOee.Data
{
    /// <summary>비가동 사유 대분류와 세부 사유. TODO: 실제로는 MES 기준정보에서 받는다.</summary>
    internal static class ReasonCatalog
    {
        private static readonly string[] Categories = { "고장", "셋업", "자재 대기", "인원 부족", "품질 대기" };

        private static readonly Dictionary<string, string[]> Details = new Dictionary<string, string[]>
        {
            { "고장", new[] { "프레스 본체", "클러치·브레이크", "금형 파손", "전기·제어" } },
            { "셋업", new[] { "금형 교체", "코일·소재 교체", "조건 조정" } },
            { "자재 대기", new[] { "코일 공급 대기", "부자재 대기", "금형 인출 대기" } },
            { "인원 부족", new[] { "작업자 부재", "교대 지연" } },
            { "품질 대기", new[] { "초물 검사 대기", "품질 판정 대기" } },
        };

        public static string[] GetCategories() { return Categories; }

        public static string[] GetDetails(string category)
        {
            string[] d;
            return category != null && Details.TryGetValue(category, out d) ? d : new string[0];
        }
    }
}
