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
        private const float BlendPatternLabelIndent = 48f;

        private MaterialProperty maskMapSeparateProp;
        private MaterialProperty maskMapProp;
        private MaterialProperty metallicMapProp;
        private MaterialProperty occlusionMapProp;
        private MaterialProperty smoothnessMapProp;
        private MaterialProperty heightMapProp;
        private MaterialProperty heightStrengthProp;
        private MaterialProperty metallicProp;
        private MaterialProperty smoothnessProp;
        private MaterialProperty occlusionStrengthProp;
        private MaterialProperty bumpScaleProp;
        private MaterialProperty bumpMapProp;

        private MaterialProperty albedoAdjustProp;
        private MaterialProperty hueShiftProp;
        private MaterialProperty contrastProp;
        private MaterialProperty brightnessProp;

        private MaterialProperty projectionSpaceProp;

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
        private MaterialProperty overlayMetallic0Prop;
        private MaterialProperty overlaySmoothness0Prop;
        private MaterialProperty overlayCoverage0Prop;
        private MaterialProperty overlayEdgeSoftness0Prop;
        private MaterialProperty overlayHeightMap0Prop;
        private MaterialProperty overlayHeightStrength0Prop;
        private MaterialProperty overlayOcclusionStrength0Prop;
        private MaterialProperty overlayMaskMap0Prop;
        private MaterialProperty overlayMetallicMap0Prop;
        private MaterialProperty overlayOcclusionMap0Prop;
        private MaterialProperty overlaySmoothnessMap0Prop;

        private MaterialProperty patternProp;
        private MaterialProperty patternMapProp;
        private MaterialProperty patternSpace0Prop;
        private MaterialProperty patternTiling0Prop;
        private MaterialProperty patternRotation0Prop;
        private MaterialProperty patternChannel0Prop;
        private MaterialProperty patternStrength0Prop;
        private MaterialProperty patternBias0Prop;

        private MaterialProperty materialMixProp;
        private MaterialProperty materialMixTwoProp;

        private MaterialProperty mixMap1Prop;
        private MaterialProperty mixColor1Prop;
        private MaterialProperty mixNormalMap1Prop;
        private MaterialProperty mixNormalScale1Prop;
        private MaterialProperty mixMetallic1Prop;
        private MaterialProperty mixSmoothness1Prop;
        private MaterialProperty mixMaskFromTexture1Prop;
        private MaterialProperty mixCoverage1Prop;
        private MaterialProperty mixPatternSpace1Prop;
        private MaterialProperty mixPatternTiling1Prop;
        private MaterialProperty mixPatternRotation1Prop;
        private MaterialProperty mixPatternChannel1Prop;
        private MaterialProperty mixPatternStrength1Prop;
        private MaterialProperty mixPatternBias1Prop;
        private MaterialProperty mixEdgeSoftness1Prop;
        private MaterialProperty mixOcclusionStrength1Prop;
        private MaterialProperty mixMaskMap1Prop;
        private MaterialProperty mixMetallicMap1Prop;
        private MaterialProperty mixOcclusionMap1Prop;
        private MaterialProperty mixSmoothnessMap1Prop;

        private MaterialProperty mixMap2Prop;
        private MaterialProperty mixColor2Prop;
        private MaterialProperty mixNormalMap2Prop;
        private MaterialProperty mixNormalScale2Prop;
        private MaterialProperty mixMetallic2Prop;
        private MaterialProperty mixSmoothness2Prop;
        private MaterialProperty mixMaskFromTexture2Prop;
        private MaterialProperty mixCoverage2Prop;
        private MaterialProperty mixPatternSpace2Prop;
        private MaterialProperty mixPatternTiling2Prop;
        private MaterialProperty mixPatternRotation2Prop;
        private MaterialProperty mixPatternChannel2Prop;
        private MaterialProperty mixPatternStrength2Prop;
        private MaterialProperty mixPatternBias2Prop;
        private MaterialProperty mixEdgeSoftness2Prop;
        private MaterialProperty mixOcclusionStrength2Prop;
        private MaterialProperty mixMaskMap2Prop;
        private MaterialProperty mixMetallicMap2Prop;
        private MaterialProperty mixOcclusionMap2Prop;
        private MaterialProperty mixSmoothnessMap2Prop;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            maskMapSeparateProp = FindProperty("_MaskMapSeparate", properties);
            maskMapProp = FindProperty("_MaskMap", properties);
            metallicMapProp = FindProperty("_MetallicMap", properties);
            occlusionMapProp = FindProperty("_OcclusionMap", properties);
            smoothnessMapProp = FindProperty("_SmoothnessMap", properties);
            heightMapProp = FindProperty("_HeightMap", properties);
            heightStrengthProp = FindProperty("_HeightStrength", properties);
            metallicProp = FindProperty("_Metallic", properties);
            smoothnessProp = FindProperty("_Smoothness", properties);
            occlusionStrengthProp = FindProperty("_OcclusionStrength", properties);
            bumpScaleProp = FindProperty("_BumpScale", properties);
            bumpMapProp = FindProperty("_BumpMap", properties);

            albedoAdjustProp = FindProperty("_AlbedoAdjust", properties);
            hueShiftProp = FindProperty("_HueShift", properties);
            contrastProp = FindProperty("_Contrast", properties);
            brightnessProp = FindProperty("_Brightness", properties);

            projectionSpaceProp = FindProperty("_ProjectionSpace", properties);

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
            overlayMetallic0Prop = FindProperty("_OverlayMetallic0", properties);
            overlaySmoothness0Prop = FindProperty("_OverlaySmoothness0", properties);
            overlayCoverage0Prop = FindProperty("_OverlayCoverage0", properties);
            overlayEdgeSoftness0Prop = FindProperty("_OverlayEdgeSoftness0", properties);
            overlayHeightMap0Prop = FindProperty("_OverlayHeightMap0", properties);
            overlayHeightStrength0Prop = FindProperty("_OverlayHeightStrength0", properties);
            overlayOcclusionStrength0Prop = FindProperty("_OverlayOcclusionStrength0", properties);
            overlayMaskMap0Prop = FindProperty("_OverlayMaskMap0", properties);
            overlayMetallicMap0Prop = FindProperty("_OverlayMetallicMap0", properties);
            overlayOcclusionMap0Prop = FindProperty("_OverlayOcclusionMap0", properties);
            overlaySmoothnessMap0Prop = FindProperty("_OverlaySmoothnessMap0", properties);

            patternProp = FindProperty("_Pattern", properties);
            patternMapProp = FindProperty("_PatternMap", properties);
            patternSpace0Prop = FindProperty("_PatternSpace0", properties);
            patternTiling0Prop = FindProperty("_PatternTiling0", properties);
            patternRotation0Prop = FindProperty("_PatternRotation0", properties);
            patternChannel0Prop = FindProperty("_PatternChannel0", properties);
            patternStrength0Prop = FindProperty("_PatternStrength0", properties);
            patternBias0Prop = FindProperty("_PatternBias0", properties);

            materialMixProp = FindProperty("_MaterialMix", properties);
            materialMixTwoProp = FindProperty("_MaterialMixTwo", properties);

            mixMap1Prop = FindProperty("_MixMap1", properties);
            mixColor1Prop = FindProperty("_MixColor1", properties);
            mixNormalMap1Prop = FindProperty("_MixNormalMap1", properties);
            mixNormalScale1Prop = FindProperty("_MixNormalScale1", properties);
            mixMetallic1Prop = FindProperty("_MixMetallic1", properties);
            mixSmoothness1Prop = FindProperty("_MixSmoothness1", properties);
            mixMaskFromTexture1Prop = FindProperty("_MixMaskFromTexture1", properties);
            mixCoverage1Prop = FindProperty("_MixCoverage1", properties);
            mixPatternSpace1Prop = FindProperty("_MixPatternSpace1", properties);
            mixPatternTiling1Prop = FindProperty("_MixPatternTiling1", properties);
            mixPatternRotation1Prop = FindProperty("_MixPatternRotation1", properties);
            mixPatternChannel1Prop = FindProperty("_MixPatternChannel1", properties);
            mixPatternStrength1Prop = FindProperty("_MixPatternStrength1", properties);
            mixPatternBias1Prop = FindProperty("_MixPatternBias1", properties);
            mixEdgeSoftness1Prop = FindProperty("_MixEdgeSoftness1", properties);
            mixOcclusionStrength1Prop = FindProperty("_MixOcclusionStrength1", properties);
            mixMaskMap1Prop = FindProperty("_MixMaskMap1", properties);
            mixMetallicMap1Prop = FindProperty("_MixMetallicMap1", properties);
            mixOcclusionMap1Prop = FindProperty("_MixOcclusionMap1", properties);
            mixSmoothnessMap1Prop = FindProperty("_MixSmoothnessMap1", properties);

            mixMap2Prop = FindProperty("_MixMap2", properties);
            mixColor2Prop = FindProperty("_MixColor2", properties);
            mixNormalMap2Prop = FindProperty("_MixNormalMap2", properties);
            mixNormalScale2Prop = FindProperty("_MixNormalScale2", properties);
            mixMetallic2Prop = FindProperty("_MixMetallic2", properties);
            mixSmoothness2Prop = FindProperty("_MixSmoothness2", properties);
            mixMaskFromTexture2Prop = FindProperty("_MixMaskFromTexture2", properties);
            mixCoverage2Prop = FindProperty("_MixCoverage2", properties);
            mixPatternSpace2Prop = FindProperty("_MixPatternSpace2", properties);
            mixPatternTiling2Prop = FindProperty("_MixPatternTiling2", properties);
            mixPatternRotation2Prop = FindProperty("_MixPatternRotation2", properties);
            mixPatternChannel2Prop = FindProperty("_MixPatternChannel2", properties);
            mixPatternStrength2Prop = FindProperty("_MixPatternStrength2", properties);
            mixPatternBias2Prop = FindProperty("_MixPatternBias2", properties);
            mixEdgeSoftness2Prop = FindProperty("_MixEdgeSoftness2", properties);
            mixOcclusionStrength2Prop = FindProperty("_MixOcclusionStrength2", properties);
            mixMaskMap2Prop = FindProperty("_MixMaskMap2", properties);
            mixMetallicMap2Prop = FindProperty("_MixMetallicMap2", properties);
            mixOcclusionMap2Prop = FindProperty("_MixOcclusionMap2", properties);
            mixSmoothnessMap2Prop = FindProperty("_MixSmoothnessMap2", properties);
        }

        // Эталонный материал кнопки сброса (ENV_LitBlocks) — HideAndDontSave, живёт до тех
        // пор, пока открыт инспектор этого шейдера. Закрытие инспектора — единственный
        // надёжный момент его освободить.
        public override void OnClosed(Material material)
        {
            ENV_LitBlocks.ReleaseReferenceMaterial();
            base.OnClosed(material);
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

            // Проекция блока наноса — трёхпозиционная, keyword'ом (тикет 06): 0 — Planar XZ
            // (обе выключены), 1 — Mesh UV, 2 — Triplanar.
            SetProjectionKeywords(material, "_PatternSpace0", "_PATTERNSPACE0_UV", "_PATTERNSPACE0_TRIPLANAR");
            bool materialMix = material.GetFloat("_MaterialMix") > 0.5f;
            bool materialMixTwo = materialMix && material.GetFloat("_MaterialMixTwo") > 0.5f;
            CoreUtils.SetKeyword(material, "_MATERIAL_MIX", materialMix);
            // Второй слой — только вместе с первым: комбинации «второй без первого»
            // не существует, и shader_feature компилирует три состояния, а не четыре.
            CoreUtils.SetKeyword(material, "_MATERIAL_MIX_2", materialMixTwo);
            CoreUtils.SetKeyword(material, "_MIX_MASK_TEXTURE_1",
                materialMix && material.GetFloat("_MixMaskFromTexture1") > 0.5f);
            CoreUtils.SetKeyword(material, "_MIX_MASK_TEXTURE_2",
                materialMixTwo && material.GetFloat("_MixMaskFromTexture2") > 0.5f);
            SetProjectionKeywords(material, "_MixPatternSpace1",
                "_MIXPATTERNSPACE1_UV", "_MIXPATTERNSPACE1_TRIPLANAR", materialMix);
            SetProjectionKeywords(material, "_MixPatternSpace2",
                "_MIXPATTERNSPACE2_UV", "_MIXPATTERNSPACE2_TRIPLANAR", materialMixTwo);

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

            // Комплект материальных карт слоя (тикет 07) — keyword выводится из наличия
            // текстуры АКТИВНОГО режима _MASKMAP_SEPARATE, вручную не ставится (та же идиома,
            // что у _NORMALMAP выше). Упакованная карта, оставшаяся висеть после переключения
            // в раздельный режим, keyword не включает — ей нечего читать в этом режиме.
            bool separate = material.GetFloat("_MaskMapSeparate") > 0.5f;
            CoreUtils.SetKeyword(material, "_OVERLAY_MAPS_0", HasLayerMaps(material, separate,
                "_OverlayMaskMap0", "_OverlayMetallicMap0", "_OverlayOcclusionMap0", "_OverlaySmoothnessMap0"));
            CoreUtils.SetKeyword(material, "_MIX_MAPS_1", materialMix && HasLayerMaps(material, separate,
                "_MixMaskMap1", "_MixMetallicMap1", "_MixOcclusionMap1", "_MixSmoothnessMap1"));
            CoreUtils.SetKeyword(material, "_MIX_MAPS_2", materialMixTwo && HasLayerMaps(material, separate,
                "_MixMaskMap2", "_MixMetallicMap2", "_MixOcclusionMap2", "_MixSmoothnessMap2"));

            // Бамп из высоты базы (тикет 02) — keyword из наличия текстуры активного режима,
            // не из значения ползунка Height Strength: иначе перетаскивание слайдера через
            // ноль дёргает перекомпиляцию варианта. _NORMALMAP доставляется принудительно
            // по той же идиоме, что у _MixNormalMap выше: surfaceData.normalTS читают только
            // ветки под _NORMALMAP, и без базовой карты нормалей материал с одной картой
            // высоты молча не получил бы рельефа вовсе. Мы только расширяем решение
            // SetMaterialKeywords, никогда не снимаем — пустой _BumpMap даёт дефолт "bump".
            bool hasHeight = separate ? material.GetTexture("_HeightMap") != null : material.GetTexture("_MaskMap") != null;
            CoreUtils.SetKeyword(material, "_HEIGHT_BUMP", hasHeight);
            if (hasHeight)
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP", true);
            }

            // Высота слоя наноса (тикет 2-03) — та же идиома: keyword из наличия текстуры
            // активного режима, не из значения ползунка Height Strength (иначе протаскивание
            // слайдера через ноль дёргает перекомпиляцию). Принудительный _NORMALMAP здесь
            // не нужен: GetOverlayNormalWS0 вызывается безусловно и не гейтится им.
            bool hasOverlayHeight = separate
                ? material.GetTexture("_OverlayHeightMap0") != null
                : material.GetTexture("_OverlayMaskMap0") != null;
            CoreUtils.SetKeyword(material, "_OVERLAY_HEIGHT_0", hasOverlayHeight);
        }

        private static bool HasLayerMaps(Material material, bool separate,
            string maskMapName, string metallicMapName, string occlusionMapName, string smoothnessMapName)
        {
            return separate
                ? material.GetTexture(metallicMapName) != null
                  || material.GetTexture(occlusionMapName) != null
                  || material.GetTexture(smoothnessMapName) != null
                : material.GetTexture(maskMapName) != null;
        }

        // Трёхпозиционный Enum-переключатель (0 Planar XZ / 1 Mesh UV / 2 Triplanar) → пара
        // keyword'ов трёхпозиционной прагмы `_ A B` (ENV_LitKeywords.hlsl). 0 не ставит ни
        // одного — планарная проекция это отсутствие обоих, как и раньше у _PatternProjection.
        private static void SetProjectionKeywords(Material material, string enumPropertyName,
            string uvKeyword, string triplanarKeyword, bool active = true)
        {
            float value = material.GetFloat(enumPropertyName);
            CoreUtils.SetKeyword(material, uvKeyword, active && value > 0.5f && value < 1.5f);
            CoreUtils.SetKeyword(material, triplanarKeyword, active && value > 1.5f);
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
            //
            // Режим один на материал (тикет 07) — переключатель действует на базу и на все
            // слои сразу (нанос, оба слоя смешивания), а не только на карту выше.
            string channelTooltip = maskMapSeparateProp.floatValue > 0.5f
                ? "Действует на базу и на все слои сразу (нанос, оба слоя смешивания). Четыре отдельные карты, из каждой читается канал R: Metallic — R, Occlusion — R, Smoothness — R, Height — R. Формат BC4 (один канал)."
                : "Действует на базу и на все слои сразу (нанос, оба слоя смешивания). Одна упакованная карта: R — Metallic, G — Occlusion, B — Height (микрорельеф под базой и под слоем наноса), A — Smoothness. Формат BC7.";
            var maskModeLabel = new GUIContent(
                "Separate Metallic/AO/Height/Smoothness Maps", channelTooltip);
            materialEditor.ShaderProperty(maskMapSeparateProp, maskModeLabel);

            // Пространство проекции — режим материала, общий для градиента по высоте
            // и для будущих слоёв наноса/узора (.scratch/env-lit-layers/spec.md), поэтому
            // стоит здесь, рядом с режимом маски, а не внутри блока градиента, который
            // его сейчас единственный читает.
            var projectionSpaceLabel = new GUIContent(projectionSpaceProp.displayName,
                "World — непрерывность через стыки модулей (плиты пола читаются одной поверхностью), " +
                "и это единственный режим, в котором триплanar-проекция карты шума остаётся бесшовной. " +
                "Local — рисунок прибит к мешу и терпит перемещение объекта (башня из повторяющихся этажей). " +
                "Зависимые блоки: Height Gradient, Top Projection Layer, оба блока RGB Noise.");
            materialEditor.ShaderProperty(projectionSpaceProp, projectionSpaceLabel);
        }

        // Все карты базы живут в одном Material Maps. Разделители внутри бокса связывают
        // карту с её силой и одновременно отделяют её от следующей карты.
        // Базовый DrawSurfaceInputs не зовём: он рисует альбедо через TexturePropertySingleLine,
        // то есть мелкой иконкой слева, и набор карт получался разнородным.
        public override void DrawSurfaceInputs(Material material)
        {
            DrawBaseMaterialMaps();
            ENV_LitBlocks.Separator();

            DrawEmissionBlock(material);
            DrawAlbedoAdjustBlock();
            DrawHeightGradientBlock();
            DrawOverlayLayerBlock();
            DrawPatternBlock();
            DrawMaterialBlendBlock();
        }

        private void DrawBaseMaterialMaps()
        {
            bool separate = maskMapSeparateProp.floatValue > 0.5f;

            ENV_LitBlocks.BeginBox();
            EditorGUILayout.LabelField("Material Maps", EditorStyles.miniBoldLabel);

            ENV_LitBlocks.ColorTexture(materialEditor, baseMapProp, baseColorProp,
                new GUIContent("Albedo"), new GUIContent(baseColorProp.displayName));
            // sRGB для альбедо обязателен — выключенный отдаёт цвет как линейные данные
            // и материал едет с неверной яркостью. Формат не проверяем: у альбедо он зависит
            // от материала (BC1 без альфы, BC7 при cutout), однозначного ожидания нет.
            ENV_LitTextureValidator.DrawTextureCheck(baseMapProp.textureValue, true, false, null);

            // Общий тайлинг PBR-набора (альбедо + маска + нормаль) — одно _BaseMap_ST,
            // без отдельных ручек на каждой карте. См. спек, "Не делаем: _ST на нормали и маске".
            ENV_LitBlocks.Separator();
            if (separate)
            {
                ENV_LitBlocks.TextureSlot(materialEditor, metallicMapProp, "Metallic", false);
                ENV_LitTextureValidator.DrawTextureCheck(metallicMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, metallicProp, "Metallic Strength");

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, smoothnessMapProp, "Smoothness", false);
                ENV_LitTextureValidator.DrawTextureCheck(smoothnessMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, smoothnessProp, "Smoothness Strength");

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, occlusionMapProp, "Occlusion", false);
                ENV_LitTextureValidator.DrawTextureCheck(occlusionMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, occlusionStrengthProp, "Occlusion Strength");

                DrawBaseNormalSection();

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, heightMapProp, "Height", false);
                ENV_LitTextureValidator.DrawTextureCheck(heightMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, heightStrengthProp, HeightStrengthLabel());
            }
            else
            {
                EditorGUILayout.LabelField("Packed Maps", EditorStyles.miniBoldLabel);
                ENV_LitBlocks.MutedMiniLabel("R: Metallic · G: AO · B: Height · A: Smoothness");
                ENV_LitBlocks.TextureSlot(materialEditor, maskMapProp, string.Empty, false);
                ENV_LitTextureValidator.DrawTextureCheck(maskMapProp.textureValue, false, false, TextureImporterFormat.BC7);

                ENV_LitBlocks.Property(materialEditor, metallicProp, "Metallic Strength");
                ENV_LitBlocks.Property(materialEditor, smoothnessProp, "Smoothness Strength");
                ENV_LitBlocks.Property(materialEditor, occlusionStrengthProp, "Occlusion Strength");
                ENV_LitBlocks.Property(materialEditor, heightStrengthProp, HeightStrengthLabel());

                DrawBaseNormalSection();
            }

            ENV_LitBlocks.EndBox();
            DrawAbandonedMaskReferencesWarning(separate);
        }

        private void DrawBaseNormalSection()
        {
            ENV_LitBlocks.Separator();
            ENV_LitBlocks.TextureSlotWithoutCompatibilityWarning(materialEditor, bumpMapProp, "Normal Map");
            ENV_LitTextureValidator.DrawTextureCheck(bumpMapProp.textureValue, false, true, TextureImporterFormat.BC5);
            ENV_LitBlocks.Property(materialEditor, bumpScaleProp, "Normal Strength");
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

            ENV_LitBlocks.ColorTexture(materialEditor, emissionMapProp, emissionColorProp,
                new GUIContent("Emission Map"), new GUIContent(emissionColorProp.displayName));
            // Флаги GI — не MaterialProperty, кнопки сброса не касаются.
            materialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);

            ENV_LitBlocks.EndBox();
        }

        // Общий тултип ручки силы бампа из высоты (тикет 02) — читается канал B упакованной
        // карты либо слот Height раздельного режима, 0 это плоско, рельеф требует включённой
        // карты нормалей (инспектор включает её сам при назначенной карте высоты).
        private static GUIContent HeightStrengthLabel()
        {
            return new GUIContent("Height Strength",
                "Рисует рельеф поверхности бампом из высоты, без смещения геометрии. Источник — " +
                "канал B Packed Maps либо слот Height раздельного режима. 0 — плоско. " +
                "Требует включённой карты нормалей — инспектор включает её сам при назначенной " +
                "карте высоты.");
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
                if (overlayMaskMap0Prop.textureValue != null) abandoned++;
                if (mixMaskMap1Prop.textureValue != null) abandoned++;
                if (mixMaskMap2Prop.textureValue != null) abandoned++;
            }
            else
            {
                if (metallicMapProp.textureValue != null) abandoned++;
                if (occlusionMapProp.textureValue != null) abandoned++;
                if (smoothnessMapProp.textureValue != null) abandoned++;
                if (heightMapProp.textureValue != null) abandoned++;
                if (overlayHeightMap0Prop.textureValue != null) abandoned++;
                if (overlayMetallicMap0Prop.textureValue != null) abandoned++;
                if (overlayOcclusionMap0Prop.textureValue != null) abandoned++;
                if (overlaySmoothnessMap0Prop.textureValue != null) abandoned++;
                if (mixMetallicMap1Prop.textureValue != null) abandoned++;
                if (mixOcclusionMap1Prop.textureValue != null) abandoned++;
                if (mixSmoothnessMap1Prop.textureValue != null) abandoned++;
                if (mixMetallicMap2Prop.textureValue != null) abandoned++;
                if (mixOcclusionMap2Prop.textureValue != null) abandoned++;
                if (mixSmoothnessMap2Prop.textureValue != null) abandoned++;
            }

            if (abandoned == 0) return;

            string textureWord = abandoned == 1 ? "texture" : "textures";
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(
                $"Inactive mode still references {abandoned} {textureWord}.",
                MessageType.Info);
            if (GUILayout.Button(
                new GUIContent("Clear",
                    "Удалить только ссылки на текстуры неактивного режима Packed/Separate."),
                GUILayout.Width(80), GUILayout.Height(38)))
            {
                if (separateActive)
                {
                    maskMapProp.textureValue = null;
                    overlayMaskMap0Prop.textureValue = null;
                    mixMaskMap1Prop.textureValue = null;
                    mixMaskMap2Prop.textureValue = null;
                }
                else
                {
                    metallicMapProp.textureValue = null;
                    occlusionMapProp.textureValue = null;
                    smoothnessMapProp.textureValue = null;
                    heightMapProp.textureValue = null;
                    overlayHeightMap0Prop.textureValue = null;
                    overlayMetallicMap0Prop.textureValue = null;
                    overlayOcclusionMap0Prop.textureValue = null;
                    overlaySmoothnessMap0Prop.textureValue = null;
                    mixMetallicMap1Prop.textureValue = null;
                    mixOcclusionMap1Prop.textureValue = null;
                    mixSmoothnessMap1Prop.textureValue = null;
                    mixMetallicMap2Prop.textureValue = null;
                    mixOcclusionMap2Prop.textureValue = null;
                    mixSmoothnessMap2Prop.textureValue = null;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAlbedoAdjustBlock()
        {
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, new GUIContent("Albedo Adjust"), albedoAdjustProp)) return;

            ENV_LitBlocks.BeginBox();
            ENV_LitBlocks.Property(materialEditor, hueShiftProp, hueShiftProp.displayName);
            ENV_LitBlocks.Property(materialEditor, contrastProp, contrastProp.displayName);
            ENV_LitBlocks.Property(materialEditor, brightnessProp, brightnessProp.displayName);
            ENV_LitBlocks.EndBox();
        }

        private void DrawHeightGradientBlock()
        {
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, new GUIContent("Height Gradient"), heightGradientProp)) return;

            ENV_LitBlocks.BeginBox();
            ENV_LitBlocks.Property(materialEditor, gradientMinHeightProp, gradientMinHeightProp.displayName);
            ENV_LitBlocks.Property(materialEditor, gradientMaxHeightProp, gradientMaxHeightProp.displayName);
            // Альфа цвета — локальная сила подмеса, её читает шейдер (см. ApplyHeightGradient).
            ENV_LitBlocks.Property(materialEditor, gradientColor01Prop, gradientColor01Prop.displayName);
            ENV_LitBlocks.Property(materialEditor, gradientColor02Prop, gradientColor02Prop.displayName);
            ENV_LitBlocks.Property(materialEditor, gradientStrengthProp, gradientStrengthProp.displayName);
            ENV_LitBlocks.EndBox();
        }

        // Слой наноса — снег/пыль/грязь поверх основного материала (.scratch/env-lit-layers/
        // spec.md). Маска считается по нормали после карты нормалей и по высоте
        // микрорельефа — см. ComputeOverlayMask0 в ENV_LitInput.hlsl.
        private void DrawOverlayLayerBlock()
        {
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, new GUIContent("Top Projection Layer"), overlayLayer0Prop)) return;

            bool separate = maskMapSeparateProp.floatValue > 0.5f;

            ENV_LitBlocks.BeginBox();
            EditorGUILayout.LabelField("Material Maps", EditorStyles.miniBoldLabel);

            var overlayColorLabel = new GUIContent(overlayColor0Prop.displayName,
                "Альфа не используется. Дефолт — sRGB 235, потолок дисциплины альбедо (§8), не белый.");
            ENV_LitBlocks.ColorTexture(materialEditor, overlayMap0Prop, overlayColor0Prop,
                new GUIContent("Albedo Top"), overlayColorLabel);
            ENV_LitTextureValidator.DrawTextureCheck(overlayMap0Prop.textureValue, true, false, null);

            var metallicLabel = new GUIContent("Metallic Strength",
                "Множитель поверх карты ниже, не переключатель — пустая карта (\"white\") отдаёт " +
                "этот ползунок как есть, заполненная умножается на него.");
            var smoothnessLabel = new GUIContent("Smoothness Strength",
                "Тот же множитель, что у Metallic выше.");
            var occlusionLabel = new GUIContent("Occlusion Strength",
                "Своя сила затенения слоя — AO базы описывает швы кладки, под сплошным снегом " +
                "он не к месту. Без карты комплекта затенения у слоя нет вовсе.");
            var heightStrengthLabel = new GUIContent("Height Strength",
                "Толщина слоя — бампом из карты Height Top, без смещения геометрии. Та же ручка " +
                "задаёт, насколько слой затекает в рельеф ОСНОВАНИЯ (по базовой карте высоты): " +
                "до 1 набирается затекание, выше растёт только толщина. 0 — слой ложится плёнкой.");

            ENV_LitBlocks.Separator();
            if (separate)
            {
                ENV_LitBlocks.TextureSlot(materialEditor, overlayMetallicMap0Prop, "Metallic Top", false);
                ENV_LitTextureValidator.DrawTextureCheck(overlayMetallicMap0Prop.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, overlayMetallic0Prop, metallicLabel);

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, overlaySmoothnessMap0Prop, "Smoothness Top", false);
                ENV_LitTextureValidator.DrawTextureCheck(overlaySmoothnessMap0Prop.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, overlaySmoothness0Prop, smoothnessLabel);

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, overlayOcclusionMap0Prop, "Occlusion Top", false);
                ENV_LitTextureValidator.DrawTextureCheck(overlayOcclusionMap0Prop.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, overlayOcclusionStrength0Prop, occlusionLabel);

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, overlayNormalMap0Prop, "Normal Top", false);
                ENV_LitTextureValidator.DrawTextureCheck(overlayNormalMap0Prop.textureValue, false, true, TextureImporterFormat.BC5);
                ENV_LitBlocks.Property(materialEditor, overlayNormalScale0Prop, "Normal Strength");

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, overlayHeightMap0Prop, "Height Top", false);
                ENV_LitTextureValidator.DrawTextureCheck(overlayHeightMap0Prop.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, overlayHeightStrength0Prop, heightStrengthLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Packed Maps", EditorStyles.miniBoldLabel);
                ENV_LitBlocks.MutedMiniLabel("R: Metallic · G: AO · B: Height · A: Smoothness");
                ENV_LitBlocks.TextureSlot(materialEditor, overlayMaskMap0Prop, string.Empty, false);
                ENV_LitTextureValidator.DrawTextureCheck(overlayMaskMap0Prop.textureValue, false, false, TextureImporterFormat.BC7);
                ENV_LitBlocks.Property(materialEditor, overlayMetallic0Prop, metallicLabel);
                ENV_LitBlocks.Property(materialEditor, overlaySmoothness0Prop, smoothnessLabel);
                ENV_LitBlocks.Property(materialEditor, overlayOcclusionStrength0Prop, occlusionLabel);
                ENV_LitBlocks.Property(materialEditor, overlayHeightStrength0Prop, heightStrengthLabel);

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, overlayNormalMap0Prop, "Normal Top", false);
                ENV_LitTextureValidator.DrawTextureCheck(overlayNormalMap0Prop.textureValue, false, true, TextureImporterFormat.BC5);
                ENV_LitBlocks.Property(materialEditor, overlayNormalScale0Prop, "Normal Strength");
            }

            ENV_LitBlocks.EndBox();

            ENV_LitBlocks.BeginBox();
            EditorGUILayout.LabelField("Mask Top", EditorStyles.miniBoldLabel);
            ENV_LitBlocks.Property(materialEditor, overlayCoverage0Prop, overlayCoverage0Prop.displayName);
            ENV_LitBlocks.Property(materialEditor, overlayEdgeSoftness0Prop, overlayEdgeSoftness0Prop.displayName);
            ENV_LitBlocks.EndBox();

            ENV_LitBlocks.BeginBox();
            EditorGUILayout.LabelField("RGB Noise", EditorStyles.miniBoldLabel);

            if (patternProp.floatValue < 0.5f)
            {
                ENV_LitBlocks.MutedMiniLabel(new GUIContent(
                    "RGB Noise is disabled; noise settings are inactive.",
                    "Общий RGB Noise выключен: Projection, Tiling, Rotation, Channel, Strength и Bias " +
                    "временно не влияют. Coverage и Edge Softness продолжают работать."));
            }

            DrawPatternSpaceBlock(patternSpace0Prop, patternTiling0Prop, patternRotation0Prop);

            // Выпадающий список канала — без кнопки сброса (решение владельца 15.09.2026).
            var channelLabel = new GUIContent(patternChannel0Prop.displayName,
                "Три рисунка в каналах одной карты. Смена канала меняет характер износа " +
                "(облачные пятна ↔ направленные подтёки) без перерисовки ассета.");
            ENV_LitBlocks.DropdownProperty(materialEditor, patternChannel0Prop, channelLabel);

            var strengthLabel = new GUIContent(patternStrength0Prop.displayName,
                "Смещает источник границы вокруг Coverage выше — не сдвигает площадь покрытия, " +
                "только разнообразит край. 0 — гладкая аналитическая граница, как без узора.");
            ENV_LitBlocks.Property(materialEditor, patternStrength0Prop, strengthLabel);

            var biasLabel = new GUIContent(patternBias0Prop.displayName,
                "Нейтраль канала карты шума. Дефолт 0.5 держит серую заглушку без смещения " +
                "при любой силе; для карты с другой средней яркостью канала подвинь сюда.");
            ENV_LitBlocks.Property(materialEditor, patternBias0Prop, biasLabel);

            ENV_LitBlocks.EndBox();
        }

        // Художественный узор блока наноса (тикет 06, .scratch/env-lit-layers/issues/
        // 06-noise-per-consumer.md) — своя проекция/тайлинг/поворот/канал/сила/байас, читает
        // общую карту шума _PatternMap. Показывается независимо от Top Projection Layer:
        // прятать его за чужой галочкой было бы ловушкой.
        private void DrawPatternBlock()
        {
            var patternHeader = new GUIContent("RGB Noise",
                "Карта шума и её общий слот. Ниже, в Top Projection Layer, — свой блок настроек " +
                "чтения этой карты для наноса; свой блок для смешивания материалов — в Material Blending.");
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, patternHeader, patternProp)) return;

            ENV_LitBlocks.BeginBox();

            if (overlayLayer0Prop.floatValue < 0.5f && materialMixProp.floatValue < 0.5f)
                EditorGUILayout.HelpBox(
                    "Top Projection Layer and Material Blending are disabled; the RGB Noise Map currently has no effect.",
                    MessageType.Info);

            ENV_LitBlocks.TextureSlot(materialEditor, patternMapProp, "RGB Noise Map", false);
            // sRGB выключен — три канала несут независимые данные, не цвет. BC7: три
            // независимых рисунка одноканальным форматом не сжать (как _MaskMap).
            ENV_LitTextureValidator.DrawTextureCheck(patternMapProp.textureValue, false, false, TextureImporterFormat.BC7);
            // Трипланар растягивает края плоскостей при Clamp — карта обязана тайлиться.
            ENV_LitTextureValidator.DrawWrapModeCheck(patternMapProp.textureValue, TextureWrapMode.Repeat,
                new GUIContent(
                    "Wrap Mode must be Repeat for triplanar projection.",
                    "Wrap Mode не Repeat — на трипланарной проекции края плоскостей растянутся."));

            EditorGUILayout.HelpBox(
                "Per-consumer Projection, Tiling, Rotation, Channel and Strength settings are in " +
                "Top Projection Layer and Material Blending below.",
                MessageType.None);

            ENV_LitBlocks.EndBox();
        }

        // Общий блок настроек чтения карты шума одним потребителем — проекция/тайлинг/
        // поворот идут парой (тикет 06: "тайлинг planar — тайлов на метр, тайлинг UV —
        // тайлов на UV-шелл, одно число не может означать оба"). Общий метод для наноса
        // и для обоих слоёв смешивания — все читают ту же карту тем же способом.
        // Все тайлинги Vector2; проверка типа оставлена как защита общей обёртки.
        private void DrawPatternSpaceBlock(MaterialProperty spaceProp, MaterialProperty tilingProp,
            MaterialProperty rotationProp, float labelIndent = 0f)
        {
            var spaceLabel = new GUIContent(spaceProp.displayName,
                "Planar XZ — непрерывность через стыки модулей, но тянется полосами на вертикалях. " +
                "Mesh UV — одинаково на любой ориентации, но повторяется на каждом экземпляре модуля. " +
                "Triplanar — три проекции по нормали, без полос на вертикалях; всегда следует " +
                "Projection Space материала (World нужен для бесшовности через стык модулей).");
            // Выпадающий список проекции — без кнопки сброса (решение владельца 15.09.2026).
            ENV_LitBlocks.DropdownProperty(materialEditor, spaceProp, spaceLabel, labelIndent);

            if (tilingProp.propertyType == UnityEngine.Rendering.ShaderPropertyType.Vector)
                ENV_LitBlocks.Vector2Property(materialEditor, tilingProp,
                    new GUIContent(tilingProp.displayName), labelIndent);
            else
                ENV_LitBlocks.Property(materialEditor, tilingProp,
                    new GUIContent(tilingProp.displayName), labelIndent);

            ENV_LitBlocks.Property(materialEditor, rotationProp,
                new GUIContent(rotationProp.displayName), labelIndent);
        }

        // Смешивание материалов мазком (.scratch/env-lit-layers/issues/04-material-blending.md,
        // 06-noise-per-consumer.md). Покрытие приходит из вершинного цвета (кисть) или
        // из уровня Blend Coverage; форму границы каждого слоя задаёт его собственный канал
        // общей карты шума этого блока.
        private void DrawMaterialBlendBlock()
        {
            var header = new GUIContent("Material Blending",
                "Второй и третий материал по весу из вершинного цвета или уровня покрытия. " +
                "Каждый слой независимо читает вершинный цвет или общую карту RGB Noise.");
            if (!ENV_LitBlocks.DrawToggleHeader(materialEditor, header, materialMixProp)) return;

            DrawMixLayer(1, mixMap1Prop, mixColor1Prop, mixNormalMap1Prop, mixNormalScale1Prop,
                mixMetallic1Prop, mixSmoothness1Prop, mixMaskFromTexture1Prop, mixCoverage1Prop,
                mixPatternSpace1Prop, mixPatternTiling1Prop, mixPatternRotation1Prop,
                mixPatternChannel1Prop, mixPatternStrength1Prop, mixPatternBias1Prop, mixEdgeSoftness1Prop,
                mixMaskMap1Prop, mixMetallicMap1Prop, mixOcclusionMap1Prop, mixSmoothnessMap1Prop,
                mixOcclusionStrength1Prop);

            if (ENV_LitBlocks.DrawToggleHeader(materialEditor,
                new GUIContent("Second Blend Layer",
                    "Второй подмешиваемый слой ложится ПОВЕРХ первого — порядок фиксирован. " +
                    "Кисть держит сумму весов в пределах единицы, поэтому на практике они почти не перекрываются."),
                materialMixTwoProp))
            {
                DrawMixLayer(2, mixMap2Prop, mixColor2Prop, mixNormalMap2Prop, mixNormalScale2Prop,
                    mixMetallic2Prop, mixSmoothness2Prop, mixMaskFromTexture2Prop, mixCoverage2Prop,
                    mixPatternSpace2Prop, mixPatternTiling2Prop, mixPatternRotation2Prop,
                    mixPatternChannel2Prop, mixPatternStrength2Prop, mixPatternBias2Prop, mixEdgeSoftness2Prop,
                    mixMaskMap2Prop, mixMetallicMap2Prop, mixOcclusionMap2Prop, mixSmoothnessMap2Prop,
                    mixOcclusionStrength2Prop);
            }
        }

        // Один подмешиваемый слой. Общий метод на оба, а не два похожих: тикет требует,
        // чтобы слои вели себя одинаково, и это касается инспектора тоже.
        private void DrawMixLayer(int index, MaterialProperty mapProp, MaterialProperty colorProp,
            MaterialProperty normalMapProp, MaterialProperty normalScaleProp,
            MaterialProperty metallicProp, MaterialProperty smoothnessProp,
            MaterialProperty maskSourceProp, MaterialProperty coverageProp,
            MaterialProperty patternSpaceProp, MaterialProperty patternTilingProp,
            MaterialProperty patternRotationProp,
            MaterialProperty patternChannelProp, MaterialProperty patternStrengthProp,
            MaterialProperty patternBiasProp, MaterialProperty edgeSoftnessProp,
            MaterialProperty maskMapProp, MaterialProperty metallicMapProp,
            MaterialProperty occlusionMapProp, MaterialProperty smoothnessMapProp,
            MaterialProperty occlusionStrengthProp)
        {
            ENV_LitBlocks.BeginBox();

            string vertexChannel = index == 1 ? "G" : "B";
            EditorGUILayout.LabelField($"Layer {index} · {vertexChannel} Channel", EditorStyles.miniBoldLabel);

            var sourceLabel = new GUIContent("Mask Source",
                $"Vertex Color читает канал {vertexChannel}. RGB Noise использует общий слот карты, " +
                $"но собственные настройки этого слоя. Белый непокрашенный vertex color имеет {vertexChannel} = 1 " +
                $"и полностью покрывает объект Layer {index}.");
            ENV_LitBlocks.DropdownProperty(materialEditor, maskSourceProp, sourceLabel);

            if (maskSourceProp.floatValue > 0.5f)
            {
                ENV_LitBlocks.Property(materialEditor, coverageProp,
                    new GUIContent("Blend Coverage",
                        "0 — слоя нет, 1 — полное покрытие. Настройка принадлежит только этому слою."));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RGB Noise Edge Settings", EditorStyles.miniBoldLabel);

            if (patternProp.floatValue < 0.5f)
            {
                ENV_LitBlocks.MutedMiniLabel(new GUIContent(
                    "RGB Noise is disabled; noise settings are inactive.",
                    "Общий RGB Noise выключен: Projection, Tiling, Rotation, Channel, Strength и Bias " +
                    "временно не влияют. Softness продолжает работать; Blend Coverage также работает, " +
                    "когда источником выбран RGB Noise."));
            }

            // Выпадающий список канала — без кнопки сброса (решение владельца 15.09.2026).
            var channelLabel = new GUIContent("Channel",
                "Канал общей карты RGB Noise. Координаты, сила и байас принадлежат этому слою.");
            ENV_LitBlocks.DropdownProperty(materialEditor, patternChannelProp, channelLabel);

            var strengthLabel = new GUIContent("Strength",
                "Смещает источник границы вокруг покрытия — не сдвигает его площадь, только " +
                "разнообразит край. 0 — жёсткая граница строго по покрытию.");
            ENV_LitBlocks.Property(materialEditor, patternStrengthProp, strengthLabel);

            var biasLabel = new GUIContent("Bias",
                "Нейтраль канала карты шума. Дефолт 0.5 — без смещения при любой силе для карты " +
                "со средней яркостью канала около середины.");
            ENV_LitBlocks.Property(materialEditor, patternBiasProp, biasLabel);

            var edgeLabel = new GUIContent("Softness",
                "Своя у слоя — у слоёв уже разные канал/сила/байас, общая мягкость была бы " +
                "произволом (грилл 15.09.2026). 0 — жёсткий край, 1 — мягкий, слой не становится " +
                "равномерной плёнкой ни при каком значении.");
            ENV_LitBlocks.Property(materialEditor, edgeSoftnessProp, edgeLabel);

            DrawPatternSpaceBlock(patternSpaceProp, patternTilingProp, patternRotationProp,
                BlendPatternLabelIndent);

            ENV_LitBlocks.EndBox();

            DrawMixLayerMaterialMaps(index, mapProp, colorProp, normalMapProp, normalScaleProp,
                metallicProp, smoothnessProp, maskMapProp, metallicMapProp, occlusionMapProp,
                smoothnessMapProp, occlusionStrengthProp);
        }

        private void DrawMixLayerMaterialMaps(int index,
            MaterialProperty mapProp, MaterialProperty colorProp,
            MaterialProperty normalMapProp, MaterialProperty normalScaleProp,
            MaterialProperty metallicProp, MaterialProperty smoothnessProp,
            MaterialProperty maskMapProp, MaterialProperty metallicMapProp,
            MaterialProperty occlusionMapProp, MaterialProperty smoothnessMapProp,
            MaterialProperty occlusionStrengthProp)
        {
            bool separate = maskMapSeparateProp.floatValue > 0.5f;

            ENV_LitBlocks.BeginBox();
            EditorGUILayout.LabelField("Material Maps", EditorStyles.miniBoldLabel);

            var colorLabel = new GUIContent("Color",
                "Альфа не используется. Дефолт — sRGB 235, потолок дисциплины альбедо (§8), не белый.");
            ENV_LitBlocks.ColorTexture(materialEditor, mapProp, colorProp,
                new GUIContent($"Albedo L{index}"), colorLabel);
            ENV_LitTextureValidator.DrawTextureCheck(mapProp.textureValue, true, false, null);

            var metallicLabel = new GUIContent("Metallic Strength",
                "Множитель поверх карты выше, не переключатель — пустая карта (\"white\") отдаёт " +
                "этот ползунок как есть, заполненная умножается на него. Карта при ползунке 0 " +
                "не даст ничего.");
            var smoothnessLabel = new GUIContent("Smoothness Strength",
                "Тот же множитель, что у Metallic выше.");
            var occlusionLabel = new GUIContent("Occlusion Strength",
                "Своя сила затенения слоя — AO базы описывает швы базы, под заменённым " +
                "материалом он не к месту. Без карты комплекта затенения у слоя нет вовсе.");
            const string normalTooltip =
                "Работает только при включённой карте нормалей материала — инспектор включает " +
                "её сам при назначении этого слота, но базовую карту всё равно стоит заполнить.";

            ENV_LitBlocks.Separator();
            if (separate)
            {
                ENV_LitBlocks.TextureSlot(materialEditor, metallicMapProp, "Metallic", false);
                ENV_LitTextureValidator.DrawTextureCheck(metallicMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, metallicProp, metallicLabel);

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, smoothnessMapProp, "Smoothness", false);
                ENV_LitTextureValidator.DrawTextureCheck(smoothnessMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, smoothnessProp, smoothnessLabel);

                ENV_LitBlocks.Separator();
                ENV_LitBlocks.TextureSlot(materialEditor, occlusionMapProp, "Occlusion", false);
                ENV_LitTextureValidator.DrawTextureCheck(occlusionMapProp.textureValue, false, false, TextureImporterFormat.BC4);
                ENV_LitBlocks.Property(materialEditor, occlusionStrengthProp, occlusionLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Packed Maps", EditorStyles.miniBoldLabel);
                ENV_LitBlocks.MutedMiniLabel("R: Metallic · G: AO · B: Unused · A: Smoothness");
                ENV_LitBlocks.TextureSlot(materialEditor, maskMapProp, string.Empty, false);
                ENV_LitTextureValidator.DrawTextureCheck(maskMapProp.textureValue, false, false, TextureImporterFormat.BC7);
                ENV_LitBlocks.Property(materialEditor, metallicProp, metallicLabel);
                ENV_LitBlocks.Property(materialEditor, smoothnessProp, smoothnessLabel);
                ENV_LitBlocks.Property(materialEditor, occlusionStrengthProp, occlusionLabel);
            }

            ENV_LitBlocks.Separator();
            ENV_LitBlocks.TextureSlot(materialEditor, normalMapProp, "Normal", false);
            ENV_LitTextureValidator.DrawTextureCheck(normalMapProp.textureValue, false, true, TextureImporterFormat.BC5);
            ENV_LitBlocks.Property(materialEditor, normalScaleProp,
                new GUIContent("Normal Strength", normalTooltip));

            ENV_LitBlocks.EndBox();
        }
    }
}
