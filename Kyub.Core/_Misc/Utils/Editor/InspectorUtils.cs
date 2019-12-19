#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using Kyub.Extensions;
using Kyub;
using Kyub.Collections;

namespace KyubEditor
{
    public static class InspectorUtils
    {
        #region Other Draws

        public static bool DrawButton(string p_text, Color v_color, params GUILayoutOption[] p_layoutOption)
        {
            Color v_oldColor = GUI.backgroundColor;
            GUI.backgroundColor = v_color;
            bool v_sucess = GUILayout.Button(p_text, p_layoutOption);
            GUI.backgroundColor = v_oldColor;
            return v_sucess;
        }

        public static void DrawTitleText(string p_text, Color v_color)
        {
            Color v_oldColor = GUI.backgroundColor;
            GUI.backgroundColor = v_color;
            EditorGUILayout.HelpBox(p_text, MessageType.None);
            GUI.backgroundColor = v_oldColor;
        }

        #endregion

        #region Serializable Type Drawer

        public static SerializableType SerializableTypePopup(Rect p_rect, string p_label, SerializableType p_value, System.Type p_filterType, bool p_acceptGenericDefinition = false, bool p_acceptAbstractDefinition = false, bool p_acceptNull = true, bool p_filterAssemblies = false, bool p_canDrawFoldOut = true)
        {
            return SerializableTypePopupInternal(p_rect, p_label, p_value, p_filterType, false, p_acceptGenericDefinition, p_acceptAbstractDefinition, p_acceptNull, p_filterAssemblies, p_canDrawFoldOut);
        }

        public static SerializableType SerializableTypePopup(string p_label, SerializableType p_value, System.Type p_filterType, bool p_acceptGenericDefinition = false, bool p_acceptAbstractDefinition = false, bool p_acceptNull = true, bool p_filterAssemblies = false, bool p_canDrawFoldOut = true, params GUILayoutOption[] p_layout)
        {
            return SerializableTypePopupInternal(new Rect(), p_label, p_value, p_filterType, true, p_acceptGenericDefinition, p_acceptAbstractDefinition, p_acceptNull, p_filterAssemblies, p_canDrawFoldOut, p_layout);
        }

