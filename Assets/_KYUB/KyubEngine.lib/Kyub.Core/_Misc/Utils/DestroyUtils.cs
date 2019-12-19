using UnityEngine;
using System.Collections;

namespace Kyub
{
    public static class DestroyUtils
    {
        public static void Destroy(Object p_object, bool p_onlyDestroyInEditor = false)
        {
            Destroy(p_object, 0f, true, p_onlyDestroyInEditor);
        }

        //Delayed Destroy in Editor
        public static void Destroy(Object p_object, float p_time, bool p_ignoreTimeScale = true, bool p_onlyDestroyInEditor = false)
        {
            if (p_object != null)
            {
                MarkedToDestroy v_mark = MarkedToDestroy.GetMark(p_object);
                if (v_mark == null)
                {
                    GameObject v_newObject = new GameObject();
                    v_mark = v_newObject.AddComponent<MarkedToDestroy>();
                    v_newObject.transform.SetAsFirstSibling();
                }
                v_mark.Target = p_object;
                v_mark.OnlyDestroyInEditor = p_onlyDestroyInEditor;
                v_mark.DestroyOnStart = false;
                v_mark.TimeToDestroy = p_time;
                v_mark.IgnoreTimeScale = p_ignoreTimeScale;
            }
        }

        //Destroy Faster Than Simple Destroy (Works in Editor too)
        public static void DestroyImmediate(Object p_object, bool p_onlyDestroyInEditor = false)
        {
            if (p_object != null)
            {
                DestroyUtils.Destroy(p_object, p_onlyDestroyInEditor);
                MarkedToDestroy v_mark = MarkedToDestroy.GetMark(p_object);
                if (v_mark != null)
                    v_mark.DestroyOnStart = true;
            }
        }
    }
}
