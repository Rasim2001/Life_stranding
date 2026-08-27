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
        // Слайдер в Constant-режиме линеен по ln(value), не по value — для диапазонов на
        // порядки шире одного (напр. дальность видимости тумана 5..2000 м), где линейная
        // шкала утопила бы весь низ диапазона в первых пикселях. Curve-режим не затронут —
        // AnimationCurve остаётся в сырых единицах, см. DailyFloatDrawer.
        public bool Logarithmic;

        public DailyRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}
