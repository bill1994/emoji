using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Extensions;
using System;

namespace Kyub
{
    [System.Serializable]
    public class ComplexShape : BaseShape
    {
        #region Private Variables

        [SerializeField]
        List<PolygonShape> m_shapes = new List<PolygonShape>();

        #endregion

        #region Public Properties

        public List<PolygonShape> Shapes
        {
            get
            {
                return m_shapes;
            }
        }

        #endregion

        #region Constructors

        public ComplexShape()
        {
        }

        public ComplexShape(ComplexShape p_complexShape, bool p_optimize = false)
        {
            if (p_complexShape != null)
            {
                m_shapes = new List<PolygonShape>();
                //Clone Each Internal Polygon
                foreach (var v_shape in p_complexShape.Shapes)
                {
                    m_shapes.Add(new PolygonShape(v_shape));
                }
                TryRecalcBounds(true);
                if (p_optimize)
                {
                    Optimize();
                    TryRecalcBounds();
                }
            }
        }

        public ComplexShape(IEnumerable<PolygonShape> p_polygons, bool p_optimize = true)
        {
            if (p_polygons != null)
            {
                m_shapes = new List<PolygonShape>(p_polygons);
                TryRecalcBounds(true);
                if (p_optimize)
                {
                    Optimize();
                    TryRecalcBounds();
                }
            }
        }

        public ComplexShape(IEnumerable<List<Vector2>> p_polygonsVertices, bool p_optimize = true)
        {
            if (p_polygonsVertices != null)
            {
                List<PolygonShape> v_polygons = new List<PolygonShape>();
                foreach (var v_vertices in p_polygonsVertices)
                {
                    var v_polygon = new PolygonShape(v_vertices);
                    v_polygons.Add(v_polygon);
                }
                m_shapes = v_polygons;
                //CorrectOrientation(true);
                TryRecalcBounds(true);
                if (p_optimize)
                {
                    Optimize();
                    TryRecalcBounds();
                }
            }
        }

        public ComplexShape(IEnumerable<Vector2[]> p_polygonsVertices, bool p_optimize = true)
        {
            if (p_polygonsVertices != null)
            {
                List<PolygonShape> v_polygons = new List<PolygonShape>();
                foreach (var v_vertices in p_polygonsVertices)
                {
                    var v_polygon = new PolygonShape(v_vertices);
                    v_polygons.Add(v_polygon);
                }
                m_shapes = v_polygons;
                //CorrectOrientation(true);
                TryRecalcBounds(true);
                if (p_optimize)
                {
                    Optimize();
                    TryRecalcBounds();
                }
            }
        }

        #endregion

        #region Geometry Clip Functions

        public ComplexShape Intersection(Rect p_rect)
        {
            return Intersection(new PolygonShape(p_rect));
        }

        public ComplexShape Intersection(PolygonShape p_shape)
        {
            return Intersection(new ComplexShape(p_shape));
        }

        public ComplexShape Intersection(ComplexShape p_complexShape)
        {
            return ExecuteGeometryClip(p_complexShape, ExternLibs.ClipperLib.ClipType.ctIntersection);
        }

        public ComplexShape Difference(Rect p_rect)
        {
            return Difference(new PolygonShape(p_rect));
        }

        public ComplexShape Difference(PolygonShape p_shape)
        {
            return Difference(new ComplexShape(p_shape));
        }

        public ComplexShape Difference(ComplexShape p_complexShape)
        {
            return ExecuteGeometryClip(p_complexShape, ExternLibs.ClipperLib.ClipType.ctDifference);
        }

        public ComplexShape Union(Rect p_rect)
        {
            return Union(new PolygonShape(p_rect));
        }

        public ComplexShape Union(PolygonShape p_shape)
        {
            return Union(new ComplexShape(p_shape));
        }

        public ComplexShape Union(ComplexShape p_complexShape)
        {
            return ExecuteGeometryClip(p_complexShape, ExternLibs.ClipperLib.ClipType.ctUnion);
        }

        #endregion

        #region Public Functions

        public virtual void ReverseOrientation()
        {
            foreach (var v_shape in Shapes)
            {
                if (v_shape != null)
                    v_shape.ReverseOrientation();
            }
        }

        public virtual bool IsOrientedClockWise()
        {
            var v_firstShape = Shapes.GetFirst();
            if (v_firstShape != null)
                return v_firstShape.IsOrientedClockwise();
            return true;
        }

        public virtual List<PolygonShape> GetHoles()
        {
            List<PolygonShape> v_holes = new List<PolygonShape>();
            foreach (var v_shape in Shapes)
            {
                if (v_shape != null && !v_shape.IsEmpty() && v_shape.IsOrientedClockwise() != IsOrientedClockWise())
                    v_holes.Add(v_shape);
            }
            return v_holes;
        }

