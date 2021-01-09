#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub;
using Kyub.UI;
using TMPro;
using Kyub.Internal.NativeInputPlugin;

namespace KyubEditor.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TMP_NativeInputField))]
    public class TMP_NativeInputFieldEditor : TMPro.EditorUtilities.TMP_InputFieldEditor
    {
        private SerializedProperty m_AsteriskChar;
        private SerializedProperty m_OnReturnPressed;
        private SerializedProperty m_PanContent;
        private SerializedProperty m_RectMaskMode;
        private SerializedProperty m_RectMaskContent;

        private SerializedProperty m_GlobalFontAsset;

        private SerializedProperty m_ShowDoneButton;
        private SerializedProperty m_ShowClearButton;
        private SerializedProperty m_ReturnKeyType;

        private SerializedProperty m_TextComponent;
        
        //private SerializedProperty m_MonospacePassDistEm;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_AsteriskChar = serializedObject.FindProperty("m_AsteriskChar");
            m_PanContent = serializedObject.FindProperty("m_PanContent");
            m_RectMaskMode = serializedObject.FindProperty("m_RectMaskMode");
            m_RectMaskContent = serializedObject.FindProperty("m_RectMaskContent");

            m_OnReturnPressed = serializedObject.FindProperty("OnReturnPressed");
            m_GlobalFontAsset = serializedObject.FindProperty("m_GlobalFontAsset");
            //m_MonospacePassDistEm = serializedObject.FindProperty("m_MonospacePassDistEm");

            m_ShowDoneButton = serializedObject.FindProperty("m_ShowDoneButton");
            m_ShowClearButton = serializedObject.FindProperty("m_ShowClearButton");
            m_ReturnKeyType = serializedObject.FindProperty("m_ReturnKeyType");

            m_TextComponent = serializedObject.FindProperty("m_TextComponent");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(m_TextComponent == null || m_TextComponent.objectReferenceValue == null);

            var cachedFont = m_GlobalFontAsset.objectReferenceValue;
            
            base.OnInspectorGUI();
            serializedObject.Update();
            if (cachedFont != m_GlobalFontAsset.objectReferenceValue)
            {
                foreach (var target in targets)
                {
                    var nativeInput = target as TMP_NativeInputField;
                    nativeInput.SetGlobalFontAsset(m_GlobalFontAsset.objectReferenceValue as TMP_FontAsset);
                    if (nativeInput.textComponent != null)
                        EditorUtility.SetDirty(nativeInput.textComponent);
                    if (nativeInput.placeholder is TextMeshProUGUI)
                        EditorUtility.SetDirty(nativeInput.placeholder);
                }
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField("Password Special Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AsteriskChar);
            //EditorGUILayout.PropertyField(m_MonospacePassDistEm);
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Virtual Keyboard Layout Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PanContent);
            EditorGUILayout.PropertyField(m_RectMaskMode);
            //Only show RectMaskContent when RectMaskMode == Manual
            EditorGUI.BeginDisabledGroup(m_RectMaskMode.enumValueIndex != 0);
            EditorGUILayout.PropertyField(m_RectMaskContent);
            EditorGUI.EndDisabledGroup();

            OnGUIExtraNativeFields();
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        public virtual void OnGUIExtraNativeFields()
        {
            GUILayout.Space(20);
            GUILayout.Label("Native Keyboard Return Type", EditorStyles.boldLabel);
            m_ReturnKeyType.enumValueIndex = GUILayout.Toolbar((int)m_ReturnKeyType.enumValueIndex, new string[] { "Default", "Next", "Done", "Search" });
            EditorGUILayout.Space();
            GUILayout.Label("Native Keyboard Options", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ShowDoneButton, new GUIContent("Show \"Done\" button"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_ShowClearButton, new GUIContent("Show \"Clear\" button"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OnReturnPressed);
        }
    }
}
#endif
