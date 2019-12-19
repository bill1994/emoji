using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.Localization.UI;

namespace KyubEditor.Localization
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TextLocalize))]
    public class TextLocalizeEditor : Editor
    {
        SerializedProperty m_key;
        SerializedProperty m_autoTrackKey;
        SerializedProperty m_supportLocaleRichTextTags;

        protected virtual void OnEnable()
        {
            m_key = serializedObject.FindProperty("m_key");
            m_autoTrackKey = serializedObject.FindProperty("m_autoTrackKey");
            m_supportLocaleRichTextTags = serializedObject.FindProperty("m_supportLocaleRichTextTags");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var v_oldAutoTrackValue = m_autoTrackKey.boolValue;
            EditorGUILayout.PropertyField(m_autoTrackKey);

            //Clear Key if AutoTrackValue Changed
            if (v_oldAutoTrackValue != m_autoTrackKey.boolValue)
                m_key.stringValue = "";

            if (!m_autoTrackKey.boolValue)
                EditorGUILayout.PropertyField(m_key);

            EditorGUILayout.PropertyField(m_supportLocaleRichTextTags);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
