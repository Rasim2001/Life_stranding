using System;
using System.Collections;
using Dreamteck.Splines;
using Infastructure.CutScenes;
using Infastructure.Data;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.CutScene;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.SaveLoadService;
using Unity.Cinemachine;
using UnityEngine;
using Zenject;

namespace Common.Laboratory
{
    public class LaboratoryExplosion : MonoBehaviour, ISavedProgressReader
    {
        private static readonly int ExplosionTriggerHash = Animator.StringToHash("ExplosionTrigger");

        [SerializeField] private Animator _animator;
        [SerializeField] private GameObject _finishedDebris;

        private ICutSceneService _cutSceneService;
        private ICameraProviderService _providerService;

        private float _blendTime;
        private Coroutine _coroutine;
        private IProgressWatchersService _progressWatchersService;

        [Inject]
        public void Construct(ICutSceneService cutSceneService, ICameraProviderService providerService,
            IProgressWatchersService progressWatchersService)
        {
            _progressWatchersService = progressWatchersService;
            _providerService = providerService;
            _cutSceneService = cutSceneService;
        }

        private void Awake() =>
            _progressWatchersService.RegisterWatchers(gameObject);

        public void LoadProgress(PlayerProgress progress)
        {
            if (!progress.WorldProgressData.CutsceneData.FlowerWasPicked)
                return;

            _animator.gameObject.SetActive(false);
            _finishedDebris.gameObject.SetActive(true);
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

            _progressWatchersService.Release(this);
            _cutSceneService.OnCutsceneActiveChanged -= CutSceneStarted;
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