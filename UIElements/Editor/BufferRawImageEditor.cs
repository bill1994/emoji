#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.Performance;

namespace KyubEditor.Performance
{
    [CustomEditor(typeof(BufferRawImage))]
    public class BufferRawImageEditor : UnityEditor.UI.RawImageEditor
    {
        #region Prigate Variables

        SerializedProperty m_renderBufferIndex;
        SerializedProperty m_uvBasedOnScreenRect;
        SerializedProperty m_offsetUV;
        SerializedProperty m_onClick;

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            m_renderBufferIndex = serializedObject.FindProperty("m_renderBufferIndex");
            m_uvBasedOnScreenRect = serializedObject.FindProperty("m_uvBasedOnScreenRect");
            m_offsetUV = serializedObject.FindProperty("m_offsetUV");
            m_onClick = serializedObject.FindProperty("OnClick");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(m_renderBufferIndex);
            EditorGUILayout.PropertyField(m_uvBasedOnScreenRect);
            EditorGUILayout.PropertyField(m_offsetUV);
            EditorGUILayout.PropertyField(m_onClick);
            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}

#endif
