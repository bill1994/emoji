#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using KyubEditor.Extensions;
using Kyub;

namespace KyubEditor
{
    [CustomPropertyDrawer(typeof(ComponentFieldAttribute))]
    public partial class ComponentFieldDrawer : SpecificFieldDrawer
    {
        #region Draw Single Component Functions

        protected override void DrawComponent(Rect position, SerializedProperty property, GUIContent label, System.Type p_type)
        {
            Color v_oldColor = GUI.backgroundColor;
            int v_selectorWidth = _isArrayOfComponents ? 40 : 30;
            System.Type v_type = p_type;
            SpecificFieldAttribute v_attr = attribute as SpecificFieldAttribute;
            if (v_attr != null && IsSameOrSubClassOrImplementInterface(v_type, v_attr.AcceptedType))
            {
                //Get Usefull Fields
                UnityEngine.Object v_oldValue = property.objectReferenceValue;
                Component v_selfAsComponent = GetSelfAsComponent(property, p_type);
                List<Component> v_components = GetComponents(property, v_type, p_type);
                string[] v_componentsString = GetStringArrayFromComponents(v_components);
                int v_currentIndex = GetCurrentComponentIndex(v_components, v_selfAsComponent);

                //Get Rects
                Rect v_objectRect = new Rect(position.x, position.y, Mathf.Max(0, position.width - v_selectorWidth), position.height);
                Rect v_indexRect = new Rect(position.x + v_objectRect.width, position.y, position.width - v_objectRect.width, position.height);

                //Draw Object Field
                UnityEngine.Object v_newObject = EditorGUI.ObjectField(v_objectRect, label, property.objectReferenceValue, v_type, true);

                //Draw PopUp
                GUI.backgroundColor = v_currentIndex == -1 ? Color.red : Color.green;
                int v_newIndex = EditorGUI.Popup(v_indexRect, v_currentIndex, v_componentsString);
                GUI.backgroundColor = v_oldColor;

                //Apply
                TryApplyValue(property, v_oldValue, v_newObject, v_currentIndex, v_newIndex, v_components);
            }
        }

        #endregion

        #region Helper Functions

        private void TryApplyValue(SerializedProperty property, Object p_oldValue, Object p_newValue, int p_oldIndex, int p_newIndex, List<Component> p_possibleComponents)
        {
            property.serializedObject.Update();
            if (GUI.changed)
            {
                if (p_newValue != p_oldValue)
                {
                    property.objectReferenceValue = p_newValue;
                    property.SetFieldValue(p_newValue);
                }
                else if (p_newIndex != p_oldIndex)
                {
                    p_newValue = p_newIndex >= 0 && p_newIndex < p_possibleComponents.Count ? p_possibleComponents[p_newIndex] : p_oldValue;
                    property.objectReferenceValue = p_newValue;
                    property.SetFieldValue(p_newValue);
                }
                try
                {
#if UNITY_5_3_OR_NEWER
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    property.serializedObject.Update();
#else
                EditorUtility.SetDirty(property.serializedObject.targetObject);
#endif
                }
                catch { }
            }
        }

        private List<Component> GetComponents(Component p_component, System.Type v_typeFilter, System.Type p_type)
        {
            System.Type v_type = p_type;
            Component[] v_components = new Component[0];
            if (IsSameOrSubClassOrImplementInterface(v_type, typeof(Component)))
            {
                if (p_component != null)
                {
                    v_components = p_component.GetComponents(v_typeFilter);
                }
            }
            return new List<Component>(v_components);
        }

        private List<Component> GetComponents(SerializedProperty property, System.Type v_typeFilter, System.Type p_type)
        {
            return GetComponents(property.objectReferenceValue as Component, v_typeFilter, p_type);
        }

        private Component GetSelfAsComponent(Object p_target, System.Type p_type)
        {
            System.Type v_type = p_type;
            Component v_componentRef = null;
            if (IsSameOrSubClassOrImplementInterface(v_type, typeof(Component)))
            {
                if (p_target != null)
                {
                    v_componentRef = p_target as Component;
                }
            }
            return v_componentRef;
        }

        private Component GetSelfAsComponent(SerializedProperty property, System.Type p_type)
        {
            return GetSelfAsComponent(property.objectReferenceValue, p_type);
        }

        private int GetCurrentComponentIndex(List<Component> p_components, Component p_current)
        {
            int v_index = -1;
            if (p_components != null)
            {
                int i = 0;
                foreach (Component v_comp in p_components)
                {
                    if (v_comp == p_current)
                    {
                        v_index = i;
                        break;
                    }
                    i++;
                }
            }
            return v_index;
        }

        private string[] GetStringArrayFromComponents(List<Component> p_components)
        {
            string[] v_componentsString = p_components != null ? GetStringList(p_components).ToArray() : new string[0];
            for (int i = 0; i < v_componentsString.Length; i++)
            {
                if (v_componentsString[i] != null)
                {
                    int v_firstRefIndex = v_componentsString[i].IndexOf("(") + 1;
                    int v_lastRefIndex = v_componentsString[i].LastIndexOf(")") - 1;
                    v_componentsString[i] = (i + 1) + ": " + (v_firstRefIndex >= 0 && v_firstRefIndex < v_componentsString[i].Length && v_lastRefIndex >= 0 && v_lastRefIndex < v_componentsString[i].Length && v_firstRefIndex < v_lastRefIndex ? v_componentsString[i].Substring(v_firstRefIndex, (v_lastRefIndex + 1) - v_firstRefIndex) : v_componentsString[i]);
                }
            }
            return v_componentsString;
        }

        #endregion
    }
}
#endif
