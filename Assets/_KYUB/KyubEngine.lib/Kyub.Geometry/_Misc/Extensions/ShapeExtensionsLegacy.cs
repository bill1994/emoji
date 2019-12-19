using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.ExternLibs.Poly2Tri;
using Kyub.Extensions;

namespace Kyub.Extensions
{
    public static class ShapeExtensionsLegacy
    {
        #region Ear Clipping Methods

        public static ComplexShape ToTrianglesLegacy(this ComplexShape p_polygon)
        {
            ComplexShape v_trianglesShape = new ComplexShape();
            if (p_polygon != null)
            {
                foreach (var v_shape in p_polygon.Shapes)
                {
                    if (v_shape != null)
                        v_trianglesShape.AddShape(v_shape.ToTrianglesLegacy(), false);
                }
            }
            return v_trianglesShape;
        }

        public static ComplexShape ToTrianglesLegacy(this PolygonShape p_polygon)
        {
            List<int> v_trianglesIndex = p_polygon.Triangulate();
            List<PolygonShape> v_shapes = new List<PolygonShape>();
            for (int i = 0; i < v_trianglesIndex.Count - 2; i += 3)
            {
                if (v_trianglesIndex[i] < p_polygon.Vertices.Count && v_trianglesIndex[i + 1] < p_polygon.Vertices.Count && v_trianglesIndex[i + 2] < p_polygon.Vertices.Count)
                {
                    PolygonShape v_polyShape = new PolygonShape(new List<Vector2>() { p_polygon.Vertices[v_trianglesIndex[i]], p_polygon.Vertices[v_trianglesIndex[i + 1]], p_polygon.Vertices[v_trianglesIndex[i + 2]] });
                    v_shapes.Add(v_polyShape);
                }
            }
            return new ComplexShape(v_shapes, false);
        }

        #endregion

        #region Deprecated Methods

        public static List<Mesh> BakeMeshs(this ComplexShape p_polygon, TriangulationOptionEnum p_triangulationOption = TriangulationOptionEnum.Default)
        {
            return p_polygon.BakeMeshs(null, p_triangulationOption);
        }

        public static List<Mesh> BakeMeshs(this ComplexShape p_polygon, List<Rect> v_rectUvPerShape, TriangulationOptionEnum p_triangulationOption = TriangulationOptionEnum.Default)
        {
            List<Mesh> v_meshs = new List<Mesh>();
            for (int i = 0; i < p_polygon.Shapes.Count; i++)
            {
                var v_shape = p_polygon.Shapes[i];
                if (v_shape != null)
                {
                    var v_rectUv = v_rectUvPerShape != null && v_rectUvPerShape.Count > i ? v_rectUvPerShape[i] : v_shape.RectBounds;
                    var v_mesh = v_shape.BakeMesh(v_rectUv, p_triangulationOption);
                    if (v_mesh != null)
                        v_meshs.Add(v_mesh);
                }
            }
            return v_meshs;
        }

        public static Mesh BakeMesh(this PolygonShape p_polygon, TriangulationOptionEnum p_triangulationOption = TriangulationOptionEnum.Default)
        {
            return p_polygon.BakeMesh(p_polygon.RectBounds, p_triangulationOption);
        }

