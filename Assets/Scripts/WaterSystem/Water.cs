using Infastructure.Services.Defeat;
using Infastructure.Services.SpiderTrack;
using Infastructure.Services.TaskPopupChecker;
using Infastructure.StaticData.GlobalWater;
using Infastructure.StaticData.StaticDataService;
using UnityEngine;
using Zenject;

namespace WaterSystem
{
    public class Water : MonoBehaviour
    {
        private ISpiderTrackService _trackService;
        private ITaskPopupCheckerService _taskPopupCheckerService;
        private IStaticDataService _staticDataService;
        private IDefeatWindowService _defeatWindowService;

        private WaterStaticData WaterStaticData => _staticDataService.WaterStaticData;

        private Transform _spiderTransform;
        private Transform _flowerTransform;

        private float _farSpeed;
        private float _nearSpeed;
        private float _actualSpeed;
        private bool _isStartingMove;


        [Inject]
        public void Construct(
            ISpiderTrackService trackService,
            ITaskPopupCheckerService taskPopupCheckerService,
            IStaticDataService staticDataService,
            IDefeatWindowService defeatWindowService)
        {
            _defeatWindowService = defeatWindowService;
            _staticDataService = staticDataService;
            _taskPopupCheckerService = taskPopupCheckerService;
            _trackService = trackService;
        }

        private void Start()
        {
            _taskPopupCheckerService.AllTasksCompleted += AllTaskCompleted;

            _farSpeed = WaterStaticData.FarSpeed;
            _nearSpeed = WaterStaticData.NearSpeed;

            _spiderTransform = _trackService.Spider.transform;
            _flowerTransform = _trackService.Flower.transform;
        }

        private void OnDestroy() =>
            _taskPopupCheckerService.AllTasksCompleted -= AllTaskCompleted;

        private void AllTaskCompleted() =>
            _isStartingMove = true;

        private void Update()
        {
            if (_isStartingMove == false || _spiderTransform == null || _flowerTransform == null)
                return;

            if (transform.position.y - _spiderTransform.position.y > WaterStaticData.DistanceBetweenSpiderToDefeat ||
                transform.position.y - _flowerTransform.position.y > WaterStaticData.DistanceBetweenSpiderToDefeat)
            {
                _isStartingMove = false;

                _defeatWindowService.OpenDefeatWindow();
            }


            _actualSpeed = Mathf.Abs(_spiderTransform.transform.position.y - transform.position.y) >
                           WaterStaticData.DistanceToSwitchSpeed
                ? _farSpeed
                : _nearSpeed;

            transform.Translate(Vector3.up * (_actualSpeed * Time.deltaTime));
        }
    }
}