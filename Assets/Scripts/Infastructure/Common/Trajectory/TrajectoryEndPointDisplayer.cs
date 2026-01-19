using System;
using Infastructure.Services.CameraProvider;
using UnityEngine;
using Zenject;

namespace Infastructure.Common.Trajectory
{
    public class TrajectoryEndPointDisplayer : MonoBehaviour, ITrajectoryEndPointDisplayer
    {
        [SerializeField] private RectTransform _endPointTransform;

        private ICameraProviderService _cameraProviderService;

        [Inject]
        public void Construct(ICameraProviderService cameraProviderService) =>
            _cameraProviderService = cameraProviderService;

        private void Start() =>
            Hide();

        public void Show() =>
            _endPointTransform.gameObject.SetActive(true);

        public void Hide() =>
            _endPointTransform.gameObject.SetActive(false);

        public void Apply(RaycastHit hit)
        {
            Vector3 screenPoint = _cameraProviderService.Camera.WorldToScreenPoint(hit.point);
            _endPointTransform.position = screenPoint;
        }
    }
}