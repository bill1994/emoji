#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.UI;
using UnityEngine.UI;
using UnityEditor.UI;

namespace KyubEditor.UI
{

    [CustomEditor(typeof(NestedScrollRect), true)]
    public class NestedScrollRectEditor : ScrollRectEditor
    {
        SerializedProperty m_SnapToDuration = null;
        SerializedProperty m_NestedDragActive = null;

        SerializedProperty m_UseContentMinWidth = null;
        SerializedProperty m_UseContentMinHeight = null;
        SerializedProperty m_UseContentPreferredWidth = null;
        SerializedProperty m_UseContentPreferredHeight = null;

        protected override void OnEnable()
        {
            m_SnapToDuration = serializedObject.FindProperty("m_SnapToDuration");
            m_NestedDragActive = serializedObject.FindProperty("m_NestedDragActive");
            m_UseContentMinWidth = serializedObject.FindProperty("m_UseContentMinWidth");
            m_UseContentMinHeight = serializedObject.FindProperty("m_UseContentMinHeight");
            m_UseContentPreferredWidth = serializedObject.FindProperty("m_UseContentPreferredWidth");
            m_UseContentPreferredHeight = serializedObject.FindProperty("m_UseContentPreferredHeight");

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_SnapToDuration);
            EditorGUILayout.PropertyField(m_NestedDragActive);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();

            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extra Layout Properties", EditorStyles.boldLabel);

            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, EditorGUIUtility.TrTextContent("Use Content Min"));
            rect.width = Mathf.Max(50, (rect.width - 4) / 3);
            EditorGUIUtility.labelWidth = 50;
            ToggleLeft(rect, m_UseContentMinWidth, EditorGUIUtility.TrTextContent("Width"));
            rect.x += rect.width + 2;
            ToggleLeft(rect, m_UseContentMinHeight, EditorGUIUtility.TrTextContent("Height"));
            EditorGUIUtility.labelWidth = 0;

            rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, EditorGUIUtility.TrTextContent("Use Content Preferred"));
            rect.width = Mathf.Max(50, (rect.width - 4) / 3);
            EditorGUIUtility.labelWidth = 50;
            ToggleLeft(rect, m_UseContentPreferredWidth, EditorGUIUtility.TrTextContent("Width"));
            rect.x += rect.width + 2;
            ToggleLeft(rect, m_UseContentPreferredHeight, EditorGUIUtility.TrTextContent("Height"));
            EditorGUIUtility.labelWidth = 0;

            serializedObject.ApplyModifiedProperties();
        }

        void ToggleLeft(Rect position, SerializedProperty property, GUIContent label)
        {
            bool toggle = property.boolValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            toggle = EditorGUI.ToggleLeft(position, label, toggle);
            EditorGUI.indentLevel = oldIndent;
            if (EditorGUI.EndChangeCheck())
            {
                property.boolValue = property.hasMultipleDifferentValues ? true : !property.boolValue;
            }
            EditorGUI.showMixedValue = false;
        }
    }
}
#endif
