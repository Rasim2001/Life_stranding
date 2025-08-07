using System;
using Unity.Cinemachine;
using UnityEngine;

namespace HUD
{
    public class HudUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _canvasRectTransform;

        private FinishIndicator _finishIndicator;

        public void Initialize(Vector3 finishTargetPosition, RectTransform arrowUI) =>
            _finishIndicator = new FinishIndicator(finishTargetPosition, arrowUI, _canvasRectTransform);

        private void Start() =>
            CinemachineCore.CameraUpdatedEvent.AddListener(UpdateIndicator);

        private void OnDestroy() =>
            CinemachineCore.CameraUpdatedEvent.RemoveListener(UpdateIndicator);

        private void UpdateIndicator(CinemachineBrain arg0)
        {
            if (_finishIndicator == null)
                return;

            _finishIndicator.Update();
        }
    }
}