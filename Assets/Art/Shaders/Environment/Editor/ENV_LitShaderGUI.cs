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
        private MaterialProperty heightMapProp;
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

        private MaterialProperty projectionSpaceProp;
        private MaterialProperty patternProjectionProp;

        private MaterialProperty heightGradientProp;
        private MaterialProperty gradientMinHeightProp;
        private MaterialProperty gradientMaxHeightProp;
        private MaterialProperty gradientColor01Prop;
        private MaterialProperty gradientColor02Prop;
        private MaterialProperty gradientStrengthProp;

        private MaterialProperty overlayLayer0Prop;
        private MaterialProperty overlayMap0Prop;
        private MaterialProperty overlayColor0Prop;
        private MaterialProperty overlayNormalMap0Prop;
        private MaterialProperty overlayNormalScale0Prop;
        private MaterialProperty overlayTiling0Prop;
        private MaterialProperty overlayMetallic0Prop;
        private MaterialProperty overlaySmoothness0Prop;
        private MaterialProperty overlayCoverage0Prop;
        private MaterialProperty overlayEdgeSoftness0Prop;
        private MaterialProperty overlayHeightDepth0Prop;

        private MaterialProperty patternProp;
        private MaterialProperty patternMapProp;
        private MaterialProperty patternChannelProp;
        private MaterialProperty patternTilingProp;
        private MaterialProperty patternStrengthProp;

        private MaterialProperty materialMixProp;
        private MaterialProperty materialMixTwoProp;
        private MaterialProperty mixMaskFromTextureProp;
        private MaterialProperty mixMaskMapProp;
        private MaterialProperty mixMaskTilingProp;
        private MaterialProperty mixEdgeSoftnessProp;
        private MaterialProperty mixCoverageProp;

        private MaterialProperty mixMap1Prop;
        private MaterialProperty mixColor1Prop;
        private MaterialProperty mixNormalMap1Prop;
        private MaterialProperty mixNormalScale1Prop;
        private MaterialProperty mixTiling1Prop;
        private MaterialProperty mixMetallic1Prop;
        private MaterialProperty mixSmoothness1Prop;

        private MaterialProperty mixMap2Prop;
        private MaterialProperty mixColor2Prop;
        private MaterialProperty mixNormalMap2Prop;
        private MaterialProperty mixNormalScale2Prop;
        private MaterialProperty mixTiling2Prop;
        private MaterialProperty mixMetallic2Prop;
        private MaterialProperty mixSmoothness2Prop;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            maskMapSeparateProp = FindProperty("_MaskMapSeparate", properties);
            maskMapProp = FindProperty("_MaskMap", properties);
            metallicMapProp = FindProperty("_MetallicMap", properties);
            occlusionMapProp = FindProperty("_OcclusionMap", properties);
            smoothnessMapProp = FindProperty("_SmoothnessMap", properties);
            heightMapProp = FindProperty("_HeightMap", properties);
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

            projectionSpaceProp = FindProperty("_ProjectionSpace", properties);
            patternProjectionProp = FindProperty("_PatternProjection", properties);

            heightGradientProp = FindProperty("_HeightGradient", properties);
            gradientMinHeightProp = FindProperty("_GradientMinHeight", properties);
            gradientMaxHeightProp = FindProperty("_GradientMaxHeight", properties);
            gradientColor01Prop = FindProperty("_GradientColor01", properties);
            gradientColor02Prop = FindProperty("_GradientColor02", properties);
            gradientStrengthProp = FindProperty("_GradientStrength", properties);

            overlayLayer0Prop = FindProperty("_OverlayLayer0", properties);
            overlayMap0Prop = FindProperty("_OverlayMap0", properties);
            overlayColor0Prop = FindProperty("_OverlayColor0", properties);
            overlayNormalMap0Prop = FindProperty("_OverlayNormalMap0", properties);
            overlayNormalScale0Prop = FindProperty("_OverlayNormalScale0", properties);
            overlayTiling0Prop = FindProperty("_OverlayTiling0", properties);
            overlayMetallic0Prop = FindProperty("_OverlayMetallic0", properties);
            overlaySmoothness0Prop = FindProperty("_OverlaySmoothness0", properties);
            overlayCoverage0Prop = FindProperty("_OverlayCoverage0", properties);
            overlayEdgeSoftness0Prop = FindProperty("_OverlayEdgeSoftness0", properties);
            overlayHeightDepth0Prop = FindProperty("_OverlayHeightDepth0", properties);

            patternProp = FindProperty("_Pattern", properties);
            patternMapProp = FindProperty("_PatternMap", properties);
            patternChannelProp = FindProperty("_PatternChannel", properties);
            patternTilingProp = FindProperty("_PatternTiling", properties);
            patternStrengthProp = FindProperty("_PatternStrength", properties);

            materialMixProp = FindProperty("_MaterialMix", properties);
            materialMixTwoProp = FindProperty("_MaterialMixTwo", properties);
            mixMaskFromTextureProp = FindProperty("_MixMaskFromTexture", properties);
            mixMaskMapProp = FindProperty("_MixMaskMap", properties);
            mixMaskTilingProp = FindProperty("_MixMaskTiling", properties);
            mixEdgeSoftnessProp = FindProperty("_MixEdgeSoftness", properties);
            mixCoverageProp = FindProperty("_MixCoverage", properties);

            mixMap1Prop = FindProperty("_MixMap1", properties);
            mixColor1Prop = FindProperty("_MixColor1", properties);
            mixNormalMap1Prop = FindProperty("_MixNormalMap1", properties);
            mixNormalScale1Prop = FindProperty("_MixNormalScale1", properties);
            mixTiling1Prop = FindProperty("_MixTiling1", properties);
            mixMetallic1Prop = FindProperty("_MixMetallic1", properties);
            mixSmoothness1Prop = FindProperty("_MixSmoothness1", properties);

            mixMap2Prop = FindProperty("_MixMap2", properties);
            mixColor2Prop = FindProperty("_MixColor2", properties);
            mixNormalMap2Prop = FindProperty("_MixNormalMap2", properties);
            mixNormalScale2Prop = FindProperty("_MixNormalScale2", properties);
            mixTiling2Prop = FindProperty("_MixTiling2", properties);
            mixMetallic2Prop = FindProperty("_MixMetallic2", properties);
            mixSmoothness2Prop = FindProperty("_MixSmoothness2", properties);
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
            CoreUtils.SetKeyword(material, "_PROJECTIONSPACE_WORLD", material.GetFloat("_ProjectionSpace") > 0.5f);
            CoreUtils.SetKeyword(material, "_OVERLAY_LAYER_0", material.GetFloat("_OverlayLayer0") > 0.5f);
            CoreUtils.SetKeyword(material, "_PATTERN", material.GetFloat("_Pattern") > 0.5f);
            CoreUtils.SetKeyword(material, "_PATTERNSPACE_UV", material.GetFloat("_PatternProjection") > 0.5f);

            bool materialMix = material.GetFloat("_MaterialMix") > 0.5f;
            CoreUtils.SetKeyword(material, "_MATERIAL_MIX", materialMix);
            // Второй слой — только вместе с первым: комбинации «второй без первого»
            // не существует, и shader_feature компилирует три состояния, а не четыре.
            CoreUtils.SetKeyword(material, "_MATERIAL_MIX_2",
                materialMix && material.GetFloat("_MaterialMixTwo") > 0.5f);
            CoreUtils.SetKeyword(material, "_MIX_MASK_TEXTURE",
                material.GetFloat("_MixMaskFromTexture") > 0.5f);

            // _NORMALMAP доставляется принудительно, если назначена нормаль подмешиваемого
            // слоя. Причина: слой правит surfaceData.normalTS, а её ниже по коду читают
            // только ветки под _NORMALMAP — и в ForwardLit, и в сборке нормали для маски
            // наноса. Без этой строки материал без базовой карты нормалей, но с назначенной
            // Mix Normal, молча потерял бы рельеф мазка.
            //
            // Порядок важен: SetMaterialKeywords выше уже выставила keyword по наличию
            // _BumpMap, и мы её решение только расширяем, никогда не снимаем. Побочных
            // эффектов нет — пустой слот _BumpMap даёт дефолт "bump", то есть плоскую
            // нормаль, а пакетные пассы сэмплят её сами и ведут себя так же.
            if (material.GetTexture("_MixNormalMap1") != null || material.GetTexture("_MixNormalMap2") != null)
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP", true);
            }
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
                ? "Четыре отдельные карты, из каждой читается канал R: Metallic — R, Occlusion — R, Smoothness — R, Height — R. Формат BC4 (один канал)."
                : "Одна упакованная карта: R — Metallic, G — Occlusion, B — Height (микрорельеф под слоем наноса), A — Smoothness. Формат BC7.";
            var maskModeLabel = new GUIContent(maskMapSeparateProp.displayName, channelTooltip);
            materialEditor.ShaderProperty(maskMapSeparateProp, maskModeLabel);

            // Пространство проекции — режим материала, общий для градиента по высоте
            // и для будущих слоёв наноса/узора (.scratch/env-lit-layers/spec.md), поэтому
            // стоит здесь, рядом с режимом маски, а не внутри блока градиента, который
            // его сейчас единственный читает.
            var projectionSpaceLabel = new GUIContent(projectionSpaceProp.displayName,
                "World — непрерывность узора через стыки модулей (плиты пола читаются одной поверхностью). " +
                "Local — рисунок прибит к мешу и терпит перемещение объекта (башня из повторяющихся этажей).");
            materialEditor.ShaderProperty(projectionSpaceProp, projectionSpaceLabel);

            // Вторая ось проекции, отдельная от первой: та выбирает мировые или объектные
            // координаты, эта — планарную развёртку или собственную UV меша. Одна ручка
            // на узор и на маску-мазок сразу: материал в работе бывает либо модульным,
            // либо уникальным объектом, и выбор ложится на материал целиком.
            var patternProjectionLabel = new GUIContent(patternProjectionProp.displayName,
                "Planar XZ — рисунок непрерывен через стыки модулей, но «развёрнут» только сверху: " +
                "на вертикальных и промежуточных гранях тянется полосами. " +
                "Mesh UV — по развёртке меша, одинаково на любой ориентации (сколы краски и грязь на стенах), " +
                "но повторяется на каждом экземпляре модуля.");
            materialEditor.ShaderProperty(patternProjectionProp, patternProjectionLabel);
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
            DrawOverlayLayerBlock();
            DrawPatternBlock();
            DrawMaterialBlendBlock();
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

                // Высота показывается всегда, а не только при включённом слое наноса —
                // это тот самый процесс, ради которого существует раздельный режим: карты
                // подключаются со стора и проверяются в движке ДО того, как решено, нужен
                // ли нанос, а потом пакуются в Substance. Высота обязана себя вести
                // как остальные три канала.
                ENV_LitBlocks.BeginBox();
                materialEditor.TextureProperty(heightMapProp, "Height", false);
                ENV_LitTextureValidator.DrawTextureCheck(heightMapProp.textureValue, false, false, TextureImporterFormat.BC4);
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
                if (heightMapProp.textureValue != null) abandoned++;
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
                    heightMapProp.textureValue = null;
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
            materialEditor.ShaderProperty(gradientMinHeightProp, gradientMinHeightProp.displayName);
            materialEditor.ShaderProperty(gradientMaxHeightProp, gradientMaxHeightProp.displayName);
            // Альфа цвета — локальная сила подмеса, её читает шейдер (см. ApplyHeightGradient).
            materialEditor.ShaderProperty(gradientColor01Prop, gradientColor01Prop.displayName);
            materialEditor.ShaderProperty(gradientColor02Prop, gradientColor02Prop.displayName);
            materialEditor.ShaderProperty(gradientStrengthProp, gradientStrengthProp.displayName);
            ENV_LitBlocks.EndBox();
        }

        // Слой наноса — снег/пыль/грязь поверх основного материала (.scratch/env-lit-layers/
        // spec.md). Маска считается по нормали после карты нормалей и по высоте
        // микрорельефа — см. ComputeOverlayMask0 в ENV_LitInput.hlsl.
        private void DrawOverlayLayerBlock()
        {
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, new GUIContent("Overlay Layer"), overlayLayer0Prop)) return;

            ENV_LitBlocks.BeginBox();

            materialEditor.TextureProperty(overlayMap0Prop, "Overlay Albedo", false);
            ENV_LitTextureValidator.DrawTextureCheck(overlayMap0Prop.textureValue, true, false, null);

            // Альфа цвета здесь не используется — в отличие от градиента по высоте, где она
            // несёт локальную силу подмеса (см. ApplyHeightGradient), слою наноса такая ручка
            // не нужна. Сказано в тултипе, а не оставлено молчать: неработающая ручка
            // в пикере — та же ловушка, от которой предостерегает комментарий там же.
            var overlayColorLabel = new GUIContent(overlayColor0Prop.displayName,
                "Альфа не используется. Дефолт — sRGB 235, потолок дисциплины альбедо (§8), не белый.");
            materialEditor.ShaderProperty(overlayColor0Prop, overlayColorLabel);

            materialEditor.TextureProperty(overlayNormalMap0Prop, "Overlay Normal", false);
            ENV_LitTextureValidator.DrawTextureCheck(overlayNormalMap0Prop.textureValue, false, true, TextureImporterFormat.BC5);
            materialEditor.ShaderProperty(overlayNormalScale0Prop, overlayNormalScale0Prop.displayName);

            materialEditor.ShaderProperty(overlayTiling0Prop, overlayTiling0Prop.displayName);
            materialEditor.ShaderProperty(overlayMetallic0Prop, overlayMetallic0Prop.displayName);
            materialEditor.ShaderProperty(overlaySmoothness0Prop, overlaySmoothness0Prop.displayName);
            materialEditor.ShaderProperty(overlayCoverage0Prop, overlayCoverage0Prop.displayName);
            materialEditor.ShaderProperty(overlayEdgeSoftness0Prop, overlayEdgeSoftness0Prop.displayName);
            materialEditor.ShaderProperty(overlayHeightDepth0Prop, overlayHeightDepth0Prop.displayName);

            ENV_LitBlocks.EndBox();
        }

        // Художественный узор (.scratch/env-lit-layers/issues/03-pattern.md) — один на
        // материал, общий для слоя наноса (02) и смешивания материалов (04). Показывается
        // независимо от обоих: прятать его за чужой галочкой было бы ловушкой.
        //
        // Потребителей теперь два, но оба могут быть выключены — тогда узор не делает
        // ничего. Без строки ниже это выглядит как сломанная ручка: карта назначена,
        // сила на максимуме, в кадре ноль и ни намёка почему.
        private void DrawPatternBlock()
        {
            var patternHeader = new GUIContent("Pattern",
                "Общий узор границы: рвёт край слоя наноса и край мазка смешивания. " +
                "Одна выборка на оба, поэтому края обоих рвутся согласованно.");
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, patternHeader, patternProp)) return;

            ENV_LitBlocks.BeginBox();

            if (overlayLayer0Prop.floatValue < 0.5f && materialMixProp.floatValue < 0.5f)
                EditorGUILayout.HelpBox(
                    "Overlay Layer и Material Blending выключены — узор сейчас ни на что не влияет: он рвёт границу их слоёв, своего вклада у него нет.",
                    MessageType.Info);

            materialEditor.TextureProperty(patternMapProp, "Pattern Map", false);
            // sRGB выключен — три канала несут независимые данные, не цвет. BC7: три
            // независимых рисунка одноканальным форматом не сжать (как _MaskMap).
            ENV_LitTextureValidator.DrawTextureCheck(patternMapProp.textureValue, false, false, TextureImporterFormat.BC7);

            var channelLabel = new GUIContent(patternChannelProp.displayName,
                "Три рисунка в каналах одной текстуры. Смена канала меняет характер износа " +
                "(облачные пятна ↔ направленные подтёки) без перерисовки ассета.");
            materialEditor.ShaderProperty(patternChannelProp, channelLabel);

            var tilingLabel = new GUIContent(patternTilingProp.displayName,
                "Проекция задаётся ручкой Pattern / Mask Projection в Surface Options — она же " +
                "управляет маской мазка. Planar XZ + World даёт непрерывность узора через стыки " +
                "модулей; Mesh UV — рваный край одинаково на вертикалях и промежуточных гранях.");
            materialEditor.ShaderProperty(patternTilingProp, tilingLabel);

            var strengthLabel = new GUIContent(patternStrengthProp.displayName,
                "Узор только убавляет покрытие: подняв силу, компенсируй ползунком Coverage " +
                "у слоя наноса (у мазка — самой покраской). 0 — гладкая аналитическая " +
                "граница. Чем выше Coverage, тем меньше узор режет; на 1 не режет вовсе.");
            materialEditor.ShaderProperty(patternStrengthProp, strengthLabel);

            ENV_LitBlocks.EndBox();
        }

        // Смешивание материалов мазком (.scratch/env-lit-layers/issues/04-material-blending.md).
        // Вес приходит из вершинного цвета (кисть) или из маски-текстуры; форму границы
        // задаёт тот же узор, что рвёт край наноса.
        private void DrawMaterialBlendBlock()
        {
            var header = new GUIContent("Material Blending",
                "Второй и третий материал по весу из вершинного цвета или маски-текстуры. " +
                "Границу рвёт узор из блока Pattern выше.");
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, header, materialMixProp)) return;

            ENV_LitBlocks.BeginBox();

            // Симметрично подсказке в блоке узора: без узора форму границе взять неоткуда,
            // и мазок ляжет жёстким пятном по треугольникам. Без этой строки выглядит
            // как сломанный ползунок мягкости.
            // Дефолт Blend Coverage — ноль, и это не описка: без него включение галочки
            // утопило бы непокрашенный объект во втором материале целиком (меш без вершинных
            // цветов отдаёт белый). Но молчать нельзя — иначе выглядит как сломанная галочка.
            if (mixCoverageProp.floatValue < 0.001f)
                EditorGUILayout.HelpBox(
                    "Blend Coverage на нуле — мазок не проявлен. Это защита: меш без вершинных цветов отдаёт белый, и без неё объект целиком ушёл бы во второй материал. Подними ползунок после покраски.",
                    MessageType.Info);

            if (patternProp.floatValue < 0.5f)
                EditorGUILayout.HelpBox(
                    "Pattern выключен — граница мазка будет жёсткой по треугольникам: форму ей задаёт узор, а мягчит только Blend Edge Softness ниже.",
                    MessageType.Info);

            var sourceLabel = new GUIContent(mixMaskFromTextureProp.displayName,
                "Вершинный цвет — для уникальных объектов: кисть клонирует меш, и мазок " +
                "принадлежит экземпляру. Текстура — для модулей: на стыке двух модулей " +
                "вершины принадлежат разным объектам, и совпадение мазка ничем не гарантировано.");
            materialEditor.ShaderProperty(mixMaskFromTextureProp, sourceLabel);

            // Слот и тайлинг маски — только в текстурном режиме: в вершинном они ничего
            // не делают, а видимая неработающая ручка это та же ловушка, от которой
            // предостерегает тултип цвета наноса.
            if (mixMaskFromTextureProp.floatValue > 0.5f)
            {
                materialEditor.TextureProperty(mixMaskMapProp, "Blend Mask", false);
                // sRGB выключен — каналы несут вес, а не цвет. BC7: несколько независимых
                // каналов одноканальным форматом не сжать (как у _MaskMap и _PatternMap).
                ENV_LitTextureValidator.DrawTextureCheck(mixMaskMapProp.textureValue, false, false, TextureImporterFormat.BC7);

                var maskTilingLabel = new GUIContent(mixMaskTilingProp.displayName,
                    "Проекция — общая с узором, Pattern / Mask Projection в Surface Options. " +
                    "В планарном режиме отдельной ручки смещения нет: место мазка задаётся тайлингом.");
                materialEditor.ShaderProperty(mixMaskTilingProp, maskTilingLabel);
            }

            var coverageLabel = new GUIContent(mixCoverageProp.displayName,
                "Множитель веса покраски. 0 — мазка нет вовсе (дефолт: меш без вершинных цветов " +
                "отдаёт белый, и без этой защиты объект ушёл бы во второй материал целиком). " +
                "1 — вес как покрашен. Выше 1 — полутона маски дотягиваются до полного покрытия, " +
                "этим убираются проблески базы сквозь мазок.");
            materialEditor.ShaderProperty(mixCoverageProp, coverageLabel);

            var edgeLabel = new GUIContent(mixEdgeSoftnessProp.displayName,
                "Один на оба слоя — края обоих мазков должны рваться согласованно. Мягчит край, " +
                "а не поднимает уровень: чтобы убрать проблески базы, крути Blend Coverage выше. " +
                "Форму границы задаёт узор; без него граница жёсткая, и мягчит её только этот ползунок.");
            materialEditor.ShaderProperty(mixEdgeSoftnessProp, edgeLabel);

            ENV_LitBlocks.EndBox();

            DrawMixLayer(1, mixMap1Prop, mixColor1Prop, mixNormalMap1Prop, mixNormalScale1Prop,
                mixTiling1Prop, mixMetallic1Prop, mixSmoothness1Prop);

            if (ENV_LitBlocks.DrawToggleHeader(materialEditor,
                new GUIContent("Second Blend Layer",
                    "Второй подмешиваемый слой ложится ПОВЕРХ первого — порядок фиксирован. " +
                    "Кисть держит сумму весов в пределах единицы, поэтому на практике они почти не перекрываются."),
                materialMixTwoProp))
            {
                DrawMixLayer(2, mixMap2Prop, mixColor2Prop, mixNormalMap2Prop, mixNormalScale2Prop,
                    mixTiling2Prop, mixMetallic2Prop, mixSmoothness2Prop);
            }
        }

        // Один подмешиваемый слой. Общий метод на оба, а не два похожих: тикет требует,
        // чтобы слои вели себя одинаково, и это касается инспектора тоже.
        private void DrawMixLayer(int index, MaterialProperty mapProp, MaterialProperty colorProp,
            MaterialProperty normalMapProp, MaterialProperty normalScaleProp,
            MaterialProperty tilingProp, MaterialProperty metallicProp, MaterialProperty smoothnessProp)
        {
            ENV_LitBlocks.BeginBox();

            string channel = index == 1 ? "G" : "B";
            EditorGUILayout.LabelField($"Layer {index}  ·  канал {channel}", EditorStyles.miniBoldLabel);

            materialEditor.TextureProperty(mapProp, "Albedo", false);
            // sRGB обязателен — это цвет. Формат не проверяем: у альбедо он зависит
            // от материала, однозначного ожидания нет (как у базовой карты).
            ENV_LitTextureValidator.DrawTextureCheck(mapProp.textureValue, true, false, null);

            var colorLabel = new GUIContent(colorProp.displayName,
                "Альфа не используется. Дефолт — sRGB 235, потолок дисциплины альбедо (§8), не белый.");
            materialEditor.ShaderProperty(colorProp, colorLabel);

            // Подпись у слота и у ползунка своя. Тултип общий — он про одно и то же условие,
            // — но текст брать у нужного свойства: с одним GUIContent на оба ползунок силы
            // получал имя карты, и две строки подряд читались одинаково.
            const string normalTooltip =
                "Работает только при включённой карте нормалей материала — инспектор включает " +
                "её сам при назначении этого слота, но базовую карту всё равно стоит заполнить.";

            materialEditor.TextureProperty(normalMapProp, normalMapProp.displayName, false);
            ENV_LitTextureValidator.DrawTextureCheck(normalMapProp.textureValue, false, true, TextureImporterFormat.BC5);
            materialEditor.ShaderProperty(normalScaleProp,
                new GUIContent(normalScaleProp.displayName, normalTooltip));

            var tilingLabel = new GUIContent(tilingProp.displayName,
                "Множитель поверх тайлинга базы (Tiling в блоке Albedo), а не самостоятельный масштаб: " +
                "слой идёт по той же развёртке, что и основной материал.");
            materialEditor.ShaderProperty(tilingProp, tilingLabel);

            materialEditor.ShaderProperty(metallicProp, metallicProp.displayName);
            materialEditor.ShaderProperty(smoothnessProp, smoothnessProp.displayName);

            ENV_LitBlocks.EndBox();
        }
    }
}
