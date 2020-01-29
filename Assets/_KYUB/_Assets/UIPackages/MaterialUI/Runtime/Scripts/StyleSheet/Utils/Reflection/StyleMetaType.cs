using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MaterialUI.Reflection.Extensions;
using System.Reflection;
using UnityEngine.UI;

namespace MaterialUI.Reflection
{
    public sealed class StyleMetaType
    {
        #region Static Fields

        static Dictionary<System.Type, StyleMetaType> s_cache = new Dictionary<System.Type, StyleMetaType>();

        #endregion

        #region Private Variables

        readonly System.Type m_type = null;
        readonly Dictionary<string, StyleMetaPropertyInfo> m_declaredMembers = new Dictionary<string, StyleMetaPropertyInfo>();

        #endregion

        #region Properties

        public System.Type Type
        {
            get
            {
                return m_type;
            }
        }

        public Dictionary<string, StyleMetaPropertyInfo> DeclaredMembers
        {
            get
            {
                return m_declaredMembers;
            }
        }

        public StyleMetaType BaseMetaType
        {
            get
            {
                if (m_type != null)
                    return GetOrCreateStyleMetaType(m_type.Resolve().BaseType);
                return null;
            }
        }

        #endregion

        #region Constructors

        internal StyleMetaType(System.Type p_type)
        {
            m_type = p_type;

            if (m_type != null)
            {
                //Pickup members of this type
                var v_members = m_type.GetDeclaredMembers();
                foreach (var v_member in v_members)
                {
                    if (v_member is PropertyInfo || v_member is FieldInfo)
                    {
                        //Found field with  SerializeStyleProperty Attr
                        var v_attrs = v_member.GetCustomAttributes(typeof(SerializeStyleProperty), false);
                        if (v_attrs.Length > 0)
                        {
                            m_declaredMembers[v_member.Name] = new StyleMetaPropertyInfo(v_member);
                        }
                    }
                }

                //Try cache members of base the type
                GetOrCreateStyleMetaType(m_type.Resolve().BaseType);
            }
        }

        #endregion

        #region Public Member Functions

        public Dictionary<string, StyleMetaPropertyInfo> GetMembers()
        {
            var v_membersDict = new Dictionary<string, StyleMetaPropertyInfo>(m_declaredMembers);
            GetMembersNonAlloc(ref v_membersDict);

            return v_membersDict;
        }

        public void GetMembersNonAlloc(ref Dictionary<string, StyleMetaPropertyInfo> p_membersDict)
        {
            if (p_membersDict == null)
                p_membersDict = new Dictionary<string, StyleMetaPropertyInfo>();
            foreach (var pair in m_declaredMembers)
            {
                p_membersDict[pair.Key] = pair.Value;
            }
            if (m_type != null)
            {
                var v_baseMetaType = GetOrCreateStyleMetaType(m_type.Resolve().BaseType);
                if (v_baseMetaType != null)
                    v_baseMetaType.GetMembersNonAlloc(ref p_membersDict);
            }
        }

        #endregion

        #region Apply StyleData Functions

        public bool ApplyStyleDataToTarget(BaseStyleElement p_target, StyleData p_styleData)
        {
            if (p_target != null)
            {
                var v_template = p_styleData != null ? p_styleData.Asset : null;
                if (v_template != null)
                {
                    var v_excludedElementsList = new List<string>(p_target.DisabledFieldStyles);
                    v_excludedElementsList.AddRange(v_template.DisabledFieldStyles);
                    return ApplyTemplateToTarget(p_target, v_template, v_excludedElementsList);
                }
            }
            return false;
        }

