using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditorInternal;

namespace MaterialUI
{
    [CustomEditor(typeof(StyleSheetAsset))]
    public class StyleSheetAssetEditor : Editor
    {
        #region Fields

        ReorderableList m_styleDataList;
        SerializedProperty m_styles;

        Texture2D _errorIcon;

        #endregion

        #region Properties

        protected Texture2D ErrorIcon
        {
            get
            {
                if (_errorIcon == null)
                {
                    var v_property = typeof(EditorGUIUtility).GetProperty("errorIcon", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (v_property != null)
                        _errorIcon = v_property.GetValue(null, null) as Texture2D;
                }
                return _errorIcon;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            InitStyleDataList();
            //TryInitResources(true);
        }

        public override void OnInspectorGUI()
        {
            var v_oldGuiEnabled = GUI.enabled;
            var v_target = target as StyleSheetAsset;
            //TryInitResources(false);
            if (v_target.StyleObjectsMap.Count == 0)
                GUI.enabled = false;
            if (GUILayout.Button("Edit Asset Palette"))
            {
                StylePalleteWindow.Init(target as StyleSheetAsset);
            }
            GUI.enabled = v_oldGuiEnabled;
            EditorGUILayout.Space();

            //GUI.enabled = false;
            //m_colorDataList.DoLayoutList();
            //m_graphicDataList.DoLayoutList();
            //GUI.enabled = v_oldGuiEnabled;

            serializedObject.Update();
            m_styleDataList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region StyleData Draw Functions

        protected virtual void InitStyleDataList()
        {
            List<float> v_cachedExpandedSize = new List<float>();
            HashSet<string> v_names = new HashSet<string>();
            m_styles = serializedObject.FindProperty("m_styles");

            m_styleDataList = new ReorderableList(serializedObject, m_styles);
            m_styleDataList.drawHeaderCallback += (rect) => { EditorGUI.LabelField(rect, m_styles.displayName); };
            m_styleDataList.onAddCallback += (list) =>
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
                var v_property = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
                v_property.FindPropertyRelative("m_assetPrefab").objectReferenceValue = null;
                v_property.FindPropertyRelative("m_name").stringValue = "";
            };
            m_styleDataList.onRemoveCallback += (list) =>
            {
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            };
            m_styleDataList.elementHeightCallback += (index) =>
            {
                var v_arrayElement = m_styles.GetArrayElementAtIndex(index);
                var v_isExpanded = v_arrayElement.isExpanded;
                var v_singlePropertyHeight = EditorGUIUtility.singleLineHeight;
                var v_extraSpace = EditorGUIUtility.standardVerticalSpacing;
                if (v_isExpanded)
                {
                    var v_expandedSize = v_cachedExpandedSize.Count > index ? v_cachedExpandedSize[index] : 0;
                    return EditorGUI.GetPropertyHeight(v_arrayElement, true) - v_singlePropertyHeight + v_extraSpace + v_expandedSize;
                }
                return v_singlePropertyHeight + v_extraSpace + 1;
            };
            m_styleDataList.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                DrawStyleDataProperty(m_styleDataList, rect, index, m_styles.GetArrayElementAtIndex(index), true, ref v_names, ref v_cachedExpandedSize);
            };
        }

        protected virtual void DrawStyleDataProperty(ReorderableList p_reordableList, Rect p_rect, int p_index, SerializedProperty p_property, bool p_canEditName, ref HashSet<string> p_names, ref List<float> v_cachedExpandedSize)
        {
            p_rect = new Rect(p_rect.x + 10, p_rect.y, p_rect.width - 10, EditorGUIUtility.singleLineHeight);
            if (p_index == 0)
            {
                p_names.Clear();
                v_cachedExpandedSize.Clear();
            }
            v_cachedExpandedSize.Add(0);

            var v_oldGuiEnabled = GUI.enabled;
            var v_nameProperty = p_property.FindPropertyRelative("m_name");
            var v_targetProperty = p_property.FindPropertyRelative("m_assetPrefab");
            var v_endProperty = p_property.GetEndProperty();

            var v_foldoutRect = new Rect(p_rect.x, p_rect.y, 10, p_rect.height);
            EditorGUI.PropertyField(v_foldoutRect, p_property, new GUIContent(""), false);
            DrawStyleDataTarget(p_rect, p_index, v_targetProperty, v_nameProperty, new GUIContent(p_index + ": " + v_nameProperty.stringValue));
            if (p_property.isExpanded)
            {
                p_property.NextVisible(true); // force enter in child
                p_rect = new Rect(p_rect.x + 10, p_rect.y + EditorGUIUtility.standardVerticalSpacing, p_rect.width - 10, p_rect.height);

                do
                {
                    if (SerializedProperty.EqualContents(p_property, v_endProperty))
                        break;
                    p_rect.y += EditorGUIUtility.singleLineHeight;
                    if (p_property.name == v_nameProperty.name)
                        DrawStyleDataName(p_rect, p_property, p_canEditName);
                    else if (p_property.name == v_targetProperty.name)
                    {
                        p_rect.y -= EditorGUIUtility.singleLineHeight;
                    }
                    else
                    {
                        EditorGUI.PropertyField(p_rect, p_property, false);
                        GUI.enabled = v_oldGuiEnabled;
                    }
                }
                while (p_property.NextVisible(false));

                v_cachedExpandedSize[p_index] = DrawExpandedStyleBehaviour(p_rect, v_targetProperty.objectReferenceValue as GameObject);
                p_rect.y += v_cachedExpandedSize[p_index];
            }

            CheckHashNames(p_index, v_nameProperty, v_targetProperty.objectReferenceValue != null ? v_targetProperty.objectReferenceValue.name : "", ref p_names);
            // Add to Hash to prevent same name
            if (!string.IsNullOrEmpty(v_nameProperty.stringValue) && !p_names.Contains(v_nameProperty.stringValue))
                p_names.Add(v_nameProperty.stringValue);
        }

        protected float DrawExpandedStyleBehaviour(Rect p_rect, GameObject p_targetObject)
        {
            float v_size = 0;
            var v_targetStyleBehaviour = p_targetObject != null ? p_targetObject.GetComponent<BaseStyleElement>() : null;
            if (v_targetStyleBehaviour != null)
            {
                var v_metaType = MaterialUI.Reflection.StyleMetaType.GetOrCreateStyleMetaType(v_targetStyleBehaviour.GetType());
                var v_metaTypeMembers = v_metaType.GetMembers();
                SerializedProperty targetPropertyExpanded = new SerializedObject(v_targetStyleBehaviour).GetIterator();
                bool enterChildren = true;
                while (targetPropertyExpanded.NextVisible(enterChildren))
                {
                    if (v_metaTypeMembers.ContainsKey(targetPropertyExpanded.name))
                    {
                        if (v_size == 0)
                        {
                            p_rect.y += EditorGUIUtility.singleLineHeight*2;
                            EditorGUI.LabelField(p_rect, "Style Properties", EditorStyles.boldLabel);
                            p_rect.y += EditorGUIUtility.singleLineHeight;

                            v_size += EditorGUIUtility.singleLineHeight * 2 + 4;
                        }
                        enterChildren = false;
                        float v_deltaSize = 0;
                        EditorGUI.PropertyField(p_rect, targetPropertyExpanded, true);
                        v_deltaSize += EditorGUI.GetPropertyHeight(targetPropertyExpanded);

                        p_rect.y += v_deltaSize;
                        v_size += v_deltaSize;
                    }
                }
            }
            return v_size;
        }

        protected void DrawStyleDataTarget(Rect rect, int index, SerializedProperty p_targetProperty, SerializedProperty p_nameProperty, GUIContent p_displayName)
        {
            if (p_targetProperty != null)
            {
                var v_displayName = p_displayName != null ? p_displayName : new GUIContent(p_targetProperty.displayName);
                var v_oldGUiEnabled = GUI.enabled;

                var v_styleMissingSize = 24;
                var v_styleBehaviour = p_targetProperty.objectReferenceValue is GameObject ? ((GameObject)p_targetProperty.objectReferenceValue).GetComponent<BaseStyleElement>() : null;
                if (v_styleBehaviour == null)
                {
                    var v_warningIconRect = new Rect(rect.x, rect.y - 2, v_styleMissingSize, v_styleMissingSize);
                    EditorGUI.LabelField(v_warningIconRect, new GUIContent("", ErrorIcon, "No components inheriting from' " + typeof(BaseStyleElement).Name + "' in this prefab root"));
                    rect = new Rect(v_warningIconRect.xMax + 5, rect.y, rect.width - v_warningIconRect.width - 5, rect.height);
                    EditorGUI.PropertyField(rect, p_targetProperty, v_displayName, false);
                }
                else
                {
                    var v_newStyleBehaviour = EditorGUI.ObjectField(rect, v_displayName, v_styleBehaviour, typeof(BaseStyleElement), false) as BaseStyleElement;
                    if (v_newStyleBehaviour != v_styleBehaviour)
                    {
                        p_targetProperty.objectReferenceValue = v_newStyleBehaviour != null ? v_newStyleBehaviour.gameObject : null;
                        if (p_targetProperty.objectReferenceValue == null && p_nameProperty != null)
                            p_nameProperty.stringValue = "";
                    }
                }
            }
        }

        protected void DrawStyleDataName(Rect rect, SerializedProperty p_property, bool p_canEditName)
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
            //Remove Prefix from BaseName if PropertyName is empty
            if (string.IsNullOrEmpty(p_property.stringValue))
            {
                var v_prefix = GetPrefixAssetName();
                if (!string.IsNullOrEmpty(v_prefix) && !string.IsNullOrEmpty(p_baseName))
                {
                    var v_index = p_baseName.IndexOf(v_prefix);
                    if (v_index == 0)
                        p_baseName = p_baseName.Length > v_index + v_prefix.Length ? p_baseName.Substring(v_index + v_prefix.Length) : "";
                }
            }

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

        #region Internal Helper Functions

        public string GetPrefixAssetName()
        {
            var v_split = target.name.Split(new char[] { '_' }, System.StringSplitOptions.None);
            return v_split.Length > 1 ? v_split[0] + "_" : name;
        }

        #endregion
    }
}
