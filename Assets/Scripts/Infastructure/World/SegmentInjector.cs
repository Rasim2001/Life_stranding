using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Zenject.Internal;

namespace Infastructure.World
{
    // Контракт сцены сегмента: после тикета 04 SceneContext уехал в Gameplay, и
    // GetInjectableMonoBehaviours сканирует только свою сцену — контент сегмента
    // инъекцию больше не получает. -5000 выбрано строго между execution order
    // SceneContext (-9999) и контентом (0), см. .scratch/plans/scratch-tech-debt-03-segment-scene-inje-compressed-blossom.md.
    [DefaultExecutionOrder(-5000)]
    public class SegmentInjector : MonoBehaviour
    {
        private void Awake()
        {
            DiContainer container = FindResolvedSceneContainer();

            if (container == null)
            {
                Debug.LogWarning($"[SegmentInjector] Сцена «{gameObject.scene.name}» запущена без " +
                    "Gameplay — инжект пропущен. Нормально при одиночном прогоне сегмента.", this);
                return;
            }

            var injectables = new List<MonoBehaviour>();
            ZenUtilInternal.AddStateMachineBehaviourAutoInjectersInScene(gameObject.scene);
            ZenUtilInternal.GetInjectableMonoBehavioursInScene(gameObject.scene, injectables);

            foreach (MonoBehaviour injectable in injectables)
                container.Inject(injectable);
        }

        private static DiContainer FindResolvedSceneContainer()
        {
            foreach (SceneContext context in FindObjectsByType<SceneContext>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (context.HasResolved)
                    return context.Container;

            return null;
        }
    }
}
