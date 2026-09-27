using UnityEngine;

namespace WeatherSystem
{
    // Единая точка для шкалы времени суток и формул дуги — общая между рантаймом
    // (WeatherService) и Edit Mode превью (Assets/Editor/Weather/WeatherPreviewDriver.cs).
    // Раньше TimeOfDay01 * 360f было продублировано в обоих местах — тот же риск
    // расхождения, от которого уже закрылись WeatherSkyApplier для списка свойств шейдера.
    //
    // Шкала: 0 полночь · 0.25 06:00 · 0.5 полдень · 0.75 18:00 (как у Cozy,
    // MeridiemTime.cs — буквально часы/24). До этой миграции 0 было восходом; стык
    // несвязанных ключей градиента (Evaluate(0) и Evaluate(1) — независимые точки,
    // не зациклены) стоял на самом динамичном участке неба. Теперь он на полночи,
    // где небо статично и рассинхрон не читается.
    //
    // Раскладка Cozy (CozyWeather.cs:908-909): пивот несёт статическую ориентацию дуги
    // сцены (где горизонт), а время едет на самих лампах. До этого пивот нёс и время,
    // и наклон дуги сидел на лампе после него — из-за этого наклон тащил за собой точки
    // горизонта (восход 150°, заход 30° вместо честных антиподов). Раскладка ниже
    // развязывает ручки: Azimuth не трогает высоту, Tilt не трогает азимуты.
    public static class WeatherTime
    {
        // Подобран измерением под наш риг (не списан у Cozy, у них другой pivot/оффсет):
        // при t=0 и Azimuth=Tilt=0 высота должна быть отрицательна (тело под горизонтом),
        // при t=0.5 — максимальна (тело в зените).
        private const float PivotOffsetDegrees = -90f;

        // Статическая ориентация дуги сцены — куда смотрит пивот до всякого учёта времени.
        public static Vector3 ArcPivotEuler(float arcAzimuth, float arcTilt) =>
            new Vector3(0f, arcAzimuth, arcTilt);

        // Поворот тела по дуге в зависимости от времени суток — локальный поворот лампы
        // относительно пивота. offsetDegrees сдвигает тело по той же траектории (луна
        // относительно солнца), 0 для солнца.
        public static Vector3 CelestialEuler(float timeOfDay01, float offsetDegrees) =>
            new Vector3(timeOfDay01 * 360f + PivotOffsetDegrees + offsetDegrees, 0f, 0f);

        // Вес светимости тела у горизонта — smoothstep(-band, +band, elevation). Общая формула
        // для WeatherCelestialApplier: то, чем гасится щелчок света и теней при переходе
        // с геометрического порога (elevation > 0f) на плавный по яркости.
        public static float HorizonWeight(float elevation, float band)
        {
            float t = Mathf.InverseLerp(-band, band, elevation);
            return t * t * (3f - 2f * t);
        }
    }
}
