using UnityEditor;
using UnityEngine;
using WeatherSystem.Profiles;

namespace SpiderRig.Editor.Weather
{
    // Без этого драйвера DailyColor рисуется дефолтным инспектором как фолдаут на
    // 4 строки (Mode, Constant, Gradient — оба контрола видны разом, хотя работает
    // только один). Здесь — одна строка: узкий popup режима + сразу за ним нужный
    // контрол. При 24 полях DailyColor/DailyFloat на профиль разница ощутима сразу.
    [CustomPropertyDrawer(typeof(DailyColor))]
    public class DailyColorDrawer : PropertyDrawer
    {
        private const float ModeWidth = 70f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty mode = property.FindPropertyRelative("_mode");
            var modeEnum = (DailyColor.Mode)mode.enumValueIndex;

            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            EditorGUI.LabelField(labelRect, label);

            float rest = position.x + EditorGUIUtility.labelWidth;
            Rect modeRect = new Rect(rest, position.y, ModeWidth, position.height);
            Rect valueRect = new Rect(rest + ModeWidth + 4f, position.y,
                position.width - EditorGUIUtility.labelWidth - ModeWidth - 4f, position.height);

            EditorGUI.PropertyField(modeRect, mode, GUIContent.none);

            SerializedProperty value = property.FindPropertyRelative(
                modeEnum == DailyColor.Mode.Constant ? "_constant" : "_gradient");
            EditorGUI.PropertyField(valueRect, value, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