        static SerializableType SerializableTypePopupInternal(Rect p_rect, string p_label, SerializableType p_value, System.Type p_filterType, bool p_guiLayout, bool p_acceptGenericDefinition, bool p_acceptAbstractDefinition = false, bool p_acceptNull = true, bool p_filterAssemblies = false, bool p_canDrawFoldOut = true, params GUILayoutOption[] p_layout)
        {
            if (p_value == null)
                p_value = new SerializableType();
            if (p_value.CastedType == null && !p_acceptNull)
                p_value = p_filterType;
            Color v_oldColor = GUI.backgroundColor;
            SerializableType v_return = new SerializableType();
            try
            {
                Vector2 v_offset = new Vector2(10, 16);
                Rect v_labelRect = new Rect(p_rect.x, p_rect.y, p_rect.width / 3.0f, v_offset.y);
                Rect v_assemblyRect = new Rect(v_labelRect.x + v_labelRect.width, p_rect.y, 30, v_offset.y);
                Rect v_typeRect = new Rect(v_assemblyRect.x + v_assemblyRect.width, p_rect.y, Mathf.Max(0, p_rect.width - (v_labelRect.width + v_assemblyRect.width)), v_offset.y);

                SerializableTypeCache v_cache = SerializableTypeCacheController.GetCache(p_value, p_filterType, p_acceptGenericDefinition, p_acceptAbstractDefinition, p_acceptNull);

                if (p_guiLayout)
                {
                    EditorGUILayout.BeginVertical(p_layout);
                    EditorGUILayout.BeginHorizontal();
                }
                if (!p_canDrawFoldOut)
                    p_value.FoldOut = true;

                //Draw Label
                bool v_drawFoldOut = p_value.CastedType != null && p_value.CastedType.GetGenericArguments().Length > 0 && p_canDrawFoldOut;
                if (v_drawFoldOut)
                {
                    if (p_guiLayout)
                    {
                        p_value.FoldOut = EditorGUILayout.Foldout(p_value.FoldOut, p_label);
                    }
                    else
                    {
                        p_value.FoldOut = EditorGUI.Foldout(v_labelRect, p_value.FoldOut, p_label);
                    }
                }
                //else
                //{
                    //if (p_guiLayout)
                    //{
                        //EditorGUILayout.PrefixLabel(p_label);
                    //}
                    //else
                    //    EditorGUI.LabelField(v_labelRect, p_label);
                //}

                //Draw Popup Select Assembly
                int v_selectedIndex = v_cache.SelectedAssemblyIndex;

                if (p_filterAssemblies)
                {
                    GUI.backgroundColor = v_selectedIndex > 0 || (!p_acceptNull && v_selectedIndex >= 0) ? v_oldColor : new Color(0.8f, 0.2f, 0);
                    //GUI.backgroundColor = v_selectedIndex > 0 || (!p_acceptNull && v_selectedIndex >= 0) ? new Color(0.78f, 0.9f, 0.75f) : new Color(0.8f, 0.2f, 0);

                    if (!v_drawFoldOut)
                    {
                        if (p_guiLayout)
                            v_selectedIndex = EditorGUILayout.Popup(p_label, v_selectedIndex, v_cache.PossibleAssembliesString, GUILayout.Width(30));
                        else
                            v_selectedIndex = EditorGUI.Popup(v_assemblyRect, p_label, v_selectedIndex, v_cache.PossibleAssembliesString);
                    }
                    else
                    {
                        if (p_guiLayout)
                            v_selectedIndex = EditorGUILayout.Popup(v_selectedIndex, v_cache.PossibleAssembliesString, GUILayout.Width(30));
                        else
                            v_selectedIndex = EditorGUI.Popup(v_assemblyRect, v_selectedIndex, v_cache.PossibleAssembliesString);
                    }
                }

                Assembly v_newAssembly = v_cache.PossibleAssemblies.Length > v_selectedIndex && v_selectedIndex >= 0 ? v_cache.PossibleAssemblies[v_selectedIndex] : null;
                bool v_assemblyChanged = false;
                if (v_newAssembly != v_cache.CurrentAssembly)
                {
                    v_return.CastedType = null;
                    v_return.StringType = v_newAssembly != null ? ", " + (v_newAssembly.FullName) : null;
                    //Get New Cache, now with assembly Changes
                    v_cache = SerializableTypeCacheController.GetCache(v_return, p_filterType, p_acceptGenericDefinition, p_acceptAbstractDefinition, p_acceptNull);
                    v_assemblyChanged = true;
                    //v_return.EditorCacheController = null;
                }
                GUI.backgroundColor = v_oldColor;

                //Pick All Types if dont use Assembly Filter
                System.Type[] v_assemblyTypes = p_filterAssemblies ? v_cache.PossibleTypesInCurrentAssembly : v_cache.PossibleTypesInAllAssemblies;
                string[] v_assemblyTypesString = p_filterAssemblies ? v_cache.PossibleTypesInCurrentAssemblyString : v_cache.PossibleTypesInAllAssembliesString;
                int v_oldSelectedIndex = p_filterAssemblies ? v_cache.SelectedTypeIndexInCurrentAssembly : v_cache.SelectedTypeIndexInAllAssemblies;
                v_selectedIndex = v_oldSelectedIndex;
                //Set Selected Index
                SetTypeSelectedIndex(ref v_assemblyTypes, ref p_value, ref v_selectedIndex, v_assemblyChanged, p_acceptNull);

                //Draw Popup Select Type
                GUI.backgroundColor = v_selectedIndex > 0 || (!p_acceptNull && v_selectedIndex >= 0) ? v_oldColor : new Color(0.8f, 0.2f, 0);
                //GUI.backgroundColor = v_selectedIndex > 0 || (!p_acceptNull && v_selectedIndex >= 0)? new Color(0.78f, 0.9f, 0.75f) : new Color(0.8f, 0.2f, 0);

                if (!p_filterAssemblies & !v_drawFoldOut)
                {
                    if (p_guiLayout)
                        v_selectedIndex = EditorGUILayout.Popup(p_label, v_selectedIndex, v_assemblyTypesString);
                    else
                        v_selectedIndex = EditorGUI.Popup(p_rect, p_label, v_selectedIndex, v_assemblyTypesString);
                }
                else
                {
                    if (p_guiLayout)
                        v_selectedIndex = EditorGUILayout.Popup(v_selectedIndex, v_assemblyTypesString);
                    else
                        v_selectedIndex = EditorGUI.Popup(v_typeRect, v_selectedIndex, v_assemblyTypesString);
                }
                if (v_selectedIndex == -1 && v_assemblyTypes.Length > 0)
                    v_selectedIndex = 0;
                System.Type v_currentType = v_assemblyTypes.Length > v_selectedIndex && v_selectedIndex >= 0 ? v_assemblyTypes[v_selectedIndex] : null;
                GUI.backgroundColor = v_oldColor;
                if (v_selectedIndex != v_oldSelectedIndex)
                {
                    //Apply Values
                    v_return.CastedType = p_value != null && TypeImplementGenericTypeDefinition(p_value.CastedType, v_currentType) ? p_value.CastedType : v_currentType;
                    v_return.StringType = p_value != null && TypeImplementGenericTypeDefinition(p_value.CastedType, v_currentType) ? p_value.StringType : v_return.StringType;
                    if (v_return.CastedType == null)
                        v_return.StringType = v_newAssembly != null ? ", " + (v_newAssembly.FullName) : null;
                }
                else if (!v_assemblyChanged)
                {
                    v_return.CastedType = p_value.CastedType;
                    v_return.StringType = p_value.StringType;
                }

                if (p_guiLayout)
                    EditorGUILayout.EndHorizontal();

                //Generic Drawer Type Definition
                Assembly v_currentAssembly = v_newAssembly;
                if (p_acceptGenericDefinition)
                    DrawGenericParametersSelector(ref v_currentAssembly, ref p_value, ref v_return, ref v_currentType, p_filterType, ref p_rect, v_offset, p_guiLayout, p_acceptAbstractDefinition);

                if (p_guiLayout)
                    EditorGUILayout.EndVertical();
                v_return.FoldOut = p_value.CastedType != null && p_value.CastedType.GetGenericArguments().Length > 0 ? p_value.FoldOut : true;
                if (!p_canDrawFoldOut)
                    v_return.FoldOut = true;
            }
            catch { }
            return v_return;
        }

