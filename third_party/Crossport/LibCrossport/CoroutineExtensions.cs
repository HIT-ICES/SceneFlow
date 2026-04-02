using System;
using System.Collections;
using UnityEngine;

namespace Ices.Crossport
{
    public static class CoroutineExtensions
    {
        public static IEnumerator Then
            (this IEnumerator previous, IEnumerator nextOperation, Func<IEnumerator, Coroutine> startCoroutine)
        {
            yield return startCoroutine(previous);
            yield return startCoroutine(nextOperation);
        }

        public static IEnumerator Then
            (this IEnumerator previous, Action nextOperation, Func<IEnumerator, Coroutine> startCoroutine)
        {
            yield return startCoroutine(previous);
            nextOperation();
        }
    }
}