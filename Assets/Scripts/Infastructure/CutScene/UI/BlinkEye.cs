using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Infastructure.CutScene.UI
{
    public class BlinkEye : MonoBehaviour
    {
        [SerializeField] private Volume _globalVolume;

        [SerializeField] private RectTransform _topImageRect;
        [SerializeField] private RectTransform _downImageRect;

        [SerializeField] private AnimationCurve _openEaseCurve;
        [SerializeField] private Ease _blinkEasy;

        [SerializeField] private float _blinkDurationDown;
        [SerializeField] private float _blinkDurationUp;
        [SerializeField] private float _holdBlinkDuration;
        [SerializeField] private float _distanceEye;

        private float _posY;

        private Tween _topTween;
        private Tween _downTween;
        private Tween _blurTween;

        private Sequence _blinkSequence;

        private DepthOfField _depthOfField;

        private void Awake()
        {
            _globalVolume.profile.TryGet(out _depthOfField);
            _depthOfField.active = false;
        }

        public void Blink()
        {
            _blinkSequence?.Kill();
            _blurTween?.Kill();
            _topImageRect?.DOKill();
            _downImageRect?.DOKill();

            BluerActivate();

            _blinkSequence = DOTween.Sequence();

            _blinkSequence.Append(_topImageRect.DOAnchorPosY(_distanceEye, _blinkDurationDown).SetEase(_blinkEasy));
            _blinkSequence.Join(_downImageRect.DOAnchorPosY(-_distanceEye, _blinkDurationDown).SetEase(_blinkEasy));

            _blinkSequence.AppendInterval(_holdBlinkDuration);

            _blinkSequence.Append(
                _topImageRect.DOAnchorPosY(800, _blinkDurationUp).SetEase(_openEaseCurve));
            _blinkSequence.Join(
                _downImageRect.DOAnchorPosY(-800, _blinkDurationUp).SetEase(_openEaseCurve));

            _blinkSequence.Play();
        }

        private void BluerActivate()
        {
            _depthOfField.active = true;
            _depthOfField.focalLength.value = 16;

            _blurTween = DOTween.To(
                    () => _depthOfField.focalLength.value,
                    x => _depthOfField.focalLength.value = x,
                    0,
                    _blinkDurationUp)
                .SetEase(_openEaseCurve)
                .SetDelay(_holdBlinkDuration)
                .OnComplete(() => _depthOfField.active = false);
        }
    }
}