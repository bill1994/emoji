using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Performance
{
    [DisallowMultipleComponent]
    public class SustainedTransformListener : TransformListener, ISustainedElement
    {
        #region Private Variables

        [Header("SustainedElement Fields")]
        [SerializeField]
        protected bool m_requiresConstantRepaint = false;
        [SerializeField, Range(-1, 150)]
        protected int m_minimumSupportedFps = -1;
        [Space]
        [SerializeField]
        bool m_forceInvalidateWhenChanged = false;
        [Space]
        [SerializeField]
        bool m_autoCalculateCullingMask = false;
        [SerializeField]
        LayerMask m_cullingMask = ~0;
        
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
                return m_cullingMask;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            FindRootCanvasParent();
            if (m_autoCalculateCullingMask)
                RecalculateCullingMask();
            SustainedPerformanceManager.RegisterDynamicElement(this);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            SustainedCanvasView.UnregisterDynamicElement(this);
            SustainedPerformanceManager.UnregisterDynamicElement(this);
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (m_autoCalculateCullingMask)
                RecalculateCullingMask();
            else
                MarkDynamicElementDirty();
        }
#endif

        protected virtual void OnCanvasHierarchyChanged()
        {
            FindRootCanvasParent();
        }

        protected virtual void OnBeforeTransformParentChanged()
        {
            SustainedCanvasView.UnregisterDynamicElement(this);
        }

        protected virtual void OnTransformParentChanged()
        {
            FindRootCanvasParent();
            if (m_autoCalculateCullingMask)
                RecalculateCullingMask();
        }

        #endregion

        #region ISustainedElement Functions

        protected virtual void RecalculateCullingMask()
        {
            var transforms = GetComponentsInChildren<Transform>();

            var cullingMasks = 0;
            //Calculate tha InvalidLayer
            for (int i = 0; i < transforms.Length; i++)
            {
                if(transforms[i] != null)
                    cullingMasks |= 1 << transforms[i].gameObject.layer;
            }

            m_cullingMask = cullingMasks;
            MarkDynamicElementDirty();
        }

        public virtual bool IsScreenCanvasMember()
        {
            return !m_forceInvalidateWhenChanged && _rootParent != null && _rootParent.renderMode != RenderMode.WorldSpace;
        }

        public virtual void MarkDynamicElementDirty()
        {
            var executed = false;
            if (IsScreenCanvasMember())
            {
                var canvasView = GetSustainedCanvasParent();
                if (canvasView != null)
                {
                    executed = true;
                    canvasView.MarkDynamicElementDirty();
                }
            }
            if(!executed)
                SustainedPerformanceManager.MarkDynamicElementsDirty();
        }

        bool ISustainedElement.IsDestroyed()
        {
            return this == null;
        }

        #endregion

        #region Receivers

        protected override void OnBeforeTransformHasChanged()
        {
            if (m_forceInvalidateWhenChanged || !IsScreenCanvasMember())
                SustainedPerformanceManager.Invalidate(this);
            else
                SustainedPerformanceManager.Refresh(this);
            
        }

        #endregion

        #region Helper Functions

        protected SustainedCanvasView _sustainedCanvasParent = null;
        protected Canvas _rootParent = null;
        protected virtual Canvas FindRootCanvasParent()
        {
            _rootParent = GetComponentInParent<Canvas>();

            //Renew Sustained canvas parent based in Canvas
            var newSustainedParent = _rootParent != null? _rootParent.GetComponentInParent<SustainedCanvasView>() : null;

            //Unregister self from the old SustainedCanvas Parent
            if (_sustainedCanvasParent != null && newSustainedParent != _sustainedCanvasParent)
                SustainedCanvasView.UnregisterDynamicElement(this);

            //Register self to a new SustainedCanvas
            _sustainedCanvasParent = newSustainedParent;
            if (_sustainedCanvasParent != null && enabled && gameObject.activeInHierarchy)
                SustainedCanvasView.RegisterDynamicElement(this);

            if(_rootParent != null)
                _rootParent = _rootParent.rootCanvas;

            return _rootParent;
        }

        protected virtual SustainedCanvasView GetSustainedCanvasParent()
        {
            return _sustainedCanvasParent;
        }

        #endregion
    }
}
