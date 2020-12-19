using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Kyub.Localization.UI;
using TMPro.EditorUtilities;

namespace KyubEditor.Localization.UI
{
    [CustomEditor(typeof(TMP_LocaleTextUGUI), true), CanEditMultipleObjects]
#if TMP_2_1_0_PREVIEW_1_OR_NEWER
    public class TMP_LocaleTextUGUIEditor : TMP_EditorPanelUI
#else
    public class TMP_LocaleTextUGUIEditor : TMP_UiEditorPanel
#endif
    {
        SerializedProperty m_isLocalized = null;
        SerializedProperty m_supportLocaleRichTextTags = null;
        SerializedProperty m_monospaceDistEm = null;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_isLocalized = serializedObject.FindProperty("m_isLocalized");
            m_supportLocaleRichTextTags = serializedObject.FindProperty("m_supportLocaleRichTextTags");
            m_monospaceDistEm = serializedObject.FindProperty("m_monospaceDistEm");
        }

        protected override void DrawExtraSettings()
        {
            //var oldGui = GUI.enabled;
            EditorGUILayout.PropertyField(m_monospaceDistEm);

            var guiTarget = target as TMP_LocaleTextUGUI;

            base.DrawExtraSettings();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Localization Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_isLocalized);

            //GUI.enabled = m_isLocalized.boolValue;
            if (!guiTarget.richText && m_supportLocaleRichTextTags.boolValue)
                EditorGUILayout.HelpBox("Require richtext active to work", MessageType.Warning);
            EditorGUILayout.PropertyField(m_supportLocaleRichTextTags);
            //GUI.enabled = oldGui;

            EditorGUI.indentLevel--;
        }
    }
}
