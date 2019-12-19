using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UI;
using UnityEditor.AnimatedValues;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BaseStyleElement), true)]
    public class BaseStyleElementEditor : MaterialBaseEditor
    {
        #region Fields

        string[] _excludingProperties = new string[] { "m_Script", "m_extraStyleProperties", "m_styleGroup", "m_styleDataName", "m_supportStyleGroup", "m_disabledFieldStyles" };

        string[] _allValidStyleBehaviours = null;
        StyleSheetAsset _cachedAsset = null;

        SerializedProperty m_script = null;
        SerializedProperty m_styleGroup = null;
        SerializedProperty m_supportStyleGroup = null;
        SerializedProperty m_styleDataName = null;
        ReorderableList m_extraStylePropertiesList = null;

        bool _cachedStyleControledByStyleGroup = false;


        bool m_ShowStyles = true;
        private string m_StylesPrefKey;

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            m_script = serializedObject.FindProperty("m_Script");
            OnBaseEnable();
        }
        protected virtual void OnDisable()
        {
            OnBaseDisable();
        }

        protected override void OnBaseEnable()
        {
            base.OnBaseEnable();

            InitOtherStyleDataPropertyStyles();
            InitExtraPropertyStyles();
        }

        public override void OnInspectorGUI()
        {
            //Script Field
            var v_oldGuiEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_script);
            GUI.enabled = v_oldGuiEnabled;

            //Other Properties
            serializedObject.Update();
            LayoutStyle_DrawPropertiesExcluding(serializedObject, _excludingProperties);
            EditorGUILayout.Space();
            DrawStyleGUIFolder();
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Main Draw Functions

        public virtual void DrawStyleGUIFolder()
        {
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            m_ShowStyles = EditorGUILayout.Foldout(m_ShowStyles, "Styles");
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(m_StylesPrefKey, m_ShowStyles);
            }

            if (m_ShowStyles)
            {
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    DrawStyleGUIContainer();
                }
            }
        }

        protected virtual void DrawStyleGUIContainer()
        {
            DrawOtherStyleDataProperties();
            EditorGUILayout.Space();
            DrawExtraStylePropertiesList();

        }

        #endregion

        #region Draw Other StyleData Properties

        protected virtual void InitOtherStyleDataPropertyStyles()
        {
            m_styleGroup = serializedObject.FindProperty("m_styleGroup");
            m_supportStyleGroup = serializedObject.FindProperty("m_supportStyleGroup");
            m_styleDataName = serializedObject.FindProperty("m_styleDataName");
            
            //Styles Folder
            string v_prefKey = GetType().Name;
            m_StylesPrefKey = v_prefKey + "_Show_Styles";
            m_ShowStyles = EditorPrefs.GetBool(m_StylesPrefKey, true);

            //Reset Important Properties
            _allValidStyleBehaviours = null;
            _cachedAsset = null;
            StyleControledByStyleGroup(true);
        }

        protected bool StyleControledByStyleGroup(bool p_recalculate = false)
        {
            if (p_recalculate)
            {
                if (m_supportStyleGroup.boolValue)
                {
                    serializedObject.ApplyModifiedProperties();
                    var v_force = false;
                    if (m_styleGroup.objectReferenceValue == null)
                    {
                        v_force = true;
                        foreach (var v_target in targets)
                        {
                            var v_styleBehaviour = v_target as BaseStyleElement;
                            if (v_styleBehaviour != null)
                            {
                                v_styleBehaviour.ForceRegisterToStyleGroup();
                                v_styleBehaviour.LoadStyles();
                                v_force = v_force && v_styleBehaviour.StyleGroup && v_styleBehaviour.StyleData.Asset != null;
                            }
                            else
                                v_force = false;
                        }
                    }
                    serializedObject.Update();
                    if (v_force || m_styleGroup.objectReferenceValue != null)
                    {
                        _cachedStyleControledByStyleGroup = true;
                        foreach (var v_target in targets)
                        {
                            var v_styleBehaviour = v_target as BaseStyleElement;
                            if(!v_force)
                                v_styleBehaviour.LoadStyles();
                            if (v_styleBehaviour.StyleData == null || v_styleBehaviour.StyleData.Asset == null)
                            {
                                _cachedStyleControledByStyleGroup = false;
                                return _cachedStyleControledByStyleGroup;
                            }
                        }
                    }
                }
                else
                    _cachedStyleControledByStyleGroup = false;
            }
            return _cachedStyleControledByStyleGroup;
        }

        protected virtual void DrawOtherStyleDataProperties()
        {
            bool v_oldGuiEnabled = GUI.enabled;
            var v_controlledByStyleGroup = StyleControledByStyleGroup();
            if (v_controlledByStyleGroup)
            {
                EditorGUILayout.HelpBox("Some values driven by CanvasStyleGroup", MessageType.None);
                GUI.enabled = false;
                EditorGUILayout.PropertyField(m_styleGroup);
                GUI.enabled = v_oldGuiEnabled;
                EditorGUILayout.Space();
            }

            var v_target = target as BaseStyleElement;
            var v_oldValue = m_supportStyleGroup.boolValue;
            EditorGUILayout.PropertyField(m_supportStyleGroup);

            //Property to control reset of cached initial values
            var v_needResetCachedValues = v_oldValue != m_supportStyleGroup.boolValue;

            //Draw Style Data Picker
            GUI.enabled = v_target.SupportStyleGroup;
            if (v_target.StyleGroup != null)
            {
                EditorGUI.showMixedValue = m_styleDataName.hasMultipleDifferentValues;
                var v_validNames = GetAllValidStyleDataNamesWithType(v_target.StyleGroup.StyleAsset, v_target.GetSupportedStyleAssetType(), false);
                var v_index = Mathf.Max(0, System.Array.IndexOf(v_validNames, m_styleDataName.stringValue));
                v_validNames[0] = "<Empty>"; // We must set first position to empty after find index (to prevent that has a property with empty name)
                var v_newIndex = Mathf.Max(0, EditorGUILayout.Popup("Style Data Name", v_index, v_validNames));
                if (v_index != v_newIndex || 
                    (v_validNames.Length > 1 && m_styleDataName.stringValue != v_validNames[v_newIndex] && v_newIndex > 0))
                {
                    m_styleDataName.stringValue = v_validNames.Length > v_newIndex ? v_validNames[v_newIndex] : "";
                    v_needResetCachedValues = true;
                }
                EditorGUI.showMixedValue = false;
            }
            else
            {
                EditorGUILayout.PropertyField(m_styleDataName);
            }
            GUI.enabled = v_oldGuiEnabled;

            if (v_needResetCachedValues)
            {
                _cachedAsset = null;
                _allValidStyleBehaviours = null;
                v_controlledByStyleGroup = StyleControledByStyleGroup(true);
            }
        }


        protected virtual string[] GetAllValidStyleDataNamesWithType(StyleSheetAsset p_asset, System.Type p_validType, bool p_recalculate)
        {
            if (_cachedAsset != p_asset || _allValidStyleBehaviours == null || p_recalculate)
            {
                _cachedAsset = p_asset;
                var v_styleDatas = p_asset != null? p_asset.GetAllStyleDatasFromType(p_validType) : new List<StyleData>();
                List<string> v_validNames = new List<string>() { "" };
                foreach (var v_styleData in v_styleDatas)
                {
                    v_validNames.Add(v_styleData.Name);
                }
                _allValidStyleBehaviours = v_validNames.ToArray();
            }

            return _allValidStyleBehaviours;
        }

        #endregion

        #region Draw StyleProperty

        public virtual void DrawExtraStylePropertiesList()
        {
            if (m_extraStylePropertiesList != null)
            {
                EditorGUI.showMixedValue = m_extraStylePropertiesList.serializedProperty.hasMultipleDifferentValues;
                DrawStylePropertiesListInternal(m_extraStylePropertiesList);
                EditorGUI.showMixedValue = false;
            }
        }

        protected virtual void DrawStylePropertiesListInternal(ReorderableList p_stylePropertiesList)
        {
            //Force Create DefaultBehaviour
            if (ReorderableList.defaultBehaviours == null)
                p_stylePropertiesList.DoList(Rect.zero);

            if (p_stylePropertiesList.serializedProperty.isExpanded)
            {
                p_stylePropertiesList.DoLayoutList();
            }
            else
            {
                GUILayout.Label("", GUILayout.ExpandWidth(true), GUILayout.Height(p_stylePropertiesList.headerHeight));
                var v_lastRect = GUILayoutUtility.GetLastRect();

                if (Event.current.type == EventType.Repaint)
                    ReorderableList.defaultBehaviours.headerBackground.Draw(v_lastRect, false, false, false, false);
                v_lastRect.x += 6;
                v_lastRect.width += 6;
                v_lastRect.y += 1;
                v_lastRect.height -= 1;
                p_stylePropertiesList.drawHeaderCallback.Invoke(v_lastRect);
                GUILayout.Space(5);
            }
            GUILayout.Space(2);
        }


        protected virtual void InitExtraPropertyStyles()
        {
            InitPropertyStylesInternal(ref m_extraStylePropertiesList, "m_extraStyleProperties", false);
        }

        protected virtual void InitPropertyStylesInternal(ref ReorderableList p_reordableList, string p_propertyName, bool p_isMainProperties)
        {
            var v_names = new HashSet<string>();
            var v_styleProperties = serializedObject.FindProperty(p_propertyName);
            p_reordableList = new ReorderableList(serializedObject, v_styleProperties);
            var v_reordableList = p_reordableList;

            p_reordableList.drawHeaderCallback += (rect) =>
            {
                rect.x += 10;
                v_styleProperties.isExpanded = EditorGUI.Foldout(rect, v_styleProperties.isExpanded, v_styleProperties.displayName);
            };
            p_reordableList.displayAdd = !p_isMainProperties;
            p_reordableList.displayRemove = !p_isMainProperties;
            p_reordableList.onAddCallback += (list) => { ReorderableList.defaultBehaviours.DoAddButton(list); };
            p_reordableList.onRemoveCallback += (list) => { ReorderableList.defaultBehaviours.DoRemoveButton(list); };
            p_reordableList.elementHeightCallback += (index) =>
            {
                var v_arrayElement = v_styleProperties.GetArrayElementAtIndex(index);
                var v_isExpanded = v_arrayElement.isExpanded;
                var v_singlePropertyHeight = EditorGUIUtility.singleLineHeight;
                var v_extraSpace = EditorGUIUtility.standardVerticalSpacing;
                if (v_isExpanded)
                {
                    return EditorGUI.GetPropertyHeight(v_arrayElement, true) - v_singlePropertyHeight + v_extraSpace;
                }
                return v_singlePropertyHeight + v_extraSpace + 1;
            };
            
            p_reordableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DrawStyleProperty(v_reordableList, rect, index, v_styleProperties.GetArrayElementAtIndex(index), !p_isMainProperties, ref v_names);
            };
        }

        protected virtual bool IsType<T>(SerializedProperty p_mainProperty, int p_index) where T : Object
        {
            var v_arrayElement = p_mainProperty.GetArrayElementAtIndex(p_index);
            if (v_arrayElement != null)
            {
                var v_targetElement = v_arrayElement.FindPropertyRelative("m_target");
                return v_targetElement.objectReferenceValue == null ||
                    (v_targetElement.objectReferenceValue is Transform && ((Transform)v_targetElement.objectReferenceValue).GetComponent<T>() != null);
            }
            return false;
        }

        protected virtual void DrawStyleProperty(ReorderableList p_reordableList, Rect p_rect, int p_index, SerializedProperty p_property, bool p_canEditName, ref HashSet<string> p_names)
        {
            p_rect = new Rect(p_rect.x + 10, p_rect.y, p_rect.width - 10, EditorGUIUtility.singleLineHeight);

            if (p_index == 0)
                p_names.Clear();

            var v_oldGuiEnabled = GUI.enabled;
            var v_nameProperty = p_property.FindPropertyRelative("m_name");
            var v_targetProperty = p_property.FindPropertyRelative("m_target");
            var v_endProperty = p_property.GetEndProperty();

            var v_foldoutRect = new Rect(p_rect.x, p_rect.y, 10, p_rect.height);
            EditorGUI.PropertyField(v_foldoutRect, p_property, new GUIContent(""), false);
            DrawStyleTarget(p_rect, p_index, v_targetProperty, new GUIContent(p_index + ": " + v_nameProperty.stringValue));
            if (p_property.isExpanded)
            {
                p_property.NextVisible(true); // force enter in child
                //p_rect = EditorGUI.IndentedRect(p_rect);
                p_rect.y += EditorGUIUtility.standardVerticalSpacing;
                p_rect.x += 10;
                p_rect.width -= 10;

                do
                {
                    if (SerializedProperty.EqualContents(p_property, v_endProperty))
                        break;
                    p_rect.y += EditorGUIUtility.singleLineHeight;
                    if (p_property.name == v_nameProperty.name)
                        DrawStyleName(p_rect, p_property, p_canEditName);
                    else if (p_property.name == v_targetProperty.name)
                        p_rect.y -= EditorGUIUtility.singleLineHeight;
                    else
                        EditorGUI.PropertyField(p_rect, p_property);
                }
                while (p_property.NextVisible(false));
            }

            CheckHashNames(p_index, v_nameProperty, v_targetProperty.objectReferenceValue != null ? v_targetProperty.objectReferenceValue.name : "", ref p_names);
            // Add to Hash to prevent same name
            if (!string.IsNullOrEmpty(v_nameProperty.stringValue) && !p_names.Contains(v_nameProperty.stringValue))
                p_names.Add(v_nameProperty.stringValue);
        }

        protected void DrawStyleTarget(Rect rect, int index, SerializedProperty p_property, GUIContent p_displayName)
        {
            if (p_property != null)
            {
                var v_displayName = p_displayName != null ? p_displayName : new GUIContent(p_property.displayName);
                var v_oldGUiEnabled = GUI.enabled;

                var v_styleElement = p_property.objectReferenceValue is Transform ? ((Transform)p_property.objectReferenceValue).GetComponent<BaseStyleElement>() : null;
                if (v_styleElement != null)
                {
                    EditorGUI.showMixedValue = p_property.hasMultipleDifferentValues;
                    var v_newStyleElement = EditorGUI.ObjectField(rect, v_displayName, v_styleElement, typeof(BaseStyleElement), true) as BaseStyleElement;
                    if (v_styleElement != v_newStyleElement)
                    {
                        p_property.objectReferenceValue = v_newStyleElement != null ? v_newStyleElement.transform : null;
                    }
                    EditorGUI.showMixedValue = false;
                }
                else
                {
                    var v_graphic = p_property.objectReferenceValue is Transform ? ((Transform)p_property.objectReferenceValue).GetComponent<Graphic>() : null;
                    if (v_graphic != null)
                    {
                        EditorGUI.showMixedValue = p_property.hasMultipleDifferentValues;
                        var v_newGraphic = EditorGUI.ObjectField(rect, v_displayName, v_graphic, typeof(Graphic), true) as Graphic;
                        if (v_graphic != v_newGraphic)
                        {
                            p_property.objectReferenceValue = v_newGraphic != null ? v_newGraphic.transform : null;
                        }
                        EditorGUI.showMixedValue = false;
                    }
                    else
                    {
                        EditorGUI.PropertyField(rect, p_property, v_displayName, false);
                    }
                }
            }
        }

        protected void DrawStyleName(Rect rect, SerializedProperty p_property, bool p_canEditName)
        {
            if (p_property != null)
            {
                var v_oldGUiEnabled = GUI.enabled;
                GUI.enabled = p_canEditName;
                EditorGUI.PropertyField(rect, p_property);
                GUI.enabled = v_oldGUiEnabled;
            }
        }

        protected void CheckHashNames(int index, SerializedProperty p_property, string p_baseName, ref HashSet<string> p_names)
        {
            //Set Object Key Name
            if ((!string.IsNullOrEmpty(p_baseName) && string.IsNullOrEmpty(p_property.stringValue)) || p_names.Contains(p_property.stringValue))
            {
                var v_counter = 0;
                var v_baseName = string.IsNullOrEmpty(p_property.stringValue) ? p_baseName : p_property.stringValue;
                var v_name = v_baseName;
                while (p_names.Contains(v_name))
                {
                    v_counter++;
                    v_name = v_baseName + " (" + v_counter + ")";
                }
                p_property.stringValue = v_name;
            }
        }

        #endregion

        #region Static Functions

        protected void LayoutStyle_DrawPropertiesExcluding(SerializedObject obj, IList<string> propertyToExclude)
        {
            var v_property = obj.GetIterator();
            v_property.NextVisible(true); //Force pick first property

            do
            {
                if (propertyToExclude == null || !propertyToExclude.Contains(v_property.name))
                    LayoutStyle_PropertyField(v_property);
            }
            while (v_property.NextVisible(false));
        }

        static GUIStyle s_miniLabelStyle = null;
        static GUIContent s_mssGuiContent = new GUIContent("mss", "Click to enable/disable Material Style Property of this field");
        static Color s_proSkinMiniButton = new Color(0, 0.7f, 0);
        static Color s_proSkinMixedMiniButton = new Color(0.9f, 0.7f, 0.3f);
        protected void LayoutStyle_PropertyField(SerializedProperty p_property, params GUILayoutOption[] options)
        {
            LayoutStyle_PropertyField(p_property, null, true, options);
        }

        protected void LayoutStyle_PropertyField(SerializedProperty p_property, bool p_includeChildren, params GUILayoutOption[] options)
        {
            LayoutStyle_PropertyField(p_property, null, p_includeChildren, options);
        }

        protected void LayoutStyle_PropertyField(SerializedProperty p_property, GUIContent p_content, params GUILayoutOption[] options)
        {
            LayoutStyle_PropertyField(p_property, p_content, false, options);
        }

        protected void LayoutStyle_PropertyField(SerializedProperty p_property, GUIContent p_content, bool p_includeChildren, params GUILayoutOption[] options)
        {
            //Cache Style
            if (s_miniLabelStyle == null)
            {
                s_miniLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                s_miniLabelStyle.normal.textColor = Color.white;
            }
            var v_oldGui = GUI.enabled;
            var v_oldShowMixedValue = EditorGUI.showMixedValue;
            var v_oldColor = GUI.color;

            var v_styleMetaType = MaterialUI.Reflection.StyleMetaType.GetOrCreateStyleMetaType(p_property.serializedObject.targetObject.GetType());
            using (new EditorGUILayout.HorizontalScope())
            {
                var v_target = p_property.serializedObject.targetObject as BaseStyleElement;
                var v_isMSS = v_styleMetaType.DeclaredMembers.ContainsKey(p_property.name) || v_styleMetaType.GetMembers().ContainsKey(p_property.name);

                //Find if this property has MSS enabled (or with mixed value)
                var v_styleControledByStyleGroup = StyleControledByStyleGroup();
                var v_mssEnabled = v_isMSS && v_target.GetFieldStyleActive(p_property.name);
                var v_showMixedValue = false;
                if (v_isMSS)
                {
                    for (int i = 1; i < targets.Length; i++)
                    {
                        var baseStyleTarget = targets[i] as BaseStyleElement;
                        if (v_mssEnabled != baseStyleTarget.GetFieldStyleActive(p_property.name))
                        {
                            v_showMixedValue = true;
                            v_mssEnabled = true;
                            break;
                        }
                    }
                }
                
                //Draw Original Field
                GUI.enabled = !v_styleControledByStyleGroup || p_property.propertyType == SerializedPropertyType.ObjectReference || !v_mssEnabled;
                EditorGUILayout.PropertyField(p_property, p_content, p_includeChildren, options);

                GUI.enabled = v_oldGui;
                var v_mssButtonSize = 30;
                //Draw MSS Toggle Field
                if (v_isMSS)
                {
                    //Draw Toggle
                    GUI.color = v_showMixedValue ? s_proSkinMixedMiniButton : (v_mssEnabled ? EditorGUIUtility.isProSkin ? s_proSkinMiniButton : Color.green : Color.red);
                    var v_newValue = EditorGUILayout.Toggle(v_mssEnabled, EditorStyles.miniButton, GUILayout.Width(v_mssButtonSize));
                    if (v_newValue != v_mssEnabled)
                    {
                        v_mssEnabled = v_newValue;
                        for (int i = 0; i < targets.Length; i++)
                        {
                            var baseStyleTarget = targets[i] as BaseStyleElement;
                            baseStyleTarget.SetFieldStyleActive(p_property.name, v_mssEnabled);
                        }
                    }
                    GUI.color = v_oldColor;

                    //Draw Label
                    var v_lastRect = GUILayoutUtility.GetLastRect();
                    v_lastRect.x -= (-2 + EditorGUI.indentLevel * 14);
                    v_lastRect.width += 10;
                    EditorGUI.LabelField(v_lastRect, s_mssGuiContent, s_miniLabelStyle);
                }
                else
                    GUILayout.Space(v_mssButtonSize + 4);

                EditorGUI.showMixedValue = v_oldShowMixedValue;
            }
        }

        #endregion
    }
}
