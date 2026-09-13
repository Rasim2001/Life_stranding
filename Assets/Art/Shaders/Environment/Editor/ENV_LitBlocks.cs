using UnityEditor;
using UnityEngine;

namespace SpiderRig.Editor.Shaders
{
    // Раскладка блоков инспектора ENV_Lit. Приём взят у AllIn13DShader (см. его
    // CommonStyles.shaderPropertiesStyle и AbstractEffectDrawer.Draw): содержимое группы
    // заворачивается в EditorGUILayout.BeginVertical со стилем helpBox, и группа читается
    // как светлый прямоугольник на тёмном фоне инспектора. Заголовок-галка при этом
    // остаётся СНАРУЖИ бокса, обычной строкой — так в AllIn1 нарисованы эффекты.
    //
    // Треугольника и полосы-шапки здесь нет намеренно: состояние блока выводится из галочки,
    // отдельного «свёрнуто/развёрнуто» не существует, и орган управления для него не нужен.
    //
    // Жёсткое правило прежнее: самодельная тут только раскладка (Rect'ы и боксы). Само
    // свойство рисует Unity через MaterialEditor.ShaderProperty — тот же путь применения
    // значения, что и у обычного вызова. Ни одного EditorGUILayout.Toggle, ни одного
    // material.SetFloat: иначе ломается override-трекинг Material Variants.
    internal static class ENV_LitBlocks
    {
        private const float ToggleWidth = 16f;
        private const float ToggleGap = 3f;
        private const float SeparatorHeight = 1f;
        private const float SeparatorPadding = 4f;

        // Галку эмиссии рисует сам Unity через EmissionEnabledProperty, и позицию он
        // не отдаёт: Rect-версии у метода нет, а попытки запереть его в узкую область или
        // в горизонтальную группу кончались тем, что чекбокс либо обрезался, либо уезжал.
        // Поэтому выравнивание идёт в обратную сторону — по нему.
        //
        // Из IL Unity: EditorGUILayout.Toggle(GUIContent, bool) → EditorGUI.Toggle(rect, label, value)
        // → PrefixLabel, который съедает labelWidth плюс внутренний отступ префикса
        // (kPrefixPaddingRight = 2) и только потом ставит глиф. Значит при схлопнутой до 1
        // подписи чекбокс эмиссии стоит на labelWidth + 2 правее начала строки, и ровно
        // на столько же сдвигаются наши собственные галки, чтобы встать с ним в одну колонку.
        private const float EmissionLabelWidth = 1f;
        private const float TogglePrefixOffset = EmissionLabelWidth + 2f;

        private static GUIStyle boxStyle;

        private static void EnsureStyles()
        {
            if (boxStyle != null) return;

            boxStyle = new GUIStyle(EditorStyles.helpBox);
            boxStyle.margin = new RectOffset(0, 0, 2, 2);
            boxStyle.padding = new RectOffset(8, 8, 6, 6);
        }

        // Светлый прямоугольник вокруг группы. Парный EndBox обязателен.
        public static void BeginBox()
        {
            EnsureStyles();
            EditorGUILayout.BeginVertical(boxStyle);
        }

        public static void EndBox()
        {
            EditorGUILayout.EndVertical();
        }

        // Линия между блоками на основном фоне. Цвет разный для тёмной и светлой темы —
        // одна и та же заливка в светлой теме читается как грязь.
        public static void Separator()
        {
            GUILayout.Space(SeparatorPadding);
            Rect line = EditorGUILayout.GetControlRect(false, SeparatorHeight);
            Color color = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.12f)
                : new Color(0f, 0f, 0f, 0.20f);
            EditorGUI.DrawRect(line, color);
            GUILayout.Space(SeparatorPadding);
        }

        // Галка слева, имя блока справа — без рамки и без треугольника. Возвращает,
        // включён ли блок, то есть надо ли рисовать его содержимое.
        public static bool DrawToggleHeader(MaterialEditor materialEditor, GUIContent label, MaterialProperty toggleProp)
        {
            Rect line = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            var toggleRect = new Rect(line.x + TogglePrefixOffset, line.y, ToggleWidth, line.height);
            var labelRect = new Rect(toggleRect.xMax + ToggleGap, line.y,
                line.xMax - toggleRect.xMax - ToggleGap, line.height);

            materialEditor.ShaderProperty(toggleRect, toggleProp, GUIContent.none);
            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);

            return toggleProp.floatValue > 0.5f;
        }

        // То же по виду, но для эмиссии: у неё нет свойства-переключателя, состояние живёт
        // в GI-флагах материала (MaterialGlobalIlluminationFlags). Рисуем штатным
        // MaterialEditor.EmissionEnabledProperty — он один умеет и флаги, и undo, и
        // мультиредактирование; своя реализация всё это потеряла бы.
        //
        // Метод не принимает ни Rect, ни подпись, поэтому зовём его как есть, в обычном
        // потоке — там он ведёт себя предсказуемо и занимает строку целиком от левого края.
        // Ни BeginArea, ни горизонтальной группы: первая считает координаты в другом
        // пространстве и уносила чекбокс в начало инспектора, вторая отдавала его позицию
        // переговорам о ширине и уводила на середину строки.
        //
        // Своя подпись рисуется поверх уже отрисованной строки: родная схлопнута через
        // labelWidth и невидима, а наша встаёт в ту же колонку, что и у DrawToggleHeader.
        public static bool DrawEmissionToggleHeader(MaterialEditor materialEditor, GUIContent label)
        {
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EmissionLabelWidth;

            bool emissive = materialEditor.EmissionEnabledProperty();

            EditorGUIUtility.labelWidth = previousLabelWidth;

            Rect line = GUILayoutUtility.GetLastRect();
            float toggleXMax = line.x + TogglePrefixOffset + ToggleWidth;
            var labelRect = new Rect(toggleXMax + ToggleGap, line.y,
                line.xMax - toggleXMax - ToggleGap, line.height);
            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);

            return emissive;
        }
    }
}
