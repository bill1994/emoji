#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(TextureImage), true)]
    [CanEditMultipleObjects]
    public class TextureImageEditor : RawImageEditor
    {
        SerializedProperty m_PreserveAspect;
        SerializedProperty m_PreserveAspectMode;

        //Shape Properties
        SerializedProperty m_ShapeType;
        SerializedProperty m_ShapeFillMode;
        SerializedProperty m_ShapeAntiAliasing;

        //Outline Properties
        SerializedProperty m_OutlineType;
        SerializedProperty m_OutlineThickness;

        //Ellipse Properties
        SerializedProperty m_EllipseFittingMode;

        //RoundedRect Properties
        SerializedProperty m_CornerMode;
        SerializedProperty m_CornerRoundness;

        SerializedProperty m_CornerRoundnessBottomLeft;
        SerializedProperty m_CornerRoundnessBottomRight;
        SerializedProperty m_CornerRoundnessTopLeft;
        SerializedProperty m_CornerRoundnessTopRight;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_PreserveAspect = serializedObject.FindProperty("m_PreserveAspect");
            m_PreserveAspectMode = serializedObject.FindProperty("m_PreserveAspectMode");

            m_ShapeType = serializedObject.FindProperty("m_ShapeType");
            m_ShapeFillMode = serializedObject.FindProperty("m_ShapeFillMode");
            m_ShapeAntiAliasing = serializedObject.FindProperty("m_ShapeAntiAliasing");

            m_OutlineType = serializedObject.FindProperty("m_OutlineType");
            m_OutlineThickness = serializedObject.FindProperty("m_OutlineThickness");

            m_EllipseFittingMode = serializedObject.FindProperty("m_EllipseFittingMode");

            m_CornerMode = serializedObject.FindProperty("m_CornerMode");
            m_CornerRoundness = serializedObject.FindProperty("m_CornerRoundness");

            m_CornerRoundnessBottomLeft = m_CornerRoundness.FindPropertyRelative("m_BottomLeft");
            m_CornerRoundnessBottomRight = m_CornerRoundness.FindPropertyRelative("m_BottomRight");
            m_CornerRoundnessTopLeft = m_CornerRoundness.FindPropertyRelative("m_TopLeft");
            m_CornerRoundnessTopRight = m_CornerRoundness.FindPropertyRelative("m_TopRight");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_PreserveAspect);
            EditorGUILayout.PropertyField(m_PreserveAspectMode);

            EditorGUILayout.Space();
            DrawShapeProperties();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void DrawShapeProperties()
        {
            EditorGUILayout.PropertyField(m_ShapeType);

            if (!m_ShapeType.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                var shapeType = (ShapeTypeModeEnum)m_ShapeType.enumValueIndex;
                if (shapeType == ShapeTypeModeEnum.RoundedRect)
                {
                    EditorGUILayout.PropertyField(m_CornerMode);

                    var cornerMode = (RoundedRectCornerModeEnum)m_CornerMode.enumValueIndex;
                    if (cornerMode == RoundedRectCornerModeEnum.Default)
                    {
                        var changed = GUI.changed;
                        EditorGUILayout.PropertyField(m_CornerRoundnessBottomLeft);
                        if (changed != GUI.changed)
                        {
                            m_CornerRoundnessBottomRight.floatValue = m_CornerRoundnessBottomLeft.floatValue;
                            m_CornerRoundnessTopLeft.floatValue = m_CornerRoundnessBottomLeft.floatValue;
                            m_CornerRoundnessTopRight.floatValue = m_CornerRoundnessBottomLeft.floatValue;
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(m_CornerRoundness, true);
                    }
                }
                else if (shapeType == ShapeTypeModeEnum.Ellipse)
                {
                    EditorGUILayout.PropertyField(m_EllipseFittingMode);
                }

                if (shapeType != ShapeTypeModeEnum.Default)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_ShapeAntiAliasing);
                    EditorGUILayout.Space();
                }
                EditorGUI.indentLevel--;
            }


            EditorGUILayout.PropertyField(m_ShapeFillMode);
            if (!m_ShapeFillMode.hasMultipleDifferentValues)
            {
                EditorGUI.indentLevel++;
                var shapFillMode = (ShapeFillModeEnum)m_ShapeFillMode.enumValueIndex;
                if (shapFillMode == ShapeFillModeEnum.Outline)
                {
                    EditorGUILayout.PropertyField(m_OutlineType);
                    EditorGUILayout.PropertyField(m_OutlineThickness);
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}

#endif
