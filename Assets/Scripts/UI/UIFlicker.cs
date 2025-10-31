using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIFlicker
    {
        private readonly Dictionary<Image, (Tween tween, float originalAlpha)> _map = new();

        public async UniTask FlickerFor(Image[] images, FlickerParams p, float durationSeconds,
            CancellationToken ct = default)
        {
            StartFlicker(images, p);

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(durationSeconds),
                    ignoreTimeScale: p.ignoreTimeScale,
                    cancellationToken: ct);
            }
            finally
            {
                StopFlicker(p.restoreOnStop);
            }
        }

        private void StartFlicker(Image[] images, FlickerParams p)
        {
            StopFlicker();

            if (images == null)
                return;

            foreach (Image img in images)
            {
                if (img == null)
                    continue;

                var col = img.color;
                float original = col.a;
                _map[img] = (null, original);

                col.a = Mathf.Clamp01(p.maxAlpha);
                img.color = col;

                float delay = p.desync ? UnityEngine.Random.Range(0f, p.halfPeriod) : 0f;


                Tween t = img.DOFade(p.minAlpha, p.halfPeriod)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(p.ease)
                    .SetUpdate(p.ignoreTimeScale)
                    .SetDelay(delay)
                    .SetLink(img.gameObject, LinkBehaviour.KillOnDestroy);

                _map[img] = (t, original);
            }
        }

        private void StopFlicker(bool restoreAlpha = true)
        {
            foreach (var kv in _map)
            {
                Image img = kv.Key;
                (Tween tween, float original) = kv.Value;

                tween?.Kill();
                if (restoreAlpha && img != null)
                {
                    Color c = img.color;
                    c.a = original;
                    img.color = c;
                }
            }

            _map.Clear();
        }
    }

    [Serializable]
    public struct FlickerParams
    {
        [Range(0f, 1f)] public float minAlpha; // во что «гасим»
        [Range(0f, 1f)] public float maxAlpha; // во что «зажигаем»
        [Min(0.01f)] public float halfPeriod; // полупериод (сек) — время от min до max
        public Ease ease; // кривая яркости (туда-обратно)
        public bool desync; // случайный сдвиг фазы на каждый Image
        public bool ignoreTimeScale; // использовать UnscaledTime
        public bool restoreOnStop; // вернуть исходную альфу при Stop

        public static FlickerParams Default => new FlickerParams
        {
            minAlpha = 0.2f,
            maxAlpha = 1f,
            halfPeriod = 0.15f,
            ease = Ease.InOutSine,
            desync = true,
            ignoreTimeScale = true,
            restoreOnStop = true
        };
    }
}