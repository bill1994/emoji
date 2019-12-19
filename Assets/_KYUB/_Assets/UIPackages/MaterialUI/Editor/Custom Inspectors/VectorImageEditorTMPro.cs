//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;
using UnityEngine;
using System.Linq;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(VectorImageTMPro))]
    class VectorImageEditorTMPro : MaterialBaseEditor
    {
        //  SerializedProperties
        private SerializedProperty m_Size;
        private SerializedProperty m_SizeMode;
        private SerializedProperty m_Material;
        private SerializedProperty m_RaycastTarget;

        void OnEnable()
        {
            OnBaseEnable();
            m_Size = serializedObject.FindProperty("m_Size");
            m_SizeMode = serializedObject.FindProperty("m_SizeMode");
            m_Material = serializedObject.FindProperty("m_Material");
            m_RaycastTarget = serializedObject.FindProperty("m_RaycastTarget");
    }

        void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SizeMode);
            if (m_SizeMode.enumValueIndex == 0)
            {
                EditorGUILayout.PropertyField(m_Size);
            }

            InspectorFields.GraphicColorMultiField("Icon", gameObject => gameObject.GetComponent<VectorImageTMPro>());
            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(m_RaycastTarget);
            serializedObject.ApplyModifiedProperties();
        }
    }
}