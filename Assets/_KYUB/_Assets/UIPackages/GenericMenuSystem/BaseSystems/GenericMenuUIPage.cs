using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MaterialUI;

namespace Kyub.UI
{
    public class GenericMenuUIPage : MonoBehaviour
    {
        #region Private Variables

        [Header("Toolbar UI")]
        [SerializeField]
        GenericMenuUI.Toolbar m_toolbar = new GenericMenuUI.Toolbar();

        GenericMenuUI _parent = null;

        string m_currentFolderPath = "";

        #endregion

        #region Public Properties

        ScrollDataView _scrollDataView = null;
        public ScrollDataView ScrollDataView
        {
            get
            {
                if (_scrollDataView == null)
                    _scrollDataView = GetComponentInChildren<ScrollDataView>();
                return _scrollDataView;
            }
        }

        public GenericMenuUI Parent
        {
            get
            {
                if (_parent == null)
                    _parent = GetComponentInParent<GenericMenuUI>();
                return _parent;
            }
        }

        public string CurrentFolderPath
        {
            get
            {
                return m_currentFolderPath;
            }
        }

        #endregion

        #region Helper Functions

        public void Init(string p_currentFolderPath)
        {
            if (p_currentFolderPath == null)
                p_currentFolderPath = "";
            //Pick path of this hierarchy
            m_currentFolderPath = p_currentFolderPath;

            //Get childrens in this path
            var v_folderElement = Parent.RootData.GetFolderAtPath(m_currentFolderPath);
            var v_childrens = v_folderElement != null ? v_folderElement.GetChildren() : (string.IsNullOrEmpty(p_currentFolderPath) ? Parent.RootData.GetRootElements() : new GenericMenuElementData[0]);
            
            //Apply data immediate
            ScrollDataView.Setup(v_childrens);
            ScrollDataView.ScrollLayoutGroup.TryRecalculateLayout();

            SetupToolbar();
        }

        protected virtual void SetupToolbar()
        {
            m_toolbar.SetupToolbar(CurrentFolderPath, MoveSelectedFolderToParent, MoveSelectedFolderToRoot);
        }

        public void MoveSelectedFolderToParent()
        {
            var v_splittedPath = CurrentFolderPath.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            var v_current = "";

            for (int i = 0; i < v_splittedPath.Length - 1; i++)
            {
                v_current += (string.IsNullOrEmpty(v_current) ? "" : "/") + v_splittedPath[i];
            }
            Parent.SelectedFolderPath = v_current;
        }

        public void MoveSelectedFolderToRoot()
        {
            Parent.MoveSelectedFolderToRoot();
        }

        #endregion
    }
}
