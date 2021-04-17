using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace MaterialUI
{
    [CustomEditor(typeof(MaterialCanvasScaler), true)]
    [CanEditMultipleObjects]
    public class MaterialCanvasScalerEditor : Editor
    {
        SerializedProperty m_EditorForceDPI;
        SerializedProperty m_EditorForceDPIValue;
        SerializedProperty m_UseLegacyPhysicalSizeCalc;
        SerializedProperty m_SupportSafeArea;
        SerializedProperty m_DefaultScalingOrientation;
        SerializedProperty m_UiScaleMode;
        SerializedProperty m_ScaleFactor;
        SerializedProperty m_ReferenceResolution;
        SerializedProperty m_ScreenMatchMode;
        SerializedProperty m_MatchWidthOrHeight;
        SerializedProperty m_PhysicalUnit;
        SerializedProperty m_FallbackScreenDPI;
        SerializedProperty m_DefaultSpriteDPI;
        SerializedProperty m_DynamicPixelsPerUnit;
        SerializedProperty m_ReferencePixelsPerUnit;
        SerializedProperty onCanvasAreaChanged;

        const int kSliderEndpointLabelsHeight = 12;

        private class Styles
        {
            public GUIContent matchContent;
            public GUIContent widthContent;
            public GUIContent heightContent;
            public GUIContent uiScaleModeContent;
            public GUIStyle leftAlignedLabel;
            public GUIStyle rightAlignedLabel;

            public Styles()
            {
                matchContent = EditorGUIUtility.TrTextContent("Match");
                widthContent = EditorGUIUtility.TrTextContent("Width");
                heightContent = EditorGUIUtility.TrTextContent("Height");
                uiScaleModeContent = EditorGUIUtility.TrTextContent("UI Scale Mode");

                leftAlignedLabel = new GUIStyle(EditorStyles.label);
                rightAlignedLabel = new GUIStyle(EditorStyles.label);
                rightAlignedLabel.alignment = TextAnchor.MiddleRight;
            }
        }
        private static Styles s_Styles;

        protected virtual void OnEnable()
        {
            m_EditorForceDPI = serializedObject.FindProperty("m_EditorForceDPI");
            m_EditorForceDPIValue = serializedObject.FindProperty("m_EditorForceDPIValue");
            m_UseLegacyPhysicalSizeCalc = serializedObject.FindProperty("m_UseLegacyPhysicalSizeCalc");

            m_SupportSafeArea = serializedObject.FindProperty("m_SupportSafeArea");
            m_UiScaleMode = serializedObject.FindProperty("m_UiScaleMode");
            m_ScaleFactor = serializedObject.FindProperty("m_ScaleFactor");
            m_ReferenceResolution = serializedObject.FindProperty("m_ReferenceResolution");
            m_ScreenMatchMode = serializedObject.FindProperty("m_ScreenMatchMode");
            m_MatchWidthOrHeight = serializedObject.FindProperty("m_MatchWidthOrHeight");
            m_PhysicalUnit = serializedObject.FindProperty("m_PhysicalUnit");
            m_FallbackScreenDPI = serializedObject.FindProperty("m_FallbackScreenDPI");
            m_DefaultSpriteDPI = serializedObject.FindProperty("m_DefaultSpriteDPI");
            m_DynamicPixelsPerUnit = serializedObject.FindProperty("m_DynamicPixelsPerUnit");
            m_ReferencePixelsPerUnit = serializedObject.FindProperty("m_ReferencePixelsPerUnit");
            m_DefaultScalingOrientation = serializedObject.FindProperty("m_DefaultScalingOrientation");

            onCanvasAreaChanged = serializedObject.FindProperty("onCanvasAreaChanged");
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            bool allAreRoot = true;
            bool showWorldDiffers = false;
            bool showWorld = ((target as CanvasScaler).GetComponent<Canvas>().renderMode == RenderMode.WorldSpace);
            for (int i = 0; i < targets.Length; i++)
            {
                CanvasScaler scaler = targets[i] as CanvasScaler;
                Canvas canvas = scaler.GetComponent<Canvas>();
                if (!canvas.isRootCanvas)
                {
                    allAreRoot = false;
                    break;
                }
                if (showWorld && canvas.renderMode != RenderMode.WorldSpace || !showWorld && canvas.renderMode == RenderMode.WorldSpace)
                {
                    showWorldDiffers = true;
                    break;
                }
            }

            if (!allAreRoot)
            {
                EditorGUILayout.HelpBox("Non-root Canvases will not be scaled.", MessageType.Warning);
                return;
            }

            serializedObject.Update();

            EditorGUI.showMixedValue = showWorldDiffers;
            using (new EditorGUI.DisabledScope(showWorld || showWorldDiffers))
            {
                if (showWorld || showWorldDiffers)
                {
                    EditorGUILayout.Popup(s_Styles.uiScaleModeContent.text, 0, new[] { "World" });
                }
                else
                {
                    EditorGUILayout.PropertyField(m_UiScaleMode, s_Styles.uiScaleModeContent);
                }
            }
            EditorGUI.showMixedValue = false;

            if (!showWorldDiffers && !(!showWorld && m_UiScaleMode.hasMultipleDifferentValues))
            {
                EditorGUILayout.Space();

                // World Canvas
                if (showWorld)
                {
                    EditorGUILayout.PropertyField(m_DynamicPixelsPerUnit);
                }
                // Constant pixel size
                else if (m_UiScaleMode.enumValueIndex == (int)CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    EditorGUILayout.PropertyField(m_ScaleFactor);
                }
                // Scale with screen size
                else if (m_UiScaleMode.enumValueIndex == (int)CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    EditorGUILayout.PropertyField(m_ReferenceResolution);
                    EditorGUILayout.PropertyField(m_ScreenMatchMode);
                    if (m_ScreenMatchMode.enumValueIndex == (int)CanvasScaler.ScreenMatchMode.MatchWidthOrHeight && !m_ScreenMatchMode.hasMultipleDifferentValues)
                    {
                        Rect r = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + kSliderEndpointLabelsHeight);
                        DualLabeledSlider(r, m_MatchWidthOrHeight, s_Styles.matchContent, s_Styles.widthContent, s_Styles.heightContent);
                    }
                    EditorGUILayout.PropertyField(m_SupportSafeArea);
                    EditorGUILayout.PropertyField(m_ScaleFactor);

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_DefaultScalingOrientation);
                    EditorGUILayout.Space();
                }
                // Constant physical size
                else if (m_UiScaleMode.enumValueIndex == (int)CanvasScaler.ScaleMode.ConstantPhysicalSize)
                {
                    EditorGUILayout.PropertyField(m_UseLegacyPhysicalSizeCalc);
                    EditorGUILayout.Space(2);
                    if (!m_UseLegacyPhysicalSizeCalc.boolValue)
                    {
                        EditorGUILayout.PropertyField(m_EditorForceDPI);
                        EditorGUILayout.PropertyField(m_EditorForceDPIValue);
                        EditorGUILayout.PropertyField(m_FallbackScreenDPI);
                        EditorGUILayout.PropertyField(m_DefaultSpriteDPI, new GUIContent("Target Screen DPI"));
                        EditorGUILayout.PropertyField(m_ScaleFactor);

                        //Show useful informations
                        MaterialCanvasScaler scaler = target as MaterialCanvasScaler;
                        EditorGUILayout.LabelField(scaler.screenWidth + " x " + scaler.screenHeight + ", " + scaler.dpi + " dpi, " + scaler.screenSizeDigonal.ToString("##.##") + " inches", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(m_PhysicalUnit);
                        EditorGUILayout.PropertyField(m_FallbackScreenDPI);
                        EditorGUILayout.PropertyField(m_DefaultSpriteDPI);
                    }
                }

                EditorGUILayout.PropertyField(m_ReferencePixelsPerUnit);  
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onCanvasAreaChanged);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DualLabeledSlider(Rect position, SerializedProperty property, GUIContent mainLabel, GUIContent labelLeft, GUIContent labelRight)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            Rect pos = position;

            position.y += 12;
            position.xMin += EditorGUIUtility.labelWidth;
            position.xMax -= EditorGUIUtility.fieldWidth;

            GUI.Label(position, labelLeft, s_Styles.leftAlignedLabel);
            GUI.Label(position, labelRight, s_Styles.rightAlignedLabel);

            EditorGUI.PropertyField(pos, property, mainLabel);
        }
    }
}
