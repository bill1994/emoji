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
        void Reload(ScrollDataView.ReloadEventArgs args);

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
                var list = new List<object>();
                if (m_data != null)
                {
                    foreach (var data in m_data)
                    {
                        list.Add(data);
                    }
                }
                return list.AsReadOnly();
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
            TrySendObjectsToPoolParent();
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

        public void Setup(IList data, IList<GameObject> customTemplatePerIndexList)
        {
            Setup(data, null, customTemplatePerIndexList);
        }

        public void Setup(IList data, Dictionary<int, GameObject> customTemplatePerIndex)
        {
            Setup(data, null, customTemplatePerIndex);
        }

        public void Setup(IList data, Dictionary<System.Type, GameObject> customTemplatePerDataType, IList<GameObject> customTemplatePerIndexList)
        {
            Dictionary<int, GameObject> customTemplateDict = new Dictionary<int, GameObject>();
            if (customTemplatePerIndexList != null)
            {
                for (int i = 0; i < customTemplatePerIndexList.Count; i++)
                {
                    var template = customTemplatePerIndexList[i];
                    if (template != null && template != m_defaultTemplate)
                        customTemplateDict[i] = template;
                }
            }
            Setup(data, customTemplatePerDataType, customTemplateDict);
        }

        bool _setupOnEnable = false;
        protected Dictionary<System.Type, GameObject> _typePerPrefabTemplate = new Dictionary<System.Type, GameObject>();
        protected Dictionary<int, GameObject> _indexPerPrefabTemplate= new Dictionary<int, GameObject>();
        public virtual void Setup(IList data, Dictionary<System.Type, GameObject> customTemplatePerDataType = null, Dictionary<int, GameObject> customTemplatePerIndex = null)
        {

            //Set new Prefab per Type
            _typePerPrefabTemplate = customTemplatePerDataType;
            if (_typePerPrefabTemplate == null)
                _typePerPrefabTemplate = new Dictionary<System.Type, GameObject>();

            //Set new Prefab per Index
            _indexPerPrefabTemplate = customTemplatePerIndex;
            if (_indexPerPrefabTemplate == null)
                _indexPerPrefabTemplate = new Dictionary<int, GameObject>();

            m_data = data;
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

        public GameObject GetTemplateFromDataType(System.Type dataType)
        {
            GameObject template = m_defaultTemplate;
            if (_typePerPrefabTemplate.Count > 0)
            {
                //Try find for each type and subclass type
                while (dataType != null)
                {
                    if (_typePerPrefabTemplate.TryGetValue(dataType, out template))
                        break;
                    dataType = dataType.BaseType;
                }
                if (template == null)
                    template = m_defaultTemplate;
            }
            return template;
        }

        public void SetTemplateFromDataIndex(System.Type dataType, GameObject template)
        {
            if (dataType != null)
            {
                GameObject oldTemplate = null;
                var sucess = _typePerPrefabTemplate.TryGetValue(dataType, out oldTemplate);

                if (sucess && (template == null || template == m_defaultTemplate))
                    _typePerPrefabTemplate.Remove(dataType);
                else if (oldTemplate != template && template != null)
                    _typePerPrefabTemplate[dataType] = template;

                if (oldTemplate != template)
                    MarkToRemapIndexes();
            }
        }

        public GameObject GetTemplateFromDataIndex(int dataIndex)
        {
            GameObject template = null;
            if (!_indexPerPrefabTemplate.TryGetValue(dataIndex, out template) && _typePerPrefabTemplate.Count > 0)
            {
                var data = m_data != null && m_data.Count > dataIndex && dataIndex >= 0 ? m_data[dataIndex] : null;
                if (data != null)
                    template = GetTemplateFromDataType(data.GetType());
            }
            //Prevent return default template
            if (template == null)
                template = m_defaultTemplate;
            return template;
        }

        public void SetTemplateFromDataIndex(int dataIndex, GameObject template)
        {
            GameObject oldTemplate = null;
            var sucess = _indexPerPrefabTemplate.TryGetValue(dataIndex, out oldTemplate);

            if (sucess && (template == null || template == m_defaultTemplate))
                _indexPerPrefabTemplate.Remove(dataIndex);
            else if(oldTemplate != template && template != null)
                _indexPerPrefabTemplate[dataIndex] = template;

            if (oldTemplate != template)
                ScrollLayoutGroup.SetCachedElementsLayoutDirty();
        }

        public GameObject GetElementAtDataIndex(int dataIndex)
        {
            int layoutIndex = ConvertDataIndexToLayoutIndex(dataIndex);
            if(ScrollLayoutGroup != null && layoutIndex >= 0 && layoutIndex < m_scrollRectLayout.ElementsCount)
            {
                return m_scrollRectLayout[layoutIndex];
            }
            return null;
        }

        public int ConvertDataIndexToLayoutIndex(int dataIndex)
        {
            var layoutIndex = -1;
            if (!_dataIndexToLayoutIndex.TryGetValue(dataIndex, out layoutIndex))
                layoutIndex = -1;
            return layoutIndex;
        }


        public int ConvertLayoutIndexToDataIndex(int layoutIndex)
        {
            var dataIndex = -1;
            if (!_layoutIndexToDataIndex.TryGetValue(layoutIndex, out dataIndex))
                dataIndex = -1;
            return dataIndex;
        }

        protected int _initialIndex = 0;
        protected Dictionary<int, int> _dataIndexToLayoutIndex = new Dictionary<int, int>();
        protected Dictionary<int, int> _layoutIndexToDataIndex = new Dictionary<int, int>();
        public virtual void TryRemapIndexes(bool force = false)
        {
            if (_indexesDirty || force)
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
                foreach (var pair in _indexPerPrefabTemplate)
                {
                    var template = pair.Value;
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
                foreach (var pair in _typePerPrefabTemplate)
                {
                    var template = pair.Value;
                    if (template != null && template.scene.IsValid())
                    {
                        template.name = "Template (Type: " + pair.Key.ToString() + ")";
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

        protected virtual void RemapIndexes(bool resetAllObjects = false)
        {
            Dictionary<int, float> replacementDataLayoutSize = new Dictionary<int, float>();

            _layoutIndexToDataIndex.Clear();
            _dataIndexToLayoutIndex.Clear();
            if (m_data != null && ScrollLayoutGroup != null)
            {
                bool needReapplyElements = false;
                var elements = new List<GameObject>();
                _initialIndex =  Mathf.Clamp((m_defaultSetupInitialIndex < 0 ? ScrollLayoutGroup.ElementsCount - 1 : m_defaultSetupInitialIndex), 0, ScrollLayoutGroup.ElementsCount);
                var lastIndexMember = -1;
                var currentDataIndex = 0;
                for (int i = 0; i < ScrollLayoutGroup.ElementsCount; i++)
                {
                    var go = ScrollLayoutGroup[i];
                    var isTemplate = IsDataViewTemplate(go);
                    if (i >= _initialIndex && !isTemplate && (go == null || IsDataViewObject(go)))
                    {
                        //We dont want extra elements related to data in ScrollLayoutGroup
                        if (currentDataIndex >= m_data.Count)
                        {
                            needReapplyElements = true;
                            if (go != null)
                            {
                                ReturnToPool(go);
                                continue;
                            }
                        }
                        //is a member, include it and save his position
                        else
                        {
                            //Check if element if valid for this current data index
                            var isValidObjectForData = !resetAllObjects && IsDataViewObject(go, currentDataIndex);

                            //Add element only if is valid and not a template
                            elements.Add(isTemplate || !isValidObjectForData ? null : go);

                            //Update Index Mappers
                            lastIndexMember = elements.Count - 1;
                            _layoutIndexToDataIndex[lastIndexMember] = currentDataIndex;
                            _dataIndexToLayoutIndex[currentDataIndex] = lastIndexMember;
                            
                            //Return to pool if is invalid
                            if (!isValidObjectForData)
                            {
                                replacementDataLayoutSize[lastIndexMember] = StipulateElementSize(currentDataIndex);
                                needReapplyElements = true;

                                if (go != null)
                                    ReturnToPool(go);
                            }

                            //Increment to next DataIndex
                            currentDataIndex++;
                        }
                    }
                    else if(go != null && !isTemplate)
                        elements.Add(go);
                    //Disable Templates
                    if (isTemplate)
                        go.SetActive(false);
                }
                
                //Try add elements not included in layout
                while (currentDataIndex < m_data.Count)
                {
                    lastIndexMember = lastIndexMember < 0 ? _initialIndex : lastIndexMember + 1;
                    elements.Insert(lastIndexMember, null);
                    _layoutIndexToDataIndex[lastIndexMember] = currentDataIndex;
                    _dataIndexToLayoutIndex[currentDataIndex] = lastIndexMember;
                    //Stipulate Size of layout for the first Recalc
                    replacementDataLayoutSize[lastIndexMember] = StipulateElementSize(currentDataIndex);
                    
                    currentDataIndex++;
                    needReapplyElements = true;
                }
                if (needReapplyElements || ScrollLayoutGroup.ElementsCount != elements.Count)
                {
                    ScrollLayoutGroup.ReplaceElements(elements);
                    //Force Change Layout Size
                    foreach (var pair in replacementDataLayoutSize)
                    {
                        ScrollLayoutGroup.SetCachedElementSize(pair.Key, pair.Value);
                    }
                }
            }
        }

        protected float StipulateElementSize(int dataIndex)
        {
            var data = m_data != null && m_data.Count > dataIndex && dataIndex >= 0 ? m_data[dataIndex] : null;
            var template = GetTemplateFromDataType(data != null? data.GetType() : null);
            return ScrollLayoutGroup.CalculateElementSize(template != null? template.transform : null, ScrollLayoutGroup != null ? ScrollLayoutGroup.IsVertical() : true);
        }

        protected bool IsDataViewObject(GameObject go)
        {
            if (go != null)
            {
                if (IsCreatedByPool(go))
                    return true;
            }
            return false;
        }

        protected bool IsDataViewObject(GameObject go, int dataIndex)
        {
            if (go != null)
            {
                var objTemplate = GetTemplateFromObject(go);
                var dataTemplate = GetTemplateFromDataIndex(dataIndex);
                return objTemplate != null && objTemplate == dataTemplate;
            }
            return false;
        }

        protected bool IsDataViewTemplate(GameObject go)
        {
            if (go != null)
            {
                if ((go == m_defaultTemplate || _templatePerCreatedObjects.ContainsKey(go)))
                    return true;
            }
            return false;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnElementBecameVisible(GameObject elementObj, int layoutIndex)
        {
            if (elementObj != null)
            {
                var dataIndex = ConvertLayoutIndexToDataIndex(layoutIndex);
                if (dataIndex >= 0)
                {
                    var elements = elementObj.GetComponents<IReloadableDataViewElement>();

                    if (elements.Length > 0)
                    {
                        ReloadEventArgs arg = new ReloadEventArgs();
                        arg.Data = m_data.Count > dataIndex ? m_data[dataIndex] : null;
                        arg.DataIndex = dataIndex;
                        arg.LayoutElement = elementObj;
                        arg.LayoutElementIndex = layoutIndex;
                        arg.Sender = this;

                        //Force reload all IReloadableElement in target gamego
                        foreach (var element in elements)
                        {
                            if (element != null && !element.IsDestroyed() && element.enabled)
                                element.Reload(arg);
                        }

                        if (OnReloadElement != null)
                            OnReloadElement.Invoke(arg);
                    }
                }
            }
        }

        protected virtual void HandleOnAddElements(int[] sortedAddedIndexes)
        {
            if (sortedAddedIndexes != null && m_data != null && _initialIndex >= 0)
            {
                bool needRemapIndexes = false;
                foreach (var index in sortedAddedIndexes)
                {
                    //Element added in initialIndex position, we must change the initial position
                    if (index == _initialIndex)
                    {
                        needRemapIndexes = true;
                        _initialIndex++;
                    }
                    else if (!needRemapIndexes)
                        needRemapIndexes = index >= _initialIndex && index < _initialIndex + m_data.Count;
                }
                if (needRemapIndexes)
                    MarkToRemapIndexes();
            }
        }

        protected virtual void HandleOnRemoveElements(int[] sortedRemovedIndexes)
        {
            if (sortedRemovedIndexes != null && m_data != null && _initialIndex >= 0)
            {
                bool needRemapIndexes = false;
                foreach (var index in sortedRemovedIndexes)
                {
                    if (!needRemapIndexes)
                    {
                        needRemapIndexes = index >= _initialIndex && index < _initialIndex + m_data.Count;
                        break;
                    }
                }
                if (needRemapIndexes)
                    MarkToRemapIndexes();
            }
        }

        protected virtual void HandleOnBeforeChangeVisibleElements(Vector2Int oldVisibleIndexRange)
        {
            if (m_data != null && m_data.Count > 0)
            {
                Dictionary<GameObject, Stack<GameObject>> templatedPerObjectToReturn = new Dictionary<GameObject, Stack<GameObject>>();
                var currentVisibleIndexRange = ScrollLayoutGroup.VisibleElementsIndexRange;
                for (int i = oldVisibleIndexRange.x; i <= oldVisibleIndexRange.y; i++)
                {
                    //We want to pick elements out of new range to reuse in pool
                    if (i >= 0 && i < ScrollLayoutGroup.ElementsCount && (i < currentVisibleIndexRange.x || i > currentVisibleIndexRange.y))
                    {
                        var element = ScrollLayoutGroup[i];
                        var dataIndex = ConvertLayoutIndexToDataIndex(i);
                        //Is a DataView Object
                        if (element != null && dataIndex >= 0)
                        {
                            var template = GetTemplateFromDataIndex(dataIndex);
                            //Add in template list (we will try recycle this gos first)
                            if (template != null)
                            {
                                Stack<GameObject> templateObjects = null;
                                if (!templatedPerObjectToReturn.TryGetValue(template, out templateObjects) || templateObjects == null)
                                {
                                    templateObjects = new Stack<GameObject>();
                                    templatedPerObjectToReturn[template] = templateObjects;
                                }
                                templateObjects.Push(element);
                            }
                            
                            //ReturnToPool(dataIndex, element);
                            ScrollLayoutGroup[i] = null;
                        }
                    }
                }

                bool needApplyTemplateNames = false;
                //Change other pool gos
                for (int i = currentVisibleIndexRange.x; i <= currentVisibleIndexRange.y; i++)
                {
                    //We want to pick elements out of old range
                    var element = ScrollLayoutGroup.ElementsCount > i && i >= 0? ScrollLayoutGroup[i] : null;
                    if (i >= 0 && i < ScrollLayoutGroup.ElementsCount && (element == null || IsDataViewTemplate(element)))
                    {
                        
                        var dataIndex = ConvertLayoutIndexToDataIndex(i);
                        //Is a DataView Object
                        if (dataIndex >= 0)
                        {
                            var template = GetTemplateFromDataIndex(dataIndex);
                            GameObject poolObject = null;
                            Stack<GameObject> stack = null;
                            if (template != null)
                            {
                                if(templatedPerObjectToReturn.TryGetValue(template, out stack) && stack != null)
                                    poolObject = stack.Pop();
                                //We dont need this stack anymore
                                if (stack == null || stack.Count == 0)
                                    templatedPerObjectToReturn.Remove(template);
                            }
                            //Pick from Default pool if we cant recycle previous deactivated templates
                            if(poolObject == null)
                                poolObject = CreateOrPopFromPool(template);

                            var oldElement = ScrollLayoutGroup[i];
                            ScrollLayoutGroup[i] = poolObject != null ? poolObject.gameObject : null;
                            if (oldElement != null)
                            {
                                oldElement.gameObject.SetActive(false);
                                needApplyTemplateNames = true;
                            }
                        }
                    }
                }

                //Return unused gos deactivate this cycle too pool
                foreach (var pair in templatedPerObjectToReturn)
                {
                    var elementsToReturn = pair.Value;
                    foreach(var elementToReturn in elementsToReturn)
                    {
                        ReturnToPool(pair.Key, elementToReturn);
                    }
                }
                templatedPerObjectToReturn.Clear();

                if (needApplyTemplateNames)
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
            var templates = new List<GameObject>(_templatePerCreatedObjects.Keys);
            foreach (var template in templates)
            {
                if(m_defaultTemplate != template && !_typePerPrefabTemplate.ContainsValue(template) && !_indexPerPrefabTemplate.ContainsValue(template))
                    DestroyAllPoolCreatedObjectsFromTemplate(template, false);
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
            var templates = new List<GameObject>( _templatePerCreatedObjects.Keys);
            foreach (var template in templates)
            {
                DestroyAllPoolCreatedObjectsFromTemplate(template, false);
            }
            _templatePerCreatedObjects.Clear();
            _templatePerPoolObjects.Clear();
        }

        protected virtual void DestroyAllPoolCreatedObjectsFromTemplate(GameObject template, bool safeTemplate = true)
        {
            if (safeTemplate)
                template = GetTemplateFromObject(template);
            if (template != null)
            {
                HashSet<GameObject> poolOfType = null;
                _templatePerCreatedObjects.TryGetValue(template, out poolOfType);
                if (poolOfType != null)
                {
                    foreach (var go in poolOfType)
                    {
                        if (go != null)
                        {
                            if (Application.isPlaying)
                                GameObject.Destroy(go.gameObject);
                            else
                                GameObject.DestroyImmediate(go.gameObject);
                        }
                    }
                    poolOfType.Clear();
                    _templatePerCreatedObjects.Remove(template);
                    _templatePerPoolObjects.Remove(template);
                }
            }
        }

        protected virtual void SetupPool()
        {
            var templates = new HashSet<GameObject>(_typePerPrefabTemplate.Values);
            if(!templates.Contains(m_defaultTemplate))
                templates.Add(m_defaultTemplate);
            foreach (var pair in _indexPerPrefabTemplate)
            {
                if (pair.Value != null && !templates.Contains(pair.Value))
                    templates.Add(pair.Value);
            }

            foreach (var template in templates)
            {
                if (template != null)
                {
                    if (!_templatePerPoolObjects.ContainsKey(template))
                        _templatePerPoolObjects.Add(template, new HashSet<GameObject>());
                    if (!_templatePerCreatedObjects.ContainsKey(template))
                        _templatePerCreatedObjects.Add(template, new HashSet<GameObject>());
                }
            }
            DestroyUselessTemplates();
            TrySetupPoolContent();
        }

        protected virtual void TrySetupPoolContent()
        {
            if (_poolContent == null && Application.isPlaying)
            {
                var poolContentObj = new GameObject("[AUTO_GEN] Pool Content");
                poolContentObj.transform.SetParent(this.transform);
                poolContentObj.transform.localPosition = Vector3.zero;
                poolContentObj.transform.localScale = Vector3.one;
                poolContentObj.transform.localRotation = Quaternion.identity;
                var canvas = poolContentObj.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = poolContentObj.AddComponent<Canvas>();
                canvas.enabled = false;

                var layoutElement = poolContentObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = poolContentObj.AddComponent<LayoutElement>();
                layoutElement.ignoreLayout = true;

                _poolContent = poolContentObj.transform as RectTransform;
                if (_poolContent == null)
                    _poolContent = poolContentObj.AddComponent<RectTransform>();
            }
            if(_poolContent != null && !m_disableElementsInPool != _poolContent.gameObject.activeSelf)
                _poolContent.gameObject.SetActive(!m_disableElementsInPool);
        }

        protected virtual bool IsCreatedByPool(GameObject go)
        {
            if (go != null)
            {
                //Is the template
                if (_templatePerCreatedObjects.ContainsKey(go))
                    return true;
                else
                {
                    foreach (var pair in _templatePerCreatedObjects)
                    {
                        var template = pair.Key;
                        if (pair.Value.Contains(go))
                            return true;
                    }
                }

            }
            return false;
        }

        protected virtual GameObject GetTemplateFromObject(GameObject go)
        {
            if (go != null)
            {
                //Is the template
                if (_templatePerCreatedObjects.ContainsKey(go))
                    return go;
                else
                {
                    foreach (var pair in _templatePerCreatedObjects)
                    {
                        var template = pair.Key;
                        if (pair.Value.Contains(go))
                            return template;
                    }
                }

            }
            return null;
        }

        protected GameObject CreateOrPopFromPool(int dataIndex)
        {
            var template = GetTemplateFromDataIndex(dataIndex);
            return CreateOrPopFromPool(template, false);
        }

        protected GameObject CreateOrPopFromPool(System.Type dataType)
        {
            var template = GetTemplateFromDataType(dataType);
            return CreateOrPopFromPool(template, false);
        }

        protected GameObject CreateOrPopFromPool(GameObject template, bool safeTemplate = false)
        {
            TrySetupPoolContent();
            if (safeTemplate)
                template = GetTemplateFromObject(template);  
            if (template != null)
            {
                //Disable Scene Templates
                if (template.gameObject.scene.IsValid())
                    template.gameObject.SetActive(false);
                var go = PopFromPool(template);
                if (go == null)
                {
                    go = GameObject.Instantiate(template);
                    go.transform.SetParent(_poolContent, false);
                    go.transform.localScale = Vector3.one;
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;

                    HashSet<GameObject> createdObjectsList = null;
                    if (!_templatePerCreatedObjects.TryGetValue(template, out createdObjectsList))
                    {
                        createdObjectsList = new HashSet<GameObject>();
                        _templatePerCreatedObjects[template] = createdObjectsList;
                    }
                    if(createdObjectsList != null)
                        createdObjectsList.Add(go);
                    go.gameObject.SetActive(true);
                }
                return go;
            }
            Debug.LogWarning("No DefaultTemplate or TypePerTemplate setted in DataView: " + name);
            return null;
        }

        protected GameObject PopFromPool(System.Type dataType)
        {
            return PopFromPool(GetTemplateFromDataType(dataType), false);
        }

        protected GameObject PopFromPool(GameObject template, bool safeTemplate = true)
        {
            if(safeTemplate)
                template = GetTemplateFromObject(template);
            GameObject poolObject = null;
            if (Application.isPlaying)
            {
                HashSet<GameObject> poolOfType = null;
                if (template != null)
                    _templatePerPoolObjects.TryGetValue(template, out poolOfType);

                if (poolOfType != null)
                {
                    foreach (var go in poolOfType)
                    {
                        if (go != null)
                        {
                            poolObject = go;
                            break;
                        }
                    }
                }
                if (poolObject != null)
                {
                    poolOfType.Remove(poolObject);
                    
                    poolObject.gameObject.hideFlags = HideFlags.None;
                    poolObject.gameObject.SetActive(true);

                    //We Poped the go, so we must cancel the pool Recalc Function
                    _gosToSendToPoolParent.Remove(poolObject);

                    //if(_gosToSendToPoolParent.Count == 0)
                    //    CancelInvoke("SendObjectsToPoolParent");
                }
            }
            return poolObject;
        }

        HashSet<GameObject> _gosToSendToPoolParent = new HashSet<GameObject>();
        protected virtual void TrySendObjectsToPoolParent()
        {
            if (_poolContent != null)
            {
                foreach (var go in _gosToSendToPoolParent)
                {
                    if(go != null)
                        go.transform.SetParent(_poolContent, false);
                }
            }
            _gosToSendToPoolParent.Clear();
        }

        protected bool ReturnToPool(GameObject go)
        {
            return ReturnToPool(GetTemplateFromObject(go), go, false);
        }

        protected bool ReturnToPool(GameObject template, GameObject go, bool safeTemplate = false)
        {
            if (go != null)
            {
                if (safeTemplate)
                    template = GetTemplateFromObject(template);
                if (go != template)
                {
                    HashSet<GameObject> poolOfType = null;
                    if (template != null)
                    {
                        _templatePerPoolObjects.TryGetValue(template, out poolOfType);
                        if (poolOfType == null)
                        {
                            poolOfType = new HashSet<GameObject>();
                            _templatePerPoolObjects[template] = poolOfType;
                        }
                    }
                    if (poolOfType != null && !poolOfType.Contains(go))
                    {
                        poolOfType.Add(go);
                        //go.transform.SetParent(_poolContent, false);
                        _gosToSendToPoolParent.Add(go);
                        //CancelInvoke("SendObjectsToPoolParent");
                        //Invoke("SendObjectsToPoolParent", 0.1f);
                        return true;
                    }
                }
            }
            return false;
        }

        protected bool ReturnToPool(int dataIndex, GameObject go)
        {
            var template = GetTemplateFromDataIndex(dataIndex);
            return ReturnToPool(template, go, false);
        }

        protected bool ReturnToPool(System.Type dataType, GameObject go)
        {
            var template = GetTemplateFromDataType(dataType);
            return ReturnToPool(template, go, false);
        }
    
        #endregion
    }
}