        public virtual List<PolygonShape> GetNonHoles()
        {
            List<PolygonShape> v_nonHoles = new List<PolygonShape>();
            foreach (var v_shape in Shapes)
            {
                if (v_shape != null && !v_shape.IsEmpty() && v_shape.IsOrientedClockwise() == IsOrientedClockWise())
                    v_nonHoles.Add(v_shape);
            }
            return v_nonHoles;
        }

        public override void TransformPosition(Rect p_fromRect, Rect p_toRect)
        {
            if (m_shapes != null)
            {
                foreach (var v_shape in m_shapes)
                {
                    if (v_shape != null)
                    {
                        v_shape.TransformPosition(p_fromRect, p_toRect);
                    }
                }
            }
            RecalcBounds();
        }

        public override bool IsEmpty()
        {
            var v_sucess = m_shapes == null || m_shapes.Count == 0;
            if (!v_sucess)
            {
                foreach (var v_shape in m_shapes)
                {
                    if (v_shape != null)
                        v_shape.IsEmpty();
                }
            }
            return v_sucess;
        }

        public bool IsPolygonShape()
        {
            return Shapes.Count == 1;
        }

        public bool IsRect()
        {
            var v_sucess = false;
            if (IsPolygonShape())
            {
                var v_shape = Shapes[0];
                if (v_shape != null)
                    v_shape.IsRect();
            }
            return v_sucess;
        }

        public override void Resize(Vector2 p_scaleFactor, int p_amountOfDecimalPlaces = 5)
        {
            if (m_shapes != null)
            {
                foreach (var v_shape in m_shapes)
                {
                    if (v_shape != null)
                    {
                        v_shape.Resize(p_scaleFactor, p_amountOfDecimalPlaces);
                    }
                }
            }
            MarkToRecalcBounds();
        }

        public void CorrectOrientation(bool p_isClockwise)
        {
            if (m_shapes != null)
            {
                foreach (var v_shape in m_shapes)
                {
                    if (v_shape.IsOrientedClockwise() != p_isClockwise)
                        v_shape.ReverseOrientation();
                }
            }
        }

        /// <summary>
        /// This functions will merge the internal shapes in an optimized shape
        /// </summary>
        public void Optimize(bool p_cleanPolygon = true, float p_cleanPointDistance = 1.415f)
        {
            if (!IsEmpty())
            {
                List<PolygonShape> v_finalShapes = new List<PolygonShape>();
                List<List<Vector2>> v_solutions = new List<List<Vector2>>();

                //Fix Scale to improve precision
                float v_resizeFactor = GetScaleFixerValue();
                if (v_resizeFactor > 0 && v_resizeFactor < 1)
                    Resize(Vector2.one / v_resizeFactor, 2);

                Kyub.ExternLibs.ClipperLib.Clipper v_cliper = new Kyub.ExternLibs.ClipperLib.Clipper();
                foreach (var v_shape in Shapes)
                {
                    if (v_shape != null)
                        v_cliper.AddPath(v_shape.Vertices, Kyub.ExternLibs.ClipperLib.PolyType.ptSubject, true);
                }
                v_cliper.Execute(Kyub.ExternLibs.ClipperLib.ClipType.ctUnion, v_solutions,
                    Kyub.ExternLibs.ClipperLib.PolyFillType.pftNonZero, Kyub.ExternLibs.ClipperLib.PolyFillType.pftNonZero);

                if (p_cleanPolygon)
                {
                    v_solutions = Kyub.ExternLibs.ClipperLib.Clipper.CleanPolygons(v_solutions, p_cleanPointDistance);
                    //v_solutions = Kyub.ExternLibs.ClipperLib.Clipper.SimplifyPolygons(v_solutions);
                }
                foreach (var v_solution in v_solutions)
                {
                    if (v_solution != null)
                        v_finalShapes.Add(new PolygonShape(v_solution));
                }

                m_shapes = v_finalShapes;

                //Revert to original size imprecision
                if (v_resizeFactor > 0 && v_resizeFactor < 1)
                    Resize(Vector2.one * v_resizeFactor, 5);

                if (!IsOrientedClockWise())
                    ReverseOrientation();
                MarkToRecalcBounds();
            }
        }

        public void AddShape(PolygonShape p_polygon, bool p_optimize = true)
        {
            if (p_polygon != null)
            {
                Shapes.Add(p_polygon);
                MarkToRecalcBounds();
            }
            if (p_optimize)
                Optimize();
        }

        public void AddShape(ComplexShape p_complexPolygon, bool p_optimize = true)
        {
            if (p_complexPolygon != null)
            {
                foreach (var v_shape in p_complexPolygon.Shapes)
                {
                    AddShape(v_shape, false);
                }
            }
            MarkToRecalcBounds();
            if (p_optimize)
                Optimize();
        }

