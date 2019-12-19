#if UNITY_EDITOR

using UnityEditor;
using Kyub.UI;
using UnityEngine.UI;
using UnityEngine;

namespace KyubEditor.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HtmlText))]
    public class HtmlTextEditor : UnityEditor.UI.TextEditor
    {
        private SerializedProperty ImageScalingFactorProp;
        private SerializedProperty hyperlinkColorProp;
        private SerializedProperty hyperlinkColorHoverProp;
        private SerializedProperty hyperlinkColorPressedProp;
        private SerializedProperty imageOffsetProp;
        private SerializedProperty iconList;
        private SerializedProperty hiperlinkClicked;
        private SerializedProperty specialHrefEvents;

        protected override void OnEnable()
        {
            base.OnEnable();
            ImageScalingFactorProp = serializedObject.FindProperty("ImageScalingFactor");
            hyperlinkColorProp = serializedObject.FindProperty("hyperlinkColor");
            hyperlinkColorHoverProp = serializedObject.FindProperty("hyperlinkHoverMultiplier");
            hyperlinkColorPressedProp = serializedObject.FindProperty("hyperlinkPressedMultiplier");
            imageOffsetProp = serializedObject.FindProperty("imageOffset");
            iconList = serializedObject.FindProperty("inspectorIconList");
            hiperlinkClicked = serializedObject.FindProperty("OnHrefClick");
            specialHrefEvents = serializedObject.FindProperty("m_specialHrefClickEvents");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(imageOffsetProp, new GUIContent("Image Offset"));
            EditorGUILayout.PropertyField(ImageScalingFactorProp, new GUIContent("Image Scaling Factor"));
            EditorGUILayout.PropertyField(hyperlinkColorProp, new GUIContent("Hyperlink Color"));
            EditorGUILayout.PropertyField(hyperlinkColorHoverProp, new GUIContent("Hyperlink Hover Multiplier"));
            EditorGUILayout.PropertyField(hyperlinkColorPressedProp, new GUIContent("Hyperlink Pressed Multiplier"));
            EditorGUILayout.PropertyField(iconList, new GUIContent("Icon List"), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hiperlinkClicked, new GUIContent("On Hyperlink Clicked"));
            EditorGUILayout.PropertyField(specialHrefEvents, new GUIContent("Special Hyperlinks"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif