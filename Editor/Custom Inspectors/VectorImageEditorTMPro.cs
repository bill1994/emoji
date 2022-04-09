// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

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
        private SerializedProperty m_IncludeInLayoutWhenEmpty;

        void OnEnable()
        {
            OnBaseEnable();
            m_IncludeInLayoutWhenEmpty = serializedObject.FindProperty("m_IncludeInLayoutWhenEmpty");
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
            EditorGUILayout.PropertyField(m_IncludeInLayoutWhenEmpty);
            EditorGUILayout.Space();

            InspectorFields.GraphicColorMultiField("Icon", gameObject => gameObject.GetComponent<VectorImageTMPro>());
            EditorGUILayout.PropertyField(m_Material);
            EditorGUILayout.PropertyField(m_RaycastTarget);
            serializedObject.ApplyModifiedProperties();
        }
    }
}