using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Extensions;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class UISkinnedMeshFilter : UIMeshGraphic
    {
        #region Private Variables

        [SerializeField]
        bool m_castShadow = true;

        Bounds _modelMeshBounds = new Bounds();
        SkinnedMeshRenderer _skinnedMeshRenderer = null;

        #endregion

        #region Properties

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

        protected SkinnedMeshRenderer skinnedMeshRenderer
        {
            get
            {
                if (_skinnedMeshRenderer == null)
                {
                    _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
                    if (_skinnedMeshRenderer == null)
                    {
                        ClearBakedMesh();
                        ClearMesh();
                    }
                    else
                        MarkToUpdateNativeSize();
                }
                return _skinnedMeshRenderer;
            }
        }

        public override Mesh SharedMesh
        {
            get
            {
                var v_mesh = skinnedMeshRenderer != null ? skinnedMeshRenderer.sharedMesh : null;
                if (m_sharedMesh != v_mesh)
                {
                    m_sharedMesh = v_mesh;
                    MarkToUpdateNativeSize();
                }
                return m_sharedMesh;
            }
            set
            {
                Debug.Log("You cannot change SharedMesh in UISkinnedMesh. This value was driven by SkinnedMeshRenderer BakeMesh");
            }
        }

        protected Mesh _bakedMesh = null;
        public virtual Mesh BakedMesh
        {
            get
            {
                if (_bakedMesh == null)
                    BakeMeshFromSkinnedRenderer();
                return _bakedMesh;
            }
        }

        public override bool AutoCalculateRectBounds
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            MarkToUpdateNativeSize();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if (skinnedMeshRenderer != null)
                skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            ClearBakedMesh();
            base.OnDisable();
        }

        protected override void LateUpdate()
        {
            CheckIsRendererEnabled();
            BakeMeshFromSkinnedRenderer();
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

        protected virtual void CheckIsRendererEnabled()
        {
            bool v_changed = false;
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.enabled && skinnedMeshRenderer.shadowCastingMode != GetShadowMode())
            {
                skinnedMeshRenderer.shadowCastingMode = GetShadowMode(); // We MUST deativate original SkinnedMeshRenderer to prevent duplicated renders
                v_changed = true;
            }
            if (SharedMesh != null && v_changed)
            {
                ClearMesh();
                SetMeshDirty();
            }
        }

        protected virtual void BakeMeshFromSkinnedRenderer()
        {
            if (skinnedMeshRenderer != null)
            {
                ClearBakedMesh();
                skinnedMeshRenderer.BakeMesh(_bakedMesh);
                SetMeshDirty();
            }
        }

        protected override void ClearMesh()
        {
            base.ClearMesh();
            _mesh.MarkDynamic();
        }

        protected virtual void ClearBakedMesh()
        {
            if (_bakedMesh == null)
                _bakedMesh = new Mesh();
            else
                _bakedMesh.Clear();
            _bakedMesh.MarkDynamic();
        }

        protected override void DestroyMesh()
        {
            base.DestroyMesh();
            ClearBakedMesh();
            Object.DestroyImmediate(_bakedMesh);
            _bakedMesh = null;
        }

        Vector3 v_currentLossyScale = Vector3.zero;
        protected override void TryUpdateNativeSize(bool p_force = false)
        {
            if (_updateNativeSize || p_force || v_currentLossyScale != transform.lossyScale)
            {
                v_currentLossyScale = transform.lossyScale;
                _updateNativeSize = false;
                SetNativeSizeWithoutMarkAsDirty();
            }
        }

        protected override void RecalcMesh()
        {
            TryUpdateNativeSize(true);
            RectTransform v_rectTransform = GetComponent<RectTransform>();
            ClearMesh();
            if (BakedMesh != null && v_rectTransform != null)
            {
                Vector3[] v_vertices = BakedMesh.vertices;
                int[] v_triangles = BakedMesh.triangles;
                Vector3[] v_normals = BakedMesh.normals;
                Vector2[] v_uv = BakedMesh.uv;
                Color32[] v_colors = new Color32[v_vertices.Length];

                Rect v_transformLocalRect = v_rectTransform.GetLocalRect();

                //Find Local Rect of the Mesh
                var v_bounds = _modelMeshBounds; // Use PreservedBounds
                Rect v_meshLocalRect = new Rect(v_bounds.min, v_bounds.size);

                //Calculate New Scale
                Vector3 v_scaleFixer = new Vector2(v_transformLocalRect.width != 0 ? (v_transformLocalRect.width / v_meshLocalRect.width) : 0,
                    v_transformLocalRect.height != 0 ? (v_transformLocalRect.height / v_meshLocalRect.height) : 0);

                //if (PreserveAspectRatio)
                v_scaleFixer = new Vector3(Mathf.Min(v_scaleFixer.x, v_scaleFixer.y), Mathf.Min(v_scaleFixer.x, v_scaleFixer.y), 0);
                v_scaleFixer.z = Mathf.Min(v_scaleFixer.x, v_scaleFixer.y);

                //Pick Delta AnchorPosition to always place the mesh in center of the rect
                Vector3 v_delta = Vector2.zero;
                if (!AutoCalculateRectBounds)
                    v_delta = new Vector2(v_transformLocalRect.xMin + (v_transformLocalRect.width * v_rectTransform.pivot.x), v_transformLocalRect.yMin + (v_transformLocalRect.height * v_rectTransform.pivot.y)) - v_transformLocalRect.center;
                if (v_scaleFixer.x > 0.00001f && v_scaleFixer.y > 0.00001f && v_scaleFixer.z > 0.00001f)
                {
                    for (int i = 0; i < v_vertices.Length; i++)
                    {
                        var v_vertice = !AutoCalculateRectBounds ? (v_vertices[i] - BakedMesh.bounds.center) : v_vertices[i];
                        v_vertice.x *= v_scaleFixer.x;
                        v_vertice.y *= v_scaleFixer.y;
                        v_vertice.z *= v_scaleFixer.z;
                        if (!float.IsNaN(v_vertice.x) || !float.IsNaN(v_vertice.y) || !float.IsNaN(v_vertice.z))
                            v_vertices[i] = v_vertice;
                        else
                        {
                            ClearMesh();
                            return;
                        }
                        v_colors[i] = color;
                    }
                    _mesh.SetVertices(new List<Vector3>(v_vertices));
                    _mesh.triangles = v_triangles;
                    _mesh.normals = v_normals;
                    _mesh.colors32 = v_colors;
                    _mesh.uv = v_uv;
                }
            }
            if (OnRecalculateMeshCallback != null)
                OnRecalculateMeshCallback.Invoke(_mesh);
        }

        protected override void SetNativeSizeWithoutMarkAsDirty()
        {
            if (BakedMesh != null)
            {
                var v_lossyScale = transform.lossyScale;
                if (v_lossyScale.x > 0 && transform.lossyScale.y > 0)
                {
                    RectTransform v_transform = transform as RectTransform;
                    var v_oldLocalPosition = v_transform.localPosition;
                    var v_bounds = BakedMesh.bounds;
                    _modelMeshBounds = v_bounds; //Preserve BakedMesh Bounds
                    if (AutoCalculateRectBounds)
                    {
                        //v_bounds.Encapsulate(Vector3.zero);
                        var v_newPivot = -new Vector2(v_bounds.size.x == 0 ? 0 : v_bounds.min.x / v_bounds.size.x, v_bounds.size.y == 0 ? 0 : v_bounds.min.y / v_bounds.size.y);
                        v_transform.SetPivotAndAnchors(v_newPivot);
                    }
                    Rect v_meshLocalRect = new Rect(v_bounds.min, v_bounds.size);
                    var v_scaler = transform;
                    if (v_scaler != null && v_transform != null)
                    {
                        var v_scale = new Vector2(v_scaler.transform.lossyScale.x == 0 ? 0 : 1.0f / v_scaler.transform.lossyScale.x, v_scaler.transform.lossyScale.y == 0 ? 0 : 1.0f / v_scaler.transform.lossyScale.y);
                        v_transform.SetLocalSize(new Vector2(v_meshLocalRect.width * v_scale.x, v_meshLocalRect.height * v_scale.y));
                    }
                    if (AutoCalculateRectBounds)
                        v_transform.localPosition = v_oldLocalPosition;
                }
            }
        }

        #endregion
    }
}
