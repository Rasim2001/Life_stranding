using System.Collections;
using UnityEngine;

namespace Infastructure.Common
{
    public interface ICoroutineRunner
    {
        Coroutine StartCoroutine(IEnumerator coroutine);

        void StopCoroutine(Coroutine coroutine);
    }
}