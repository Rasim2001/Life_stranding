using Infastructure.StaticData;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;

namespace Infastructure.Services.CheckPoint
{
    public class CheckPointService : ICheckPointService
    {
        public Transform PointIndicator { get; set; }
        private GameStaticData GameStaticData => _staticDataService.GameStaticData;

        private readonly IStaticDataService _staticDataService;

        private int _count = 0;

        public CheckPointService(IStaticDataService staticDataService) =>
            _staticDataService = staticDataService;


        public void GoToNextPoint()
        {
            if (_count >= GameStaticData.CheckPoints.Count)
                return;

            PointIndicator.position = GameStaticData.CheckPoints[_count];
            _count++;
        }
    }
}