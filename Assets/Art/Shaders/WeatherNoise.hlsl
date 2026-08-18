#ifndef SPIDERRIG_WEATHER_NOISE_INCLUDED
#define SPIDERRIG_WEATHER_NOISE_INCLUDED

// Общая библиотека шума для купольных шейдеров погоды (SHD_Weather_DomeSky,
// SHD_Weather_DomeClouds). Первый наш .hlsl-инклуд в проекте — до этого у каждого
// шейдера была своя копия этой математики (изначально ~300 строк, продублированных
// в SHD_Weather_SkyProcedural). Вся математика уже отлажена и откалибрована замерами
// на предыдущих срезах — здесь только перенос без изменений.
//
// Функции здесь либо чисто параметрические (ничего не читают из CBUFFER материала),
// либо явно принимают нужные величины аргументами — так один файл можно инклудить
// в оба шейдера, у каждого из которых свой CBUFFER с одноимёнными по смыслу,
// но раздельно объявленными свойствами.

// --- Simplex noise (Ashima Arts / Stefan Gustavson, McEwan et al., MIT) -------------
// Публичная реализация, перенесена без изменения математики. Всё во float: внутри
// модуль 289 и множитель 42, в half это разваливается (проверено на аудите: hash-based
// шум в half ловит переполнение мантиссы на характерных величинах ~5000).

float3 SR_mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float4 SR_mod289(float4 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
float4 SR_permute(float4 x) { return SR_mod289(((x * 34.0) + 1.0) * x); }
float4 SR_taylorInvSqrt(float4 r) { return 1.79284291400159 - 0.85373472095314 * r; }

float SR_snoise(float3 v)
{
    const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
    const float4 D = float4(0.0, 0.5, 1.0, 2.0);

    float3 i  = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);

    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy;
    float3 x3 = x0 - D.yyy;

    i = SR_mod289(i);
    float4 p = SR_permute(SR_permute(SR_permute(
                 i.z + float4(0.0, i1.z, i2.z, 1.0))
               + i.y + float4(0.0, i1.y, i2.y, 1.0))
               + i.x + float4(0.0, i1.x, i2.x, 1.0));

    float n_ = 0.142857142857;
    float3 ns = n_ * D.wyz - D.xzx;

    float4 j = p - 49.0 * floor(p * ns.z * ns.z);

    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_);

    float4 x = x_ * ns.x + ns.yyyy;
    float4 y = y_ * ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, 0.0);

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);

    float4 norm = SR_taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}

// --- Дешёвый хэш направления — звёзды и base value noise ---------------------------
// Не идеально равномерен на сфере, для стилизованного прототипа достаточно.

half Hash13(half3 p)
{
    p = frac(p * 0.1031h);
    p += dot(p, p.yzx + 33.33h);
    return frac((p.x + p.y) * p.z);
}

half Stars(half3 dir, half density)
{
    half3 cell = floor(dir * 400.0h);
    half h = Hash13(cell);
    return step(1.0h - density, h);
}

half ValueNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0h - 2.0h * f);
    half c000 = Hash13(i + half3(0, 0, 0));
    half c100 = Hash13(i + half3(1, 0, 0));
    half c010 = Hash13(i + half3(0, 1, 0));
    half c110 = Hash13(i + half3(1, 1, 0));
    half c001 = Hash13(i + half3(0, 0, 1));
    half c101 = Hash13(i + half3(1, 0, 1));
    half c011 = Hash13(i + half3(0, 1, 1));
    half c111 = Hash13(i + half3(1, 1, 1));
    half x00 = lerp(c000, c100, u.x);
    half x10 = lerp(c010, c110, u.x);
    half x01 = lerp(c001, c101, u.x);
    half x11 = lerp(c011, c111, u.x);
    half y0 = lerp(x00, x10, u.y);
    half y1 = lerp(x01, x11, u.y);
    return lerp(y0, y1, u.z);
}

// Веса октав (0.5+0.25+0.125=0.875) не суммируются в 1 — без деления на эту сумму
// итоговая FBM в среднем сидит около 0.44 и почти никогда не подходит к 0.875.
half CloudShapeFBM(float3 p)
{
    half n = ValueNoise3D(p) * 0.5h;
    n += ValueNoise3D(p * 2.03h) * 0.25h;
    n += ValueNoise3D(p * 4.01h) * 0.125h;
    return n / 0.875h;
}

