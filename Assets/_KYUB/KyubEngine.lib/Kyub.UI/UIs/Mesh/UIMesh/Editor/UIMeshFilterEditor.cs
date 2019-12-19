#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Kyub.UI;
using Kyub;
using UnityEditor.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(UIMeshFilter))]
    [CanEditMultipleObjects]
    public class UIMeshFilterEditor : UIMeshGraphicEditor
    {
        #region Private Variables

        protected SerializedProperty m_castShadow;

        #endregion

        #region Unity Editor Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            this.m_castShadow = base.serializedObject.FindProperty("m_castShadow");
        }

        public override void OnInspectorGUI()
        {
            base.serializedObject.Update();
            EditorGUILayout.HelpBox("UIMeshFilter pick SharedMesh from MeshFilter attached", MessageType.Info);
            EditorGUILayout.PropertyField(m_texture);
            base.AppearanceControlsGUI();
            EditorGUILayout.PropertyField(m_castShadow);
            base.RaycastControlsGUI();
            EditorGUILayout.PropertyField(OnRecalculateMeshCallback);
            if (GUI.changed)
                SetAllTargetsDirty();
            base.serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}

#endif