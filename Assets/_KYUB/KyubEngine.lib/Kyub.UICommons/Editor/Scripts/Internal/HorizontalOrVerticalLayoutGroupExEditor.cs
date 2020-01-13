#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HorizontalOrVerticalLayoutGroupEx), true)]
    public class HorizontalOrVerticalLayoutGroupExEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        SerializedProperty m_ReverseOrder = null;

        protected override void OnEnable()
        {
            m_ReverseOrder = serializedObject.FindProperty("m_ReverseOrder");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_ReverseOrder);
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            //EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
#endif
