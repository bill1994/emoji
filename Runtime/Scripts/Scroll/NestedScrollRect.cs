using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using System.Reflection;

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

        #region Constructors

        public NestedScrollRect()
        {
            scrollSensitivity = 20;
            decelerationRate = 0.3f;
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

        /*protected override void LateUpdate()
        {
            if (!content)
                return;

            var isDragging = Dragging;
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float deltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            Vector2 velocity = this.velocity;
            if (!isDragging && (offset != Vector2.zero || velocity != Vector2.zero))
            {
                Vector2 position = content.anchoredPosition;
                for (int axis = 0; axis < 2; axis++)
                {
                    // Apply spring physics if movement is elastic and content has an offset from the view.
                    if (movementType == MovementType.Elastic && offset[axis] != 0)
                    {
                        float speed = velocity[axis];
                        float smoothTime = elasticity;
                        if (Scrolling)
                            smoothTime *= 3.0f;
                        position[axis] = Mathf.SmoothDamp(content.anchoredPosition[axis], content.anchoredPosition[axis] + offset[axis], ref speed, smoothTime, Mathf.Infinity, deltaTime);
                        if (Mathf.Abs(speed) < 1)
                            speed = 0;
                        velocity[axis] = speed;
                    }
                    // Else move content according to velocity with deceleration applied.
                    else if (inertia)
                    {
                        const float MAX_DELTA_SIZE = 0.016666f;
                        velocity[axis] *= Mathf.Pow(decelerationRate, Mathf.Min(deltaTime, MAX_DELTA_SIZE));
                        if (Mathf.Abs(velocity[axis]) < 1)
                            velocity[axis] = 0;
                        position[axis] += velocity[axis] * deltaTime;
                    }
                    // If we have neither elaticity or friction, there shouldn't be any velocity.
                    else
                    {
                        velocity[axis] = 0;
                    }
                }

                if (movementType == MovementType.Clamped)
                {
                    offset = CalculateOffset(position - content.anchoredPosition);
                    position += offset;
                }

                SetContentAnchoredPosition(position);
            }

            var prevPosition = PrevPosition;
            if (isDragging && inertia)
            {
                Vector3 newVelocity = (content.anchoredPosition - prevPosition) / deltaTime;
                velocity = Vector3.Lerp(velocity, newVelocity, deltaTime * 10);
            }

            this.velocity = velocity;
            if (ViewBounds != PrevViewBounds || m_ContentBounds != PrevContentBounds || content.anchoredPosition != prevPosition)
            {
                UpdateScrollbars(offset);
                UISystemProfilerApi.AddMarker("ScrollRect.value", this);
                if(onValueChanged != null)
                    onValueChanged.Invoke(normalizedPosition);
                UpdatePrevData();
            }
            UpdateScrollbarVisibility();
            Scrolling = false;
        }*/

        #endregion

        #region ScrollRect Hidden Properties

        FieldInfo m_HasRebuiltLayoutField = null;
        protected bool HasRebuiltLayout
        {
            get
            {
                return GetFieldValue_Internal<bool>(ref m_HasRebuiltLayoutField, "m_HasRebuiltLayout");
            }
            set
            {
                SetFieldValue_Internal<bool>(ref m_HasRebuiltLayoutField, "m_HasRebuiltLayout", value);
            }
        }

        FieldInfo m_ViewBoundsField = null;
        protected Bounds ViewBounds
        {
            get
            {
                return GetFieldValue_Internal<Bounds>(ref m_ViewBoundsField, "m_ViewBounds");
            }
            set
            {
                SetFieldValue_Internal<Bounds>(ref m_ViewBoundsField, "m_ViewBounds", value);
            }
        }

        FieldInfo m_PrevViewBoundsField = null;
        protected Bounds PrevViewBounds
        {
            get
            {
                return GetFieldValue_Internal<Bounds>(ref m_PrevViewBoundsField, "m_PrevViewBounds");
            }
            set
            {
                SetFieldValue_Internal<Bounds>(ref m_PrevViewBoundsField, "m_PrevViewBounds", value);
            }
        }

        FieldInfo m_PrevContentBoundsField = null;
        protected Bounds PrevContentBounds
        {
            get
            {
                return GetFieldValue_Internal<Bounds>(ref m_PrevContentBoundsField, "m_PrevContentBounds");
            }
            set
            {
                SetFieldValue_Internal<Bounds>(ref m_PrevContentBoundsField, "m_PrevContentBounds", value);
            }
        }

        

        FieldInfo m_DraggingField = null;
        protected bool Dragging
        {
            get
            {
                return GetFieldValue_Internal<bool>(ref m_DraggingField, "m_Dragging");
            }
            set
            {
                SetFieldValue_Internal<bool>(ref m_DraggingField, "m_Dragging", value);
            }
        }

        FieldInfo m_ScrollingField = null;
        protected bool Scrolling
        {
            get
            {
                return GetFieldValue_Internal<bool>(ref m_ScrollingField, "m_Scrolling");
            }
            set
            {
                SetFieldValue_Internal<bool>(ref m_ScrollingField, "m_Scrolling", value);
            }
        }

        FieldInfo m_PrevPositionField = null;
        protected Vector2 PrevPosition
        {
            get
            {
                return GetFieldValue_Internal<Vector2>(ref m_PrevPositionField, "m_PrevPosition");
            }
            set
            {
                SetFieldValue_Internal<Vector2>(ref m_PrevPositionField, "m_PrevPosition", value);
            }
        }

        protected T GetFieldValue_Internal<T>(ref FieldInfo field, string fieldName)
        {
            if (field == null && !string.IsNullOrEmpty(fieldName))
            {
                field = typeof(ScrollRect).GetField(fieldName, BindingFlags.NonPublic|BindingFlags.Instance);
            }

            if (field != null)
            {
                var returnUncasted = field.GetValue(this);
                if (returnUncasted is T)
                    return (T)returnUncasted;
            }
            return default(T);
        }

        protected void SetFieldValue_Internal<T>(ref FieldInfo field, string fieldName, T value)
        {
            if (field == null && !string.IsNullOrEmpty(fieldName))
            {
                field = typeof(ScrollRect).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (field != null)
                field.SetValue(this, value);
        }

        #endregion

        #region ScrollRect Hidden Functions

        protected void EnsureLayoutHasRebuilt()
        {
            if (!HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
                Canvas.ForceUpdateCanvases();
        }

        protected Vector2 CalculateOffset(Vector2 delta)
        {
            return InternalCalculateOffset(ViewBounds, ref m_ContentBounds, horizontal, vertical, movementType, ref delta);
        }

        MethodInfo m_UpdateScrollbarsMethod = null;
        protected void UpdateScrollbars(Vector2 offset)
        {
            InvokeMethod_Internal(ref m_UpdateScrollbarsMethod, "UpdateScrollbars", offset);
        }

        MethodInfo m_UpdateScrollbarVisibilityMethod = null;
        protected void UpdateScrollbarVisibility()
        {
            InvokeMethod_Internal(ref m_UpdateScrollbarVisibilityMethod, "UpdateScrollbarVisibility");
        }

        protected void InvokeMethod_Internal(ref MethodInfo method, string methodName, params object[] parameters)
        {
            if (method == null && !string.IsNullOrEmpty(methodName))
            {
                method = typeof(ScrollRect).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (method != null)
            {
                method.Invoke(this, parameters);
            }
        }

        #endregion

        #region ScrollRect Hidden Static Functions

        protected internal static Vector2 InternalCalculateOffset(Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, MovementType movementType, ref Vector2 delta)
        {
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
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