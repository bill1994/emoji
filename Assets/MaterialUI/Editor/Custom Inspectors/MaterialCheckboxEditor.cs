//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaterialCheckbox))]
    class MaterialCheckboxEditor : MaterialToggleBaseEditor
    {
        private SerializedProperty m_CheckImage;
        private SerializedProperty m_FrameImage;
        private SerializedProperty m_AnimationSize;
        private SerializedProperty m_OnColor;
        private SerializedProperty m_OffColor;
        private SerializedProperty m_DisabledColor;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_CheckImage = serializedObject.FindProperty("m_CheckImage");
            m_FrameImage = serializedObject.FindProperty("m_FrameImage");
            m_AnimationSize = serializedObject.FindProperty("m_AnimationSize");
            m_OnColor = serializedObject.FindProperty("m_OnColor");
            m_OffColor = serializedObject.FindProperty("m_OffColor");
            m_DisabledColor = serializedObject.FindProperty("m_DisabledColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LayoutStyle_PropertyField(m_AnimationSize);
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }


        protected override void ColorsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_OnColor);
            LayoutStyle_PropertyField(m_OffColor);
            LayoutStyle_PropertyField(m_DisabledColor);
            EditorGUI.indentLevel--;

            base.ColorsSection();
        }

        protected override void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_CheckImage);
            LayoutStyle_PropertyField(m_FrameImage);
            EditorGUI.indentLevel--;

            base.ComponentsSection();
        }
    }
}