using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;

namespace Common.Antenna
{
    public class AntennaVisual : MonoBehaviour
    {
        [SerializeField] private Transform[] _antenna;

        private float _antennaPieceOffset;
        private Sequence _antennaSequence;

        private void Awake() =>
            _antennaPieceOffset = _antenna.FirstOrDefault().localPosition.z;

        private void Start() =>
            Hide();

        private void Hide()
        {
            foreach (Transform piece in _antenna)
            {
                piece.transform.localPosition =
                    new Vector3(piece.transform.localPosition.x, piece.transform.localPosition.y, 0);
            }
        }

        private void Show()
        {
            _antennaSequence?.Kill();
            _antennaSequence = DOTween.Sequence();

            float currentPosition = 0;

            foreach (Transform piece in _antenna)
            {
                currentPosition += _antennaPieceOffset;

                _antennaSequence.Append(piece.transform.DOLocalMoveZ(currentPosition, 0.25f));
            }
        }
    }
}