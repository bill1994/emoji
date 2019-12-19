using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MaterialUI;

namespace Kyub.UI
{
    public class ScreenViewGenericMenuUI : GenericMenuUI
    {
        protected override void SetupAsPages()
        {
            var v_targetIndex = 2;
            var v_screenView = GetComponent<ScreenView>();
            bool v_pageCreated = false;
            while (_pages.Count != v_targetIndex && m_menuPageTemplate != null)
            {
                if (_pages.Count > v_targetIndex)
                {
                    if (Application.isPlaying)
                        GameObject.Destroy(_pages[_pages.Count - 1]);
                    else
                        GameObject.DestroyImmediate(_pages[_pages.Count - 1]);
                }
                else
                {
                    v_pageCreated = true;
                    var v_instance = CreateTemplateInstance();
                    v_instance.gameObject.SetActive(false);
                    _pages.Add(v_instance);
                }
            }

            var v_screens = new List<MaterialScreen>();
            foreach (var v_page in _pages)
            {
                //v_page.gameObject.SetActive(false);
                v_screens.Add(v_page.GetComponent<MaterialScreen>());
            }

            //Setup Main Page if is was created in this setup
            if (v_pageCreated)
            {
                MainPage.Init(SelectedFolderPath);
                MainPage.gameObject.SetActive(true);
                v_screenView.currentScreenIndex = 0;
            }

            v_screenView.materialScreen = v_screens;
            v_screenView.currentScreenIndex = Mathf.Clamp(v_screenView.currentScreenIndex, 0, v_screens.Count-1);

            var v_currentPage = _pages[v_screenView.currentScreenIndex];

            if (v_currentPage.CurrentFolderPath != SelectedFolderPath)
            {
                var v_moveNext = v_currentPage.CurrentFolderPath.Length <= SelectedFolderPath.Length;
                var v_newScreenIndex = 0;

                if (v_moveNext)
                    v_newScreenIndex = v_screenView.GetNextScreenIndex(true);
                else
                    v_newScreenIndex = v_screenView.GetPreviousScreenIndex(true);

                //Set animation (foward or backward)
                v_screenView.materialScreen[v_newScreenIndex].optionsControlledByScreenView = v_moveNext;
                v_screenView.materialScreen[v_screenView.currentScreenIndex].optionsControlledByScreenView = v_moveNext;

                //Init Pages
                _pages[v_newScreenIndex].Init(SelectedFolderPath);
                v_screenView.Transition(v_newScreenIndex);
            }
        }
    }
}
