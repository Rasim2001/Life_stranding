using DistantLands.Cozy;
using Infastructure.Services.CutScene;
using UnityEngine;
using Zenject;

namespace Common
{
    public class CozyWeatherChanger : MonoBehaviour
    {
        private CozyWeather _cozyWeather;

        private ICutSceneService _cutSceneService;

        [Inject]
        public void Construct(ICutSceneService cutSceneService) =>
            _cutSceneService = cutSceneService;

        private void Awake() =>
            _cozyWeather = GetComponent<CozyWeather>();

        private void Start()
        {
            _cozyWeather.timeModule.currentTime.hours = 5;
            _cozyWeather.timeModule.currentTime.minutes = 45;

            _cutSceneService.OnWeatherChanged += ChangeWeather;
        }

        private void OnDestroy() =>
            _cutSceneService.OnWeatherChanged -= ChangeWeather;

        private void ChangeWeather()
        {
            _cozyWeather.timeModule.currentTime.hours = 7;
            _cozyWeather.timeModule.currentTime.minutes = 40;

            _cozyWeather.timeModule.perennialProfile.pauseTime = false;
        }
    }
}