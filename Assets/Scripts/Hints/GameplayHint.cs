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

        private IHintService _hintService;


        [Inject]
        public void Construct(IHintService hintService) =>
            _hintService = hintService;

        protected override void Start()
        {
            base.Start();

            _hintService.OnProductHint += ShowProduct;
            _hintService.OnCheckpointHint += ShowCheckpointHint;
            _hintService.OnGeneratorHint += ShowGeneratorHint;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            _hintService.OnProductHint -= ShowProduct;
            _hintService.OnCheckpointHint -= ShowCheckpointHint;
            _hintService.OnGeneratorHint -= ShowGeneratorHint;
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