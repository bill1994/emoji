using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Kyub.Performance
{
    [DisallowMultipleComponent]
    public abstract class SustainedRenderView : MonoBehaviour, ISustainedElement
    {
        #region Static Properties

        protected internal static List<SustainedRenderView> s_sceneRenderViews = new List<SustainedRenderView>();

        public static ReadOnlyCollection<SustainedRenderView> SceneRenderViews
        {
            get
            {
                return s_sceneRenderViews.AsReadOnly();
            }
        }

        #endregion

        #region Private Variables

        [Header("SustainedElement Fields")]
        [SerializeField]
        protected bool m_requiresConstantRepaint = false;
        [SerializeField, Range(-1, 150)]
        protected int m_minimumSupportedFps = -1;

        #endregion

        #region Public Properties

        public virtual bool RequiresConstantRepaint
        {
            get
            {
                return m_requiresConstantRepaint;
            }
            set
            {
                if (m_requiresConstantRepaint == value)
                    return;
                m_requiresConstantRepaint = value;
                MarkDynamicElementDirty();
            }
        }

        public virtual int MinimumSupportedFps
        {
            get
            {
                return m_minimumSupportedFps;
            }
            set
            {
                if (m_minimumSupportedFps == value)
                    return;
                m_minimumSupportedFps = value;
                MarkDynamicElementDirty();
            }
        }

        public virtual bool UseRenderBuffer
        {
            get
            {
                return false;
            }
            set
            {
            }
        }

        public virtual int CullingMask
        {
            get
            {
                return ~0;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            _isViewActive = SustainedPerformanceManager.IsHighPerformanceActive();
            RegisterRenderView();
        }

        protected virtual void OnEnable()
        {
            SustainedPerformanceManager.RegisterDynamicElement(this);
            RegisterEvents();
            if (_started)
            {
                if (SustainedPerformanceManager.IsHighPerformanceActive())
                    HandleOnSetHighPerformance(true);
                else
                    HandleOnSetLowPerformance();
            }
        }

        protected bool _started = false;
        protected virtual void Start()
        {
            _started = true;
            if (SustainedPerformanceManager.IsHighPerformanceActive())
                HandleOnSetHighPerformance(true);
            else
                HandleOnSetLowPerformance();
        }

        protected virtual void OnDisable()
        {
            if (IsScreenCanvasMember())
                SustainedPerformanceManager.Refresh();
            else
                SustainedPerformanceManager.Invalidate(this);

            SustainedPerformanceManager.UnregisterDynamicElement(this);
            UnregisterEvents();
            SetViewActive(!Application.isPlaying);
        }

        protected virtual void OnDestroy()
        {
            //SetViewActive(true);
            UnregisterRenderView();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            MarkDynamicElementDirty();
            if (Application.isPlaying && enabled && gameObject.activeInHierarchy)
                RegisterEvents();
        }
#endif

        #endregion

        #region Rendering Helper Functions

        public bool IsViewActive()
        {
            return _isViewActive;
        }

        protected bool _isViewActive = false;
        protected virtual void SetViewActive(bool active)
        {
            _isViewActive = active;
        }

        #endregion

        #region Helper Functions

        public virtual bool IsScreenCanvasMember()
        {
            return false;
        }

        public virtual void MarkDynamicElementDirty()
        {
            SustainedPerformanceManager.MarkDynamicElementsDirty();
        }

        protected virtual void UnregisterRenderView()
        {
            var index = s_sceneRenderViews.IndexOf(this);
            if (index >= 0)
                s_sceneRenderViews.RemoveAt(index);
        }

        protected virtual void RegisterRenderView()
        {
            if (!s_sceneRenderViews.Contains(this))
                s_sceneRenderViews.Add(this);
        }

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();
            SustainedPerformanceManager.OnSetHighPerformance += HandleOnSetHighPerformance;
            SustainedPerformanceManager.OnSetLowPerformance += HandleOnSetLowPerformance;
        }

        protected virtual void UnregisterEvents()
        {
            SustainedPerformanceManager.OnSetHighPerformance -= HandleOnSetHighPerformance;
            SustainedPerformanceManager.OnSetLowPerformance -= HandleOnSetLowPerformance;
        }

        public bool IsDestroyed()
        {
            return this == null;
        }

        #endregion

        #region SustainedPerformance Receivers

        protected virtual void HandleOnSetLowPerformance()
        {
            SetViewActive(SustainedPerformanceManager.RequiresConstantRepaint);
        }

        protected virtual void HandleOnSetHighPerformance(bool invalidateBuffer)
        {
            var isViewActive = SustainedPerformanceManager.RequiresConstantRepaint || invalidateBuffer;
            SetViewActive(isViewActive);
        }

        #endregion
    }
}
