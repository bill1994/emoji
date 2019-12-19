using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScrollLayoutGroup), true)]
    public class ScrollLayoutGroupEditor : Editor
    {
        protected string[] _elementsToIgnore = new string[] { "OnElementChanged", "OnElementsRemoved", "OnElementsAdded", "OnAllElementsReplaced", "OnElementCachedSizeChanged", "OnBeforeChangeVisibleElements", "OnElementBecameInvisible", "OnElementBecameVisible", "m_Script" };

        public override void OnInspectorGUI()
        {
            GUILayout.Space(15);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Recalculate Elements Layout", GUILayout.Width(200)))
                {
                    serializedObject.Update();
                    foreach (var v_target in targets)
                    {
                        ScrollLayoutGroup v_castedTarget = v_target as ScrollLayoutGroup;
                        if (v_castedTarget != null)
                        {
                            v_castedTarget.SetCachedElementsLayoutDirty(true);
                            v_castedTarget.TryRecalculateLayout(true);
                            UnityEditor.EditorUtility.SetDirty(v_castedTarget);
                        }
                    }
                    serializedObject.ApplyModifiedProperties();
                }
                GUILayout.FlexibleSpace();
            }
            
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, _elementsToIgnore);
            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                foreach (var v_target in targets)
                {
                    ScrollLayoutGroup v_castedTarget = v_target as ScrollLayoutGroup;
                    if (v_castedTarget != null)
                        v_castedTarget.SetCachedElementsLayoutDirty();
                }
            }
        }
    }
}
