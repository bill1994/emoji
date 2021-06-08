#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HorizontalOrVerticalLayoutGroupEx), true)]
    public class HorizontalOrVerticalLayoutGroupExEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        SerializedProperty m_ReverseOrder = null;
        SerializedProperty m_ForceExpandMode = null;

        SerializedProperty m_InnerAlign;
        SerializedProperty m_MaxInnerWidth;
        SerializedProperty m_MaxInnerHeight;

        protected override void OnEnable()
        {
            m_InnerAlign = serializedObject.FindProperty("m_InnerAlign");
            m_MaxInnerWidth = serializedObject.FindProperty("m_MaxInnerWidth");
            m_MaxInnerHeight = serializedObject.FindProperty("m_MaxInnerHeight");

            m_ReverseOrder = serializedObject.FindProperty("m_ReverseOrder");
            m_ForceExpandMode = serializedObject.FindProperty("m_ForceExpandMode");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ReverseOrder);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ForceExpandMode);

            EditorGUILayout.Space();
            m_InnerAlign.isExpanded = EditorGUILayout.Foldout(m_InnerAlign.isExpanded, "Inner Layout Options", true);

            if (m_InnerAlign.isExpanded)
            {
                EditorGUI.indentLevel++;
                LayoutElementField(m_MaxInnerWidth, t => t.rect.width);
                LayoutElementField(m_MaxInnerHeight, t => t.rect.height);
                EditorGUILayout.PropertyField(m_InnerAlign);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
        protected void LayoutElementField(SerializedProperty property, float defaultValue)
        {
            LayoutElementField(property, _ => defaultValue);
        }

        protected void LayoutElementField(SerializedProperty property, GUIContent contentLabel, float defaultValue)
        {
            LayoutElementField(property, contentLabel, _ => defaultValue);
        }

        protected void LayoutElementField(SerializedProperty property, System.Func<RectTransform, float> defaultValue)
        {
            LayoutElementField(property, null, defaultValue);
        }

        protected void LayoutElementField(SerializedProperty property, GUIContent contentLabel, System.Func<RectTransform, float> defaultValue)
        {
            Rect position = EditorGUILayout.GetControlRect();

            // Label
            GUIContent label = EditorGUI.BeginProperty(position, contentLabel, property);

            // Rects
            Rect fieldPosition = EditorGUI.PrefixLabel(position, label);

            Rect toggleRect = new Rect(fieldPosition);
            toggleRect.xMin -= (16 * EditorGUI.indentLevel);
            toggleRect.width = (16 * (1 + EditorGUI.indentLevel));

            Rect floatFieldRect = new Rect(position);
            floatFieldRect.xMin = toggleRect.xMin + (16 * (1 + EditorGUI.indentLevel));

            // Checkbox
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUI.ToggleLeft(toggleRect, GUIContent.none, property.floatValue >= 0);
            if (EditorGUI.EndChangeCheck())
            {
                // This could be made better to set all of the targets to their initial width, but mimizing code change for now
                property.floatValue = (enabled ? defaultValue((target as MonoBehaviour).transform as RectTransform) : -1);
            }

            if (!property.hasMultipleDifferentValues && property.floatValue >= 0)
            {
                // Float field
                EditorGUIUtility.labelWidth = 4; // Small invisible label area for drag zone functionality
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUI.FloatField(floatFieldRect, new GUIContent(" "), property.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    property.floatValue = Mathf.Max(0, newValue);
                }
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUI.EndProperty();
        }

        protected void LayoutIntElementField(SerializedProperty property, int defaultValue)
        {
            LayoutIntElementField(property, null, _ => defaultValue);
        }

        protected void LayoutIntElementField(SerializedProperty property, GUIContent contentLabel, int defaultValue)
        {
            LayoutIntElementField(property, contentLabel, _ => defaultValue);
        }

        protected void LayoutIntElementField(SerializedProperty property, GUIContent contentLabel, System.Func<RectTransform, int> defaultValue)
        {
            Rect position = EditorGUILayout.GetControlRect();

            // Label
            GUIContent label = EditorGUI.BeginProperty(position, contentLabel, property);

            // Rects
            Rect fieldPosition = EditorGUI.PrefixLabel(position, label);

            Rect toggleRect = new Rect(fieldPosition);
            toggleRect.xMin -= (16 * EditorGUI.indentLevel);
            toggleRect.width = (16 * (1 + EditorGUI.indentLevel));

            Rect intFieldRect = new Rect(position);
            intFieldRect.xMin = toggleRect.xMin + (16 * (1 + EditorGUI.indentLevel));

            // Checkbox
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUI.ToggleLeft(toggleRect, GUIContent.none, property.intValue >= 0);
            if (EditorGUI.EndChangeCheck())
            {
                // This could be made better to set all of the targets to their initial width, but mimizing code change for now
                property.intValue = (enabled ? defaultValue((target as MonoBehaviour).transform as RectTransform) : -1);
            }

            if (!property.hasMultipleDifferentValues && property.intValue >= 0)
            {
                // Int field
                EditorGUIUtility.labelWidth = 4; // Small invisible label area for drag zone functionality
                EditorGUI.BeginChangeCheck();
                int newValue = EditorGUI.IntField(intFieldRect, new GUIContent(" "), property.intValue);
                if (EditorGUI.EndChangeCheck())
                {
                    property.intValue = Mathf.Max(0, newValue);
                }
                EditorGUIUtility.labelWidth = 0;
            }

            EditorGUI.EndProperty();
        }
    }
}
#endif
