using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static MaterialUI.MaterialMenuSpinner;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(MaterialDialogFrameAddress), true)]
    public class MaterialDialogFrameAddressDrawer : GenericAssetAddressDrawer<MaterialDialogFrame>
    {
    }

    [CustomEditor(typeof(MaterialMenuSpinner))]
    public class MaterialMenuSpinnerEditor : BaseStyleElementEditor
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

        private SerializedProperty OnCancelCallback;
        private SerializedProperty OnShowCallback;

        protected override void OnEnable()
        {
            OnBaseEnable();
            m_UIShowTriggerMode = serializedObject.FindProperty("m_UIShowTriggerMode");
            m_UIHideTriggerMode = serializedObject.FindProperty("m_UIHideTriggerMode");

            m_SpinnerMode = serializedObject.FindProperty("m_SpinnerMode");
            m_OpenDialogAsync = serializedObject.FindProperty("m_OpenDialogAsync");
            m_DropdownOffset = serializedObject.FindProperty("m_DropdownOffset");
            m_DropdownExpandPivot = serializedObject.FindProperty("m_DropdownExpandPivot");
            m_DropdownFramePivot = serializedObject.FindProperty("m_DropdownFramePivot");
            m_DropdownFramePreferredSize = serializedObject.FindProperty("m_DropdownFramePreferredSize");
            m_CustomFramePrefabAddress = serializedObject.FindProperty("m_CustomFramePrefabAddress");
            OnCancelCallback = serializedObject.FindProperty("OnCancelCallback");
            OnShowCallback = serializedObject.FindProperty("OnShowMenuCallback");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_UIShowTriggerMode);
            EditorGUILayout.PropertyField(m_UIHideTriggerMode);
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
            EditorGUILayout.PropertyField(OnShowCallback);
            EditorGUILayout.PropertyField(OnCancelCallback);

            EditorGUILayout.Space();
            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
