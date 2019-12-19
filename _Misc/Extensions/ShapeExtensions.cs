using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.ExternLibs.Poly2Tri;
using Kyub.Extensions;

namespace Kyub.Extensions
{
    public static class ShapeExtensions
    {
        #region P2T Methods

        public static ComplexShape ToTriangles(this ComplexShape p_polygon)
        {
            var v_trianglesShape = new ComplexShape();
            List<PolygonShape> v_holes = p_polygon.GetHoles();
            List<PolygonShape> v_nonHoles = p_polygon.GetNonHoles();
            Dictionary<PolygonShape, List<PolygonShape>> v_shapeAndHoles = new Dictionary<PolygonShape, List<PolygonShape>>();
            foreach (var v_shape in v_nonHoles)
            {
                if (!v_shapeAndHoles.ContainsKey(v_shape))
                    v_shapeAndHoles.Add(v_shape, new List<PolygonShape>());
            }
            //Build dictionary that contains Hole Hierarchy per Shape
            foreach (var v_hole in v_holes)
            {
                foreach (var v_shape in v_nonHoles)
                {
                    bool v_isInsideShape = true;
                    //Detect if Point is in Shape
                    foreach (var v_holeVertice in v_hole.Vertices)
                    {
                        if (!v_shape.PointInShape(v_holeVertice))
                        {
                            v_isInsideShape = false;
                            break;
                        }
                    }
                    if (v_isInsideShape)
                    {
                        if (!v_shapeAndHoles[v_shape].Contains(v_hole))
                            v_shapeAndHoles[v_shape].Add(v_hole);
                        break;
                    }
                }
            }

            if (v_shapeAndHoles.Count > 0)
            {
                PolygonSet v_polygonSet = new PolygonSet();

                foreach (var v_shape in v_shapeAndHoles.Keys)
                {
                    var v_P2TPolygon = v_shape.ToP2TPolygon(v_shapeAndHoles[v_shape]);
                    if (v_P2TPolygon != null)
                        v_polygonSet.Add(v_P2TPolygon);
                }
                P2T.Triangulate(v_polygonSet);

                foreach (var v_P2TPolygon in v_polygonSet.Polygons)
                {
                    foreach (var v_P2TTriangle in v_P2TPolygon.Triangles)
                    {
                        PolygonShape v_triangle = new PolygonShape();
                        foreach (var v_point in v_P2TTriangle.Points)
                        {
                            v_triangle.Vertices.Add(new Vector2((float)v_point.X, (float)v_point.Y));
                        }
                        if (v_triangle.GetPolygonArea() != 0)
                        {
                            v_triangle.RecalcBounds();
                            if (!v_triangle.IsOrientedClockwise())
                                v_triangle.ReverseOrientation();
                            v_trianglesShape.AddShape(v_triangle, false);
                        }
                    }
                }
            }
            return v_trianglesShape;
        }

        internal static Polygon ToP2TPolygon(this PolygonShape p_polygon, List<PolygonShape> p_holes = null)
        {
            if (p_polygon != null && p_polygon.Vertices.Count > 2)
            {
                List<PolygonPoint> v_P2TPoints = new List<PolygonPoint>();
                foreach (var v_vertice in p_polygon.Vertices)
                {
                    v_P2TPoints.Add(new PolygonPoint(v_vertice.x, v_vertice.y));
                }
                Polygon v_P2TPolygon = new Polygon(v_P2TPoints);
                if (p_holes != null)
                {
                    foreach (var v_hole in p_holes)
                    {
                        if (v_hole != null)
                        {
                            var v_P2THole = ToP2TPolygon(v_hole);
                            if (v_P2THole != null)
                                v_P2TPolygon.AddHole(v_P2THole);
                        }
                    }
                }
                return v_P2TPolygon;
            }
            return null;
        }

        #endregion
    }
}
