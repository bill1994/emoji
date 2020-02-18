#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI.Experimental;

namespace KyubEditor.UI.Experimental
{
    [CustomEditor(typeof(FastFlexLayoutGroup), true)]
    public class FlexLayoutGroupEditor : FastHorizontalOrVerticalLayoutGroupEditor
    {
        SerializedProperty m_IsVertical = null;
        SerializedProperty m_SpacingBetween = null;

        protected override void OnEnable()
        {
            m_IsVertical = serializedObject.FindProperty("m_IsVertical");
            m_SpacingBetween = serializedObject.FindProperty("m_SpacingBetween");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_IsVertical);
            EditorGUILayout.PropertyField(m_SpacingBetween);
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            //EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
#endif