        public bool ApplyTemplateToTarget(object p_target, object p_template, ICollection<string> m_excludedProperties = null)
        {
            if (m_type != null && p_target != null && p_template != null)
            {
                var v_excludedPropertiesHash = m_excludedProperties is HashSet<string>? (HashSet<string>)m_excludedProperties  : (m_excludedProperties != null? new HashSet<string>(m_excludedProperties) : new HashSet<string>());

                var v_targetType = p_target.GetType();
                var v_templateType = p_template.GetType();
                if ((m_type == v_targetType || v_targetType.IsSubclassOf(m_type)) &&
                    (m_type == v_templateType || v_templateType.IsSubclassOf(m_type)))
                {
                    var v_members = GetMembers();
                    foreach (var v_pair in v_members)
                    {
                        if (v_excludedPropertiesHash.Contains(v_pair.Key))
                            continue;
                        else
                        {
                            var v_member = v_pair.Value;

                            if (v_member.IsType<Object>())
                            {
                                if (v_member.HasGetDelegate())
                                {
                                    var v_targetValue = v_member.GetValue(p_target);
                                    var v_templateValue = v_member.GetValue(p_template);

                                    //Disable or enable Component/Behaviour/GameObject based in template
                                    StyleUtils.ApplyObjectActive(v_targetValue as Object, v_templateValue as Object);

                                    if (v_member.IsType<BaseStyleElement>())
                                    {
                                        StyleUtils.ApplyStyleElement(v_targetValue as BaseStyleElement, v_templateValue as BaseStyleElement);
                                    }
                                    //Set as special graphic field (only apply Asset of Graphic)
                                    else if (v_member.IsType<Graphic>())
                                    {
                                        StyleUtils.ApplyGraphic(v_targetValue as Graphic, v_templateValue as Graphic, v_member.CanApplyGraphicResources());
                                    }
                                }
                            }
                            //Set as normal field
                            else
                            {
                                if (v_member.HasGetDelegate() && v_member.HasSetDelegate())
                                {
                                    //Get StyleBehaviour Value
                                    var v_templateValue = v_member.GetValue(p_template);
                                    //Set Value in Target
                                    v_member.SetValue(ref p_target, v_templateValue);
                                }
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Style Resources Functions

        //Only add Color or Graphic Assets (Sprite/Font/TMP_Font/VectorImageData/Texture2D)
        public HashSet<object> CollectStyleResources(object p_target, HashSet<BaseStyleElement> p_excludedElements = null)
        {
            var v_targetAsBaseStyleElement = p_target as BaseStyleElement;
            if (p_excludedElements == null)
                p_excludedElements = new HashSet<BaseStyleElement>();

            HashSet<object> v_resourcesList = new HashSet<object>();

            if (p_target != null && (v_targetAsBaseStyleElement == null || !p_excludedElements.Contains(v_targetAsBaseStyleElement)))
            {
                //Prevent circular references adding self to ExcludeHash
                if (v_targetAsBaseStyleElement != null)
                    p_excludedElements.Add(v_targetAsBaseStyleElement);

                var v_members = GetMembers();
                foreach (var v_pair in v_members)
                {
                    var v_member = v_pair.Value;

                    if (v_member.HasGetDelegate())
                    {
                        var v_value = v_member.GetValue(p_target);

                        if (v_member.IsType<BaseStyleElement>())
                        {
                            BaseStyleElement v_element = v_value as BaseStyleElement;
                            if (v_element != null && !p_excludedElements.Contains(v_element))
                            {
                                p_excludedElements.Add(v_element);
                                //Collect resouces checking if not previous collected the resources
                                var v_internalResources = v_element.CollectStyleResources(p_excludedElements);
                                foreach (var v_resource in v_internalResources)
                                {
                                    v_resourcesList.Add(v_element);
                                }
                            }
                        }
                        if (v_member.IsType<Graphic>())
                        {
                            object v_resourceAsset = StyleUtils.GetStyleResource(v_value as Graphic);

                            if (v_resourceAsset is VectorImageData ||
                                ((v_resourceAsset is Object) && ((Object)v_resourceAsset) != null)
                               )
                            {
                                v_resourcesList.Add(v_resourceAsset);
                            }
                        }
                        else if (v_member.IsType<Color>() || v_member.IsType<Color32>())
                        {
                            Color32 v_resourceColor = v_value is Color ? (Color32)((Color)v_value) : (v_value is Color32 ? (Color32)v_value : default(Color32));
                            v_resourceColor = StyleUtils.GetStyleColor(v_resourceColor);
                            v_resourcesList.Add(v_resourceColor);
                        }
                    }
                }
            }

            return v_resourcesList;
        }

        public bool TryReplaceStyleResource(object p_target, object p_oldResource, object p_newResource)
        {
            var v_sucess = false;
            if (p_target != null)
            {
                var v_members = GetMembers();
                foreach (var v_pair in v_members)
                {
                    var v_member = v_pair.Value;

                    v_sucess = TryReplaceMemberResouce(v_member, p_target, p_oldResource, p_newResource) || v_sucess;
                }
            }
            return v_sucess;
        }

        public bool TryReplaceStyleResources(object p_target, IList<KeyValuePair<object, object>> p_oldNewResoucesList)
        {
            var v_sucess = false;
            if (p_target != null && p_oldNewResoucesList != null)
            {
                var v_members = new List<StyleMetaPropertyInfo>(GetMembers().Values);
                foreach (var v_newOldResource in p_oldNewResoucesList)
                {
                    //try replace resource (one time per member in case of sucess)
                    for (int i = 0; i < v_members.Count; i++)
                    {
                        var v_member = v_members[i];
                        if (TryReplaceMemberResouce(v_member, p_target, v_newOldResource.Key, v_newOldResource.Value))
                        {
                            v_sucess = true;
                            //Prevent to check this member again (when trying to replace the second resource)
                            v_members.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            return v_sucess;
        }

        private bool TryReplaceMemberResouce(StyleMetaPropertyInfo p_memberInfo, object p_target, object p_oldResource, object p_newResource)
        {
            if (p_target != null && p_oldResource != p_newResource)
            {
                if (p_memberInfo.HasGetDelegate())
                {
                    var v_value = p_memberInfo.GetValue(p_target);

                    if (p_memberInfo.IsType<Graphic>())
                    {
                        return StyleUtils.TryReplaceStyleResource(v_value as Graphic, p_oldResource, p_newResource);
                    }
                    else if (p_memberInfo.IsType<Color>() || p_memberInfo.IsType<Color32>())
                    {
                        if (p_memberInfo.HasSetDelegate())
                        {
                            //Convert all colors to Color32
                            var v_color = v_value is Color ? (Color32)((Color)v_value) : (v_value is Color32 ? (Color32)v_value : default(Color32));
                            var v_oldColor = p_oldResource is Color ? (Color32)((Color)p_oldResource) : (p_oldResource is Color32 ? (Color32)p_oldResource : default(Color32));
                            var v_newColor = p_newResource is Color ? (Color32)((Color)p_newResource) : (p_newResource is Color32 ? (Color32)p_newResource : default(Color32));
                            var v_sucess = StyleUtils.ReplaceStyleColor(ref v_color, v_oldColor, v_newColor);

                            //Reflection cannot use implicit conversions
                            if (p_memberInfo.IsType<Color32>())
                                p_memberInfo.SetValue(ref p_target, v_color);
                            else
                                p_memberInfo.SetValue(ref p_target, (Color)v_color);

                            return v_sucess;
                        }
                    }
                }
                return false;
            }
            return true;
        }

        #endregion

        #region Public Static Functions

        public static StyleMetaType GetOrCreateStyleMetaType<T>()
        {
            return GetOrCreateStyleMetaType(typeof(T));
        }

        public static StyleMetaType GetOrCreateStyleMetaType(System.Type p_type)
        {
            if (p_type != null)
            {
                StyleMetaType v_metaType = null;
                //Not found, create new one
                if (!s_cache.TryGetValue(p_type, out v_metaType) || v_metaType == null)
                {
                    v_metaType = new StyleMetaType(p_type);
                    s_cache[p_type] = v_metaType;
                }

                return v_metaType;
            }
            return null;
        }

        #endregion
    }

    public class StyleMetaPropertyInfo
    {
        #region Private Variables

        MemberInfo m_memberInfo = null;

        SerializeStyleProperty _styleProperty = null;
        System.Type _memberType = null;
        MemberGetter<object, object> _delegateForGet = null;
        MemberSetter<object, object> _delegateForSet = null;
        #endregion

        #region Properties

        public MemberInfo MemberInfo
        {
            get
            {
                return m_memberInfo;
            }
        }

        public SerializeStyleProperty StyleProperty
        {
            get
            {
                return _styleProperty;
            }
        }

        public System.Type MemberType
        {
            get
            {
                return _memberType;
            }
        }

        #endregion

        #region Constructors

        public StyleMetaPropertyInfo(MemberInfo p_memberInfo)
        {
            Init(p_memberInfo);
        }

        #endregion

        #region Public Functions

        public bool CanApplyGraphicResources()
        {
            return _styleProperty != null && _styleProperty.CanApplyGraphicResources;
        }

        public bool IsType<T>()
        {
            return IsType(typeof(T));
        }

        public bool IsType(System.Type p_typeToCheck)
        {
            return _memberType == p_typeToCheck || _memberType.IsSubclassOf(p_typeToCheck);
        }

        public object GetValue(object p_target)
        {
            if (_delegateForGet != null)
                return _delegateForGet.Invoke(p_target);

            return null;
        }

        public void SetValue(ref object p_target, object p_value)
        {
            if (_delegateForSet != null)
                _delegateForSet.Invoke(ref p_target, p_value);
        }

        public bool HasGetDelegate()
        {
            return _delegateForGet != null;
        }

        public bool HasSetDelegate()
        {
            return _delegateForSet != null;
        }

        #endregion

        #region Internal Helper Functions

        private void Init(MemberInfo p_memberInfo)
        {
            m_memberInfo = p_memberInfo;
            if (m_memberInfo != null)
            {
                _styleProperty = MaterialPortableReflection.GetAttribute<SerializeStyleProperty>(m_memberInfo);
                _delegateForGet = m_memberInfo.DelegateForGet();
                _delegateForSet = m_memberInfo.DelegateForSet();
                _memberType = GetMemberType();
            }
            else
            {
                _styleProperty = null;
                _delegateForGet = null;
                _delegateForSet = null;
                _memberType = null;
            }
        }

        private System.Type GetMemberType()
        {
            var v_field = m_memberInfo as FieldInfo;
            var v_property = m_memberInfo as PropertyInfo;

            if (v_field != null)
                return v_field.FieldType;
            else if (v_property != null)
                return v_property.PropertyType;

            return typeof(object);
        }

        #endregion
    }
}
