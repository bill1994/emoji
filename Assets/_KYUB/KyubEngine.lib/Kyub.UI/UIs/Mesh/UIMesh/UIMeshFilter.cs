using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using Kyub;
using Kyub.Extensions;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class UIMeshFilter : UIMeshGraphic
    {
        #region Private Variables

        [SerializeField]
        bool m_castShadow = true;

        MeshFilter _meshFilter = null;

        #endregion

        #region Properties

        protected MeshRenderer meshRenderer
        {
            get
            {
                if (meshFilter != null)
                    return meshFilter.GetComponent<MeshRenderer>();
                return null;
            }
        }

        protected MeshFilter meshFilter
        {
            get
            {
                if (_meshFilter == null)
                {
                    _meshFilter = GetComponent<MeshFilter>();
                    if (_meshFilter == null)
                    {
                        ClearMesh();
                    }
                    else
                        MarkToUpdateNativeSize();
                }
                return _meshFilter;
            }
        }

        public override bool AutoCalculateRectBounds
        {
            get
            {
                return true;
            }
        }

        public override Mesh SharedMesh
        {
            get
            {
                var v_mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (m_sharedMesh != v_mesh)
                {
                    m_sharedMesh = v_mesh;
                    MarkToUpdateNativeSize();
                    SetMeshDirty();
                }
                return m_sharedMesh;
            }
            set
            {
                Debug.Log("You cannot change SharedMesh in UIMeshFilter. This value was driven by MeshFilter SharedMesh");
            }
        }

        public bool CastShadow
        {
            get
            {
                return m_castShadow;
            }
            set
            {
                if (m_castShadow == value)
                    return;
                m_castShadow = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnDisable()
        {
            if (meshRenderer != null)
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            base.OnDisable();
        }

        protected override void LateUpdate()
        {
            CheckIsRendererEnabled();
            base.LateUpdate();
        }

        #endregion

        #region Helper Functions

        protected virtual UnityEngine.Rendering.ShadowCastingMode GetShadowMode()
        {
            if (CastShadow)
                return UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            else
                return UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        protected override void SetNativeSizeWithoutMarkAsDirty()
        {
            if (SharedMesh != null)
            {
                var v_lossyScale = transform.lossyScale;
                if (v_lossyScale.x > 0 && transform.lossyScale.y > 0)
                {
                    RectTransform v_transform = transform as RectTransform;
                    var v_oldLocalPosition = v_transform.localPosition;
                    var v_bounds = SharedMesh.bounds;
                    if (AutoCalculateRectBounds)
                    {
                        //v_bounds.Encapsulate(Vector3.zero);
                        var v_newPivot = -new Vector2(v_bounds.size.x == 0 ? 0 : v_bounds.min.x / v_bounds.size.x, v_bounds.size.y == 0 ? 0 : v_bounds.min.y / v_bounds.size.y);
                        v_transform.SetPivotAndAnchors(v_newPivot);
                    }
                    Rect v_meshLocalRect = new Rect(v_bounds.min, v_bounds.size);
                    var v_scaler = transform.parent;
                    if (v_scaler != null && v_transform != null)
                    {
                        //var v_scale = new Vector2(v_scaler.transform.lossyScale.x == 0 ? 0 : v_scaler.transform.lossyScale.x, v_scaler.transform.lossyScale.y == 0 ? 0 : v_scaler.transform.lossyScale.y);
                        var v_scale = Vector3.one;
                        v_transform.SetLocalSize(new Vector2(v_meshLocalRect.width * v_scale.x, v_meshLocalRect.height * v_scale.y));
                    }
                    if (AutoCalculateRectBounds)
                        v_transform.localPosition = v_oldLocalPosition;
                }
            }
        }

        protected virtual void CheckIsRendererEnabled()
        {
            bool v_changed = false;
            if (meshRenderer != null && meshRenderer.enabled && meshRenderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
            {
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly; // We MUST deativate original SkinnedMeshRenderer to prevent duplicated renders
                v_changed = true;
            }
            if (SharedMesh != null && v_changed)
            {
                ClearMesh();
                SetMeshDirty();
            }
        }

        protected override void TryUpdateNativeSize(bool p_force = false)
        {
            if (_updateNativeSize || p_force)
            {
                _updateNativeSize = false;
                SetNativeSizeWithoutMarkAsDirty();
            }
        }

        #endregion
    }
}
