using System;
using UnityEngine;

namespace WeatherSystem.Profiles
{
    // Одна высотная полоса в WeatherStaticData.Bands. Массив полос обязан быть
    // отсортирован по возрастанию StartY — SkyBandBlender сканирует их линейным проходом.
    //
    // [StartY, EndY] — плоскость: профиль звучит в чистом виде, без подмеса соседнего.
    // [EndY, EndY + BlendUpwards] — переход к СЛЕДУЮЩЕЙ полосе, ширина у каждой своя
    // (переход A->B и B->C не обязаны совпадать). BlendUpwards = 0 — мгновенный переход,
    // без промежуточного смешения.
    [Serializable]
    public class SkyBand
    {
        public float StartY;
        public float EndY;
        public float BlendUpwards;
        public SkyBandProfile Profile;
    }
}
