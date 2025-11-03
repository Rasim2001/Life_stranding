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
    }
}