#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kyub.Extensions;
using KyubEditor.Extensions;
using Kyub;

namespace KyubEditor
{
    [CustomPropertyDrawer(typeof(MaskEnumAttribute))]
    public partial class MaskEnumDrawer : SpecificFieldDrawer
    {
        #region Internal Drawer Functions

        protected override void DrawComponent(Rect position, SerializedProperty property, GUIContent label, System.Type p_type)
        {
            if (property.propertyType == SerializedPropertyType.Enum && System.Attribute.IsDefined(p_type, typeof(System.FlagsAttribute)))
            {
                int v_value = GetEnumValue(property, p_type, true);
                int v_newValue = EditorGUI.MaskField(position, label, v_value, property.enumNames);
                v_newValue = (int)ConvertToSafeValue(property, v_newValue, p_type);
                v_value = (int)ConvertToSafeValue(property, v_value, p_type);

                if (v_value != v_newValue)
                {
                    property.serializedObject.Update();
                    property.intValue = v_newValue;
                    SetEnumValue(property, v_newValue, p_type, true);
                    try
                    {
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
                    }
                    catch { }
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
                if (GUI.changed)
                {
                    SetEnumValue(property, property.intValue, p_type, true);
                    try
                    {
#if UNITY_5_3_OR_NEWER
                        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
#else
                        EditorUtility.SetDirty(property.serializedObject.targetObject);
#endif
                    }
                    catch { }
                }
            }
        }

        #endregion

        #region Helper Functions

        protected object ConvertToSafeValue(SerializedProperty p_property, object p_value, System.Type p_type)
        {
            try
            {
                if (p_property.propertyType == SerializedPropertyType.Enum)
                {
                    int v_flagValue = (int)p_value;
                    //Pick All Possible values of Flagged Enum because unity return -1 when all flags selected
                    if (v_flagValue == -1)
                    {
                        v_flagValue = 0;
                        foreach (object v_value in System.Enum.GetValues(p_type))
                        {
                            v_flagValue = (int)v_value | (int)v_flagValue;
                        }
                    }
                    return System.Enum.ToObject(p_type, v_flagValue);
                }
            }
            catch { }
            return p_value;
        }

        protected virtual int GetEnumValue(SerializedProperty p_property, System.Type p_type, bool p_hasFlags)
        {
            int v_return = 0;
            try
            {
                if(p_property.GetFieldInfo() == null || 
                    (p_property.serializedObject.targetObject is ScriptableObject))
                    v_return = (int)GetSafeEnumValue((int)p_property.intValue, p_type, p_hasFlags);
                else
                    v_return = (int)GetSafeEnumValue((int)p_property.GetFieldValue(), p_type, p_hasFlags);
            }
            catch { }
            return v_return;
        }

        protected virtual void SetEnumValue(SerializedProperty p_property, int p_value, System.Type p_type, bool p_hasFlags)
        {
            try
            {
                int v_safeValue = (int)GetSafeEnumValue(p_value, p_type, p_hasFlags);
                p_property.SetFieldValue(v_safeValue);
            }
            catch { }
        }

        public static object GetSafeEnumValue(int p_value, System.Type p_type, bool p_hasFlags)
        {
            if (!p_hasFlags)
                return System.Enum.ToObject(p_type, p_value);
            else
            {
                int v_flagValue = p_value;
                //Pick All Possible values of Flagged Enum because unity return -1 when all flags selected
                if (v_flagValue == -1)
                {
                    v_flagValue = 0;
                    foreach (object v_value in System.Enum.GetValues(p_type))
                    {
                        v_flagValue = (int)v_value | (int)v_flagValue;
                    }
                }
                return System.Enum.ToObject(p_type, v_flagValue);
            }
        }

        #endregion
    }
}
#endif
