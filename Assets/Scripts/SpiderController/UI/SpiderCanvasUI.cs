using Common;
using Infastructure.Services.CutScene;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace SpiderController.UI
{
    public class SpiderCanvasUI : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;

        private RotateToCamera _rotateToCamera;
        private ICutSceneService _cutSceneService;

        [Inject]
        public void Construct(ICutSceneService cutSceneService)
        {
            _cutSceneService = cutSceneService;
        }

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rotateToCamera = new RotateToCamera();

            _cutSceneService.OnCutsceneActiveChanged += CutSceneActiveChanged;
        }

        private void Start() =>
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateRotation);

        private void OnDestroy()
        {
            _cutSceneService.OnCutsceneActiveChanged -= CutSceneActiveChanged;
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateRotation);
        }

        private void UpdateRotation(CinemachineBrain _) =>
            _rotateToCamera.UpdateRotation(transform);

        private void CutSceneActiveChanged(bool value) =>
            _canvasGroup.alpha = value ? 0 : 1;
    }
}