using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Extensions;
using Kyub;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class GLWireframeMeshEffect : DirtyBehaviour
    {
        #region Helper Classes

        [System.Flags]
        public enum LinesToDrawEnum { First = 1, Second = 2, Third = 4 }

        #endregion

        #region Static Properties

        protected static Material s_defaultMaterial = null;
        protected static Material DefaultMaterial
        {
            get
            {
                if (s_defaultMaterial == null)
                {
                    var v_shader = Shader.Find("Hidden/Internal-Colored");
                    if (v_shader != null)
                    {
                        s_defaultMaterial = new Material(v_shader);
                        s_defaultMaterial.hideFlags = HideFlags.HideAndDontSave;
                        //Turn on alpha blending
                        s_defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        s_defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        // Turn backface culling off
                        s_defaultMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                        // Turn off depth writes
                        s_defaultMaterial.SetInt("_ZWrite", 0);
                    }
                }
                return s_defaultMaterial;
            }
        }

        #endregion

        #region Private Variables

        [SerializeField]
        bool m_drawMeshGraphics = true;
        [SerializeField, MaskEnum]
        LinesToDrawEnum m_linesToDraw = LinesToDrawEnum.First | LinesToDrawEnum.Second | LinesToDrawEnum.Third;
        [SerializeField, Range(0, 30)]
        float m_lineWidth = 7.0f;
        [SerializeField]
        Color m_color = Color.white;
        [SerializeField]
        Material m_material = null;

        #endregion

        #region Public Properties

        public bool DrawMeshGraphics
        {
            get
            {
                return m_drawMeshGraphics;
            }
            set
            {
                if (m_drawMeshGraphics == value)
                    return;
                m_drawMeshGraphics = value;
            }
        }

        public LinesToDrawEnum LinesToDraw
        {
            get
            {
                return m_linesToDraw;
            }
            set
            {
                if (m_linesToDraw == value)
                    return;
                m_linesToDraw = value;
            }
        }

        public float LineWidth
        {
            get
            {
                if (m_lineWidth < 0)
                    m_lineWidth = 0;
                return m_lineWidth;
            }
            set
            {
                if (m_lineWidth == value)
                    return;
                m_lineWidth = value;
            }
        }

        public Color Color
        {
            get
            {
                return m_color;
            }
            set
            {
                if (m_color == value)
                    return;
                m_color = value;
            }
        }

        public Material Material
        {
            get
            {
                if (m_material == null)
                    return DefaultMaterial;
                return m_material;
            }
            set
            {
                if (m_material == value)
                    return;
                m_material = value;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Update()
        {
            CheckRendererActive();
            //base.Update();
        }

        protected virtual void OnRenderObject()
        {
            DrawWireframeMesh();
        }

        #endregion

        #region Helper Functions

        protected override void Apply()
        {
            RecalcMesh();
        }

        protected Mesh _mesh = null;
        protected virtual void RecalcMesh()
        {
            MeshFilter v_meshFilter = GetComponent<MeshFilter>();
            SkinnedMeshRenderer v_skinnedRenderer = GetComponent<SkinnedMeshRenderer>();
            UIMeshGraphic v_uiFilter = GetComponent<UIMeshGraphic>();

            if (v_uiFilter != null)
            {
                if (!v_uiFilter.enabled || !v_uiFilter.gameObject.activeSelf || !v_uiFilter.gameObject.activeInHierarchy)
                    v_uiFilter.TryApplyBakedMesh(true);
                var v_mesh = v_uiFilter.Mesh;
                if (_mesh != v_mesh)
                {
                    DestroyMesh();
                    _mesh = v_mesh;
                }
            }
            else if (v_skinnedRenderer != null)
            {
                ClearMesh();
                v_skinnedRenderer.BakeMesh(_mesh);
            }
            else if (v_meshFilter != null)
            {
                _mesh = v_meshFilter.sharedMesh;
            }
        }

        protected virtual void ClearMesh()
        {
            MeshFilter v_meshFilter = GetComponent<MeshFilter>();
            if (v_meshFilter == null || v_meshFilter.sharedMesh != _mesh)
            {
                if (_mesh == null)
                    _mesh = new Mesh();
                else
                    _mesh.Clear();
            }
            else
                _mesh = new Mesh();
        }

        protected virtual void DestroyMesh()
        {
            MeshFilter v_meshFilter = GetComponent<MeshFilter>();
            if (v_meshFilter == null || v_meshFilter.sharedMesh != _mesh)
            {
                if (_mesh != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(_mesh);
                    else
                        Object.DestroyImmediate(_mesh);
                }
            }
            _mesh = null;
        }

        protected virtual Camera FindDrawingCamera()
        {
            Camera v_camera = CameraUtils.FindDrawingCamera(this.gameObject);
            if (v_camera == null)
                v_camera = CameraUtils.CachedMainCamera;
            return v_camera;
        }

        protected virtual void CheckRendererActive()
        {
            Renderer v_renderer = GetComponent<Renderer>();
            UIMeshGraphic v_uiFilter = GetComponent<UIMeshGraphic>();
            //MeshFilter v_meshFilter = GetComponent<MeshFilter>();

            if (v_renderer != null)
                v_renderer.enabled = m_drawMeshGraphics;
            if (v_uiFilter != null)
                v_uiFilter.enabled = m_drawMeshGraphics;
            //if ((v_uiFilter != null && v_uiFilter.Mesh != _mesh) ||
            //    (v_meshFilter != null && v_meshFilter.sharedMesh != _mesh) ||
            //    v_renderer is SkinnedMeshRenderer)
            //{
                SetDirty();
            //}
        }

        #endregion

        #region Helper Draw Functions

        protected virtual void DrawLine(Vector3 p_point1, Vector3 p_point2, Camera p_camera = null)
        {
            if (p_camera != null)
            {
                float v_width = LineWidth == 0? 0 : (1.0f / Screen.width * LineWidth * 0.5f);
                //vector from line center to camera
                Vector3 v_edge1 = p_camera.transform.position - (p_point2 + p_point1) / 2.0f;
                //vector from point to point
                Vector3 v_edge2 = p_point2 - p_point1;
                Vector3 v_perpendicular = Vector3.Cross(v_edge1, v_edge2).normalized * v_width;

                GL.Vertex(p_point1 - v_perpendicular);
                GL.Vertex(p_point1 + v_perpendicular);
                GL.Vertex(p_point2 + v_perpendicular);
                GL.Vertex(p_point2 - v_perpendicular);
            }
            else
            {
                GL.Vertex(p_point1);
                GL.Vertex(p_point2);
                GL.Vertex(p_point2);
                GL.Vertex(p_point1);
            }
        }

        protected virtual void DrawWireframeMesh()
        {
            if (_mesh != null && Material != null)
            {
                var v_camera = FindDrawingCamera();
                // Apply the Material
                Material.SetPass(0);
                GL.PushMatrix();
                // Set transformation matrix for drawing to
                // match our transform
                //GL.MultMatrix(transform.localToWorldMatrix);
                
                GL.Begin(GL.QUADS);
                GL.Color(m_color);
                var v_vertices = _mesh.vertices;
                var v_indexes = _mesh.GetIndices(0);
                for (int i = 0; i + 2 < v_indexes.Length; i += 3)
                {
                    Vector3 v_vertice1 = transform.TransformPoint(v_vertices[v_indexes[i]]);
                    Vector3 v_vertice2 = transform.TransformPoint(v_vertices[v_indexes[i + 1]]);
                    Vector3 v_vertice3 = transform.TransformPoint(v_vertices[v_indexes[i + 2]]);
                    if (m_linesToDraw.ContainsFlag(LinesToDrawEnum.First)) DrawLine(v_vertice1, v_vertice2, v_camera);
                    if (m_linesToDraw.ContainsFlag(LinesToDrawEnum.Second)) DrawLine(v_vertice2, v_vertice3, v_camera);
                    if (m_linesToDraw.ContainsFlag(LinesToDrawEnum.Third)) DrawLine(v_vertice3, v_vertice1, v_camera);
                }
                GL.End();
                GL.PopMatrix();
            }
        }

        #endregion

    }
}
