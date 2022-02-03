using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.Localization;

namespace KyubEditor.Localization
{
    [CustomEditor(typeof(EditorLocaleConfigAsset), true)]
    public class EditorLocaleConfigAssetEditor : Editor
    {
        SerializedProperty m_script;
        SerializedProperty m_enabled;
        protected string[] elementsToIgnore = new string[] { "m_startingLanguage", "m_Script", "m_enabled" };

        protected virtual void OnEnable()
        {
            m_script = serializedObject.FindProperty("m_Script");
            m_enabled = serializedObject.FindProperty("m_enabled");
        }

        public override void OnInspectorGUI()
        {
            var oldGuiEnabled = GUI.enabled;

            serializedObject.Update();
            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_script);
            GUI.enabled = oldGuiEnabled;
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_enabled);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General Config Fields", EditorStyles.boldLabel);
            DrawLanguegePicker();
            DrawPropertiesExcluding(serializedObject, elementsToIgnore);
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawLanguegePicker()
        {
            var guiTarget = target as EditorLocaleConfigAsset;

            //var oldGuiEnabled = GUI.enabled;
            //GUI.enabled = Application.isPlaying || !target.AlwaysPickFromSystemLanguage;

            List<string> possibleLanguages = new List<string>();
            List<string> displayLanguages = new List<string>();
            for (int i = 0; i < guiTarget.LocalizationDatas.Count; i++)
            {
                var localeData = guiTarget.LocalizationDatas[i];
                var language = localeData != null && !string.IsNullOrEmpty(localeData.Name) ? localeData.Name : "";
                var fullLanguage = localeData != null && !string.IsNullOrEmpty(localeData.FullName) ? localeData.FullName : "";
                possibleLanguages.Add(language);
                displayLanguages.Add(i + ": " + (!string.IsNullOrEmpty(language) ? language : "-") + (!string.IsNullOrEmpty(fullLanguage) ? " (" + fullLanguage + ")" : ""));
            }

            int index = possibleLanguages.IndexOf(Application.isPlaying ? guiTarget.CurrentLanguage : guiTarget.StartingLanguage);
            var newIndex = EditorGUILayout.Popup(Application.isPlaying ? "Current Language" : "Starting Language", index, displayLanguages.ToArray());
            if (newIndex != index)
            {
                serializedObject.ApplyModifiedProperties();
                guiTarget.CurrentLanguage = possibleLanguages[newIndex];
                guiTarget.StartingLanguage = possibleLanguages[newIndex];
                guiTarget.Init();
                serializedObject.Update();
                EditorUtility.SetDirty(target);
            }

            //GUI.enabled = oldGuiEnabled;
        }
    }
}
