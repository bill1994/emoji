#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NativeInputField))]
    public class NativeInputFieldEditor : UnityEditor.UI.InputFieldEditor
    {
        private SerializedProperty m_AsteriskChar;
        private SerializedProperty m_OnReturnPressed;
        private SerializedProperty m_TextViewport;
        private SerializedProperty m_PanContent;

        private SerializedProperty m_ShowDoneButton;
        private SerializedProperty m_ShowClearButton;
        private SerializedProperty m_ReturnKeyType;

        private SerializedProperty m_TextComponent;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_TextViewport = serializedObject.FindProperty("m_TextViewport");
            m_AsteriskChar = serializedObject.FindProperty("m_AsteriskChar");
            m_PanContent = serializedObject.FindProperty("m_PanContent");
            m_OnReturnPressed = serializedObject.FindProperty("OnReturnPressed");

            m_ShowDoneButton = serializedObject.FindProperty("m_ShowDoneButton");
            m_ShowClearButton = serializedObject.FindProperty("m_ShowClearButton");
            m_ReturnKeyType = serializedObject.FindProperty("m_ReturnKeyType");

            m_TextComponent = serializedObject.FindProperty("m_TextComponent");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(m_TextComponent == null || m_TextComponent.objectReferenceValue == null);

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Viewport Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_TextViewport);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Password Special Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AsteriskChar);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Virtual Keyboard Layout Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PanContent);

            OnGUIExtraNativeFields();
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();
        }

        public virtual void OnGUIExtraNativeFields()
        {
            GUILayout.Space(20);
            GUILayout.Label("Native Keyboard Return Type", EditorStyles.boldLabel);
            m_ReturnKeyType.enumValueIndex = GUILayout.Toolbar((int)m_ReturnKeyType.enumValueIndex, new string[] { "Default", "Next", "Done", "Search" });
            EditorGUILayout.Space();
            GUILayout.Label("Native Keyboard Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ShowDoneButton, new GUIContent("Show \"Done\" button"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_ShowClearButton, new GUIContent("Show \"Clear\" button"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OnReturnPressed);
        }
    }
}
#endif
