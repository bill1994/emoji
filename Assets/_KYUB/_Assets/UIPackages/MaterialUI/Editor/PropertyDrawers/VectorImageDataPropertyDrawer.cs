//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(VectorImageData), true)]
    class VectorImageDataPropertyDrawer : PropertyDrawer
    {
        GUIStyle iconStyle = null;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty code = property.FindPropertyRelative("m_Glyph.m_Unicode");
            SerializedProperty name = property.FindPropertyRelative("m_Glyph.m_Name");
            SerializedProperty font = property.FindPropertyRelative("m_Font");
            var vectorFont = font.objectReferenceValue as VectorImageFont;

            if (iconStyle == null)
            {
                iconStyle = new GUIStyle { font = vectorFont != null ? vectorFont.font : font.objectReferenceValue as Font };
                if (iconStyle.font == null && vectorFont != null && vectorFont.fontTMPro != null)
                    iconStyle.font = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(vectorFont.fontTMPro.creationSettings.sourceFontFileGUID));
            }

            if (iconStyle.font != null && iconStyle.font.dynamic)
                iconStyle.fontSize = 16;

            RectOffset offset = new RectOffset(0, 0, -1, -3);
            position = offset.Add(position);
            position.height = EditorGUIUtility.singleLineHeight;

            float offsetH = 0;

            offsetH -= EditorGUI.PrefixLabel(new Rect(position.x + offsetH, position.y, 40, position.height), label).width;

            offsetH += 40;

            if (!string.IsNullOrEmpty(name.stringValue))
            {
                GUIContent iconLabel = new GUIContent(IconDecoder.Decode(code.stringValue));

                if (iconStyle.font != null && !iconStyle.font.dynamic)
                {
                    CharacterInfo charInfo;
                    iconStyle.font.GetCharacterInfo(iconLabel.text[0], out charInfo);
                    var uvRect = Rect.MinMaxRect(charInfo.uvBottomLeft.x, charInfo.uvBottomLeft.y, charInfo.uvTopRight.x, charInfo.uvTopRight.y);

                    GUI.DrawTextureWithTexCoords(new Rect(position.x + offsetH, position.y, 16, position.height), iconStyle.font.material.mainTexture, uvRect);
                }
                else
                {
                    EditorGUI.LabelField(new Rect(position.x + offsetH, position.y, 16, position.height), iconLabel, iconStyle);
                }

                float iconWidth = 16; // iconStyle.CalcSize(iconLabel).x;
                offsetH += iconWidth + 2f;

                EditorGUI.LabelField(new Rect(position.x + offsetH, position.y, position.width - offsetH - 80, position.height), name.stringValue);
            }
            else
            {
                EditorGUI.LabelField(new Rect(position.x + offsetH, position.y, position.width - 70 - 56, position.height), "No icon selected");
            }

            if (GUI.Button(new Rect(position.width - 74, position.y, 70, position.height), "Pick Icon"))
            {
                var target = GetTargetFromSerializedProperty(property);
                VectorImagePickerWindow.Show((VectorImageData)fieldInfo.GetValue(target), property.serializedObject.targetObject);
            }
            if (GUI.Button(new Rect(position.width - 2, position.y, 18, position.height), IconDecoder.Decode(@"\ue14c"), new GUIStyle { fontSize = 16, font = VectorImageManager.GetIconFont(VectorImageManager.materialDesignIconsFontName) }))
            {
                var target = GetTargetFromSerializedProperty(property);
                VectorImageData data = (VectorImageData)fieldInfo.GetValue(target);
                data.vectorFont = null;
                data.glyph = null;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
        }

        #region Reflection Utils

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
                            //If is not the last field in paths because target of last field must be the list inself
                            if (i < v_paths.Length - 1)
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
                            System.Reflection.FieldInfo v_field = GetSerializableFieldWithName(v_currentTarget, v_fieldName, true);
                            if (v_field != null)
                                v_currentTarget = v_field.GetValue(v_currentTarget);
                        }
                    }
                }
            }
            return v_currentTarget;
        }

        static System.Reflection.FieldInfo GetSerializableFieldWithName(object p_target, string p_name, bool p_acceptHiddenFields = false)
        {
            System.Reflection.FieldInfo[] v_fields = GetAllSerializableFields(p_target, p_acceptHiddenFields);
            foreach (System.Reflection.FieldInfo v_field in v_fields)
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
            List<System.Reflection.FieldInfo> v_processedFields = new List<System.Reflection.FieldInfo>();
            System.Type v_type = p_target != null ? p_target.GetType() : null;
            while (v_type != null)
            {
                List<System.Reflection.FieldInfo> v_currentFields = new List<System.Reflection.FieldInfo>();
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

        #endregion
    }
}