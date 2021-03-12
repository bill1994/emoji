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

        protected string[] _excludingProperties = new string[] { "m_Script", "m_extraStyleProperties", "m_styleGroup", "m_styleDataName", "m_supportStyleGroup", "m_disabledFieldStyles" };

        protected string[] _allValidStyleBehaviours = null;
        protected StyleSheetAsset _cachedAsset = null;

        protected SerializedProperty m_script = null;
        protected SerializedProperty m_styleGroup = null;
        protected SerializedProperty m_supportStyleGroup = null;
        protected SerializedProperty m_styleDataName = null;
        protected ReorderableList m_extraStylePropertiesList = null;

        protected bool _cachedStyleControledByStyleGroup = false;


        protected bool m_ShowStyles = true;
        protected private string m_StylesPrefKey;

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
            var oldGuiEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_script);
            GUI.enabled = oldGuiEnabled;

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
            string prefKey = GetType().Name;
            m_StylesPrefKey = prefKey + "_Show_Styles";
            m_ShowStyles = EditorPrefs.GetBool(m_StylesPrefKey, true);

            //Reset Important Properties
            _allValidStyleBehaviours = null;
            _cachedAsset = null;
            StyleControledByStyleGroup(true);
        }

        protected bool StyleControledByStyleGroup(bool recalculate = false)
        {
            if (recalculate)
            {
                if (m_supportStyleGroup.boolValue)
                {
                    serializedObject.ApplyModifiedProperties();
                    var force = false;
                    if (m_styleGroup.objectReferenceValue == null)
                    {
                        force = true;
                        foreach (var target in targets)
                        {
                            var styleBehaviour = target as BaseStyleElement;
                            if (styleBehaviour != null)
                            {
                                styleBehaviour.ForceRegisterToStyleGroup();
                                styleBehaviour.LoadStyles();
                                force = force && styleBehaviour.StyleGroup && styleBehaviour.StyleData.Asset != null;
                            }
                            else
                                force = false;
                        }
                    }
                    serializedObject.Update();
                    if (force || m_styleGroup.objectReferenceValue != null)
                    {
                        _cachedStyleControledByStyleGroup = true;
                        foreach (var target in targets)
                        {
                            var styleBehaviour = target as BaseStyleElement;
                            if(!force)
                                styleBehaviour.LoadStyles();
                            if (styleBehaviour.StyleData == null || styleBehaviour.StyleData.Asset == null)
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
            bool oldGuiEnabled = GUI.enabled;
            var controlledByStyleGroup = StyleControledByStyleGroup();
            if (controlledByStyleGroup)
            {
                EditorGUILayout.HelpBox("Some values driven by CanvasStyleGroup", MessageType.None);
                GUI.enabled = false;
                EditorGUILayout.PropertyField(m_styleGroup);
                GUI.enabled = oldGuiEnabled;
                EditorGUILayout.Space();
            }

            var castedTarget = target as BaseStyleElement;
            var oldValue = m_supportStyleGroup.boolValue;
            EditorGUILayout.PropertyField(m_supportStyleGroup);

            //Property to control reset of cached initial values
            var needResetCachedValues = oldValue != m_supportStyleGroup.boolValue;

            //Draw Style Data Picker
            GUI.enabled = castedTarget.SupportStyleGroup;
            if (castedTarget.StyleGroup != null)
            {
                EditorGUI.showMixedValue = m_styleDataName.hasMultipleDifferentValues;
                var validNames = GetAllValidStyleDataNamesWithType(castedTarget.StyleGroup.StyleAsset, castedTarget.GetSupportedStyleAssetType(), false);
                var index = Mathf.Max(0, System.Array.IndexOf(validNames, m_styleDataName.stringValue));
                validNames[0] = "<Empty>"; // We must set first position to empty after find index (to prevent that has a property with empty name)
                var newIndex = Mathf.Max(0, EditorGUILayout.Popup("Style Data Name", index, validNames));
                if (index != newIndex || 
                    (validNames.Length > 1 && m_styleDataName.stringValue != validNames[newIndex] && newIndex > 0))
                {
                    m_styleDataName.stringValue = validNames.Length > newIndex ? validNames[newIndex] : "";
                    needResetCachedValues = true;
                }
                EditorGUI.showMixedValue = false;
            }
            else
            {
                EditorGUILayout.PropertyField(m_styleDataName);
            }
            GUI.enabled = oldGuiEnabled;

            if (needResetCachedValues)
            {
                _cachedAsset = null;
                _allValidStyleBehaviours = null;
                controlledByStyleGroup = StyleControledByStyleGroup(true);
            }
        }


        protected virtual string[] GetAllValidStyleDataNamesWithType(StyleSheetAsset asset, System.Type validType, bool recalculate)
        {
            if (_cachedAsset != asset || _allValidStyleBehaviours == null || recalculate)
            {
                _cachedAsset = asset;
                var styleDatas = asset != null? asset.GetAllStyleDatasFromType(validType) : new List<StyleData>();
                List<string> validNames = new List<string>() { "" };
                foreach (var styleData in styleDatas)
                {
                    validNames.Add(styleData.Name);
                }
                _allValidStyleBehaviours = validNames.ToArray();
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

        protected virtual void DrawStylePropertiesListInternal(ReorderableList stylePropertiesList)
        {
            //Force Create DefaultBehaviour
            if (ReorderableList.defaultBehaviours == null)
                stylePropertiesList.DoList(Rect.zero);

            if (stylePropertiesList.serializedProperty.isExpanded)
            {
                stylePropertiesList.DoLayoutList();
            }
            else
            {
                GUILayout.Label("", GUILayout.ExpandWidth(true), GUILayout.Height(stylePropertiesList.headerHeight));
                var lastRect = GUILayoutUtility.GetLastRect();

                if (Event.current.type == EventType.Repaint)
                    ReorderableList.defaultBehaviours.headerBackground.Draw(lastRect, false, false, false, false);
                lastRect.x += 6;
                lastRect.width += 6;
                lastRect.y += 1;
                lastRect.height -= 1;
                stylePropertiesList.drawHeaderCallback.Invoke(lastRect);
                GUILayout.Space(5);
            }
            GUILayout.Space(2);
        }


        protected virtual void InitExtraPropertyStyles()
        {
            InitPropertyStylesInternal(ref m_extraStylePropertiesList, "m_extraStyleProperties", false);
        }

        protected virtual void InitPropertyStylesInternal(ref ReorderableList reordableList, string propertyName, bool isMainProperties)
        {
            var names = new HashSet<string>();
            var styleProperties = serializedObject.FindProperty(propertyName);
            var internalReordableList = new ReorderableList(serializedObject, styleProperties);
            reordableList = internalReordableList;

            internalReordableList.drawHeaderCallback += (rect) =>
            {
                rect.x += 10;
                styleProperties.isExpanded = EditorGUI.Foldout(rect, styleProperties.isExpanded, styleProperties.displayName);
            };
            internalReordableList.displayAdd = !isMainProperties;
            internalReordableList.displayRemove = !isMainProperties;
            internalReordableList.onAddCallback += (list) => { ReorderableList.defaultBehaviours.DoAddButton(list); };
            internalReordableList.onRemoveCallback += (list) => { ReorderableList.defaultBehaviours.DoRemoveButton(list); };
            internalReordableList.elementHeightCallback += (index) =>
            {
                var arrayElement = styleProperties.GetArrayElementAtIndex(index);
                var isExpanded = arrayElement.isExpanded;
                var singlePropertyHeight = EditorGUIUtility.singleLineHeight;
                var extraSpace = EditorGUIUtility.standardVerticalSpacing;
                if (isExpanded)
                {
                    return EditorGUI.GetPropertyHeight(arrayElement, true) - singlePropertyHeight + extraSpace;
                }
                return singlePropertyHeight + extraSpace + 1;
            };
            
            internalReordableList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DrawStyleProperty(internalReordableList, rect, index, styleProperties.GetArrayElementAtIndex(index), !isMainProperties, ref names);
            };
        }

        protected virtual bool IsType<T>(SerializedProperty mainProperty, int index) where T : Object
        {
            var arrayElement = mainProperty.GetArrayElementAtIndex(index);
            if (arrayElement != null)
            {
                var targetElement = arrayElement.FindPropertyRelative("m_target");
                return targetElement.objectReferenceValue == null ||
                    (targetElement.objectReferenceValue is Transform && ((Transform)targetElement.objectReferenceValue).GetComponent<T>() != null);
            }
            return false;
        }

        protected virtual void DrawStyleProperty(ReorderableList reordableList, Rect rect, int index, SerializedProperty property, bool canEditName, ref HashSet<string> names)
        {
            rect = new Rect(rect.x + 10, rect.y, rect.width - 10, EditorGUIUtility.singleLineHeight);

            if (index == 0)
                names.Clear();

            var oldGuiEnabled = GUI.enabled;
            var nameProperty = property.FindPropertyRelative("m_name");
            var targetProperty = property.FindPropertyRelative("m_target");
            var endProperty = property.GetEndProperty();

            var foldoutRect = new Rect(rect.x, rect.y, 10, rect.height);
            EditorGUI.PropertyField(foldoutRect, property, new GUIContent(""), false);
            DrawStyleTarget(rect, index, targetProperty, new GUIContent(index + ": " + nameProperty.stringValue));
            if (property.isExpanded)
            {
                property.NextVisible(true); // force enter in child
                //rect = EditorGUI.IndentedRect(rect);
                rect.y += EditorGUIUtility.standardVerticalSpacing;
                rect.x += 10;
                rect.width -= 10;

                do
                {
                    if (SerializedProperty.EqualContents(property, endProperty))
                        break;
                    rect.y += EditorGUIUtility.singleLineHeight;
                    if (property.name == nameProperty.name)
                        DrawStyleName(rect, property, canEditName);
                    else if (property.name == targetProperty.name)
                        rect.y -= EditorGUIUtility.singleLineHeight;
                    else
                        EditorGUI.PropertyField(rect, property);
                }
                while (property.NextVisible(false));
            }

            CheckHashNames(index, nameProperty, targetProperty.objectReferenceValue != null ? targetProperty.objectReferenceValue.name : "", ref names);
            // Add to Hash to prevent same name
            if (!string.IsNullOrEmpty(nameProperty.stringValue) && !names.Contains(nameProperty.stringValue))
                names.Add(nameProperty.stringValue);
        }

        protected void DrawStyleTarget(Rect rect, int index, SerializedProperty property, GUIContent displayName)
        {
            if (property != null)
            {
                displayName = displayName != null ? displayName : new GUIContent(property.displayName);
                var oldGUiEnabled = GUI.enabled;

                var styleElement = property.objectReferenceValue is Transform ? ((Transform)property.objectReferenceValue).GetComponent<BaseStyleElement>() : null;
                if (styleElement != null)
                {
                    EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                    var newStyleElement = EditorGUI.ObjectField(rect, displayName, styleElement, typeof(BaseStyleElement), true) as BaseStyleElement;
                    if (styleElement != newStyleElement)
                    {
                        property.objectReferenceValue = newStyleElement != null ? newStyleElement.transform : null;
                    }
                    EditorGUI.showMixedValue = false;
                }
                else
                {
                    var graphic = property.objectReferenceValue is Transform ? ((Transform)property.objectReferenceValue).GetComponent<Graphic>() : null;
                    if (graphic != null)
                    {
                        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                        var newGraphic = EditorGUI.ObjectField(rect, displayName, graphic, typeof(Graphic), true) as Graphic;
                        if (graphic != newGraphic)
                        {
                            property.objectReferenceValue = newGraphic != null ? newGraphic.transform : null;
                        }
                        EditorGUI.showMixedValue = false;
                    }
                    else
                    {
                        EditorGUI.PropertyField(rect, property, displayName, false);
                    }
                }
            }
        }

        protected void DrawStyleName(Rect rect, SerializedProperty property, bool canEditName)
        {
            if (property != null)
            {
                var oldGUiEnabled = GUI.enabled;
                GUI.enabled = canEditName;
                EditorGUI.PropertyField(rect, property);
                GUI.enabled = oldGUiEnabled;
            }
        }

        protected void CheckHashNames(int index, SerializedProperty property, string baseName, ref HashSet<string> names)
        {
            //Set Object Key Name
            if ((!string.IsNullOrEmpty(baseName) && string.IsNullOrEmpty(property.stringValue)) || names.Contains(property.stringValue))
            {
                var counter = 0;
                baseName = string.IsNullOrEmpty(property.stringValue) ? baseName : property.stringValue;
                var name = baseName;
                while (names.Contains(name))
                {
                    counter++;
                    name = baseName + " (" + counter + ")";
                }
                property.stringValue = name;
            }
        }

        #endregion

        #region Static Functions

        protected void LayoutStyle_DrawPropertiesExcluding(SerializedObject obj, IList<string> propertyToExclude)
        {
            var property = obj.GetIterator();
            property.NextVisible(true); //Force pick first property

            do
            {
                if (propertyToExclude == null || !propertyToExclude.Contains(property.name))
                    LayoutStyle_PropertyField(property);
            }
            while (property.NextVisible(false));
        }

        static GUIStyle s_miniLabelStyle = null;
        static GUIContent s_mssGuiContent = new GUIContent("mss", "Click to enable/disable Material Style Property of this field");
        static Color s_proSkinMiniButton = new Color(0, 0.7f, 0);
        static Color s_proSkinMixedMiniButton = new Color(0.9f, 0.7f, 0.3f);
        protected void LayoutStyle_PropertyField(SerializedProperty property, params GUILayoutOption[] options)
        {
            LayoutStyle_PropertyField(property, null, true, options);
        }

        protected void LayoutStyle_PropertyField(SerializedProperty property, bool includeChildren, params GUILayoutOption[] options)
        {
            LayoutStyle_PropertyField(property, null, includeChildren, options);
        }

        protected void LayoutStyle_PropertyField(SerializedProperty property, GUIContent content, params GUILayoutOption[] options)
        {
            LayoutStyle_PropertyField(property, content, false, options);
        }

        protected void LayoutStyle_PropertyField(SerializedProperty property, GUIContent content, bool includeChildren, params GUILayoutOption[] options)
        {
            LayoutStyle_PropertyField(property, property, content, includeChildren, options);
        }

        protected void LayoutStyle_PropertyField(SerializedProperty property, SerializedProperty mssProperty, GUIContent content, bool includeChildren, params GUILayoutOption[] options)
        {
            if (mssProperty == null)
                mssProperty = property;

            //Cache Style
            if (s_miniLabelStyle == null)
            {
                s_miniLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                s_miniLabelStyle.normal.textColor = Color.white;
            }
            var oldGui = GUI.enabled;
            var oldShowMixedValue = EditorGUI.showMixedValue;
            var oldColor = GUI.color;

            var styleMetaType = MaterialUI.Reflection.StyleMetaType.GetOrCreateStyleMetaType(mssProperty.serializedObject.targetObject.GetType());
            using (new EditorGUILayout.HorizontalScope())
            {
                var target = mssProperty.serializedObject.targetObject as BaseStyleElement;
                var isMSS = styleMetaType.DeclaredMembers.ContainsKey(mssProperty.name) || styleMetaType.GetMembers().ContainsKey(mssProperty.name);

                //Find if this property has MSS enabled (or with mixed value)
                var styleControledByStyleGroup = StyleControledByStyleGroup();
                var mssEnabled = isMSS && target.GetFieldStyleActive(mssProperty.name);
                var showMixedValue = false;
                if (isMSS)
                {
                    for (int i = 1; i < targets.Length; i++)
                    {
                        var baseStyleTarget = targets[i] as BaseStyleElement;
                        if (mssEnabled != baseStyleTarget.GetFieldStyleActive(mssProperty.name))
                        {
                            showMixedValue = true;
                            mssEnabled = true;
                            break;
                        }
                    }
                }
                
                //Draw Original Field
                GUI.enabled = oldGui && (!styleControledByStyleGroup || mssProperty.propertyType == SerializedPropertyType.ObjectReference || !mssEnabled);
                EditorGUILayout.PropertyField(property, content, includeChildren, options);

                GUI.enabled = oldGui;
                var mssButtonSize = 30;
                //Draw MSS Toggle Field
                if (isMSS)
                {
                    //Draw Toggle
                    GUI.color = showMixedValue ? s_proSkinMixedMiniButton : (mssEnabled ? EditorGUIUtility.isProSkin ? s_proSkinMiniButton : Color.green : Color.red);
                    var newValue = EditorGUILayout.Toggle(mssEnabled, EditorStyles.miniButton, GUILayout.Width(mssButtonSize));
                    if (newValue != mssEnabled)
                    {
                        mssEnabled = newValue;
                        for (int i = 0; i < targets.Length; i++)
                        {
                            var baseStyleTarget = targets[i] as BaseStyleElement;
                            baseStyleTarget.SetFieldStyleActive(mssProperty.name, mssEnabled);
                        }
                    }
                    GUI.color = oldColor;

                    //Draw Label
                    var lastRect = GUILayoutUtility.GetLastRect();
                    lastRect.x -= (-2 + EditorGUI.indentLevel * 14);
                    lastRect.width += 10;
                    EditorGUI.LabelField(lastRect, s_mssGuiContent, s_miniLabelStyle);
                }
                else
                    GUILayout.Space(mssButtonSize + 4);

                EditorGUI.showMixedValue = oldShowMixedValue;
            }
        }

        #endregion
    }
}