        static void SetTypeSelectedIndex(ref System.Type[] p_assemblyTypes, ref SerializableType p_value, ref int p_selectedIndex, bool p_assemblyChanged, bool p_acceptNull)
        {
            List<System.Type> v_assemblyTypesList = new List<System.Type>(p_assemblyTypes);
            if (p_assemblyChanged)
                p_selectedIndex = p_acceptNull ? 1 : 0;
            else
            {
                if (p_value != null && p_value.CastedType != null && p_value.CastedType.GetGenericArguments().Length > 0 && (p_selectedIndex < 0 || (p_selectedIndex == 0 && !p_acceptNull)))
                {
                    p_selectedIndex = -1;
                    string[] v_splittedValues = p_value.StringType.Split('`');
                    string v_typeString = v_splittedValues.Length > 0 ? v_splittedValues[0] : (p_value.CastedType != null ? p_value.CastedType.FullName : "");
                    string v_genericArgString = v_splittedValues.Length > 1 && v_splittedValues[1].Length > 1 ? v_splittedValues[1][0] + "" : "";
                    if (!string.IsNullOrEmpty(v_genericArgString))
                        v_typeString += "`" + v_genericArgString;
                    for (int i = 0; i < v_assemblyTypesList.Count; i++)
                    {
                        System.Type v_type = v_assemblyTypesList[i];
                        if (v_type != null && v_splittedValues.Length > 0 && v_type.FullName.Contains(v_typeString))
                        {
                            p_selectedIndex = i;
                            break;
                        }
                    }
                }
                //else
                //{
                //	p_selectedIndex = p_value.CastedType != null ? v_assemblyTypesList.IndexOf(p_value.CastedType) : -1;
                //}
            }
        }

        static void DrawGenericParametersSelector(ref Assembly p_currentAssembly, ref SerializableType p_value, ref SerializableType p_return, ref System.Type p_currentType, System.Type p_filterType, ref Rect p_rect, Vector2 p_offset, bool p_guiLayout, bool p_acceptAbstractDefinition)
        {
            if (p_return.CastedType != null && p_return.CastedType.FullName.Contains("`"))
            {
                // If FilterType is a generic implementation the Currenttype, so the CurrentType is smaller in hyerarchy than FilterType and both are the same class. 
                // In this case, we must filter generic parameters using FilterType instead of CurrentType because FilterType is a IsGenericTypeDefinition.
                // Ex: FilterType = MyClass<int> and CurrentType = MyClass<T>
                // PS: in this case we must DISABLE the type selection because we cant change the GenericParater to one inherited: MyClass<Type> do not inherit from MyClass<TypeInherited>
                string v_filterTypeSafeName = GetSafeTypedNameInAssembly(p_filterType);
                string v_currentTypeSafeName = GetSafeTypedNameInAssembly(p_currentType);
                bool v_filterIsGreaterThanCurrentType = p_filterType.IsGenericType && p_currentType.IsGenericType && v_filterTypeSafeName.Equals(v_currentTypeSafeName);
                System.Type[] v_genericArguments = v_filterIsGreaterThanCurrentType ? p_filterType.GetGenericArguments() : p_currentType.GetGenericArguments();
                System.Type[] v_specializedArguments = p_return.CastedType.GetGenericArguments();
                string p_composedInternalType = "`" + v_genericArguments.Length + "[";

                bool v_oldGuiEnabled = GUI.enabled;
                GUI.enabled = !v_filterIsGreaterThanCurrentType;
                for (int i = 0; i < v_genericArguments.Length; i++)
                {
                    System.Type v_genericArgumentType = v_genericArguments[i];
                    System.Type v_specializedArgumentType = v_specializedArguments.Length > i ? v_specializedArguments[i] : null;
                    if (v_specializedArgumentType != null && v_specializedArgumentType.IsGenericParameter)
                        v_specializedArgumentType = null;
                    if (v_genericArgumentType != null)
                    {
                        p_composedInternalType += (i > 0) ? ",[" : "[";
                        System.Type v_baseType = v_filterIsGreaterThanCurrentType ? v_genericArgumentType : v_genericArgumentType.BaseType;
                        if (v_baseType == null)
                            v_baseType = typeof(object);
                        if (v_specializedArgumentType == null)
                            v_specializedArgumentType = v_baseType;

                        SerializableType v_argument = new SerializableType(v_specializedArgumentType);
                        Rect v_argumentRect = new Rect(p_rect.x + p_offset.x, p_rect.y + p_offset.y * (i + 1), p_rect.width - (p_offset.x), p_offset.y);
                        if (p_value.FoldOut)
                        {
                            if (p_guiLayout)
                            {
                                EditorGUILayout.BeginHorizontal();
                                GUILayout.Space(p_offset.x);
                            }
                            v_argument = SerializableTypePopupInternal(v_argumentRect, "Param " + (i + 1) + ": ", v_argument, v_baseType, p_guiLayout, false, p_acceptAbstractDefinition, false);
                            if (p_guiLayout)
                                EditorGUILayout.EndHorizontal();
                        }
                        p_composedInternalType += v_argument.StringType;
                        p_composedInternalType += "]";

                    }
                }
                GUI.enabled = v_oldGuiEnabled;
                p_composedInternalType += "]";
                string[] v_splittedValue = p_return.StringType.Split('`');
                if (v_splittedValue.Length > 0)
                {
                    string v_castedTypeString = v_splittedValue[0] + p_composedInternalType + ", " + p_currentAssembly.FullName;
                    System.Type v_type = System.Type.GetType(v_castedTypeString);
                    if (v_type != null)
                    {
                        if (v_type != p_return.CastedType)
                            p_return.CastedType = v_type;
                    }
                    else
                        p_return.CastedType = p_currentType;
                    p_return.StringType = v_castedTypeString;
                }
            }
        }

