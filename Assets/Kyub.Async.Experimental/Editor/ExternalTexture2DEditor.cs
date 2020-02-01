using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Async;
using UnityEditor;
using Kyub.Async.Experimental;
using System;

namespace KyubEditor.Async.Experimental
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ExternalTexture2D))]
    public class ExternalTexture2DEditor : Editor
    {
        SerializedProperty m_Url;
        SerializedProperty m_Texture2D;
        SerializedProperty m_MaxSize;

        static int[] s_SizeOptions = new int[] { 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };
        static GUIContent[] s_SizeOptionsLabel = new GUIContent[] {
            new GUIContent("16"),
            new GUIContent("32"),
            new GUIContent("64"),
            new GUIContent("128"),
            new GUIContent("256"),
            new GUIContent("512"),
            new GUIContent("1024"),
            new GUIContent("2048"),
            new GUIContent("4096") };

        public virtual void OnEnable()
        {
            m_Url = serializedObject.FindProperty("m_Url");
            m_Texture2D = serializedObject.FindProperty("m_Texture2D");
            m_MaxSize = serializedObject.FindProperty("m_MaxSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Url);
            EditorGUILayout.PropertyField(m_Texture2D);
            EditorGUILayout.Space();

            EditorGUILayout.IntPopup(m_MaxSize, s_SizeOptionsLabel, s_SizeOptions);
            EditorGUILayout.Space();

            DrawPropertiesExcluding(serializedObject, new string[] { "m_Url", "m_Texture2D", "m_MaxSize", "m_Script" });
            serializedObject.ApplyModifiedProperties();
        }
    }
}
