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

            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);

            float rest = position.x + EditorGUIUtility.labelWidth;
            Rect modeRect = new Rect(rest, position.y, ModeWidth, position.height);
            Rect valueRect = new Rect(rest + ModeWidth + 4f, position.y,
                position.width - EditorGUIUtility.labelWidth - ModeWidth - 4f, position.height);

            EditorGUI.PropertyField(modeRect, mode, GUIContent.none);

            DailyRangeAttribute range = fieldInfo.GetCustomAttribute<DailyRangeAttribute>();

            if (modeEnum == DailyFloat.Mode.Constant)
            {
                SerializedProperty constant = property.FindPropertyRelative("_constant");
                if (range != null)
                {
                    float newValue = EditorGUI.Slider(valueRect, GUIContent.none, constant.floatValue, range.Min, range.Max);
                    if (!Mathf.Approximately(newValue, constant.floatValue))
                        constant.floatValue = newValue;
                }
                else
                {
                    EditorGUI.PropertyField(valueRect, constant, GUIContent.none);
                }
            }
            else
            {
                SerializedProperty curve = property.FindPropertyRelative("_curve");
                if (range != null)
                {
                    Rect ranges = new Rect(0f, range.Min, 1f, range.Max - range.Min);
                    EditorGUI.CurveField(valueRect, curve, Color.green, ranges, GUIContent.none);
                }
                else
                {
                    EditorGUI.PropertyField(valueRect, curve, GUIContent.none);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
