//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;
using UnityEngine;

namespace MaterialUI
{
    [CustomEditor(typeof(TabPage))]
    public class TabPageEditor : Editor
    {
		private SerializedProperty m_Interactable;
		private SerializedProperty m_DisableWhenNotVisible;
        private SerializedProperty m_TabName;
        private SerializedProperty m_TabIcon;
        private SerializedProperty m_TabIconType;

        private SerializedProperty m_OnShow;
        private SerializedProperty m_OnHide;

        void OnEnable()
        {
			m_Interactable = serializedObject.FindProperty("m_Interactable");
			m_DisableWhenNotVisible = serializedObject.FindProperty("m_DisableWhenNotVisible");
            m_TabName = serializedObject.FindProperty("m_TabName");
            m_TabIcon = serializedObject.FindProperty("m_TabIcon");
            m_TabIconType = serializedObject.FindProperty("m_TabIcon.m_ImageDataType");

            m_OnShow = serializedObject.FindProperty("OnShow");
            m_OnHide = serializedObject.FindProperty("OnHide");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

			EditorGUILayout.PropertyField(m_Interactable);
			EditorGUILayout.PropertyField(m_DisableWhenNotVisible);
            EditorGUILayout.PropertyField(m_TabName);
            EditorGUILayout.PropertyField(m_TabIconType, new GUIContent("Tab Icon Type"));
            EditorGUILayout.PropertyField(m_TabIcon);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OnShow);
            EditorGUILayout.PropertyField(m_OnHide);
            serializedObject.ApplyModifiedProperties();
        }
    }
}