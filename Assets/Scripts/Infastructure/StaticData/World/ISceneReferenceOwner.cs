using System.Collections.Generic;

namespace Infastructure.StaticData.World
{
    /// <summary>
    /// Конфигурационный ассет, держащий ссылки на сцены. Реализуется ради
    /// SceneReferencePostprocessor: он обходит владельцев и пересинхронизирует
    /// производные имена после переименования или переноса .unity.
    /// </summary>
    public interface ISceneReferenceOwner
    {
        IEnumerable<SceneReference> SceneReferences { get; }
    }
}
