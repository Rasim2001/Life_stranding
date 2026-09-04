using UnityEngine;

namespace Infastructure.World
{
    public interface ISegmentActivation
    {
        void ActivateAll();
    }

    /// <summary>
    /// Точка вызова зафиксирована тикетом 02 (BuildLevelState.InitGameWorld,
    /// последней строкой). Обход без guard и без реестра: InitGameWorld вызывается
    /// повторно при рестарте, сцены при рестарте перезагружаются, ветки возвращаются
    /// выключенными и обязаны включиться снова (docs/scene-regulations.md §3, §6).
    /// </summary>
    public class SegmentActivation : ISegmentActivation
    {
        public void ActivateAll()
        {
            SegmentLiveRoot[] roots = Object.FindObjectsByType<SegmentLiveRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (SegmentLiveRoot root in roots)
                root.gameObject.SetActive(true);

            Debug.Log($"[SegmentActivation] живых веток включено: {roots.Length}");
        }
    }
}
