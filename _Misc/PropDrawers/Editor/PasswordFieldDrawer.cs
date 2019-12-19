#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using Kyub;

namespace KyubEditor
{
    [CustomPropertyDrawer(typeof(PasswordFieldAttribute))]
    public partial class PasswordFieldDrawer : SpecificFieldDrawer
    {
        protected override void DrawComponent(Rect position, SerializedProperty property, GUIContent label, System.Type p_type)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = EditorGUI.PasswordField(position, label, property.stringValue);
            }
            else
                EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
#endif