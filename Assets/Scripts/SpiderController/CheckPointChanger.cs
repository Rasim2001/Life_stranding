using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using Infastructure.Services.CheckPoint;
using UnityEngine;

namespace SpiderController
{
    public class CheckPointChanger
    {
        private const string CheckPointLayer = "CheckPoint";

        private readonly float _cooldownDefault = 1;
        private readonly int _layerMask = 1 << LayerMask.NameToLayer(CheckPointLayer);

        private readonly Transform _spiderTransform;
        private readonly ICheckPointService _checkPointService;

        private float _cooldown;

        public CheckPointChanger(Transform spiderTransform, ICheckPointService checkPointService)
        {
            _spiderTransform = spiderTransform;
            _checkPointService = checkPointService;
        }

        public void Update()
        {
            if (_cooldown >= 0)
                _cooldown -= Time.deltaTime;

            bool checkSphere = Physics.CheckSphere(_spiderTransform.position, 4, _layerMask);
            if (checkSphere && _cooldown <= 0)
            {
                _checkPointService.GoToNextPoint();
                _cooldown = _cooldownDefault;
            }
        }
    }
}