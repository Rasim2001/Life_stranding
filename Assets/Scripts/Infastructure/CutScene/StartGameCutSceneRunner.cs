using Infastructure.Services.PlayerInput;
using UnityEngine;
using Zenject;

namespace Infastructure.CutScene
{
    public class StartGameCutSceneRunner : MonoBehaviour
    {
        private IInputService _inputService;

        [Inject]
        public void Construct(IInputService inputService) =>
            _inputService = inputService;

        private void Awake() =>
            _inputService.SetInputSource(new CutSceneInputSource());
    }
}