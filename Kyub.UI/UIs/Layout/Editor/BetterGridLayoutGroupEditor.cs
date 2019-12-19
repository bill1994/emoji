#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(BetterGridLayoutGroup))]
    public class BetterGridLayoutGroupEditor : GridLayoutGroupEditor
    {
        #region Private Variables

        protected SerializedProperty m_flexibleContentToExpand;

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            this.m_flexibleContentToExpand = base.serializedObject.FindProperty("m_flexibleContentToExpand");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            base.serializedObject.Update();
            EditorGUILayout.PropertyField(m_flexibleContentToExpand);
            base.serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}

#endif
