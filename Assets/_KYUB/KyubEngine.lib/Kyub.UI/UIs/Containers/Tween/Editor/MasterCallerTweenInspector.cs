#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using Kyub.UI;

namespace KyubEditor.UI
{
    //TODO: Build Inspector
    [CustomEditor(typeof(MasterCallerTween))]
    public class MasterCallerSchedulerInspector : Editor
    {

        MasterCallerTween m_controller;

        public override void OnInspectorGUI()
        {
            m_controller = target as MasterCallerTween;

            //Do Logic Here

            if (GUI.changed)
                EditorUtility.SetDirty(m_controller);

        }
    }
}

#endif
