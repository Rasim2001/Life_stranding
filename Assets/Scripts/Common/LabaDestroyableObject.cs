using System;
using System.Collections;
using Infastructure.CutScenes;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Common
{
    public class LaboratoryDestroyableObject : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] _allRigidbodies;
        [SerializeField] private Transform _impulsePoint;

        [Header("Impulse")]
        [SerializeField, Min(0f)] private float _forwardImpulse = 2.5f;

        private ICutSceneService _cutSceneService;
        private ICameraProviderService _providerService;

        private float _blendTime;
        private Coroutine _coroutine;

        [Inject]
        public void Construct(ICutSceneService cutSceneService, ICameraProviderService providerService)
        {
            _providerService = providerService;
            _cutSceneService = cutSceneService;
        }

        private void Start()
        {
            _blendTime = _providerService.CameraTransform.GetComponent<CinemachineBrain>().DefaultBlend.Time;

            _cutSceneService.OnCutsceneActiveChanged += CutSceneStarted;
        }

        private void OnDestroy()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _cutSceneService.OnCutsceneActiveChanged -= CutSceneStarted;
        }

        private void CutSceneStarted(bool isStarted)
        {
            if (isStarted && _cutSceneService.CutsceneId == CutsceneId.FlowerPickupCutscene)
                _coroutine = StartCoroutine(DestroyObjects());
        }

        private IEnumerator DestroyObjects()
        {
            yield return new WaitForSeconds(_blendTime);

            foreach (Rigidbody rb in _allRigidbodies)
            {
                rb.isKinematic = false;

                Vector3 direction = rb.transform.position - _impulsePoint.position;
                Vector3 impulse = direction * _forwardImpulse;

                rb.AddForce(impulse, ForceMode.Impulse);
            }
        }
    }
}