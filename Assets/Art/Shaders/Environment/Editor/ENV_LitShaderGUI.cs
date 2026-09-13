using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace SpiderRig.Editor.Shaders
{
    // Инспектор ENV_Lit. Surface Type / blend / cull / receive shadows / queue offset —
    // всё это уже умеет BaseShaderGUI (URP). Ниже — наши блоки: режим маски, правка альбедо,
    // градиент по высоте, плюс валидатор импорта текстур. Спек: docs/lighting-and-shading.md
    // §6 и .scratch/env-lit-shader/spec.md.
    internal class ENV_LitShaderGUI : BaseShaderGUI
    {
        private MaterialProperty maskMapSeparateProp;
        private MaterialProperty maskMapProp;
        private MaterialProperty metallicMapProp;
        private MaterialProperty occlusionMapProp;
        private MaterialProperty smoothnessMapProp;
        private MaterialProperty metallicProp;
        private MaterialProperty smoothnessProp;
        private MaterialProperty smoothnessIsRoughnessProp;
        private MaterialProperty occlusionStrengthProp;
        private MaterialProperty bumpScaleProp;
        private MaterialProperty bumpMapProp;

        private MaterialProperty albedoAdjustProp;
        private MaterialProperty hueShiftProp;
        private MaterialProperty contrastProp;
        private MaterialProperty brightnessProp;

        private MaterialProperty heightGradientProp;
        private MaterialProperty gradientSpaceProp;
        private MaterialProperty gradientMinHeightProp;
        private MaterialProperty gradientMaxHeightProp;
        private MaterialProperty gradientColor01Prop;
        private MaterialProperty gradientColor02Prop;
        private MaterialProperty gradientStrengthProp;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            maskMapSeparateProp = FindProperty("_MaskMapSeparate", properties);
            maskMapProp = FindProperty("_MaskMap", properties);
            metallicMapProp = FindProperty("_MetallicMap", properties);
            occlusionMapProp = FindProperty("_OcclusionMap", properties);
            smoothnessMapProp = FindProperty("_SmoothnessMap", properties);
            metallicProp = FindProperty("_Metallic", properties);
            smoothnessProp = FindProperty("_Smoothness", properties);
            smoothnessIsRoughnessProp = FindProperty("_SmoothnessIsRoughness", properties);
            occlusionStrengthProp = FindProperty("_OcclusionStrength", properties);
            bumpScaleProp = FindProperty("_BumpScale", properties);
            bumpMapProp = FindProperty("_BumpMap", properties);

            albedoAdjustProp = FindProperty("_AlbedoAdjust", properties);
            hueShiftProp = FindProperty("_HueShift", properties);
            contrastProp = FindProperty("_Contrast", properties);
            brightnessProp = FindProperty("_Brightness", properties);

            heightGradientProp = FindProperty("_HeightGradient", properties);
            gradientSpaceProp = FindProperty("_GradientSpace", properties);
            gradientMinHeightProp = FindProperty("_GradientMinHeight", properties);
            gradientMaxHeightProp = FindProperty("_GradientMaxHeight", properties);
            gradientColor01Prop = FindProperty("_GradientColor01", properties);
            gradientColor02Prop = FindProperty("_GradientColor02", properties);
            gradientStrengthProp = FindProperty("_GradientStrength", properties);
        }

        // ShaderGUI.ValidateMaterial у базового класса пустой — вся раскладка keyword'ов
        // и блендинга живёт в статике BaseShaderGUI.SetMaterialKeywords: Surface Type →
        // _SURFACE_TYPE_TRANSPARENT + blend state + render queue, Alpha Clip → _ALPHATEST_ON,
        // Receive Shadows → _RECEIVE_SHADOWS_OFF, _NORMALMAP по наличию текстуры, _EMISSION
        // по GI-флагам, double-sided GI по Cull. Не позвать её — значит молча получить
        // переключатели в инспекторе, которые ничего не делают. Наши четыре keyword'а
        // выводятся из свойств здесь же и тем же способом — руками не ставятся никогда,
        // иначе Material Variants и пресеты не смогут их переопределить.
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material);

            CoreUtils.SetKeyword(material, "_MASKMAP_SEPARATE", material.GetFloat("_MaskMapSeparate") > 0.5f);
            CoreUtils.SetKeyword(material, "_ALBEDO_ADJUST", material.GetFloat("_AlbedoAdjust") > 0.5f);
            CoreUtils.SetKeyword(material, "_HEIGHT_GRADIENT", material.GetFloat("_HeightGradient") > 0.5f);
            CoreUtils.SetKeyword(material, "_GRADIENTSPACE_WORLD", material.GetFloat("_GradientSpace") > 0.5f);
        }

        // Режим маски — не параметр поверхности, а выбор раскладки карт: он меняет сам набор
        // слотов ниже. Поэтому живёт здесь, рядом с Surface Type, а не внутри блока карт,
        // который сам же и перестраивает.
        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);

            // Раскладка каналов — в тултипе подписи, не отдельной строкой: тултип уже есть
            // как штатный механизм GUIContent, а вечно развёрнутый текст занимал место
            // у всех, кому он не нужен здесь и сейчас. Текст сверен с SampleMaskMap /
            // InitializeStandardLitSurfaceData в ENV_LitInput.hlsl — если там поменяются
            // каналы, менять и тут.
            string channelTooltip = maskMapSeparateProp.floatValue > 0.5f
                ? "Три отдельные карты, из каждой читается канал R: Metallic — R, Occlusion — R, Smoothness — R. Формат BC4 (один канал)."
                : "Одна упакованная карта: R — Metallic, G — Occlusion, B — не используется, A — Smoothness. Формат BC7.";
            var maskModeLabel = new GUIContent(maskMapSeparateProp.displayName, channelTooltip);
            materialEditor.ShaderProperty(maskMapSeparateProp, maskModeLabel);
        }

        // Каждая карта — своя секция в светлом боксе. Разделитель остаётся только между
        // Normal Map и Emission: это граница между картами, описывающими саму поверхность,
        // и Emission, который к поверхности не привязан. Между остальными блоками разделитель
        // убран — боксы сами по себе достаточно разделяют секции, вторая линия была лишней.
        // Базовый DrawSurfaceInputs не зовём: он рисует альбедо через TexturePropertySingleLine,
        // то есть мелкой иконкой слева, и набор карт получался разнородным.
        public override void DrawSurfaceInputs(Material material)
        {
            DrawAlbedoBlock();
            DrawMaskMapBlock(material);
            DrawNormalBlock();
            ENV_LitBlocks.Separator();

            DrawEmissionBlock(material);
            DrawAlbedoAdjustBlock();
            DrawHeightGradientBlock();
        }

        private void DrawAlbedoBlock()
        {
            ENV_LitBlocks.BeginBox();

            materialEditor.TextureProperty(baseMapProp, "Albedo", false);
            // sRGB для альбедо обязателен — выключенный отдаёт цвет как линейные данные
            // и материал едет с неверной яркостью. Формат не проверяем: у альбедо он зависит
            // от материала (BC1 без альфы, BC7 при cutout), однозначного ожидания нет.
            ENV_LitTextureValidator.DrawTextureCheck(baseMapProp.textureValue, true, false, null);

            materialEditor.ShaderProperty(baseColorProp, baseColorProp.displayName);

            // Общий тайлинг PBR-набора (альбедо + маска + нормаль) — одно _BaseMap_ST,
            // без отдельных ручек на каждой карте. См. спек, "Не делаем: _ST на нормали и маске".
            DrawTileOffset(materialEditor, baseMapProp);

            ENV_LitBlocks.EndBox();
        }

        private void DrawNormalBlock()
        {
            ENV_LitBlocks.BeginBox();

            materialEditor.TextureProperty(bumpMapProp, "Normal Map", false);
            ENV_LitTextureValidator.DrawTextureCheck(bumpMapProp.textureValue, false, true, TextureImporterFormat.BC5);

            if (bumpMapProp.textureValue != null)
            {
                materialEditor.ShaderProperty(bumpScaleProp, bumpScaleProp.displayName);
            }

            ENV_LitBlocks.EndBox();
        }

        // Эмиссия собрана вручную, а не через BaseShaderGUI.DrawEmissionProperties: тот рисует
        // карту через TexturePropertyWithHDRColor, то есть одной строкой с мелкой иконкой.
        // Галочка Emission и Global Illumination — по-прежнему штатные вызовы MaterialEditor:
        // они пишут не в свойство, а в GI-флаги материала, и своей реализации тут быть не должно.
        private void DrawEmissionBlock(Material material)
        {
            bool emissive = ENV_LitBlocks.DrawEmissionToggleHeader(materialEditor, new GUIContent("Emission"));
            if (!emissive) return;

            ENV_LitBlocks.BeginBox();

            // Подпись у слота пустая: имя блока уже стоит в заголовке снаружи бокса,
            // вторая такая же строка внутри ничего не добавляет.
            materialEditor.TextureProperty(emissionMapProp, string.Empty, false);
            materialEditor.ShaderProperty(emissionColorProp, emissionColorProp.displayName);
            DrawTileOffset(materialEditor, emissionMapProp);
            materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);

            ENV_LitBlocks.EndBox();
        }

        private void DrawMaskMapBlock(Material material)
        {
            bool separate = maskMapSeparateProp.floatValue > 0.5f;

            if (separate)
            {
                // Каждая карта — своя секция: слот, валидация именно её, её скаляр. Раньше
                // три слота и три проверки шли двумя группами, и одинаковые предупреждения
                // подряд не давали понять, какую из карт чинить.
                ENV_LitBlocks.BeginBox();
                materialEditor.TextureProperty(metallicMapProp, "Metallic", false);
                ENV_LitTextureValidator.DrawTextureCheck(metallicMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                materialEditor.ShaderProperty(metallicProp, metallicProp.displayName);
                ENV_LitBlocks.EndBox();

                ENV_LitBlocks.BeginBox();
                materialEditor.TextureProperty(occlusionMapProp, "Occlusion", false);
                ENV_LitTextureValidator.DrawTextureCheck(occlusionMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                materialEditor.ShaderProperty(occlusionStrengthProp, occlusionStrengthProp.displayName);
                ENV_LitBlocks.EndBox();

                ENV_LitBlocks.BeginBox();
                materialEditor.TextureProperty(smoothnessMapProp, "Smoothness / Roughness", false);
                ENV_LitTextureValidator.DrawTextureCheck(smoothnessMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                materialEditor.ShaderProperty(smoothnessProp, smoothnessProp.displayName);
                DrawRoughnessToggle();
                ENV_LitBlocks.EndBox();
            }
            else
            {
                ENV_LitBlocks.BeginBox();
                // Раскладка каналов больше не дублируется в подписи слота — она стоит
                // у переключателя режима в Surface Options.
                materialEditor.TextureProperty(maskMapProp, "Mask Map", false);
                ENV_LitTextureValidator.DrawTextureCheck(maskMapProp.textureValue, false, false, TextureImporterFormat.BC7);

                // Одна карта на три канала — значит и три скаляра относятся к ней одной.
                materialEditor.ShaderProperty(metallicProp, metallicProp.displayName);
                materialEditor.ShaderProperty(occlusionStrengthProp, occlusionStrengthProp.displayName);
                materialEditor.ShaderProperty(smoothnessProp, smoothnessProp.displayName);
                DrawRoughnessToggle();
                ENV_LitBlocks.EndBox();
            }

            DrawAbandonedMaskReferencesWarning(separate);
        }

        private void DrawRoughnessToggle()
        {
            materialEditor.ShaderProperty(smoothnessIsRoughnessProp, smoothnessIsRoughnessProp.displayName);
            ENV_LitTextureValidator.DrawInversionWarning(smoothnessIsRoughnessProp,
                "Канал читается как roughness — если источник на самом деле smoothness, сними галочку.");
        }

        // Текстура, назначенная в материал, грузится в память по факту назначения —
        // независимо от того, какой режим сейчас активен (см. спек, "Брошенные ссылки").
        // Не автоочистка: переключил режим посмотреть — назначения не потерялись, но
        // инспектор обязан сказать, что они висят, и дать кнопку убрать их осознанно.
        private void DrawAbandonedMaskReferencesWarning(bool separateActive)
        {
            int abandoned = 0;
            if (separateActive)
            {
                if (maskMapProp.textureValue != null) abandoned++;
            }
            else
            {
                if (metallicMapProp.textureValue != null) abandoned++;
                if (occlusionMapProp.textureValue != null) abandoned++;
                if (smoothnessMapProp.textureValue != null) abandoned++;
            }

            if (abandoned == 0) return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox($"В неактивном режиме висят текстуры: {abandoned}.", MessageType.Info);
            if (GUILayout.Button("Очистить", GUILayout.Width(80), GUILayout.Height(38)))
            {
                if (separateActive)
                {
                    maskMapProp.textureValue = null;
                }
                else
                {
                    metallicMapProp.textureValue = null;
                    occlusionMapProp.textureValue = null;
                    smoothnessMapProp.textureValue = null;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAlbedoAdjustBlock()
        {
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, new GUIContent("Albedo Adjust"), albedoAdjustProp)) return;

            ENV_LitBlocks.BeginBox();
            materialEditor.ShaderProperty(hueShiftProp, hueShiftProp.displayName);
            materialEditor.ShaderProperty(contrastProp, contrastProp.displayName);
            materialEditor.ShaderProperty(brightnessProp, brightnessProp.displayName);
            ENV_LitBlocks.EndBox();
        }

        private void DrawHeightGradientBlock()
        {
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, new GUIContent("Height Gradient"), heightGradientProp)) return;

            ENV_LitBlocks.BeginBox();
            materialEditor.ShaderProperty(gradientSpaceProp, gradientSpaceProp.displayName);
            materialEditor.ShaderProperty(gradientMinHeightProp, gradientMinHeightProp.displayName);
            materialEditor.ShaderProperty(gradientMaxHeightProp, gradientMaxHeightProp.displayName);
            // Альфа цвета — локальная сила подмеса, её читает шейдер (см. ApplyHeightGradient).
            materialEditor.ShaderProperty(gradientColor01Prop, gradientColor01Prop.displayName);
            materialEditor.ShaderProperty(gradientColor02Prop, gradientColor02Prop.displayName);
            materialEditor.ShaderProperty(gradientStrengthProp, gradientStrengthProp.displayName);
            ENV_LitBlocks.EndBox();
        }
    }
}
