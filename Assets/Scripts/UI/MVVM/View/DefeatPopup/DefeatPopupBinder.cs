using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.CheckPoint;
using Infastructure.Services.Pause;
using Infastructure.Services.Registries.SpiderRegistry;
using Infastructure.States;
using TMPro;
using UI.MVVM.Base;
using UnityEngine;
using Zenject;

namespace UI.MVVM.View.DefeatPopup
{
    public class DefeatPopupBinder : PopupBinder<DefeatPopupViewModel>
    {
        private static readonly int StartTrigger = Animator.StringToHash("StartTrigger");

        [SerializeField] private TextMeshProUGUI _distanceToGoalText;
        [SerializeField] private Transform _container;
        [SerializeField] private FramePiecesUI _framePiecesUI;
        [SerializeField] private Animator _flowerAnimator;

        private Transform SpiderTransform => _spiderRegistryService.Spider.transform;
        private Transform BiospherePointIndicator => _biospherePointService.PointIndicator.transform;

        private IPauseService _pauseService;
        private IStateMachine _stateMachine;
        private IBiospherePointService _biospherePointService;
        private ISpiderRegistryService _spiderRegistryService;

        private Tween _containerRotateTween;
        private Coroutine _flowerCoroutine;

        [Inject]
        public void Construct(
            IPauseService pauseService,
            IStateMachine stateMachine,
            IBiospherePointService biospherePointService,
            ISpiderRegistryService spiderRegistryService)
        {
            _spiderRegistryService = spiderRegistryService;
            _biospherePointService = biospherePointService;
            _stateMachine = stateMachine;
            _pauseService = pauseService;
        }


        protected override void Start()
        {
            base.Start();

            _pauseService.StartPause(gameObject.name);

            StopCoroutine();

            _flowerCoroutine = StartCoroutine(StartFlowerAnimation());
            _framePiecesUI.MoveFramePiecesAsync().Forget();
            _containerRotateTween = _container.DORotate(Vector3.zero, 0.2f).SetUpdate(true);

            _distanceToGoalText.text = GetFormattedDistance();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _containerRotateTween?.Kill();
            _pauseService.StopPause(gameObject.name);

            StopCoroutine();
        }

        protected override void OnCloseButtonClick()
        {
            base.OnCloseButtonClick();

            _stateMachine.Enter<ExitGameLoopState>();
        }

        private IEnumerator StartFlowerAnimation()
        {
            yield return new WaitForSecondsRealtime(0.75f);

            _flowerAnimator.SetTrigger(StartTrigger);
        }

        private void StopCoroutine()
        {
            if (_flowerCoroutine != null)
            {
                StopCoroutine(_flowerCoroutine);
                _flowerCoroutine = null;
            }
        }

        private string GetFormattedDistance()
        {
            float distance = Vector3.Distance(SpiderTransform.position, BiospherePointIndicator.position);
            int meters = Mathf.FloorToInt(distance);
            string formattedMeters = $"<font-weight=500>{meters:D3}<size=15>м</size></font-weight>";

            return formattedMeters;
        }
    }
}