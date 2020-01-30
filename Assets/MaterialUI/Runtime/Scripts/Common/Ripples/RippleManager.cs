//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    /// <summary>
    /// Singleton class that handles the creation and pooling of all ripples in a scene.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [AddComponentMenu("MaterialUI/Managers/Ripple Manager")]
    public class RippleManager : MonoBehaviour
    {
        /// <summary>
        /// The in-scene instance.
        /// </summary>
        private static RippleManager m_Instance;
        /// <summary>
        /// The in-scene instance.
        /// If null, automatically creates one.
        /// </summary>
        public static RippleManager instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new GameObject("RippleManager", typeof(RippleManager)).GetComponent<RippleManager>();
                }

                return m_Instance;
            }
        }

        /// <summary>
        /// The VectorImageData to use for all Ripples.
        /// </summary>
        private VectorImageData m_RippleImageData;
        /// <summary>
        /// The VectorImageData to use for all Ripples.
        /// If null, automatically gets the 'circle' icon from the built-in MaterialUIIcons pack.
        /// </summary>
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

        /// <summary>
        /// The number of ripples, active or pooled (queued), in the scene.
        /// </summary>
        private static int rippleCount;

        /// <summary>
        /// The active ripples in the scene.
        /// </summary>
        private List<Ripple> m_ActiveRipples = new List<Ripple>();
        /// <summary>
        /// The queued (pooled) ripples in the scene.
        /// </summary>
        private Queue<Ripple> m_QueuedRipples = new Queue<Ripple>();

        [SerializeField]
        protected Canvas m_Canvas = null;

        protected virtual void Awake()
        {
            FindCanvas();
        }

        /// <summary>
        /// See MonoBehaviour.OnApplicationQuit.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            Ripple.ResetMaterial();
        }

        protected void FindCanvas()
        {
            if (m_Canvas == null)
            {
                m_Canvas = GetComponentInParent<Canvas>();
                if (m_Canvas == null)
                {
                    //Find canvas in scene
                    var canvasArray = FindObjectsOfType<Canvas>();
                    foreach (var canvasMember in canvasArray)
                    {
                        if (canvasMember != null &&
                            canvasMember.gameObject.scene.IsValid() &&
                            canvasMember.enabled &&
                            canvasMember.gameObject.activeInHierarchy)
                        {
                            m_Canvas = canvasMember;
                            break;
                        }
                    }
                }

                if (m_Canvas != null)
                    m_Canvas = m_Canvas.rootCanvas;
            }

            transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Gets the next queued ripple.
        /// If none available, one will be created.
        /// </summary>
        /// <returns>A ripple object, ready to Setup and expand.</returns>
        public Ripple GetRipple()
        {
            if (m_QueuedRipples.Count <= 0)
            {
                CreateRipple();
            }

            Ripple ripple = m_QueuedRipples.Dequeue();
            m_ActiveRipples.Add(ripple);
            ripple.gameObject.SetActive(true);
            return ripple;
        }

        /// <summary>
        /// Creates a new Ripple and adds it to the queue.
        /// </summary>
        private void CreateRipple()
        {
            if (m_Canvas == null)
                FindCanvas();

            Ripple ripple = PrefabManager.InstantiateGameObject("Ripple", m_Canvas != null? m_Canvas.transform : null).GetComponent<Ripple>();
            ripple.Create(rippleCount, rippleImageData);
            rippleCount++;

            ReleaseRippleImmediate(ripple);
        }

        /// <summary>
        /// Resets a ripple's data, ready to reuse.
        /// </summary>
        /// <param name="ripple">The ripple.</param>
        private void ResetRipple(Ripple ripple)
        {
            ripple.rectTransform.SetParent(transform);
            ripple.rectTransform.localScale = Vector3.zero;
            ripple.rectTransform.sizeDelta = Vector2.zero;
            ripple.rectTransform.anchoredPosition = Vector2.zero;
            ripple.color = Color.clear;
            ripple.canvasGroup.alpha = 0f;
            ripple.ClearData();
        }

        /// <summary>
        /// Calls <see cref="ResetRipple"/> with the specified ripple and adds it back into the queue.
        /// </summary>
        /// <param name="ripple">The ripple to reset and queue.</param>
        public void ReleaseRipple(Ripple ripple)
        {
            _ripplesToRelease.Add(ripple);
        }

        protected void ReleaseRippleImmediate(Ripple ripple)
        {
            ResetRipple(ripple);
            ripple.gameObject.SetActive(false);
            m_QueuedRipples.Enqueue(ripple);
        }

        protected List<Ripple> _ripplesToRelease = new List<Ripple>();
        protected void TryReleaseRipplesStack()
        {
            foreach (var v_ripple in _ripplesToRelease)
            {
                if (v_ripple != null)
                    ReleaseRippleImmediate(v_ripple);
            }
            _ripplesToRelease.Clear();
        }

        

        private void Update()
        {
            TryReleaseRipplesStack();
        }
    }
}
