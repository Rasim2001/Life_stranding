using Infastructure.Data;
using Infastructure.Services.Defeat;
using Infastructure.Services.ProgressWatchers;
using Infastructure.Services.Registries.FlowerRegistry;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.Services.SaveLoadService;
using Infastructure.Services.Tasks;
using Infastructure.StaticData.GlobalWater;
using Infastructure.StaticData.StaticDataService;
using PickupObjects.PickUpOnPlatform.FlowerManagement;
using SpiderController;
using UnityEngine;
using Zenject;

namespace WaterSystem
{
    public class Water : MonoBehaviour, ISavedProgress
    {
        private ITasksService _tasksService;
        private IStaticDataService _staticDataService;
        private IDefeatWindowService _defeatWindowService;
        private IProgressWatchersService _progressWatchersService;
        private ISpiderRegistryService _spiderRegistryService;
        private IFlowerRegistryService _flowerRegistryService;

        private WaterStaticData WaterStaticData => _staticDataService.WaterStaticData;

        private Spider Spider => _spiderRegistryService.Spider;
        private Flower Flower => _flowerRegistryService.Flower;

        private float _farSpeed;
        private float _nearSpeed;
        private float _actualSpeed;
        private bool _isStartingMove;


        [Inject]
        public void Construct(
            ITasksService tasksService,
            IStaticDataService staticDataService,
            IDefeatWindowService defeatWindowService,
            IProgressWatchersService progressWatchersService,
            ISpiderRegistryService spiderRegistryService,
            IFlowerRegistryService flowerRegistryService)
        {
            _flowerRegistryService = flowerRegistryService;
            _spiderRegistryService = spiderRegistryService;
            _progressWatchersService = progressWatchersService;
            _defeatWindowService = defeatWindowService;
            _staticDataService = staticDataService;
            _tasksService = tasksService;
        }

        private void Awake() =>
            _progressWatchersService.RegisterWatchers(gameObject);

        private void Start()
        {
            _tasksService.AllTasksCompleted += AllTaskCompleted;

            _farSpeed = WaterStaticData.FarSpeed;
            _nearSpeed = WaterStaticData.NearSpeed;
        }

        public void LoadProgress(PlayerProgress progress)
        {
            if (progress.WorldProgressData.WaterData.WaterPosition != null)
            {
                transform.position = progress.WorldProgressData.WaterData.WaterPosition.AsUnityVector();

                _isStartingMove = true;
            }
        }

        public void UpdateProgress(PlayerProgress progress) =>
            progress.WorldProgressData.WaterData.WaterPosition = transform.position.AsVectorData();

        private void OnDestroy()
        {
            _progressWatchersService.Release(this);

            _tasksService.AllTasksCompleted -= AllTaskCompleted;
        }


        private void AllTaskCompleted()
        {
            Debug.Log("AllTaskCompleted");

            _isStartingMove = true;
        }


        private void Update()
        {
            if (_isStartingMove == false || Spider == null || Flower == null)
                return;

            if (transform.position.y - Spider.transform.position.y > WaterStaticData.DistanceBetweenSpiderToDefeat ||
                transform.position.y - Flower.transform.position.y > WaterStaticData.DistanceBetweenSpiderToDefeat)
            {
                _isStartingMove = false;

                _defeatWindowService.OpenDefeatWindow();
            }

            _actualSpeed = Mathf.Abs(Spider.transform.position.y - transform.position.y) >
                           WaterStaticData.DistanceToSwitchSpeed
                ? _farSpeed
                : _nearSpeed;

            transform.Translate(Vector3.up * (_actualSpeed * Time.deltaTime));
        }
    }
}