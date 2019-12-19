using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_LocaleTextUGUI), true), CanEditMultipleObjects]
#if UNITY_2019_2_OR_NEWER
    public class TMP_LocaleTextUGUIEditor : TMP_EditorPanelUI
#else
    public class TMP_LocaleTextUGUIEditor : TMP_UiEditorPanel
#endif
    {
        SerializedProperty m_isLocalized = null;
        SerializedProperty m_supportLocaleRichTextTags = null;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_isLocalized = serializedObject.FindProperty("m_isLocalized");
            m_supportLocaleRichTextTags = serializedObject.FindProperty("m_supportLocaleRichTextTags");
        }

        protected override void DrawExtraSettings()
        {
            //var v_oldGui = GUI.enabled;

            var v_target = target as TMP_LocaleTextUGUI;

            base.DrawExtraSettings();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Localization Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_isLocalized);

            //GUI.enabled = m_isLocalized.boolValue;
            if (!v_target.richText && m_supportLocaleRichTextTags.boolValue)
                EditorGUILayout.HelpBox("Require richtext active to work", MessageType.Warning);
            EditorGUILayout.PropertyField(m_supportLocaleRichTextTags);
            //GUI.enabled = v_oldGui;

            EditorGUI.indentLevel--;
        }
    }
}