        /// <summary>
        /// Default option use EarBased Algorithm (very slow) for huge amount of vertices
        /// </summary>
        /// <param name="p_rectToCalcUV"></param>
        /// <param name="p_triangulationOption"></param>
        /// <returns></returns>
        public static Mesh BakeMesh(this PolygonShape p_polygon, Rect p_rectToCalcUV, TriangulationOptionEnum p_triangulationOption = TriangulationOptionEnum.Default)
        {
            if (p_polygon.Vertices != null && p_polygon.Vertices.Count > 0)
            {
                Mesh v_mesh = new Mesh();
                var v_vertices = new List<Vector3>();
                v_vertices.Add(p_polygon.RectBounds.center);

                var v_uvs = new List<Vector2>();
                v_uvs.Add(new Vector2(0.5f, 0.5f));

                var v_colors = new List<Color>();
                v_colors.Add(Color.white);

                //Calculate Triangulation
                var v_indexes = p_triangulationOption == TriangulationOptionEnum.Default ? p_polygon.GetTrianglesWithDefaultOption() : p_polygon.GetTringlesWithUnsafeOption();

                //Map the Vertex and Calculate UVs
                foreach (var v_vertice in p_polygon.Vertices)
                {
                    v_vertices.Add(v_vertice);
                    var v_uv = Rect.PointToNormalized(p_rectToCalcUV, v_vertice);
                    v_uvs.Add(v_uv);
                    v_colors.Add(Color.white);
                }

                //FillMesh
                v_mesh.SetVertices(v_vertices);
                v_mesh.SetUVs(0, v_uvs);
                v_mesh.SetColors(v_colors);
                v_mesh.SetIndices(v_indexes.ToArray(), MeshTopology.Triangles, 0);
                return v_mesh;
            }
            return null;
        }

        static List<int> GetTringlesWithUnsafeOption(this PolygonShape p_polygon)
        {
            var v_indexes = new List<int>();
            if (p_polygon.Vertices != null && p_polygon.Vertices.Count > 0)
            {
                //Add center to initial vertice
                var v_vertices = new List<Vector3>();
                v_vertices.Add(p_polygon.RectBounds.center);

                //Map all vertices to temp array
                foreach (var v_vertice in p_polygon.Vertices)
                {
                    v_vertices.Add(v_vertice);
                }

                //Pick IndexBuffer
                for (int i = 1; i < v_vertices.Count; i++)
                {
                    if (v_vertices.Count > i + 1)
                    {
                        v_indexes.Add(0);
                        v_indexes.Add(i);
                        v_indexes.Add(i + 1);
                    }
                    else
                    {
                        v_indexes.Add(0);
                        v_indexes.Add(i);
                        v_indexes.Add(1);
                    }
                }
            }
            return v_indexes;
        }

        static List<int> GetTrianglesWithDefaultOption(this PolygonShape p_polygon)
        {
            List<int> v_indexes = p_polygon.Triangulate();
            //First Index in PolygonShape is the Center. When using default tringulation this value is not used so we must add one to each index
            for (int i = 0; i < v_indexes.Count; i++)
            {
                v_indexes[i] += 1;
            }
            //Prevent Erros when indexes is not multiple of 3
            var v_extraElements = (v_indexes.Count % 3);
            if (v_extraElements != 0)
                v_indexes.ClampToCount(Mathf.Max(0, v_indexes.Count - v_extraElements));
            return v_indexes;
        }

        // Triangulate the polygon.
        //
        // For a nice, detailed explanation of this method,
        // see Ian Garton's Web page:
        // http://www-cgrl.cs.mcgill.ca/~godfried/teaching/cg-projects/97/Ian/cutting_ears.html
        static List<int> Triangulate(this PolygonShape p_polygon)
        {
            List<int> v_indexes = new List<int>();
            if (p_polygon.Vertices.Count > 2)
            {
                // Copy the points into a scratch array.
                Vector2[] v_pts = p_polygon.Vertices.ToArray();

                // Make a scratch polygon.
                MorphableShape v_pgon = new MorphableShape(v_pts);

                // Orient the polygon Clockwise
                if (!v_pgon.IsOrientedClockwise())
                    v_pgon.ReverseOrientation();

                // Make room for the triangles.
                List<MorphableShape> v_triangles = new List<MorphableShape>();

                // While the copy of the polygon has more than
                // three points, remove an ear.
                while (v_pgon.Vertices.Count > 3)
                {
                    // Remove an ear from the polygon.
                    v_pgon.RemoveEar(v_triangles);
                }

                // Copy the last three points into their own triangle.
                v_triangles.Add(new MorphableShape(new List<Vector2>() { v_pgon.Vertices[0], v_pgon.Vertices[1], v_pgon.Vertices[2] }));

                //Find Index of vertice of each triangle in self Vertice List
                foreach (var v_triangle in v_triangles)
                {
                    if (!v_triangle.IsOrientedClockwise())
                        v_triangle.ReverseOrientation();
                    foreach (var v_vertice in v_triangle.Vertices)
                    {
                        for (int i = 0; i < p_polygon.Vertices.Count; i++)
                        {
                            var v_thisVertice = p_polygon.Vertices[i];
                            if (v_thisVertice == v_vertice)
                            {
                                v_indexes.Add(i);
                            }
                        }
                    }
                }
            }

            return v_indexes;
        }

