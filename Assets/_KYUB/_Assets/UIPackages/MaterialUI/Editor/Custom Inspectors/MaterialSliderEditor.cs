//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;
using UnityEngine;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaterialSlider))]
    public class MaterialSliderEditor : BaseStyleElementEditor
    {
        private MaterialSlider m_MaterialSlider;

        private SerializedProperty m_Interactable;
        protected SerializedProperty m_AnimationDuration;

        private SerializedProperty m_HasPopup;
        private SerializedProperty m_HasDots;
        private SerializedProperty m_HasManualPreferredWidth;
        private SerializedProperty m_ManualPreferredWidth;

        private SerializedProperty m_EnabledColor;
        private SerializedProperty m_DisabledColor;
        private SerializedProperty m_BackgroundColor;

        private SerializedProperty m_LowLeftDisabledOpacity;
        private SerializedProperty m_LowRightDisabledOpacity;

        private SerializedProperty m_SliderHandleTransform;
        private SerializedProperty m_HandleGraphic;
        private SerializedProperty m_FillTransform;

        private SerializedProperty m_PopupTransform;
        private SerializedProperty m_PopupText;

        private SerializedProperty m_ValueText;
        private SerializedProperty m_InputField;

        private SerializedProperty m_BackgroundGraphic;

        private SerializedProperty m_LeftContentTransform;
        private SerializedProperty m_RightContentTransform;
        private SerializedProperty m_SliderContentTransform;

        private SerializedProperty m_DotTemplateIcon;

        protected override void OnEnable()
        {
            OnBaseEnable();

            m_MaterialSlider = (MaterialSlider)target;

            m_Interactable = serializedObject.FindProperty("m_Interactable");
            m_AnimationDuration = serializedObject.FindProperty("m_AnimationDuration");

            m_HasPopup = serializedObject.FindProperty("m_HasPopup");
            m_HasDots = serializedObject.FindProperty("m_HasDots");
            m_HasManualPreferredWidth = serializedObject.FindProperty("m_HasManualPreferredWidth");
            m_ManualPreferredWidth = serializedObject.FindProperty("m_ManualPreferredWidth");
            m_EnabledColor = serializedObject.FindProperty("m_EnabledColor");
            m_DisabledColor = serializedObject.FindProperty("m_DisabledColor");
            m_BackgroundColor = serializedObject.FindProperty("m_BackgroundColor");
            m_LowLeftDisabledOpacity = serializedObject.FindProperty("m_LowLeftDisabledOpacity");
            m_LowRightDisabledOpacity = serializedObject.FindProperty("m_LowRightDisabledOpacity");
            m_HandleGraphic = serializedObject.FindProperty("m_HandleGraphic");
            m_FillTransform = serializedObject.FindProperty("m_FillTransform");
            m_SliderHandleTransform = serializedObject.FindProperty("m_SliderHandleTransform");
            m_PopupTransform = serializedObject.FindProperty("m_PopupTransform");
            m_PopupText = serializedObject.FindProperty("m_PopupText");
            m_ValueText = serializedObject.FindProperty("m_ValueText");
            m_InputField = serializedObject.FindProperty("m_InputField");
            m_BackgroundGraphic = serializedObject.FindProperty("m_BackgroundGraphic");
            m_LeftContentTransform = serializedObject.FindProperty("m_LeftContentTransform");
            m_RightContentTransform = serializedObject.FindProperty("m_RightContentTransform");
            m_SliderContentTransform = serializedObject.FindProperty("m_SliderContentTransform");
            m_DotTemplateIcon = serializedObject.FindProperty("m_DotTemplateIcon");
        }

        protected override void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
            m_MaterialSlider.OnBeforeValidate();

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                Undo.RecordObject(serializedObject.targetObject, "");
                LayoutStyle_PropertyField(m_Interactable);
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_MaterialSlider.interactable = m_Interactable.boolValue;
            }
            else
            {
                Undo.ClearUndo(serializedObject.targetObject);
            }

            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_AnimationDuration);
            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_HasPopup);

            using (new DisabledScope(!m_MaterialSlider.slider.wholeNumbers))
            {
                LayoutStyle_PropertyField(m_HasDots);
            }

            using (new GUILayout.HorizontalScope())
            {
                LayoutStyle_PropertyField(m_HasManualPreferredWidth, new GUIContent("Manual Preferred Width"));
                if (m_HasManualPreferredWidth.boolValue)
                {
                    LayoutStyle_PropertyField(m_ManualPreferredWidth, new GUIContent(""));
                }
            }

            if (m_LeftContentTransform.objectReferenceValue != null)
            {
                LayoutStyle_PropertyField(m_LowLeftDisabledOpacity);
            }

            if (m_RightContentTransform.objectReferenceValue != null)
            {
                LayoutStyle_PropertyField(m_LowRightDisabledOpacity);
            }

            DrawFoldoutColors(ColorsSection);
            DrawFoldoutComponents(ComponentsSection);

            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }

        private void ColorsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_EnabledColor);
            LayoutStyle_PropertyField(m_DisabledColor);
            LayoutStyle_PropertyField(m_BackgroundColor);
            EditorGUI.indentLevel--;
        }

        private void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_HandleGraphic);
            LayoutStyle_PropertyField(m_FillTransform);
            LayoutStyle_PropertyField(m_SliderHandleTransform);
            LayoutStyle_PropertyField(m_PopupTransform);
            LayoutStyle_PropertyField(m_PopupText);
            LayoutStyle_PropertyField(m_ValueText);
            LayoutStyle_PropertyField(m_InputField);
            LayoutStyle_PropertyField(m_BackgroundGraphic);
            LayoutStyle_PropertyField(m_LeftContentTransform);
            LayoutStyle_PropertyField(m_RightContentTransform);
            LayoutStyle_PropertyField(m_SliderContentTransform);
            LayoutStyle_PropertyField(m_DotTemplateIcon);
            EditorGUI.indentLevel--;
        }
    }
}