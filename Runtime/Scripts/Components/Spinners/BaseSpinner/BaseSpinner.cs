using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    public enum SpinnerMode { AutoDetect, Dialog, Dropdown }

    [System.Flags]
    public enum SpinnerUIEventTriggerMode { None = 0, OnPointerEnter = 1, OnPointerExit = 2, OnPointerDown = 4, OnPointerUp = 8, OnPointerClick = 16 }

    public interface IBaseSpinner
    {
        RectTransform rectTransform { get; }
        SpinnerMode spinnerMode { get; set; }
        Vector2 dropdownExpandPivot { get; set; }
        Vector2 dropdownOffset { get; set; }
        Vector2 dropdownFramePivot { get; set; }
        Vector2 dropdownFramePreferredSize { get; set; }

        bool IsDestroyed();
    }

    public abstract class BaseSpinner<T> : StyleElement<MaterialStylePanel.PanelStyleProperty>, 
        IBaseSpinner, 
        IPointerClickHandler, 
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler, 
        IPointerUpHandler where T : StyleProperty, new()
    {
        #region Private Variables

        [SerializeField]
        protected SpinnerMode m_SpinnerMode = SpinnerMode.AutoDetect;
        [SerializeField]
        protected bool m_OpenDialogAsync = true;
        [SerializeField]
        protected Vector2 m_DropdownOffset = new Vector2(0, 0);
        [SerializeField]
        protected Vector2 m_DropdownExpandPivot = new Vector2(0, 0);
        [SerializeField]
        protected Vector2 m_DropdownFramePivot = new Vector2(0, 1);
        [SerializeField, Tooltip("* <VALUE> == -1 will force use preferred size defined in panel.\n* <VALUE> == 0 or <VALUE> < -1 will override preferred size in panel to -1.\n* <VALUE> > 0 will override preferred size in panel to <VALUE>")]
        protected Vector2 m_DropdownFramePreferredSize = new Vector2(-1, -1); // 0 or < -1 will override the preferred value to -1. 
        [SerializeField]
        protected SpinnerUIEventTriggerMode m_UIShowTriggerMode = SpinnerUIEventTriggerMode.OnPointerClick;
        [SerializeField]
        protected SpinnerUIEventTriggerMode m_UIHideTriggerMode = SpinnerUIEventTriggerMode.None;

        #endregion

        #region Callback

        [UnityEngine.Serialization.FormerlySerializedAs("OnPickerFailedCallback")]
        public UnityEvent OnCancelCallback;

        #endregion

        #region Public Properties

        public virtual bool OpenDialogAsync
        {
            get
            {
                return m_OpenDialogAsync;
            }
            set
            {
                if (m_OpenDialogAsync == value)
                    return;
                m_OpenDialogAsync = value;
            }
        }

        public virtual SpinnerUIEventTriggerMode uiShowTriggerMode
        {
            get
            {
                return m_UIShowTriggerMode;
            }
            set
            {
                if (m_UIShowTriggerMode == value)
                    return;
                m_UIShowTriggerMode = value;
            }
        }

        public virtual SpinnerUIEventTriggerMode uiHideTriggerMode
        {
            get
            {
                return m_UIHideTriggerMode;
            }
            set
            {
                if (m_UIHideTriggerMode == value)
                    return;
                m_UIHideTriggerMode = value;
            }
        }

        public SpinnerMode spinnerMode
        {
            get
            {
                return m_SpinnerMode;
            }

            set
            {
                m_SpinnerMode = value;
            }
        }

        public Vector2 dropdownExpandPivot
        {
            get
            {
                return m_DropdownExpandPivot;
            }

            set
            {
                m_DropdownExpandPivot = value;
            }
        }

        public Vector2 dropdownOffset
        {
            get
            {
                return m_DropdownOffset;
            }

            set
            {
                m_DropdownOffset = value;
            }
        }

        public Vector2 dropdownFramePivot
        {
            get
            {
                return m_DropdownFramePivot;
            }

            set
            {
                m_DropdownFramePivot = value;
            }
        }

        public Vector2 dropdownFramePreferredSize
        {
            get
            {
                return m_DropdownFramePreferredSize;
            }

            set
            {
                m_DropdownFramePreferredSize = value;
            }
        }

        public RectTransform rectTransform
        {
            get
            {
                return transform as RectTransform;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            RegisterEvents();
            base.OnEnable();
        }

        protected override void Start()
        {
            base.Start();
            Init();
        }

        protected override void OnDisable()
        {
            UnregisterEvents();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
        }

#endif
        public void OnPointerClick(PointerEventData eventData)
        {
            var isExpand = IsExpanded();
            if (!isExpand && uiShowTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerClick))
                Show();
            if (isExpand && uiHideTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerClick))
                Hide();

        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var isExpand = IsExpanded();
            if (!isExpand && uiShowTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerEnter))
                Show();
            if (isExpand && uiHideTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerEnter))
                Hide();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            var isExpand = IsExpanded();
            if (!isExpand && uiShowTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerExit))
                Show();
            if (isExpand && uiHideTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerExit))
                Hide();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            var isExpand = IsExpanded();
            if (!isExpand && uiShowTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerDown))
                Show();
            if (isExpand && uiHideTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerDown))
                Hide();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            var isExpand = IsExpanded();
            if (!isExpand && uiShowTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerUp))
                Show();
            if (isExpand && uiHideTriggerMode.HasFlag(SpinnerUIEventTriggerMode.OnPointerUp))
                Hide();
        }

        #endregion

        #region Public Helper Functions

        public abstract bool IsExpanded();

        public abstract void Show();

        public abstract void Hide();

        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0);
        }

        protected virtual bool IsDialogMode()
        {
            return m_SpinnerMode == SpinnerMode.Dialog ||
#if (UNITY_ANDROID || UNITY_IOS) && UNITY_EDITOR
                (m_SpinnerMode == SpinnerMode.AutoDetect);
#else
                (m_SpinnerMode == SpinnerMode.AutoDetect && Application.isMobilePlatform);
#endif
        }

        protected virtual void ShowFrameActivity<TFrame>(TFrame cachedFrame, string dialogPrefabPath, System.Action<TFrame, bool> initializeCallback) where TFrame : MaterialDialogFrame
        {
            if (IsDialogMode())
            {
                if (cachedFrame == null || !(cachedFrame.activity is MaterialDialogActivity))
                {
                    if (cachedFrame != null && cachedFrame.activity != null)
                    {
                        cachedFrame.activity.destroyOnHide = true;
                        cachedFrame.activity.Hide();
                    }

                    System.Action<TFrame> initDelegate = (dialog) =>
                    {
                        cachedFrame = dialog;
                        if (initializeCallback != null)
                            initializeCallback(dialog, true);
                    };
                    if (m_OpenDialogAsync)
                        DialogManager.ShowCustomDialogAsync<TFrame>(dialogPrefabPath, initDelegate);
                    else
                        DialogManager.ShowCustomDialog<TFrame>(dialogPrefabPath, initDelegate);
                }
                else
                {
                    if(initializeCallback != null)
                        initializeCallback(cachedFrame, true);
                    cachedFrame.Show();
                }
            }
            else
            {
                if (cachedFrame == null || !(cachedFrame.activity is MaterialSpinnerActivity))
                {
                    if (cachedFrame != null && cachedFrame.activity != null)
                    {
                        cachedFrame.activity.destroyOnHide = true;
                        cachedFrame.activity.Hide();
                    }

                    cachedFrame = PrefabManager.InstantiateGameObject(dialogPrefabPath, null).GetComponent<TFrame>();
                    CreateSpinnerActivity(cachedFrame);
                }

                if (cachedFrame != null)
                {
                    if (initializeCallback != null)
                        initializeCallback(cachedFrame, false);
                    var activity = (cachedFrame.activity as MaterialSpinnerActivity);
                    if (activity != null)
                        activity.RecalculatePosition(this);
                    cachedFrame.Show();
                }
            }
        }

        protected virtual MaterialSpinnerActivity CreateSpinnerActivity(MaterialFrame frame)
        {
            if (frame == null)
                return null;

            MaterialSpinnerActivity activity = new GameObject(frame.name + " (SpinnerActivity)").AddComponent<MaterialSpinnerActivity>();

            //Setup Has Background
            MaterialDialogCompat dialogFrame = frame as MaterialDialogCompat;
            if (dialogFrame != null)
                activity.hasBackground = dialogFrame.hasBackground;

            if (DialogManager.rectTransform != null)
                activity.Build(DialogManager.rectTransform);
            else
                activity.Build(this.transform.GetRootCanvas());

            activity.SetFrame(frame, this);

            return activity;
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void Init()
        {

        }
        
        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
        }

        protected virtual void UnregisterEvents()
        {
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnHide()
        {
            if (OnCancelCallback != null)
                OnCancelCallback.Invoke();
        }

        #endregion
    }
}
