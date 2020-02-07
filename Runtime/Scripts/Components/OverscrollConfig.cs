using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

namespace MaterialUI
{
    /// <summary>
    /// Component that handles overscroll effects for a ScrollRect.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    /// <seealso cref="UnityEngine.EventSystems.IInitializePotentialDragHandler" />
    [AddComponentMenu("MaterialUI/Overscroll Config", 50)]
    [DisallowMultipleComponent]
    public class OverscrollConfig : StyleElement<MaterialStylePanel.PanelStyleProperty>, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        #region Helper Classes

        protected internal class OverscrollObject
        {
            public GameObject gameObject { get; set; }
            public RectTransform rectTransform { get; set; }
            public Image image { get; set; }
            public CanvasGroup canvasGroup { get; set; }
            public int tweenId { get; protected set; }
            public bool isShow { get; protected set; }
            public bool isAnimating { get { return tweenId >= 0; } }

            public OverscrollObject()
            {
                tweenId = -1;
            }

            public virtual void TransitionPingPong(float animationDuration, float attenuation, float pingPongDelay, System.Action callback = null)
            {
                if (!isAnimating)
                {
                    if (isShow)
                    {
                        Transition(false, attenuation, animationDuration, 0f, callback);
                    }
                    else
                    {
                        Transition(true, attenuation, animationDuration, 0f, () =>
                        {
                            Transition(false, attenuation, animationDuration, pingPongDelay, callback);
                        });
                    }
                }
                else
                    callback.InvokeIfNotNull();
            }

            public virtual void Transition(bool show, float attenuation, float animationDuration, float delay = 0, System.Action callback = null)
            {
                attenuation = Mathf.Clamp01(attenuation);
                if (gameObject != null && isShow != show)
                {
                    gameObject.SetActive(true);

                    isShow = show;
                    var inverseTargetValue = show ? 0f : attenuation;
                    var targetValue = show ? attenuation : 0f;
                    var currentValue = Mathf.Clamp01(rectTransform.localScale.y);
                    TweenManager.EndTween(tweenId);
                    tweenId = TweenManager.TweenFloat((value) =>
                    {
                        //Update Color and Scale
                        if (gameObject != null)
                        {
                            if (canvasGroup != null)
                                canvasGroup.alpha = value;
                            if (rectTransform != null)
                                rectTransform.localScale = new Vector3(rectTransform.localScale.x, value, rectTransform.localScale.z);

                        }
                    },
                    currentValue,
                    targetValue,
                    animationDuration,
                    0,
                    () =>
                    {
                        tweenId = -1;
                        if (gameObject != null && !isShow)
                            gameObject.SetActive(false);
                        callback.InvokeIfNotNull();
                    });
                }
                else
                {
                    isShow = show;
                    callback.InvokeIfNotNull();
                }
            }

            public virtual void TransitionImmediate(bool show, float attenuation)
            {
                attenuation = Mathf.Clamp01(attenuation);
                isShow = show;
                if (gameObject != null)
                {
                    gameObject.SetActive(isShow);
                    var targetValue = show ? attenuation : 0f;
                    TweenManager.EndTween(tweenId);
                    tweenId = -1;

                    if (canvasGroup != null)
                        canvasGroup.alpha = targetValue;
                    if (rectTransform != null)
                        rectTransform.localScale = new Vector3(rectTransform.localScale.x, targetValue, rectTransform.localScale.z);
                }
            }

            public virtual void StopTransition()
            {
                TweenManager.EndTween(tweenId);
                tweenId = -1;
            }
        }

        #endregion

        #region Private Variables

        //  0 = Left
        //  1 = Right
        //  2 = Top
        //  3 = Bottom

        [SerializeField, SerializeStyleProperty]
        private Color m_OverscrollColor = new Color(0f, 0f, 0f, 0.25f);
        [SerializeField, SerializeStyleProperty]
        private float m_OverscrollScale = 1;

        private readonly OverscrollObject[] m_OverscrollObjects = new OverscrollObject[4];

        private ScrollRect _ScrollRect;

        private Vector2 _MousePositionNormalized = Vector2.zero;
        private Vector2 _ScrollPosition;
        private Vector2 _LastScrollPosition;

        private bool _IsOverScrollEnabled = false;
        private float _RealDistanceDeltaTolerance = 1;
        private bool _IsPressing = false;
        private Vector2 _ImpactPosition;
        private bool _SkipNextDrag = false;

