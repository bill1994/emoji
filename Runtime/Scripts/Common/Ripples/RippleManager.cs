//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Kyub;

namespace MaterialUI
{
    /// <summary>
    /// Singleton class that handles the creation and pooling of all ripples in a scene.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [AddComponentMenu("MaterialUI/Managers/Ripple Manager")]
    public class RippleManager : Singleton<RippleManager>
    {
        #region Variables

        private VectorImageData m_RippleImageData;

        private static int s_rippleCount;

        private List<Ripple> m_ActiveRipples = new List<Ripple>();
        private Queue<Ripple> m_QueuedRipples = new Queue<Ripple>();

        protected List<Ripple> _ripplesToRelease = new List<Ripple>();

        #endregion

        #region Properties

        public VectorImageData rippleImageData
        {
            get
            {
                if (m_RippleImageData == null)
                {
                    m_RippleImageData = MaterialUIIconHelper.GetIcon("circle").vectorImageData;
                }
                return m_RippleImageData;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            if (s_instance == this)
                Ripple.ResetMaterial();
        }

        protected virtual void Update()
        {
            if (s_instance == this)
            {
                TryReleaseRipplesStack();
            }
        }

        #endregion

        #region Public Functions

        /// <summary>
        /// Gets the next queued ripple.
        /// If none available, one will be created.
        /// </summary>
        /// <returns>A ripple object, ready to Setup and expand.</returns>
        public virtual Ripple GetRipple()
        {
            //Try pick ripple from pool
            Ripple ripple = m_QueuedRipples.Count > 0 ? m_QueuedRipples.Dequeue() : null;
            while (m_QueuedRipples.Count > 0 && ripple == null)
            {
                ripple = m_QueuedRipples.Dequeue();
            }

            //Create a new one if failed to retrieve from pool
            if (ripple == null)
            {
                CreateRipple();
                ripple = m_QueuedRipples.Dequeue();
            }
            m_ActiveRipples.Add(ripple);
            ripple.gameObject.SetActive(true);
            return ripple;
        }

        /// <summary>
        /// Calls <see cref="ResetRipple"/> with the specified ripple and adds it back into the queue.
        /// </summary>
        /// <param name="ripple">The ripple to reset and queue.</param>
        public virtual void ReleaseRipple(Ripple ripple)
        {
            _ripplesToRelease.Add(ripple);
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// Creates a new Ripple and adds it to the queue.
        /// </summary>
        protected virtual void CreateRipple()
        {
            Ripple ripple = PrefabManager.InstantiateGameObject("Ripple", DialogManager.rectTransform).GetComponent<Ripple>();
            ripple.Create(s_rippleCount, rippleImageData);
            s_rippleCount++;

            ReleaseRippleImmediate(ripple);
        }

        /// <summary>
        /// Resets a ripple's data, ready to reuse.
        /// </summary>
        /// <param name="ripple">The ripple.</param>
        protected virtual void ResetRipple(Ripple ripple)
        {
            ripple.rectTransform.SetParent(transform);
            ripple.rectTransform.localScale = Vector3.zero;
            ripple.rectTransform.sizeDelta = Vector2.zero;
            ripple.rectTransform.anchoredPosition = Vector2.zero;
            ripple.color = Color.clear;
            ripple.canvasGroup.alpha = 0f;
            ripple.ClearData();
        }

        protected virtual void ReleaseRippleImmediate(Ripple ripple)
        {
            ResetRipple(ripple);
            ripple.gameObject.SetActive(false);
            m_QueuedRipples.Enqueue(ripple);
        }

        protected virtual void TryReleaseRipplesStack()
        {
            foreach (var v_ripple in _ripplesToRelease)
            {
                if (v_ripple != null)
                    ReleaseRippleImmediate(v_ripple);
            }
            _ripplesToRelease.Clear();
        }

        #endregion
    }
}
