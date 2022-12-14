#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(PrefabAddress))]
    public class PrefabAddressDrawer : GenericAssetAddressDrawer<GameObject>
    {
    }

    [CustomPropertyDrawer(typeof(ComponentAddress))]
    public class ComponentAddressDrawer : GenericAssetAddressDrawer<Component>
    {
    }

    [CustomPropertyDrawer(typeof(ScriptableObjectAddress))]
    public class ScriptableObjectAddressDrawer : GenericAssetAddressDrawer<ScriptableObject>
    {
    }

    [CustomPropertyDrawer(typeof(AssetAddress))]
    public class AssetAddressDrawer : GenericAssetAddressDrawer<Object>
    {
    }

    public class GenericAssetAddressDrawer<T> : PropertyDrawer where T : Object
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, property.isExpanded) - (property.isExpanded ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing : 0);
        }

        bool _validate = true;
        // Draw the property inside the given rect
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GenericAssetAddress<T> v_loader = AssetAddressEditorInternalUtils.GetFieldValue(property) as GenericAssetAddress<T>;

            if (v_loader == null)
                base.OnGUI(position, property, label);
            else
            {
                _validate = _validate || (v_loader.IsResources() && v_loader.Asset == null);
                //Pre-Load asset inside resources folder
                if (_validate)
                {
                    _validate = true;
                    v_loader.Validate();
                }

                var v_asset = v_loader.Asset;
                var v_rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

                var v_oldShowMixed = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                var v_otherAsset = EditorGUI.ObjectField(v_rect, label, v_asset, typeof(T), true) as T;
                EditorGUI.showMixedValue = v_oldShowMixed;

                //Draw Minilabel to make easy to see if asset is inside resources folder
                if (!string.IsNullOrEmpty(v_loader.AssetPath))
                {
                    var v_size = 40;
                    var v_miniRect = new Rect(v_rect.xMax - v_size - (15 * EditorGUI.indentLevel), v_rect.y, v_size, v_rect.height);

                    var v_oldGuiColor = GUI.color;
                    GUI.color = EditorGUIUtility.isProSkin ? new Color(0, 0.7f, 0) : new Color(0, 0.5f, 0);
                    if (v_loader.KeepLoaded)
                        GUI.color = new Color(GUI.color.g, GUI.color.g, GUI.color.b);
                    EditorGUI.LabelField(v_miniRect, "res", EditorStyles.whiteMiniLabel);
                    GUI.color = v_oldGuiColor;
                }

                //Commit Changes
                if (v_asset != v_otherAsset)
                {
                    Undo.RecordObjects(property.serializedObject.targetObjects, "Changed Asset Address");
                    v_loader.Asset = v_otherAsset;
                    v_loader.Validate();
                    AssetAddressEditorInternalUtils.SetFieldValue(property, v_loader);
                }

                //Draw foldOut internal values
                v_rect.x -= 3;
                EditorGUI.PropertyField(v_rect, property, new GUIContent(""), false);
                //property.isExpanded = EditorGUI.Foldout(v_rect, property.isExpanded, "");
                v_rect.x += 3;

                //Enter self
                if (property.isExpanded)
                {
                    var v_endProperty = property.GetEndProperty();

                    property.NextVisible(true);
                    var v_oldGuiChanged = GUI.changed;

                    //Draw Childrens
                    v_rect = EditorGUI.IndentedRect(v_rect);
                    do
                    {
                        if (SerializedProperty.EqualContents(property, v_endProperty))
                            break;

                        //Prevent Draw Asset Again
                        if (property.name.Equals("m_asset"))
                            continue;

                        var v_oldGuiEnabled = GUI.enabled;
                        //Disable AutoManaged Fields
                        if (property.name.Equals("m_assetPath"))
                            GUI.enabled = false;

                        if (property.name.Equals("m_keepLoaded") && string.IsNullOrEmpty(v_loader.AssetPath))
                            continue;

                        v_rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        EditorGUI.PropertyField(v_rect, property, true);
                        GUI.enabled = v_oldGuiEnabled;
                    }
                    while (property.NextVisible(false));

                    //Validate Name
                    if (GUI.changed && !v_oldGuiChanged && v_loader.Name != null) { }
                }

            }
        }
    }

    #region Helper Classes

    internal class AssetAddressEditorInternalUtils
    {
        /// @note: switch/case derived from the decompilation of SerializedProperty's internal SetToValueOfTarget() method.
        internal static ValueT GetPropertyValue<ValueT>(SerializedProperty thisSP)
        {
            System.Type valueType = typeof(ValueT);

            // First, do special Type checks
            if (valueType.IsEnum)
            {
                return (ValueT)GetSafeEnumPropertyValue(thisSP, valueType);
            }

            // Next, check for literal UnityEngine struct-types
            // @note: ->object->ValueT double-casts because C# is too dumb to realize that that the ValueT in each situation is the exact type needed.
            // 	e.g. `return thisSP.colorValue` spits _error CS0029: Cannot implicitly convert type `UnityEngine.Color' to `ValueT'_
            // 	and `return (ValueT)thisSP.colorValue;` spits _error CS0030: Cannot convert type `UnityEngine.Color' to `ValueT'_
            if (typeof(Color).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.colorValue;
            else if (typeof(LayerMask).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.intValue;
            else if (typeof(Vector2).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.vector2Value;
            else if (typeof(Vector3).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.vector3Value;
            else if (typeof(Rect).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.rectValue;
            else if (typeof(AnimationCurve).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.animationCurveValue;
            else if (typeof(Bounds).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.boundsValue;
            else if (typeof(Gradient).IsAssignableFrom(valueType))
                return (ValueT)(object)GetSafeGradientPropertyValue(thisSP);
            else if (typeof(Quaternion).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.quaternionValue;

            // Next, check if derived from UnityEngine.Object base class
            if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.objectReferenceValue;

            // Finally, check for native type-families
            if (typeof(int).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.intValue;
            else if (typeof(bool).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.boolValue;
            else if (typeof(float).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.floatValue;
            else if (typeof(string).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.stringValue;
            else if (typeof(char).IsAssignableFrom(valueType))
                return (ValueT)(object)thisSP.intValue;

            // And if all fails, throw an exception.
            throw new System.NotImplementedException("Unimplemented propertyType " + thisSP.propertyType + ".");
        }

        internal static object GetSafeEnumPropertyValue(SerializedProperty thisSP, System.Type p_type)
        {
            int v_flagValue = thisSP.intValue;
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

        internal static object GetPropertyValue(SerializedProperty thisSP)
        {
            switch (thisSP.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return thisSP.intValue;
                case SerializedPropertyType.Boolean:
                    return thisSP.boolValue;
                case SerializedPropertyType.Float:
                    return thisSP.floatValue;
                case SerializedPropertyType.String:
                    return thisSP.stringValue;
                case SerializedPropertyType.Color:
                    return thisSP.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return thisSP.objectReferenceValue;
                case SerializedPropertyType.LayerMask:
                    return thisSP.intValue;
                case SerializedPropertyType.Enum:
                    {
                        if (thisSP.enumValueIndex >= 0)
                            return thisSP.enumValueIndex;
                        else
                            return thisSP.intValue;
                    }
                case SerializedPropertyType.Vector2:
                    return thisSP.vector2Value;
                case SerializedPropertyType.Vector3:
                    return thisSP.vector3Value;
                case SerializedPropertyType.Rect:
                    return thisSP.rectValue;
                case SerializedPropertyType.ArraySize:
                    return thisSP.intValue;
                case SerializedPropertyType.Character:
                    return (char)thisSP.intValue;
                case SerializedPropertyType.AnimationCurve:
                    return thisSP.animationCurveValue;
                case SerializedPropertyType.Bounds:
                    return thisSP.boundsValue;
                case SerializedPropertyType.Gradient:
                    return GetSafeGradientPropertyValue(thisSP);
                case SerializedPropertyType.Quaternion:
                    return thisSP.quaternionValue;

                default:
                    throw new System.NotImplementedException("Unimplemented propertyType " + thisSP.propertyType + ".");
            }
        }

        internal static object GetFieldValue(SerializedProperty p_property)
        {
            object v_return = null;
            try
            {
                if (p_property.serializedObject != null && p_property.serializedObject.targetObject != null)
                {
                    try
                    {
                        object v_target = GetTargetFromSerializedProperty(p_property);
                        if (v_target != null)
                        {
                            FieldInfo v_fieldToGet = GetSerializableFieldWithName(v_target, p_property.name, true);
                            if (v_fieldToGet != null)
                                v_return = v_fieldToGet.GetValue(v_target);
                            //We Must Set Value in Array
                            else if (IsArrayElement(p_property))
                            {
                                int v_index = GetElementIndex(p_property);
                                SerializedProperty v_parentProperty = GetParentProperty(p_property);
                                if (v_parentProperty != null)
                                {
                                    IList v_listValue = GetFieldValue(v_parentProperty) as IList;
                                    if (v_listValue != null)
                                    {
                                        v_return = v_listValue[v_index];
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return ConvertToSafeFieldValue(p_property, v_return);
        }

        internal static void SetFieldValue(SerializedProperty p_property, object p_value)
        {
            try
            {
                if (p_property.serializedObject != null && p_property.serializedObject.targetObject != null)
                {
                    p_value = ConvertToSafeFieldValue(p_property, p_value);
                    object v_target = GetTargetFromSerializedProperty(p_property);
                    if (v_target != null)
                    {
                        FieldInfo v_fieldToSet = GetSerializableFieldWithName(v_target, p_property.name);
                        if (v_fieldToSet != null)
                            v_fieldToSet.SetValue(v_target, p_value);
                        //We Must Set Value in Array
                        else if (IsArrayElement(p_property))
                        {
                            int v_index = GetElementIndex(p_property);
                            SerializedProperty v_parentProperty = GetParentProperty(p_property);
                            if (v_parentProperty != null)
                            {
                                IList v_listValue = GetFieldValue(v_parentProperty) as IList;
                                if (v_listValue != null)
                                {
                                    v_listValue[v_index] = p_value;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        /// Access to SerializedProperty's internal gradientValue property getter, in a manner that'll only soft break (returning null) if the property changes or disappears in future Unity revs.
        static Gradient GetSafeGradientPropertyValue(SerializedProperty sp)
        {
            BindingFlags instanceAnyPrivacyBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
                "gradientValue",
                instanceAnyPrivacyBindingFlags,
                null,
                typeof(Gradient),
                new System.Type[0],
                null
            );
            if (propertyInfo == null)
                return null;

            Gradient gradientValue = propertyInfo.GetValue(sp, null) as Gradient;
            return gradientValue;
        }

        static SerializedProperty GetParentProperty(SerializedProperty p_property)
        {
            SerializedProperty v_parentProperty = null;
            if (p_property != null && p_property.serializedObject != null && p_property.serializedObject.targetObject != null)
            {
                try
                {
                    string v_propertyPath = ReplaceLast(p_property.propertyPath.Replace("Array.data", "%"), ".", "$");
                    v_propertyPath = (v_propertyPath.Split('$'))[0].Replace("%", "Array.data").Trim();
                    if (!string.IsNullOrEmpty(v_propertyPath))
                        v_parentProperty = p_property.serializedObject.FindProperty(v_propertyPath);
                }
                catch { }
            }
            return v_parentProperty;
        }

        /* The target of a property is the object in one level above
         * Ex: 
         * public class MyClass
         * {
         *     int m_field;
         * } 
         * the target of m_field will be MyClass instance.. if you want to get the m_field value use GetFieldValue instead
        */
        static object GetTargetFromSerializedProperty(SerializedProperty p_property)
        {
            object v_currentTarget = null;
            if (p_property != null && p_property.serializedObject != null && p_property.serializedObject.targetObject != null)
            {
                string v_propertyPath = p_property.propertyPath.Replace("Array.data", "%");
                string[] v_paths = v_propertyPath.Split(new char[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries);
                v_currentTarget = p_property.serializedObject.targetObject;
                for (int i = 0; i < v_paths.Length; i++)
                {
                    string v_fieldName = v_paths[i].Trim();
                    if (!string.IsNullOrEmpty(v_fieldName) && !string.Equals(p_property.name, v_fieldName))
                    {
                        if (v_fieldName.Contains("%"))
                        {
                            //If is not the last field in paths because target of last field must be the list in self
                            //if (i < v_paths.Length - 1)
                            if (v_paths[i].Contains("%["))
                            {
                                int v_index = -1;
                                string v_indexStr = v_fieldName.Replace("%[", "").Replace("]", "").Trim();
                                int.TryParse(v_indexStr, out v_index);
                                if (v_currentTarget is System.Collections.ICollection)
                                {
                                    int v_counter = 0;
                                    foreach (object v_object in ((System.Collections.ICollection)v_currentTarget))
                                    {
                                        if (v_counter == v_index)
                                        {
                                            v_currentTarget = v_object;
                                            break;
                                        }
                                        v_counter++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            FieldInfo v_field = GetSerializableFieldWithName(v_currentTarget, v_fieldName, true);
                            if (v_field != null)
                                v_currentTarget = v_field.GetValue(v_currentTarget);
                        }
                    }
                }
            }
            return v_currentTarget;
        }

        static int GetElementIndex(SerializedProperty p_property)
        {
            if (p_property != null && p_property.serializedObject != null && p_property.serializedObject.targetObject != null)
            {
                if (p_property.propertyPath.Contains("Array.data"))
                {
                    try
                    {
                        string v_propertyPath = ReplaceLast(p_property.propertyPath, "Array.data", "%");
                        v_propertyPath = v_propertyPath.Contains("%") ? v_propertyPath.Split('%')[1] : "[-1]";
                        string v_indexString = v_propertyPath.Replace("[", "").Replace("]", "").Trim();
                        int v_index = -1;
                        int.TryParse(v_indexString, out v_index);
                        return v_index;
                    }
                    catch { }
                }
            }
            return -1;
        }

        static bool IsArrayElement(SerializedProperty p_property)
        {
            if (p_property != null && p_property.serializedObject != null && p_property.serializedObject.targetObject != null)
            {
                if (p_property.propertyPath.EndsWith("]")) //ex: m_list.Array.data[0]
                    return true;
            }
            return false;
        }

        static FieldInfo GetFieldInfo(SerializedProperty p_property)
        {
            return GetFieldInfoOrNullIfIsAnArrayElement(p_property);
        }

        static FieldInfo GetFieldInfoOrNullIfIsAnArrayElement(SerializedProperty p_property)
        {
            if (IsArrayElement(p_property))
            {
                return null;
            }
            return GetFieldInfoOrArrayInfoIfIsArrayElement(p_property);
        }

        static FieldInfo GetFieldInfoOrArrayInfoIfIsArrayElement(SerializedProperty p_property)
        {
            FieldInfo v_currentField = null;
            object v_currentTarget = null;
            if (p_property != null && p_property.serializedObject != null && p_property.serializedObject.targetObject != null)
            {
                string v_propertyPath = p_property.propertyPath.Replace("Array.data", "%");
                v_propertyPath = v_propertyPath.Contains("%") ? v_propertyPath.Split('%')[0] : v_propertyPath;
                string[] v_paths = v_propertyPath.Split('.');
                v_currentTarget = p_property.serializedObject.targetObject;
                for (int i = 0; i < v_paths.Length; i++)
                {
                    string v_fieldName = v_paths[i].Trim();
                    if (!string.IsNullOrEmpty(v_fieldName) && !string.Equals(p_property.name, v_fieldName))
                    {
                        v_currentField = GetSerializableFieldWithName(v_currentTarget, v_fieldName);
                        v_currentTarget = v_currentField.GetValue(v_currentTarget);

                    }
                }
            }
            return v_currentField;
        }

        static System.Reflection.FieldInfo GetSerializableFieldWithName(object p_target, string p_name, bool p_acceptHiddenFields = false)
        {
            System.Reflection.FieldInfo[] v_fields = GetAllSerializableFields(p_target, p_acceptHiddenFields);
            foreach (FieldInfo v_field in v_fields)
            {
                try
                {
                    if (v_field.Name.Equals(p_name))
                    {
                        return v_field;
                    }
                }
                catch { }
            }
            return null;
        }

        static System.Reflection.FieldInfo[] GetAllSerializableFields(object p_target, bool p_acceptHiddenFields = false)
        {
            List<System.Reflection.FieldInfo> v_processedFields = new List<FieldInfo>();
            System.Type v_type = p_target != null ? p_target.GetType() : null;
            while (v_type != null)
            {
                List<System.Reflection.FieldInfo> v_currentFields = new List<FieldInfo>();
                //All Privates and Publics in this Specific Type
                System.Reflection.FieldInfo[] v_fieldOfTheType = v_type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public);
                foreach (System.Reflection.FieldInfo v_field in v_fieldOfTheType)
                {
                    try
                    {
                        if (!v_field.IsNotSerialized && (v_field.IsPublic || System.Attribute.IsDefined(v_field, typeof(SerializeField))))
                        {
                            bool v_isHidden = false;
                            try
                            {
                                v_isHidden = System.Attribute.IsDefined(v_field, typeof(HideInInspector));
                            }
                            catch { }
                            if (!v_isHidden || p_acceptHiddenFields)
                            {
                                v_currentFields.Add(v_field);
                            }
                        }
                    }
                    catch { }
                }
                v_type = v_type.BaseType;
                //Order by Lower Inheritance to Higher Inheritance
                v_currentFields.AddRange(v_processedFields);
                v_processedFields = v_currentFields;
            }
            return v_processedFields.ToArray();
        }

        static object ConvertToSafeFieldValue(SerializedProperty p_property, object p_value)
        {
            try
            {
                if (p_property.propertyType == SerializedPropertyType.Enum)
                {
                    FieldInfo v_field = GetFieldInfoOrNullIfIsAnArrayElement(p_property);
                    System.Type v_type = v_field != null ? v_field.FieldType : null;
                    if (v_type == null)
                        v_type = IsArrayElement(p_property) ? GetElementType(((IList)GetFieldValue(GetParentProperty(p_property)))) : null;
                    int v_flagValue = (int)p_value;
                    //Pick All Possible values of Flagged Enum because unity return -1 when all flags selected
                    if (v_flagValue == -1)
                    {
                        v_flagValue = 0;
                        foreach (object v_value in System.Enum.GetValues(v_type))
                        {
                            v_flagValue = (int)v_value | (int)v_flagValue;
                        }
                    }
                    return System.Enum.ToObject(v_type, v_flagValue);
                }
            }
            catch { }
            return p_value;
        }

        static string ReplaceLast(string p_text, string p_oldText, string p_newText)
        {
            try
            {
                p_oldText = p_oldText != null ? p_oldText : "";
                p_newText = p_newText != null ? p_newText : "";
                p_text = p_text != null ? p_text : "";

                int v_pos = string.IsNullOrEmpty(p_oldText) ? -1 : p_text.LastIndexOf(p_oldText);
                if (v_pos < 0)
                    return p_text;

                string v_result = p_text.Remove(v_pos, p_oldText.Length).Insert(v_pos, p_newText);
                return v_result;
            }
            catch { }
            return p_text;
        }

        static System.Type GetElementType(IList p_list)
        {
            System.Type v_returnedType = null;
            System.Type v_arrayType = p_list != null ? p_list.GetType() : null;
            if (v_arrayType != null)
            {
                if (v_arrayType.IsArray)
                {
                    v_returnedType = v_arrayType.GetElementType();
                }
                else
                {

                    System.Type[] v_interfaceTypes = v_arrayType.GetInterfaces();
                    foreach (System.Type v_interfaceType in v_interfaceTypes)
                    {
                        string v_interfaceSafeName = v_interfaceType.FullName;
                        if (v_interfaceSafeName.Contains("IList`1") ||
                            v_interfaceSafeName.Contains("ICollection`1") ||
                            v_interfaceSafeName.Contains("IEnumerable`1"))

                        {
                            try
                            {
                                v_returnedType = v_interfaceType.GetGenericArguments()[0];
                                break;
                            }
                            catch { }
                        }
                    }
                }
            }
            return v_returnedType;
        }
    }

    #endregion
}

#endif