        private static float _AnimationDuration = 0.3f;
        private static Vector2 _EffectSize = new Vector2(100f, 30f);
        private static Sprite _OverscrollSprite;

        #endregion

        #region Properties

        protected BaseInput input
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        bool isPressing
        {
            get
            {
                return _IsPressing;
            }
            set
            {
                if (_IsPressing == value)
                    return;
                _IsPressing = value;
                if (!_IsPressing)
                {
                    //foreach (var objectToDisable in m_OverscrollObjects)
                    for(int i=0; i<m_OverscrollObjects.Length; i++)
                    {
                        var overScrollObject = m_OverscrollObjects[i];
                        if (overScrollObject != null && overScrollObject.isShow && !overScrollObject.isAnimating)
                        {
                            RecalculateEffectPosition(i, true);
                            overScrollObject.Transition(false, 0, _AnimationDuration);
                        }
                    }
                }
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                return transform as RectTransform;
            }
        }

        public ScrollRect scrollRect
        {
            get
            {
                if (_ScrollRect == null)
                    _ScrollRect = GetComponent<ScrollRect>();
                return _ScrollRect;
            }
        }

        public Color overscrollColor
        {
            get { return m_OverscrollColor; }
            set
            {
                m_OverscrollColor = value;
                for (int i = 0; i < m_OverscrollObjects.Length; i++)
                {
                    if (m_OverscrollObjects[i] == null)
                        m_OverscrollObjects[i] = new OverscrollObject();
                    if (m_OverscrollObjects[i].image != null)
                    {
                        m_OverscrollObjects[i].image.color = m_OverscrollColor;
                    }
                }
            }
        }

        public float overscrollScale
        {
            get { return m_OverscrollScale; }
            set { m_OverscrollScale = value; }
        }

        #endregion

        #region Unity Functions

