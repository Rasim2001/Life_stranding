// ENV-специфичные фрагментные keyword'ы, общие для ForwardLit и Meta — оба подключают
// этот файл через #include_with_pragmas (тикет 06, .scratch/env-lit-layers/issues/
// 06-noise-per-consumer.md). Раньше один и тот же список прагм держался руками в двух
// пассах отдельно; здесь одна точка объявления не даёт им разойтись молча.
//
// Что сюда НЕ едет и почему — не наводить тут порядок следующим заходом:
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

// Проекция шума наноса — трёхпозиционная форма (позиция 0 — Planar XZ, дефолт; UV — развёртка
// меша; Triplanar). Keyword, не сравнение с uniform: у трипланара меняется число текстурных
// выборок между ветками (одна против трёх), а производные UV внутри ветвления формально
// не определены — тот случай, где платформенные компиляторы расходятся.
#pragma shader_feature_local_fragment _ _PATTERNSPACE0_UV _PATTERNSPACE0_TRIPLANAR

// Пространство эффектов — у каждого потребителя своё, общего переключателя нет:
// слой Top (карты, наклон, шум) и градиент по высоте. Только World; Local — отсутствие keyword'а.
#pragma shader_feature_local_fragment _OVERLAYSPACE0_WORLD
#pragma shader_feature_local_fragment _GRADIENTSPACE_WORLD

// Проекция карт Base: позиция 0 — Mesh UV (обе выключены), Local и World — трипланар.
// Тоже keyword, а не uniform: у трипланара три выборки на карту вместо одной.
#pragma shader_feature_local_fragment _ _BASEPROJECTION_LOCAL _BASEPROJECTION_WORLD

// Проекция карт слоёв Blend — у каждого слоя своя, независимо от Base и друг от друга.
#pragma shader_feature_local_fragment _ _MIXPROJECTION1_LOCAL _MIXPROJECTION1_WORLD
#pragma shader_feature_local_fragment _ _MIXPROJECTION2_LOCAL _MIXPROJECTION2_WORLD

// Проекция RGB Noise (маски) слоёв Blend — отдельно от карт слоя, другого слоя, Top и градиента.
#pragma shader_feature_local_fragment _ _MIXPATTERNPROJECTION1_LOCAL _MIXPATTERNPROJECTION1_WORLD
#pragma shader_feature_local_fragment _ _MIXPATTERNPROJECTION2_LOCAL _MIXPATTERNPROJECTION2_WORLD

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
