#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Kyub;
using KyubEditor.Extensions;

namespace KyubEditor
{
    [CustomPropertyDrawer(typeof(SerializableTypeAttribute))]
    public partial class SerializableTypeDrawer : DependentFieldDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializableType v_type = GetSerializableTypeValue(property);
            int v_fixedSize = 16;
            if (v_type.FoldOut)
                v_fixedSize = v_type != null && v_type.CastedType != null ? v_fixedSize * (v_type.CastedType.GetGenericArguments().Length + 1) : v_fixedSize;
            return ValueEqualsTrigger(property) ? v_fixedSize : -2;
        }

        protected override void DrawComponentAfterDependenceCheck(Rect position, SerializedProperty property, GUIContent label, System.Type p_type)
        {
            SerializableTypeAttribute v_attr = (SerializableTypeAttribute)attribute;
            SerializableType v_serType = GetSerializableTypeValue(property);
            SerializableType v_newType = KyubEditor.InspectorUtils.SerializableTypePopup(position, label.text, v_serType, v_attr.FilterType, v_attr.AcceptGenericDefinitions, v_attr.AcceptAbstractDefinitions, v_attr.AcceptNulls, v_attr.FilterAssemblies);
            if (v_newType == null)
                v_newType = new SerializableType();
            if (v_serType.CastedType != v_newType.CastedType || v_serType.StringType != v_newType.StringType)
            {
                SetSerializableTypeValue(property, v_newType);
                try
                {
#if UNITY_5_3_OR_NEWER
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
#else
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
#endif
                }
                catch { }
            }
        }

        protected virtual SerializableType GetSerializableTypeValue(SerializedProperty p_property)
        {
            SerializableType v_return = null;
            try
            {
                v_return = (SerializableType)p_property.GetFieldValue();
            }
            catch { }
            if (v_return == null)
                v_return = new SerializableType();
            return v_return;
        }

        protected virtual void SetSerializableTypeValue(SerializedProperty p_property, SerializableType p_value)
        {
            try
            {
                p_property.SetFieldValue(p_value);
            }
            catch { }
        }
    }
}
#endif