        bool _Started = false;
        protected override void Start()
        {
            base.Start();
            _Started = true;
            Setup();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_Started)
                Setup();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClearOverScrolls();
            CancelInvoke();
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            _SkipNextDrag = true;
            if (isActiveAndEnabled)
            {
                isPressing = true;
                if (_ScrollRect != null)
                    _ScrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (_SkipNextDrag)
            {
                _SkipNextDrag = false;
                return;
            }
            if (isActiveAndEnabled)
            {
                if (scrollRect != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(this.rectTransform, eventData.position, eventData.pressEventCamera, out _MousePositionNormalized);
                    _MousePositionNormalized = Rect.PointToNormalized(rectTransform.rect, _MousePositionNormalized);
                    OnScrollRectValueChanged(scrollRect.normalizedPosition);
                }
            }
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            _SkipNextDrag = false;
            if (isActiveAndEnabled)
            {
                isPressing = false;
                if (_ScrollRect != null)
                    _ScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            if (_Started && Application.isPlaying)
            {
                CancelInvoke("UpdateOverScroll");
                Invoke("UpdateOverScroll", 0);
            }
        }

        protected virtual void OnScrollRectValueChanged(Vector2 normalizedValue)
        {
            _LastScrollPosition = _ScrollPosition;
            _ScrollPosition = ClampVector2(normalizedValue);

            var delta = _ScrollPosition - _LastScrollPosition;

            //Check if effect is Active
            var overScrollEnabled = ((delta.x != 0) && IsScrollHorizontalEnabled() && (_ScrollPosition.x == 0 || _ScrollPosition.x == 1)) ||
                                    ((delta.y != 0) && IsScrollVerticalEnabled() && (_ScrollPosition.y == 0 || _ScrollPosition.y == 1));

            //Change cached state
            if (_IsOverScrollEnabled != overScrollEnabled)
            {
                _ImpactPosition = input.mousePosition;
                _IsOverScrollEnabled = overScrollEnabled;
            }

            //Check Special Cases
            if (!overScrollEnabled)
            {
                overScrollEnabled = (isPressing && m_OverscrollObjects.Where((o) => { return o != null && !o.isAnimating && o.isShow; }).Count() > 0) ||
                  ((isPressing || delta.x != 0) && IsScrollHorizontalEnabled() && (_ScrollPosition.x == 0 || _ScrollPosition.x == 1)) ||
                  ((isPressing || delta.y != 0) && IsScrollVerticalEnabled() && (_ScrollPosition.y == 0 || _ScrollPosition.y == 1));
            }

            //Calculate Impact Force (this cicle) and accumulatedImpact (when isPressing is true)
            float impactForce = Mathf.Clamp01(Mathf.Abs(scrollRect.velocity.magnitude) * 0.001f);

            if (overScrollEnabled)
            {
                var indexes = GetValidIndexes(_ScrollPosition, isPressing ? null : (Vector2?)delta);

                for (int i = 0; i < m_OverscrollObjects.Length; i++)
                {
                    var overscrollObject = m_OverscrollObjects[i];
                    if (overscrollObject != null)
                    {
                        var isValid = indexes.Contains(i);
                        //When IsPressing == false we can auto animate effect
                        if (!_IsPressing)
                        {
                            if (isValid)
                            {
                                if (overscrollObject.gameObject == null || !overscrollObject.gameObject.activeSelf)
                                    CreateOverscroll(i);
                                overscrollObject.TransitionPingPong(_AnimationDuration, impactForce, 0.3f, null);
                            }
                            else if (!overscrollObject.isAnimating && overscrollObject.isShow)
                                overscrollObject.Transition(false, 0f, 0.3f);
                        }
                        //Dont animate effect (we are dragging)
                        else
                        {
                            if (isValid)
                            {
                                if (overscrollObject.gameObject == null || !overscrollObject.gameObject.activeSelf)
                                    CreateOverscroll(i);
                                impactForce = Mathf.Clamp01((_ImpactPosition - (Vector2)input.mousePosition).magnitude * 0.005f);
                                overscrollObject.TransitionImmediate(true, impactForce);
                            }
                            else if (overscrollObject.isShow)
                                overscrollObject.Transition(false, 0f, 0.3f);
                        }

                        //Recalculate position
                        if (!overscrollObject.isAnimating)
                            RecalculateEffectPosition(i, isPressing || !isValid);
                    }
                }
            }
        }

        #endregion

        #region Helper Functions

        protected virtual HashSet<int> GetValidIndexes(Vector2 normalizedValue, Vector2? delta)
        {
            HashSet<int> indexes = new HashSet<int>();

            if (normalizedValue.x == 0 && (delta == null || delta.Value.x < 0))
            {
                if (m_OverscrollObjects[0] != null)
                    indexes.Add(0);
            }
            else if (normalizedValue.x == 1 && (delta == null || delta.Value.x > 0))
            {
                if (m_OverscrollObjects[1] != null)
                    indexes.Add(1);
            }

            if (normalizedValue.y == 1 && (delta == null || delta.Value.y > 0))
            {
                if (m_OverscrollObjects[2] != null)
                    indexes.Add(2);
            }
            else if (normalizedValue.y == 0 && (delta == null || delta.Value.y < 0))
            {
                if (m_OverscrollObjects[3] != null)
                    indexes.Add(3);
            }

            return indexes;
        }

        protected virtual Vector2 ClampVector2(Vector2 normalizedValue)
        {
            var contentSize = _ScrollRect != null ? new Vector2(GetLocalWidth(_ScrollRect.content), GetLocalHeight(_ScrollRect.content)) : Vector2.zero;
            var nonNormalizedValue = normalizedValue * contentSize;

            normalizedValue.x = Mathf.Clamp01(normalizedValue.x);
            if (normalizedValue.x > 0 && normalizedValue.x < 1)
            {
                if (Mathf.Abs(nonNormalizedValue.x) < _RealDistanceDeltaTolerance)
                    normalizedValue.x = 0;
                else if (Mathf.Abs(contentSize.x - nonNormalizedValue.x) < _RealDistanceDeltaTolerance)
                    normalizedValue.x = 1;
            }

            normalizedValue.y = Mathf.Clamp01(normalizedValue.y);
            if (normalizedValue.y > 0 && normalizedValue.y < 1)
            {
                if (Mathf.Abs(nonNormalizedValue.y) < _RealDistanceDeltaTolerance)
                    normalizedValue.y = 0;
                else if (Mathf.Abs(contentSize.y - nonNormalizedValue.y) < _RealDistanceDeltaTolerance)
                    normalizedValue.y = 1;
            }

            return normalizedValue;
        }

        protected float GetLocalWidth(RectTransform rectTransform)
        {
            if (rectTransform != null)
                return rectTransform.rect.width;
            return 0;
        }

        protected float GetLocalHeight(RectTransform rectTransform)
        {
            if (rectTransform != null)
                return rectTransform.rect.height;
            return 0;
        }

        protected bool IsScrollEnabled()
        {
            return IsScrollHorizontalEnabled() || IsScrollVerticalEnabled();
        }

        protected bool IsScrollHorizontalEnabled()
        {
            return _ScrollRect != null && _ScrollRect.enabled && _ScrollRect.horizontal && GetLocalWidth(rectTransform) < GetLocalWidth(scrollRect.content);
        }

        protected bool IsScrollVerticalEnabled()
        {
            return _ScrollRect != null && _ScrollRect.enabled && _ScrollRect.vertical && GetLocalHeight(rectTransform) < GetLocalHeight(scrollRect.content);
        }

        private void ClearOverScrolls()
        {
            if (_ScrollRect != null)
                _ScrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);

            for (int i = 0; i < m_OverscrollObjects.Length; i++)
            {
                if (m_OverscrollObjects[i] != null)
                {
                    if (m_OverscrollObjects[i].gameObject != null)
                    {
                        if (Application.isPlaying)
                            GameObject.Destroy(m_OverscrollObjects[i].gameObject);
                        else
                            GameObject.DestroyImmediate(m_OverscrollObjects[i].gameObject);
                    }
                    m_OverscrollObjects[i] = null;
                }
            }
        }

