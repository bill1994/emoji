﻿// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Alert", 1)]
    public class DialogAlert : MaterialDialogCompat
    {
        #region Private Variables

        [SerializeField]
        private DialogTitleSection m_TitleSection = new DialogTitleSection();
        [SerializeField]
        private DialogButtonSection m_ButtonSection = new DialogButtonSection();
        [SerializeField]
        private Graphic m_BodyText = null;

        #endregion

        #region Public Properties

        public DialogTitleSection titleSection
        {
            get { return m_TitleSection; }
            set { m_TitleSection = value; }
        }

        public DialogButtonSection buttonSection
        {
            get { return m_ButtonSection; }
            set { m_ButtonSection = value; }
        }

        public Graphic bodyText
        {
            get { return m_BodyText; }
        }

        protected override void ValidateKeyTriggers(MaterialFocusGroup p_materialKeyFocus)
        {
            if (p_materialKeyFocus != null)
            {
                var v_affirmativeTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_affirmativeTrigger.Name = "Return KeyDown";
                v_affirmativeTrigger.Key = KeyCode.Return;
                v_affirmativeTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_affirmativeTrigger.OnCallTrigger, AffirmativeButtonClicked);

                var v_cancelTrigger = new MaterialFocusGroup.KeyTriggerData();
                v_cancelTrigger.Name = "Escape KeyDown";
                v_cancelTrigger.Key = KeyCode.Escape;
                v_cancelTrigger.TriggerType = MaterialFocusGroup.KeyTriggerData.KeyTriggerType.KeyDown;
                MaterialActivity.AddEventListener(v_cancelTrigger.OnCallTrigger, DismissiveButtonClicked);

                p_materialKeyFocus.KeyTriggers = new System.Collections.Generic.List<MaterialFocusGroup.KeyTriggerData> { v_affirmativeTrigger, v_cancelTrigger };
            }
        }

        public void Initialize(string bodyText, Action onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            if(m_TitleSection != null)
                m_TitleSection.SetTitle(titleText, icon);
            if (m_BodyText != null)
                m_BodyText.SetGraphicText(bodyText);

            if (m_ButtonSection != null)
            {
                m_ButtonSection.SetButtons(onAffirmativeButtonClicked, affirmativeButtonText, onDismissiveButtonClicked, dismissiveButtonText);
                m_ButtonSection.SetupButtonLayout(rectTransform);
            }
        }

        public void AffirmativeButtonClicked()
        {
            m_ButtonSection.OnAffirmativeButtonClicked();
            Hide();
        }

        public void DismissiveButtonClicked()
        {
            m_ButtonSection.OnDismissiveButtonClicked();
            Hide();
        }

        #endregion
    }
}