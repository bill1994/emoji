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
    [CustomEditor(typeof(UIMeshGraphic))]
    [CanEditMultipleObjects]
    public class UIMeshGraphicEditor : GraphicEditor
    {
        #region Private Variables

        protected SerializedProperty m_sharedMesh;
        protected SerializedProperty m_texture;
        protected SerializedProperty m_preserveAspectRatio;
        protected SerializedProperty OnRecalculateMeshCallback;

        #endregion

        #region Unity Editor Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            this.m_sharedMesh = base.serializedObject.FindProperty("m_sharedMesh");
            this.m_texture = base.serializedObject.FindProperty("m_texture");
            this.m_preserveAspectRatio = base.serializedObject.FindProperty("m_preserveAspectRatio");
            this.OnRecalculateMeshCallback = base.serializedObject.FindProperty("OnRecalculateMeshCallback");
        }

        public override void OnInspectorGUI()
        {
            base.serializedObject.Update();
            EditorGUILayout.PropertyField(m_sharedMesh, new GUIContent("Mesh"));
            EditorGUILayout.PropertyField(m_texture);
            base.AppearanceControlsGUI();
            base.RaycastControlsGUI();
            EditorGUILayout.PropertyField(m_preserveAspectRatio);
            base.SetShowNativeSize(true, false);
            base.NativeSizeButtonGUI();
            EditorGUILayout.PropertyField(OnRecalculateMeshCallback);
            if (GUI.changed)
                SetAllTargetsDirty();
            base.serializedObject.ApplyModifiedProperties();

        }

        protected virtual void SetAllTargetsDirty()
        {
            foreach (var v_target in targets)
            {
                var v_castedTarget = v_target as UIMeshGraphic;
                if (v_castedTarget != null)
                    v_castedTarget.SetAllDirty();
            }
        }

        #endregion
    }
}

#endif