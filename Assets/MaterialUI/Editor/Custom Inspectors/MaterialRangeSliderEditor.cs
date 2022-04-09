// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEditor;
using UnityEngine;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaterialRangeSlider))]
    public class MaterialRangeSliderEditor : BaseStyleElementEditor
    {
        private MaterialRangeSlider m_MaterialRangeSlider;

        private SerializedProperty m_AnimationScale;
        private SerializedProperty m_Interactable;
        protected SerializedProperty m_AnimationDuration;

        private SerializedProperty m_HasPopup;
        private SerializedProperty m_HasDots;

        private SerializedProperty m_EnabledColor;
        private SerializedProperty m_DisabledColor;
        private SerializedProperty m_BackgroundColor;

        private SerializedProperty m_HandleLowGraphic;
        private SerializedProperty m_PopupLowTransform;
        private SerializedProperty m_PopupLowText;
        private SerializedProperty m_ValueLowText;
        private SerializedProperty m_InputFieldLow;

        private SerializedProperty m_HandleHighGraphic;
        private SerializedProperty m_PopupHighTransform;
        private SerializedProperty m_PopupHighText;
        private SerializedProperty m_ValueHighText;
        private SerializedProperty m_InputFieldHigh;

        private SerializedProperty m_BackgroundGraphic;

        private SerializedProperty m_SliderContentTransform;

        private SerializedProperty m_DotContentTransform;
        private SerializedProperty m_DotTemplateIcon;

        protected override void OnEnable()
        {
            OnBaseEnable();

            m_MaterialRangeSlider = (MaterialRangeSlider)target;

            m_AnimationScale = serializedObject.FindProperty("m_AnimationScale");
            m_Interactable = serializedObject.FindProperty("m_Interactable");
            m_AnimationDuration = serializedObject.FindProperty("m_AnimationDuration");

            m_HasPopup = serializedObject.FindProperty("m_HasPopup");
            m_HasDots = serializedObject.FindProperty("m_HasDots");
            m_EnabledColor = serializedObject.FindProperty("m_EnabledColor");
            m_DisabledColor = serializedObject.FindProperty("m_DisabledColor");
            m_BackgroundColor = serializedObject.FindProperty("m_BackgroundColor");

            m_HandleLowGraphic = serializedObject.FindProperty("m_HandleLowGraphic");
            m_PopupLowTransform = serializedObject.FindProperty("m_PopupLowTransform");
            m_PopupLowText = serializedObject.FindProperty("m_PopupLowText");
            m_ValueLowText = serializedObject.FindProperty("m_ValueLowText");
            m_InputFieldLow = serializedObject.FindProperty("m_InputFieldLow");

            m_HandleHighGraphic = serializedObject.FindProperty("m_HandleHighGraphic");
            m_PopupHighTransform = serializedObject.FindProperty("m_PopupHighTransform");
            m_PopupHighText = serializedObject.FindProperty("m_PopupHighText");
            m_ValueHighText = serializedObject.FindProperty("m_ValueHighText");
            m_InputFieldHigh = serializedObject.FindProperty("m_InputFieldHigh");

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
                m_MaterialRangeSlider.interactable = m_Interactable.boolValue;
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

            using (new DisabledScope(!m_MaterialRangeSlider.slider.wholeNumbers))
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
            LayoutStyle_PropertyField(m_HandleLowGraphic);
            LayoutStyle_PropertyField(m_PopupLowTransform);
            LayoutStyle_PropertyField(m_PopupLowText);
            LayoutStyle_PropertyField(m_ValueLowText);
            LayoutStyle_PropertyField(m_InputFieldLow);

            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_HandleHighGraphic);
            LayoutStyle_PropertyField(m_PopupHighTransform);
            LayoutStyle_PropertyField(m_PopupHighText);
            LayoutStyle_PropertyField(m_ValueHighText);
            LayoutStyle_PropertyField(m_InputFieldHigh);

            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_BackgroundGraphic);
            LayoutStyle_PropertyField(m_SliderContentTransform);
            LayoutStyle_PropertyField(m_DotContentTransform);
            LayoutStyle_PropertyField(m_DotTemplateIcon);
            EditorGUI.indentLevel--;
        }
    }
}