using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UI.MVVM.View.ProductDescriptionPopup;
using UnityEngine;

namespace UI
{
    public class FramePiecesUI : MonoBehaviour
    {
        [SerializeField] private FramePiece[] _framePieces;

        private Vector3[] _defaultParentPositions;
        private Vector3[] _defaultMaskPositions;

        private void Awake()
        {
            _defaultParentPositions = new Vector3[_framePieces.Length];
            _defaultMaskPositions = new Vector3[_framePieces.Length];

            for (int i = 0; i < _framePieces.Length; i++)
            {
                _defaultParentPositions[i] = _framePieces[i].ParentTransform.localPosition;
                _defaultMaskPositions[i] = _framePieces[i].MaskTransform.localPosition;
            }
        }

        public async UniTask MoveFramePiecesAsync()
        {
            List<UniTask> tasks = new List<UniTask>();

            foreach (FramePiece framePiece in _framePieces)
            {
                float posX = framePiece.ParentTransform.localPosition.x;
                framePiece.ParentTransform.localPosition = new Vector3(0, framePiece.ParentTransform.localPosition.y,
                    framePiece.ParentTransform.localPosition.z);

                Tween tween = framePiece.ParentTransform
                    .DOLocalMoveX(posX, 0.5f)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .SetLink(gameObject, LinkBehaviour.KillOnDestroy).SetDelay(0.3f)
                    .OnComplete(() =>
                    {
                        framePiece.MaskTransform.DOLocalMoveY(0, 0.5f)
                            .SetUpdate(true)
                            .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
                    });

                tasks.Add(tween.AsyncWaitForCompletion().AsUniTask());
            }

            await UniTask.WhenAll(tasks);
        }

        public void ResetFramePieces()
        {
            DOTween.Kill(gameObject);

            for (int i = 0; i < _framePieces.Length; i++)
            {
                _framePieces[i].ParentTransform.localPosition = _defaultParentPositions[i];
                _framePieces[i].MaskTransform.localPosition = _defaultMaskPositions[i];
            }
        }
    }
}