using Infastructure.Services.PlayerInput;
using Infastructure.Services.PlayerInput.InputSourceRealization;
using UnityEngine;
using Zenject;

namespace Infastructure.Localization
{
    public class StaticLocalizationSprite : MonoBehaviour
    {
        [SerializeField] private GameObject _gamepad;
        [SerializeField] private GameObject _keyboard;

        private IInputService _inputService;

        [Inject]
        public void Construct(IInputService inputService) =>
            _inputService = inputService;

        private void Start()
        {
            bool isGamepadActive = _inputService.IsActiveSource<JoystickInputSource>();

            _gamepad.SetActive(isGamepadActive);
            _keyboard.SetActive(!isGamepadActive);

            _inputService.OnJoystickEnableHappend += JoystickEnabled;
            _inputService.OnJoystickDisableHappend += JoystickDisabled;
        }

        private void OnDestroy()
        {
            _inputService.OnJoystickEnableHappend -= JoystickEnabled;
            _inputService.OnJoystickDisableHappend -= JoystickDisabled;
        }

        private void JoystickDisabled()
        {
            _gamepad.SetActive(false);
            _keyboard.SetActive(true);
        }

        private void JoystickEnabled(IInputSource obj)
        {
            _gamepad.SetActive(true);
            _keyboard.SetActive(false);
        }
    }
}