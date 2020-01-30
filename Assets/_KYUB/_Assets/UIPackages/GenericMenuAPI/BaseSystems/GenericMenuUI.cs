using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

namespace Kyub.UI
{
    public class GenericMenuUI : MonoBehaviour
    {
        [System.Serializable]
        public struct Toolbar
        {
            public Text FolderPathTextField;
            public Button BackButton;
            public Button HomeButton;

            public void SetupToolbar(string p_path, UnityAction p_MoveToFolderParentAction, UnityAction p_MoveToFolderRootAction)
            {
                //Set Current Path
                if (FolderPathTextField != null)
                    FolderPathTextField.text = p_path;

                //Setup Buttons
                var v_toolbarButtonsActive = !string.IsNullOrEmpty(p_path);
                if (BackButton != null)
                {
                    if (p_MoveToFolderParentAction != null)
                    {
                        BackButton.onClick.RemoveListener(p_MoveToFolderParentAction);
                        BackButton.onClick.AddListener(p_MoveToFolderParentAction);
                    }

                    var v_buttonCanvas = BackButton.GetComponent<CanvasGroup>();
                    if (v_buttonCanvas != null)
                        v_buttonCanvas.interactable = v_toolbarButtonsActive;
                    BackButton.interactable = v_toolbarButtonsActive;
                }
                if (HomeButton != null)
                {
                    if (p_MoveToFolderRootAction != null)
                    {
                        HomeButton.onClick.RemoveListener(p_MoveToFolderRootAction);
                        HomeButton.onClick.AddListener(p_MoveToFolderRootAction);
                    }

                    var v_buttonCanvas = HomeButton.GetComponent<CanvasGroup>();
                    if (v_buttonCanvas != null)
                        v_buttonCanvas.interactable = v_toolbarButtonsActive;
                    HomeButton.interactable = v_toolbarButtonsActive;
                }
            }
        }

        [System.Serializable]
        public class IntUnityEvent : UnityEvent<int> { }
        [System.Serializable]
        public class StringUnityEvent : UnityEvent<string> { }

        #region Private Variables

        [Header("General Fields")]
        [SerializeField]
        protected GenericMenuUIPage m_menuPageTemplate = null;
        [SerializeField]
        protected int m_selectedIndex = -1;
        [SerializeField]
        protected string m_selectedFolderPath = null;
        [SerializeField]
        protected List<Dropdown.OptionData> m_items = new List<Dropdown.OptionData>();

        [Header("Toolbar UI")]
        [SerializeField]
        Toolbar m_mainToolbar = new Toolbar();

        protected List<GenericMenuUIPage> _pages = new List<GenericMenuUIPage>();

        protected GenericMenuRootData _rootData = null;
        [System.NonSerialized]
        protected IList<GenericMenuElementData> _cachedItems = null;

        #endregion

        #region Callbacks

        [Header("Callbacks")]
        public IntUnityEvent OnSelectedIndexChangedCallback = new IntUnityEvent();
        public StringUnityEvent OnSelectedFolderChangedCallback = new StringUnityEvent();

        #endregion

        #region Properties

        public GenericMenuUIPage MainPage
        {
            get
            {
                if (_pages.Count == 0 || _pages[0] == null)
                    SetupMainPage();
                return _pages[0];
            }
        }

        public ScrollDataView ScrollDataView
        {
            get
            {
                return MainPage.ScrollDataView;
            }
        }

        public GenericMenuRootData RootData
        {
            get
            {
                if (_rootData == null)
                    _rootData = ScriptableObject.CreateInstance<GenericMenuRootData>();
                return _rootData;
            }
        }

        public List<Dropdown.OptionData> Items
        {
            get
            {
                return m_items;
            }

            set
            {
                m_items = value;
            }
        }

        public string SelectedFolderPath
        {
            get
            {
                //Update Selected Path
                if (m_selectedFolderPath == null)
                {
                    var v_selectedElement = GetCurrentSelectedItem();
                    m_selectedFolderPath = v_selectedElement != null && v_selectedElement.Parent != null ? v_selectedElement.Parent.GetPath() : "";
                    if (m_selectedFolderPath == null)
                        m_selectedFolderPath = "";
                }
                return m_selectedFolderPath;
            }

            set
            {
                if (m_selectedFolderPath == value || (!RootData.FolderExists(value) && !string.IsNullOrEmpty(value)))
                    return;
                m_selectedFolderPath = value;
                MarkToSetup();
                if (OnSelectedFolderChangedCallback != null)
                    OnSelectedFolderChangedCallback.Invoke(m_selectedFolderPath);
            }
        }

