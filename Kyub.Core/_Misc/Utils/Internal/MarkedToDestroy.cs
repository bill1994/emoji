using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Collections;
using Kyub.Extensions;

namespace Kyub
{
    //Used to Replace DestroyImmediate when Calling Self Script
    [ExecuteInEditMode]
    public class MarkedToDestroy : MonoBehaviour
    {
        #region Static Variables

        static ArrayList<MarkedToDestroy> _markedToDestroyInstances = new ArrayList<MarkedToDestroy>();

        #endregion

        #region Private Variables

        [SerializeField]
        Object m_target = null;
        [SerializeField]
        bool m_onlyDestroyInEditor = false;
        [SerializeField]
        bool m_destroyOnStart = false;
        [SerializeField]
        float m_timeToDestroy = 0f;
        [SerializeField]
        bool m_ignoreTimeScale = true;

        #endregion

        #region Public Properties

        public Object Target { get { return m_target; } set { m_target = value; } }
        public bool OnlyDestroyInEditor { get { return m_onlyDestroyInEditor; } set { m_onlyDestroyInEditor = value; } }
        public bool DestroyOnStart { get { return m_destroyOnStart; } set { m_destroyOnStart = value; } }
        public float TimeToDestroy { get { return m_timeToDestroy; } set { m_timeToDestroy = value; } }
        public bool IgnoreTimeScale { get { return m_ignoreTimeScale; } set { m_ignoreTimeScale = value; } }

        protected GameObject GameObjectCastedTarget { get { return m_target as GameObject; } }
        protected Component ComponentCastedTarget { get { return m_target as Component; } }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            gameObject.name = "DestroyMark";
            _markedToDestroyInstances.AddChecking(this);
        }

        protected virtual void Start()
        {
            InitAsGameObjectTarget();
            CheckActivation();
            if (DestroyOnStart && (!OnlyDestroyInEditor || (Application.isEditor && !Application.isPlaying)))
                DestroyTarget();
        }

        protected virtual void Update()
        {
            UpdateTime();
            CheckActivation();
            if (TimeToDestroy <= 0)
            {
                if (!OnlyDestroyInEditor || (Application.isEditor && !Application.isPlaying))
                    DestroyTarget();
            }
        }

        protected virtual void LateUpdate()
        {
            if (TimeToDestroy <= 0)
            {
                if (!OnlyDestroyInEditor || (Application.isEditor && !Application.isPlaying))
                    DestroyTarget();
            }
        }

        protected virtual void OnDestroy()
        {
            _markedToDestroyInstances.RemoveChecking(this);
        }

        #endregion

        #region Helper Functions

        protected virtual void UpdateTime()
        {
            if (TimeToDestroy > 0f)
            {
                float v_delta = m_ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                TimeToDestroy = Mathf.Max(0, TimeToDestroy - v_delta);
            }
        }

        protected virtual void DestroyTarget()
        {
            if (Target == null)
                Target = this.gameObject;
            if (Target != null)
            {
                if (GameObjectCastedTarget != null)
                    GameObjectCastedTarget.hideFlags |= hideFlags = HideFlags.HideInHierarchy;
                //if(Target is DisposableBehaviour)
                //	((DisposableBehaviour)Target).Destroy();
                Object.DestroyImmediate(Target);
            }
        }

        protected virtual void InitAsGameObjectTarget()
        {
            if (GameObjectCastedTarget != null)
            {
                if (!GameObjectCastedTarget.name.Contains("(MarkedToDestroy)"))
                    GameObjectCastedTarget.name += "(MarkedToDestroy)";
            }
        }

        protected void CheckActivation()
        {
            if (TimeToDestroy <= 0)
            {
                if (GameObjectCastedTarget != null)
                {
                    if (!OnlyDestroyInEditor || (Application.isEditor && !Application.isPlaying))
                    {
                        GameObjectCastedTarget.SetActive(false);
                        GameObjectCastedTarget.hideFlags.SetFlags(HideFlags.HideInHierarchy);
                    }
                }
                if (ComponentCastedTarget != null)
                {
                    if (ComponentCastedTarget is MonoBehaviour)
                    {
                        try
                        {
                            (ComponentCastedTarget as MonoBehaviour).enabled = false;
                        }
                        catch { }
                    }
                }
            }
        }

        //Used to Remove Mark!
        private void ReverseCheckActivation()
        {
            if (TimeToDestroy <= 0)
            {
                if (GameObjectCastedTarget != null)
                {
                    if (!OnlyDestroyInEditor || (Application.isEditor && !Application.isPlaying))
                    {
                        GameObjectCastedTarget.SetActive(true);
                        GameObjectCastedTarget.hideFlags.ClearFlags(HideFlags.HideInHierarchy);
                    }
                }
                if (ComponentCastedTarget != null)
                {
                    if (ComponentCastedTarget is MonoBehaviour)
                    {
                        try
                        {
                            (ComponentCastedTarget as MonoBehaviour).enabled = true;
                        }
                        catch { }
                    }
                }
            }
        }

        #endregion

        #region Static Functions

        public static bool IsMarked(Object p_object)
        {
            return GetMark(p_object) != null;
        }

        public static MarkedToDestroy GetMark(Object p_object)
        {
            MarkedToDestroy v_mark = null;
            if (p_object != null)
            {
                foreach (MarkedToDestroy v_marked in _markedToDestroyInstances)
                {
                    if (v_marked != null && v_marked.Target == p_object)
                    {
                        v_mark = v_marked;
                        break;
                    }
                }
            }
            return v_mark;
        }

        public static void RemoveMark(Object p_object)
        {
            MarkedToDestroy v_mark = GetMark(p_object);
            if (v_mark != null && v_mark.Target == p_object)
            {
                if (v_mark.Target != null && v_mark.Target.name != null)
                {
                    v_mark.Target.name = v_mark.Target.name.Replace("(MarkedToDestroy)", "");
                    v_mark.ReverseCheckActivation();
                }
                v_mark.Target = null;
                v_mark.TimeToDestroy = 0;
            }
        }

        #endregion
    }
}
