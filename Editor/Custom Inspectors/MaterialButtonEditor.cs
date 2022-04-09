// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    [CustomEditor(typeof(MaterialButton), true)]
    [CanEditMultipleObjects]
    public class MaterialButtonEditor : BaseStyleElementEditor
    {
        private MaterialButton m_SelectedMaterialButton;
        private TargetArray<MaterialButton> m_SelectedMaterialButtons;

        private SerializedProperty m_Interactable;

        private SerializedProperty m_ResetRippleOnDisable;

        private SerializedProperty m_ShadowsCanvasGroup;
        private SerializedProperty m_ContentRectTransform;
        private SerializedProperty m_BackgroundImage;
        private SerializedProperty m_Text;
        private SerializedProperty m_Icon;

        private SerializedProperty m_ContentPaddingX;
        private SerializedProperty m_ContentPaddingY;

        private SerializedProperty m_FitWidthToContent;
        private SerializedProperty m_FitHeightToContent;

        private SerializedProperty onPress;
        private SerializedProperty onClick;

        protected override void OnEnable()
        {
            OnBaseEnable();

            m_SelectedMaterialButton = (MaterialButton)target;
            m_SelectedMaterialButtons = new TargetArray<MaterialButton>(targets);

            m_Interactable = serializedObject.FindProperty("m_Interactable");

            m_ResetRippleOnDisable = serializedObject.FindProperty("m_ResetRippleOnDisable");

            m_ShadowsCanvasGroup = serializedObject.FindProperty("m_ShadowsCanvasGroup");
            m_ContentRectTransform = serializedObject.FindProperty("m_ContentRectTransform");

            m_BackgroundImage = serializedObject.FindProperty("m_BackgroundImage");
            m_Text = serializedObject.FindProperty("m_Text");
            m_Icon = serializedObject.FindProperty("m_Icon");

            m_ContentPaddingX = serializedObject.FindProperty("m_ContentPadding.x");
            m_ContentPaddingY = serializedObject.FindProperty("m_ContentPadding.y");

            m_FitWidthToContent = serializedObject.FindProperty("m_FitWidthToContent");
            m_FitHeightToContent = serializedObject.FindProperty("m_FitHeightToContent");

            onPress = serializedObject.FindProperty("onPress");
            onClick = serializedObject.FindProperty("onClick");
        }

        protected override void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            LayoutStyle_PropertyField(m_Interactable);
            if (EditorGUI.EndChangeCheck())
            {
                m_SelectedMaterialButtons.ExecuteAction(button => button.interactable = m_Interactable.boolValue);
            }

            EditorGUI.BeginChangeCheck();
            LayoutStyle_PropertyField(m_ResetRippleOnDisable);
            if (EditorGUI.EndChangeCheck())
            {
                m_SelectedMaterialButtons.ExecuteAction(button => button.resetRippleOnDisable = m_ResetRippleOnDisable.boolValue);
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                LayoutStyle_PropertyField(m_FitWidthToContent);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedMaterialButtons.ExecuteAction(button => button.ClearTracker());
                }
                if (m_FitWidthToContent.boolValue)
                {
                    EditorGUILayout.LabelField("Padding", GUILayout.Width(52));
                    LayoutStyle_PropertyField(m_ContentPaddingX, new GUIContent());
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                LayoutStyle_PropertyField(m_FitHeightToContent);
                if (EditorGUI.EndChangeCheck())
                {
                    m_SelectedMaterialButtons.ExecuteAction(button => button.ClearTracker());
                }
                if (m_FitHeightToContent.boolValue)
                {
                    EditorGUILayout.LabelField("Padding", GUILayout.Width(52));
                    LayoutStyle_PropertyField(m_ContentPaddingY, new GUIContent());
                }
            }

            //ConvertButtonSection();

            DrawFoldoutExternalProperties(ExternalPropertiesSection);

            DrawFoldoutComponents(ComponentsSection);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onPress);
            EditorGUILayout.PropertyField(onClick);

            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }

        private bool ExternalPropertiesSection()
        {
            return InspectorFields.MaterialButtonMultiField(go => go.GetComponent<MaterialButton>(), m_SelectedMaterialButton);
        }

        private void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_ContentRectTransform);
            LayoutStyle_PropertyField(m_BackgroundImage);
            LayoutStyle_PropertyField(m_ShadowsCanvasGroup);
            LayoutStyle_PropertyField(m_Text);
            LayoutStyle_PropertyField(m_Icon);
            EditorGUI.indentLevel--;
        }

        /*private void ConvertButtonSection()
        {
            GUIContent convertText = new GUIContent();

            if (m_ShadowsCanvasGroup.objectReferenceValue != null)
            {
                convertText.text = "Convert to flat button";
            }
            else
            {
                convertText.text = "Convert to raised button";
            }

            if (Selection.objects.Length > 1)
            {
                GUI.enabled = false;
                convertText.text = "Convert button";
            }

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUIUtility.labelWidth - 8f);
                if (GUILayout.Button(convertText, EditorStyles.miniButton))
                {
                    m_SelectedMaterialButton.Convert();
                }
            }

            GUI.enabled = true;
        }*/
    }
}