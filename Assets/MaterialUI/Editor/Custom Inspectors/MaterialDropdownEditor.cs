using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static MaterialUI.MaterialDropdown;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(DialogRadioAddress), true)]
    public class DialogRadioAddressDrawer : GenericAssetAddressDrawer<DialogRadioList>
    {
    }

    [CustomEditor(typeof(MaterialDropdown))]
    public class MaterialDropdownEditor : BaseStyleElementEditor
    {
        private SerializedProperty m_UIShowTriggerMode;
        private SerializedProperty m_UIHideTriggerMode;

        private SerializedProperty m_SpinnerMode;
        private SerializedProperty m_OpenDialogAsync;
        private SerializedProperty m_DropdownOffset;
        private SerializedProperty m_DropdownExpandPivot;
        private SerializedProperty m_DropdownFramePivot;
        private SerializedProperty m_DropdownFramePreferredSize;
        private SerializedProperty m_CustomFramePrefabAddress;
        private SerializedProperty m_AlwaysDisplayHintOption;

        private SerializedProperty m_TextComponent;
        private SerializedProperty m_IconComponent;

        private SerializedProperty m_HintTextComponent;
        private SerializedProperty m_HintIconComponent;

        private SerializedProperty m_AllowSwitchOff;
        private SerializedProperty m_SelectedIndex;

        private SerializedProperty OnCancelCallback;
        private SerializedProperty OnItemSelected;

        SerializedProperty m_OptionDataList = null;
        SerializedProperty m_HintOption = null;

        protected override void OnEnable()
        {
            OnBaseEnable();
            m_OptionDataList = serializedObject.FindProperty("m_OptionDataList");
            m_HintOption = serializedObject.FindProperty("m_HintOption");

            m_UIShowTriggerMode = serializedObject.FindProperty("m_UIShowTriggerMode");
            m_UIHideTriggerMode = serializedObject.FindProperty("m_UIHideTriggerMode");

            m_SpinnerMode = serializedObject.FindProperty("m_SpinnerMode");
            m_OpenDialogAsync = serializedObject.FindProperty("m_OpenDialogAsync");
            m_DropdownOffset = serializedObject.FindProperty("m_DropdownOffset");
            m_DropdownExpandPivot = serializedObject.FindProperty("m_DropdownExpandPivot");
            m_DropdownFramePivot = serializedObject.FindProperty("m_DropdownFramePivot");
            m_DropdownFramePreferredSize = serializedObject.FindProperty("m_DropdownFramePreferredSize");
            m_CustomFramePrefabAddress = serializedObject.FindProperty("m_CustomFramePrefabAddress");
            m_AlwaysDisplayHintOption = serializedObject.FindProperty("m_AlwaysDisplayHintOption");

            m_TextComponent = serializedObject.FindProperty("m_TextComponent");
            m_IconComponent = serializedObject.FindProperty("m_IconComponent");

            m_HintTextComponent = serializedObject.FindProperty("m_HintTextComponent");
            m_HintIconComponent = serializedObject.FindProperty("m_HintIconComponent");

            m_SelectedIndex = serializedObject.FindProperty("m_SelectedIndex");
            m_AllowSwitchOff = serializedObject.FindProperty("m_AllowSwitchOff");

            OnCancelCallback = serializedObject.FindProperty("OnCancelCallback");
            OnItemSelected = serializedObject.FindProperty("OnItemSelected");
        }

        protected override void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_UIShowTriggerMode);
            EditorGUILayout.PropertyField(m_UIHideTriggerMode);
            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_AllowSwitchOff);
            EditorGUILayout.IntSlider(m_SelectedIndex, -1, m_OptionDataList.FindPropertyRelative("m_Options").arraySize - 1);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_CustomFramePrefabAddress, new GUIContent("Custom Address"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_SpinnerMode);

            EditorGUI.indentLevel++;
            if (m_SpinnerMode.enumValueIndex != 1)
            {
                LayoutStyle_PropertyField(m_DropdownOffset);
                LayoutStyle_PropertyField(m_DropdownExpandPivot);
                LayoutStyle_PropertyField(m_DropdownFramePivot);
                LayoutStyle_PropertyField(m_DropdownFramePreferredSize);
                
            }
            if (m_SpinnerMode.enumValueIndex != 2)
            {
                LayoutStyle_PropertyField(m_OpenDialogAsync);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            DrawFoldoutComponents(ComponentsSection);

            EditorGUILayout.Space();
            OptionsSection();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(OnItemSelected);
            EditorGUILayout.PropertyField(OnCancelCallback);

            EditorGUILayout.Space();
            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }

        private void OptionsSection()
        {
            m_HintOption.FindPropertyRelative("m_ImageData.m_ImageDataType").enumValueIndex = m_OptionDataList.FindPropertyRelative("m_ImageType").enumValueIndex;
            EditorGUILayout.PropertyField(m_HintOption);
            EditorGUILayout.PropertyField(m_AlwaysDisplayHintOption);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OptionDataList);
        }

        private void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_TextComponent);
            LayoutStyle_PropertyField(m_IconComponent);
            LayoutStyle_PropertyField(m_HintTextComponent);
            LayoutStyle_PropertyField(m_HintIconComponent);
            EditorGUI.indentLevel--;
        }
    }
}
