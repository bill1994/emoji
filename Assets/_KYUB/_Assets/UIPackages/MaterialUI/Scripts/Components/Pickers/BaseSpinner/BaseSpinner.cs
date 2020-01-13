using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    public enum SpinnerMode { AutoDetect, Dialog, Dropdown }

    public interface IBaseSpinner
    {
        RectTransform rectTransform { get; }
        SpinnerMode spinnerMode { get; set; }
        Vector2 dropdownExpandPivot { get; set; }
        Vector2 dropdownFramePivot { get; set; }
        Vector2 dropdownFramePreferredSize { get; set; }

        bool IsDestroyed();
    }

    public abstract class BaseSpinner<T> : StyleElement<MaterialStylePanel.PanelStyleProperty>, IBaseSpinner where T : StyleProperty, new()
    {
        #region Private Variables

        [SerializeField]
        protected SpinnerMode m_SpinnerMode = SpinnerMode.AutoDetect;
        [SerializeField]
        protected Vector2 m_DropdownExpandPivot = new Vector2(0.5f, 1);
        [SerializeField]
        protected Vector2 m_DropdownFramePivot = new Vector2(0.5f, 1);
        [SerializeField]
        protected Vector2 m_DropdownFramePreferredSize = new Vector2(-1, -1);

        #endregion

        #region Callback

        [UnityEngine.Serialization.FormerlySerializedAs("OnPickerFailedCallback")]
        public UnityEvent OnCancelCallback;

        #endregion

        #region Public Properties

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

        public virtual MaterialButton button
        {
            get
            {
                return GetComponent<MaterialButton>();
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

        #endregion

        #region Public Helper Functions

        public abstract bool IsExpanded();

        public abstract void Show();

        public abstract void Hide();

        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0);
        }

        protected bool IsDialogMode()
        {
            return m_SpinnerMode == SpinnerMode.Dialog ||
#if (UNITY_ANDROID || UNITY_IOS) && UNITY_EDITOR
                (m_SpinnerMode == SpinnerMode.AutoDetect);
#else
                (m_SpinnerMode == SpinnerMode.AutoDetect && Application.isMobilePlatform);
#endif
        }

        protected void ShowFrameActivity<TFrame>(TFrame cachedFrame, string dialogPrefabPath, System.Action<TFrame, bool> initializeCallback) where TFrame : MaterialDialogFrame
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

                    //DialogManager.ShowRadioListAsync(options.ToArray(), Select, "OK", hintOption.text, hintOption.imageData, HandleOnHide, "Cancel", selectedIndex, false,

                    DialogManager.ShowCustomDialogAsync<TFrame>(dialogPrefabPath,
                        (dialog) =>
                        {
                            if(initializeCallback != null)
                                initializeCallback(dialog, true);
                        });
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

        protected MaterialSpinnerActivity CreateSpinnerActivity(MaterialFrame frame)
        {
            if (frame == null)
                return null;

            MaterialSpinnerActivity activity = new GameObject(frame.name + " (SpinnerActivity)").AddComponent<MaterialSpinnerActivity>();
            activity.Build(DialogManager.rectTransform);
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
            if (button != null)
                button.onClick.AddListener(Show);
        }

        protected virtual void UnregisterEvents()
        {
            if (button != null)
                button.onClick.RemoveListener(Show);
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