        public override bool PointInShape(Vector2 p_point)
        {
            foreach (var v_shape in Shapes)
            {
                if (v_shape != null && v_shape.PointInShape(p_point))
                    return true;
            }
            return false;
        }

        public void RecalcBounds()
        {
            TryRecalcBounds(true);
        }

        public override bool TryRecalcBounds(bool p_force = false)
        {
            foreach (var v_shape in Shapes)
            {
                if (v_shape != null)
                {
                    var v_recalculated = v_shape.TryRecalcBounds();
                    if (v_recalculated)
                        _recalcBounds = true;
                }
            }
            return base.TryRecalcBounds(p_force);
        }

        #endregion

        #region Internal Helper Functions

        protected ComplexShape ExecuteGeometryClip(ComplexShape p_complexShape, ExternLibs.ClipperLib.ClipType p_clipType)
        {
            //PolygonShape v_rectShape = new PolygonShape(Rect.MinMaxRect(v_offset.x * i, v_offset.y * j, v_offset.x * (i + 1), v_offset.y * (j + 1)));
            Kyub.ExternLibs.ClipperLib.Clipper v_clipper = new ExternLibs.ClipperLib.Clipper();

            //Fix Scale to improve precision
            var v_resizeFactor = Mathf.Max(p_complexShape.GetScaleFixerValue(), GetScaleFixerValue());
            if (v_resizeFactor > 0 && v_resizeFactor < 1)
            {
                p_complexShape.Resize(Vector2.one / v_resizeFactor, 2);
                Resize(Vector2.one / v_resizeFactor, 2);
            }

            foreach (var v_shape in Shapes)
            {
                if (v_shape != null)
                    v_clipper.AddPath(v_shape.Vertices, ExternLibs.ClipperLib.PolyType.ptSubject, true);
            }
            if (p_complexShape != null)
            {
                foreach (var v_shape in p_complexShape.Shapes)
                {
                    if (v_shape != null)
                        v_clipper.AddPath(v_shape.Vertices, ExternLibs.ClipperLib.PolyType.ptClip, true);
                }
            }
            List<List<Vector2>> v_solutions = new List<List<Vector2>>();
            v_clipper.Execute(p_clipType, v_solutions, Kyub.ExternLibs.ClipperLib.PolyFillType.pftNonZero, Kyub.ExternLibs.ClipperLib.PolyFillType.pftNonZero);

            //v_solutions = Kyub.ExternLibs.ClipperLib.Clipper.CleanPolygons(v_solutions);
            //Revert to original size precision
            ComplexShape v_finalSolution = new ComplexShape(v_solutions);
            if (v_resizeFactor > 0 && v_resizeFactor < 1)
            {
                p_complexShape.Resize(Vector2.one * v_resizeFactor);
                Resize(Vector2.one * v_resizeFactor);
                v_finalSolution.Resize(Vector2.one * v_resizeFactor);
            }

            return v_finalSolution;
        }

        protected override void RecalcBoundsInternal()
        {
            var v_finalRect = new Rect(0, 0, 0, 0);
            if (Shapes.Count > 0 && Shapes[0] != null)
                v_finalRect = new Rect(Shapes[0].RectBounds);
            foreach (var v_shape in Shapes)
            {
                var v_shapeRect = v_shape.RectBounds;
                v_finalRect.xMin = Mathf.Min(v_shapeRect.xMin, v_finalRect.xMin);
                v_finalRect.yMin = Mathf.Min(v_shapeRect.yMin, v_finalRect.yMin);
                v_finalRect.xMax = Mathf.Max(v_shapeRect.xMax, v_finalRect.xMax);
                v_finalRect.yMax = Mathf.Max(v_shapeRect.yMax, v_finalRect.yMax);
            }
            _rectBounds = v_finalRect;
        }

        protected internal override void FlipInternal(Vector2 p_center, bool p_flipX, bool p_flipY)
        {
            if (m_shapes != null)
            {
                foreach (var v_shape in m_shapes)
                {
                    if (v_shape != null)
                    {
                        v_shape.FlipInternal(p_center, p_flipX, p_flipY);
                    }
                }
            }
        }

        protected internal override void RotateInternal(Vector2 p_center, float p_angle)
        {
            if (m_shapes != null)
            {
                foreach (var v_shape in m_shapes)
                {
                    if (v_shape != null)
                    {
                        v_shape.Rotate(p_center, p_angle);
                    }
                }
            }
        }

        #endregion

        #region Operators

        public static implicit operator ComplexShape(Rect p_rect)
        {
            ComplexShape v_shape = new PolygonShape(p_rect);
            return v_shape;
        }

