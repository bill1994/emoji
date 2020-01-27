//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MaterialUI
{
    [Serializable]
    [AddComponentMenu("MaterialUI/Dialogs/Title Section", 100)]
    public class DialogTitleSection
    {
        #region Private Variables

        [SerializeField]
        private Graphic m_Text = null;
		[SerializeField]
		private Graphic m_Sprite = null;
		[SerializeField]
		private Graphic m_VectorImageData = null;

        #endregion

        #region Public Propertoes

        public Graphic text
        {
            get { return m_Text; }
        }

        public Graphic sprite
        {
            get { return m_Sprite; }
        }

        public Graphic vectorImageData
        {
            get { return m_VectorImageData; }
        }

        #endregion

        #region Helper Functions

        public void SetTitle(string titleText, ImageData icon)
        {
            if (m_Text == null) return;

            if (!string.IsNullOrEmpty(titleText) || (icon != null && icon.ContainsData(true)))
            {
                if (!string.IsNullOrEmpty(titleText))
                {
                    m_Text.SetGraphicText(titleText);
                }
                else
                {
                    m_Text.gameObject.SetActive(false);
                }

				if (icon == null || !icon.ContainsData(true))
				{
                    if (m_Sprite != null && m_VectorImageData != null)
                    {
                        m_Sprite.gameObject.SetActive(false);
                        m_VectorImageData.gameObject.SetActive(false);
                    }
				}
				else
				{
					if (icon.imageDataType == ImageDataType.VectorImage)
					{
                        if(m_VectorImageData != null)
						    m_VectorImageData.SetImageData(icon.vectorImageData);
                        if(m_Sprite != null)
						    m_Sprite.gameObject.SetActive(false);
					}
					else
					{
                        if (m_Sprite != null)
                            m_Sprite.SetImageData(icon);
                        if (m_VectorImageData != null)
                            m_VectorImageData.gameObject.SetActive(false);
					}
				}
            }
            else
            {
                if(m_Text != null && m_Text.transform.parent != null)
                    m_Text.transform.parent.gameObject.SetActive(false);
            }
        }

        #endregion
    }

    [Serializable]
    [AddComponentMenu("MaterialUI/Dialogs/Button Section", 100)]
    public class DialogButtonSection
    {
        #region Private Variables

        [SerializeField]
        private MaterialButton m_AffirmativeButton = null;
        [SerializeField]
        private MaterialButton m_DismissiveButton = null;

        private bool m_ShowDismissiveButton;

        #endregion

        #region Public Properties

        public MaterialButton affirmativeButton
        {
            get { return m_AffirmativeButton; }
        }

        public MaterialButton dismissiveButton
        {
            get { return m_DismissiveButton; }
        }

        #endregion

        #region Callbacks

        private Action m_OnAffirmativeButtonClicked;
        public Action onAffirmativeButtonClicked
        {
            get { return m_OnAffirmativeButtonClicked; }
        }

        private Action m_OnDismissiveButtonClicked;
        public Action onDismissiveButtonClicked
        {
            get { return m_OnDismissiveButtonClicked; }
        }

        #endregion

        #region Helper Functions

        public void SetButtons(Action onAffirmativeButtonClick, string affirmativeButtonText, Action onDismissiveButtonClick, string dismissiveButtonText)
        {
			SetAffirmativeButton(onAffirmativeButtonClick, affirmativeButtonText);
			SetDismissiveButton(onDismissiveButtonClick, dismissiveButtonText);
        }

		public void SetAffirmativeButton(Action onButtonClick, string text)
        {
            if (m_AffirmativeButton != null)
            {
                m_OnAffirmativeButtonClicked = onButtonClick;

                if(m_AffirmativeButton.text != null)
                {
                    m_AffirmativeButton.textText = text;
                }
            }
        }

        public void SetDismissiveButton(Action onButtonClick, string text)
        {
            if (string.IsNullOrEmpty(text) && onButtonClick == null) return;

            if (m_DismissiveButton != null)
            {
                m_ShowDismissiveButton = true;
                m_OnDismissiveButtonClicked = onButtonClick;

                if(m_DismissiveButton.text != null)
                {
                    m_DismissiveButton.textText = text;
                }
            }
        }

        public void SetupButtonLayout(RectTransform dialogRectTransform)
        {
            if(m_DismissiveButton != null)
            {
                m_DismissiveButton.gameObject.SetActive(m_ShowDismissiveButton);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(dialogRectTransform);

            /*if (m_AffirmativeButton != null && m_DismissiveButton != null && m_AffirmativeButton.minWidth + m_DismissiveButton.minWidth + 24f > DialogManager.rectTransform.rect.width - 48f)
            {
                Object.DestroyImmediate(m_AffirmativeButton.rectTransform.parent.GetComponent<HorizontalLayoutGroup>());

                VerticalLayoutGroup verticalLayoutGroup = m_AffirmativeButton.rectTransform.parent.gameObject.GetComponent<VerticalLayoutGroup>();
                if(verticalLayoutGroup == null)
                    verticalLayoutGroup = m_AffirmativeButton.rectTransform.parent.gameObject.AddComponent<VerticalLayoutGroup>();
                if (verticalLayoutGroup != null)
                {
                    verticalLayoutGroup.padding = new RectOffset(8, 8, 8, 8);
                    verticalLayoutGroup.childAlignment = TextAnchor.UpperRight;
                    verticalLayoutGroup.childForceExpandHeight = false;
                    verticalLayoutGroup.childForceExpandWidth = false;
                }
            }*/
        }

        public void OnAffirmativeButtonClicked()
        {
            if(m_AffirmativeButton != null)
            {
                m_OnAffirmativeButtonClicked.InvokeIfNotNull();
            }
        }

        public void OnDismissiveButtonClicked()
        {
            if(m_OnDismissiveButtonClicked != null)
            {
                m_OnDismissiveButtonClicked.InvokeIfNotNull();
            }
        }

        #endregion
    }
}