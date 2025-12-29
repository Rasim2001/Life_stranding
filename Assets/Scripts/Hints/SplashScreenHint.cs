using System.Collections;
using UnityEngine;

namespace Hints
{
    public class SplashScreenHint : HintBase
    {
        private const float ShowTime = 15;
        private const float AnchorPositionX = -20;

        private readonly float _repeatTimer = 5;
        private Coroutine _waitCoroutine;

        protected override void Start()
        {
            base.Start();

            _waitCoroutine = StartCoroutine(StartWaitCoroutine());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopCoroutine(_waitCoroutine);
            _waitCoroutine = null;
        }

        private IEnumerator StartWaitCoroutine()
        {
            while (true)
            {
                Show(ShowTime, AnchorPositionX);

                yield return new WaitForSeconds(ShowTime + _repeatTimer);
            }
        }
    }
}