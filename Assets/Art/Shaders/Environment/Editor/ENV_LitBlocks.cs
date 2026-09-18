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

        // Кнопка сброса на дефолт (тикет 2-01). Ширина квадратная под однобуквенную
        // надпись "R", без иконки — решение владельца: форма произвольна, лишь бы
        // вписывалась в квадрат.
        private const float ResetButtonWidth = 18f;
        private const float ResetButtonGap = 2f;
        private const float InlineTextureGap = 8f;
        private const float InlineColorTextureMinViewWidth = 430f;
        private const float ColorTextureTileOffsetIndent = 48f;

        private static GUIStyle boxStyle;
        private static GUIStyle mutedMiniLabelStyle;

        // Один эталонный материал на шейдер — источник дефолтов для кнопки сброса.
        // Не Shader.GetPropertyDefaultFloatValue/DefaultVectorValue: то API отдаёт число
        // так, как оно записано в Properties, то есть Color — в гамме, а
        // MaterialProperty.colorValue хранит уже линейное значение проекта. Сброс цвета
        // тихо разошёлся бы с тем, что художник заготовил в Substance, и разница не ловится
        // глазами за один проход. Материал того же шейдера получает те же значения тем же
        // путём, что и любой новый материал — вопрос гаммы не возникает вовсе.
        private static Material referenceMaterial;

        private static Material GetReferenceMaterial(Shader shader)
        {
            if (referenceMaterial != null && referenceMaterial.shader != shader)
            {
                Object.DestroyImmediate(referenceMaterial);
                referenceMaterial = null;
            }

            if (referenceMaterial == null)
            {
                referenceMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }

            return referenceMaterial;
        }

        // Звать из ShaderGUI.OnClosed — иначе эталонный материал переживает закрытие
        // инспектора как утечка HideAndDontSave-объекта.
        public static void ReleaseReferenceMaterial()
        {
            if (referenceMaterial == null) return;
            Object.DestroyImmediate(referenceMaterial);
            referenceMaterial = null;
        }

        private static void EnsureStyles()
        {
            if (boxStyle != null) return;

            boxStyle = new GUIStyle(EditorStyles.helpBox);
            boxStyle.margin = new RectOffset(0, 0, 2, 2);
            boxStyle.padding = new RectOffset(8, 8, 6, 6);

            mutedMiniLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true
            };
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

        public static void MutedMiniLabel(string text)
        {
            MutedMiniLabel(new GUIContent(text));
        }

        public static void MutedMiniLabel(GUIContent content)
        {
            EnsureStyles();
            GUILayout.Label(content, mutedMiniLabelStyle);
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

        // Скаляр / вектор / цвет со своей кнопкой сброса (тикет 2-01). Кнопка прижата
        // к ПЕРВОЙ строке свойства — у высоких контролов (например [HDR]-цвета с
        // дополнительной строкой интенсивности) она не растягивается на всю высоту.
        public static void Property(MaterialEditor materialEditor, MaterialProperty prop, string label)
        {
            Property(materialEditor, prop, new GUIContent(label));
        }

        public static void Property(MaterialEditor materialEditor, MaterialProperty prop, GUIContent label)
        {
            Property(materialEditor, prop, label, 0f);
        }

        // Вариант с отступом только подписи: поле значения и кнопка R остаются в общих
        // колонках. Нужен для вложенных Projection/Tiling/Rotation в Blend Layers.
        public static void Property(MaterialEditor materialEditor, MaterialProperty prop,
            GUIContent label, float labelIndent)
        {
            float height = materialEditor.GetPropertyHeight(prop, label.text);
            Rect line = EditorGUILayout.GetControlRect(false, height);

            var propRect = new Rect(line.x, line.y, line.width - ResetButtonWidth - ResetButtonGap, line.height);
            var buttonRect = new Rect(propRect.xMax + ResetButtonGap, line.y,
                ResetButtonWidth, EditorGUIUtility.singleLineHeight);

            if (labelIndent > 0f)
            {
                DrawIndentedLabel(propRect, label, labelIndent);
                Rect fieldRect = ValueRect(propRect);
                materialEditor.ShaderProperty(fieldRect, prop, GUIContent.none);
            }
            else
            {
                materialEditor.ShaderProperty(propRect, prop, label);
            }

            DrawResetButton(buttonRect, prop, materialEditor);
        }

        // Dropdown без R. При labelIndent смещается только текст подписи, значение остаётся
        // в той же колонке, что и остальные свойства блока.
        public static void DropdownProperty(MaterialEditor materialEditor, MaterialProperty prop,
            GUIContent label, float labelIndent = 0f)
        {
            if (labelIndent <= 0f)
            {
                materialEditor.ShaderProperty(prop, label);
                return;
            }

            float height = materialEditor.GetPropertyHeight(prop, label.text);
            Rect line = EditorGUILayout.GetControlRect(false, height);
            DrawIndentedLabel(line, label, labelIndent);
            materialEditor.ShaderProperty(ValueRect(line), prop, GUIContent.none);
        }

        // Vector2 со своей кнопкой сброса (тикет 2-03) — для тайлинга RGB Noise. Готового
        // [Vector2] в Unity нет; вендорский drawer в AllIn13DShader/Editor/Drawers не трогаем —
        // атрибут в шейдере привязался бы к чужому коду. Компоненты z/w сохраняются как есть:
        // свойство может пригодиться под них позже (тикет 2-04), эта раскладка их не читает.
        // Кнопка одна на весь Vector2 (решено в 2-01), не по кнопке на компонент.
        public static void Vector2Property(MaterialEditor materialEditor, MaterialProperty prop, GUIContent label)
        {
            Vector2Property(materialEditor, prop, label, 0f);
        }

        public static void Vector2Property(MaterialEditor materialEditor, MaterialProperty prop,
            GUIContent label, float labelIndent)
        {
            float height = EditorGUIUtility.singleLineHeight;
            Rect line = EditorGUILayout.GetControlRect(false, height);

            var propRect = new Rect(line.x, line.y, line.width - ResetButtonWidth - ResetButtonGap, height);
            var buttonRect = new Rect(propRect.xMax + ResetButtonGap, line.y, ResetButtonWidth, height);

            Vector4 current = prop.vectorValue;
            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            Vector2 edited;
            if (labelIndent > 0f)
            {
                DrawIndentedLabel(propRect, label, labelIndent);
                edited = EditorGUI.Vector2Field(ValueRect(propRect), GUIContent.none,
                    new Vector2(current.x, current.y));
            }
            else
            {
                edited = EditorGUI.Vector2Field(propRect, label, new Vector2(current.x, current.y));
            }

            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(prop.displayName);
                prop.vectorValue = new Vector4(edited.x, edited.y, current.z, current.w);
            }
            EditorGUI.showMixedValue = previousMixedValue;

            if (GUI.Button(buttonRect, "R"))
            {
                var material = materialEditor.target as Material;
                if (material != null)
                {
                    Vector4 reference = GetReferenceMaterial(material.shader).GetVector(prop.name);
                    materialEditor.RegisterPropertyChangeUndo(prop.displayName);
                    prop.vectorValue = new Vector4(reference.x, reference.y, current.z, current.w);
                }
            }
        }

        // Слот текстуры со своей кнопкой сброса — сбрасывает в None, как требует тикет.
        public static void TextureSlot(MaterialEditor materialEditor, MaterialProperty prop, string label, bool scaleOffset)
        {
            float height = materialEditor.GetPropertyHeight(prop, label);
            Rect line = EditorGUILayout.GetControlRect(false, height);

            DrawTextureSlot(materialEditor, prop, label, scaleOffset, line);
        }

        // Вариант для слотов, которые проверяет ENV_LitTextureValidator. Он сохраняет
        // стандартное поле назначения, mixed values, Undo и Material Variants, но намеренно
        // не вызывает MaterialEditor.TextureProperty: тот добавляет собственный большой
        // compatibility warning поверх нашего компактного предупреждения.
        public static void TextureSlotWithoutCompatibilityWarning(
            MaterialEditor materialEditor, MaterialProperty prop, string label)
        {
            float height = materialEditor.GetPropertyHeight(prop, label);
            Rect line = EditorGUILayout.GetControlRect(false, height);

            DrawTextureSlotWithoutCompatibilityWarning(materialEditor, prop, label, line);
        }

        // Общий контрол цветной карты для Base/Top/Mix и Emission: цвет отдельной строкой, затем большой
        // слот. На широкой панели Scale/Offset занимает пустую область слева от превью;
        // на узкой переезжает под слот. Рисуют значения по-прежнему штатные методы
        // MaterialEditor, поэтому Material Variants, mixed values и Undo не обходятся.
        public static void ColorTexture(MaterialEditor materialEditor,
            MaterialProperty mapProp, MaterialProperty colorProp,
            GUIContent mapLabel, GUIContent colorLabel)
        {
            Property(materialEditor, colorProp, colorLabel);

            // В Layout GetControlRect может вернуть служебный Rect шириной 1 px, тогда как
            // в Repaint тот же вызов уже возвращает фактическую ширину. Нельзя выбирать по
            // нему ветку с другим числом EditorGUILayout-вызовов: следующие контролы получат
            // чужие Rect. currentViewWidth стабилен в пределах IMGUI-прохода и поэтому задаёт
            // один и тот же режим раскладки для Layout и Repaint.
            bool drawTileOffsetInline = EditorGUIUtility.currentViewWidth >= InlineColorTextureMinViewWidth;
            float textureHeight = materialEditor.GetPropertyHeight(mapProp, mapLabel.text);
            Rect textureLine = EditorGUILayout.GetControlRect(false, textureHeight);
            DrawTextureSlot(materialEditor, mapProp, mapLabel.text, false, textureLine);

            if (drawTileOffsetInline)
            {
                float tileOffsetWidth = textureLine.width
                    - ResetButtonWidth - ResetButtonGap
                    - textureHeight - InlineTextureGap;
                float rowHeight = EditorGUIUtility.singleLineHeight;
                float tileOffsetHeight = rowHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
                var tileOffsetRect = new Rect(textureLine.x + ColorTextureTileOffsetIndent,
                    textureLine.y + rowHeight + EditorGUIUtility.standardVerticalSpacing,
                    tileOffsetWidth - ColorTextureTileOffsetIndent, tileOffsetHeight);
                DrawTileOffset(materialEditor, mapProp, tileOffsetRect);
            }
            else
            {
                TileOffset(materialEditor, mapProp, ColorTextureTileOffsetIndent);
            }
        }

        private static void DrawTextureSlot(MaterialEditor materialEditor, MaterialProperty prop,
            string label, bool scaleOffset, Rect line)
        {
            var propRect = new Rect(line.x, line.y,
                line.width - ResetButtonWidth - ResetButtonGap, line.height);
            var buttonRect = new Rect(propRect.xMax + ResetButtonGap, line.y,
                ResetButtonWidth, EditorGUIUtility.singleLineHeight);

            materialEditor.TextureProperty(propRect, prop, label, scaleOffset);
            DrawResetButton(buttonRect, prop, materialEditor);
        }

        private static void DrawTextureSlotWithoutCompatibilityWarning(
            MaterialEditor materialEditor, MaterialProperty prop, string label, Rect line)
        {
            var propRect = new Rect(line.x, line.y,
                line.width - ResetButtonWidth - ResetButtonGap, line.height);
            var buttonRect = new Rect(propRect.xMax + ResetButtonGap, line.y,
                ResetButtonWidth, EditorGUIUtility.singleLineHeight);

            var propertyScopeRect = new Rect(propRect.x, propRect.y,
                propRect.width, EditorGUIUtility.singleLineHeight);
            MaterialEditor.BeginProperty(propertyScopeRect, prop);
            materialEditor.BeginAnimatedCheck(propRect, prop);

            Rect textureRect = EditorGUI.PrefixLabel(propRect, new GUIContent(label));
            textureRect.xMin = textureRect.xMax - EditorGUIUtility.fieldWidth;

            bool previousMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            Texture selectedTexture = EditorGUI.ObjectField(
                textureRect, prop.textureValue, typeof(Texture2D), false) as Texture;
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(prop.displayName);
                prop.textureValue = selectedTexture;
            }

            EditorGUI.showMixedValue = previousMixedValue;
            materialEditor.EndAnimatedCheck();
            MaterialEditor.EndProperty();

            DrawResetButton(buttonRect, prop, materialEditor);
        }

        private static Rect ValueRect(Rect line)
        {
            float labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, line.width);
            return new Rect(line.x + labelWidth, line.y, line.width - labelWidth, line.height);
        }

        private static void DrawIndentedLabel(Rect line, GUIContent label, float labelIndent)
        {
            float labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, line.width);
            var labelRect = new Rect(line.x + labelIndent, line.y,
                Mathf.Max(0f, labelWidth - labelIndent), EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);
        }

        // Tiling + Offset одного _ST-свойства, каждая ось — своя кнопка сброса: Tiling (XY)
        // возвращает компоненты x/y, Offset (ZW) — z/w, вторая пара при этом не трогается
        // ни одной из кнопок.
        public static void TileOffset(MaterialEditor materialEditor, MaterialProperty stProp,
            float leftIndent = 0f)
        {
            if (stProp == null) return;

            float rowHeight = EditorGUIUtility.singleLineHeight;
            float totalHeight = rowHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
            Rect line = EditorGUILayout.GetControlRect(false, totalHeight);
            line.x += leftIndent;
            line.width -= leftIndent;

            DrawTileOffset(materialEditor, stProp, line);
        }

        private static void DrawTileOffset(MaterialEditor materialEditor, MaterialProperty stProp, Rect line)
        {
            float rowHeight = EditorGUIUtility.singleLineHeight;
            float totalHeight = rowHeight * 2f + EditorGUIUtility.standardVerticalSpacing;

            var propRect = new Rect(line.x, line.y, line.width - ResetButtonWidth - ResetButtonGap, totalHeight);
            materialEditor.TextureScaleOffsetProperty(propRect, stProp);

            var tilingButtonRect = new Rect(propRect.xMax + ResetButtonGap, line.y, ResetButtonWidth, rowHeight);
            var offsetButtonRect = new Rect(propRect.xMax + ResetButtonGap,
                line.y + rowHeight + EditorGUIUtility.standardVerticalSpacing, ResetButtonWidth, rowHeight);

            if (GUI.Button(tilingButtonRect, "R"))
            {
                Material material = materialEditor.target as Material;
                Vector2 reference = GetReferenceMaterial(material.shader).GetTextureScale(stProp.name);
                Vector4 current = stProp.textureScaleAndOffset;
                materialEditor.RegisterPropertyChangeUndo(stProp.displayName + " Tiling");
                stProp.textureScaleAndOffset = new Vector4(reference.x, reference.y, current.z, current.w);
            }

            if (GUI.Button(offsetButtonRect, "R"))
            {
                Material material = materialEditor.target as Material;
                Vector2 reference = GetReferenceMaterial(material.shader).GetTextureOffset(stProp.name);
                Vector4 current = stProp.textureScaleAndOffset;
                materialEditor.RegisterPropertyChangeUndo(stProp.displayName + " Offset");
                stProp.textureScaleAndOffset = new Vector4(current.x, current.y, reference.x, reference.y);
            }
        }

        // Пишет только через MaterialProperty (RegisterPropertyChangeUndo + присваивание) —
        // тот же путь, каким Unity применяет любое ручное изменение в инспекторе. Ни одного
        // material.SetFloat: иначе рвётся override-трекинг Material Variants (см. шапку файла).
        private static void DrawResetButton(Rect buttonRect, MaterialProperty prop, MaterialEditor materialEditor)
        {
            if (!GUI.Button(buttonRect, "R")) return;

            var material = materialEditor.target as Material;
            if (material == null) return;
            Material reference = GetReferenceMaterial(material.shader);

            materialEditor.RegisterPropertyChangeUndo(prop.displayName);

            switch (prop.propertyType)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    prop.textureValue = null;
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    prop.colorValue = reference.GetColor(prop.name);
                    break;
                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    prop.vectorValue = reference.GetVector(prop.name);
                    break;
                default:
                    prop.floatValue = reference.GetFloat(prop.name);
                    break;
            }
        }
    }
}
