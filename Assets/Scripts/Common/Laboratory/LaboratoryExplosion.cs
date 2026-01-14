using System.Collections;
using Infastructure.CutScenes;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Common.Laboratory
{
    public class LaboratoryExplosion : MonoBehaviour
    {
        private static readonly int ExplosionTriggerHash = Animator.StringToHash("ExplosionTrigger");

        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _finishedDebris;

        private ICutSceneService _cutSceneService;
        private ICameraProviderService _providerService;

        private float _blendTime;
        private Coroutine _coroutine;

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

        [Inject]
        public void Construct(ICutSceneService cutSceneService, ICameraProviderService providerService)
        {
            _providerService = providerService;
            _cutSceneService = cutSceneService;
        }

        private void CutSceneStarted(bool isStarted)
        {
            if (isStarted && _cutSceneService.CutsceneId == CutsceneId.FlowerPickupCutscene)
                _coroutine = StartCoroutine(StartExplosion());
        }

        private IEnumerator StartExplosion()
        {
            yield return new WaitForSeconds(_blendTime);

            _animator.SetTrigger(ExplosionTriggerHash);

            yield return new WaitForSeconds(5);

            _animator.gameObject.SetActive(false);
            _finishedDebris.gameObject.SetActive(true);
        }
    }
}