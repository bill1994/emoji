﻿#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(MaxLayoutElement), true)]
    [CanEditMultipleObjects]
    public class MaxLayoutElementEditor : LayoutElementEditor
    {
        SerializedProperty m_IgnoreLayout;
        SerializedProperty m_MinWidth;
        SerializedProperty m_MinHeight;
        SerializedProperty m_PreferredWidth;
        SerializedProperty m_PreferredHeight;
        SerializedProperty m_FlexibleWidth;
        SerializedProperty m_FlexibleHeight;
        SerializedProperty m_LayoutPriority;

        SerializedProperty m_MaxWidthMode;
        SerializedProperty m_MaxHeightMode;
        SerializedProperty m_MaxWidth;
        SerializedProperty m_MaxHeight;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_IgnoreLayout = serializedObject.FindProperty("m_IgnoreLayout");
            m_MinWidth = serializedObject.FindProperty("m_MinWidth");
            m_MinHeight = serializedObject.FindProperty("m_MinHeight");
            m_PreferredWidth = serializedObject.FindProperty("m_PreferredWidth");
            m_PreferredHeight = serializedObject.FindProperty("m_PreferredHeight");
            m_FlexibleWidth = serializedObject.FindProperty("m_FlexibleWidth");
            m_FlexibleHeight = serializedObject.FindProperty("m_FlexibleHeight");
            m_LayoutPriority = serializedObject.FindProperty("m_LayoutPriority");

            m_MaxWidthMode = serializedObject.FindProperty("m_MaxWidthMode");
            m_MaxHeightMode = serializedObject.FindProperty("m_MaxHeightMode");
            m_MaxWidth = serializedObject.FindProperty("m_MaxWidth");
            m_MaxHeight = serializedObject.FindProperty("m_MaxHeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IgnoreLayout);

            if (!m_IgnoreLayout.boolValue)
            {
                EditorGUILayout.Space();

                LayoutElementField(m_MinWidth, 0);
                LayoutElementField(m_MinHeight, 0);
                LayoutElementField(m_PreferredWidth, t => t.rect.width);
                LayoutElementField(m_PreferredHeight, t => t.rect.height);
                LayoutElementField(m_FlexibleWidth, 1);
                LayoutElementField(m_FlexibleHeight, 1);
                EditorGUILayout.PropertyField(m_LayoutPriority);
                EditorGUILayout.Space();

                LayoutElementField(m_MaxWidth, t => t.rect.width);
                if (m_MaxWidth.floatValue >= 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_MaxWidthMode);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(2);
                }

                LayoutElementField(m_MaxHeight, t => t.rect.height);
                if (m_MaxHeight.floatValue >= 0)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_MaxHeightMode);
                    EditorGUI.indentLevel--;
                    EditorGUILayout.Space(2);
                }
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

            Rect toggleRect = fieldPosition;
            toggleRect.width = 16;

            Rect floatFieldRect = fieldPosition;
            floatFieldRect.xMin += 16;

            // Checkbox
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUI.ToggleLeft(toggleRect, GUIContent.none, property.floatValue >= 0);
            if (EditorGUI.EndChangeCheck())
            {
                // This could be made better to set all of the targets to their initial width, but mimizing code change for now
                property.floatValue = (enabled ? defaultValue((target as MaxLayoutElement).transform as RectTransform) : -1);
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
    }
}

#endif
