#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(TextureImage), true)]
    [CanEditMultipleObjects]
    public class TextureImageEditor : RawImageEditor
    {
        SerializedProperty m_PreserveAspect;
        SerializedProperty m_PreserveAspectMode;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");
            m_PreserveAspectMode = serializedObject.FindProperty("m_PreserveAspectMode");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_PreserveAspect);
            EditorGUILayout.PropertyField(m_PreserveAspectMode);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif
