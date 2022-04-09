// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

// From: https://gist.github.com/benblo/10732554

#if UNITY_EDITOR
using System.Collections;
using UnityEditor;

namespace MaterialUI
{
    /// <summary>
    /// Performs a coroutine during edit mode.
    /// </summary>
    public class EditorCoroutine
    {
        /// <summary>
        /// Starts the specified coroutine.
        /// </summary>
        /// <param name="routine">The coroutine to start.</param>
        /// <returns>The coroutine that was started.</returns>
        public static EditorCoroutine Start(IEnumerator routine)
        {
            EditorCoroutine coroutine = new EditorCoroutine(routine);
            coroutine.Start();
            return coroutine;
        }

        /// <summary>
        /// The coroutine.
        /// </summary>
        private readonly IEnumerator m_Routine;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorCoroutine"/> class.
        /// </summary>
        /// <param name="routine">The coroutine.</param>
        EditorCoroutine(IEnumerator routine)
        {
            m_Routine = routine;
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start()
        {
            EditorApplication.update += Update;
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop()
        {
            EditorApplication.update -= Update;
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        void Update()
        {
            if (!m_Routine.MoveNext())
            {
                Stop();
            }
        }
    }
}
#endif