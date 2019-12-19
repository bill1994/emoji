#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using KyubEditor.Extensions;
using Kyub;

namespace KyubEditor
{
    [CustomPropertyDrawer(typeof(DependentFieldAttribute))]
    public partial class DependentFieldDrawer : SpecificFieldDrawer
    {
        #region Normal Component Functions

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            DependentFieldAttribute attr = attribute as DependentFieldAttribute;
            if (attr != null && attr.DrawOption != DependentDrawOptionEnum.DontDrawFieldWhenNotExpectedValue)
                return EditorGUI.GetPropertyHeight(property, label, true);
            else
                return ValueEqualsTrigger(property) ? EditorGUI.GetPropertyHeight(property, label, true) : -2;
        }

        protected override void DrawComponent(Rect position, SerializedProperty property, GUIContent label, System.Type p_type)
        {
            DependentFieldAttribute attr = attribute as DependentFieldAttribute;
            if (attr != null)
            {
                bool v_dependentFieldValue = ValueEqualsTrigger(property);
                if (v_dependentFieldValue || attr.DrawOption != DependentDrawOptionEnum.DontDrawFieldWhenNotExpectedValue)
                {
                    bool v_oldGUIEnabled = GUI.enabled;
                    if (!v_dependentFieldValue && attr.DrawOption == DependentDrawOptionEnum.ReadOnlyFieldWhenNotExpectedValue)
                        GUI.enabled = false;
                    DrawComponentAfterDependenceCheck(position, property, label, p_type);
                    GUI.enabled = v_oldGUIEnabled;
                }
            }
        }

        protected virtual void DrawComponentAfterDependenceCheck(Rect position, SerializedProperty property, GUIContent label, System.Type p_type)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        //Custom Implementation Here
        protected virtual bool ValueEqualsTrigger(SerializedProperty property)
        {
            bool v_return = false;
            try
            {
                DependentFieldAttribute attr = attribute as DependentFieldAttribute;
                if (string.IsNullOrEmpty(attr.DependentFieldName))
                    return true;
                SerializedProperty v_dependentField = property.serializedObject != null && attr != null ? property.serializedObject.FindProperty(GetPrePath(property) + attr.DependentFieldName) : null;
                //bool v_isEnum = v_dependentField.propertyType == SerializedPropertyType.Enum && attr.ValueToTrigger != null && attr.ValueToTrigger.GetType().IsEnum;
                object v_dependentFieldValue = v_dependentField.GetFieldValue();// v_isEnum ? v_dependentField.GetSafeEnumPropertyValue(attr.ValueToTrigger.GetType()) : v_dependentField.GetPropertyValue();
                if (attr != null)
                {
                    if ((attr.ValueToTrigger == null && v_dependentFieldValue == null) ||
                    (attr.ValueToTrigger != null && attr.ValueToTrigger.Equals(v_dependentFieldValue)))
                        v_return = true;
                    //Invert value if we must use not equal comparer
                    if (attr.UseNotEqualComparer)
                        v_return = !v_return;
                }
            }
            catch { }
            return v_return;
        }

        public virtual string GetPrePath(SerializedProperty property)
        {
            string v_prePath = "";
            if (property != null && !string.IsNullOrEmpty(property.propertyPath))
            {
                if (property.propertyPath.Contains("."))
                {
                    string v_pathToCheck = property.propertyPath;
                    string v_stringToRemove = ".Array.data";
                    if (property.propertyPath.Contains(v_stringToRemove))
                    {
                        int v_pathLastIndex = property.propertyPath.LastIndexOf(v_stringToRemove);
                        int v_pathSize = v_pathLastIndex;
                        if (v_pathSize > 0 && v_pathSize <= property.propertyPath.Length)
                            v_pathToCheck = property.propertyPath.Substring(0, v_pathSize);
                        else
                            v_pathToCheck = "";
                    }
                    int v_lastIndex = v_pathToCheck.LastIndexOf(".");
                    int v_size = v_lastIndex + 1;
                    if (v_size > 0 && v_size <= v_pathToCheck.Length)
                        v_prePath = v_pathToCheck.Substring(0, v_size);
                }
            }
            return v_prePath;
        }

        #endregion
    }
}
#endif
