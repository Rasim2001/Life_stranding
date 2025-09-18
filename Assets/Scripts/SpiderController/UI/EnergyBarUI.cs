using System.Collections;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace SpiderController.UI
{
    public class EnergyBarUI : MonoBehaviour
    {
        [SerializeField] private Image[] _segments;
        [SerializeField] private Image[] _containers;
        private int SegmentCount => _segments.Length;
        private float PerSegment => 1f / SegmentCount;

        private Sequence _holoSequence;
        private Coroutine _holoCoroutine;

        public void SetEnergyValue(float energyValue)
        {
            for (int i = 0; i < SegmentCount; i++)
            {
                float segmentFill = Mathf.Clamp01((energyValue - i * PerSegment) / PerSegment);
                _segments[i].fillAmount = segmentFill;
            }
        }

        [Button]
        public void PlayFadeHologramEffect()
        {
            if (_holoCoroutine != null)
                return;

            _holoCoroutine = StartCoroutine(StartFadeHologramEffectCoroutine());
        }

        [Button]
        public void ShowHologram()
        {
            if (_holoCoroutine == null)
                return;

            StopCoroutine(_holoCoroutine);
            _holoCoroutine = null;

            for (int i = 0; i < SegmentCount; i++)
            {
                Color segmentColor = _segments[i].color;
                Color containerColor = _containers[i].color;

                segmentColor.a = 1;
                containerColor.a = 1;

                _segments[i].color = segmentColor;
                _containers[i].color = containerColor;
            }
        }

        private IEnumerator StartFadeHologramEffectCoroutine()
        {
            yield return new WaitForSeconds(2f);

            for (int i = 0; i < SegmentCount; i++)
            {
                DisableFirstPiece(i);

                yield return new WaitForSeconds(0.05f);

                FadeFirstPiece(i, 1);
                FadeOtherPieces(i);

                yield return new WaitForSeconds(0.03f);

                FadeAllPieces();

                yield return new WaitForSeconds(0.03f);

                DisableFirstPiece(i);

                yield return new WaitForSeconds(0.02f);

                FadeFirstPiece(i, 2);
                FadeAllPieces();

                yield return new WaitForSeconds(0.01f);

                FadeAllPieces();

                DisableFirstPiece(i);
            }
        }

        private void FadeOtherPieces(int i)
        {
            for (int y = i + 1; y < SegmentCount; y++)
            {
                if (y >= SegmentCount)
                    break;

                Color segmentColor = _segments[i].color;
                Color containerColor = _containers[i].color;

                segmentColor.a -= i * 0.05f;
                containerColor.a -= i * 0.05f;

                _segments[y].color = segmentColor;
                _containers[y].color = containerColor;
            }
        }

        private void FadeFirstPiece(int i, int iteration)
        {
            Color segmentColor = _segments[i].color;
            Color containerColor = _containers[i].color;

            segmentColor.a = 0.5f / iteration - i * 0.1f;
            containerColor.a = 0.5f / iteration - i * 0.1f;

            _segments[i].color = segmentColor;
            _containers[i].color = containerColor;
        }


        private void DisableFirstPiece(int i)
        {
            Color segmentColor = _segments[i].color;
            Color containerColor = _containers[i].color;

            segmentColor.a = 0;
            containerColor.a = 0;

            _segments[i].color = segmentColor;
            _containers[i].color = containerColor;
        }

        private void FadeAllPieces()
        {
            for (int x = 0; x < SegmentCount; x++)
            {
                Color segmentColorX = _segments[x].color;
                Color containerColorX = _containers[x].color;

                segmentColorX.a -= 0.1f;
                containerColorX.a -= 0.1f;

                _segments[x].color = segmentColorX;
                _containers[x].color = containerColorX;
            }
        }
    }
}