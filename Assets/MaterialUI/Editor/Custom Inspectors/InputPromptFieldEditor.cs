using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(InputPromptField), true)]
    public class InputPromptFieldEditor : UnityEditor.UI.SelectableEditor
    {
        protected string[] m_PropertyPathToExcludeForChildClassesDerived;
        protected override void OnEnable()
        {
            base.OnEnable();

            m_PropertyPathToExcludeForChildClassesDerived = typeof(UnityEditor.UI.SelectableEditor).GetField("m_PropertyPathToExcludeForChildClasses", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(this) as string[];
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, m_PropertyPathToExcludeForChildClassesDerived);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
