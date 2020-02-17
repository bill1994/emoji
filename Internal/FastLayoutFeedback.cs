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
    public class FastLayoutFeedback: UIBehaviour, IFastLayoutFeedback
    {
        #region Private Variables

        protected float _cachedMinWidth = -1;
        protected float _cachedMinHeight = -1;
        protected float _cachedPreferredWidth = -1;
        protected float _cachedPreferredHeight = -1;
        protected float _cachedFlexibleWidth = -1;
        protected float _cachedFlexibleHeight = -1;

        protected float _cachedRectHeight = -1;
        protected float _cachedRectWidth = -1;

        protected bool _cachedLayoutIgnore = false;

        [System.NonSerialized] protected RectTransform _rect;
        [System.NonSerialized] protected IFastLayoutFeedbackGroup _group;

        [System.NonSerialized] protected int m_CachedRectTransformHash = 0;

        #endregion

        #region Properties

        public virtual IFastLayoutFeedbackGroup group
        {
            get
            {
                if ((_group == null || _group.rectTransform == null))
                    _group = this != null && transform.parent != null? transform.parent.GetComponent<IFastLayoutFeedbackGroup>() : null;

                return _group;
            }
        }

        public virtual float cachedMinWidth
        {
            get
            {
                return _cachedMinWidth;
            }
            protected set
            {
                if (_cachedMinWidth == value)
                    return;
                _cachedMinWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public virtual float cachedMinHeight
        {
            get
            {
                return _cachedMinHeight;
            }
            protected set
            {
                if (_cachedMinHeight == value)
                    return;
                _cachedMinHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public virtual float cachedPreferredWidth
        {
            get
            {
                return _cachedPreferredWidth;
            }
            protected set
            {
                if (_cachedPreferredWidth == value)
                    return;
                _cachedPreferredWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public virtual float cachedPreferredHeight
        {
            get
            {
                return _cachedPreferredHeight;
            }
            protected set
            {
                if (_cachedPreferredHeight == value)
                    return;
                _cachedPreferredHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public virtual float cachedFlexibleWidth
        {
            get
            {
                return _cachedFlexibleWidth;
            }
            protected set
            {
                if (_cachedFlexibleWidth == value)
                    return;
                _cachedFlexibleWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public virtual float cachedFlexibleHeight
        {
            get
            {
                return _cachedFlexibleHeight;
            }
            protected set
            {
                if (_cachedFlexibleHeight == value)
                    return;
                _cachedFlexibleHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public virtual bool cachedLayoutIgnore
        {
            get
            {
                return _cachedLayoutIgnore;
            }
            protected set
            {
                if (_cachedLayoutIgnore == value)
                    return;
                _cachedLayoutIgnore = value;
                SetAxisDirty(DrivenAxis.Ignore);
            }
        }

        public virtual float cachedRectWidth
        {
            get
            {
                return _cachedRectWidth;
            }
            protected set
            {
                if (_cachedRectWidth == value)
                    return;
                _cachedRectWidth = value;
                SetAxisDirty(DrivenAxis.Horizontal);
            }
        }

        public virtual float cachedRectHeight
        {
            get
            {
                return _cachedRectHeight;
            }
            protected set
            {
                if (_cachedRectHeight == value)
                    return;
                _cachedRectHeight = value;
                SetAxisDirty(DrivenAxis.Vertical);
            }
        }

        public virtual RectTransform rectTransform
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

        public virtual float minWidth { get { return -1; } set { } }
        public virtual float minHeight { get { return -1; } set { } }
        public virtual float preferredWidth { get { return -1; } set { } }
        public virtual float preferredHeight { get { return -1; } set { } }
        public virtual float flexibleWidth { get { return -1; } set { } }
        public virtual float flexibleHeight { get { return -1; } set { } }
        public virtual int layoutPriority { get { return -1; } set { } }

        #endregion

        #region Constructors

        protected FastLayoutFeedback()
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
            CheckAutoDestroy();
        }
#endif

        #endregion

        #region Helper Functions

        protected virtual void CheckAutoDestroy()
        {
            hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
            if (group == null || group.rectTransform == null)
            {
                if (Application.isPlaying)
                    Object.Destroy(this);
                else
                {
                    Debug.Log("[FastLayoutFeedback] Removing feedback component. No parent groups found.");
                    Object.DestroyImmediate(this);
                }
            }
        }

        DrivenAxis _dirtyAxis = DrivenAxis.None;
        protected void SetAxisDirty(DrivenAxis dirtyAxis)
        {
            _dirtyAxis |= dirtyAxis;
        }

        protected void SetDirty()
        {
            SetAxisDirty(DrivenAxis.Horizontal| DrivenAxis.Vertical| DrivenAxis.Ignore);
        }

        public virtual void SendFeedback()
        {
            if (group != null && group.rectTransform != null && (_dirtyAxis.HasFlag(DrivenAxis.Ignore) || (group.childrenControlledAxis & _dirtyAxis) != 0))
            {
                group.SetElementDirty(this, _dirtyAxis);
                _dirtyAxis = 0;
            }
        }

        protected virtual void CalculateLayoutIgnore()
        {
            var toIgnoreList = ListPool<Component>.Get();
            rectTransform.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);

            var canAdd = toIgnoreList.Count == 0;
            if (!canAdd)
            {
                for (int j = 0; j < toIgnoreList.Count; j++)
                {
                    var ignorer = (ILayoutIgnorer)toIgnoreList[j];
                    if (!ignorer.ignoreLayout)
                    {
                        canAdd = true;
                        break;
                    }
                }
            }
            cachedLayoutIgnore = !canAdd;
        }

        protected virtual void CalculateRectTransformDimensions(bool canDirtyInLayoutRebuild = false)
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

        public virtual void CalculateLayoutInputHorizontal()
        {
            cachedMinWidth = LayoutUtility.GetMinWidth(this.rectTransform);
            cachedPreferredWidth = LayoutUtility.GetPreferredWidth(this.rectTransform);
            cachedFlexibleWidth = LayoutUtility.GetFlexibleWidth(this.rectTransform);
            CalculateLayoutIgnore();
        }

        public virtual void CalculateLayoutInputVertical()
        {
            cachedMinHeight = LayoutUtility.GetMinHeight(this.rectTransform);
            cachedPreferredHeight = LayoutUtility.GetPreferredHeight(this.rectTransform);
            cachedFlexibleHeight = LayoutUtility.GetFlexibleHeight(this.rectTransform);
            SendFeedback();
        }

        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }

        #endregion
    }
}