        #endregion

        #region Helper Classes

        public enum TriangulationOptionEnum { Default, UnsafeCenterBased }

        class MorphableShape : ClosedShape
        {
            #region Constructors

            public MorphableShape()
            {
            }

            public MorphableShape(IEnumerable<Vector2> p_vertices)
            {
                Vertices = new List<Vector2>(p_vertices);
            }

            #endregion

            #region Helper Functions

            // Find the indexes of three points that form an "ear."
            public bool FindEar(ref int p_a, ref int p_b, ref int p_c)
            {
                for (p_a = 0; p_a < Vertices.Count; p_a++)
                {
                    p_b = (p_a + 1) % Vertices.Count;
                    p_c = (p_b + 1) % Vertices.Count;

                    if (FormsEar(p_a, p_b, p_c))
                        return true;
                }
                p_a = Vertices.Count - 1; // prevent exception
                return false;
            }

            // Return true if the three points form an ear.
            public bool FormsEar(int p_a, int p_b, int p_c)
            {
                if (Vertices != null)
                {
                    // See if the angle ABC is concave.
                    if (GetAngle(Vertices[p_a], Vertices[p_b], Vertices[p_c]) > 0)
                    {
                        // This is a concave corner so the triangle
                        // cannot be an ear.
                        return false;
                    }

                    // Make the triangle A, B, C.
                    MorphableShape v_triangle = new MorphableShape(new List<Vector2>() { Vertices[p_a], Vertices[p_b], Vertices[p_c] });

                    // Check the other points to see 
                    // if they lie in triangle A, B, C.
                    for (int i = 0; i < Vertices.Count; i++)
                    {
                        if ((i != p_a) && (i != p_b) && (i != p_c))
                        {
                            if (v_triangle.PointInShape(Vertices[i]))
                            {
                                // This point is in the triangle 
                                // do this is not an ear.
                                return false;
                            }
                        }
                    }
                    // This is an ear.
                    return true;
                }
                return false;
            }

            // Remove an ear from the polygon and
            // add it to the triangles array.
            public void RemoveEar(List<MorphableShape> p_triangles)
            {
                if (p_triangles != null)
                {
                    // Find an ear.
                    int v_a = 0, v_b = 0, v_c = 0;
                    FindEar(ref v_a, ref v_b, ref v_c);
                    // Create a new triangle for the ear.
                    p_triangles.Add(new MorphableShape(new List<Vector2>() { Vertices[v_a], Vertices[v_b], Vertices[v_c] }));

                    // Remove the ear from the polygon.
                    RemovePointFromArray(v_b);
                }
            }


            // Remove point target from the array.
            public void RemovePointFromArray(int p_target)
            {
                Vector2[] v_vertices = new Vector2[Vertices.Count - 1];
                System.Array.Copy(Vertices.ToArray(), 0, v_vertices, 0, p_target);
                System.Array.Copy(Vertices.ToArray(), p_target + 1, v_vertices, p_target, Vertices.Count - p_target - 1);
                Vertices = new List<Vector2>(v_vertices);
            }

            #endregion
        }

        #endregion
    }
}