        public int SelectedIndex
        {
            get
            {
                return m_selectedIndex;
            }

            set
            {
                var v_value = Mathf.Clamp(value, -1, m_items.Count - 1);
                if (m_selectedIndex == v_value)
                    return;
                m_selectedIndex = v_value;

                //Set Layout Dirty
                SetLayoutDirty();
                if (OnSelectedIndexChangedCallback != null)
                    OnSelectedIndexChangedCallback.Invoke(m_selectedIndex);

                //Update Selected Path
                var v_selectedElement = GetCurrentSelectedItem();
                SelectedFolderPath = v_selectedElement != null && v_selectedElement.Parent != null ? v_selectedElement.Parent.GetPath() : "";
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            m_selectedFolderPath = null;
            if (_started)
            {
                TryBuildDataAndSetup(true);
            }
        }

        bool _started = false;
        protected virtual void Start()
        {
            _started = true;
            SetupMainPage();
            TryBuildDataAndSetup(true);
        }

        protected virtual void OnDisable()
        {
            ClearPages();
        }

        protected virtual void LateUpdate()
        {
            TryBuildDataAndSetup();
        }

        #endregion

        #region Public Functions

        public virtual void ClearOptions()
        {
            m_items.Clear();
            MarkToBuildRootData();
            MarkToSetup();
        }

        public virtual void AddOption(string p_optionStr)
        {
            AddOptions(new List<string>() { p_optionStr });
        }

        public virtual void AddOption(Dropdown.OptionData p_option)
        {
            AddOptions(new List<Dropdown.OptionData>() { p_option });
        }

        public virtual void AddOptions(IList<string> p_optionsStr)
        {
            var v_listOptions = new List<Dropdown.OptionData>();
            if (p_optionsStr != null)
            {
                foreach (var v_optionStr in p_optionsStr)
                {
                    v_listOptions.Add(new Dropdown.OptionData(v_optionStr));
                }
            }
            AddOptions(v_listOptions);
        }

        public virtual void AddOptions(IList<Dropdown.OptionData> p_options)
        {
            if (p_options != null)
            {
                foreach (var v_option in p_options)
                {
                    if(v_option != null)
                        m_items.Add(v_option);
                }
            }
            MarkToBuildRootData();
            MarkToSetup();
        }


        #endregion

        #region Helper Functions

        public void TryBuildDataAndSetup(bool p_force = false)
        {
            if (_markedBuildRootData || p_force)
            {
                _markedBuildRootData = false;
                BuildToRootData();
            }
            if (_markedToSetup || p_force)
            {
                _markedToSetup = false;
                Setup();
            }
        }

        protected bool _markedToSetup = false;
        public virtual void MarkToSetup()
        {
            _markedToSetup = true;
        }

        protected bool _markedBuildRootData = false;
        public virtual void MarkToBuildRootData()
        {
            _markedBuildRootData = true;
        }

        protected virtual void SetLayoutDirty()
        {
            foreach (var v_subFolder in _pages)
            {
                if (v_subFolder != null)
                    v_subFolder.ScrollDataView.SetLayoutDirty();
            }
        }

        protected virtual void BuildToRootData()
        {
            RootData.Clear();
            foreach (var v_item in m_items)
            {
                RootData.AddItem(v_item.text);
            }
            _cachedItems = null;
        }

        public int IndexOf(GenericMenuElementData p_element)
        {
            if (_cachedItems == null)
                _cachedItems = RootData.GetNonFolderElements();

            return _cachedItems.IndexOf(p_element);
        }

        public GenericMenuElementData GetCurrentSelectedItem()
        {
            if (_cachedItems == null)
                _cachedItems = RootData.GetNonFolderElements();

            //Clamp Values
            m_selectedIndex = Mathf.Clamp(m_selectedIndex, -1, m_items.Count - 1);
            if (m_selectedIndex >= 0 && m_selectedIndex < _cachedItems.Count)
            {
                var v_selectedElement = _cachedItems[m_selectedIndex];
                return v_selectedElement;
            }
            return null;
        }

        public GenericMenuElementData GetCurrentSelectedFolder()
        {
            if (_cachedItems == null)
                _cachedItems = RootData.GetNonFolderElements();

            var v_folderElement = RootData.GetFolderAtPath(SelectedFolderPath);
            //Invalid Path
            if (!string.IsNullOrEmpty(SelectedFolderPath) && v_folderElement == null)
                m_selectedFolderPath = "";

            return v_folderElement;
        }

        protected GenericMenuElementData[] GetCurrentExpandedList()
        {
            if (_cachedItems == null)
                _cachedItems = RootData.GetNonFolderElements();

            GenericMenuElementData[] v_list = null;
            GenericMenuElementData v_parent = GetCurrentSelectedFolder();

            if (v_parent != null)
                v_list = v_parent.GetChildren();
            else
                v_list = RootData.GetRootElements();

            return v_list;
        }

        #endregion

        #region Public Helper Navigation Functions

        public void MoveSelectedFolderToParent()
        {
            var v_splittedPath = SelectedFolderPath.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            var v_current = "";

            for (int i = 0; i < v_splittedPath.Length - 1; i++)
            {
                v_current += (string.IsNullOrEmpty(v_current) ? "" : "/") + v_splittedPath[i];
            }
            SelectedFolderPath = v_current;
        }

        public void MoveSelectedFolderToRoot()
        {
            SelectedFolderPath = "";
        }

        #endregion

        #region Setup Functions

        public virtual void Setup()
        {
            SetupMainToolbar();
            SetupAsPages();
        }

        protected virtual void SetupMainToolbar()
        {
            m_mainToolbar.SetupToolbar(SelectedFolderPath, MoveSelectedFolderToParent, MoveSelectedFolderToRoot);
        }

        public void SetupMainPage()
        {
            m_menuPageTemplate.gameObject.SetActive(false);
            FixEmptyPages();
            if (_pages.Count == 0)
            {
                var v_instance = CreateTemplateInstance();
                v_instance.gameObject.SetActive(false);
                _pages.Add(v_instance);
            }
        }

        protected GenericMenuUIPage CreateTemplateInstance()
        {
            //Instantiate based in parent
            var v_instance = GameObject.Instantiate(m_menuPageTemplate);

            CloneTransform(v_instance.transform as RectTransform, m_menuPageTemplate.transform as RectTransform);
            v_instance.transform.localScale = m_menuPageTemplate.transform.localScale;
            v_instance.transform.rotation = m_menuPageTemplate.transform.rotation;
            v_instance.transform.localPosition = m_menuPageTemplate.transform.localPosition;
            //v_subFolder.transform.SetParent(v_parentSubfolder.transform);
            //Add in GameObject Tree
            v_instance.transform.SetAsLastSibling();

            return v_instance;
        }

        public static void CloneTransform(RectTransform p_target, RectTransform p_transformToClone)
        {
            p_target.transform.SetParent(p_transformToClone.parent);
            p_target.anchoredPosition = p_transformToClone.anchoredPosition;
            p_target.anchorMin = p_transformToClone.anchorMin;
            p_target.anchorMax = p_transformToClone.anchorMax;
            p_target.pivot = p_transformToClone.pivot;
            p_target.sizeDelta = p_transformToClone.sizeDelta;
        }


        protected virtual void FixEmptyPages()
        {
            for (int i = 0; i < _pages.Count; i++)
            {
                if (_pages[i] == null)
                {
                    _pages.RemoveAt(i);
                    i--;
                }
            }
        }

        protected virtual void SetupAsPages()
        {
            MainPage.Init(SelectedFolderPath);
            MainPage.gameObject.SetActive(true);

            /*var v_splittedPath = new List<string>(SelectedFolderPath.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries));
            v_splittedPath.Insert(0, ""); //RootPath
            //Remove nulls before try pre-process
            FixEmptyPages();

            var v_parentSubfolder = ScrollDataView; //_subFolderHierarchy.Count > 0 ? _subFolderHierarchy[_subFolderHierarchy.Count - 1] : ScrollDataView;
            //Pre-process all subfolders hierarchy
            var v_max = Math.Max(v_splittedPath.Count, _pages.Count);
            for (int i = 0; i< v_splittedPath.Count; i++)
            {
                GenericMenuUIPage v_subFolder = i < _pages.Count? _pages[i] : null;

                if (v_subFolder == null)
                {
                    v_subFolder = CreateTemplateInstance();
                    _pages.Add(v_subFolder);
                }
            }
            //Deactivate Extra Screens
            for (int i = v_splittedPath.Count -1; i < _pages.Count; i++)
            {
                GenericMenuUIPage v_subFolder = i < _pages.Count ? _pages[i] : null;
                if (v_subFolder != null)
                {
                    v_subFolder.Hide(this);
                }
            }

            //Show
            var v_currentPath = "";
            for (int i = 0; i < v_splittedPath.Count; i++)
            {
                var v_subFolder = _pages[i];
                if (v_subFolder != null)
                {
                    //Pick path of this hierarchy
                    v_currentPath += (string.IsNullOrEmpty(v_currentPath) ? "" : "/") + v_splittedPath[i];
                    //Get childrens in this path
                    var v_folderElement = RootData.GetFolderAtPath(v_currentPath);
                    var v_childrens = v_folderElement != null ? v_folderElement.GetChildren() : RootData.GetRootElements();

                    v_subFolder.Show(this, v_currentPath);
                    v_subFolder.ScrollDataView.Setup(v_childrens);
                    v_subFolder.ScrollDataView.ScrollLayoutGroup.TryRecalculateLayout();
                }
            }*/
        }

        protected virtual void ClearPages()
        {
            while(_pages.Count > 1)
            {
                var v_subfolder = _pages[_pages.Count -1];
                if (v_subfolder != null)
                {
                    v_subfolder.gameObject.SetActive(false);
                    if (Application.isPlaying)
                        GameObject.Destroy(v_subfolder.gameObject);
                    else
                        GameObject.DestroyImmediate(v_subfolder.gameObject);
                }
                _pages.RemoveAt(_pages.Count - 1);
            }
        }

        #endregion
    }
}
