using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.CutScenes.FlowerPickupCutscene;
using Infastructure.Services.CameraProvider;
using Infastructure.Services.GeneratorLaunchTracker;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using Zenject;

namespace Infastructure.CutScenes.GeneratorCutscene
{
    public class GeneratorCutsceneRunner : MonoBehaviour, ICutSceneRunner
    {
        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private CinemachineCamera _lastCamera;

        private UniTaskCompletionSource _tcs;

        private ICameraProviderService _cameraProviderService;
        private CinemachineBrain _brain;
        private BorderCutsceneAnimator _borderCutsceneAnimator;
        private IGeneratorLaunchTrackerService _generatorLaunchTrackerService;

        public float BlendingTime => Mathf.Epsilon;

        [Inject]
        public void Construct(ICameraProviderService cameraProviderService,
            IGeneratorLaunchTrackerService generatorLaunchTrackerService)
        {
            _generatorLaunchTrackerService = generatorLaunchTrackerService;
            _cameraProviderService = cameraProviderService;
        }

        private void Awake() =>
            _borderCutsceneAnimator = GetComponentInChildren<BorderCutsceneAnimator>();

        private void Start() =>
            _brain = _cameraProviderService.CameraTransform.GetComponent<CinemachineBrain>();

        private void OnDestroy() =>
            SetBlendEasy();

        public void SetBlendCut() =>
            _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Cut;

        public void SetBlendEasy() =>
            _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseInOut;

        public void LaunchVisualBiosphere() =>
            _generatorLaunchTrackerService.Launch();

        public UniTask PlayAsync(CancellationToken ct = default)
        {
            _borderCutsceneAnimator.PlayAnimation();

            _playableDirector.stopped += OnStopped;
            _tcs = new UniTaskCompletionSource();

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    if (_playableDirector == null)
                        return;

                    _playableDirector.stopped -= OnStopped;
                    _playableDirector.Stop();
                    _tcs.TrySetCanceled(ct);
                });
            }

            _playableDirector.Play();

            return _tcs.Task;
        }

        private void OnStopped(PlayableDirector d)
        {
            _playableDirector.stopped -= OnStopped;

            _lastCamera.gameObject.SetActive(false);
            _tcs.TrySetResult();
        }
    }
}