#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(BetterToggle), true)]
    [CanEditMultipleObjects]
    public class BetterToggleInspector : UnityEditor.UI.ToggleEditor
    {
        SerializedProperty m_changeToggleOnPressDown;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_changeToggleOnPressDown = serializedObject.FindProperty("m_changeToggleOnPressDown");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (m_changeToggleOnPressDown != null)
                EditorGUILayout.PropertyField(m_changeToggleOnPressDown, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif