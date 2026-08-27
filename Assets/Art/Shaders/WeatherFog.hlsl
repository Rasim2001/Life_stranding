#ifndef SPIDERRIG_WEATHER_FOG_INCLUDED
#define SPIDERRIG_WEATHER_FOG_INCLUDED

// Туман на мировой геометрии. Общий include для fullscreen-пасса (SHD_Weather_Fog), стекла
// AllIn13DShader и оверлея воды (SHD_Weather_WaterFog) — единственная точка правды для формулы,
// см. .scratch/fog-generation-and-layers/spec.md, решения #1-3, #7-8. Расхождение копий
// цепочки между потребителями — это и есть видимая ступенька цвета на стыке материалов
// (см. решение при разборе тикета 04), поэтому SR_ApplyWorldFog/SR_WorldFogOverlay ниже —
// не только математика, но и единственный способ её вызвать.

#include "WeatherNoise.hlsl"

// Глобалы — не свойства материала. Для fullscreen-пасса и AllIn1-стекла это Shader.SetGlobalX
// из WeatherFogApplier; для воды — те же значения, подставленные под другим механизмом
// (custom directives), но имя и семантика одни и те же на весь проект.
float _SR_FogVisibilityDistance;
float4 _SR_FogNearColor;
float4 _SR_FogMidColor;
float4 _SR_FogFarColor;
float _SR_FogMidPosition;
float _SR_FogFarPosition;
float4 _SR_FilterColor;
float _SR_FilterSaturation;
float _SR_FilterValue;

// σ = 3/V — на дальности V непрозрачность 95% (1 - exp(-3) ≈ 0.95).
half SR_FogAmount(half distanceToCamera, half visibilityDistance)
{
    half sigma = 3.0h / max(visibilityDistance, 0.001h);
    return 1.0h - exp(-sigma * distanceToCamera);
}

// 3-стоповая рампа по долям V (решения #2-3): u = d/V, стопы на [0, pMid, pFar].
// Альфа результата — художественная маска A(d) поверх экспоненты, не сама плотность.
half4 SR_FogRamp(half distanceToCamera, half visibilityDistance,
    half4 nearColor, half4 midColor, half4 farColor, half pMid, half pFar)
{
    half u = distanceToCamera / max(visibilityDistance, 0.001h);
    half4 c = lerp(nearColor, midColor, saturate(u / max(pMid, 0.0001h)));
    c = lerp(c, farColor, saturate((u - pMid) / max(pFar - pMid, 0.0001h)));
    return c;
}

// result = lerp(sceneColor, rampColorFiltered, fogAmount * rampAlpha) — финал решения #1.
// rampColorFiltered уже должен быть пропущен через SR_ApplyWeatherFilter (WeatherNoise.hlsl)
// вызывающей стороной — фильтр применяется к цвету тумана до этого смешения (решение #18).
half3 SR_ApplyFog(half3 sceneColor, half3 rampColorFiltered, half fogAmount, half rampAlpha)
{
    return lerp(sceneColor, rampColorFiltered, fogAmount * rampAlpha);
}

// Цепочка без финального смешения: плотность → рампа → фильтр. Возвращает цвет тумана
// и его результирующую альфу (fogAmount * rampAlpha) — то есть ровно то, что использовал бы
// SR_ApplyFog как lerp-фактор. Нужна отдельно от SR_ApplyWorldFog для потребителей, которые
// не имеют доступа к цвету "под собой" и полагаются на аппаратный альфа-блендинг вместо
// программного lerp (оверлей тумана на воде — см. .scratch/fog-generation-and-layers/spec.md).
half4 SR_WorldFogOverlay(float3 positionWS, float3 cameraPositionWS)
{
    half distanceToCamera = half(distance(positionWS, cameraPositionWS));

    half fogAmount = SR_FogAmount(distanceToCamera, half(_SR_FogVisibilityDistance));
    half4 ramp = SR_FogRamp(distanceToCamera, half(_SR_FogVisibilityDistance),
        half4(_SR_FogNearColor), half4(_SR_FogMidColor), half4(_SR_FogFarColor),
        half(_SR_FogMidPosition), half(_SR_FogFarPosition));

    half3 filteredRamp = SR_ApplyWeatherFilter(ramp.rgb, half4(_SR_FilterColor),
        half(_SR_FilterSaturation), half(_SR_FilterValue));

    return half4(filteredRamp, fogAmount * ramp.a);
}

// Вся цепочка разом: плотность → рампа → фильтр → смешение, на глобалах выше. Единственная
// точка вызова для всех потребителей (fullscreen-пасс, стекло AllIn1) — иначе рассинхрон
// копий даёт видимую ступеньку цвета там, где эти материалы граничат друг с другом.
//
// cameraPositionWS — параметром, не глобалом: у потребителей разные источники позиции камеры.
// Пасс и стекло AllIn1 читают штатную _WorldSpaceCameraPos в точке вызова (она там уже
// объявлена к этому моменту).
half3 SR_ApplyWorldFog(float3 positionWS, float3 cameraPositionWS, half3 inColor)
{
    half4 overlay = SR_WorldFogOverlay(positionWS, cameraPositionWS);
    return lerp(inColor, overlay.rgb, overlay.a);
}

#endif // SPIDERRIG_WEATHER_FOG_INCLUDED
