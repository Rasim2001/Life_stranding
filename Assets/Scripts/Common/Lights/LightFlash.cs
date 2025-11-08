using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Infastructure.Services.SpiderTrack;
using UnityEngine;
using Zenject;

namespace Common.Lights
{
    public class LightFlash : MonoBehaviour
    {
        [SerializeField] private float _minIntensity = 0.05f;
        [SerializeField] private float _maxIntensity = 2f;
        [SerializeField] private float _duration = 0.5f;

        private Light _light;
        private Tween _blinkTween;

        private void Awake() =>
            _light = GetComponent<Light>();

        private void Start() =>
            StartBlinking();

        private void StartBlinking()
        {
            _blinkTween = DOTween
                .To(() => _light.intensity, x => _light.intensity = x, _maxIntensity, _duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InFlash);
        }

        private void OnDestroy() =>
            _blinkTween?.Kill();
    }
}