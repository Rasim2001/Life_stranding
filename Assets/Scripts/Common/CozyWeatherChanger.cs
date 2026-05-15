using DistantLands.Cozy;
using Infastructure.Services.StartGame;
using UnityEngine;
using Zenject;

namespace Common
{
    public class CozyWeatherChanger : MonoBehaviour
    {
        private CozyWeather _cozyWeather;

        private IStartGameReceiver _startGameReceiver;

        [Inject]
        public void Construct(IStartGameReceiver startGameReceiver) =>
            _startGameReceiver = startGameReceiver;


        private void Awake()
        {
            _cozyWeather = GetComponent<CozyWeather>();

            _cozyWeather.timeModule.perennialProfile.pauseTime = true;

            _cozyWeather.timeModule.currentTime.hours = 5;
            _cozyWeather.timeModule.currentTime.minutes = 45;

            _startGameReceiver.OnStartGameHappened += ChangeWeather;
        }

        private void OnDestroy() =>
            _startGameReceiver.OnStartGameHappened -= ChangeWeather;

        private void ChangeWeather()
        {
            _cozyWeather.timeModule.currentTime.hours = 10;
            _cozyWeather.timeModule.currentTime.minutes = 30;

            //_cozyWeather.timeModule.perennialProfile.pauseTime = false;
        }
    }
}