// Billow: |2n-1| перевёрнутый — вместо равномерной дымки обычного FBM даёт округлые
// "шапки", силуэт цветной капусты у кучевых.
half BillowFBM(float3 p)
{
    half n = (1.0h - abs(2.0h * ValueNoise3D(p) - 1.0h)) * 0.5h;
    n += (1.0h - abs(2.0h * ValueNoise3D(p * 2.03h) - 1.0h)) * 0.25h;
    n += (1.0h - abs(2.0h * ValueNoise3D(p * 4.01h) - 1.0h)) * 0.125h;
    return n / 0.875h;
}

// --- Voronoi 3D ----------------------------------------------------------------------
// Джиттер узла через sin(hash*2PI)*0.5+0.5, не сырой хэш — ячейки ровнее.
// Возвращает 0.5*dot(r,r) — половину квадрата расстояния, падение к центру
// квадратичное, край мягче, чем у length(r).

half3 Hash33(float3 p)
{
    float3 q = float3(
        dot(p, float3(127.1h, 311.7h, 74.7h)),
        dot(p, float3(269.5h, 183.3h, 246.1h)),
        dot(p, float3(113.5h, 271.9h, 124.6h)));
    return frac(sin(q) * 43758.5453h);
}

half Voronoi3D(float3 p)
{
    float3 n = floor(p);
    float3 f = frac(p);
    half F1 = 8.0h;

    [unroll]
    for (int z = -1; z <= 1; z++)
    {
        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            [unroll]
            for (int x = -1; x <= 1; x++)
            {
                float3 g = float3(x, y, z);
                float3 o = Hash33(n + g);
                o = sin(o * 6.2831h) * 0.5h + 0.5h;
                float3 r = f - g - o;
                half d = 0.5h * dot(r, r);
                F1 = min(F1, d);
            }
        }
    }

    return F1;
}

// 3-октавный Voronoi-FBM — недостающая у нас изначально мелкая деталь силуэта кучевых
// (аналог CZY_DetailScale/DetailAmount у Cozy). Замешивается в плотность ДО решения
// об альфе на стороне вызывающего кода.
half DetailVoronoiFBM(float3 p)
{
    half voro = 0.0h;
    half fade = 0.5h;
    [unroll]
    for (int o = 0; o < 3; o++)
    {
        voro += fade * saturate(Voronoi3D(p));
        p *= 2.0h;
        fade *= 0.5h;
    }
    return voro / 0.875h; // сумма весов трёх октав
}

// Общая перенормировка плотности на покрытие: после вычитания порога делим на само
// покрытие, поэтому остаток снова занимает весь [0,1]. Без этого при низком покрытии
// над порогом остаётся узкая полоска значений, и затенка вырождается в плоское пятно.
half CloudRel(half density, half coverage)
{
    return saturate((density - (1.0h - coverage)) / max(coverage, 0.001h));
}

// --- HSV-фильтр погоды ---------------------------------------------------------------
// Параметрический, не читает CBUFFER сам — каждый шейдер передаёт свои
// _FilterColor/_FilterSaturation/_FilterValue явно, тогда один инклуд работает
// для двух независимо объявленных материалов.

half3 SR_RGBToHSV(half3 c)
{
    half4 K = half4(0.0h, -1.0h / 3.0h, 2.0h / 3.0h, -1.0h);
    half4 p = lerp(half4(c.bg, K.wz), half4(c.gb, K.xy), step(c.b, c.g));
    half4 q = lerp(half4(p.xyw, c.r), half4(c.r, p.yzx), step(p.x, c.r));
    half d = q.x - min(q.w, q.y);
    // 1e-10 (как у Cozy) в half — ноль, то есть деление на ноль. Нужен эпсилон под half.
    half e = 1.0e-4h;
    return half3(abs(q.z + (q.w - q.y) / (6.0h * d + e)), d / (q.x + e), q.x);
}

half3 SR_HSVToRGB(half3 c)
{
    half4 K = half4(1.0h, 2.0h / 3.0h, 1.0h / 3.0h, 3.0h);
    half3 p = abs(frac(c.xxx + K.xyz) * 6.0h - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

// Яркость МНОЖИМ, а не вычитаем (как у Cozy). На уже тёмных цветах (тень грозовых,
// V~0.3) вычитание того же порядка обнуляет яркость — в кадре появляются чёрные дыры.
// Множитель гасит пропорционально, до чёрного доходит только при value=-1.
half3 SR_ApplyWeatherFilter(half3 c, half4 filterColor, half filterSaturation, half filterValue)
{
    half3 hsv = SR_RGBToHSV(c);
    hsv.y = saturate(hsv.y + filterSaturation);
    hsv.z = max(hsv.z * (1.0h + filterValue), 0.0h);
    return SR_HSVToRGB(hsv) * filterColor.rgb;
}

#endif // SPIDERRIG_WEATHER_NOISE_INCLUDED
