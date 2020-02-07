using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static MaterialUI.MaterialMenuSpinner;
using static MaterialUI.AssetAdressEditor;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(MaterialDialogFrameAddress), true)]
    public class MaterialDialogFrameAddressDrawer : GenericAssetAddressDrawer<MaterialDialogFrame>
    {
    }

    [CustomEditor(typeof(MaterialMenuSpinner))]
    public class MaterialMenuSpinnerEditor : BaseStyleElementEditor
    {
        private SerializedProperty m_SpinnerMode;
        private SerializedProperty m_DropdownExpandPivot;
        private SerializedProperty m_DropdownFramePivot;
        private SerializedProperty m_DropdownFramePreferredSize;
        private SerializedProperty m_CustomFramePrefabAddress;

        private SerializedProperty OnCancelCallback;

        protected override void OnEnable()
        {
            OnBaseEnable();
            m_SpinnerMode = serializedObject.FindProperty("m_SpinnerMode");
            m_DropdownExpandPivot = serializedObject.FindProperty("m_DropdownExpandPivot");
            m_DropdownFramePivot = serializedObject.FindProperty("m_DropdownFramePivot");
            m_DropdownFramePreferredSize = serializedObject.FindProperty("m_DropdownFramePreferredSize");
            m_CustomFramePrefabAddress = serializedObject.FindProperty("m_CustomFramePrefabAddress");
            OnCancelCallback = serializedObject.FindProperty("OnCancelCallback");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_CustomFramePrefabAddress, new GUIContent("Custom Address"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_SpinnerMode);
            if (m_SpinnerMode.enumValueIndex != 1)
            {
                EditorGUI.indentLevel++;
                LayoutStyle_PropertyField(m_DropdownExpandPivot);
                LayoutStyle_PropertyField(m_DropdownFramePivot);
                LayoutStyle_PropertyField(m_DropdownFramePreferredSize);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(OnCancelCallback);

            EditorGUILayout.Space();
            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
