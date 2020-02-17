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
    [CustomEditor(typeof(FastLinearLayoutGroup), true)]
    public class FastLinearLayoutGroupEditor : FastHorizontalOrVerticalLayoutGroupEditor
    {
        SerializedProperty m_IsVertical = null;

        protected override void OnEnable()
        {
            m_IsVertical = serializedObject.FindProperty("m_IsVertical");
            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_IsVertical);
            EditorGUILayout.Space();
            serializedObject.ApplyModifiedProperties();
            //EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
#endif
