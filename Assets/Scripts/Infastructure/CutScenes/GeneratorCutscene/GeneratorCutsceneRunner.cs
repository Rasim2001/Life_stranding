using System.Threading;
using Cysharp.Threading.Tasks;
using Infastructure.CutScenes.FlowerPickupCutscene;
using Infastructure.Services.CameraProvider;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using Zenject;

namespace Infastructure.CutScenes.GeneratorCutscene
{
    public class GeneratorCutsceneRunner : MonoBehaviour, ICutSceneRunner
    {
        [SerializeField] private PlayableDirector _playableDirector;

        private UniTaskCompletionSource _tcs;

        private ICameraProviderService _cameraProviderService;
        private CinemachineBrain _brain;

        public float BlendingTime => _brain.DefaultBlend.Time;

        [Inject]
        public void Construct(ICameraProviderService cameraProviderService) =>
            _cameraProviderService = cameraProviderService;

        private void Start() =>
            _brain = _cameraProviderService.CameraTransform.GetComponent<CinemachineBrain>();

        public UniTask PlayAsync(CancellationToken ct = default)
        {
            _tcs = new UniTaskCompletionSource();

            return _tcs.Task;
        }
    }
}