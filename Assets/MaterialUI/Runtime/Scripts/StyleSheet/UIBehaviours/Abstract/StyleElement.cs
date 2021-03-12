using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MaterialUI.Reflection.Extensions;
using MaterialUI.Reflection;

namespace MaterialUI
{
    public abstract class SelectableStyleElement<T> : StyleElement<T>, ISelectHandler where T : StyleProperty, new()
    {
        #region Unity Functions

        public virtual void OnSelect(BaseEventData eventData)
        {
            SnapTo();
        }

        #endregion

        #region Public Functions

        public virtual void Select()
        {
            if (EventSystem.current == null || EventSystem.current.alreadySelecting)
                return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public virtual void SnapTo()
        {
#if UI_COMMONS_DEFINED
            if (enabled && gameObject.activeInHierarchy)
            {
                var nestedRect = GetComponentInParent<Kyub.UI.NestedScrollRect>();
                if (nestedRect != null)
                    nestedRect.SnapTo(this.transform as RectTransform);
            }
#endif
        }

        #endregion
    }

    public abstract class StyleElement<T> : BaseStyleElement where T : StyleProperty, new()
    {
        #region Private Variables

        [SerializeField]
        protected List<T> m_extraStyleProperties = new List<T>();

        Dictionary<string, StyleProperty> _extraStylePropertiesMap = null;

        #endregion

        #region Properties

        public override Dictionary<string, StyleProperty> ExtraStylePropertiesMap
        {
            get
            {
                if (_extraStylePropertiesMap == null)
                    Optimize();
                return _extraStylePropertiesMap;
            }
        }

        protected internal override IList ExtraStylePropertiesInternalList
        {
            get
            {
                return m_extraStyleProperties;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            TweenManager.ForceInitialize();
            base.Awake();
        }

        #endregion

        #region Helper Functions Functions

        public bool TryGetExtraCastedStyleProperty(string name, out T styleProperty)
        {
            StyleProperty property = null;
            var sucess = ExtraStylePropertiesMap.TryGetValue(name, out property);
            styleProperty = property as T;

            return sucess;
        }

        protected virtual void Optimize()
        {
            if (_extraStylePropertiesMap == null)
                _extraStylePropertiesMap = new Dictionary<string, StyleProperty>();
            else
                _extraStylePropertiesMap.Clear();

            //Add Style Properties
            foreach (var property in ExtraStylePropertiesInternalList)
            {
                var styleProp = property as T;
                if (styleProp != null && !string.IsNullOrEmpty(styleProp.Name) && !_extraStylePropertiesMap.ContainsKey(styleProp.Name))
                    _extraStylePropertiesMap.Add(styleProp.Name, styleProp);
            }
        }

#if UNITY_EDITOR

        protected override void OnValidateDelayed()
        {
            base.OnValidateDelayed();
            Optimize();
        }
#endif

#endregion

    }

    public abstract class BaseStyleElement : UIBehaviour
    {
        #region Private Variables

        [SerializeField]
        protected bool m_supportStyleGroup = true;
        [SerializeField]
        protected string m_styleDataName = "";

        [SerializeField, HideInInspector]
        protected CanvasStyleGroup m_styleGroup = null;

        [System.NonSerialized]
        protected StyleData _styleData = new StyleData();

        [SerializeField, Tooltip("Fields that must be excluded from StyleGroup (useful to disable SerializedStyleProperty of a field in a specific StyleElement)")]
        protected List<string> m_disabledFieldStyles = new List<string>();

        #endregion

        #region Public Properties

        public abstract Dictionary<string, StyleProperty> ExtraStylePropertiesMap
        {
            get;
        }

        protected internal abstract IList ExtraStylePropertiesInternalList
        {
            get;
        }

        public CanvasStyleGroup StyleGroup
        {
            get
            {
                if (m_styleGroup != null && !SupportStyleGroup)
                    UnregisterFromStyleGroup();
                return m_styleGroup;
            }
        }

        public virtual bool SupportStyleGroup
        {
            get
            {
                return m_supportStyleGroup;
            }

            set
            {
                if (m_supportStyleGroup == value)
                    return;
                m_supportStyleGroup = value;
                if (gameObject.activeInHierarchy && enabled)
                    RegisterToStyleGroup();
            }
        }

        public StyleData StyleData
        {
            get
            {
                return _styleData;
            }
        }

