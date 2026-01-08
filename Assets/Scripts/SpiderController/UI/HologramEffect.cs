using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class HologramEffect
    {
        private readonly Image[] _segments;
        private readonly Image[] _containers;
        private readonly Image[] _otherObjects;

        public event Action OnHologramEffectStartHappened;

        private CancellationTokenSource _cts;

        public HologramEffect(Image[] segments, Image[] containers, Image[] otherObjects)
        {
            _segments = segments;
            _containers = containers;
            _otherObjects = otherObjects;
        }


        public void Play()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();

            RunHologramEffectAsync(_cts.Token).Forget();
        }

        public void Stop()
        {
            Clear();
            ResetAlpha();
            ResetOtherObjects();
        }

        public void Clear()
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private async UniTaskVoid RunHologramEffectAsync(CancellationToken token)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(2f), cancellationToken: token);

                OnHologramEffectStartHappened?.Invoke();

                int count = _segments.Count(x => x != null && x.gameObject.activeSelf);

                for (int i = 0; i < count; i++)
                {
                    DisableFirstPiece(_segments, _containers, i);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.05f), cancellationToken: token);

                    FadeFirstPiece(_segments, _containers, i, 1);
                    FadeOtherPieces(_segments, _containers, i);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.03f), cancellationToken: token);

                    FadeAllPieces(_segments, _containers);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.03f), cancellationToken: token);

                    DisableFirstPiece(_segments, _containers, i);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.02f), cancellationToken: token);

                    FadeFirstPiece(_segments, _containers, i, 2);
                    FadeAllPieces(_segments, _containers);
                    await UniTask.Delay(TimeSpan.FromSeconds(0.01f), cancellationToken: token);

                    FadeAllPieces(_segments, _containers);
                    DisableFirstPiece(_segments, _containers, i);
                    DisableOtherObjects();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void DisableOtherObjects()
        {
            foreach (Image otherObject in _otherObjects)
            {
                if (otherObject == null)
                    return;

                Color seg = otherObject.color;
                seg.a = 0;

                otherObject.color = seg;
            }
        }

        private void ResetOtherObjects()
        {
            foreach (Image otherObject in _otherObjects)
            {
                if (otherObject == null)
                    return;

                Color seg = otherObject.color;
                seg.a = 1;

                otherObject.color = seg;
            }
        }

        private void FadeOtherPieces(Image[] segments, Image[] containers, int i)
        {
            for (int y = i + 1; y < segments.Length; y++)
            {
                if (segments[y] == null)
                    return;

                Color seg = segments[y].color;
                Color con = containers[y].color;

                seg.a -= i * 0.05f;
                con.a -= i * 0.05f;

                segments[y].color = seg;
                containers[y].color = con;
            }
        }

        private void FadeFirstPiece(Image[] segments, Image[] containers, int i, int iteration)
        {
            if (segments[i] == null)
                return;

            Color seg = segments[i].color;
            Color con = containers[i].color;

            seg.a = 0.5f / iteration - i * 0.1f;
            con.a = 0.5f / iteration - i * 0.1f;

            segments[i].color = seg;
            containers[i].color = con;
        }

        private void DisableFirstPiece(Image[] segments, Image[] containers, int i)
        {
            if (segments[i] == null)
                return;

            Color seg = segments[i].color;
            Color con = containers[i].color;

            seg.a = 0;
            con.a = 0;

            segments[i].color = seg;
            containers[i].color = con;
        }

        private void FadeAllPieces(Image[] segments, Image[] containers)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null)
                    return;

                Color seg = segments[i].color;
                Color con = containers[i].color;

                seg.a -= 0.1f;
                con.a -= 0.1f;

                segments[i].color = seg;
                containers[i].color = con;
            }
        }

        private void ResetAlpha()
        {
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i] == null)
                    return;

                Color seg = _segments[i].color;

                seg.a = 1;

                _segments[i].color = seg;
            }

            for (int i = 0; i < _containers.Length; i++)
            {
                if (_containers[i] == null)
                    return;

                Color con = _containers[i].color;
                con.a = 1;

                _containers[i].color = con;
            }
        }
    }
}