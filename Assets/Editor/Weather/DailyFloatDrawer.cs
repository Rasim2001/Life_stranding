using System.Reflection;
using UnityEditor;
using UnityEngine;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Та же логика, что у DailyColorDrawer, для скалярного варианта (Constant/Curve).
    // Плюс: если у поля есть [DailyRange] — рисуем слайдер вместо голого числа
    // (Constant) и ограничиваем видимую область кривой (Curve) тем же диапазоном.
    // [DailyRange] — не PropertyAttribute (см. её комментарий), поэтому Unity не пытается
    // подобрать под него отдельный драйвер и этот класс остаётся единственным.
    [CustomPropertyDrawer(typeof(DailyFloat))]
    public class DailyFloatDrawer : PropertyDrawer
    {
        private const float ModeWidth = 70f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty mode = property.FindPropertyRelative("_mode");
            var modeEnum = (DailyFloat.Mode)mode.enumValueIndex;

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);

            // Скоуп подписи закрываем ДО отрисовки значения и относится он только к ней —
            // копирование/вставка тут работает с DailyFloat целиком (режим + значение).
            label = EditorGUI.BeginProperty(labelRect, label, property);
            EditorGUI.LabelField(labelRect, label);
            EditorGUI.EndProperty();

            float rest = position.x + EditorGUIUtility.labelWidth;
            Rect modeRect = new Rect(rest, position.y, ModeWidth, position.height);
            Rect valueRect = new Rect(rest + ModeWidth + 4f, position.y,
                position.width - EditorGUIUtility.labelWidth - ModeWidth - 4f, position.height);

            EditorGUI.PropertyField(modeRect, mode, GUIContent.none);

            DailyRangeAttribute range = fieldInfo.GetCustomAttribute<DailyRangeAttribute>();

            if (modeEnum == DailyFloat.Mode.Constant)
            {
                SerializedProperty constant = property.FindPropertyRelative("_constant");

                // Собственный скоуп над значением — та же причина, что в DailyColorDrawer:
                // без него контекстное меню либо не появляется (Curve сам себе меню не
                // создаёт), либо достаётся внешнему DailyFloat (Generic), а тот принимает
                // из буфера только свой формат, не формат обычного AnimationCurve-поля.
                // См. .scratch/daily-drawers-gradient-paste/spec.md.
                EditorGUI.BeginProperty(valueRect, GUIContent.none, constant);
                if (range != null && range.Logarithmic)
                {
                    float newValue = LogSlider(valueRect, constant.floatValue, range.Min, range.Max);
                    if (!Mathf.Approximately(newValue, constant.floatValue))
                        constant.floatValue = newValue;
                }
                else if (range != null)
                {
                    float newValue = EditorGUI.Slider(valueRect, GUIContent.none, constant.floatValue, range.Min, range.Max);
                    if (!Mathf.Approximately(newValue, constant.floatValue))
                        constant.floatValue = newValue;
                }
                else
                {
                    EditorGUI.PropertyField(valueRect, constant, GUIContent.none);
                }
                EditorGUI.EndProperty();
            }
            else
            {
                SerializedProperty curve = property.FindPropertyRelative("_curve");

                // Curve-режим намеренно остаётся линейным по сырым метрам даже когда
                // range.Logarithmic — полноценный log-редактор AnimationCurve означал бы
                // переотображение keyframe'ов (включая тангенсы) без порчи исходных данных,
                // что заметно дороже, чем это оправдывает разница с Constant-режимом.
                // Принятое упрощение (спек, .scratch/fog-generation-and-layers/spec.md):
                // при большом диапазоне низ шкалы будет визуально сжат — художник
                // компенсирует плотностью ключей у крутых участков, как и для любого
                // другого DailyFloat в этом проекте.
                EditorGUI.BeginProperty(valueRect, GUIContent.none, curve);
                if (range != null)
                {
                    Rect ranges = new Rect(0f, range.Min, 1f, range.Max - range.Min);
                    EditorGUI.CurveField(valueRect, curve, Color.green, ranges, GUIContent.none);
                }
                else
                {
                    EditorGUI.PropertyField(valueRect, curve, GUIContent.none);
                }
                EditorGUI.EndProperty();
            }
        }

        // Слайдер линеен по ln(value), не по value — иначе, например, 5 м и 2000 м не
        // сосуществуют на одной шкале (нижняя половина диапазона схлопывается в несколько
        // пикселей). Числовое поле справа показывает и позволяет ввести реальное значение
        // напрямую, минуя нелинейность слайдера.
        private static float LogSlider(Rect rect, float value, float min, float max)
        {
            const float FieldWidth = 46f;
            Rect sliderRect = new Rect(rect.x, rect.y, rect.width - FieldWidth - 4f, rect.height);
            Rect fieldRect = new Rect(rect.xMax - FieldWidth, rect.y, FieldWidth, rect.height);

            float logMin = Mathf.Log(min);
            float logMax = Mathf.Log(max);
            float t = Mathf.InverseLerp(logMin, logMax, Mathf.Log(Mathf.Clamp(value, min, max)));

            float newT = GUI.HorizontalSlider(sliderRect, t, 0f, 1f);
            float sliderValue = Mathf.Exp(Mathf.Lerp(logMin, logMax, newT));
            float shown = Mathf.Approximately(newT, t) ? value : sliderValue;

            float fieldValue = EditorGUI.FloatField(fieldRect, shown);
            return Mathf.Clamp(fieldValue, min, max);
        }
    }
}
