#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(ScrollGridLayoutGroup), true)]
    public class ScrollGridRectLayoutEditor : ScrollLayoutGroupEditor
    {
        SerializedProperty m_cellSize;
        SerializedProperty m_constraintType;
        SerializedProperty m_constraintAlign;
        SerializedProperty m_constraintSpacing;
        SerializedProperty m_defaultConstraintCount;

        protected virtual void OnEnable()
        {
            m_cellSize = serializedObject.FindProperty("m_cellSize");
            m_constraintType = serializedObject.FindProperty("m_constraintType");
            m_constraintAlign = serializedObject.FindProperty("m_constraintAlign");
            m_constraintSpacing = serializedObject.FindProperty("m_constraintSpacing");
            m_defaultConstraintCount = serializedObject.FindProperty("m_defaultConstraintCount");

            _elementsToIgnore = new string[] { "m_defaultConstraintCount", "m_constraintSpacing", "m_constraintAlign", "m_constraintType", "m_cellSize", "OnElementChanged", "OnElementsRemoved", "OnElementsAdded", "OnAllElementsReplaced", "OnElementCachedSizeChanged", "OnBeforeChangeVisibleElements", "OnElementBecameInvisible", "OnElementBecameVisible", "m_Script" };
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Space(15);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Recalculate Elements Layout", GUILayout.Width(200)))
                {
                    serializedObject.Update();
                    (target as ScrollLayoutGroup).SetCachedElementsLayoutDirty(true);
                    (target as ScrollLayoutGroup).TryRecalculateLayout(true);
                    UnityEditor.EditorUtility.SetDirty(target);
                    serializedObject.ApplyModifiedProperties();
                }
                GUILayout.FlexibleSpace();
            }

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, _elementsToIgnore);
            DrawConstraintProperties();
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                (target as ScrollLayoutGroup).SetCachedElementsLayoutDirty();
            }
        }

        protected virtual void DrawConstraintProperties()
        {
            var v_oldGui = GUI.enabled;
            var v_target = target as ScrollGridLayoutGroup;
                var v_isVertical = v_target.IsVertical();
                EditorGUILayout.PropertyField(m_constraintType);
                EditorGUILayout.PropertyField(m_constraintAlign, new GUIContent("Align"));
                if (v_target.ConstraintType == ScrollGridLayoutGroup.GridConstraintTypeEnum.FixedWithFlexibleCells)
                {
                    var v_newValue = EditorGUILayout.FloatField(new GUIContent(v_isVertical ? "Cell Y" : "Cell X"), v_isVertical ? v_target.CellSize.y : v_target.CellSize.x);
                    m_cellSize.vector2Value = v_isVertical ? new Vector2(m_cellSize.vector2Value.x, v_newValue) : new Vector2(v_newValue, m_cellSize.vector2Value.y);
                }
                else
                {
                    EditorGUILayout.PropertyField(m_cellSize);
                }


                var v_constraintGuiContent = new GUIContent(v_isVertical ? "Amount of Collumns" : "Amount of Rows");
                //Only show real constraint if is not flexible mode
                GUI.enabled = v_target.ConstraintType != ScrollGridLayoutGroup.GridConstraintTypeEnum.Flexible;
                if (GUI.enabled)
                    EditorGUILayout.PropertyField(m_defaultConstraintCount, v_constraintGuiContent);
                else
                    EditorGUILayout.IntField(v_constraintGuiContent, v_target.CachedConstraintCount);
                GUI.enabled = v_oldGui;

                EditorGUILayout.PropertyField(m_constraintSpacing, new GUIContent(v_isVertical ? "Collumn Spacing" : "Row Spacing"));
            }
    }
}
#endif
