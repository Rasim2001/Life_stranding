using System;
using Cysharp.Threading.Tasks;
using Infastructure.Services.CutScene;
using Infastructure.Services.PlayerInput;
using Infastructure.StaticData.StaticDataService;
using Infastructure.StaticData.VolumeProfiles;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Infastructure.CutScene
{
    public class StartGameCutSceneRunner : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera _secondCamera;
        [SerializeField] private CinemachineCamera _fourthCamera;

        private VolumeProfilesStaticData VolumeProfilesStaticData => _staticDataService.VolumeProfilesStaticData;

        private IInputService _inputService;
        private ICutSceneService _cutSceneService;
        private CutSceneInputSource _cutSceneInputSource;
        private IStaticDataService _staticDataService;
        private Volume _globalVolume;

        [Inject]
        public void Construct(Volume globalVolume,
            IInputService inputService,
            ICutSceneService cutSceneService,
            IStaticDataService staticDataService)
        {
            _globalVolume = globalVolume;
            _staticDataService = staticDataService;
            _cutSceneService = cutSceneService;
            _inputService = inputService;
        }

        public void Initialize(Transform spiderTransform)
        {
            _secondCamera.Follow = spiderTransform;
            _fourthCamera.Follow = spiderTransform;
        }

        private void Awake()
        {
            _cutSceneInputSource = new CutSceneInputSource();
            _inputService.SetInputSource(_cutSceneInputSource);

            _inputService.SetInputSource(new PlayerInputSource());
        }


        private void Start()
        {
            Debug.Log("Start");

            _cutSceneService.IsActive = true;
        }

        public void SetFirstCameraVolume() =>
            _globalVolume.profile = VolumeProfilesStaticData.StartGameFirstCameraProfile;

        public void SetDefaultVolume() =>
            _globalVolume.profile = VolumeProfilesStaticData.DefaultProfile;

        public void MoveTowardSignal() =>
            _cutSceneInputSource.InputVector += Vector3.forward;

        public void StopMoveSignal() =>
            _cutSceneInputSource.InputVector = Vector3.zero;

        public void TurnRightSignal() =>
            _cutSceneInputSource.InputVector += -Vector3.left;

        public void TurnLeftSignal() =>
            _cutSceneInputSource.InputVector += Vector3.left;

        public void JumpSignal() =>
            _cutSceneInputSource.JumpPressed = true;

        public void FastRunMovingSignal()
        {
            Debug.Log("IsLeftShiftPressed");

            _cutSceneInputSource.IsLeftShiftPressed = true;
        }

        public void StopFastRunMovingSignal() =>
            _cutSceneInputSource.IsLeftShiftUp = true;
    }
}