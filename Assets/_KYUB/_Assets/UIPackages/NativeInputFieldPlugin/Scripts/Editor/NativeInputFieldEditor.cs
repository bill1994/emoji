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

        protected override void OnEnable()
        {
            base.OnEnable();
            m_TextViewport = serializedObject.FindProperty("m_TextViewport");
            m_AsteriskChar = serializedObject.FindProperty("m_AsteriskChar");
            m_PanContent = serializedObject.FindProperty("m_PanContent");
            m_OnReturnPressed = serializedObject.FindProperty("OnReturnPressed");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Viewport Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_TextViewport);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Password Special Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AsteriskChar);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Virtual Keyboard Layout Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PanContent);

            GUILayout.Space(5);
            EditorGUILayout.PropertyField(m_OnReturnPressed);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