        /// <summary>
        /// Creates an overscroll object.
        /// </summary>
        /// <param name="i">The index/direction of the overscroll object.</param>
        protected void CreateOverscroll(int i, bool canCreate = true)
        {
            if (!Application.isPlaying)
                return;

            if (_OverscrollSprite == null)
                _OverscrollSprite = Resources.Load<Sprite>("Overscroll");

            if (m_OverscrollObjects[i] == null)
            {
                if (!canCreate)
                    return;
                m_OverscrollObjects[i] = new OverscrollObject();
            }

            if (m_OverscrollObjects[i].gameObject == null)
            {
                if (!canCreate)
                    return;
                m_OverscrollObjects[i].gameObject = new GameObject { name = "Overscroll Effect" };
            }
            //Force only update self overscroll (optimized performance)
            var layoutElement = m_OverscrollObjects[i].gameObject.GetAddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            m_OverscrollObjects[i].image = m_OverscrollObjects[i].gameObject.GetAddComponent<Image>();
            m_OverscrollObjects[i].image.sprite = _OverscrollSprite;
            m_OverscrollObjects[i].image.color = m_OverscrollColor;

            m_OverscrollObjects[i].rectTransform = m_OverscrollObjects[i].gameObject.GetAddComponent<RectTransform>();
            m_OverscrollObjects[i].rectTransform.SetParent(transform);

            Vector2 localPos = Vector2.zero;
            Vector2 size = Vector2.zero;
            Vector2 anchorMin = Vector2.zero;
            Vector2 anchorMax = Vector2.zero;
            Vector3 rotation = Vector3.zero;

            switch (i)
            {
                //Left
                case 0:
                    localPos = new Vector2(rectTransform.rect.xMin, rectTransform.rect.center.y);
                    size = new Vector2(GetLocalHeight(rectTransform), 0f);
                    anchorMin = new Vector2(0f, 0.5f);
                    anchorMax = new Vector2(0f, 0.5f);
                    rotation = new Vector3(0f, 0f, 270f);
                    break;
                //Right
                case 1:
                    localPos = new Vector2(rectTransform.rect.xMax, rectTransform.rect.center.y);
                    size = new Vector2(GetLocalHeight(rectTransform), 0f);
                    anchorMin = new Vector2(1f, 0.5f);
                    anchorMax = new Vector2(1f, 0.5f);
                    rotation = new Vector3(0f, 0f, 90f);
                    break;
                //Top
                case 2:
                    localPos = new Vector2(rectTransform.rect.center.x, rectTransform.rect.yMax);
                    size = new Vector2(GetLocalWidth(rectTransform), 0f);
                    anchorMin = new Vector2(0.5f, 1f);
                    anchorMax = new Vector2(0.5f, 1f);
                    rotation = new Vector3(0f, 0f, 180f);
                    break;
                //Bottom
                case 3:
                    localPos = new Vector2(rectTransform.rect.center.x, rectTransform.rect.yMin);
                    size = new Vector2(GetLocalWidth(rectTransform), 0f);
                    anchorMin = new Vector2(0.5f, 0f);
                    anchorMax = new Vector2(0.5f, 0f);
                    rotation = new Vector3(0f, 0f, 0f);
                    break;
            }

            size += _EffectSize;
            //size.y = _OverscrollSprite.rect.width == 0 ? 0 : size.x * (_OverscrollSprite.rect.height/_OverscrollSprite.rect.width);

            m_OverscrollObjects[i].rectTransform.sizeDelta = size;
            m_OverscrollObjects[i].rectTransform.anchorMin = anchorMin;
            m_OverscrollObjects[i].rectTransform.anchorMax = anchorMax;
            m_OverscrollObjects[i].rectTransform.pivot = new Vector2(0.5f, 0f);
            m_OverscrollObjects[i].rectTransform.localEulerAngles = rotation;
            m_OverscrollObjects[i].rectTransform.localScale = Vector3.one;
            m_OverscrollObjects[i].rectTransform.anchoredPosition = localPos;

            m_OverscrollObjects[i].canvasGroup = m_OverscrollObjects[i].gameObject.GetAddComponent<CanvasGroup>();
            m_OverscrollObjects[i].canvasGroup.alpha = 1;
            m_OverscrollObjects[i].canvasGroup.blocksRaycasts = false;
            m_OverscrollObjects[i].canvasGroup.interactable = false;
            //m_OverscrollObjects[i].canvasGroup.ignoreParentGroups = true;

            //Optimize redraw
            m_OverscrollObjects[i].gameObject.GetAddComponent<Canvas>();

            m_OverscrollObjects[i].TransitionImmediate(false, 0f);
        }

