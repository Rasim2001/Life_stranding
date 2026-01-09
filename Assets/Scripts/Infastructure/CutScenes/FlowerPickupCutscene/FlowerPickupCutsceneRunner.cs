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

        private BorderCutsceneUI[] _borders;
        private UniTaskCompletionSource _tcs;
        private ICameraProviderService _cameraProviderService;

        private Coroutine _coroutine;
        private CinemachineBrain _brain;


        [Inject]
        public void Construct(ICameraProviderService cameraProviderService) =>
            _cameraProviderService = cameraProviderService;

        private void Awake() =>
            _borders = GetComponentsInChildren<BorderCutsceneUI>();

        private void Start() =>
            _brain = _cameraProviderService.CameraTransform.GetComponent<CinemachineBrain>();

        private void OnDestroy()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);
        }

        public float BlendingTime => _brain.DefaultBlend.Time;

        public UniTask PlayAsync(CancellationToken ct = default)
        {
            foreach (BorderCutsceneUI border in _borders)
                border.Play();

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