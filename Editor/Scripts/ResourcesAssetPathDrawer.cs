using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.Localization;

namespace KyubEditor.Localization
{
    [CustomPropertyDrawer(typeof(ResourcesAssetPathAttribute))]
    public class ResourcesAssetPathDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        Dictionary<string, Object> _cachedFiles = new Dictionary<string, Object>();
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string v_path = property.stringValue;
            Object v_file = null;
            if (!string.IsNullOrEmpty(v_path))
            {
                if (!_cachedFiles.ContainsKey(v_path))
                {
                    v_file = Resources.Load<Object>(v_path);
                    if(v_file != null)
                        _cachedFiles.Add(v_path, v_file);
                }
                else
                    v_file = _cachedFiles[v_path];

                if (v_file == null)
                    v_path = "";  
            }
            var v_attr = attribute as ResourcesAssetPathAttribute;
            var v_newFile = EditorGUI.ObjectField(position, label, v_file, v_attr.FilterType, false) as Object;
            if (v_file != v_newFile)
            {
                v_file = v_newFile;
                if (v_file != null)
                {
                    v_path = AssetDatabase.GetAssetPath(v_file);
                    if(v_path != null && System.IO.Path.HasExtension(v_path))
                        v_path = v_path.Replace(System.IO.Path.GetExtension(v_path), "");
                    if (!string.IsNullOrEmpty(v_path))
                    {
                        var v_splittedPath = v_path.Split(new System.String[] { "Resources/" }, System.StringSplitOptions.None);
                        if (v_splittedPath.Length > 1)
                            v_path = v_splittedPath[1];
                        else
                        {
                            v_file = null;
                            v_path = "";
                        }
                    }
                    else
                    {
                        v_file = null;
                        v_path = "";
                    }
                    if (!string.IsNullOrEmpty(v_path) && v_file != null && !_cachedFiles.ContainsKey(v_path))
                    {
                        _cachedFiles.Add(v_path, v_file);
                    }
                }
                else
                    v_path = "";
                property.serializedObject.Update();
                property.stringValue = v_path;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