        public string StyleDataName
        {
            get
            {
                return m_styleDataName;
            }

            set
            {
                if (m_styleDataName == value)
                    return;
                m_styleDataName = value;
                _styleData = null;
            }
        }

        protected internal List<string> DisabledFieldStyles
        {
            get
            {
                if (m_disabledFieldStyles == null)
                    m_disabledFieldStyles = new List<string>();
                return m_disabledFieldStyles;
            }

            set
            {
                m_disabledFieldStyles = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            RegisterToStyleGroup();
            RefreshVisualStyles(false);
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnregisterFromStyleGroup();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            RegisterToStyleGroup();
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            //Register Validate Delayed
            UnityEditor.EditorApplication.update -= EditorOnValidateDelayed;
            if (this.gameObject.activeInHierarchy)
            {
                UnityEditor.EditorApplication.update += EditorOnValidateDelayed;
            }
        }

        protected virtual void OnValidateDelayed()
        {
            ForceRegisterToStyleGroup();
            RefreshVisualStyles(false);
        }

        [System.NonSerialized] bool _isFirstOnValidate = true;
        //Internal functions to manage validate invoke
        void EditorOnValidateDelayed()
        {
            //Unregister Validate Delayed
            UnityEditor.EditorApplication.update -= EditorOnValidateDelayed;
            if (this != null)
            {
                OnValidateDelayed();
                //Force Layout Rebuild when loading scene in editor for the first time
                if (_isFirstOnValidate)
                {
                    _isFirstOnValidate = false;
                    if(!Application.isPlaying)
                        UnityEditor.EditorApplication.update += EditorOnValidateDelayed;
                }
            }
        }

#endif

        #endregion

        #region Style Group Helper Functions

        public bool ForceRegisterToStyleGroup()
        {
            return RegisterToStyleGroup(true);
        }

        protected internal bool RegisterToStyleGroup(bool force = false)
        {
            if (SupportStyleGroup)
            {
                if (m_styleGroup == null || force)
                {
                    var styleGroup = GetComponentInParent<CanvasStyleGroup>();
                    _styleData = null;
                    if (m_styleGroup != null)
                        UnregisterFromStyleGroup();
                    m_styleGroup = styleGroup;
                }

                if (m_styleGroup != null)
                {
                    return m_styleGroup.RegisterStyleBehaviour(this);
                }
            }
            return false;
        }

        protected internal bool UnregisterFromStyleGroup()
        {
            if (m_styleGroup != null)
            {
                _styleData = null;
                var sucess = m_styleGroup.UnregisterStyleBehaviour(this);
                m_styleGroup = null;
                return sucess;
            }
            return false;
        }
        #endregion

        #region Style Property Helper Functions

        public bool TryGetExtraStyleProperty(string name, out StyleProperty styleProperty)
        {
            return ExtraStylePropertiesMap.TryGetValue(name, out styleProperty);
        }

        #endregion

        #region Public StyleData Functions

        public virtual void RefreshVisualStyles(bool canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(canAnimate, 0.25f);
        }

        public virtual StyleData GetStyleDataTemplate(bool force = false)
        {
            if (m_styleGroup != null && 
                (_styleData == null || force || 
                (!string.IsNullOrEmpty(m_styleDataName) && _styleData.Name != m_styleDataName)))
            {
                if (m_supportStyleGroup)
                {
                    if (!string.IsNullOrEmpty(m_styleDataName) && m_styleGroup.TryGetStyleData(m_styleDataName, GetSupportedStyleAssetType(), out _styleData))
                    {
                        //Clone StyleData to prevent references to StyleSheetAsset
                        _styleData = _styleData != null ? new StyleData(_styleData.Name, _styleData.Asset) : new StyleData();
                    }
                    else
                    {
                        //Prevent try find null asset again (we only recalculate if StyleData is Null)
                        _styleData = new StyleData();
                    }
                }
                else
                    _styleData = null;

                return _styleData;
            }

            if (!m_supportStyleGroup || m_styleGroup == null)
                _styleData = null;

            return _styleData;
        }

        public bool LoadStyles()
        {
            var styleData = GetStyleDataTemplate();
            return m_supportStyleGroup && IsSupportedStyleData(styleData) ? OnLoadStyles(styleData) : false;
        }

        public System.Type GetSupportedStyleAssetType()
        {
            var type = GetSupportedStyleAssetType_Internal();
            var validationType = typeof(BaseStyleElement);
            if (type != null && (type == validationType || type.IsSubclassOf(validationType)))
                return type;
            else
                return validationType;
        }

        #endregion

        #region Internal Apply StyleData Functions

        protected virtual void SetStylePropertyColorsActive_Internal(bool canAnimate, float animationDuration)
        {
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            Dictionary<string, StyleProperty>[] styleMaps = new Dictionary<string, StyleProperty>[] { ExtraStylePropertiesMap };

            for (int i = 0; i < styleMaps.Length; i++)
            {
                //Apply Style Properties
                foreach (var pair in styleMaps[i])
                {
                    var styleProperty = pair.Value;
                    if (styleProperty != null)
                        styleProperty.Tween(this, canAnimate, animationDuration);
                }
            }
        }

        protected virtual System.Type GetSupportedStyleAssetType_Internal()
        {
            return GetType();
        }

        protected internal bool IsSupportedStyleData(StyleData styleData)
        {
            if (styleData != null && styleData.Asset != null)
            {
                return IsSupportedStyleElement(styleData.Asset);
            }
            return false;
        }

        protected internal bool IsSupportedStyleElement(BaseStyleElement styleElement)
        {
            if (styleElement != null)
            {
                var supportedType = GetSupportedStyleAssetType();
                var templateType = styleElement.GetType();
                if (supportedType == templateType || templateType.IsSubclassOf(supportedType))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual bool OnLoadStyles(StyleData styleData)
        {
            return LoadGenericStyles(styleData);
        }

        protected virtual bool LoadGenericStyles(StyleData styleData)
        {
            //Cache StyleData Asset
            _styleData = styleData;

            var template = styleData != null ? styleData.Asset : null;
            if (template != null)
            {
                Dictionary<string, StyleProperty>[] otherStyleMaps = new Dictionary<string, StyleProperty>[] { template.ExtraStylePropertiesMap };
                Dictionary<string, StyleProperty>[] selfStyleMaps = new Dictionary<string, StyleProperty>[] { ExtraStylePropertiesMap };

                for (int i = 0; i < otherStyleMaps.Length; i++)
                {
                    //Apply Style Properties
                    foreach (var pair in otherStyleMaps[i])
                    {
                        StyleProperty selfStyleProperty = null;
                        selfStyleMaps[i].TryGetValue(pair.Key, out selfStyleProperty);
                        if (selfStyleProperty != null && pair.Value != null)
                        {
                            var otherStyleProperty = pair.Value;
                            selfStyleProperty.LoadStyles(otherStyleProperty);

                            
                            //Disable/Enable GameObject based in Template (prevent apply if selfStyleProperty.Target is self transform)
                            if (selfStyleProperty.Target != this.transform && selfStyleProperty.Target != null && otherStyleProperty.Target != null)
                                StyleUtils.ApplyObjectActive(selfStyleProperty.Target, otherStyleProperty.Target);

                            //Apply Graphic if Supported
                            if (selfStyleProperty.UseStyleGraphic)
                                StyleUtils.ApplyGraphic(selfStyleProperty.Target, otherStyleProperty.Target);
                        }
                    }
                }

                var metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
                metaType.ApplyStyleDataToTarget(this, styleData);

#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif

                return true;
            }
            return false;
        }

        #endregion

        #region Style Resouces Functions

        //Only add Color or Graphic Assets (Sprite/Font/TMP_Font/VectorImageData/ Texture2D)
        public virtual HashSet<object> CollectStyleResources(HashSet<BaseStyleElement> excludedElements = null)
        {
            if (excludedElements == null)
                excludedElements = new HashSet<BaseStyleElement>();

            //Get Self Resources
            var metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            var mainResources = metaType.CollectStyleResources(this, excludedElements);

            //Get StyleProperties Dicts
            Dictionary<string, StyleProperty>[] styleMaps = new Dictionary<string, StyleProperty>[] { ExtraStylePropertiesMap };

            for (int i = 0; i < styleMaps.Length; i++)
            {
                //Interate over all style properties
                foreach (var pair in styleMaps[i])
                {
                    var styleProperty = pair.Value;

                    if (styleProperty != null)
                    {
                        var propResources = styleProperty.CollectStyleResources(excludedElements);
                        //Add all resources in MainResources
                        foreach (var resource in propResources)
                        {
                            mainResources.Add(resource);
                        }
                    }
                }
            }

            return mainResources;
        }

        public bool TryReplaceStyleResource(object oldResource, object newResource)
        {
            return TryReplaceStyleResources(new List<KeyValuePair<object, object>>() { new KeyValuePair<object, object>(oldResource, newResource) });
        }

        public virtual bool TryReplaceStyleResources(IList<KeyValuePair<object, object>> oldNewResoucesList)
        {
            var metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            var sucess = metaType.TryReplaceStyleResources(this, oldNewResoucesList);

            //Get StyleProperties Dicts
            List<StyleProperty> styleProperties = new List<StyleProperty>(ExtraStylePropertiesMap.Values);

            //Interate over all style properties
            for (int i = 0; i < styleProperties.Count; i++)
            {
                var styleProperty = styleProperties[i];
                //Replace Resource in StyleProperty
                if (styleProperty != null)
                {
                    if (styleProperty.TryReplaceStyleResources(oldNewResoucesList))
                    {
                        sucess = true;
                        //Prevent to check this member again (when trying to replace the second resource)
                        styleProperties.RemoveAt(i);
                        i--;
                    }
                }
            }

#if UNITY_EDITOR
            if(sucess)
                UnityEditor.EditorUtility.SetDirty(this);
#endif

            return sucess;
        }

        #endregion

        #region Style Exclude Fields

        public void SetFieldStyleActive(string fieldName, bool active)
        {
            if (!active)
            {
                if (!m_disabledFieldStyles.Contains(fieldName))
                    m_disabledFieldStyles.Add(fieldName);
            }
            else
            {
                var index = m_disabledFieldStyles.IndexOf(fieldName);
                if (index >= 0)
                    m_disabledFieldStyles.RemoveAt(index);
            }
        }

        public bool GetFieldStyleActive(string fieldName)
        {
            return !m_disabledFieldStyles.Contains(fieldName);
        }

        #endregion
    }

    #region Helper Classes

    public class EmptyStyleProperty : StyleProperty
    {
        public override void Tween(BaseStyleElement sender, bool canAnimate, float animationDuration)
        {
            TweenManager.EndTween(_tweenId);
        }
    }

    [System.Serializable]
    public abstract class StyleProperty
    {
        #region Private Variables

        [SerializeField]
        protected string m_name = "";
        [SerializeField]
        protected Transform m_target = null;
        [SerializeField, Tooltip("This property will tell if must clone Font/Sprite/VectorImage to target graphic when applying style")]
        protected bool m_useStyleGraphic = false;

        protected int _tweenId;

        #endregion

        #region Public Properties

        public string Name
        {
            get
            {
                return m_name;
            }
            internal set
            {
                m_name = value;
            }
        }

        public Transform Target
        {
            get
            {
                return m_target;
            }

            set
            {
                m_target = value;
            }
        }

        public bool UseStyleGraphic
        {
            get
            {
                return m_useStyleGraphic;
            }

            set
            {
                m_useStyleGraphic = value;
            }
        }

        #endregion

        #region Helper Functions

        public bool HasGraphic()
        {
            return GetTarget<Graphic>() != null;
        }

        public T GetTarget<T>() where T : Component
        {
            if (m_target is T)
                return m_target as T;

            return m_target != null ? m_target.GetComponent<T>() : null;
        }

        public virtual void EndTween()
        {
            TweenManager.EndTween(_tweenId);
        }

        public abstract void Tween(BaseStyleElement sender, bool canAnimate, float animationDuration);

        public virtual bool LoadStyles(StyleProperty styleProperty)
        {
            var metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            return metaType.ApplyTemplateToTarget(this, styleProperty);
        }

        #endregion

        #region Style Resources Functions

        //Only add Color or Graphic Assets (Sprite/Font/TMP_Font/VectorImageData/ Texture2D)
        public virtual HashSet<object> CollectStyleResources(HashSet<BaseStyleElement> excludedElements = null)
        {
            var metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            
            return metaType.CollectStyleResources(this, excludedElements);
        }

        public bool TryReplaceStyleResource(object oldResource, object newResource)
        {
            return TryReplaceStyleResources(new List<KeyValuePair<object, object>>() { new KeyValuePair<object, object>(oldResource, newResource) });
        }

        public virtual bool TryReplaceStyleResources(IList<KeyValuePair<object, object>> oldNewResoucesList)
        {
            var metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            return metaType.TryReplaceStyleResources(this, oldNewResoucesList);
        }

        #endregion
    }

    #endregion

}
