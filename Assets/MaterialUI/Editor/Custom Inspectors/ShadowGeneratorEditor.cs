// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ShadowGenerator))]
    class ShadowGeneratorEditor : Editor
    {
        private bool m_IsGenerating;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ShadowGenerator myTarget = (ShadowGenerator)target;

            ShadowGenerator.generatedShadowsDirectory = EditorGUILayout.TextField("Generated Shadows Directory", ShadowGenerator.generatedShadowsDirectory);
            ValidateShadowPath();

            if (m_IsGenerating)
            {
                GUI.enabled = false;
                GUILayout.Button("Generating Shadow...");
                GUI.enabled = true;
            }
            else
            {
                if (GUILayout.Button("Generate Shadow"))
                {
                    GenerateShadow(myTarget);
                }
            }
        }

        private void GenerateShadow(ShadowGenerator myTarget)
        {
            if (Selection.gameObjects.Length > 1)
            {
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    if (Selection.gameObjects[i].GetComponent<ShadowGenerator>())
                    {
                        Selection.gameObjects[i].GetComponent<ShadowGenerator>().GenerateShadowFromImage();
                    }
                }
            }
            else
            {
                m_IsGenerating = true;
                Repaint();
                myTarget.GetComponent<ShadowGenerator>().GenerateShadowFromImage();
                EditorCoroutine.Start(WaitUntilGeneratorFinishRoutine(myTarget, () => 
                {
                    m_IsGenerating = false;
                    Repaint();
                }));
            }
        }

        private IEnumerator WaitUntilGeneratorFinishRoutine(ShadowGenerator target, System.Action callback)
        {
            while (target != null && !target.isDone)
            {
                yield return null;
            }

            if (callback != null)
                callback.Invoke();
        }

        private static void ValidateShadowPath()
        {
            if (ShadowGenerator.generatedShadowsDirectory.EndsWith("/"))
            {
                char[] chars = ShadowGenerator.generatedShadowsDirectory.ToCharArray();
                ShadowGenerator.generatedShadowsDirectory = "";
                for (int i = 0; i < chars.Length - 1; i++)
                {
                    ShadowGenerator.generatedShadowsDirectory += chars[i];
                }
            }
            if (ShadowGenerator.generatedShadowsDirectory.StartsWith("/"))
            {
                char[] chars = ShadowGenerator.generatedShadowsDirectory.ToCharArray();
                ShadowGenerator.generatedShadowsDirectory = "";
                for (int i = 1; i < chars.Length; i++)
                {
                    ShadowGenerator.generatedShadowsDirectory += chars[i];
                }
            }
            if (ShadowGenerator.generatedShadowsDirectory.StartsWith("Assets/"))
            {
                char[] chars = ShadowGenerator.generatedShadowsDirectory.ToCharArray();
                ShadowGenerator.generatedShadowsDirectory = "";
                for (int i = 7; i < chars.Length; i++)
                {
                    ShadowGenerator.generatedShadowsDirectory += chars[i];
                }
            }
        }
    }
}

#endif