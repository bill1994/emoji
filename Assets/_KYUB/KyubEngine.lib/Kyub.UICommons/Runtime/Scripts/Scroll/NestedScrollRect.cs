using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using System.Reflection;
using UnityEngine.Events;

namespace Kyub.UI
{
    [AddComponentMenu("Kyub UI/Nested Scroll Rect")]
    public class NestedScrollRect : ScrollRect
    {
        #region Private Variables

        [SerializeField, Range(0, 1), Tooltip("Snap animation duration")]
        protected float m_SnapToDuration = 0.05f;
        [SerializeField, Tooltip("Support route extra drag movements to parent")]
        protected bool m_NestedDragActive = false;

        protected bool _routeToParent = false;

        //Used to animate
        protected Vector2AnimValue _contentPosition = null;

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

        public float snapToDuration
        {
            get
            {
                return m_SnapToDuration;
            }
            set
            {
                if (m_SnapToDuration == value)
                    return;
                m_SnapToDuration = value;
                if (_contentPosition != null)
                    _contentPosition.duration = value;
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

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAnimations();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            StepAnimations();
        }

        /// <summary>
        /// Always route initialize potential drag event to parents
        /// </summary>
        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (m_NestedDragActive && transform.parent != null)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
            base.OnInitializePotentialDrag(eventData);
        }

        /// <summary>
        /// Drag event
        /// </summary>
        public override void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_routeToParent && m_NestedDragActive && transform.parent != null)
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

