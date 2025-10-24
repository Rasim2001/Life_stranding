using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Sirenix.Utilities;
using SpiderController;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Splines;
using Zenject;

namespace Infastructure.CutScene
{
    public class StartGameCutSceneRunner : MonoBehaviour
    {
        private const string CinemachineTrack = "Cinemachine Track";

        [SerializeField] private CinemachineCamera _firstCamera;
        [SerializeField] private CinemachineCamera[] _cameras;

        private IInputService _inputService;
        private ICutSceneService _cutSceneService;

        private Spline _spline;
        private Vector3 _lastPoint;

        private Spider _spider;
        private CutSceneInputSource _cutSceneInputSource;
        private PlayableDirector _playableDirector;
        private CinemachineBrain _mainBrainCamera;

        [Inject]
        public void Construct(IInputService inputService, ICutSceneService cutSceneService)
        {
            _inputService = inputService;
            _cutSceneService = cutSceneService;
        }

        public void Initialize(Spider spider)
        {
            _cameras.ForEach(x => x.Follow = spider.transform);
            _spider = spider;
        }


        private void Awake()
        {
            _playableDirector = GetComponent<PlayableDirector>();
            _cutSceneInputSource = _inputService.GetInputSource<CutSceneInputSource>();
            _mainBrainCamera = Camera.main.GetComponent<CinemachineBrain>();
        }


        private void Start()
        {
            _firstCamera.Priority = 100;

            List<PlayableBinding> playableBindings = _playableDirector.playableAsset.outputs
                .Where(x => x.streamName == CinemachineTrack).ToList();

            playableBindings.ForEach(x => _playableDirector.SetGenericBinding(x.sourceObject, _mainBrainCamera));

            _playableDirector.stopped += StopCutScene;
            _cutSceneService.OnSkipHappened += SkipCutscene;
        }


        private void OnDestroy()
        {
            _playableDirector.stopped -= StopCutScene;
            _cutSceneService.OnSkipHappened -= SkipCutscene;
        }

        private void StopCutScene(PlayableDirector obj) =>
            SkipCustom();

        private void SkipCutscene()
        {
            SkipCustom();

            _playableDirector.time = _playableDirector.duration;
            _playableDirector.Stop();
        }

        private void SkipCustom()
        {
            _mainBrainCamera.UpdateMethod = CinemachineBrain.UpdateMethods.FixedUpdate;
            _inputService.SetInputSource(new PlayerInputSource());

            _firstCamera.Priority = 0;
        }


        public void FastRunMovingSignal() =>
            _cutSceneInputSource.IsLeftShiftPressed = true;

        public void StopFastRunMovingSignal() =>
            _cutSceneInputSource.IsLeftShiftUp = true;

        public void ShakeCamera() =>
            _spider.OnShakeCameraHappened?.Invoke(20);
    }
}