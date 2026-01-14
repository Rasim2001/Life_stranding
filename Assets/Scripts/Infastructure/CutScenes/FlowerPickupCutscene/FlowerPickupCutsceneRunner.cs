using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.Services.CameraProvider;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using Zenject;

namespace Infastructure.CutScenes.FlowerPickupCutscene
{
    public class FlowerPickupCutsceneRunner : MonoBehaviour, ICutSceneRunner
    {
        [SerializeField] private PlayableDirector _playableDirector;
        [SerializeField] private CinemachineCamera _cutSceneCamera;
        public float BlendingTime => _brain.DefaultBlend.Time;

        private UniTaskCompletionSource _tcs;
        private BorderCutsceneAnimator _borderCutsceneAnimator;
        private ICameraProviderService _cameraProviderService;

        private CinemachineBrain _brain;


        [Inject]
        public void Construct(ICameraProviderService cameraProviderService) =>
            _cameraProviderService = cameraProviderService;

        private void Awake() =>
            _borderCutsceneAnimator = GetComponentInChildren<BorderCutsceneAnimator>();

        private void Start() =>
            _brain = _cameraProviderService.CameraTransform.GetComponent<CinemachineBrain>();

        public UniTask PlayAsync(CancellationToken ct = default)
        {
            _borderCutsceneAnimator.PlayAnimation();

            _tcs = new UniTaskCompletionSource();

            _playableDirector.stopped += OnStopped;

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
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

            _cutSceneCamera.gameObject.SetActive(false);
            _tcs.TrySetResult();
        }

        private void OnDisable() =>
            _playableDirector.Stop();
    }
}