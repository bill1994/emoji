using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    internal sealed class FastLayoutFeedback: UIBehaviour, IFastLayoutFeedback
    {
        #region Private Variables

        internal float _cachedMinWidth = -1;
        internal float _cachedMinHeight = -1;
        internal float _cachedPreferredWidth = -1;
        internal float _cachedPreferredHeight = -1;
        internal float _cachedFlexibleWidth = -1;
        internal float _cachedFlexibleHeight = -1;

        internal float _cachedRectHeight = -1;
        internal float _cachedRectWidth = -1;

        internal float _cachedMaxWidth = -1;
        internal float _cachedMaxHeight = -1;

        internal bool _cachedLayoutIgnore = false;

        [System.NonSerialized] internal RectTransform _rect;
        [System.NonSerialized] internal IFastLayoutGroup _group;

        [System.NonSerialized] internal int m_CachedRectTransformHash = 0;

        #endregion

        #region Properties

        public IFastLayoutGroup group
        {
            get
            {
                if ((_group == null || _group.rectTransform == null))
                    _group = this != null && transform.parent != null? transform.parent.GetComponent<IFastLayoutGroup>() : null;

                return _group;
            }
        }

        public float cachedMinWidth
        {
            get
            {
                return _cachedMinWidth;
            }
            internal set
            {
                if (_cachedMinWidth == value)
                    return;
                _cachedMinWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public float cachedMinHeight
        {
            get
            {
                return _cachedMinHeight;
            }
            internal set
            {
                if (_cachedMinHeight == value)
                    return;
                _cachedMinHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public float cachedPreferredWidth
        {
            get
            {
                return _cachedPreferredWidth;
            }
            internal set
            {
                if (_cachedPreferredWidth == value)
                    return;
                _cachedPreferredWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public float cachedPreferredHeight
        {
            get
            {
                return _cachedPreferredHeight;
            }
            internal set
            {
                if (_cachedPreferredHeight == value)
                    return;
                _cachedPreferredHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public float cachedFlexibleWidth
        {
            get
            {
                return _cachedFlexibleWidth;
            }
            internal set
            {
                if (_cachedFlexibleWidth == value)
                    return;
                _cachedFlexibleWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public float cachedFlexibleHeight
        {
            get
            {
                return _cachedFlexibleHeight;
            }
            internal set
            {
                if (_cachedFlexibleHeight == value)
                    return;
                _cachedFlexibleHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public bool cachedLayoutIgnore
        {
            get
            {
                return _cachedLayoutIgnore;
            }
            internal set
            {
                if (_cachedLayoutIgnore == value)
                    return;
                _cachedLayoutIgnore = value;
                SetAxisDirty(DrivenAxis.Ignore);
            }
        }

        public float cachedMaxWidth
        {
            get
            {
                return _cachedMaxWidth;
            }
            internal set
            {
                if (_cachedMaxWidth == value)
                    return;
                _cachedMaxWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public float cachedMaxHeight
        {
            get
            {
                return _cachedMaxHeight;
            }
            internal set
            {
                if (_cachedMaxHeight == value)
                    return;
                _cachedMaxHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public float cachedRectWidth
        {
            get
            {
                return _cachedRectWidth;
            }
            internal set
            {
                if (_cachedRectWidth == value)
                    return;
                _cachedRectWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public float cachedRectHeight
        {
            get
            {
                return _cachedRectHeight;
            }
            internal set
            {
                if (_cachedRectHeight == value)
                    return;
                _cachedRectHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                if (this == null)
                    return null;

                if (_rect == null)
                    _rect = GetComponent<RectTransform>();
                return _rect;
            }
        }

        #endregion

        #region ILayout Properties

        public float minWidth { get { return -1; } set { } }
        public float minHeight { get { return -1; } set { } }
        public float preferredWidth { get { return -1; } set { } }
        public float preferredHeight { get { return -1; } set { } }
        public float flexibleWidth { get { return -1; } set { } }
        public float flexibleHeight { get { return -1; } set { } }
        public int layoutPriority { get { return -1; } set { } }

        #endregion

        #region Constructors

        internal FastLayoutFeedback()
        { }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
            SendFeedback();
            CheckAutoDestroy();
        }

        protected override void OnDisable()
        {
            SetDirty();
            SendFeedback();
            base.OnDisable();
        }

        protected override void OnBeforeTransformParentChanged()
        {
            SetDirty();
            SendFeedback();
        }

        protected override void OnTransformParentChanged()
        {
            _group = null;
            SetDirty();
            SendFeedback();
            CheckAutoDestroy();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            CalculateRectTransformDimensions();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if(!IsInvoking(nameof(CheckAutoDestroy)))
                Invoke(nameof(CheckAutoDestroy), 0);
        }
#endif

        #endregion

        #region Helper Functions

        internal void CheckAutoDestroy()
        {
#if UNITY_EDITOR
            CancelInvoke(nameof(CheckAutoDestroy));
            hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
#endif
            if (group == null || group.rectTransform == null)
            {
                if (Application.isPlaying)
                    Object.Destroy(this);
                else
                {
#if UNITY_EDITOR
                    //Prevent UnityEditor Crash while Copy/Paste
                    UnityEditor.EditorApplication.CallbackFunction removeDelayed = null;
                    removeDelayed = () =>
                    {
                        UnityEditor.EditorApplication.update -= removeDelayed;
                        if (this == null)
                            return;
                        //Debug.Log("[FastLayoutFeedback] Removing feedback component. No parent groups found.");
                        Object.DestroyImmediate(this);
                    };
                    UnityEditor.EditorApplication.update += removeDelayed;
#endif
                }
            }
        }

        internal DrivenAxis _dirtyAxis = DrivenAxis.None;
        internal void SetAxisDirty(DrivenAxis dirtyAxis)
        {
            _dirtyAxis |= dirtyAxis;
        }

        internal void SetDirty()
        {
            SetAxisDirty(DrivenAxis.Horizontal| DrivenAxis.Vertical| DrivenAxis.Ignore);
        }

        public void SendFeedback()
        {
            if (group != null && group.rectTransform != null && (_dirtyAxis.HasFlag(DrivenAxis.Ignore) || (group.childrenControlledAxis & _dirtyAxis) != 0))
            {
                group.SetElementDirty(this, _dirtyAxis);
                _dirtyAxis = 0;
            }
        }

        public void CalculateLayoutIgnore()
        {
            var toIgnoreList = ListPool<Component>.Get();
            rectTransform.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

            var canAdd = toIgnoreList.Count == 0;
            if (!canAdd)
            {
                for (int j = 0; j < toIgnoreList.Count; j++)
                {
                    var layoutIgnore = toIgnoreList[j];
                    if (layoutIgnore != null && layoutIgnore != this)
                    {
                        var ignorer = (ILayoutIgnorer)layoutIgnore;
                        if (!ignorer.ignoreLayout)
                        {
                            canAdd = true;
                            break;
                        }
                    }
                }
            }
            cachedLayoutIgnore = !canAdd;
        }

        internal void CalculateRectTransformDimensions(bool canDirtyInLayoutRebuild = false)
        {
            cachedRectWidth = rectTransform.sizeDelta.x;
            cachedRectHeight = rectTransform.sizeDelta.y;

            //Prevent change size while calculating feedback
            if (!canDirtyInLayoutRebuild && CanvasUpdateRegistry.IsRebuildingLayout())
                _dirtyAxis = DrivenAxis.None;
            else
                SendFeedback();
        }

        public override int GetHashCode()
        {
            if (this != null && m_CachedRectTransformHash == 0)
                m_CachedRectTransformHash = rectTransform.GetHashCode();
            return m_CachedRectTransformHash;
        }


        #endregion

        #region ILayoutController Functions

        public void CalculateLayoutInputHorizontal()
        {
            cachedMinWidth = LayoutUtilityEx.GetMinWidth(this.rectTransform);
            cachedPreferredWidth = LayoutUtilityEx.GetPreferredWidth(this.rectTransform);
            cachedFlexibleWidth = LayoutUtilityEx.GetFlexibleWidth(this.rectTransform);
            cachedMaxWidth = LayoutUtilityEx.GetMaxWidth(this.rectTransform, -1);
            CalculateLayoutIgnore();
            SendFeedback();
        }

        public void CalculateLayoutInputVertical()
        {
            cachedMinHeight = LayoutUtilityEx.GetMinHeight(this.rectTransform);
            cachedPreferredHeight = LayoutUtilityEx.GetPreferredHeight(this.rectTransform);
            cachedFlexibleHeight = LayoutUtilityEx.GetFlexibleHeight(this.rectTransform);
            cachedMaxHeight = LayoutUtilityEx.GetMaxHeight(this.rectTransform, -1);
            SendFeedback();
        }

        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }

        #endregion
    }
}