        protected virtual void RecalculateEffectPosition(int edge, bool useMousePosition)
        {
            if (m_OverscrollObjects[edge] == null || m_OverscrollObjects[edge].rectTransform == null)
                return;

            var rect = rectTransform.rect;
            var clamp = _EffectSize.x / 3.0f;
            var offset = useMousePosition? 
                new Vector2(Mathf.Clamp((_MousePositionNormalized.x - 0.5f) / 2f * rect.width, -clamp, clamp), 
                            Mathf.Clamp((_MousePositionNormalized.y - 0.5f) / 2f * rect.height, -clamp, clamp)) : 
                Vector2.zero;
            var localPos = Vector2.zero;
            switch (edge)
            {
                case 0:
                    localPos = new Vector2(rect.xMin, rect.center.y + offset.y);
                    break;
                case 1:
                    localPos = new Vector2(rect.xMax, rect.center.y + offset.y);
                    break;
                case 2:
                    localPos = new Vector2(rect.center.x + offset.x, rect.yMax);
                    break;
                case 3:
                    localPos = new Vector2(rect.center.x + offset.x, rect.yMin);
                    break;
            }

            m_OverscrollObjects[edge].rectTransform.localPosition = localPos;
        }

        /// <summary>
        /// Calculates which/if overscrolls are needed and generates them accordingly.
        /// This should be called if the ScrollRect or content sizes change. 
        /// </summary>
        public void Setup()
        {
            ClearOverScrolls();

            if (scrollRect == null || scrollRect.movementType == ScrollRect.MovementType.Unrestricted)
            {
                string log = _ScrollRect == null ? "[OverScroll] Invalid ScrollRect" : "[OverScroll] Can only setup if ScrollRect does not have 'MovementType Unrestricted' (sender: " + name + ")";

                Debug.Log(log);
                enabled = false;
                return;
            }

            if (rectTransform == null)
                gameObject.GetAddComponent<RectTransform>();
            if (scrollRect != null)
                _ScrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);

            _ScrollPosition = ClampVector2(_ScrollRect.normalizedPosition);
            _LastScrollPosition = ClampVector2(_ScrollRect.normalizedPosition);

            UpdateOverScroll();
        }

        public void UpdateOverScroll()
        {
            if (IsScrollHorizontalEnabled())
            {
                CreateOverscroll(0);
                CreateOverscroll(1);
            }
            if (IsScrollVerticalEnabled())
            {
                CreateOverscroll(2);
                CreateOverscroll(3);
            }
        }

        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0);
        }

        #endregion
    }
}