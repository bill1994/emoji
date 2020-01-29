using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.Localization;

namespace KyubEditor.Localization
{
    [CustomEditor(typeof(LocaleManager), true)]
    public class LocaleManagerEditor : Editor
    {
        SerializedProperty m_script;
        protected string[] v_elementsToIgnore = new string[] { "m_startingLanguage", "m_Script" };

        protected virtual void OnEnable()
        {
            m_script = serializedObject.FindProperty("m_Script");
        }

        public override void OnInspectorGUI()
        {
            var v_oldGuiEnabled = GUI.enabled;

            serializedObject.Update();
            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_script);
            GUI.enabled = v_oldGuiEnabled;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("General Config Fields", EditorStyles.boldLabel);
            DrawLanguegePicker();
            DrawPropertiesExcluding(serializedObject, v_elementsToIgnore);
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawLanguegePicker()
        {
            var v_target = target as LocaleManager;

            //var v_oldGuiEnabled = GUI.enabled;
            //GUI.enabled = Application.isPlaying || !v_target.AlwaysPickFromSystemLanguage;

            List<string> v_possibleLanguages = new List<string>();
            List<string> v_displayLanguages = new List<string>();
            for (int i = 0; i < v_target.LocalizationDatas.Count; i++)
            {
                var v_localeData = v_target.LocalizationDatas[i];
                var v_language = v_localeData != null && !string.IsNullOrEmpty(v_localeData.Name) ? v_localeData.Name : "";
                var v_fullLanguage = v_localeData != null && !string.IsNullOrEmpty(v_localeData.FullName) ? v_localeData.FullName : "";
                v_possibleLanguages.Add(v_language);
                v_displayLanguages.Add(i + ": " + (!string.IsNullOrEmpty(v_language) ? v_language : "-") + (!string.IsNullOrEmpty(v_fullLanguage) ? " ("+ v_fullLanguage +")" : ""));
            }

            int v_index = v_possibleLanguages.IndexOf(Application.isPlaying ? v_target.CurrentLanguage : v_target.StartingLanguage);
            var v_newIndex = EditorGUILayout.Popup(Application.isPlaying? "Current Language" : "Starting Language", v_index, v_displayLanguages.ToArray());
            if (v_newIndex != v_index)
            {
                serializedObject.ApplyModifiedProperties();
                if (Application.isPlaying)
                    v_target.CurrentLanguage = v_possibleLanguages[v_newIndex];
                else
                    v_target.StartingLanguage = v_possibleLanguages[v_newIndex];
                serializedObject.Update();
                EditorUtility.SetDirty(v_target);
            }

            //GUI.enabled = v_oldGuiEnabled;
        }
    }
}
