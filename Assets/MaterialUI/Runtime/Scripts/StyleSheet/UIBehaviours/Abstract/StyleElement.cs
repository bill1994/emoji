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

        public bool TryGetExtraCastedStyleProperty(string p_name, out T p_styleProperty)
        {
            StyleProperty v_property = null;
            var v_sucess = ExtraStylePropertiesMap.TryGetValue(p_name, out v_property);
            p_styleProperty = v_property as T;

            return v_sucess;
        }

        protected virtual void Optimize()
        {
            if (_extraStylePropertiesMap == null)
                _extraStylePropertiesMap = new Dictionary<string, StyleProperty>();
            else
                _extraStylePropertiesMap.Clear();

            //Add Style Properties
            foreach (var v_property in ExtraStylePropertiesInternalList)
            {
                var v_styleProp = v_property as T;
                if (v_styleProp != null && !string.IsNullOrEmpty(v_styleProp.Name) && !_extraStylePropertiesMap.ContainsKey(v_styleProp.Name))
                    _extraStylePropertiesMap.Add(v_styleProp.Name, v_styleProp);
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

        protected internal bool RegisterToStyleGroup(bool p_force = false)
        {
            if (SupportStyleGroup)
            {
                if (m_styleGroup == null || p_force)
                {
                    var v_styleGroup = GetComponentInParent<CanvasStyleGroup>();
                    _styleData = null;
                    if (m_styleGroup != null)
                        UnregisterFromStyleGroup();
                    m_styleGroup = v_styleGroup;
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
                var v_sucess = m_styleGroup.UnregisterStyleBehaviour(this);
                m_styleGroup = null;
                return v_sucess;
            }
            return false;
        }
        #endregion

        #region Style Property Helper Functions

        public bool TryGetExtraStyleProperty(string p_name, out StyleProperty p_styleProperty)
        {
            return ExtraStylePropertiesMap.TryGetValue(p_name, out p_styleProperty);
        }

        #endregion

        #region Public StyleData Functions

        public virtual void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0.25f);
        }

        public virtual StyleData GetStyleDataTemplate(bool p_force = false)
        {
            if (m_styleGroup != null && 
                (_styleData == null || p_force || 
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
            var v_styleData = GetStyleDataTemplate();
            return m_supportStyleGroup && IsSupportedStyleData(v_styleData) ? OnLoadStyles(v_styleData) : false;
        }

        public System.Type GetSupportedStyleAssetType()
        {
            var v_type = GetSupportedStyleAssetType_Internal();
            var v_validationType = typeof(BaseStyleElement);
            if (v_type != null && (v_type == v_validationType || v_type.IsSubclassOf(v_validationType)))
                return v_type;
            else
                return v_validationType;
        }

        #endregion

        #region Internal Apply StyleData Functions

        protected virtual void SetStylePropertyColorsActive_Internal(bool p_canAnimate, float p_animationDuration)
        {
            Kyub.Performance.SustainedPerformanceManager.Refresh(this);
            Dictionary<string, StyleProperty>[] v_styleMaps = new Dictionary<string, StyleProperty>[] { ExtraStylePropertiesMap };

            for (int i = 0; i < v_styleMaps.Length; i++)
            {
                //Apply Style Properties
                foreach (var v_pair in v_styleMaps[i])
                {
                    var v_styleProperty = v_pair.Value;
                    if (v_styleProperty != null)
                        v_styleProperty.Tween(this, p_canAnimate, p_animationDuration);
                }
            }
        }

        protected virtual System.Type GetSupportedStyleAssetType_Internal()
        {
            return GetType();
        }

        protected internal bool IsSupportedStyleData(StyleData p_styleData)
        {
            if (p_styleData != null && p_styleData.Asset != null)
            {
                return IsSupportedStyleElement(p_styleData.Asset);
            }
            return false;
        }

        protected internal bool IsSupportedStyleElement(BaseStyleElement p_styleElement)
        {
            if (p_styleElement != null)
            {
                var v_supportedType = GetSupportedStyleAssetType();
                var v_templateType = p_styleElement.GetType();
                if (v_supportedType == v_templateType || v_templateType.IsSubclassOf(v_supportedType))
                {
                    return true;
                }
            }
            return false;
        }

        protected virtual bool OnLoadStyles(StyleData p_styleData)
        {
            return LoadGenericStyles(p_styleData);
        }

        protected virtual bool LoadGenericStyles(StyleData p_styleData)
        {
            //Cache StyleData Asset
            _styleData = p_styleData;

            var v_template = p_styleData != null ? p_styleData.Asset : null;
            if (v_template != null)
            {
                Dictionary<string, StyleProperty>[] v_otherStyleMaps = new Dictionary<string, StyleProperty>[] { v_template.ExtraStylePropertiesMap };
                Dictionary<string, StyleProperty>[] v_selfStyleMaps = new Dictionary<string, StyleProperty>[] { ExtraStylePropertiesMap };

                for (int i = 0; i < v_otherStyleMaps.Length; i++)
                {
                    //Apply Style Properties
                    foreach (var v_pair in v_otherStyleMaps[i])
                    {
                        StyleProperty v_selfStyleProperty = null;
                        v_selfStyleMaps[i].TryGetValue(v_pair.Key, out v_selfStyleProperty);
                        if (v_selfStyleProperty != null && v_pair.Value != null)
                        {
                            var v_otherStyleProperty = v_pair.Value;
                            v_selfStyleProperty.LoadStyles(v_otherStyleProperty);

                            
                            //Disable/Enable GameObject based in Template (prevent apply if selfStyleProperty.Target is self transform)
                            if (v_selfStyleProperty.Target != this.transform && v_selfStyleProperty.Target != null && v_otherStyleProperty.Target != null)
                                StyleUtils.ApplyObjectActive(v_selfStyleProperty.Target, v_otherStyleProperty.Target);

                            //Apply Graphic if Supported
                            if (v_selfStyleProperty.UseStyleGraphic)
                                StyleUtils.ApplyGraphic(v_selfStyleProperty.Target, v_otherStyleProperty.Target);
                        }
                    }
                }

                var v_metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
                v_metaType.ApplyStyleDataToTarget(this, p_styleData);

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
        public virtual HashSet<object> CollectStyleResources(HashSet<BaseStyleElement> p_excludedElements = null)
        {
            if (p_excludedElements == null)
                p_excludedElements = new HashSet<BaseStyleElement>();

            //Get Self Resources
            var v_metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            var v_mainResources = v_metaType.CollectStyleResources(this, p_excludedElements);

            //Get StyleProperties Dicts
            Dictionary<string, StyleProperty>[] v_styleMaps = new Dictionary<string, StyleProperty>[] { ExtraStylePropertiesMap };

            for (int i = 0; i < v_styleMaps.Length; i++)
            {
                //Interate over all style properties
                foreach (var v_pair in v_styleMaps[i])
                {
                    var v_styleProperty = v_pair.Value;

                    if (v_styleProperty != null)
                    {
                        var v_propResources = v_styleProperty.CollectStyleResources(p_excludedElements);
                        //Add all resources in MainResources
                        foreach (var v_resource in v_propResources)
                        {
                            v_mainResources.Add(v_resource);
                        }
                    }
                }
            }

            return v_mainResources;
        }

        public bool TryReplaceStyleResource(object p_oldResource, object p_newResource)
        {
            return TryReplaceStyleResources(new List<KeyValuePair<object, object>>() { new KeyValuePair<object, object>(p_oldResource, p_newResource) });
        }

        public virtual bool TryReplaceStyleResources(IList<KeyValuePair<object, object>> p_oldNewResoucesList)
        {
            var v_metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            var v_sucess = v_metaType.TryReplaceStyleResources(this, p_oldNewResoucesList);

            //Get StyleProperties Dicts
            List<StyleProperty> v_styleProperties = new List<StyleProperty>(ExtraStylePropertiesMap.Values);

            //Interate over all style properties
            for (int i = 0; i < v_styleProperties.Count; i++)
            {
                var v_styleProperty = v_styleProperties[i];
                //Replace Resource in StyleProperty
                if (v_styleProperty != null)
                {
                    if (v_styleProperty.TryReplaceStyleResources(p_oldNewResoucesList))
                    {
                        v_sucess = true;
                        //Prevent to check this member again (when trying to replace the second resource)
                        v_styleProperties.RemoveAt(i);
                        i--;
                    }
                }
            }

#if UNITY_EDITOR
            if(v_sucess)
                UnityEditor.EditorUtility.SetDirty(this);
#endif

            return v_sucess;
        }

        #endregion

        #region Style Exclude Fields

        public void SetFieldStyleActive(string p_fieldName, bool p_active)
        {
            if (!p_active)
            {
                if (!m_disabledFieldStyles.Contains(p_fieldName))
                    m_disabledFieldStyles.Add(p_fieldName);
            }
            else
            {
                var v_index = m_disabledFieldStyles.IndexOf(p_fieldName);
                if (v_index >= 0)
                    m_disabledFieldStyles.RemoveAt(v_index);
            }
        }

        public bool GetFieldStyleActive(string p_fieldName)
        {
            return !m_disabledFieldStyles.Contains(p_fieldName);
        }

        #endregion
    }

    #region Helper Classes

    public class EmptyStyleProperty : StyleProperty
    {
        public override void Tween(BaseStyleElement p_sender, bool p_canAnimate, float p_animationDuration)
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

        public abstract void Tween(BaseStyleElement p_sender, bool p_canAnimate, float p_animationDuration);

        public virtual bool LoadStyles(StyleProperty p_styleProperty)
        {
            var v_metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            return v_metaType.ApplyTemplateToTarget(this, p_styleProperty);
        }

        #endregion

        #region Style Resources Functions

        //Only add Color or Graphic Assets (Sprite/Font/TMP_Font/VectorImageData/ Texture2D)
        public virtual HashSet<object> CollectStyleResources(HashSet<BaseStyleElement> p_excludedElements = null)
        {
            var v_metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            
            return v_metaType.CollectStyleResources(this, p_excludedElements);
        }

        public bool TryReplaceStyleResource(object p_oldResource, object p_newResource)
        {
            return TryReplaceStyleResources(new List<KeyValuePair<object, object>>() { new KeyValuePair<object, object>(p_oldResource, p_newResource) });
        }

        public virtual bool TryReplaceStyleResources(IList<KeyValuePair<object, object>> p_oldNewResoucesList)
        {
            var v_metaType = StyleMetaType.GetOrCreateStyleMetaType(GetType());
            return v_metaType.TryReplaceStyleResources(this, p_oldNewResoucesList);
        }

        #endregion
    }

    #endregion

}
