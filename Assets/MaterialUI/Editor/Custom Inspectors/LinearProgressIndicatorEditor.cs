// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace MaterialUI
{
    [CustomEditor(typeof(LinearProgressIndicator))]
    public class LinearProgressIndicatorEditor : BaseStyleElementEditor
    {
        private SerializedProperty m_CurrentProgress;
        private SerializedProperty m_BaseObjectOverride;
        private SerializedProperty m_BarRectTransform;
        private SerializedProperty m_BackgroundImage;
        private SerializedProperty m_StartsIndeterminate;
        private SerializedProperty m_StartsHidden;

        protected override void OnEnable()
        {
            OnBaseEnable();

            var properties = new List<string>(_excludingProperties);
            properties.AddRange(new string[] { "m_CurrentProgress", "m_BaseObjectOverride", "m_CircleRectTransform", "m_StartsIndeterminate", "m_StartsHidden" });
            _excludingProperties = properties.ToArray();

            m_CurrentProgress = serializedObject.FindProperty("m_CurrentProgress");
            m_BaseObjectOverride = serializedObject.FindProperty("m_BaseObjectOverride");
            m_BarRectTransform = serializedObject.FindProperty("m_BarRectTransform");
            m_BackgroundImage = serializedObject.FindProperty("m_BackgroundImage");
            m_StartsIndeterminate = serializedObject.FindProperty("m_StartsIndeterminate");
            m_StartsHidden = serializedObject.FindProperty("m_StartsHidden");
        }

        protected override void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_CurrentProgress);
            EditorGUILayout.PropertyField(m_StartsIndeterminate);
            EditorGUILayout.PropertyField(m_StartsHidden);

            DrawFoldoutExternalProperties(ExternalPropertiesSection);
            DrawFoldoutComponents(ComponentSection);

            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }

        private bool ExternalPropertiesSection()
        {
            bool result = false;

            RectTransform barRectTransform = m_BarRectTransform.objectReferenceValue as RectTransform;
            if (barRectTransform != null)
            {
                if (InspectorFields.GraphicColorField("Bar", barRectTransform.GetComponent<Graphic>()))
                    result = true;
            }

            Image backgroundImage = m_BackgroundImage.objectReferenceValue as Image;
            if (backgroundImage != null)
            {
                if (InspectorFields.GraphicColorField("Background", backgroundImage))
                    result = true;
            }

            return result;
        }

        private void ComponentSection()
        {
            EditorGUILayout.PropertyField(m_BarRectTransform);
            EditorGUILayout.PropertyField(m_BackgroundImage);
            EditorGUILayout.PropertyField(m_BaseObjectOverride);
        }
    }
}