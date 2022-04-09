// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEditor;
using UnityEngine;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaterialSlider))]
    public class MaterialSliderEditor : BaseStyleElementEditor
    {
        private MaterialSlider m_MaterialSlider;

        private SerializedProperty m_AnimationScale;
        private SerializedProperty m_Interactable;
        protected SerializedProperty m_AnimationDuration;

        private SerializedProperty m_HasPopup;
        private SerializedProperty m_HasDots;

        private SerializedProperty m_EnabledColor;
        private SerializedProperty m_DisabledColor;
        private SerializedProperty m_BackgroundColor;

        private SerializedProperty m_HandleGraphic;

        private SerializedProperty m_PopupTransform;
        private SerializedProperty m_PopupText;

        private SerializedProperty m_ValueText;
        private SerializedProperty m_InputField;

        private SerializedProperty m_BackgroundGraphic;

        private SerializedProperty m_SliderContentTransform;

        private SerializedProperty m_DotContentTransform;
        private SerializedProperty m_DotTemplateIcon;

        protected override void OnEnable()
        {
            OnBaseEnable();

            m_MaterialSlider = (MaterialSlider)target;

            m_AnimationScale = serializedObject.FindProperty("m_AnimationScale");
            m_Interactable = serializedObject.FindProperty("m_Interactable");
            m_AnimationDuration = serializedObject.FindProperty("m_AnimationDuration");

            m_HasPopup = serializedObject.FindProperty("m_HasPopup");
            m_HasDots = serializedObject.FindProperty("m_HasDots");
            m_EnabledColor = serializedObject.FindProperty("m_EnabledColor");
            m_DisabledColor = serializedObject.FindProperty("m_DisabledColor");
            m_BackgroundColor = serializedObject.FindProperty("m_BackgroundColor");
            m_HandleGraphic = serializedObject.FindProperty("m_HandleGraphic");
            m_PopupTransform = serializedObject.FindProperty("m_PopupTransform");
            m_PopupText = serializedObject.FindProperty("m_PopupText");
            m_ValueText = serializedObject.FindProperty("m_ValueText");
            m_InputField = serializedObject.FindProperty("m_InputField");
            m_BackgroundGraphic = serializedObject.FindProperty("m_BackgroundGraphic");
            m_SliderContentTransform = serializedObject.FindProperty("m_SliderContentTransform");
            m_DotContentTransform = serializedObject.FindProperty("m_DotContentTransform");
            m_DotTemplateIcon = serializedObject.FindProperty("m_DotTemplateIcon");
        }

        protected override void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
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
            LayoutStyle_PropertyField(m_AnimationScale);
            LayoutStyle_PropertyField(m_AnimationDuration);
            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_HasPopup);

            using (new DisabledScope(!m_MaterialSlider.slider.wholeNumbers))
            {
                LayoutStyle_PropertyField(m_HasDots);
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
            LayoutStyle_PropertyField(m_PopupTransform);
            LayoutStyle_PropertyField(m_PopupText);
            LayoutStyle_PropertyField(m_ValueText);
            LayoutStyle_PropertyField(m_InputField);

            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_BackgroundGraphic);
            LayoutStyle_PropertyField(m_SliderContentTransform);
            LayoutStyle_PropertyField(m_DotContentTransform);
            LayoutStyle_PropertyField(m_DotTemplateIcon);
            EditorGUI.indentLevel--;
        }
    }
}