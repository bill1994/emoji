// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using Kyub.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Checkbox Option", 100)]
    public class DialogCheckboxOption : DialogClickableOption
    {
        #region Private Variables

        [SerializeField]
		private Graphic m_ItemText = null;
        [SerializeField]
        private Graphic m_ItemIcon = null;
        [SerializeField]
		private ToggleBase m_ItemCheckbox = null;
        [SerializeField]
        protected CanvasGroup m_InteractionCanvasGroup = null;
        [SerializeField]
        protected float m_CanvasDisableAlphaValue = 1;

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
        }

        public Graphic itemIcon
        {
            get { return m_ItemIcon; }
        }

        public ToggleBase itemCheckbox
        {
            get { return m_ItemCheckbox; }
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
            if (!_isChangingCanvasGroup)
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
            BaseDialogList dialog = DataView != null? DataView.GetComponentInParent<BaseDialogList>() : null;
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
                if (m_ItemCheckbox != null)
                    m_ItemCheckbox.isOn = dialog.IsUnsafeDataIndexSelected(DataIndex);

                if (m_InteractionCanvasGroup != null)
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