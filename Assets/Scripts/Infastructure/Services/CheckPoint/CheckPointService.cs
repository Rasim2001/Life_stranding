using Infastructure.StaticData;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Infastructure.Services.CheckPoint
{
    public class CheckPointService : ICheckPointService
    {
        private readonly IStaticDataService _staticDataService;
        public Transform PointIndicator { get; set; }
        private string ActiveSceneName => SceneManager.GetActiveScene().name;
        private GameData GameData => _staticDataService.GameStaticData.GameDatas[ActiveSceneName];

        private int _count = 0;

        public CheckPointService(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;


        public void GoToNextPoint()
        {
            if (_count >= GameData.CheckPoints.Count)
                return;

            PointIndicator.position = GameData.CheckPoints[_count].WorldPosition;
            _count++;
        }
    }
}