        public static implicit operator ComplexShape(PolygonShape p_polygonShape)
        {
            ComplexShape v_shape = new ComplexShape();
            v_shape.Shapes.Add(p_polygonShape);

            return v_shape;
        }

        public static explicit operator ComplexShape(List<PolygonShape> p_polygons)
        {
            ComplexShape v_shape = new ComplexShape(p_polygons);
            return v_shape;
        }

        public static explicit operator ComplexShape(PolygonShape[] p_polygons)
        {
            ComplexShape v_shape = new ComplexShape(p_polygons);
            return v_shape;
        }

        public static explicit operator Rect(ComplexShape p_complexShape)
        {
            Rect v_rect = p_complexShape != null ? p_complexShape.RectBounds : new Rect(0, 0, 0, 0);
            return v_rect;
        }

        #endregion

        #region Equals Override

        public override bool Equals(object p_obj)
        {
            var v_complexShape = p_obj as ComplexShape;
            if (v_complexShape == null || v_complexShape.IsEmpty())
                return IsEmpty();
            else if(Shapes.Count == v_complexShape.Shapes.Count && RectBounds.Equals(v_complexShape.RectBounds))
            {
                Shapes.RemoveNulls();
                List<PolygonShape> v_shapesToCheck = new List<PolygonShape>(v_complexShape.Shapes);
                v_shapesToCheck.RemoveNulls();
                foreach (var v_shape in Shapes)
                {
                    bool v_hasEqual = false;
                    for (int i = 0; i < v_shapesToCheck.Count; i++)
                    {
                        if (v_shape.Equals(v_shapesToCheck[i]))
                        {
                            v_hasEqual = true;
                            v_shapesToCheck.RemoveAt(i);
                            break;
                        }
                    }
                    if (!v_hasEqual)
                        break;
                }
                //If we removed all checked shapes, it means that both shapes are equal
                return v_shapesToCheck.Count == 0;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    [System.Serializable]
    public class PolygonShape : ClosedShape
    {
        #region Constructors

        public PolygonShape()
        {
        }

        public PolygonShape(IEnumerable<Vector2> p_vertices)
        {
            if(p_vertices != null)
                Vertices = new List<Vector2>(p_vertices);
        }

        public PolygonShape(PolygonShape p_shape)
        {
            if (p_shape != null)
                Vertices = new List<Vector2>(p_shape.Vertices);
        }

        public PolygonShape(Rect p_rect)
        {
            Vertices = new List<Vector2>() { new Vector2(p_rect.xMin, p_rect.yMin), new Vector2(p_rect.xMin, p_rect.yMax), new Vector2(p_rect.xMax, p_rect.yMax), new Vector2(p_rect.xMax, p_rect.yMin) };
        }

        #endregion

        #region Geometry Clip Functions

        public ComplexShape Intersection(Rect p_rect)
        {
            return Intersection(new ComplexShape(p_rect));
        }

        public ComplexShape Intersection(PolygonShape p_shape)
        {
            return Intersection(new ComplexShape(p_shape));
        }

        public ComplexShape Intersection(ComplexShape p_complexShape)
        {
            return new ComplexShape(this).Intersection(p_complexShape);
        }

        public ComplexShape Difference(Rect p_rect)
        {
            return Difference(new ComplexShape(p_rect));
        }

        public ComplexShape Difference(PolygonShape p_shape)
        {
            return Difference(new ComplexShape(p_shape));
        }

        public ComplexShape Difference(ComplexShape p_complexShape)
        {
            return new ComplexShape(this).Difference(p_complexShape);
        }

        public ComplexShape Union(Rect p_rect)
        {
            return Union(new PolygonShape(p_rect));
        }

        public ComplexShape Union(PolygonShape p_shape)
        {
            return Union(new ComplexShape(p_shape));
        }

        public ComplexShape Union(ComplexShape p_complexShape)
        {
            return new ComplexShape(this).Union(p_complexShape);
        }

        #endregion

        #region Public Functions

        public bool IsRect()
        {
            var v_sucess = false;
            if (Vertices.Count == 4)
            {
                Rect v_bounds = RectBounds;
                v_sucess = true;
                for (int i=0; i<Vertices.Count; i++)
                {
                    var v_next = i < Vertices.Count - 1 ? i + 1 : 0;
                    var v_diff = Vertices[v_next] - Vertices[i];
                    if ((Mathf.Abs(v_diff.x) == Mathf.Abs(v_bounds.width) && v_diff.y == 0) ||
                        (Mathf.Abs(v_diff.y) == Mathf.Abs(v_bounds.height) && v_diff.x == 0))
                    {
                        continue;
                    }
                    else
                    {
                        v_sucess = false;
                        break;
                    }
                }
            }
            return v_sucess;
        }

        #endregion

        #region Static Functions

        public static PolygonShape CreateRegularPolygon(Rect p_rect, int p_verticesCount, float p_startingAngle = 0)
        {
            List<Vector2> v_points = new List<Vector2>();
            float v_stepDeltaAngle = 360.0f / p_verticesCount;

            float v_angle = p_startingAngle;
            for (float i = p_startingAngle; i < p_startingAngle + 360.0; i += v_stepDeltaAngle) //go in a circle
            {
                v_points.Add(DegreesToPoint(v_angle, p_rect));
                v_angle += v_stepDeltaAngle;
            }

            var v_polygonShape = new PolygonShape(v_points);

            return v_polygonShape;
        }

        #endregion

        #region internal Static Helper Functions

        private static Vector2 DegreesToPoint(float p_degrees, Rect p_rect)
        {
            Vector2 v_point = new Vector2();
            float v_radians = p_degrees * Mathf.PI / 180.0f;

            v_point.x = Mathf.Cos(v_radians) * p_rect.size.x / 2.0f + p_rect.center.x;
            v_point.y = Mathf.Sin(-v_radians) * p_rect.size.y / 2.0f + p_rect.center.y;

            return v_point;
        }

        #endregion

        #region Operators

        public static implicit operator PolygonShape(Rect p_rect)
        {
            PolygonShape v_shape = new PolygonShape(p_rect);
            return v_shape;
        }

        public static explicit operator PolygonShape(List<Vector2> p_vertices)
        {
            PolygonShape v_shape = new PolygonShape(p_vertices);
            return v_shape;
        }

        public static explicit operator List<Vector2>(PolygonShape p_shape)
        {
            return p_shape != null? p_shape.Vertices: new List<Vector2>();
        }

        public static explicit operator PolygonShape(Vector2[] p_vertices)
        {
            PolygonShape v_shape = new PolygonShape(p_vertices);
            return v_shape;
        }

        public static explicit operator Vector2[](PolygonShape p_shape)
        {
            return p_shape != null ? p_shape.Vertices.ToArray() : new Vector2[0];
        }

        public static explicit operator PolygonShape(ComplexShape p_complexShape)
        {
            PolygonShape v_shape = p_complexShape != null ? p_complexShape.Shapes.GetFirst() : null;
            return v_shape;
        }

        public static explicit operator Rect(PolygonShape p_shape)
        {
            Rect v_rect = p_shape != null ? p_shape.RectBounds : new Rect(0, 0, 0, 0);
            return v_rect;
        }

        #endregion
    }

    [System.Serializable]
    public abstract class ClosedShape : BaseShape
    {
        #region Private Variables

        [SerializeField]
        List<Vector2> m_vertices = new List<Vector2>();

        #endregion

        #region Public Properties

        public List<Vector2> Vertices
        {
            get
            {
                if (m_vertices == null)
                    m_vertices = new List<Vector2>();
                return m_vertices;
            }
            set
            {
                if (m_vertices == value)
                    return;
                m_vertices = value;
                TryRecalcBounds(true);
            }
        }
        #endregion

        #region Public Functions

        public override void TransformPosition(Rect p_fromRect, Rect p_toRect)
        {
            if (m_vertices != null)
            {
                for (int i = 0; i < m_vertices.Count; i++)
                {
                    m_vertices[i] = Rect.PointToNormalized(p_fromRect, m_vertices[i]); // convert to normalized position
                    m_vertices[i] = Rect.NormalizedToPoint(p_toRect, m_vertices[i]); //convert to new rect position
                }
            }
            RecalcBounds();
        }

        public override bool IsEmpty()
        {
            return m_vertices == null || m_vertices.Count == 0;
        }

        public override void Resize(Vector2 p_scaleFactor, int p_amountOfDecimalPlaces = 5)
        {
            if (m_vertices != null)
            {
                for (int i = 0; i < m_vertices.Count; i++)
                {
                    if (p_amountOfDecimalPlaces <= 0)
                        m_vertices[i] = new Vector2(Mathf.Round(m_vertices[i].x * p_scaleFactor.x), Mathf.Round(m_vertices[i].y * p_scaleFactor.y));
                    else
                        m_vertices[i] = new Vector2((float)System.Math.Round(m_vertices[i].x * p_scaleFactor.x, p_amountOfDecimalPlaces), (float)System.Math.Round(m_vertices[i].y * p_scaleFactor.y, p_amountOfDecimalPlaces));
                }
            }
            MarkToRecalcBounds();
        }

        public void RecalcBounds()
        {
            TryRecalcBounds(true);
        }

        public Vector2 GetCentroid()
        {
            Vector2 v_point = new Vector2(0, 0);
            if (Vertices.Count > 0)
            {
                // Add the first point at the end of the array.
                // Find the centroid.

                //We need to interate until last vertice of original polygon (not including the extra vertice add
                for (int i = 0; i < Vertices.Count; i++)
                {
                    int v_nextIndex = i + 1 < Vertices.Count ? i + 1 : 0;
                    float v_delta = Vertices[i].x * Vertices[v_nextIndex].y - Vertices[v_nextIndex].x * Vertices[i].y;
                    v_point.x += (Vertices[i].x + Vertices[v_nextIndex].x) * v_delta;
                    v_point.y += (Vertices[i].y + Vertices[v_nextIndex].y) * v_delta;
                }

                // Divide by 6 times the polygon's area.
                float polygon_area = GetPolygonArea();
                v_point.x /= (6 * polygon_area);
                v_point.x /= (6 * polygon_area);

                // If the values are negative, the polygon is
                // oriented counterclockwise so reverse the signs.
                if (v_point.x < 0)
                {
                    v_point.x = -v_point.x;
                    v_point.y = -v_point.y;
                }
            }
            return v_point;
        }

        public float GetPolygonArea()
        {
            // Return the absolute value of the signed area.
            // The signed area is negative if the polygon is
            // oriented clockwise.
            return Mathf.Abs(GetSignedPolygonArea());
        }

        public bool IsConvex()
        {
            // For each set of three adjacent points A, B, C,
            // find the dot product AB · BC. If the sign of
            // all the dot products is the same, the angles
            // are all positive or negative (depending on the
            // order in which we visit them) so the polygon
            // is convex.
            bool v_isNegative = false;
            bool v_isPositive = false;
            int B, C;
            for (int A = 0; A < Vertices.Count; A++)
            {
                B = (A + 1) % Vertices.Count;
                C = (B + 1) % Vertices.Count;

                float v_crossProduct = CrossProductLength(Vertices[A], Vertices[B], Vertices[C]);
                if (v_crossProduct < 0)
                {
                    v_isNegative = true;
                }
                else if (v_crossProduct > 0)
                {
                    v_isPositive = true;
                }
                if (v_isNegative && v_isPositive)
                    return false;
            }

            // If we got this far, the polygon is convex.
            return true;
        }

        public override bool PointInShape(Vector2 p_point)
        {
            if (Vertices.Count > 0)
            {
                //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
                //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
                //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
                int result = 0;
                int cnt = Vertices.Count;
                if (cnt < 3)
                    return false;
                Vector2 ip = Vertices[0];
                for (int i = 1; i <= cnt; ++i)
                {
                    Vector2 ipNext = (i == cnt ? Vertices[0] : Vertices[i]);
                    if (ipNext.y == p_point.y)
                    {
                        if ((ipNext.x == p_point.x) || (ip.y == p_point.y &&
                          ((ipNext.x > p_point.x) == (ip.x < p_point.x))))
                            return true;
                    }
                    if ((ip.y < p_point.y) != (ipNext.y < p_point.y))
                    {
                        if (ip.x >= p_point.x)
                        {
                            if (ipNext.x > p_point.x)
                                result = 1 - result;
                            else
                            {
                                double d = (double)(ip.x - p_point.x) * (ipNext.y - p_point.y) -
                                  (double)(ipNext.x - p_point.x) * (ip.y - p_point.y);
                                if (d == 0)
                                    return true;
                                else if ((d > 0) == (ipNext.y > ip.y))
                                    result = 1 - result;
                            }
                        }
                        else
                        {
                            if (ipNext.x > p_point.x)
                            {
                                double d = (double)(ip.x - p_point.x) * (ipNext.y - p_point.y) -
                                  (double)(ipNext.x - p_point.x) * (ip.y - p_point.y);
                                if (d == 0)
                                    return true;
                                else if ((d > 0) == (ipNext.y > ip.y))
                                    result = 1 - result;
                            }
                        }
                    }
                    ip = ipNext;
                }

                return result != 0;
            }
            return false;
        }

        // Return true if the point is in the polygon.
        /*public override bool PointInShape(Vector2 p_point)
        {
            if (Vertices.Count > 0)
            {
                // Get the angle between the point and the
                // first and last vertices.
                int v_maxIndex = Vertices.Count - 1;
                float v_totalAngle = GetAngle(Vertices[v_maxIndex], p_point, Vertices[0]);

                // Add the angles from the point
                // to each other pair of vertices.
                for (int i = 0; i < v_maxIndex; i++)
                {
                    v_totalAngle += GetAngle(Vertices[i], p_point, Vertices[i + 1]);
                }

                // The total angle should be 2 * PI or -2 * PI if
                // the point is in the polygon and close to zero
                // if the point is outside the polygon.
                return (Mathf.Abs(v_totalAngle) > 0.000001);
            }
            return false;
        }*/

        #endregion

        #region Internal Vector Functions

        // Return the cross product AB x BC.
        // The cross product is a vector perpendicular to AB
        // and BC having length |AB| * |BC| * Sin(theta) and
        // with direction given by the right-hand rule.
        // For two vectors in the X-Y plane, the result is a
        // vector with X and Y components 0 so the Z component
        // gives the vector's length and direction.
        protected float CrossProductLength(Vector2 p_a, Vector2 p_b, Vector2 p_c)
        {
            // Get the vectors' coordinates.
            float v_BAx = p_a.x - p_b.x;
            float v_BAy = p_a.y - p_b.y;
            float v_BCx = p_c.x - p_b.x;
            float v_BCy = p_c.y - p_b.y;

            // Calculate the Z coordinate of the cross product.
            return (v_BAx * v_BCy - v_BAy * v_BCx);
        }

        // Return the dot product AB · BC.
        // Note that AB · BC = |AB| * |BC| * Cos(theta).
        protected float DotProduct(Vector2 p_a, Vector2 p_b, Vector2 p_c)
        {
            // Get the vectors' coordinates.
            float v_BAx = p_a.x - p_b.x;
            float v_BAy = p_a.y - p_b.y;
            float v_BCx = p_c.x - p_b.x;
            float v_BCy = p_c.y - p_b.y;

            // Calculate the dot product.
            return (v_BAx * v_BCx + v_BAy * v_BCy);
        }

        // Return the angle ABC.
        // Return a value between PI and -PI.
        // Note that the value is the opposite of what you might
        // expect because Y coordinates increase downward.
        protected float GetAngle(Vector2 p_a, Vector2 p_b, Vector2 p_c)
        {
            // Get the dot product.
            float v_dotProduct = DotProduct(p_a, p_b, p_c);

            // Get the cross product.
            float v_crossProduct = CrossProductLength(p_a, p_b, p_c);

            // Calculate the angle.
            return (float)Mathf.Atan2(v_crossProduct, v_dotProduct);
        }

        #endregion

        #region Internal Orientation Functions

        // Return true if the polygon is oriented clockwise.
        public bool IsOrientedClockwise()
        {
            return (GetSignedPolygonArea() < 0);
        }

        // If the polygon is oriented counterclockwise,
        // reverse the order of its points.
        public void ReverseOrientation()
        {
            Vertices.Reverse();
        }

        #endregion

        #region Internal Area Functions

        // Return the polygon's area in "square units."
        // Add the areas of the trapezoids defined by the
        // polygon's edges dropped to the X-axis. When the
        // program considers a bottom edge of a polygon, the
        // calculation gives a negative area so the space
        // between the polygon and the axis is subtracted,
        // leaving the polygon's area. This method gives odd
        // results for non-simple polygons.
        //
        // The value will be negative if the polygon is
        // oriented clockwise.
        protected float GetSignedPolygonArea()
        {
            // Get the areas.
            float v_area = 0;
            for (int i = 0; i < Vertices.Count; i++)
            {
                int v_nextIndex = i + 1 < Vertices.Count ? i + 1 : 0;
                //1/2 * (Sum of Xi*Yi+1 - Xi+1*Yi) is the Signed Area
                v_area += (Vertices[i].x * Vertices[v_nextIndex].y - Vertices[v_nextIndex].x * Vertices[i].y);
            }
            v_area /= 2;

            // Return the result.
            return v_area;
        }
        #endregion // Area Routines

        #region Internal Helper Functions

        protected internal override void FlipInternal(Vector2 p_center, bool p_flipX, bool p_flipY)
        {
            var v_center = p_center;
            if (m_vertices != null)
            {
                for (int i = 0; i < m_vertices.Count; i++)
                {
                    var v_vertice = m_vertices[i];
                    var v_delta = v_center - v_vertice;
                    if (p_flipX)
                        v_vertice.x = v_center.x + v_delta.x;
                    if (p_flipY)
                        v_vertice.y = v_center.y + v_delta.y;
                    m_vertices[i] = v_vertice;
                }
            }
        }

        protected internal override void RotateInternal(Vector2 p_center, float p_angle)
        {
            if (m_vertices != null && p_angle != 0)
            {
                for (int i = 0; i < m_vertices.Count; i++)
                {
                    var v_vertice = m_vertices[i];
                    m_vertices[i] = RotatePointAroundPivot(v_vertice, p_center, p_angle);
                }
                RecalcBounds();
            }
        }

        Vector2 RotatePointAroundPivot(Vector2 p_point, Vector2 p_pivot, float p_angle)
        {
            if (p_angle != 0)
            {
                Vector2 v_dir = p_point - p_pivot; // get point direction relative to pivot
                v_dir = Quaternion.Euler(new Vector3(0, 0, p_angle)) * v_dir; // rotate it
                p_point = v_dir + p_pivot; // calculate rotated point
            }
            return p_point;
        }

        protected override void RecalcBoundsInternal()
        {
            _rectBounds = new Rect(0, 0, 0, 0);
            if (m_vertices != null)
            {
                Vector2 v_min = m_vertices.Count > 0 ? (Vector2)m_vertices[0] : Vector2.zero;
                Vector2 v_max = m_vertices.Count > 0 ? (Vector2)m_vertices[0] : Vector2.zero;
                foreach (var v_vertice in m_vertices)
                {
                    if (v_vertice.x < v_min.x)
                        v_min.x = v_vertice.x;
                    if (v_vertice.y < v_min.y)
                        v_min.y = v_vertice.y;
                    if (v_vertice.x > v_max.x)
                        v_max.x = v_vertice.x;
                    if (v_vertice.y > v_max.y)
                        v_max.y = v_vertice.y;
                }
                _rectBounds = Rect.MinMaxRect(v_min.x, v_min.y, v_max.x, v_max.y);
            }
        }

        #endregion

        #region Equals Override

        public override bool Equals(object p_obj)
        {
            var v_polygonShape = p_obj as PolygonShape;
            if (p_obj == this)
                return true;
            else if (v_polygonShape == null || v_polygonShape.IsEmpty())
                return IsEmpty();
            else if( v_polygonShape.Vertices.Count == Vertices.Count && RectBounds.Equals(v_polygonShape.RectBounds))
            {
                var v_firstSelfVertice = Vertices[0];
                var v_verticeIndexInOtherShape = v_polygonShape.Vertices.IndexOf(v_firstSelfVertice);
                if (v_verticeIndexInOtherShape < 0)
                    return false;
                else
                {
                    var v_sucess = true;
                    for (int i = 0; i < Vertices.Count; i++)
                    {
                        var v_currentVertice = Vertices[i];
                        var v_otherVerticeIndex = i + v_verticeIndexInOtherShape;
                        if (v_otherVerticeIndex >= v_polygonShape.Vertices.Count)
                            v_otherVerticeIndex -= v_polygonShape.Vertices.Count;
                        var v_otherVertice = v_polygonShape.Vertices[v_otherVerticeIndex];
                        if (v_currentVertice != v_otherVertice)
                        {
                            v_sucess = false;
                            break;
                        }
                    }
                    return v_sucess;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }

    [System.Serializable]
    public abstract class BaseShape
    {
        #region Consts

        protected internal const float MIN_RECT_SIZE_TO_USE_AS_SCALE_FIXER = 500f;

        #endregion

        #region Properties

        protected Rect _rectBounds = new Rect(0, 0, 0, 0);
        public Rect RectBounds
        {
            get
            {
                TryRecalcBounds();
                return new Rect(_rectBounds.position, _rectBounds.size);
            }
        }

        #endregion

        #region Internal Helper Functions

        public void FlipVertical(Vector2 p_flipCenter)
        {
            FlipInternal(p_flipCenter, false, true);
        }

        public void FlipVertical()
        {
            FlipVertical(RectBounds.center);
        }

        public void FlipHorizontal(Vector2 p_flipCenter)
        {
            FlipInternal(p_flipCenter, true, false);
        }

        public void FlipHorizontal()
        {
            FlipHorizontal(RectBounds.center);
        }

        protected internal abstract void FlipInternal(Vector2 p_center, bool p_flipX, bool p_flipY);

        public void Rotate(float p_angle)
        {
            Rotate(RectBounds.center, p_angle);
        }

        public void Rotate(Vector2 p_pivotCenter, float p_angle)
        {
            RotateInternal(p_pivotCenter, p_angle);
        }

        protected internal abstract void RotateInternal(Vector2 p_center, float p_angle);

        public abstract void TransformPosition(Rect p_fromRect, Rect p_toRect);

        public abstract bool IsEmpty();

        public abstract void Resize(Vector2 p_scaleFactor, int p_amountOfDecimalPlaces = 5);

        public abstract bool PointInShape(Vector2 p_point);

        protected bool _recalcBounds = false;
        public virtual void MarkToRecalcBounds()
        {
            _recalcBounds = true;
        }

        public virtual bool TryRecalcBounds(bool p_force = false)
        {
            if (_recalcBounds || p_force)
            {
                _recalcBounds = false;
                RecalcBoundsInternal();
                return true;
            }
            return false;
        }

        protected abstract void RecalcBoundsInternal();

        
        protected float GetScaleFixerValue()
        {
            return GetScaleFixerValue(MIN_RECT_SIZE_TO_USE_AS_SCALE_FIXER);
        }

        protected float GetScaleFixerValue(float p_minRectSize)
        {
            var v_resizeFactor = Mathf.Max(RectBounds.size.x / p_minRectSize, RectBounds.size.y / p_minRectSize);
            return Mathf.Clamp01(v_resizeFactor);
        }

        #endregion
    }
}
