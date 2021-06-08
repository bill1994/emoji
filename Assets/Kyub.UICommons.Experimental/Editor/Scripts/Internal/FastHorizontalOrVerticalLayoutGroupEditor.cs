using UnityEngine;
using UnityEngine.UI;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
using Kyub.UI.Experimental;
using UnityEditor;

namespace KyubEditor.UI.Experimental
{
    [CustomEditor(typeof(FastHorizontalOrVerticalLayoutGroup), true)]
    [CanEditMultipleObjects]
    /// <summary>
    /// Custom Editor for the HorizontalOrVerticalLayoutGroupEditor Component.
    /// Extend this class to write a custom editor for a component derived from HorizontalOrVerticalLayoutGroupEditor.
    /// </summary>
    public class FastHorizontalOrVerticalLayoutGroupEditor : Editor
    {
        SerializedProperty m_Padding;
        SerializedProperty m_Spacing;
        SerializedProperty m_ChildAlignment;
        SerializedProperty m_ChildControlWidth;
        SerializedProperty m_ChildControlHeight;
        SerializedProperty m_ChildScaleWidth;
        SerializedProperty m_ChildScaleHeight;
        SerializedProperty m_ChildForceExpandWidth;
        SerializedProperty m_ChildForceExpandHeight;
        SerializedProperty m_ReverseOrder;
        SerializedProperty m_ForceExpandMode;

        SerializedProperty m_InnerAlign;
        SerializedProperty m_MaxInnerWidth;
        SerializedProperty m_MaxInnerHeight;

        protected virtual void OnEnable()
        {
            m_InnerAlign = serializedObject.FindProperty("m_InnerAlign");
            m_MaxInnerWidth = serializedObject.FindProperty("m_MaxInnerWidth");
            m_MaxInnerHeight = serializedObject.FindProperty("m_MaxInnerHeight");

            m_Padding = serializedObject.FindProperty("m_Padding");
            m_Spacing = serializedObject.FindProperty("m_Spacing");
            m_ChildAlignment = serializedObject.FindProperty("m_ChildAlignment");
            m_ChildControlWidth = serializedObject.FindProperty("m_ChildControlWidth");
            m_ChildControlHeight = serializedObject.FindProperty("m_ChildControlHeight");
            m_ChildScaleWidth = serializedObject.FindProperty("m_ChildScaleWidth");
            m_ChildScaleHeight = serializedObject.FindProperty("m_ChildScaleHeight");
            m_ChildForceExpandWidth = serializedObject.FindProperty("m_ChildForceExpandWidth");
            m_ChildForceExpandHeight = serializedObject.FindProperty("m_ChildForceExpandHeight");
            m_ReverseOrder = serializedObject.FindProperty("m_ReverseOrder");
            m_ForceExpandMode = serializedObject.FindProperty("m_ForceExpandMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ReverseOrder);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_Padding, true);
            EditorGUILayout.PropertyField(m_Spacing, true);
            EditorGUILayout.PropertyField(m_ChildAlignment, true);

            EditorGUILayout.PropertyField(m_ForceExpandMode);
            Rect rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, EditorGUIUtility.TrTextContent("Control Child Size"));
            rect.width = Mathf.Max(50, (rect.width - 4) / 3);
            EditorGUIUtility.labelWidth = 50;
            ToggleLeft(rect, m_ChildControlWidth, EditorGUIUtility.TrTextContent("Width"));
            rect.x += rect.width + 2;
            ToggleLeft(rect, m_ChildControlHeight, EditorGUIUtility.TrTextContent("Height"));
            EditorGUIUtility.labelWidth = 0;

            rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, EditorGUIUtility.TrTextContent("Use Child Scale"));
            rect.width = Mathf.Max(50, (rect.width - 4) / 3);
            EditorGUIUtility.labelWidth = 50;
            ToggleLeft(rect, m_ChildScaleWidth, EditorGUIUtility.TrTextContent("Width"));
            rect.x += rect.width + 2;
            ToggleLeft(rect, m_ChildScaleHeight, EditorGUIUtility.TrTextContent("Height"));
            EditorGUIUtility.labelWidth = 0;

            rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, -1, EditorGUIUtility.TrTextContent("Child Force Expand"));
            rect.width = Mathf.Max(50, (rect.width - 4) / 3);
            EditorGUIUtility.labelWidth = 50;
            ToggleLeft(rect, m_ChildForceExpandWidth, EditorGUIUtility.TrTextContent("Width"));
            rect.x += rect.width + 2;
            ToggleLeft(rect, m_ChildForceExpandHeight, EditorGUIUtility.TrTextContent("Height"));
            EditorGUIUtility.labelWidth = 0;

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