            if (_routeToParent && m_NestedDragActive && transform.parent != null)
                ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.beginDragHandler);
            else
                base.OnBeginDrag(eventData);
        }

        /// <summary>
        /// End drag event
        /// </summary>
        public override void OnEndDrag(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (_routeToParent && m_NestedDragActive && transform.parent != null)
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

        #region Animation Functions

        public void SnapToImmediate(RectTransform child)
        {
            SnapToInternal(child, false);
        }

        public void SnapToImmediate(Vector3 worldSpacePosition)
        {
            SnapToInternal(worldSpacePosition, false);
        }

        public void SnapToImmediate(Rect childRectInScrollerSpace)
        {
            SnapToInternal(childRectInScrollerSpace, false);
        }

        public void SnapTo(RectTransform child)
        {
            SnapToInternal(child, enabled && gameObject.activeInHierarchy);
        }

        public void SnapTo(Vector3 worldSpacePosition)
        {
            SnapToInternal(worldSpacePosition, enabled && gameObject.activeInHierarchy);
        }

        public void SnapTo(Rect childRectInScrollerSpace)
        {
            SnapToInternal(childRectInScrollerSpace, enabled && gameObject.activeInHierarchy);
        }

        protected virtual void SnapToInternal(RectTransform child, bool animate)
        {
            if (this.content == null || !child.IsChildOf(this.content))
                return;

            var childMin = (Vector2)this.content.transform.InverseTransformPoint(child.TransformPoint(child.rect.min));
            var childMax = (Vector2)this.content.transform.InverseTransformPoint(child.TransformPoint(child.rect.max));

            var childRectInScrollerSpace = Rect.MinMaxRect(childMin.x, childMin.y, childMax.x, childMax.y);
            SnapToInternal(childRectInScrollerSpace, animate);
        }

        protected virtual void SnapToInternal(Vector3 worldSpacePosition, bool animate)
        {
            if (this.content == null)
                return;

            var childMin = (Vector2)this.content.transform.InverseTransformPoint(worldSpacePosition);
            var childMax = (Vector2)childMin;

            var childRectInScrollerSpace = Rect.MinMaxRect(childMin.x, childMin.y, childMax.x, childMax.y);
            SnapToInternal(childRectInScrollerSpace, true);
        }

        protected virtual void SnapToInternal(Rect childRectInScrollerSpace, bool animate)
        {
            if (this.content == null)
                return;

            var view = this.viewRect;
            var contentMin = (Vector2)this.content.InverseTransformPoint(view.TransformPoint(view.rect.min));
            var contentMax = (Vector2)this.content.InverseTransformPoint(view.TransformPoint(view.rect.max));

            var contentRectInScroller = Rect.MinMaxRect(contentMin.x, contentMin.y, contentMax.x, contentMax.y);

            var delta = new Vector2(0, 0);
            if (this.horizontal)
            {
                delta.x = childRectInScrollerSpace.xMin < contentRectInScroller.xMin ?
                    contentRectInScroller.xMin - childRectInScrollerSpace.xMin :
                    (childRectInScrollerSpace.xMax > contentRectInScroller.xMax ?
                    contentRectInScroller.xMax - childRectInScrollerSpace.xMax : 0);
            }

            if (this.vertical)
            {
                delta.y = childRectInScrollerSpace.yMin < contentRectInScroller.yMin ?
                    contentRectInScroller.yMin - childRectInScrollerSpace.yMin :
                    (childRectInScrollerSpace.yMax > contentRectInScroller.yMax ?
                    contentRectInScroller.yMax - childRectInScrollerSpace.yMax : 0);
            }

            //Try initialize content
            if (_contentPosition == null)
                InitializeAnimations();

            if (_contentPosition != null)
            {
                _contentPosition.duration = Mathf.Max(0, m_SnapToDuration);
                _contentPosition.value = this.content.anchoredPosition;
                _contentPosition.target = this.content.anchoredPosition + delta;
                this.velocity = new Vector2(0, 0);

                if (!animate || _contentPosition.duration <= 0)
                    _contentPosition.StopAnim(true);
            }
            else
            {
                this.content.anchoredPosition = this.content.anchoredPosition + delta;
            }
        }

        protected virtual void InitializeAnimations()
        {
            _contentPosition = null;
            if (this != null && this.content != null)
                _contentPosition = new Vector2AnimValue(this.content.anchoredPosition, this.content.anchoredPosition, this.m_SnapToDuration);

            _contentPosition.valueChanged.AddListener(() =>
            {
                if (this != null && this.content != null)
                {
                    this.content.anchoredPosition = _contentPosition.value;
                    this.velocity = new Vector2(0, 0);
                }
            });
        }

        protected virtual void StepAnimations()
        {
            if (_contentPosition != null && _contentPosition.isAnimating)
                _contentPosition.Step(Time.unscaledDeltaTime);
        }

        protected virtual void StopAnimations()
        {
            if (_contentPosition != null && _contentPosition.isAnimating)
                _contentPosition.StopAnim(true);
        }

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
                field = typeof(ScrollRect).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
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

        #region ILayoutElement

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

        #region Internal AnimatedValue

        protected class Vector2AnimValue
        {
            #region Private Variables

            Vector2 _initial = default(Vector2);
            Vector2 _value = default(Vector2);
            Vector2 _target = default(Vector2);

            float _duration = 0.5f;
            float _currentTime = 0f;

            #endregion

            #region Callbacks

            public UnityEvent valueChanged = new UnityEvent();
            public UnityEvent onAnimationEnd = new UnityEvent();

            #endregion

            #region Constructors

            public Vector2AnimValue(Vector2 value, Vector2 target, float duration = 0.5f)
            {
                _value = value;
                _target = target;
                _duration = duration;
            }

            #endregion

            #region Properties

            public float currentNormalizedDeltaTime
            {
                get
                {
                    return _currentTime < 0 || _duration < 0 ? 1 :
                        (_currentTime == 0 || _duration == 0 ? 0 :
                            Mathf.Clamp01(_currentTime / _duration));
                }
            }

            public float duration
            {
                get
                {
                    return _duration;
                }
                set
                {
                    if (Mathf.Approximately(_duration, value))
                        return;
                    _duration = value;
                    BeginAnim();
                }
            }

            public Vector2 target
            {
                get
                {
                    return _target;
                }
                set
                {
                    if (Vector2.Equals(_target, value))
                        return;
                    _target = value;
                    BeginAnim();
                }
            }

            public Vector2 value
            {
                get
                {
                    return _value;
                }
                set
                {
                    if (Vector2.Equals(_value, value))
                        return;
                    _value = value;
                    BeginAnim();
                }
            }

            public bool isAnimating
            {
                get
                {
                    return _currentTime >= 0 && _duration >= 0;
                }
            }

            #endregion

            #region Functions

            public void BeginAnim()
            {
                if (_duration >= 0)
                {
                    _currentTime = 0;
                    _initial = _value;
                }
                else
                {
                    _initial = _target;
                }
            }

            public void Step(float deltaTime)
            {
                if (isAnimating)
                {
                    _currentTime += deltaTime;
                    _value = Vector2.Lerp(_initial, _target, currentNormalizedDeltaTime);
                    valueChanged?.Invoke();

                    if (_currentTime > _duration)
                        StopAnim(true);
                }
            }

            public void StopAnim(bool finalize)
            {
                if (isAnimating)
                {
                    _currentTime = -1;
                    if (finalize)
                    {
                        _value = _target;
                        valueChanged?.Invoke();
                        onAnimationEnd?.Invoke();
                    }
                }
            }

            #endregion

            #region Implicit

            public static implicit operator Vector2AnimValue(Vector2 value)
            {
                return new Vector2AnimValue(value, value);
            }

            #endregion
        }

        #endregion
    }
}