using DG.Tweening;
using Infastructure.Localization;
using Localization;
using UI.Buttons;
using UnityEngine;
using Zenject;

namespace UI
{
    public class LanguageSwitcherUI : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private LanguageSwitcherArrowUI _leftArrow;
        [SerializeField] private LanguageSwitcherArrowUI _rightArrow;

        private const float PreferredWidthElement = 180f;

        private ButtonSelectedBaseUI _buttonSelection;
        private Tween _moveXTween;

        private readonly LanguageId[] _languageIds = { LanguageId.EN, LanguageId.RU };
        private int _languageIndex;

        private float _startX;

        private ILocalizationService _localizationService;

        [Inject]
        public void Construct(ILocalizationService localizationService) =>
            _localizationService = localizationService;

        private void Awake()
        {
            _buttonSelection = _content.GetComponent<ButtonSelectedBaseUI>();
            _startX = _content.anchoredPosition.x;

            _languageIndex = System.Array.IndexOf(_languageIds, _localizationService.CurrentLanguage);
            if (_languageIndex < 0)
                _languageIndex = 0;

            SnapToIndex(_languageIndex);
        }

        private void Start()
        {
            _leftArrow.OnClickHappened += MoveLeft;
            _rightArrow.OnClickHappened += MoveRight;
        }

        private void OnDestroy()
        {
            _leftArrow.OnClickHappened -= MoveLeft;
            _rightArrow.OnClickHappened -= MoveRight;

            _moveXTween?.Kill();
            _moveXTween = null;
        }

        private void Update()
        {
            if (_buttonSelection == null || !_buttonSelection.IsSelected)
                return;

            if (Input.GetKeyDown(KeyCode.A))
                MoveLeft();
            else if (Input.GetKeyDown(KeyCode.D))
                MoveRight();
        }

        private void MoveRight()
        {
            if (!CanChangeIndex(+1))
                return;

            _languageIndex++;
            _rightArrow.Select();

            ApplyIndexChange();
        }

        private void MoveLeft()
        {
            if (!CanChangeIndex(-1))
                return;

            _languageIndex--;
            _leftArrow.Select();

            ApplyIndexChange();
        }

        private bool CanChangeIndex(int delta)
        {
            int newIndex = _languageIndex + delta;
            return newIndex >= 0 && newIndex < _languageIds.Length;
        }

        private void ApplyIndexChange()
        {
            float targetX = GetPositionForIndex(_languageIndex);

            _moveXTween?.Kill();
            _moveXTween = _content
                .DOAnchorPosX(targetX, 0.25f)
                .SetEase(Ease.OutCubic);

            _localizationService.CurrentLanguage = _languageIds[_languageIndex];
        }

        private void SnapToIndex(int index)
        {
            float x = GetPositionForIndex(index);
            _content.anchoredPosition = new Vector2(x, _content.anchoredPosition.y);
        }

        private float GetPositionForIndex(int index) =>
            _startX - PreferredWidthElement * index;
    }
}