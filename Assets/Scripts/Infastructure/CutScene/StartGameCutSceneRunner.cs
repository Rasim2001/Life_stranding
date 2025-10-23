using System;
using System.Collections.Generic;
using System.Linq;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.VolumeProfiles;
using Sirenix.Utilities;
using SpiderController;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using Zenject;

namespace Infastructure.CutScene
{
    public class StartGameCutSceneRunner : MonoBehaviour
    {
        private const string CinemachineTrack = "Cinemachine Track";

        [SerializeField] private Volume _globalVolume;
        [SerializeField] private SplineContainer splineContainer;
        [SerializeField] private float minDistance = 0.5f;

        [SerializeField] private CinemachineCamera[] _cameras;
        private VolumeProfilesStaticData VolumeProfilesStaticData => _staticDataService.VolumeProfilesStaticData;

        private IInputService _inputService;
        private ICutSceneService _cutSceneService;
        private IStaticDataService _staticDataService;

        private Spline _spline;
        private Vector3 _lastPoint;

        private Spider _spider;
        private CutSceneInputSource _cutSceneInputSource;
        private PlayableDirector _playableDirector;
        private CinemachineBrain _mainBrainCamera;

        [Inject]
        public void Construct(
            IInputService inputService,
            ICutSceneService cutSceneService,
            IStaticDataService staticDataService)
        {
            _staticDataService = staticDataService;
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
            _spline = splineContainer.Spline;
            AddPoint(_spider.transform.position);

            _cutSceneService.IsActive = true;

            List<PlayableBinding> playableBindings = _playableDirector.playableAsset.outputs
                .Where(x => x.streamName == CinemachineTrack).ToList();

            playableBindings.ForEach(x => _playableDirector.SetGenericBinding(x.sourceObject, _mainBrainCamera));

            _playableDirector.stopped += StopCutScene;
        }

        private void OnDestroy() =>
            _playableDirector.stopped -= StopCutScene;

        private void StopCutScene(PlayableDirector obj)
        {
            _mainBrainCamera.UpdateMethod = CinemachineBrain.UpdateMethods.FixedUpdate;
            _inputService.SetInputSource(new PlayerInputSource());

            _cutSceneService.IsActive = false;
        }

        private void Update()
        {
            if (_spider == null)
                return;

            Vector3 pos = _spider.transform.position;
            pos.y = pos.y;

            if ((pos - _lastPoint).sqrMagnitude >= minDistance * minDistance)
                AddPoint(pos);
        }

        public void FastRunMovingSignal() =>
            _cutSceneInputSource.IsLeftShiftPressed = true;

        public void StopFastRunMovingSignal() =>
            _cutSceneInputSource.IsLeftShiftUp = true;

        public void ShakeCamera() =>
            _spider.OnShakeCameraHappened?.Invoke(20);

        private void AddPoint(Vector3 position)
        {
            BezierKnot knot = new BezierKnot(position: position);
            _spline.Add(knot);
            _lastPoint = position;
        }
    }
}