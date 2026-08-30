namespace Infastructure.StaticData.World
{
    /// <summary>
    /// Политика загрузки сегментов столба. Выгрузки нет по решению спека
    /// (.scratch/additive-scenes-vertical/spec.md) — второе значение появится
    /// только после замеров реального веса этажа.
    /// </summary>
    public enum LoadPolicy
    {
        KeepAllLoaded
    }
}
