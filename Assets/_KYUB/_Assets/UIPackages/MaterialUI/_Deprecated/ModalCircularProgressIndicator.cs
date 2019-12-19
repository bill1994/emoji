using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    //[ExecuteInEditMode]
    [System.Obsolete("Use DialogProgress Instead")]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("MaterialUI/Progress/ModalCircular Progress Indicator")]
    public class ModalCircularProgressIndicator : CircularProgressIndicator
    {
        #region Private Variables

        [SerializeField]
        protected bool m_UseFocusGroup = true;

        [Space]
        [SerializeField, SerializeStyleProperty]
        protected bool m_hasBackground = true;
        [SerializeField, SerializeStyleProperty]
        protected Color m_BackgroundColor = Color.black;
        [SerializeField, SerializeStyleProperty]
        protected bool m_UseBackgroundColorAlpha = false;
        [SerializeField, SerializeStyleProperty]
        protected float m_AnimationDuration = 0.25f;
        [Space]
        [SerializeField] protected bool m_DestroyOnHide = false;

        [SerializeField]
        protected DialogBackground m_DialogBackgroundOverride = null;

        protected DialogBackground m_Background;

        #endregion

        #region Public Properties

        public bool destroyOnHide
        {
            get { return m_DestroyOnHide; }
            set { m_DestroyOnHide = value; }
        }

        public float animationDuration
        {
            get
            {
                return m_AnimationDuration;
            }

            set
            {
                m_AnimationDuration = value;
            }
        }

        public bool useFocusGroup
        {
            get { return m_UseFocusGroup; }
            set { m_UseFocusGroup = value; }
        }

        public DialogBackground background
        {
            get
            {
                if (m_Background == null && m_hasBackground)
                {
                    if (m_DialogBackgroundOverride != null)
                    {
                        var v_internalPanel = m_DialogBackgroundOverride.gameObject.scene.IsValid();
                        if (!v_internalPanel)
                        {
                            m_Background = GameObject.Instantiate(m_DialogBackgroundOverride, v_internalPanel ? m_DialogBackgroundOverride.transform.parent : this.rectTransform.parent);
                            m_Background.transform.localScale = v_internalPanel ? m_DialogBackgroundOverride.transform.localScale : Vector3.one;
                            m_Background.transform.localEulerAngles = v_internalPanel ? m_DialogBackgroundOverride.transform.localEulerAngles : Vector3.zero;
                            m_Background.transform.localPosition = v_internalPanel ? m_DialogBackgroundOverride.transform.localPosition : Vector3.zero;
                        }
                        else
                            m_Background = m_DialogBackgroundOverride;
                        m_Background.gameObject.SetActive(true);

                        if(!v_internalPanel)
                            m_Background.SetSiblingIndex(this.rectTransform.GetSiblingIndex());
                    }
                    else
                    {
                        m_Background = PrefabManager.InstantiateGameObject("Dialogs/Dialog Background", this.rectTransform.parent).GetComponent<DialogBackground>();
                        m_Background.SetSiblingIndex(this.rectTransform.GetSiblingIndex());
                    }
                    m_Background.GetComponent<Image>().color = m_BackgroundColor;
                    if (m_UseBackgroundColorAlpha)
                        m_Background.backgroundAlpha = m_BackgroundColor.a;
                }

                return m_Background;
            }
        }

        public bool useBackgroundColorAlpha
        {
            get
            {
                return m_UseBackgroundColorAlpha;
            }

            set
            {
                m_UseBackgroundColorAlpha = value;
            }
        }

        protected virtual void InitializeFocusGroup()
        {
            var v_materialKeyFocus = GetComponent<MaterialFocusGroup>();
            if (m_UseFocusGroup && v_materialKeyFocus == null)
            {
                v_materialKeyFocus = gameObject.AddComponent<MaterialFocusGroup>();
            }
            if (v_materialKeyFocus != null)
            {
                v_materialKeyFocus.enabled = m_UseFocusGroup;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Update()
        {
            //Background destroyed bug the progress is up, so we must recreate the background
            if (!IsAnimatingSize(false) && 
                scaledRectTransform.localScale.x > 0 && //Prevent creating BG when StartHidden
                m_hasBackground && background != null)
            {
                background.AnimateShowBackground(null, m_AnimationDuration);
            }

            base.Update(); 
        }

        #endregion

        #region Helper Functions

        public override void Show(bool startIndeterminate)
        {
            Show(startIndeterminate, null);
        }

        public override void Show(bool startIndeterminate, string labelText)
        {
            if(m_hasBackground && background != null)
            {
                background.AnimateShowBackground(null, m_AnimationDuration);
            }
            InitializeFocusGroup();
            base.Show(startIndeterminate, labelText);
        }

        public override void Hide()
        {
            if(m_hasBackground && m_Background != null)
            {
                m_Background.AnimateHideBackground(null, m_AnimationDuration);
            }

            base.Hide();
        }

        protected override void HandleOnHideAnimationFinished()
        {
            base.HandleOnHideAnimationFinished();
            if (Application.isPlaying && m_DestroyOnHide)
                GameObject.Destroy(this.gameObject);
        }

        #endregion
    }
}