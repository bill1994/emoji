#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.UI;
using UnityEngine.UI;
using UnityEditor.UI;

namespace KyubEditor.UI
{

    [CustomEditor(typeof(NestedScrollRect), true)]
    public class NestedScrollRectEditor : ScrollRectEditor
    {
        SerializedProperty m_SnapToDuration = null;
        SerializedProperty m_NestedDragActive = null;

        protected override void OnEnable()
        {
            m_SnapToDuration = serializedObject.FindProperty("m_SnapToDuration");
            m_NestedDragActive = serializedObject.FindProperty("m_NestedDragActive");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_SnapToDuration);
            EditorGUILayout.PropertyField(m_NestedDragActive);
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
}
#endif
