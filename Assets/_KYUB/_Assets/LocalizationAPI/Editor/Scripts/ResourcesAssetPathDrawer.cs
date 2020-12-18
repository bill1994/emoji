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
            string path = property.stringValue;
            Object file = null;
            if (!string.IsNullOrEmpty(path))
            {
                if (!_cachedFiles.ContainsKey(path))
                {
                    file = Resources.Load<Object>(path);
                    if(file != null)
                        _cachedFiles.Add(path, file);
                }
                else
                    file = _cachedFiles[path];

                if (file == null)
                    path = "";  
            }
            var attr = attribute as ResourcesAssetPathAttribute;
            var newFile = EditorGUI.ObjectField(position, label, file, attr.FilterType, false) as Object;
            if (file != newFile)
            {
                file = newFile;
                if (file != null)
                {
                    path = AssetDatabase.GetAssetPath(file);
                    if(path != null && System.IO.Path.HasExtension(path))
                        path = path.Replace(System.IO.Path.GetExtension(path), "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var splittedPath = path.Split(new System.String[] { "Resources/" }, System.StringSplitOptions.None);
                        if (splittedPath.Length > 1)
                            path = splittedPath[1];
                        else
                        {
                            file = null;
                            path = "";
                        }
                    }
                    else
                    {
                        file = null;
                        path = "";
                    }
                    if (!string.IsNullOrEmpty(path) && file != null && !_cachedFiles.ContainsKey(path))
                    {
                        _cachedFiles.Add(path, file);
                    }
                }
                else
                    path = "";
                property.serializedObject.Update();
                property.stringValue = path;
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
