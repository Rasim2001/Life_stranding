using Infastructure.Services.CheckPoint;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController;
using UnityEngine;

namespace Infastructure.Services.SpiderTrack
{
    public class SpiderTrackService : ISpiderTrackService
    {
        public Spider Spider { get; set; }
        public Flower Flower { get; set; }

        private readonly IBiospherePointService _biospherePointService;

        public SpiderTrackService(IBiospherePointService biospherePointService) =>
            _biospherePointService = biospherePointService;

        public string GetDistanceToGoal()
        {
            float distance = Vector3.Distance(Spider.transform.position,
                _biospherePointService.PointIndicator.transform.position);
            int meters = Mathf.FloorToInt(distance);
            string formattedMeters = $"<font-weight=500>{meters:D3}<size=15>м</size></font-weight>";

            return formattedMeters;
        }
    }
}