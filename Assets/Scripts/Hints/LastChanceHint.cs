using System;
using Infastructure.Services.Hint;
using UnityEngine;
using Zenject;

namespace Hints
{
    public class LastChanceHint : MonoBehaviour
    {
        [SerializeField] private GameObject _container;

        private IHintReceiverService _hintReceiverService;

        [Inject]
        public void Construct(IHintReceiverService hintReceiverService) =>
            _hintReceiverService = hintReceiverService;

        private void Awake() =>
            Hide();

        private void Start()
        {
            _hintReceiverService.OnLastChanceHintShowHappened += Show;
            _hintReceiverService.OnLastChanceHintHideHappened += Hide;
        }

        private void OnDestroy()
        {
            _hintReceiverService.OnLastChanceHintShowHappened -= Show;
            _hintReceiverService.OnLastChanceHintHideHappened -= Hide;
        }

        private void Show()
        {
            Debug.Log("Show");

            _container.SetActive(true);
        }

        private void Hide() =>
            _container.SetActive(false);
    }
}