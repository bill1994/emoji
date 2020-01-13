using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Kyub.UI
{
    public interface IReloadableDataViewElement
    {
        void Reload(ScrollDataView.ReloadEventArgs p_args);

        bool enabled
        {
            get;
            set;
        }

        GameObject gameObject
        {
            get;
        }

        bool IsDestroyed();
    }

    public class ScrollDataView : MonoBehaviour
    {
        #region Helper Classes

        [System.Serializable]
        public struct ReloadEventArgs
        {
            public ScrollDataView Sender { get; set; }
            public object Data { get; set; }
            public int DataIndex { get; set; }
            public GameObject LayoutElement { get; set; }
            public int LayoutElementIndex { get; set; }
        }

        [System.Serializable]
        public class ReloadUnityEvent : UnityEvent<ReloadEventArgs> { }

        #endregion

        #region Private Variables

        [SerializeField]
        bool m_disableElementsInPool = true;
        [SerializeField]
        protected int m_defaultSetupInitialIndex = 0;
        [SerializeField]
        protected GameObject m_defaultTemplate = null;
        [SerializeField]
        protected GameObject m_emptyDataObject= null;
        [SerializeField]
        protected ScrollLayoutGroup m_scrollRectLayout = null;

        protected IList m_data = null;

        #endregion

        #region Callbacks

        public UnityEvent OnSetup = new UnityEvent();
        public ReloadUnityEvent OnReloadElement = new ReloadUnityEvent();

        #endregion

        #region Public Properties

        public bool DisableElementsInPool
        {
            get
            {
                return m_disableElementsInPool;
            }
            set
            {
                if (m_disableElementsInPool == value)
                    return;
                m_disableElementsInPool = value;
                TrySetupPoolContent();
            }
        }

        public GameObject DefaultTemplate
        {
            get
            {
                return m_defaultTemplate;
            }
            set
            {
                if (m_defaultTemplate == value)
                    return;
                m_defaultTemplate = value;
                MarkToRemapIndexes();
            }
        }

        public int DefaultSetupInitialIndex
        {
            get
            {
                return m_defaultSetupInitialIndex;
            }
            set
            {
                if (m_defaultSetupInitialIndex == value)
                    return;
                m_defaultSetupInitialIndex = value;
                MarkToRemapIndexes();
            }
        }

        public ReadOnlyCollection<object> Data
        {
            get
            {
                var v_list = new List<object>();
                if (m_data != null)
                {
                    foreach (var v_data in m_data)
                    {
                        v_list.Add(v_data);
                    }
                }
                return v_list.AsReadOnly();
            }
        }

        public ScrollLayoutGroup ScrollLayoutGroup
        {
            get
            {
                if (m_scrollRectLayout == null)
                {
                    m_scrollRectLayout = GetComponentInChildren<ScrollLayoutGroup>(true);
                    if(m_scrollRectLayout != null && enabled && gameObject.activeInHierarchy)
                        RegisterScrollLayoutEvents();
                }
                return m_scrollRectLayout;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            RegisterScrollLayoutEvents();
            TrySetupOnEnable();
        }

        protected virtual void Start()
        {
            InitTemplates();
        }

        protected virtual void OnDisable()
        {
            UnregisterScrollLayoutEvents();
        }

        protected virtual void OnDestroy()
        {
            TryDestroyPoolContent();
        }

        protected virtual void Update()
        {
            TryRemapIndexes();
        }

        protected virtual void OnRectTransformDimensionsChange()
        {
            SetLayoutDirty();
        }

        #endregion

        #region Main Functions

        public void ClearSetup()
        {
            Setup(null);
            ScrollLayoutGroup.TryRecalculateLayout();
        }

        public void Setup(IList p_data, IList<GameObject> p_customTemplatePerIndexList)
        {
            Setup(p_data, null, p_customTemplatePerIndexList);
        }

        public void Setup(IList p_data, Dictionary<int, GameObject> p_customTemplatePerIndex)
        {
            Setup(p_data, null, p_customTemplatePerIndex);
        }

        public void Setup(IList p_data, Dictionary<System.Type, GameObject> p_customTemplatePerDataType, IList<GameObject> p_customTemplatePerIndexList)
        {
            Dictionary<int, GameObject> v_customTemplateDict = new Dictionary<int, GameObject>();
            if (p_customTemplatePerIndexList != null)
            {
                for (int i = 0; i < p_customTemplatePerIndexList.Count; i++)
                {
                    var v_template = p_customTemplatePerIndexList[i];
                    if (v_template != null && v_template != m_defaultTemplate)
                        v_customTemplateDict[i] = v_template;
                }
            }
            Setup(p_data, p_customTemplatePerDataType, v_customTemplateDict);
        }

        bool _setupOnEnable = false;
        protected Dictionary<System.Type, GameObject> _typePerPrefabTemplate = new Dictionary<System.Type, GameObject>();
        protected Dictionary<int, GameObject> _indexPerPrefabTemplate= new Dictionary<int, GameObject>();
        public virtual void Setup(IList p_data, Dictionary<System.Type, GameObject> p_customTemplatePerDataType = null, Dictionary<int, GameObject> p_customTemplatePerIndex = null)
        {

            //Set new Prefab per Type
            _typePerPrefabTemplate = p_customTemplatePerDataType;
            if (_typePerPrefabTemplate == null)
                _typePerPrefabTemplate = new Dictionary<System.Type, GameObject>();

            //Set new Prefab per Index
            _indexPerPrefabTemplate = p_customTemplatePerIndex;
            if (_indexPerPrefabTemplate == null)
                _indexPerPrefabTemplate = new Dictionary<int, GameObject>();

            m_data = p_data;
            if (m_data == null)
                m_data = new object[0];

            if (gameObject.activeInHierarchy && enabled)
            {
                _setupOnEnable = false;
                //Prevent erros with "Auto Pick Elements" forcing ScrollLayout to Initialize
                if (ScrollLayoutGroup != null)
                    ScrollLayoutGroup.ForceInitialize();
                SetupPool();
                RemapIndexes();
                if (ScrollLayoutGroup != null)
                    ScrollLayoutGroup.SetCachedElementsLayoutDirty(true);
                InitTemplates();
                ScrollLayoutGroup.TryRecalculateLayout();

                if (m_emptyDataObject != null)
                    m_emptyDataObject.SetActive(m_data.Count == 0);

                if (OnSetup != null)
                    OnSetup.Invoke();
            }
            else
            {
                _setupOnEnable = true;
            }
        }

        protected void TrySetupOnEnable()
        {
            if(_setupOnEnable)
                Setup(m_data, _typePerPrefabTemplate, _indexPerPrefabTemplate);
        }

        #endregion

        #region Other Public Functions

        public GameObject GetTemplateFromDataType(System.Type p_dataType)
        {
            GameObject v_template = m_defaultTemplate;
            //Try find for each type and subclass type
            while (p_dataType != null)
            {
                if (_typePerPrefabTemplate.TryGetValue(p_dataType, out v_template))
                    break;
                p_dataType = p_dataType.BaseType;
            }
            if (v_template == null)
                v_template = m_defaultTemplate;
            return v_template;
        }

        public void SetTemplateFromDataIndex(System.Type p_dataType, GameObject p_template)
        {
            if (p_dataType != null)
            {
                GameObject v_oldTemplate = null;
                var v_sucess = _typePerPrefabTemplate.TryGetValue(p_dataType, out v_oldTemplate);

                if (v_sucess && (p_template == null || p_template == m_defaultTemplate))
                    _typePerPrefabTemplate.Remove(p_dataType);
                else if (v_oldTemplate != p_template && p_template != null)
                    _typePerPrefabTemplate[p_dataType] = p_template;

                if (v_oldTemplate != p_template)
                    MarkToRemapIndexes();
            }
        }

        public GameObject GetTemplateFromDataIndex(int p_dataIndex)
        {
            GameObject v_template = null;
            if (!_indexPerPrefabTemplate.TryGetValue(p_dataIndex, out v_template))
            {
                var v_data = m_data != null && m_data.Count > p_dataIndex && p_dataIndex >= 0 ? m_data[p_dataIndex] : null;
                if (v_data != null)
                    v_template = GetTemplateFromDataType(v_data.GetType());
            }
            //Prevent return default template
            if (v_template == null)
                v_template = m_defaultTemplate;
            return v_template;
        }

        public void SetTemplateFromDataIndex(int p_dataIndex, GameObject p_template)
        {
            GameObject v_oldTemplate = null;
            var v_sucess = _indexPerPrefabTemplate.TryGetValue(p_dataIndex, out v_oldTemplate);

            if (v_sucess && (p_template == null || p_template == m_defaultTemplate))
                _indexPerPrefabTemplate.Remove(p_dataIndex);
            else if(v_oldTemplate != p_template && p_template != null)
                _indexPerPrefabTemplate[p_dataIndex] = p_template;

            if (v_oldTemplate != p_template)
                ScrollLayoutGroup.SetCachedElementsLayoutDirty();
        }

        public GameObject GetElementAtDataIndex(int p_dataIndex)
        {
            int v_layoutIndex = ConvertDataIndexToLayoutIndex(p_dataIndex);
            if(ScrollLayoutGroup != null && v_layoutIndex >= 0 && v_layoutIndex < m_scrollRectLayout.ElementsCount)
            {
                return m_scrollRectLayout[v_layoutIndex];
            }
            return null;
        }

        public int ConvertDataIndexToLayoutIndex(int p_dataIndex)
        {
            var v_layoutIndex = -1;
            if (!_dataIndexToLayoutIndex.TryGetValue(p_dataIndex, out v_layoutIndex))
                v_layoutIndex = -1;
            return v_layoutIndex;
        }


        public int ConvertLayoutIndexToDataIndex(int p_layoutIndex)
        {
            var v_dataIndex = -1;
            if (!_layoutIndexToDataIndex.TryGetValue(p_layoutIndex, out v_dataIndex))
                v_dataIndex = -1;
            return v_dataIndex;
        }

        protected int _initialIndex = 0;
        protected Dictionary<int, int> _dataIndexToLayoutIndex = new Dictionary<int, int>();
        protected Dictionary<int, int> _layoutIndexToDataIndex = new Dictionary<int, int>();
        public virtual void TryRemapIndexes(bool p_force = false)
        {
            if (_indexesDirty || p_force)
            {
                _indexesDirty = false;
                RemapIndexes();
            }
        }

        bool _indexesDirty = false;
        public virtual void MarkToRemapIndexes()
        {
            _indexesDirty = true;
        }

        public virtual void SetLayoutDirty()
        {
            if (ScrollLayoutGroup != null)
            {
                ScrollLayoutGroup.SetCachedElementsLayoutDirty(true);
            }
        }

        public virtual void FastReloadAll()
        {
            if (ScrollLayoutGroup != null)
            {
                ScrollLayoutGroup.FastReloadAll();
            }
        }

        public virtual void FullReloadAll()
        {
            if (ScrollLayoutGroup != null)
            {
                ScrollLayoutGroup.FullReloadAll();
            }
        }

        #endregion

        #region Helper Functions

        protected virtual void InitTemplates()
        {
            /*if (_indexPerPrefabTemplate != null)
            {
                foreach (var v_pair in _indexPerPrefabTemplate)
                {
                    var template = v_pair.Value;
                    if (template != null && template.scene.IsValid())
                    {
                        template.name = "IndexTemplate";
                        //if (template.activeSelf)
                        //    template.SetActive(false);
                    }
                }
            }*/
            if (_typePerPrefabTemplate != null)
            {
                foreach (var v_pair in _typePerPrefabTemplate)
                {
                    var template = v_pair.Value;
                    if (template != null && template.scene.IsValid())
                    {
                        template.name = "Template (Type: " + v_pair.Key.ToString() + ")";
                        //if (template.activeSelf)
                        //    template.SetActive(false);
                    }
                }
            }
            if (m_defaultTemplate != null && m_defaultTemplate.scene.IsValid())
            {
                m_defaultTemplate.gameObject.name = "DefaultTemplate";
                //if (m_defaultTemplate.activeSelf)
                //    m_defaultTemplate.SetActive(false);
            }
        }

        protected virtual void RegisterScrollLayoutEvents()
        {
            UnregisterScrollLayoutEvents();
            if (ScrollLayoutGroup != null)
            {
                m_scrollRectLayout.OnBeforeChangeVisibleElements.AddListener(HandleOnBeforeChangeVisibleElements);
                m_scrollRectLayout.OnElementsAdded.AddListener(HandleOnAddElements);
                m_scrollRectLayout.OnElementsRemoved.AddListener(HandleOnRemoveElements);
                m_scrollRectLayout.OnElementBecameVisible.AddListener(HandleOnElementBecameVisible);
            }
        }

        protected virtual void UnregisterScrollLayoutEvents()
        {
            if (ScrollLayoutGroup != null)
            {
                m_scrollRectLayout.OnBeforeChangeVisibleElements.RemoveListener(HandleOnBeforeChangeVisibleElements);
                m_scrollRectLayout.OnElementsAdded.RemoveListener(HandleOnAddElements);
                m_scrollRectLayout.OnElementsRemoved.RemoveListener(HandleOnRemoveElements);
                m_scrollRectLayout.OnElementBecameVisible.RemoveListener(HandleOnElementBecameVisible);
            }
        }

        protected virtual void RemapIndexes(bool p_resetAllObjects = false)
        {
            Dictionary<int, float> v_replacementDataLayoutSize = new Dictionary<int, float>();

            _layoutIndexToDataIndex.Clear();
            _dataIndexToLayoutIndex.Clear();
            if (m_data != null && ScrollLayoutGroup != null)
            {
                bool v_needReapplyElements = false;
                var v_elements = new List<GameObject>();
                _initialIndex =  Mathf.Clamp((m_defaultSetupInitialIndex < 0 ? ScrollLayoutGroup.ElementsCount - 1 : m_defaultSetupInitialIndex), 0, ScrollLayoutGroup.ElementsCount);
                var v_lastIndexMember = -1;
                var v_currentDataIndex = 0;
                for (int i = 0; i < ScrollLayoutGroup.ElementsCount; i++)
                {
                    var v_object = ScrollLayoutGroup[i];
                    var v_isTemplate = IsDataViewTemplate(v_object);
                    if (i >= _initialIndex && !v_isTemplate && (v_object == null || IsDataViewObject(v_object)))
                    {
                        //We dont want extra elements related to data in ScrollLayoutGroup
                        if (v_currentDataIndex >= m_data.Count)
                        {
                            v_needReapplyElements = true;
                            if (v_object != null)
                            {
                                ReturnToPool(v_object);
                                continue;
                            }
                        }
                        //is a member, include it and save his position
                        else
                        {
                            //Check if element if valid for this current data index
                            var v_isValidObjectForData = !p_resetAllObjects && IsDataViewObject(v_object, v_currentDataIndex);

                            //Add element only if is valid and not a template
                            v_elements.Add(v_isTemplate || !v_isValidObjectForData ? null : v_object);

                            //Update Index Mappers
                            v_lastIndexMember = v_elements.Count - 1;
                            _layoutIndexToDataIndex[v_lastIndexMember] = v_currentDataIndex;
                            _dataIndexToLayoutIndex[v_currentDataIndex] = v_lastIndexMember;
                            
                            //Return to pool if is invalid
                            if (!v_isValidObjectForData)
                            {
                                v_replacementDataLayoutSize[v_lastIndexMember] = StipulateElementSize(v_currentDataIndex);
                                v_needReapplyElements = true;

                                if (v_object != null)
                                    ReturnToPool(v_object);
                            }

                            //Increment to next DataIndex
                            v_currentDataIndex++;
                        }
                    }
                    else if(v_object != null && !v_isTemplate)
                        v_elements.Add(v_object);
                    //Disable Templates
                    if (v_isTemplate)
                        v_object.SetActive(false);
                }
                
                //Try add elements not included in layout
                while (v_currentDataIndex < m_data.Count)
                {
                    v_lastIndexMember = v_lastIndexMember < 0 ? _initialIndex : v_lastIndexMember + 1;
                    v_elements.Insert(v_lastIndexMember, null);
                    _layoutIndexToDataIndex[v_lastIndexMember] = v_currentDataIndex;
                    _dataIndexToLayoutIndex[v_currentDataIndex] = v_lastIndexMember;
                    //Stipulate Size of layout for the first Recalc
                    v_replacementDataLayoutSize[v_lastIndexMember] = StipulateElementSize(v_currentDataIndex);
                    
                    v_currentDataIndex++;
                    v_needReapplyElements = true;
                }
                if (v_needReapplyElements || ScrollLayoutGroup.ElementsCount != v_elements.Count)
                {
                    ScrollLayoutGroup.ReplaceElements(v_elements);
                    //Force Change Layout Size
                    foreach (var v_pair in v_replacementDataLayoutSize)
                    {
                        ScrollLayoutGroup.SetCachedElementSize(v_pair.Key, v_pair.Value);
                    }
                }
            }
        }

        protected float StipulateElementSize(int p_dataIndex)
        {
            var v_data = m_data != null && m_data.Count > p_dataIndex && p_dataIndex >= 0 ? m_data[p_dataIndex] : null;
            var v_template = GetTemplateFromDataType(v_data != null? v_data.GetType() : null);
            return ScrollLayoutGroup.CalculateElementSize(v_template != null? v_template.transform : null, ScrollLayoutGroup != null ? ScrollLayoutGroup.IsVertical() : true);
        }

        protected bool IsDataViewObject(GameObject p_object)
        {
            if (p_object != null)
            {
                if (IsCreatedByPool(p_object))
                    return true;
            }
            return false;
        }

        protected bool IsDataViewObject(GameObject p_object, int p_dataIndex)
        {
            if (p_object != null)
            {
                var v_objTemplate = GetTemplateFromObject(p_object);
                var v_dataTemplate = GetTemplateFromDataIndex(p_dataIndex);
                return v_objTemplate != null && v_objTemplate == v_dataTemplate;
            }
            return false;
        }

        protected bool IsDataViewTemplate(GameObject p_object)
        {
            if (p_object != null)
            {
                if ((p_object == m_defaultTemplate || _templatePerCreatedObjects.ContainsKey(p_object)))
                    return true;
            }
            return false;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnElementBecameVisible(GameObject p_elementObj, int p_layoutIndex)
        {
            if (p_elementObj != null)
            {
                var v_dataIndex = ConvertLayoutIndexToDataIndex(p_layoutIndex);
                if (v_dataIndex >= 0)
                {
                    var v_elements = p_elementObj.GetComponents<IReloadableDataViewElement>();

                    if (v_elements.Length > 0)
                    {
                        ReloadEventArgs v_arg = new ReloadEventArgs();
                        v_arg.Data = m_data.Count > v_dataIndex ? m_data[v_dataIndex] : null;
                        v_arg.DataIndex = v_dataIndex;
                        v_arg.LayoutElement = p_elementObj;
                        v_arg.LayoutElementIndex = p_layoutIndex;
                        v_arg.Sender = this;

                        //Force reload all IReloadableElement in target gameobject
                        foreach (var v_element in v_elements)
                        {
                            if (v_element != null && !v_element.IsDestroyed() && v_element.enabled)
                                v_element.Reload(v_arg);
                        }

                        if (OnReloadElement != null)
                            OnReloadElement.Invoke(v_arg);
                    }
                }
            }
        }

        protected virtual void HandleOnAddElements(int[] p_sortedAddedIndexes)
        {
            if (p_sortedAddedIndexes != null && m_data != null && _initialIndex >= 0)
            {
                bool v_needRemapIndexes = false;
                foreach (var v_index in p_sortedAddedIndexes)
                {
                    //Element added in initialIndex position, we must change the initial position
                    if (v_index == _initialIndex)
                    {
                        v_needRemapIndexes = true;
                        _initialIndex++;
                    }
                    else if (!v_needRemapIndexes)
                        v_needRemapIndexes = v_index >= _initialIndex && v_index < _initialIndex + m_data.Count;
                }
                if (v_needRemapIndexes)
                    MarkToRemapIndexes();
            }
        }

        protected virtual void HandleOnRemoveElements(int[] p_sortedRemovedIndexes)
        {
            if (p_sortedRemovedIndexes != null && m_data != null && _initialIndex >= 0)
            {
                bool v_needRemapIndexes = false;
                foreach (var v_index in p_sortedRemovedIndexes)
                {
                    if (!v_needRemapIndexes)
                    {
                        v_needRemapIndexes = v_index >= _initialIndex && v_index < _initialIndex + m_data.Count;
                        break;
                    }
                }
                if (v_needRemapIndexes)
                    MarkToRemapIndexes();
            }
        }

        protected virtual void HandleOnBeforeChangeVisibleElements(Vector2Int p_oldVisibleIndexRange)
        {
            if (m_data != null && m_data.Count > 0)
            {
                var v_currentVisibleIndexRange = ScrollLayoutGroup.VisibleElementsIndexRange;
                for (int i = p_oldVisibleIndexRange.x; i <= p_oldVisibleIndexRange.y; i++)
                {
                    //We want to pick elements out of new range to reuse in pool
                    if (i >= 0 && i < ScrollLayoutGroup.ElementsCount && (i < v_currentVisibleIndexRange.x || i > v_currentVisibleIndexRange.y))
                    {
                        var v_element = ScrollLayoutGroup[i];
                        var v_dataIndex = ConvertLayoutIndexToDataIndex(i);
                        //Is a DataView Object
                        if (v_element != null && v_dataIndex >= 0)
                        {
                            ReturnToPool(v_dataIndex, v_element);
                            ScrollLayoutGroup[i] = null;
                        }
                    }
                }

                bool v_needApplyTemplateNames = false;
                //Change other pool objects
                for (int i = v_currentVisibleIndexRange.x; i <= v_currentVisibleIndexRange.y; i++)
                {
                    //We want to pick elements out of old range
                    var v_element = ScrollLayoutGroup.ElementsCount > i && i >= 0? ScrollLayoutGroup[i] : null;
                    if (i >= 0 && i < ScrollLayoutGroup.ElementsCount && (v_element == null || IsDataViewTemplate(v_element)))
                    {
                        
                        var v_dataIndex = ConvertLayoutIndexToDataIndex(i);
                        //Is a DataView Object
                        if (v_dataIndex >= 0)
                        {
                            var v_poolObject = CreateOrPopFromPool(v_dataIndex);
                            var v_oldElement = ScrollLayoutGroup[i];
                            ScrollLayoutGroup[i] = v_poolObject != null ? v_poolObject.gameObject : null;
                            if (v_oldElement != null)
                            {
                                v_oldElement.gameObject.SetActive(false);
                                v_needApplyTemplateNames = true;
                            }
                        }
                    }
                }
                if (v_needApplyTemplateNames)
                    InitTemplates();
            }
        }

        #endregion

        #region Pool

        Dictionary<GameObject, HashSet<GameObject>> _templatePerCreatedObjects = new Dictionary<GameObject, HashSet<GameObject>>();
        Dictionary<GameObject, HashSet<GameObject>> _templatePerPoolObjects = new Dictionary<GameObject, HashSet<GameObject>>();

        [SerializeField, HideInInspector]
        protected Transform _poolContent = null;

        protected virtual void DestroyUselessTemplates()
        {
            var v_templates = new List<GameObject>(_templatePerCreatedObjects.Keys);
            foreach (var v_template in v_templates)
            {
                if(m_defaultTemplate != v_template && !_typePerPrefabTemplate.ContainsValue(v_template) && !_indexPerPrefabTemplate.ContainsValue(v_template))
                    DestroyAllPoolCreatedObjectsFromTemplate(v_template, false);
            }
        }

        protected virtual void TryDestroyPoolContent()
        {
            DestroyAllPoolCreatedObjects();
            if (_poolContent != null)
            {
                if (Application.isPlaying)
                    GameObject.Destroy(_poolContent.gameObject);
                else
                    GameObject.DestroyImmediate(_poolContent.gameObject);
            }
        }

        protected virtual void DestroyAllPoolCreatedObjects()
        {
            var v_templates = new List<GameObject>( _templatePerCreatedObjects.Keys);
            foreach (var v_template in v_templates)
            {
                DestroyAllPoolCreatedObjectsFromTemplate(v_template, false);
            }
            _templatePerCreatedObjects.Clear();
            _templatePerPoolObjects.Clear();
        }

        protected virtual void DestroyAllPoolCreatedObjectsFromTemplate(GameObject p_template, bool p_safeTemplate = true)
        {
            if (p_safeTemplate)
                p_template = GetTemplateFromObject(p_template);
            if (p_template != null)
            {
                HashSet<GameObject> v_poolOfType = null;
                _templatePerCreatedObjects.TryGetValue(p_template, out v_poolOfType);
                if (v_poolOfType != null)
                {
                    foreach (var v_object in v_poolOfType)
                    {
                        if (v_object != null)
                        {
                            if (Application.isPlaying)
                                GameObject.Destroy(v_object.gameObject);
                            else
                                GameObject.DestroyImmediate(v_object.gameObject);
                        }
                    }
                    v_poolOfType.Clear();
                    _templatePerCreatedObjects.Remove(p_template);
                    _templatePerPoolObjects.Remove(p_template);
                }
            }
        }

        protected virtual void SetupPool()
        {
            var v_templates = new HashSet<GameObject>(_typePerPrefabTemplate.Values);
            if(!v_templates.Contains(m_defaultTemplate))
                v_templates.Add(m_defaultTemplate);
            foreach (var v_pair in _indexPerPrefabTemplate)
            {
                if (v_pair.Value != null && !v_templates.Contains(v_pair.Value))
                    v_templates.Add(v_pair.Value);
            }

            foreach (var v_template in v_templates)
            {
                if (v_template != null)
                {
                    if (!_templatePerPoolObjects.ContainsKey(v_template))
                        _templatePerPoolObjects.Add(v_template, new HashSet<GameObject>());
                    if (!_templatePerCreatedObjects.ContainsKey(v_template))
                        _templatePerCreatedObjects.Add(v_template, new HashSet<GameObject>());
                }
            }
            DestroyUselessTemplates();
            TrySetupPoolContent();
        }

        protected virtual void TrySetupPoolContent()
        {
            if (_poolContent == null && Application.isPlaying)
            {
                var v_poolContentObj = new GameObject("[AUTO_GEN] Pool Content");
                v_poolContentObj.transform.SetParent(this.transform);
                v_poolContentObj.transform.localPosition = Vector3.zero;
                v_poolContentObj.transform.localScale = Vector3.one;
                v_poolContentObj.transform.localRotation = Quaternion.identity;
                var v_canvas = v_poolContentObj.GetComponent<Canvas>();
                if (v_canvas == null)
                    v_canvas = v_poolContentObj.AddComponent<Canvas>();
                v_canvas.enabled = false;

                var v_layoutElement = v_poolContentObj.GetComponent<LayoutElement>();
                if (v_layoutElement == null)
                    v_layoutElement = v_poolContentObj.AddComponent<LayoutElement>();
                v_layoutElement.ignoreLayout = true;

                _poolContent = v_poolContentObj.transform as RectTransform;
                if (_poolContent == null)
                    _poolContent = v_poolContentObj.AddComponent<RectTransform>();
            }
            if(_poolContent != null && !m_disableElementsInPool != _poolContent.gameObject.activeSelf)
                _poolContent.gameObject.SetActive(!m_disableElementsInPool);
        }

        protected virtual bool IsCreatedByPool(GameObject p_object)
        {
            if (p_object != null)
            {
                //Is the template
                if (_templatePerCreatedObjects.ContainsKey(p_object))
                    return true;
                else
                {
                    foreach (var v_pair in _templatePerCreatedObjects)
                    {
                        var v_template = v_pair.Key;
                        if (v_pair.Value.Contains(p_object))
                            return true;
                    }
                }

            }
            return false;
        }

        protected virtual GameObject GetTemplateFromObject(GameObject p_object)
        {
            if (p_object != null)
            {
                //Is the template
                if (_templatePerCreatedObjects.ContainsKey(p_object))
                    return p_object;
                else
                {
                    foreach (var v_pair in _templatePerCreatedObjects)
                    {
                        var v_template = v_pair.Key;
                        if (v_pair.Value.Contains(p_object))
                            return v_template;
                    }
                }

            }
            return null;
        }

        protected GameObject CreateOrPopFromPool(int p_dataIndex)
        {
            var v_template = GetTemplateFromDataIndex(p_dataIndex);
            return CreateOrPopFromPool(v_template, false);
        }

        protected GameObject CreateOrPopFromPool(System.Type p_dataType)
        {
            var v_template = GetTemplateFromDataType(p_dataType);
            return CreateOrPopFromPool(v_template, false);
        }

        protected GameObject CreateOrPopFromPool(GameObject p_template, bool p_safeTemplate = false)
        {
            TrySetupPoolContent();
            if (p_safeTemplate)
                p_template = GetTemplateFromObject(p_template);  
            if (p_template != null)
            {
                //Disable Scene Templates
                if (p_template.gameObject.scene.IsValid())
                    p_template.gameObject.SetActive(false);
                var v_object = PopFromPool(p_template);
                if (v_object == null)
                {
                    v_object = GameObject.Instantiate(p_template);
                    v_object.transform.SetParent(_poolContent, false);
                    v_object.transform.localScale = Vector3.one;
                    v_object.transform.localPosition = Vector3.zero;
                    v_object.transform.localRotation = Quaternion.identity;

                    HashSet<GameObject> v_createdObjectsList = null;
                    if (!_templatePerCreatedObjects.TryGetValue(p_template, out v_createdObjectsList))
                    {
                        v_createdObjectsList = new HashSet<GameObject>();
                        _templatePerCreatedObjects[p_template] = v_createdObjectsList;
                    }
                    if(v_createdObjectsList != null)
                        v_createdObjectsList.Add(v_object);
                    v_object.gameObject.SetActive(true);
                }
                return v_object;
            }
            Debug.LogWarning("No DefaultTemplate or TypePerTemplate setted in DataView: " + name);
            return null;
        }

        protected GameObject PopFromPool(System.Type p_dataType)
        {
            return PopFromPool(GetTemplateFromDataType(p_dataType), false);
        }

        protected GameObject PopFromPool(GameObject p_template, bool p_safeTemplate = true)
        {
            if(p_safeTemplate)
                p_template = GetTemplateFromObject(p_template);
            GameObject v_poolObject = null;
            if (Application.isPlaying)
            {
                HashSet<GameObject> v_poolOfType = null;
                if (p_template != null)
                    _templatePerPoolObjects.TryGetValue(p_template, out v_poolOfType);

                if (v_poolOfType != null)
                {
                    foreach (var v_object in v_poolOfType)
                    {
                        if (v_object != null)
                        {
                            v_poolObject = v_object;
                            break;
                        }
                    }
                }
                if (v_poolObject != null)
                {
                    v_poolOfType.Remove(v_poolObject);
                    
                    v_poolObject.gameObject.hideFlags = HideFlags.None;
                    v_poolObject.gameObject.SetActive(true);

                    //We Poped the object, so we must cancel the pool Recalc Function
                    var v_index = _objectsToSendToPoolParent.IndexOf(v_poolObject);
                    if (v_index >= 0)
                        _objectsToSendToPoolParent.RemoveAt(v_index);
                }
            }
            return v_poolObject;
        }

        List<GameObject> _objectsToSendToPoolParent = new List<GameObject>();
        protected virtual void SendObjectsToPoolParent()
        {
            if (_poolContent != null)
            {
                foreach (var v_object in _objectsToSendToPoolParent)
                {
                    if(v_object != null)
                        v_object.transform.SetParent(_poolContent, false);
                }
            }
            _objectsToSendToPoolParent.Clear();
        }

        protected bool ReturnToPool(GameObject p_object)
        {
            return ReturnToPool(GetTemplateFromObject(p_object), p_object, false);
        }

        protected bool ReturnToPool(GameObject p_template, GameObject p_object, bool p_safeTemplate = false)
        {
            if (p_object != null)
            {
                if (p_safeTemplate)
                    p_template = GetTemplateFromObject(p_template);
                if (p_object != p_template)
                {
                    HashSet<GameObject> v_poolOfType = null;
                    if (p_template != null)
                    {
                        _templatePerPoolObjects.TryGetValue(p_template, out v_poolOfType);
                        if (v_poolOfType == null)
                        {
                            v_poolOfType = new HashSet<GameObject>();
                            _templatePerPoolObjects[p_template] = v_poolOfType;
                        }
                    }
                    if (v_poolOfType != null && !v_poolOfType.Contains(p_object))
                    {
                        v_poolOfType.Add(p_object);
                        //p_object.transform.SetParent(_poolContent, false);
                        _objectsToSendToPoolParent.Add(p_object);
                        CancelInvoke("SendObjectsToPoolParent");
                        Invoke("SendObjectsToPoolParent", 0.1f);
                        return true;
                    }
                }
            }
            return false;
        }

        protected bool ReturnToPool(int p_dataIndex, GameObject p_object)
        {
            var v_template = GetTemplateFromDataIndex(p_dataIndex);
            return ReturnToPool(v_template, p_object, false);
        }

        protected bool ReturnToPool(System.Type p_dataType, GameObject p_object)
        {
            var v_template = GetTemplateFromDataType(p_dataType);
            return ReturnToPool(v_template, p_object, false);
        }
    
        #endregion
    }
}
