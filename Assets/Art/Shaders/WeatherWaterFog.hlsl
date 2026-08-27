#ifndef SPIDERRIG_WEATHER_WATER_FOG_INCLUDED
#define SPIDERRIG_WEATHER_WATER_FOG_INCLUDED

// Мост между нашим туманом и Stylized Water 3. Функция называется BlendStylizedFog и
// подключается через "интеграцию COZY" в шейдере воды не потому, что это COZY —
// а потому, что это единственный готовый слот в Fog.hlsl с нужной сигнатурой
// (float3 worldPos, float4 inColor) -> float4. См. .scratch/fog-generation-and-layers/
// spec.md, решения #7-8, предположение #1; настройка слота — StylizedWater3_Standard
// .watershader3.meta (autoIntegration выключен, customIncludeDirectives).
//
// Тело — та же цепочка вызовов, что и в fullscreen-пассе (SHD_Weather_Fog.shader), в том
// же порядке: расхождение с пассом даёт видимую ступеньку цвета на кромке воды с землёй.

#include "WeatherFog.hlsl"

// НЕ _WorldSpaceCameraPos: этот файл подключается customIncludeDirectives — текстово
// раньше, чем шейдер воды сам подключает Libraries/Input.hlsl (там объявлена штатная
// _WorldSpaceCameraPos). Использование её здесь даёт "undeclared identifier", а повторное
// объявление той же переменной — "redefinition" уже на Input.hlsl (проверено эмпирически,
// оба варианта). Свой уникальный глобал, который кормит WeatherService.Tick() через
// ICameraProviderService — единственный способ получить позицию камеры в этой точке файла.
// Остальные глобалы тумана (_SR_Fog*/_SR_Filter*) объявлены в WeatherFog.hlsl — общие
// для всех потребителей, здесь не дублируются.
float3 _SR_CameraPositionWS;

// Дистанция от поверхности воды до камеры (позиция входного фрагмента), не от дна под
// водой — по решению пользователя. Преломлённый луч на мелкой стилизованной воде идёт
// почти по той же дистанции; учёт настоящей дистанции дна требует второго вызова после
// пересборки кадра из _CameraOpaqueTexture и не входит в этот тикет.
float4 BlendStylizedFog(float3 worldPos, float4 inColor)
{
    half3 result = SR_ApplyWorldFog(worldPos, _SR_CameraPositionWS, half3(inColor.rgb));

    // Альфа входа не трогается — за прозрачность воды отвечает water.edgeFade
    // (ForwardPass.hlsl), не туман.
    return float4(float3(result), inColor.a);
}

#endif // SPIDERRIG_WEATHER_WATER_FOG_INCLUDED
