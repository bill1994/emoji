//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    /// <summary>
    /// Changes the state of a ScrollRect, depending on whether its content RectTransform is over a specified height.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    /// <seealso cref="UnityEngine.UI.ILayoutElement" />
    //[AddComponentMenu("MaterialUI/Vertical Scroll Layout Element", 50)]
    [System.Obsolete("Use NestedScrollRect and/or MaxLayoutElement instead")]
    public class VerticalScrollLayoutElement : MonoBehaviour, ILayoutElement
    {
        #region Private Variables

        [SerializeField]
        private float m_MaxHeight = 0;
        [SerializeField]
        private RectTransform m_ContentRectTransform = null;
        [SerializeField]
        private ScrollRect m_ScrollRect = null;
        [SerializeField]
        private Image m_ScrollHandleImage = null;
        [SerializeField]
        private Image m_ScrollBackgroundImage = null;
        [SerializeField]
        private ScrollRect.MovementType m_MovementTypeWhenScrollable = ScrollRect.MovementType.Clamped;
        [SerializeField]


        private Image[] m_ShowWhenScrollable = null;
        private bool m_ScrollEnabled;
        private float m_Height;
        private RectTransform m_ScrollRectTransform;

        private float m_Width;
        private float m_MinWidth;
        private float m_FlexibleWidth;

        #endregion

        #region Public Properties

        public float maxHeight
        {
            get { return m_MaxHeight; }
            set
            {
                m_MaxHeight = Mathf.Max(0, value);
                RefreshLayout();
            }
        }

        public RectTransform contentRectTransform
        {
            get { return m_ContentRectTransform; }
            set
            {
                m_ContentRectTransform = value;
                RefreshLayout();
            }
        }

        public ScrollRect scrollRect
        {
            get { return m_ScrollRect; }
            set
            {
                m_ScrollRect = value;
                RefreshLayout();
            }
        }

        public RectTransform scrollRectTransform
        {
            get { return m_ScrollRectTransform; }
            set
            {
                m_ScrollRectTransform = value;
                RefreshLayout();
            }
        }

        public Image scrollHandleImage
        {
            get { return m_ScrollHandleImage; }
            set
            {
                m_ScrollHandleImage = value;
                RefreshLayout();
            }
        }

        public Image scrollBackgroundImage
        {
            get { return m_ScrollBackgroundImage; }
            set
            {
                m_ScrollBackgroundImage = value;
                RefreshLayout();
            }
        }

        public ScrollRect.MovementType movementTypeWhenScrollable
        {
            get { return m_MovementTypeWhenScrollable; }
            set
            {
                m_MovementTypeWhenScrollable = value;
                RefreshLayout();
            }
        }

        public bool scrollEnabled
        {
            get { return m_ScrollEnabled; }
        }

        #endregion

        #region Helper Functions

        private void RefreshLayout()
        {
            m_Width = m_ContentRectTransform != null? LayoutUtility.GetPreferredWidth(m_ContentRectTransform) : -1;
            m_MinWidth = m_ContentRectTransform != null ? LayoutUtility.GetMinWidth(m_ContentRectTransform) : -1;
            m_FlexibleWidth = m_ContentRectTransform != null ? LayoutUtility.GetFlexibleWidth(m_ContentRectTransform) : -1;

            if (!m_ScrollRect)
            {
                m_ScrollRect = GetComponent<ScrollRect>();
            }
            if (!m_ScrollRectTransform)
            {
                m_ScrollRectTransform = m_ScrollRect.GetComponent<RectTransform>();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);

            m_ScrollRectTransform.sizeDelta = new Vector2(m_ScrollRectTransform.sizeDelta.x, m_Height);

            float tempHeight = LayoutUtility.GetPreferredHeight(contentRectTransform);

            if (tempHeight > m_MaxHeight && m_MaxHeight >= 0)
            {
                m_Height = maxHeight;
                m_ScrollRect.movementType = movementTypeWhenScrollable;
            }
            else
            {
                m_Height = Mathf.Max(0, tempHeight);
                m_ScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
            {
                StopCoroutine("FinalizeRefreshLayoutRoutine");
                StartCoroutine("FinalizeRefreshLayoutRoutine");
            }
            else
            {
#if UNITY_EDITOR
                EditorCoroutine.Start(FinalizeRefreshLayoutRoutine());
#else
                FinalizeRefreshLayout();
#endif
            }
        }

        private System.Collections.IEnumerator FinalizeRefreshLayoutRoutine()
        {
            yield return null;
            FinalizeRefreshLayout();
        }

        private void FinalizeRefreshLayout()
        {
            if (this != null)
            {
                var active = m_Height >= m_MaxHeight && m_MaxHeight >= 0;

                if(m_ScrollHandleImage != null)
                    m_ScrollHandleImage.enabled = active;
                if(m_ScrollBackgroundImage != null)
                    m_ScrollBackgroundImage.enabled = active;

                m_ScrollEnabled = active;

                for (int i = 0; i < m_ShowWhenScrollable.Length; i++)
                {
                    m_ShowWhenScrollable[i].enabled = active;
                }
            }
        }

        #endregion

        #region Layout Functions

        public void CalculateLayoutInputHorizontal() { }

        public void CalculateLayoutInputVertical()
        {
            RefreshLayout();
        }

        public float minWidth { get { return m_MinWidth; } }
        public float preferredWidth { get { return m_Width; } }
        public float flexibleWidth { get { return m_FlexibleWidth; } }
        public float minHeight { get { return m_Height; } }
        public float preferredHeight { get { return m_Height; } }
        public float flexibleHeight { get { return m_Height; } }
        public int layoutPriority { get { return 0; } }

        #endregion
    }
}
