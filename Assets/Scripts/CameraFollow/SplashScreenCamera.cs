using Infastructure.Services.StartGame;
using UnityEngine;
using Zenject;

namespace CameraFollow
{
    public class SplashScreenCamera : MonoBehaviour
    {
        private IStartGameReceiver _startGameReceiver;

        [Inject]
        public void Construct(IStartGameReceiver startGameReceiver) =>
            _startGameReceiver = startGameReceiver;

        private void Awake() =>
            _startGameReceiver.OnStartGameHappened += DestroyCamera;

        private void OnDestroy() =>
            _startGameReceiver.OnStartGameHappened -= DestroyCamera;

        private void DestroyCamera() =>
            Destroy(gameObject);
    }
}