        //Used to find correct type in assembly when filter is generic
        static string GetSafeTypedNameInAssembly(SerializableType p_type)
        {
            if (p_type != null)
            {
                string[] v_splittedValues = p_type.StringType.Split('`');
                string v_typeString = v_splittedValues.Length > 0 ? v_splittedValues[0] : (p_type.CastedType != null ? p_type.CastedType.FullName : "");
                string v_genericArgString = v_splittedValues.Length > 1 && v_splittedValues[1].Length > 1 ? v_splittedValues[1][0] + "" : "";
                if (!string.IsNullOrEmpty(v_genericArgString))
                    v_typeString += "`" + v_genericArgString;
                return v_typeString;
            }
            return "";
        }

        static bool TypeImplementGenericTypeDefinition(System.Type p_type, System.Type p_genericType)
        {
            if (p_type != null && p_genericType != null && p_genericType.IsGenericTypeDefinition)
            {
                string[] v_typeStringSplitted = p_type.FullName.Split('`');
                string[] v_genericTypeStringSplitted = p_genericType.FullName.Split('`');

                string v_typeString = v_typeStringSplitted.Length >= 2 && v_typeStringSplitted[1].Length > 0 ? v_typeStringSplitted[0] + '`' + v_typeStringSplitted[1].Split('[') : "";
                string v_genericTypeString = v_genericTypeStringSplitted.Length >= 2 && v_genericTypeStringSplitted[1].Length > 0 ? v_genericTypeStringSplitted[0] + '`' + v_genericTypeStringSplitted[1].Split('[') : "";
                if (!string.IsNullOrEmpty(v_typeString) && !string.IsNullOrEmpty(v_genericTypeString) && string.Equals(v_typeString, v_genericTypeString))
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Bitmap Popup Drawer

        public static System.IConvertible BitmapPopup(string p_label, System.IConvertible p_enum)
        {
            return BitmapPopupInternal(new Rect(), true, p_label, p_enum, p_enum.GetType());
        }

        public static System.IConvertible BitmapPopup(string p_label, System.IConvertible p_enum, System.Type p_type)
        {
            return BitmapPopupInternal(new Rect(), true, p_label, p_enum, p_type);
        }

        public static System.IConvertible BitmapPopup(Rect p_rect, string p_label, System.IConvertible p_enum)
        {
            return BitmapPopupInternal(p_rect, false, p_label, p_enum, p_enum.GetType());
        }

        public static System.IConvertible BitmapPopup(Rect p_rect, string p_label, System.IConvertible p_enum, System.Type p_type)
        {
            return BitmapPopupInternal(p_rect, false, p_label, p_enum, p_type);
        }

        public static System.IConvertible BitmapPopupInternal(Rect p_rect, bool p_useGuiLayout, string p_label, System.IConvertible p_enum, System.Type p_type)
        {
            System.IConvertible p_return = p_enum;
            if (EnumExtensions.CheckIfIsEnum(p_type, true))
            {
                try
                {
                    int v_value = p_enum == null ? (int)System.Enum.GetValues(p_type).GetValue(0) : (int)((System.IConvertible)p_enum);
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_label))
                            v_value = EditorGUILayout.MaskField(v_value, System.Enum.GetNames(p_type));
                        else
                            v_value = EditorGUILayout.MaskField(p_label, v_value, System.Enum.GetNames(p_type));
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_label))
                            v_value = EditorGUI.MaskField(p_rect, v_value, System.Enum.GetNames(p_type));
                        else
                            v_value = EditorGUI.MaskField(p_rect, p_label, v_value, System.Enum.GetNames(p_type));
                    }
                    p_return = ((System.IConvertible)v_value);
                }
                catch { }
            }
            else if (EnumExtensions.CheckIfIsEnum(p_type, false))
            {
                try
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_label))
                            p_return = (System.Enum)EditorGUILayout.EnumPopup((System.Enum)p_enum);
                        else
                            p_return = (System.Enum)EditorGUILayout.EnumPopup(p_label, (System.Enum)p_enum);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_label))
                            p_return = (System.Enum)EditorGUI.EnumPopup(p_rect, (System.Enum)p_enum);
                        else
                            p_return = (System.Enum)EditorGUI.EnumPopup(p_rect, p_label, (System.Enum)p_enum);
                    }
                }
                catch { }
            }

            return p_return;
        }

        #endregion

        #region Wild Card Drawer

        public static object DrawType<ObjectType>(string p_labelString, object p_object)
        {
            return (ObjectType)DrawTypeInternal(new Rect(), true, p_labelString, p_object, typeof(ObjectType));
        }

        public static object DrawType(string p_labelString, object p_object)
        {
            return DrawTypeInternal(new Rect(), true, p_labelString, p_object);
        }

        public static object DrawType(string p_labelString, object p_object, System.Type p_type)
        {
            return DrawTypeInternal(new Rect(), true, p_labelString, p_object, p_type);
        }

        public static object DrawType<ObjectType>(Rect p_rect, string p_labelString, object p_object)
        {
            return (ObjectType)DrawTypeInternal(p_rect, false, p_labelString, p_object, typeof(ObjectType));
        }

        public static object DrawType(Rect p_rect, string p_labelString, object p_object)
        {
            return DrawTypeInternal(p_rect, false, p_labelString, p_object);
        }

        public static object DrawType(Rect p_rect, string p_labelString, object p_object, System.Type p_type)
        {
            return DrawTypeInternal(p_rect, false, p_labelString, p_object, p_type);
        }

        static object DrawTypeInternal(Rect p_rect, bool p_useGuiLayout, string p_labelString, object p_object)
        {
            return DrawTypeInternal(p_rect, p_useGuiLayout, p_labelString, p_object, p_object != null ? p_object.GetType() : null);
        }

        static object DrawTypeInternal(Rect p_rect, bool p_useGuiLayout, string p_labelString, object p_object, System.Type p_type)
        {
            object v_return = null;

            try
            {
                System.Type v_type = p_type;

                if (v_type.IsEnum)
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.EnumPopup((System.Enum)p_object);
                        else
                            v_return = (object)EditorGUILayout.EnumPopup(p_labelString, (System.Enum)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.EnumPopup(p_rect, (System.Enum)p_object);
                        else
                            v_return = (object)EditorGUI.EnumPopup(p_rect, p_labelString, (System.Enum)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(float)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.FloatField((float)p_object);
                        else
                            v_return = (object)EditorGUILayout.FloatField(p_labelString, (float)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.FloatField(p_rect, (float)p_object);
                        else
                            v_return = (object)EditorGUI.FloatField(p_rect, p_labelString, (float)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(long)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.LongField((long)p_object);
                        else
                            v_return = (object)EditorGUILayout.LongField(p_labelString, (long)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.LongField(p_rect, (long)p_object);
                        else
                            v_return = (object)EditorGUI.LongField(p_rect, p_labelString, (long)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(int)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.IntField((int)p_object);
                        else
                            v_return = (object)EditorGUILayout.IntField(p_labelString, (int)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.IntField(p_rect, (int)p_object);
                        else
                            v_return = (object)EditorGUI.IntField(p_rect, p_labelString, (int)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(Color)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.ColorField((Color)p_object);
                        else
                            v_return = (object)EditorGUILayout.ColorField(p_labelString, (Color)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.ColorField(p_rect, (Color)p_object);
                        else
                            v_return = (object)EditorGUI.ColorField(p_rect, p_labelString, (Color)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(string)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.TextField((string)p_object);
                        else
                            v_return = (object)EditorGUILayout.TextField(p_labelString, (string)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.TextField(p_rect, (string)p_object);
                        else
                            v_return = (object)EditorGUI.TextField(p_rect, p_labelString, (string)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(Vector4)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.Vector4Field("", (Vector4)p_object);
                        else
                            v_return = (object)EditorGUILayout.Vector4Field(p_labelString, (Vector4)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.Vector4Field(p_rect, "", (Vector4)p_object);
                        else
                            v_return = (object)EditorGUI.Vector4Field(p_rect, p_labelString, (Vector4)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(Vector3)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.Vector3Field("", (Vector3)p_object);
                        else
                            v_return = (object)EditorGUILayout.Vector3Field(p_labelString, (Vector3)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.Vector3Field(p_rect, "", (Vector3)p_object);
                        else
                            v_return = (object)EditorGUI.Vector3Field(p_rect, p_labelString, (Vector3)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(Vector2)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.Vector2Field("", (Vector2)p_object);
                        else
                            v_return = (object)EditorGUILayout.Vector2Field(p_labelString, (Vector2)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.Vector2Field(p_rect, "", (Vector2)p_object);
                        else
                            v_return = (object)EditorGUI.Vector2Field(p_rect, p_labelString, (Vector2)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(Rect)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.RectField("", (Rect)p_object);
                        else
                            v_return = (object)EditorGUILayout.RectField(p_labelString, (Rect)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.RectField(p_rect, "", (Rect)p_object);
                        else
                            v_return = (object)EditorGUI.RectField(p_rect, p_labelString, (Rect)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubClassOrImplementInterface(v_type, typeof(bool)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.Toggle((bool)p_object);
                        else
                            v_return = (object)EditorGUILayout.Toggle(p_labelString, (bool)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.Toggle(p_rect, (bool)p_object);
                        else
                            v_return = (object)EditorGUI.Toggle(p_rect, p_labelString, (bool)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubclass(v_type, typeof(AnimationCurve)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.CurveField((AnimationCurve)p_object);
                        else
                            v_return = (object)EditorGUILayout.CurveField(p_labelString, (AnimationCurve)p_object);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.CurveField(p_rect, (AnimationCurve)p_object);
                        else
                            v_return = (object)EditorGUI.CurveField(p_rect, p_labelString, (AnimationCurve)p_object);
                    }
                }
                else if (TypeExtensions.IsSameOrSubclass(v_type, typeof(Object)))
                {
                    if (p_useGuiLayout)
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUILayout.ObjectField((Object)p_object, v_type, true);
                        else
                            v_return = (object)EditorGUILayout.ObjectField(p_labelString, (Object)p_object, v_type, true);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(p_labelString))
                            v_return = (object)EditorGUI.ObjectField(p_rect, (Object)p_object, v_type, true);
                        else
                            v_return = (object)EditorGUI.ObjectField(p_rect, p_labelString, (Object)p_object, v_type, true);
                    }
                }
                else
                {
                    if (p_useGuiLayout)
                    {
                        EditorGUILayout.HelpBox(v_type + " not supported.", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUI.HelpBox(p_rect, v_type + " not supported.", MessageType.Warning);
                    }
                }
            }
            catch
            {
                if (p_useGuiLayout)
                {
                    EditorGUILayout.HelpBox("Error drawing object.", MessageType.Warning);
                }
                else
                {
                    EditorGUI.HelpBox(p_rect, "Error drawing object.", MessageType.Warning);
                }
            }

            return v_return;
        }

        #endregion

        #region Container Drawer

        static List<Texture2D> _dynamicGeneratedTextures = new List<Texture2D>();
        public static void BeginContainer(float p_initialOffsetX = 0, float p_initialOffsetY = 5)
        {
            BeginContainer(new Color(0.65f, 0.65f, 0.65f, 0.3f), new Color(0.15f, 0.15f, 0.15f, 0.3f), p_initialOffsetX, p_initialOffsetY);
        }

        public static void BeginContainer(GUIStyle p_style, float p_initialOffsetX = 0, float p_initialOffsetY = 5)
        {
            GUILayout.Space(p_initialOffsetY);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(p_initialOffsetX);
            EditorGUILayout.BeginVertical(p_style);
        }

        public static void BeginContainer(Color p_personalColor, Color p_proColor, float p_initialOffsetX = 0, float p_initialOffsetY = 5)
        {
            if (p_initialOffsetY != 0)
                GUILayout.Space(p_initialOffsetY);
            EditorGUILayout.BeginHorizontal();
            if (p_initialOffsetX != 0)
                GUILayout.Space(p_initialOffsetX);
            Color v_color = EditorGUIUtility.isProSkin ? p_proColor : p_personalColor;
            GUIStyle v_containerGUIStyle = GetColoredGUIStyle(v_color);
            _dynamicGeneratedTextures.Add(v_containerGUIStyle.normal.background);
            EditorGUILayout.BeginVertical(v_containerGUIStyle);
        }

        public static bool DrawFoldoutContainer(string p_containerName, bool p_foldout, System.Action p_drawFunctionWhenOpened, Color p_titleSpacementColor, Color p_bgColor, float p_initialOffsetX = 0, float p_initialOffsetY = 5)
        {
            return DrawFoldoutContainer(p_containerName, p_foldout, p_drawFunctionWhenOpened, null, p_titleSpacementColor, p_bgColor, p_initialOffsetX, p_initialOffsetY);
        }

        public static bool DrawFoldoutContainer(string p_containerName, bool p_foldout, System.Action p_drawFunctionWhenOpened, System.Action p_drawFunctionHeader, Color p_titleSpacementColor, Color p_bgColor, float p_initialOffsetX = 0, float p_initialOffsetY = 5)
        {
            InspectorUtils.BeginContainer(p_titleSpacementColor, new Color(0.7f * p_titleSpacementColor.r, 0.7f * p_titleSpacementColor.g, 0.7f * p_titleSpacementColor.b, p_titleSpacementColor.a), p_initialOffsetX, p_initialOffsetY);
            GUILayout.BeginHorizontal();
            bool v_foldout = EditorGUILayout.Foldout(p_foldout, p_containerName);
            if (p_drawFunctionHeader != null)
                p_drawFunctionHeader.DynamicInvoke();
            GUILayout.EndHorizontal();
            if (v_foldout)
            {
                InspectorUtils.BeginContainer(p_bgColor, new Color(0.7f * p_bgColor.r, 0.7f * p_bgColor.g, 0.7f * p_bgColor.b, 0.7f * p_bgColor.a));
                if (p_drawFunctionWhenOpened != null)
                    p_drawFunctionWhenOpened.DynamicInvoke();
                InspectorUtils.EndContainer();
            }
            EndContainer();
            return v_foldout;
        }

        public static void EndContainer(float p_finalOffsetY = 0)
        {
            if (p_finalOffsetY != 0)
                GUILayout.Space(p_finalOffsetY);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            if (_dynamicGeneratedTextures != null && _dynamicGeneratedTextures.Count > 0)
            {
                Texture2D v_textureToDestroy = _dynamicGeneratedTextures.GetLast();
                _dynamicGeneratedTextures.RemoveAt(_dynamicGeneratedTextures.Count - 1);
                Object.DestroyImmediate(v_textureToDestroy);
                _dynamicGeneratedTextures.RemoveNulls();
            }
        }

        public static GUIStyle GetColoredGUIStyle(Color v_color)
        {
            GUIStyle v_containerGUIStyle = new GUIStyle();
            v_containerGUIStyle.normal.background = MakeTexture(1, 1, v_color);
            return v_containerGUIStyle;
        }

        public static Texture2D MakeTexture(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
#if UNITY_IOS && !UNITY_EDITOR
		Texture2D result = new Texture2D(width, height, TextureFormat.PVRTC_RGBA4, false);
#else
            Texture2D result = new Texture2D(width, height);
#endif
            result.SetPixels(pix);
            result.Apply();

            return result;
        }

        #endregion

        #region Collection Drawers

        public static bool DrawArray<ObjectType>(string p_labelString, ref ObjectType[] p_array, bool p_isOpened)
        {
            List<ObjectType> v_list = p_array != null ? new List<ObjectType>(p_array) : new List<ObjectType>();
            bool v_return = DrawList<ObjectType>(p_labelString, ref v_list, p_isOpened);
            p_array = v_list.ToArray();
            return v_return;
        }

        public static bool DrawList<ObjectType>(string p_labelString, ref List<ObjectType> p_list, bool p_isOpened)
        {
            IList v_genericList = p_list as IList;
            bool v_return = DrawListInternal<ObjectType>(p_labelString, ref v_genericList, p_isOpened);
            try
            {
                p_list = v_genericList as List<ObjectType>;
            }
            catch { }
            return v_return;

        }

        public static bool DrawDictionary<ComparerType, ObjectType>(string p_labelString, ref ArrayDict<ComparerType, ObjectType> p_dict, bool p_isOpened)
        {
            bool v_return = DrawDictionaryInternal<ComparerType, ObjectType>(p_labelString, ref p_dict, p_isOpened);
            return v_return;
        }

        #endregion

        #region Kyub.Collection Drawers

        public static void DrawAOTBaseList<AOTBaseListType>
            (string p_labelString,
             AOTBaseListType p_list,
             System.Action<AOTBaseListType, int> p_onDrawElementFunction,
             System.Action<AOTBaseListType> p_onAddElementFunctions
             ) where AOTBaseListType : IArrayList
        {
            DrawAOTBaseList<AOTBaseListType>(p_labelString, p_list, p_onDrawElementFunction, p_onAddElementFunctions, Vector2.zero, Vector2.zero, false);
        }

        public static Vector2 DrawAOTBaseList<AOTBaseListType>
            (string p_labelString,
             AOTBaseListType p_list,
             System.Action<AOTBaseListType, int> p_onDrawElementFunction,
             System.Action<AOTBaseListType> p_onAddElementFunctions,
             Vector2 p_scrollView,
             bool p_useScroll = true) where AOTBaseListType : IArrayList
        {
            return DrawAOTBaseList<AOTBaseListType>(p_labelString, p_list, p_onDrawElementFunction, p_onAddElementFunctions, p_scrollView, Vector2.zero, p_useScroll);
        }

        public static Vector2 DrawAOTBaseList<AOTBaseListType>
            (string p_labelString,
             AOTBaseListType p_list,
             System.Action<AOTBaseListType, int> p_onDrawElementFunction,
             System.Action<AOTBaseListType> p_onAddElementFunctions,
             Vector2 p_scrollView,
             Vector2 p_initialOffset,
             bool p_useScroll = true) where AOTBaseListType : IArrayList
        {
            Vector2 v_scrollView = p_scrollView;
            if (p_list != null)
            {
                InspectorUtils.BeginContainer(p_initialOffset.x, p_initialOffset.y);
                p_list.FoldOut = EditorGUILayout.Foldout(p_list.FoldOut, p_labelString);
                int v_indexToRemove = -1;
                if (p_list.FoldOut)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.BeginVertical();

                    EditorGUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    EditorGUILayout.LabelField("Length", GUILayout.Width(50));
                    EditorGUILayout.IntField(p_list.Count, GUILayout.Width(80));
                    GUI.enabled = true;
                    //Add Functions Caller
                    if (InspectorUtils.DrawButton("Add", Color.cyan, GUILayout.Width(50)))
                    {
                        if (p_onAddElementFunctions != null)
                            p_onAddElementFunctions(p_list);
                    }
                    EditorGUILayout.EndHorizontal();

                    //Draw Opener
                    InspectorUtils.DrawTitleText("{", new Color(0.5f, 0.5f, 0.5f));

                    if (p_useScroll)
                        v_scrollView = GUILayout.BeginScrollView(p_scrollView);
                    for (int i = 0; i < p_list.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        EditorGUILayout.BeginVertical();
                        if (p_onDrawElementFunction != null)
                            p_onDrawElementFunction(p_list, i);
                        EditorGUILayout.EndVertical();
                        if (InspectorUtils.DrawButton("X", Color.red, GUILayout.MaxWidth(24), GUILayout.MaxHeight(15)))
                            v_indexToRemove = i;
                        EditorGUILayout.EndHorizontal();

                    }
                    if (p_useScroll)
                        GUILayout.EndScrollView();

                    //Draw Closer
                    InspectorUtils.DrawTitleText("}", new Color(0.5f, 0.5f, 0.5f));
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                //Remove Clicked
                if (v_indexToRemove >= 0 && v_indexToRemove <= p_list.Count)
                {
                    p_list.RemoveAt(v_indexToRemove);
                    v_indexToRemove = -1;
                }
                InspectorUtils.EndContainer();
            }
            return v_scrollView;
        }

        #endregion

        #region Internal Draws

        private static bool DrawListInternal<ObjectType>(string p_labelString, ref IList p_list, bool p_isOpened)
        {
            p_isOpened = EditorGUILayout.Foldout(p_isOpened, p_labelString);
            try
            {
                //Try Fill Empty Lists
                if (p_list == null)
                {
                    try
                    {
                        p_list = new List<ObjectType>();
                    }
                    catch { }
                }
                if (p_isOpened)
                {
                    if (p_list != null)
                    {
                        //CheckDummy (This can return Exception, so check before draw anything)
                        int v_index = p_list.Add(default(ObjectType));
                        p_list.RemoveAt(v_index);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        int v_count = Mathf.Max(0, EditorGUILayout.IntField("Size", p_list.Count));
                        EditorGUILayout.EndHorizontal();

                        while (v_count != p_list.Count)
                        {
                            if (v_count > p_list.Count)
                                p_list.Add(default(ObjectType));
                            else
                                p_list.RemoveAt(p_list.Count - 1);
                        }
                        for (int i = 0; i < p_list.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            object p_object = DrawType<ObjectType>("Element " + (i + 1), p_list[i]);
                            p_list[i] = p_object;
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                        EditorGUILayout.HelpBox("Array cannot be null", MessageType.Warning);
                }
            }
            catch
            {
                EditorGUILayout.HelpBox("Array dont accept " + typeof(ObjectType), MessageType.Warning);
            }
            return p_isOpened;
        }

        private static bool DrawDictionaryInternal<ComparerType, ObjectType>(string p_labelString, ref ArrayDict<ComparerType, ObjectType> p_dict, bool p_isOpened)
        {
            p_isOpened = EditorGUILayout.Foldout(p_isOpened, p_labelString);
            try
            {
                //Try Fill Empty Lists
                if (p_dict == null)
                {
                    try
                    {
                        p_dict = new ArrayDict<ComparerType, ObjectType>();
                    }
                    catch { }
                }
                if (p_isOpened)
                {
                    if (p_dict != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(10);
                        int v_count = Mathf.Max(0, EditorGUILayout.IntField("Size", p_dict.Count));
                        EditorGUILayout.EndHorizontal();

                        while (v_count != p_dict.Count)
                        {
                            if (v_count > p_dict.Count)
                                p_dict.Add(new KVPair<ComparerType, ObjectType>());
                            else
                                p_dict.RemoveAt(p_dict.Count - 1);
                        }
                        for (int i = 0; i < p_dict.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(10);
                            object p_comparer = DrawType<ComparerType>("Element " + (i + 1), p_dict[i].Key);
                            object p_object = DrawType<ObjectType>("", p_dict[i].Value);
                            p_dict[i] = new KVPair<ComparerType, ObjectType>((ComparerType)p_comparer, (ObjectType)p_object);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    else
                        EditorGUILayout.HelpBox("Dictionary cannot be null", MessageType.Warning);
                }
            }
            catch
            {
                EditorGUILayout.HelpBox("Dictionary dont accept " + typeof(ObjectType), MessageType.Warning);
            }
            return p_isOpened;
        }

        #endregion

    }
}
#endif
