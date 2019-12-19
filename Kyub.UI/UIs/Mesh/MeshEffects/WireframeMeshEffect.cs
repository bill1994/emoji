using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Extensions;
using Kyub;
using UnityEngine.UI;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class WireframeMeshEffect : BaseMeshEffect
    {
        #region Helper Classes

        [System.Flags]
        public enum LinesToDrawEnum { First = 1, Second = 2, Third = 4 }

        #endregion

        #region Private Variables

        [SerializeField]
        bool m_drawMeshGraphics = true;
        [SerializeField, MaskEnum]
        LinesToDrawEnum m_linesToDraw = LinesToDrawEnum.First | LinesToDrawEnum.Second | LinesToDrawEnum.Third;
        [SerializeField, Range(0, 30)]
        float m_lineWidth = 1.0f;
        [SerializeField]
        Color m_color = Color.white;

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
                if (base.graphic != null)
                    base.graphic.SetVerticesDirty();
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
                if (base.graphic != null)
                    base.graphic.SetVerticesDirty();
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
                if (base.graphic != null)
                    base.graphic.SetVerticesDirty();
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
                if (base.graphic != null)
                    base.graphic.SetVerticesDirty();
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            if(base.graphic != null)
                base.graphic.SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            if (base.graphic != null)
                base.graphic.SetVerticesDirty();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (base.graphic != null)
                base.graphic.SetVerticesDirty();
            base.OnValidate();
        }
#endif

        public override void ModifyMesh(VertexHelper p_vertexHelper)
        {
            if (!IsActive())
                return;

            RecalcMesh();
            List<UIVertex> v_vertexList = new List<UIVertex>();
            List<int> v_triangles = new List<int>();
            if (DrawMeshGraphics && _mesh != null)
            {
                var v_vertices = _mesh.vertices;
                var v_tringles = _mesh.triangles;
                var v_normals = _mesh.normals;
                var v_colors = _mesh.colors32;
                var v_uvs = _mesh.uv;
                v_triangles.AddRange(_mesh.triangles);
                for (int i = 0; i < v_vertices.Length; i++)
                {
                    UIVertex v_vertex = new UIVertex();
                    v_vertex.position = v_vertices[i];
                    if (v_normals.Length > i)
                        v_vertex.normal = v_normals[i];
                    else
                        v_vertex.normal = Vector3.one;
                    if (v_colors.Length > i)
                        v_vertex.color = v_colors[i];
                    if (v_uvs.Length > i)
                        v_vertex.uv0 = v_uvs[i];
                    v_vertexList.Add(v_vertex);
                }
            }
            ApplyWireframe(v_vertexList, v_triangles);
            p_vertexHelper.Clear();
            p_vertexHelper.AddUIVertexStream(v_vertexList, v_triangles);
        }

        #endregion

        #region Helper Functions

        protected Mesh _mesh = null;
        protected virtual void RecalcMesh()
        {
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

        #endregion

        #region Helper Draw Functions

        protected virtual void ApplyLine(List<UIVertex> p_vertexList, List<int> p_triangles, Vector3 p_point1, Vector3 p_point2)
        {
            //Find Perpendicular Line to build quad
            var v_perpendicular = p_point1 - p_point2;
            v_perpendicular = Vector3.Cross(v_perpendicular, Vector3.forward);
            v_perpendicular.Normalize();
            v_perpendicular *= LineWidth;

            //Build Vertices
            var v_initialIndex = p_vertexList.Count;
            List<Vector3> v_vertices = new List<Vector3>() { p_point1 - v_perpendicular, p_point1 + v_perpendicular, p_point2 + v_perpendicular, p_point2 - v_perpendicular };
            for (int i = 0; i < v_vertices.Count; i++)
            {
                UIVertex v_vertex = new UIVertex();
                v_vertex.position = v_vertices[i];
                v_vertex.color = Color;
                v_vertex.uv0 = Vector2.zero;
                v_vertex.normal = Vector3.one;
                p_vertexList.Add(v_vertex);
            }
            //Add Indexes
            p_triangles.Add(v_initialIndex);
            p_triangles.Add(v_initialIndex + 1);
            p_triangles.Add(v_initialIndex + 2);
            p_triangles.Add(v_initialIndex + 2);
            p_triangles.Add(v_initialIndex + 3);
            p_triangles.Add(v_initialIndex);
        }

        protected virtual void ApplyWireframe(List<UIVertex> p_vertexList, List<int> p_triangles)
        {
            if (_mesh != null && p_vertexList != null && p_triangles != null)
            {
                var v_vertices = _mesh.vertices;
                var v_indexes = _mesh.GetIndices(0);
                for (int i = 0; i + 2 < v_indexes.Length; i += 3)
                {
                    Vector3 v_vertice1 = v_vertices[v_indexes[i]];
                    Vector3 v_vertice2 = v_vertices[v_indexes[i + 1]];
                    Vector3 v_vertice3 = v_vertices[v_indexes[i + 2]];
                    if (m_linesToDraw.ContainsFlag(LinesToDrawEnum.First)) ApplyLine(p_vertexList, p_triangles, v_vertice1, v_vertice2);
                    if (m_linesToDraw.ContainsFlag(LinesToDrawEnum.Second)) ApplyLine(p_vertexList, p_triangles, v_vertice2, v_vertice3);
                    if (m_linesToDraw.ContainsFlag(LinesToDrawEnum.Third)) ApplyLine(p_vertexList, p_triangles, v_vertice3, v_vertice1);
                }
            }
        }

        #endregion

    }
}
