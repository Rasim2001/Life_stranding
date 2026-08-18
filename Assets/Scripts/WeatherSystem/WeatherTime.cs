using UnityEngine;

namespace WeatherSystem
{
    // Единая точка для шкалы времени суток и формулы дуги пивота — общая между рантаймом
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
    // PivotOffsetDegrees подобран измерением под наш риг (не списан у Cozy, у них другой
    // pivot/оффсет): при t=0 GetSunElevation() должен быть отрицателен (солнце под
    // горизонтом), при t=0.5 — максимален (солнце в зените).
    public static class WeatherTime
    {
        private const float PivotOffsetDegrees = -90f;

        public static Vector3 PivotEuler(float timeOfDay01) =>
            new Vector3(timeOfDay01 * 360f + PivotOffsetDegrees, 0f, 0f);
    }
}
