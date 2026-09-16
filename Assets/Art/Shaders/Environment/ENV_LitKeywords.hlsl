// ENV-специфичные фрагментные keyword'ы, общие для ForwardLit и Meta — оба подключают
// этот файл через #include_with_pragmas (тикет 06, .scratch/env-lit-layers/issues/
// 06-noise-per-consumer.md). Раньше один и тот же список прагм держался руками в двух
// пассах отдельно; здесь одна точка объявления не даёт им разойтись молча.
//
// Что сюда НЕ едет и почему — не наводить тут порядок следующим заходом:
//   _PROJECTIONSPACE_WORLD — в ForwardLit объявлен с суффиксом _fragment, в Meta без
//     него: мета резолвит positionPS в вершине, форвард — во фрагменте (ENV_Lit.shader).
//   _NORMALMAP — гейтит вершинную работу (сборку тангента) в обоих пассах, не только
//     фрагментную часть.
//   _OVERLAY_HEIGHT_0 — только ForwardLit: итоговая нормаль наноса не участвует
//     в Meta. Базовый _HEIGHT_BUMP, напротив, влияет на маску наноса и его альбедо.
//   _SURFACE_TYPE_TRANSPARENT, _RECEIVE_SHADOWS_OFF — только у ForwardLit, у Meta их нет.
//   EDITOR_VISUALIZATION, _ALPHATEST_ON, _EMISSION — своя история keyword'а у каждого
//     пасса (EDITOR_VISUALIZATION не shader_feature вовсе); оставлены объявленными на
//     месте, а не растащены по третьему файлу без причины.

#pragma shader_feature_local_fragment _MASKMAP_SEPARATE
#pragma shader_feature_local_fragment _HEIGHT_BUMP
#pragma shader_feature_local_fragment _ALBEDO_ADJUST
#pragma shader_feature_local_fragment _HEIGHT_GRADIENT
#pragma shader_feature_local_fragment _OVERLAY_LAYER_0
#pragma shader_feature_local_fragment _PATTERN

// Проекция узора наноса и узора смешивания — раздельные keyword'ы, у каждого своя
// трёхпозиционная форма (позиция 0 — Planar XZ, дефолт; UV — развёртка меша; Triplanar).
// Keyword, не сравнение с uniform: у трипланара меняется число текстурных выборок между
// ветками (одна против трёх), а производные UV внутри ветвления формально не определены —
// тот случай, где платформенные компиляторы расходятся.
#pragma shader_feature_local_fragment _ _PATTERNSPACE0_UV _PATTERNSPACE0_TRIPLANAR
#pragma shader_feature_local_fragment _ _MIXPATTERNSPACE1_UV _MIXPATTERNSPACE1_TRIPLANAR
#pragma shader_feature_local_fragment _ _MIXPATTERNSPACE2_UV _MIXPATTERNSPACE2_TRIPLANAR

#pragma shader_feature_local_fragment _MATERIAL_MIX
#pragma shader_feature_local_fragment _MATERIAL_MIX_2
#pragma shader_feature_local_fragment _MIX_MASK_TEXTURE_1
#pragma shader_feature_local_fragment _MIX_MASK_TEXTURE_2

// Комплект материальных карт слоя (тикет 07, .scratch/env-lit-layers/issues/07-layer-maps.md).
// Выводится из наличия текстуры активного режима _MASKMAP_SEPARATE (ENV_LitShaderGUI.
// ValidateMaterial), вручную не ставится — та же идиома, что у _NORMALMAP от _BumpMap.
// Гейт keyword'ом, а не дефолтом "white" как у базы: база активна всегда, слои опциональны,
// и включённый слой без карт не должен платить выборками ни за что (до девяти на материал
// в раздельном режиме при трёх слоях).
#pragma shader_feature_local_fragment _OVERLAY_MAPS_0
#pragma shader_feature_local_fragment _MIX_MAPS_1
#pragma shader_feature_local_fragment _MIX_MAPS_2
