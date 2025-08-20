using Cysharp.Threading.Tasks;
using Infastructure.Services.PlayerInput;
using UnityEngine;
using Zenject;

namespace Infastructure.CutScene
{
    public class StartGameCutSceneRunner : MonoBehaviour
    {
        private IInputService _inputService;
        private CutSceneInputSource _cutSceneInputSource;

        [Inject]
        public void Construct(IInputService inputService) =>
            _inputService = inputService;

        private void Awake()
        {
            _cutSceneInputSource = new CutSceneInputSource();
            _inputService.SetInputSource(_cutSceneInputSource);
        }

        public void MoveTowardSignal() =>
            _cutSceneInputSource.InputVector += Vector3.forward;

        public void StopMoveSignal() =>
            _cutSceneInputSource.InputVector = Vector3.zero;

        public void TurnRightSignal() =>
            _cutSceneInputSource.InputVector += -Vector3.left;

        public void TurnLeftSignal() =>
            _cutSceneInputSource.InputVector += Vector3.left;

        public void JumpSignal()
        {
            Debug.Log("JumpSignal");

            JumpAsync().Forget();
        }

        private async UniTask JumpAsync()
        {
            _cutSceneInputSource.JumpPressed = true;

            await UniTask.DelayFrame(2);

            _cutSceneInputSource.JumpPressed = false;
        }
    }
}