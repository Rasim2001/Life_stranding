using System;
using Infastructure.Services.CheckPoint;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController;
using UnityEngine;

namespace Infastructure.Services.SpiderTrack
{
    public class SpiderTrackService : ISpiderTrackService
    {
        public event Action OnSpiderInitialized;
        public event Action OnFlowerInitialized;

        public Spider Spider
        {
            get => _spider;
            set
            {
                _spider = value;
                if (_spider != null)
                    OnSpiderInitialized?.Invoke();
            }
        }
        public Flower Flower
        {
            get => _flower;
            set
            {
                _flower = value;
                if (_flower != null)
                    OnFlowerInitialized?.Invoke();
            }
        }

        private readonly IBiospherePointService _biospherePointService;

        private Flower _flower;
        private Spider _spider;

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