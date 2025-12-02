using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.Hint;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Zenject;

namespace Hints
{
    public class GameplayHint : HintBase
    {
        private const float ShowTime = 2;
        private const float AnchorPositionX = -20;

        [SerializeField] private TextMeshProUGUI _description;

        private IHintReceiverService _hintReceiverService;


        [Inject]
        public void Construct(IHintReceiverService hintReceiverService) =>
            _hintReceiverService = hintReceiverService;

        protected override void Start()
        {
            base.Start();

            _hintReceiverService.OnProductHint += ShowProduct;
            _hintReceiverService.OnCheckpointHint += ShowCheckpointHint;
            _hintReceiverService.OnGeneratorHint += ShowGeneratorHint;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _hintReceiverService.OnProductHint -= ShowProduct;
            _hintReceiverService.OnCheckpointHint -= ShowCheckpointHint;
            _hintReceiverService.OnGeneratorHint -= ShowGeneratorHint;
        }

        private void ShowProduct()
        {
            _description.text = "Одновременно можно брать только один тип носителя";

            Show(ShowTime, AnchorPositionX);
        }

        private void ShowCheckpointHint()
        {
            _description.text = "Чекпоинт работает только с колбой";

            Show(ShowTime, AnchorPositionX);
        }

        private void ShowGeneratorHint()
        {
            _description.text = "Генератор нужно активировать с перемычкой";

            Show(ShowTime, AnchorPositionX);
        }
    }
}