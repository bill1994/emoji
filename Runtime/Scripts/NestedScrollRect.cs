using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    [AddComponentMenu("Kyub UI/Nested Scroll Rect")]
    public class NestedScrollRect : ScrollRect
    {
        #region Private Variables

        [SerializeField, Tooltip("Support route extra drag movements to parent")]
        protected bool m_NestedDragActive = false;

        protected bool _routeToParent = false;

        #endregion

        #region Public Properties

        public bool nestedDragActive
        {
            get
            {
                return m_NestedDragActive;
            }
            set
            {
                if (m_NestedDragActive == value)
                    return;
                m_NestedDragActive = value;
            }
        }

        #endregion

        #region Unity Functions

        /// <summary>
        /// Always route initialize potential drag event to parents
        /// </summary>
        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (m_NestedDragActive)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
            base.OnInitializePotentialDrag(eventData);
        }

        /// <summary>
        /// Drag event
        /// </summary>
        public override void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_routeToParent && m_NestedDragActive)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.dragHandler);
            else
                base.OnDrag(eventData);
        }

        /// <summary>
        /// Begin drag event
        /// </summary>
        public override void OnBeginDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
                _routeToParent = true;
            else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
                _routeToParent = true;
            else
                _routeToParent = false;

            if (_routeToParent && m_NestedDragActive)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.beginDragHandler);
            else
                base.OnBeginDrag(eventData);
        }

        /// <summary>
        /// End drag event
        /// </summary>
        public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_routeToParent && m_NestedDragActive)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.endDragHandler);
            else
                base.OnEndDrag(eventData);
            _routeToParent = false;
        }

        #endregion

        #region ÍLayoutElement

        protected float _MinWidth = -1;
        protected float _MinHeight = -1;
        protected float _PreferredWidth = -1;
        protected float _PreferredHeight = -1;

        public override float preferredWidth
        {
            get
            {
                if (_PreferredWidth == 0)
                    _PreferredWidth = -1;
                return _PreferredWidth;
            }
        }

        public override float preferredHeight
        {
            get
            {
                if (_PreferredHeight == 0)
                    _PreferredHeight = -1;
                return _PreferredHeight;
            }
        }

        public override float minWidth
        {
            get
            {
                if (_MinWidth == 0)
                    _MinWidth = -1;
                return _MinWidth;
            }
        }

        public override float minHeight
        {
            get
            {
                if (_MinHeight == 0)
                    _MinHeight = -1;
                return _MinHeight;
            }
        }

        public override int layoutPriority
        {
            get
            {
                return 1;
            }
        }

        public override void CalculateLayoutInputHorizontal()
        {
            _MinWidth = content == null ? -1 : LayoutUtility.GetLayoutProperty(content, e => e.minWidth, -1);
            _PreferredWidth = content == null ? -1 : LayoutUtility.GetLayoutProperty(content, e => e.preferredWidth, -1);
            base.CalculateLayoutInputHorizontal();
        }

        public override void CalculateLayoutInputVertical()
        {
            _MinHeight = content == null ? -1 : LayoutUtility.GetLayoutProperty(content, e => e.minHeight, -1);
            _PreferredHeight = content == null ? -1 : LayoutUtility.GetLayoutProperty(content, e => e.preferredHeight, -1);
            base.CalculateLayoutInputVertical();
        }

        #endregion
    }
}