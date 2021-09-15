using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class ScrollMaxLayoutElement : MaxLayoutElement, ILayoutController
    {
        #region Private Variables

        bool _isVisible = false;
        protected int _cachedIndex = -1;
        protected ScrollLayoutGroup _cachedScrollLayout = null;

        #endregion

        #region Callbacks

        public UnityEvent OnBecameVisible = new UnityEvent();
        public UnityEvent OnBecameInvisible = new UnityEvent();

        #endregion

        #region Public Properties

        public int LayoutElementIndex
        {
            get
            {
                if (!Application.isPlaying && ScrollLayoutGroup != null && _cachedIndex < 0)
                    return ScrollLayoutGroup.FindElementIndex(this.gameObject);

                return _cachedIndex;
            }
            protected internal set
            {
                if (_cachedIndex == value)
                    return;
                _cachedIndex = value;
            }
        }

        public ScrollLayoutGroup ScrollLayoutGroup
        {
            get
            {
                if (_cachedScrollLayout == null)
                    _cachedScrollLayout = ScrollLayoutGroup.GetComponentInParent<ScrollLayoutGroup>(this, true);
                return _cachedScrollLayout;
            }
            protected set
            {
                if (_cachedScrollLayout == value)
                    return;
                UnregisterEvents();
                _cachedScrollLayout = value;
                if (enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
            }
        }

        public bool IsVisible
        {
            get
            {
                return _isVisible || (!Application.isPlaying && gameObject.activeInHierarchy);
            }
        }


        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            if (ScrollLayoutGroup != null)
                RegisterEvents();
            base.OnEnable();
            if (_started && gameObject.activeInHierarchy && enabled)
            {
                //ApplyElementSize();
                //Force recalculate element
                if (!_previousActiveSelf && ScrollLayoutGroup != null)
                    ScrollLayoutGroup.SetCachedElementsLayoutDirty();
            }

            _previousActiveSelf = gameObject.activeSelf;
        }

        bool _started = false;
        protected override void Start()
        {
            base.Start();
            _started = true;
        }

        bool _previousActiveSelf = false;
        protected override void OnDisable()
        {
            CancelInvoke();
            UnregisterEvents();
            base.OnDisable();

            //Recalculate element size ignoring this element position (object disabled)
            if (!gameObject.activeSelf && ScrollLayoutGroup != null)
                ScrollLayoutGroup.SetCachedElementsLayoutDirty();

            _previousActiveSelf = gameObject.activeSelf;
        }

        /*protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (enabled)
                SetElementSizeDirty();
        }*/

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            ScrollLayoutGroup = ScrollLayoutGroup.GetComponentInParent<ScrollLayoutGroup>(this, true);
            SetDirty();
        }

        #endregion

        #region Helper Functions

        public virtual void SetElementSizeDirty()
        {
            if (!IsInvoking("ApplyElementSize") && LayoutElementIndex >= 0 && _isVisible)
                Invoke("ApplyElementSize", 0);
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameInvisible.AddListener(HandleOnBecameInvisible);
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameVisible.AddListener(HandleOnBecameVisible);
        }

        protected virtual void UnregisterEvents()
        {
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameInvisible.RemoveListener(HandleOnBecameInvisible);
            if (_cachedScrollLayout != null)
                _cachedScrollLayout.OnElementBecameVisible.RemoveListener(HandleOnBecameVisible);
        }

        protected void ApplyElementSize()
        {
            CancelInvoke("ApplyElementSize");
            if (ScrollLayoutGroup != null && LayoutElementIndex >= 0 && IsVisible)
            {
                if (IsReloading())
                    SetElementSizeDirty();
                else
                    ScrollLayoutGroup.SetCachedElementSize(LayoutElementIndex, ScrollLayoutGroup.CalculateElementSize(transform, ScrollLayoutGroup.IsVertical()));
            }
        }

        protected bool IsReloading()
        {
            var delayedElements = GetComponents<IDelayedReloadableDataViewElement>();
            foreach (var delayedElement in delayedElements)
            {
                if (delayedElement != null && !delayedElement.IsDestroyed() && delayedElement.IsReloading())
                    return true;
            }
            return false;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnBecameInvisible(GameObject element, int index)
        {
            if (element == this.gameObject)
            {
                _isVisible = false;
                LayoutElementIndex = index;
                if (OnBecameInvisible != null)
                    OnBecameInvisible.Invoke();
            }
            else if (LayoutElementIndex >= 0 && index == LayoutElementIndex)
            {
                _isVisible = false;
                LayoutElementIndex = -1;
            }
        }

        protected virtual void HandleOnBecameVisible(GameObject element, int index)
        {
            if (element == this.gameObject)
            {
                _isVisible = true;
                LayoutElementIndex = index;
                if (OnBecameVisible != null)
                    OnBecameVisible.Invoke();
            }
            else if (LayoutElementIndex >= 0 && index == LayoutElementIndex)
            {
                _isVisible = false;
                LayoutElementIndex = -1;
            }
        }

        #endregion

        #region Override

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
        }

        public override void CalculateLayoutInputVertical()
        {
            base.CalculateLayoutInputVertical();
        }

        public virtual void SetLayoutHorizontal()
        {
            if (ScrollLayoutGroup != null && !ScrollLayoutGroup.IsVertical())
                ApplyElementSize();
        }

        public virtual void SetLayoutVertical()
        {
            if (ScrollLayoutGroup != null && ScrollLayoutGroup.IsVertical())
                ApplyElementSize();
        }

        #endregion
    }
}
