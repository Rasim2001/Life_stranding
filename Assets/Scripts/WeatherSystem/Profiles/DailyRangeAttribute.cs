using System;

namespace WeatherSystem.Profiles
{
    // Не PropertyAttribute (не UnityEngine.PropertyAttribute) — намеренно. Unity отдаёт
    // атрибутным PropertyDrawer приоритет над типовыми: если бы это был PropertyAttribute,
    // Unity искал бы драйвер под сам атрибут и подобрал бы встроенный RangeDrawer (он
    // понимает только Float/Integer), и DailyFloatDrawer (типовой драйвер под DailyFloat)
    // вообще перестал бы вызываться — поле показывало бы "Use Range with float or int".
    // Обычный Attribute Unity под сериализацию не резолвит, поэтому DailyFloatDrawer
    // остаётся единственным драйвером и сам читает границы через fieldInfo.
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DailyRangeAttribute : Attribute
    {
        public readonly float Min;
        public readonly float Max;

        public DailyRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
