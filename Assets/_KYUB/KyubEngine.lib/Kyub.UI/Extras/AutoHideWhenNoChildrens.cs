using UnityEngine;
using System.Collections;
using Kyub.Extensions;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class AutoHideWhenNoChildrens : MonoBehaviour
    {
        [System.Flags]
        public enum TrackModeEnum { Editor = 1, Runtime = 2 }

        #region Private Variables

        [SerializeField]
        Vector3 m_defaultScale = Vector3.one;
        [SerializeField, MaskEnum]
        TrackModeEnum m_trackMode = TrackModeEnum.Editor;

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            RecalcScale();
        }

        protected virtual void Update()
        {
            if ((Application.isEditor && !Application.isPlaying && m_trackMode.ContainsFlag(TrackModeEnum.Editor)) ||
                (Application.isPlaying && m_trackMode.ContainsFlag(TrackModeEnum.Runtime)))
            {
                RecalcScale();
            }
        }

#if UNITY_EDITOR

        protected virtual void OnRenderObject()
        {
            if (!Application.isPlaying)
                RecalcScale();
        }

#endif

        #endregion

        #region Helper Functions

        protected virtual void RecalcScale()
        {
            if ((Application.isEditor && !Application.isPlaying && m_trackMode.ContainsFlag(TrackModeEnum.Editor)) ||
                (Application.isPlaying && m_trackMode.ContainsFlag(TrackModeEnum.Runtime)))
            {
                bool v_keepAlive = false;
                foreach (Transform v_childTransform in transform)
                {
                    if (v_childTransform != null && v_childTransform.gameObject.activeSelf && v_childTransform.localScale != Vector3.zero)
                    {
                        v_keepAlive = true;
                    }
                }
                Vector3 v_newScale = v_keepAlive ? m_defaultScale : Vector3.zero;
                if (transform.localScale.x != v_newScale.x || transform.localScale.y != v_newScale.y || transform.localScale.z != v_newScale.z)
                    transform.localScale = v_newScale;
            }
            //We must revert to default values if PlayMode is not compatible with TrackMode
            else
            {
                Vector3 v_newScale = m_defaultScale;
                if (transform.localScale.x != v_newScale.x || transform.localScale.y != v_newScale.y || transform.localScale.z != v_newScale.z)
                    transform.localScale = v_newScale;
            }
        }

        #endregion
    }
}
