using Infastructure.Services.CutScene;
using UnityEngine;
using Zenject;

namespace Common
{
    public class DestroyCameraEye : MonoBehaviour
    {
        private ICutSceneService _cutSceneService;

        [Inject]
        public void Construct(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;

        private void Start() =>
            _cutSceneService.OnCutsceneActiveChanged += ActiveChanged;

        private void OnDestroy() =>
            _cutSceneService.OnCutsceneActiveChanged -= ActiveChanged;

        private void ActiveChanged(bool isActive)
        {
            if (isActive == false)
                Destroy(gameObject);
        }
    }
}