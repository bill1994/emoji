using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Kyub.UI
{
    [CreateAssetMenu]
    [System.Serializable]
    public class GenericMenuRootData : ScriptableObject, ISerializationCallbackReceiver
    {
        #region Private Variables

        [SerializeField]
        List<int> m_rootElementIds = new List<int>();

        [SerializeField]
        List<GenericMenuElementData> m_cachedElements = new List<GenericMenuElementData>();

        [System.NonSerialized]
        Dictionary<int, GenericMenuElementData> _cachedElementsDict = null;

        #endregion

        #region Public Functions

        public bool ItemExists(string p_path)
        {
            return GetItemAtPath(p_path) != null;
        }

        public bool FolderExists(string p_path)
        {
            return GetFolderAtPath(p_path) != null;
        }

        public bool ElementExists(string p_path)
        {
            return GetElementAtPath(p_path) != null;
        }

        public virtual void Clear()
        {
            m_rootElementIds.Clear();
            m_cachedElements.Clear();
            _cachedElementsDict = new Dictionary<int, GenericMenuElementData>();
        }

        public virtual GenericMenuElementData AddItem(string p_path)
        {
            var v_currentPaths = p_path.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            return AddItem_Internal(new List<string>(v_currentPaths), true);
        }

        public virtual GenericMenuElementData AddDisabledItem(string p_path)
        {
            var v_currentPaths = p_path.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            return AddItem_Internal(new List<string>(v_currentPaths), false);
        }

        public GenericMenuElementData GetElementById(int p_id)
        {
            TryFixCachedElements();
            GenericMenuElementData v_element = null;
            if (p_id < 0 || !_cachedElementsDict.TryGetValue(p_id, out v_element))
                v_element = null;
            return v_element;
        }

        public bool RemoveElementById(int p_id)
        {
            var v_sucess = false;
            for (int i = 0; i < m_cachedElements.Count; i++)
            {
                var v_element = m_cachedElements[i];
                if (v_element != null && v_element.Id == p_id)
                {
                    var v_parent = v_element.Parent;
                    if (v_parent != null)
                    {
                        var v_index = v_parent.IndexOf(v_element);
                        //remove from index list of child preventing from infinity loop
                        v_parent.RemoveAt_Internal(v_index);
                    }
                    else
                    {
                        //Remove From Root
                        var v_rootElementIndex = m_rootElementIds.IndexOf(p_id);
                        if (v_rootElementIndex >= 0)
                            m_rootElementIds.RemoveAt(v_rootElementIndex);
                    }
                    m_cachedElements.RemoveAt(i);
                    v_sucess = true;
                }
            }
            TryFixCachedElements();
            _cachedElementsDict.Remove(p_id);
            return v_sucess;
        }

        public virtual GenericMenuElementData GetElementAtPath(string p_path)
        {
            return GetElementInfoAtPath_Internal(p_path, true, true);
        }

        public virtual GenericMenuElementData GetFolderAtPath(string p_path)
        {
            return GetElementInfoAtPath_Internal(p_path, true, false);
        }

        public virtual GenericMenuElementData GetItemAtPath(string p_path)
        {
            return GetElementInfoAtPath_Internal(p_path, false, true);
        }

        public GenericMenuElementData[] GetNonFolderElements()
        {
            List<GenericMenuElementData> v_items = new List<GenericMenuElementData>();
            foreach (var v_element in m_cachedElements)
            {
                if (!v_element.IsFolder)
                    v_items.Add(v_element);
            }
            return v_items.ToArray();
        }

        public GenericMenuElementData[] GetFolderElements()
        {
            List<GenericMenuElementData> v_items = new List<GenericMenuElementData>();
            foreach (var v_element in m_cachedElements)
            {
                if (v_element.IsFolder)
                    v_items.Add(v_element);
            }
            return v_items.ToArray();
        }

        public GenericMenuElementData[] GetElements()
        {
            return m_cachedElements.ToArray();
        }

        public GenericMenuElementData[] GetRootNonFolder()
        {
            var v_root = new List<GenericMenuElementData>();
            foreach (var v_element in this)
            {
                if (v_element != null && !v_element.IsFolder)
                    v_root.Add(v_element);
            }
            return v_root.ToArray();
        }

        public GenericMenuElementData[] GetRootFolders()
        {
            var v_root = new List<GenericMenuElementData>();
            foreach (var v_element in this)
            {
                if (v_element != null && v_element.IsFolder)
                    v_root.Add(v_element);
            }
            return v_root.ToArray();
        }

        public GenericMenuElementData[] GetRootElements()
        {
            var v_root = new List<GenericMenuElementData>();
            foreach (var v_element in this)
            {
                if(v_element != null)
                    v_root.Add(v_element);
            }
            return v_root.ToArray();
        }

        #endregion

        #region List Enumerator Functions

        public virtual GenericMenuElementData this[int i]
        {
            get
            {
                TryFixCachedElements();
                GenericMenuElementData v_element = null;
                _cachedElementsDict.TryGetValue(m_rootElementIds[i], out v_element);
                
                return v_element;
            }
        }

        public int TotalCount
        {
            get
            {
                return m_cachedElements.Count;
            }
        }

        public int Count
        {
            get
            {
                return m_rootElementIds.Count;
            }
        }

        public IEnumerator<GenericMenuElementData> GetEnumerator()
        {
            TryFixCachedElements();
            for (int i = 0; i < m_rootElementIds.Count; ++i)
            {
                GenericMenuElementData v_element = null;
                _cachedElementsDict.TryGetValue(m_rootElementIds[i], out v_element);
                yield return v_element;
            }
        }

        #endregion

        #region Helper Functions

        protected internal virtual GenericMenuElementData GetElementInfoAtPath_Internal(string p_path, bool p_acceptFolder, bool p_acceptFiles)
        {
            var v_currentPaths = p_path.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            return GetElementInfoAtPath_Internal(new List<string>(v_currentPaths), p_acceptFolder, p_acceptFiles);
        }


        protected virtual GenericMenuElementData GetElementInfoAtPath_Internal(List<string> p_pathSplitted, bool p_acceptFolder, bool p_acceptFiles)
        {
            if ((!p_acceptFiles && !p_acceptFolder) ||
                p_pathSplitted == null ||
                p_pathSplitted.Count == 0)
            {
                return null;
            }

            var v_currentName = p_pathSplitted.Count > 0 ? p_pathSplitted[0] : null;
            if (p_pathSplitted.Count > 0)
                p_pathSplitted.RemoveAt(0);
            foreach (var v_child in this)
            {
                if (v_child != null && v_child.Name == v_currentName)
                {
                    //must check in min assets
                    if (p_pathSplitted.Count == 0)
                    {
                        if (v_child.IsFolder == p_acceptFolder || !v_child.IsFolder == p_acceptFiles)
                        {
                            return v_child;
                        }
                    }
                    //Check in Sub-folders
                    else if (v_child.IsFolder)
                    {
                        return v_child.GetElementInfoAtSubPath_Internal(p_pathSplitted, p_acceptFolder, p_acceptFiles);
                    }

                }
            }
            return null;
        }

        protected virtual GenericMenuElementData AddItem_Internal(List<string> p_splittedPath, bool p_enabled)
        {
            var v_currentName = p_splittedPath.Count > 0 ? p_splittedPath[0] : null;
            if (p_splittedPath.Count > 0)
                p_splittedPath.RemoveAt(0);

            GenericMenuElementData v_includedElement = null;
            //Add in Hierarchy
            if (p_splittedPath.Count > 0)
            {
                GenericMenuElementData v_parentWithName = null;
                foreach (var v_element in this)
                {
                    if (v_element.Name == v_currentName && v_element.IsFolder)
                    {
                        v_parentWithName = v_element;
                        break;
                    }
                }
                if (v_parentWithName == null)
                {
                    v_parentWithName = new GenericMenuElementData();
                    v_parentWithName.Name = v_currentName;
                    v_parentWithName.IsFolder = true;
                    v_parentWithName.Enabled = true;
                    if(AddAndCacheElement_Internal(v_parentWithName))
                    {
                        //Add in Root (is a Folder in Root)
                        m_rootElementIds.Add(v_parentWithName.Id);
                    }
                }
                v_includedElement = v_parentWithName.AddItemAtSubPath_Internal(p_splittedPath, p_enabled);
            }
            //Create in Root Elements
            else if (p_splittedPath.Count == 0)
            {
                v_includedElement = new GenericMenuElementData();
                v_includedElement.Name = v_currentName;
                v_includedElement.IsFolder = false;
                v_includedElement.Enabled = p_enabled;
                if (AddAndCacheElement_Internal(v_includedElement))
                {
                    //Add in Root (is an item in Root)
                    m_rootElementIds.Add(v_includedElement.Id);
                }
            }
            return v_includedElement;
        }

        //Called by childrens
        protected internal bool AddAndCacheElement_Internal(GenericMenuElementData p_element)
        {
            TryFixCachedElements();
            if (p_element != null && !_cachedElementsDict.ContainsKey(p_element.Id))
            {
                p_element.Root = this;
                m_cachedElements.Add(p_element);
                _cachedElementsDict[p_element.Id] = p_element;
                return true;
            }
            return false;
        }

        protected bool TryFixCachedElements(bool p_force = false)
        {
            if (_cachedElementsDict == null || _cachedElementsDict.Count != m_cachedElements.Count || p_force)
            {
                _cachedElementsDict = new Dictionary<int, GenericMenuElementData>();
                foreach (var v_element in m_cachedElements)
                {
                    if (v_element != null)
                    {
                        _cachedElementsDict[v_element.Id] = v_element;
                    }
                }
                return true;
            }
            return false;
        }

        #endregion

        #region Serialization Receivers

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            TryFixCachedElements(true);
        }

        #endregion
    }

    [System.Serializable]
    public class GenericMenuElementData
    {
        #region Private Variables

        [Space]
        [SerializeField]
        string m_name = null;
        [SerializeField]
        Sprite m_icon = null;
        [SerializeField]
        bool m_isFolder = false;
        [SerializeField]
        bool m_enabled = true;

        [Header("Hierarchy Fields")]
        [SerializeField]
        protected GenericMenuRootData m_root = null;
        [SerializeField]
        int m_id = -1;
        [SerializeField]
        List<int> m_childIds = new List<int>();
        [SerializeField]
        protected int m_parentId = -1;

        protected static System.Random _random = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get
            {
                return m_name;
            }

            set
            {
                m_name = value;
            }
        }

        public Sprite Icon
        {
            get
            {
                return m_icon;
            }

            set
            {
                m_icon = value;
            }
        }

        public bool IsFolder
        {
            get
            {
                return m_isFolder;
            }

            set
            {
                m_isFolder = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return m_enabled;
            }

            set
            {
                m_enabled = value;
            }
        }

        public GenericMenuElementData Parent
        {
            get
            {
                return m_root != null ? m_root.GetElementById(m_parentId) : null;
            }
        }

        public GenericMenuRootData Root
        {
            get
            {
                return m_root;
            }
            protected internal set
            {
                m_root = value;
            }
        }

        public int Id
        {
            get
            {
                if (m_id == -1)
                {
                    if (_random == null)
                        _random = new System.Random();
                    m_id = _random.Next(0, int.MaxValue);
                }
                return m_id;
            }
        }

        #endregion

        #region Other Helper Functions

        public bool IsRootElement()
        {
            return m_parentId < 0;
        }

        public virtual string GetPath()
        {
            System.Text.StringBuilder v_builder = new System.Text.StringBuilder();
            var v_currentObj = this;
            do
            {
                if (v_currentObj != null)
                {
                    //Add Folder Separator
                    if (v_builder.Length > 0)
                        v_builder.Insert(0, "/");
                    v_builder.Insert(0, v_currentObj.Name);
                    v_currentObj = v_currentObj.Parent;
                }
            }
            while (v_currentObj != null);

            return v_builder.ToString();
        }

        public GenericMenuElementData[] GetChildren()
        {
            List<GenericMenuElementData> v_children = new List<GenericMenuElementData>();
            foreach (var v_child in this)
            {
                if(v_child != null)
                    v_children.Add(v_child);
            }
            return v_children.ToArray();
        }

        #endregion

        #region Child List Functions

        public virtual GenericMenuElementData this[int i]
        {
            get
            {
                return m_root != null ? m_root.GetElementById(m_childIds[i]) : null;
            }
        }

        public int Count
        {
            get
            {
                return m_childIds.Count;
            }
        }

        public virtual void Clear()
        {
            for (int i = 0; i < m_childIds.Count; i++)
            {
                if (m_root != null)
                    m_root.RemoveElementById(m_childIds[i]);
            }
            m_childIds.Clear();
        }

        public virtual bool Add(GenericMenuElementData p_itemInfo)
        {
            if (p_itemInfo != null && m_root != null)
            {
                if (!m_childIds.Contains(p_itemInfo.Id))
                    m_childIds.Add(p_itemInfo.Id);
                p_itemInfo.m_parentId = this.Id;
                return m_root.AddAndCacheElement_Internal(p_itemInfo);
            }
            return false;
        }

        public bool RemoveAt(int p_index)
        {
            if (m_root != null && m_childIds.Count > p_index && p_index >= 0)
            {
                m_root.RemoveElementById(m_childIds[p_index]);
                return true;
            }
            return false;
        }

        public bool Remove(GenericMenuElementData p_item)
        {
            var v_index = IndexOf(p_item);
            return RemoveAt(v_index);
        }

        //Called by root
        protected internal virtual bool RemoveAt_Internal(int p_index)
        {
            if (m_childIds.Count > p_index && p_index >= 0)
            {
                m_childIds.RemoveAt(m_childIds[p_index]);
                return true;
            }
            return false;
        }

        public int IndexOf(GenericMenuElementData p_item)
        {
            return p_item != null ? m_childIds.IndexOf(p_item.Id) : -1;
        }

        public virtual bool Contains(GenericMenuElementData p_item)
        {
            return p_item != null ? m_childIds.Contains(p_item.Id) : false;
        }

        public IEnumerator<GenericMenuElementData> GetEnumerator()
        {
            if (m_childIds != null && m_root != null)
            {
                for (int i = 0; i < m_childIds.Count; ++i)
                {
                    yield return m_root.GetElementById(m_childIds[i]);
                }
            }
        }

        #endregion

        #region Internal Helper Functions

        protected internal virtual GenericMenuElementData AddItemAtSubPath_Internal(List<string> p_pathSplitted, bool p_enabled)
        {
            GenericMenuElementData v_element = null;

            if (p_pathSplitted.Count > 0)
            {
                var v_currentName = p_pathSplitted[0];
                //Dont accept empty names
                if (string.IsNullOrEmpty(v_currentName))
                {
                    return null;
                }
                //Try Find or create element using recursion interating over hierarchy
                else
                {
                    //Remove path to try finalize recursion
                    p_pathSplitted.RemoveAt(0);

                    //Create item if does not exists, or get it
                    if (p_pathSplitted.Count == 0)
                    {
                        //v_element = GetItemAtSubPath(v_currentName);
                        //Create Element if not found in hierarchy
                        if (v_element == null)
                        {
                            v_element = new GenericMenuElementData();
                            v_element.m_name = v_currentName;
                            v_element.m_isFolder = false;
                            this.Add(v_element);
                        }

                    }
                    //Check in Sub-folders (create if does not exists)
                    else
                    {
                        var v_folder = GetElementInfoAtSubPath_Internal(new List<string>() { v_currentName }, true, false);
                        if (v_folder == null)
                        {
                            v_folder = new GenericMenuElementData();
                            v_folder.m_name = v_currentName;
                            v_folder.m_isFolder = true;
                            this.Add(v_folder);
                        }
                        v_element = v_folder.AddItemAtSubPath_Internal(p_pathSplitted, p_enabled);
                    }
                }
            }
            if (v_element != null)
                v_element.Enabled = p_enabled;
            return v_element;
        }

        protected internal virtual GenericMenuElementData GetElementInfoAtSubPath_Internal(List<string> p_pathSplitted, bool p_acceptFolder, bool p_acceptFiles)
        {
            if ((!p_acceptFiles && !p_acceptFolder) ||
                p_pathSplitted == null ||
                p_pathSplitted.Count == 0)
            {
                return null;
            }

            GenericMenuElementData v_itemFound = null;
            foreach (var v_child in this)
            {
                //We found a possible child
                if (v_child.Name == p_pathSplitted[0])
                {
                    //Last Path to Search
                    if (p_pathSplitted.Count <= 1)
                    {
                        //Must be same type of searching
                        if (v_child.IsFolder == p_acceptFolder || !v_child.IsFolder == p_acceptFiles)
                        {
                            p_pathSplitted.RemoveAt(0);
                            v_itemFound = v_child;
                            break;
                        }
                    }
                    //Enter inside folder if paths need continue interaction
                    else if (v_child.IsFolder)
                    {
                        p_pathSplitted.RemoveAt(0);
                        v_itemFound = v_child.GetElementInfoAtSubPath_Internal(p_pathSplitted, p_acceptFolder, p_acceptFiles);
                        break;
                    }
                }
            }
            return v_itemFound;
        }

        #endregion
    }
}
