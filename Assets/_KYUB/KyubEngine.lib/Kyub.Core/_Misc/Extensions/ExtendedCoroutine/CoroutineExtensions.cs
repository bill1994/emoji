using UnityEngine;
using System.Collections;

namespace Kyub.Extensions
{
    public static class CoroutineExtensions
    {
        /// <summary>
        /// Better Coroutine with ability to stop, start or break
        /// </summary>
        /// <param name="monoBehaviour"></param>
        /// <param name="routine"></param>
        /// <param name="coroutineController"></param>
        /// <returns></returns>
        public static Coroutine StartCoroutineEx(this MonoBehaviour monoBehaviour, IEnumerator routine, out CoroutineController coroutineController)
        {
            if (routine == null)
            {
                throw new System.ArgumentNullException("routine");
            }

            coroutineController = new CoroutineController(routine);
            return monoBehaviour.StartCoroutine(coroutineController.Start());
        }
    }
}