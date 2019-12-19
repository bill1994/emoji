//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Progress", 1)]
    public class DialogProgress : MaterialDialogCompat
    {
        #region Private Variables

        [SerializeField]
        private DialogTitleSection m_TitleSection = new DialogTitleSection();
        [SerializeField]
        private Graphic m_BodyText = null;
        [SerializeField]
        private ProgressIndicator m_LinearIndicator = null;
        [SerializeField]
        private ProgressIndicator m_CircularIndicator = null;

        private ProgressIndicator m_ProgressIndicator;

        #endregion

        #region Public Properties

        public DialogTitleSection titleSection
        {
            get { return m_TitleSection; }
            set { m_TitleSection = value; }
        }

        public Graphic bodyText
        {
            get { return m_BodyText; }
        }

        public ProgressIndicator progressIndicator
        {
            get { return m_ProgressIndicator; }
            set { m_ProgressIndicator = value; }
        }

        #endregion

        #region Helper Functions

        protected override void ValidateKeyTriggers(MaterialFocusGroup p_materialKeyFocus)
        {
        }

        public void Initialize(string bodyText, string titleText, ImageData icon, bool startStationaryAtZero = true)
        {
            if(m_TitleSection != null)
                m_TitleSection.SetTitle(titleText, icon);

            if (m_BodyText != null)
            {
                if (string.IsNullOrEmpty(bodyText))
                {
                    m_BodyText.transform.parent.gameObject.SetActive(false);
                }
                else
                {
                    m_BodyText.SetGraphicText(bodyText);
                }
            }

            if (m_ProgressIndicator == null)
                SetupIndicator(m_LinearIndicator != null);

            if (!startStationaryAtZero)
            {
                m_ProgressIndicator.StartIndeterminate();
            }
            else
            {
                m_ProgressIndicator.SetProgress(0f, false);
            }

            //Initialize();
        }

        public void SetupIndicator(bool isLinear)
        {
            if(m_LinearIndicator != null)
                m_LinearIndicator.gameObject.SetActive(isLinear);
            if(m_CircularIndicator != null)
                m_CircularIndicator.gameObject.SetActive(!isLinear);
            m_ProgressIndicator = isLinear ? m_LinearIndicator : m_CircularIndicator;
            if (m_ProgressIndicator != null && m_ProgressIndicator.transform.parent != null)
            {
                var verticalGroup = m_ProgressIndicator.transform.parent.GetComponent<HorizontalOrVerticalLayoutGroup>();
                if(verticalGroup != null)
                    verticalGroup.childForceExpandWidth = isLinear;
            }
        }

        #endregion
    }
}