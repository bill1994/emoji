#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using Kyub;

namespace KyubEditor
{

    [CustomPropertyDrawer(typeof(IntVector2))]
    public class IntVector2Editor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var v_propertyX = property.FindPropertyRelative("m_x");
            var v_propertyY = property.FindPropertyRelative("m_y");
            int v_x = v_propertyX.intValue;
            int v_y = v_propertyY.intValue;
            IntVector2 v_result = EditorGUI.Vector2Field(position, label, new Vector2(v_x, v_y));

            if (v_result.x != v_x || v_result.y != v_y)
            {
                property.serializedObject.Update();
                v_propertyX.intValue = v_result.x;
                v_propertyY.intValue = v_result.y;
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.Update();
            }
        }
    }
}
#endif