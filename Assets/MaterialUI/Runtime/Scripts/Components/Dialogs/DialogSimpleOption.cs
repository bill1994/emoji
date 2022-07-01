// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using Kyub.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Simple Option", 100)]
    public class DialogSimpleOption : DialogClickableOption
    {
        #region Private Variables

        [SerializeField]
        protected Graphic m_ItemIcon = null;
        [SerializeField]
        protected Graphic m_ItemText = null;
        [SerializeField]
        protected MaterialRipple m_ItemRipple = null;
        [SerializeField]
        protected CanvasGroup m_InteractionCanvasGroup = null;
        [SerializeField]
        protected float m_CanvasDisableAlphaValue = 0.5f;

        #endregion

        #region Public Properties

        public CanvasGroup interactionCanvasGroup
        {
            get { return m_InteractionCanvasGroup; }
            set { m_InteractionCanvasGroup = value; }
        }

        public Graphic itemText
        {
            get { return m_ItemText; }
            set { m_ItemText = value; }
        }

        public Graphic itemIcon
        {
            get { return m_ItemIcon; }
            set { m_ItemIcon = value; }
        }

        public MaterialRipple itemRipple
        {
            get { return m_ItemRipple; }
            set { m_ItemRipple = value; }
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

        protected bool _isChangingCanvasGroup = false;
        protected virtual void OnCanvasGroupChanged()
        {
            if (!_isChangingCanvasGroup && !Kyub.Performance.SustainedPerformanceManager.IsSettingLowPerformance)
            {
                try
                {
                    ApplyInteratableColor();
                }
                finally
                {
                    _isChangingCanvasGroup = false;
                }
            }
        }

        #endregion

        #region Reload Functions

        protected override void ApplyReload(ScrollDataView.ReloadEventArgs oldArgs, ScrollDataView.ReloadEventArgs newArgs)
        {
            BaseDialogList dialog = DataView != null ? DataView.GetComponentInParent<BaseDialogList>() : null;
            if (dialog != null)
            {
                OptionData option = Data as OptionData;
                if (m_ItemText != null)
                    m_ItemText.SetGraphicText(option != null ? option.text : "");
                if (m_ItemIcon != null)
                {
                    m_ItemIcon.SetImageData(option != null ? option.imageData : null);
                    //m_ItemIcon.gameObject.SetActive(m_ItemIcon.GetImageData() != null && m_ItemIcon.GetImageData().ContainsData(true));
                }

                if(m_InteractionCanvasGroup != null)
                {
                    var interactable = option == null || option.interactable;

                    var willChange = m_InteractionCanvasGroup.blocksRaycasts != interactable ||
                                     m_InteractionCanvasGroup.interactable != interactable;
                    if (willChange)
                    {
                        m_InteractionCanvasGroup.blocksRaycasts = interactable;
                        m_InteractionCanvasGroup.interactable = interactable;
                    }
                    else
                    {
                        ApplyInteratableColor();
                    }
                }
            }
        }

        protected virtual void ApplyInteratableColor()
        {
            OptionData option = Data as OptionData;
            if (m_InteractionCanvasGroup != null)
            {
                var interactable = option == null || option.interactable;
                m_InteractionCanvasGroup.alpha = interactable && IsInteractable() ? 1 : 0.5f;
            }
        }

        #endregion
    }
}