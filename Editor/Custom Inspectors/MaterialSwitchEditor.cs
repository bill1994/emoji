//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MaterialSwitch))]
    class MaterialSwitchEditor : MaterialToggleBaseEditor
    {
        private SerializedProperty m_Trail;
        private SerializedProperty m_SlideDirection;
        private SerializedProperty m_SlideSwitch;

        private SerializedProperty m_SwitchImage;
        private SerializedProperty m_BackImage;
        private SerializedProperty m_ShadowImage;

        private SerializedProperty m_OnColor;
        private SerializedProperty m_OffColor;
        private SerializedProperty m_DisabledColor;

        private SerializedProperty m_BackOnColor;
        private SerializedProperty m_BackOffColor;
        private SerializedProperty m_BackDisabledColor;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_Trail = serializedObject.FindProperty("m_Trail");
            m_SlideSwitch = serializedObject.FindProperty("m_SlideSwitch");
            m_SlideDirection = serializedObject.FindProperty("m_SlideDirection");

            m_SwitchImage = serializedObject.FindProperty("m_SwitchImage");
            m_BackImage = serializedObject.FindProperty("m_BackImage");
            m_ShadowImage = serializedObject.FindProperty("m_ShadowImage");

            m_OnColor = serializedObject.FindProperty("m_OnColor");
            m_OffColor = serializedObject.FindProperty("m_OffColor");
            m_DisabledColor = serializedObject.FindProperty("m_DisabledColor");

            m_BackOnColor = serializedObject.FindProperty("m_BackOnColor");
            m_BackOffColor = serializedObject.FindProperty("m_BackOffColor");
            m_BackDisabledColor = serializedObject.FindProperty("m_BackDisabledColor");
        }

        protected override void InheritedFieldsSection()
        {
            LayoutStyle_PropertyField(m_SlideSwitch);
            LayoutStyle_PropertyField(m_SlideDirection);
        }

        protected override void ColorsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_OnColor);
            LayoutStyle_PropertyField(m_OffColor);
            LayoutStyle_PropertyField(m_DisabledColor);
            LayoutStyle_PropertyField(m_BackOnColor);
            LayoutStyle_PropertyField(m_BackOffColor);
            LayoutStyle_PropertyField(m_BackDisabledColor);
            EditorGUI.indentLevel--;

            base.ColorsSection();
        }

        protected override void ComponentsSection()
        {
            base.ComponentsSection();

            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_SwitchImage);
            LayoutStyle_PropertyField(m_BackImage);
            LayoutStyle_PropertyField(m_ShadowImage);
            LayoutStyle_PropertyField(m_Trail);
            EditorGUI.indentLevel--;
        }
    }
}