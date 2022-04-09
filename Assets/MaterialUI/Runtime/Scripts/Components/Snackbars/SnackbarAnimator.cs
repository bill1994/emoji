﻿// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Snackbar Animator", 100)]
    public class SnackbarAnimator : ToastAnimator
    {
        #region Private Variables

        [SerializeField]
        private MaterialButton m_ActionButton = null;

        private Action m_OnActionButtonClicked;

        #endregion

        #region Public Properties

        public override void Show(Toast toast, RectTransform targetTransform = null, System.Func<Toast, ToastAnimator, bool> onToastComplete = null)
        {
            var snackbar = toast as Snackbar;
            if (snackbar != null)
            {
                m_OnActionButtonClicked = snackbar.onActionButtonClicked;

                if (m_ActionButton != null)
                {
                    m_ActionButton.gameObject.SetActive(!string.IsNullOrEmpty(snackbar.actionName));

                    if (!string.IsNullOrEmpty(snackbar.actionName))
                    {
                        m_ActionButton.textText = snackbar.actionName.ToUpper();
                    }

                    m_ActionButton.onClick.RemoveListener(OnActionButtonClicked);
                    m_ActionButton.onClick.AddListener(OnActionButtonClicked);
                }
            }

            base.Show(toast, targetTransform, onToastComplete);
            //StartCoroutine(Setup());
        }

        #endregion

        #region Helper Functions

        protected void OnActionButtonClicked()
        {
            if (m_OnActionButtonClicked != null)
            {
                m_OnActionButtonClicked();
            }

            m_CurrentPosition = m_RectTransform.position.y;
            m_State = 2;
            m_AnimStartTime = Time.realtimeSinceStartup;
        }

        /*private IEnumerator Setup()
        {
            yield return new WaitForEndOfFrame();

            LayoutElement layoutElement = m_Text.GetComponent<LayoutElement>();
            float buttonWidth = m_ActionButton.GetComponent<MaterialButton>().preferredWidth;
            HorizontalLayoutGroup horizontalLayoutGroup = GetComponent<HorizontalLayoutGroup>();
            float otherWidth = buttonWidth + horizontalLayoutGroup.padding.left + horizontalLayoutGroup.spacing;

            if (Screen.height > Screen.width)
            {
                float height = m_RectTransform.GetProperSize().y;
                GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                m_RectTransform.sizeDelta = new Vector2(Screen.width, height);
                layoutElement.minWidth = Screen.width - otherWidth - 4;
                m_PanelImage.sprite = null;

                m_MaterialMovableFab = FindObjectOfType<MaterialMovableFab>();
                if (m_MaterialMovableFab != null)
                {
                    m_FabRectTransform = m_MaterialMovableFab.GetComponent<RectTransform>();
                    m_FabStartPos = m_FabRectTransform.position.y;
                    m_MoveFab = true;
                }
                else
                {
                    m_FabRectTransform = null;
                    m_MoveFab = false;
                }
            }
            else
            {
                layoutElement.minWidth = 288f - otherWidth;
                layoutElement.preferredWidth = -1f;

                LayoutRebuilder.MarkLayoutForRebuild(m_RectTransform);

                if (m_RectTransform.GetProperSize().x > 568f)
                {
                    layoutElement.preferredWidth = 568f;
                }
            }

            m_OutPos.y = -m_RectTransform.GetProperSize().y * 1.05f;
            m_RectTransform.position = m_OutPos;
            m_CurrentPosition = m_RectTransform.position.y;

            GetComponent<CanvasGroup>().alpha = 1f;
            m_InPos.y = 0f;
            m_OutAlpha = 1f;
        }*/

        #endregion
    }
}