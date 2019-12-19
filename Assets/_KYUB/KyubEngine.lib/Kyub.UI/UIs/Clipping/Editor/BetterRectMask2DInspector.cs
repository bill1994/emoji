#if UNITY_EDITOR

using UnityEditor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Kyub.UI;

namespace KyubEditor.UI
{
    [CustomEditor(typeof(BetterRectMask2D), true)]
    [CanEditMultipleObjects]
    public class BetterRectMask2DInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}

#endif