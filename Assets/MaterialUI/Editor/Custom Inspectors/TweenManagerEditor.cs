// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEditor;
using UnityEngine;
using System.Linq;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TweenManager))]
    class TweenManagerEditor : Editor
    {
        private TweenManager m_TweenManager;

        protected virtual void OnEnable()
        {
            m_TweenManager = (TweenManager)serializedObject.targetObject;
        }

        public override void OnInspectorGUI()
        {
			EditorGUILayout.LabelField("Total Tweens", m_TweenManager.totalTweenCount.ToString());
			EditorGUILayout.LabelField("Active Tweens", m_TweenManager.activeTweenCount.ToString());
			EditorGUILayout.LabelField("Dormant Tweens", m_TweenManager.dormantTweenCount.ToString());
        }
    }
}