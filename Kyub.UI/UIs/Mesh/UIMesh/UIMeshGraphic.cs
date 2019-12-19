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
    public class UIMeshGraphic : MaskableGraphic
    {
        #region Helper Classes

        [System.Serializable]
        public class MeshUnityEvent : UnityEvent<Mesh> { } 

        #endregion

        #region Private Variables

        [SerializeField]
        protected Mesh m_sharedMesh = null;
        [SerializeField]
        Texture2D m_texture = null;
        [SerializeField]
        bool m_preserveAspectRatio = true;

        protected Mesh _mesh = null;

        #endregion

        #region Callbacks

        public MeshUnityEvent OnRecalculateMeshCallback = new MeshUnityEvent();

        #endregion

        #region Public Properties

        public virtual Mesh SharedMesh
        {
            get
            {
                return m_sharedMesh;
            }
            set
            {
                if (m_sharedMesh == value)
                    return;
                m_sharedMesh = value;
                _mesh = null;
                SetVerticesDirty();
            }
        }

        public Mesh Mesh
        {
            get
            {
                if (_mesh == null)
                {
                    if (m_sharedMesh != null)
                    {
                        RecalcMesh();
                        SetVerticesDirty();
                    }
                }
                return _mesh;
            }
        }

        public Texture2D Texture
        {
            get
            {
                return m_texture;
            }
            set
            {
                if (m_texture == value)
                    return;
                m_texture = value;
                SetMaterialDirty();
            }
        }

        public bool PreserveAspectRatio
        {
            get
            {
                if (AutoCalculateRectBounds)
                    return true;
                return m_preserveAspectRatio;
            }
            set
            {
                if (m_preserveAspectRatio == value)
                    return;
                m_preserveAspectRatio = value;
                SetVerticesDirty();
            }
        }

        public virtual bool AutoCalculateRectBounds
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
                TryApplyBakedMesh(true);
        }

        protected bool _started = false;
        protected override void Start()
        {
            base.Start();
            _started = true;
            TryApplyBakedMesh(true);
        }

        protected override void OnDisable()
        {
            ClearMesh();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyMesh();
        }

        protected override void UpdateGeometry()
        {
            SetMeshDirty();
        }

        protected virtual void LateUpdate()
        {
            TryUpdateNativeSize();
            TryApplyBakedMesh();
        }

        protected override void OnPopulateMesh(VertexHelper p_toFill)
        {
            if (Mesh == null)
            {
                base.OnPopulateMesh(p_toFill);
            }
            else
            {
                p_toFill.Clear();
                var v_vertices = Mesh.vertices;
                var v_triangles = Mesh.triangles;
                var v_uvs = Mesh.uv;
                var v_colors = Mesh.colors32;
                var v_normals = _mesh.normals;

                List<UIVertex> v_vertexTriangles = new List<UIVertex>();
                for (int i = 0; i < v_triangles.Length; i++)
                {
                    UIVertex v_vertex = new UIVertex();
                    if (v_vertices.Length > i)
                        v_vertex.position = v_vertices[i];
                    if (v_colors.Length > i)
                        v_vertex.color = v_colors[i];
                    if (v_uvs.Length > i)
                    {
                        v_vertex.uv0 = v_uvs[i];
                        v_vertex.uv1 = v_uvs[i];
                    }
                    if (v_normals.Length > i)
                        v_vertex.normal = v_normals[i];
                    else
                        v_vertex.normal = Vector3.one;
                    v_vertexTriangles.Add(v_vertex);
                }
                p_toFill.AddUIVertexStream(v_vertexTriangles, new List<int>(v_triangles));
            }
        }

        #endregion

            #region Helper Functions

        protected bool _updateNativeSize = false;
        public virtual void MarkToUpdateNativeSize()
        {
            _updateNativeSize = true;
        }

        protected virtual void ClearMesh()
        {
            if (_mesh == null)
                _mesh = new Mesh();
            else
                _mesh.Clear();
        }

        protected virtual void DestroyMesh()
        {
            ClearMesh();
            Object.DestroyImmediate(_mesh);
            _mesh = null;

            CanvasRenderer v_canvasRenderer = GetComponent<CanvasRenderer>();
            if (v_canvasRenderer != null)
                v_canvasRenderer.Clear();
        }

        protected bool _isMeshDirty = false;
        protected void SetMeshDirty()
        {
            _isMeshDirty = true;
        }

        public virtual void TryApplyBakedMesh(bool p_force = false)
        {
            if (_isMeshDirty || p_force)
            {
                _isMeshDirty = false;
                RecalcMesh();
                base.UpdateGeometry(); //Force Update Geometry
                /*if (enabled && gameObject.activeSelf && gameObject.activeInHierarchy)
                {
                    CanvasRenderer v_canvasRenderer = GetComponent<CanvasRenderer>();
                    v_canvasRenderer.SetMesh(Mesh);
                }*/
            }
        }

        protected virtual void TryUpdateNativeSize(bool p_force = false)
        {
            if (_updateNativeSize || p_force || AutoCalculateRectBounds)
            {
                _updateNativeSize = false;
                SetNativeSizeWithoutMarkAsDirty();
            }
        }

        protected virtual void RecalcMesh()
        {
            TryUpdateNativeSize();
            ClearMesh();
            if (SharedMesh != null)
            {
                Vector3[] v_vertices = SharedMesh.vertices;
                int[] v_triangles = SharedMesh.triangles;
                Vector3[] v_normals = SharedMesh.normals;
                Vector2[] v_uv = SharedMesh.uv;
                Color32[] v_colors = new Color32[v_vertices.Length];

                Rect v_transformLocalRect = rectTransform.GetLocalRect();

                //Find Local Rect of the Mesh
                var v_bounds = SharedMesh.bounds;
                Rect v_meshLocalRect = new Rect(v_bounds.min, v_bounds.size);

                //Calculate New Scale
                Vector3 v_scaleFixer = new Vector2(v_transformLocalRect.width != 0 ? (v_transformLocalRect.width / v_meshLocalRect.width) : 0,
                    v_transformLocalRect.height != 0 ? (v_transformLocalRect.height / v_meshLocalRect.height) : 0);
                if (PreserveAspectRatio)
                    v_scaleFixer = new Vector3(Mathf.Min(v_scaleFixer.x, v_scaleFixer.y), Mathf.Min(v_scaleFixer.x, v_scaleFixer.y), 0);
                v_scaleFixer.z = Mathf.Min(v_scaleFixer.x, v_scaleFixer.y);

                //Pick Delta AnchorPosition to always place the mesh in center of the rect
                Vector3 v_delta = Vector2.zero;
                if (!AutoCalculateRectBounds)
                    v_delta = new Vector2(v_transformLocalRect.xMin + (v_transformLocalRect.width * rectTransform.pivot.x), v_transformLocalRect.yMin + (v_transformLocalRect.height * rectTransform.pivot.y)) - v_transformLocalRect.center;
                if (v_scaleFixer.x > 0.00001f && v_scaleFixer.y > 0.00001f && v_scaleFixer.z > 0.00001f)
                {
                    for (int i = 0; i < v_vertices.Length; i++)
                    {
                        var v_vertice = !AutoCalculateRectBounds ? (v_vertices[i] - SharedMesh.bounds.center) : v_vertices[i];
                        v_vertice.x *= v_scaleFixer.x;
                        v_vertice.y *= v_scaleFixer.y;
                        v_vertice.z *= v_scaleFixer.z;
                        v_vertice = v_vertice - v_delta;
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

        public override void SetNativeSize()
        {
            if (SharedMesh != null)
            {
                SetNativeSizeWithoutMarkAsDirty();
                this.SetAllDirty();
            }
        }

        protected virtual void SetNativeSizeWithoutMarkAsDirty()
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
                        v_transform.anchorMin = new Vector2(Mathf.Clamp01(v_transform.anchorMin.x), Mathf.Clamp01(v_transform.anchorMin.y));
                        v_transform.anchorMax = new Vector2(Mathf.Clamp01(v_transform.anchorMax.x), Mathf.Clamp01(v_transform.anchorMax.y));
                    }
                    Rect v_meshLocalRect = new Rect(v_bounds.min, v_bounds.size);
                    var v_scaler = transform.parent;
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

        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();
            base.canvasRenderer.SetTexture(m_texture != null? m_texture : mainTexture);
        }

        #endregion
    }
}
