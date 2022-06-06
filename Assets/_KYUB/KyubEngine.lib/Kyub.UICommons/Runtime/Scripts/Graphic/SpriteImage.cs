using UnityEngine;
using UnityEngine.UI;
using Kyub.UI.Internal;

using static Kyub.UI.Internal.EllipseShapeUtils.EllipseProperties;
using static Kyub.UI.Internal.RoundedRectShapeUtils;
using static Kyub.UI.Internal.UIGeometryUtils;
using static Kyub.UI.Internal.UIGeometryUtils.OutlineProperties;

namespace Kyub.UI
{
    [AddComponentMenu("UI Commons/Sprite Image", 11)]
    public class SpriteImage : Image
    {
        #region Private Variables

        [SerializeField]
        PreserveAspectRatioModeEnum m_PreserveAspectMode = PreserveAspectRatioModeEnum.Default;

        //Shape Properties
        [SerializeField]
        ShapeTypeModeEnum m_ShapeType = ShapeTypeModeEnum.Default;
        [SerializeField]
        ShapeFillModeEnum m_ShapeFillMode = ShapeFillModeEnum.Fill;
        [SerializeField]
        float m_ShapeAntiAliasing = 0;

        //Outline Properties
        [SerializeField]
        ShapeOutlineModeEnum m_OutlineType = ShapeOutlineModeEnum.Inner;
        [SerializeField]
        float m_OutlineThickness = 2.0f;

        //Ellipse Properties
        [SerializeField]
        EllipseShapeFittingModeEnum m_EllipseFittingMode = EllipseShapeFittingModeEnum.Default;

        //RoundedRect Properties
        [SerializeField]
        RoundedRectCornerModeEnum m_CornerMode = RoundedRectCornerModeEnum.Default;
        [SerializeField]
        RectRoundness m_CornerRoundness = new RectRoundness(15, 15, 15, 15);

        UnitPositionData _ellipseCachedData;
        RoundedCornerUnitPositionData _roundedRectCachedData;

        #endregion

        #region Public Properties

        public PreserveAspectRatioModeEnum preserveAspectMode
        {
            get
            {
                return m_PreserveAspectMode;
            }
            set
            {
                if (m_PreserveAspectMode == value)
                    return;
                m_PreserveAspectMode = value;

                SetVerticesDirty();
            }
        }

        public ShapeTypeModeEnum shapeType
        {
            get
            {
                return m_ShapeType;
            }
            set
            {
                if (m_ShapeType == value)
                    return;
                m_ShapeType = value;

                SetVerticesDirty();
            }
        }

        public ShapeFillModeEnum shapeFillMode
        {
            get
            {
                return m_ShapeFillMode;
            }
            set
            {
                if (m_ShapeFillMode == value)
                    return;
                m_ShapeFillMode = value;

                SetVerticesDirty();
            }
        }

        public float shapeAntiAliasing
        {
            get
            {
                return m_ShapeAntiAliasing;
            }
            set
            {
                if (m_ShapeAntiAliasing == value)
                    return;
                m_ShapeAntiAliasing = value;

                SetVerticesDirty();
            }
        }

        public ShapeOutlineModeEnum outlineType
        {
            get
            {
                return m_OutlineType;
            }
            set
            {
                if (m_OutlineType == value)
                    return;
                m_OutlineType = value;

                SetVerticesDirty();
            }
        }

        public float outlineThickness
        {
            get
            {
                return m_OutlineThickness;
            }
            set
            {
                if (m_OutlineThickness == value)
                    return;
                m_OutlineThickness = value;

                SetVerticesDirty();
            }
        }

        public EllipseShapeFittingModeEnum ellipseFittingMode
        {
            get
            {
                return m_EllipseFittingMode;
            }
            set
            {
                if (m_EllipseFittingMode == value)
                    return;
                m_EllipseFittingMode = value;

                SetVerticesDirty();
            }
        }

        public RoundedRectCornerModeEnum cornerMode
        {
            get
            {
                return m_CornerMode;
            }
            set
            {
                if (m_CornerMode == value)
                    return;
                m_CornerMode = value;

                SetVerticesDirty();
            }
        }

        public RectRoundness cornerRoundness
        {
            get
            {
                if (m_CornerRoundness == null)
                    m_CornerRoundness = new RectRoundness();
                return m_CornerRoundness;
            }
            set
            {
                if (m_CornerRoundness == value)
                    return;
                m_CornerRoundness = value;
                if (m_CornerRoundness == null)
                    m_CornerRoundness = new RectRoundness();

                SetVerticesDirty();
            }
        }

        public float uniformRoundness
        {
            get
            {
                return m_CornerRoundness != null ? m_CornerRoundness.bottomLeft : 0;
            }
            set
            {
                if (m_CornerRoundness == null)
                    m_CornerRoundness = new RectRoundness();

                var recalculate = m_CornerRoundness.bottomLeft != value;

                m_CornerRoundness.bottomLeft = value;
                m_CornerRoundness.bottomRight = value;
                m_CornerRoundness.topLeft = value;
                m_CornerRoundness.topRight = value;

                if (recalculate)
                {
                    SetVerticesDirty();
                }
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            var applyDefaultLogic = true;

            //Shape Drawer Logic
            if ((sprite == null || type == Type.Simple) &&
                (m_ShapeType != ShapeTypeModeEnum.Default || m_ShapeFillMode != ShapeFillModeEnum.Fill))
            {
                applyDefaultLogic = false;
                var safePreserveAspect = preserveAspect && sprite != null;
                if (m_ShapeType == ShapeTypeModeEnum.RoundedRect)
                {
                    GenerateRoundedMesh(toFill, safePreserveAspect);
                }
                else if (m_ShapeType == ShapeTypeModeEnum.Ellipse)
                {
                    GenerateEllipseMesh(toFill, safePreserveAspect);
                }
                else
                {
                    if (m_ShapeFillMode == ShapeFillModeEnum.Outline)
                    {
                        GenerateRectMesh(toFill, safePreserveAspect);
                    }
                    else
                    {
                        applyDefaultLogic = true;
                    }
                }
            }

            //Default Drawer Logic
            if (applyDefaultLogic)
            {
                var size = sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);
                var preserveAspectMode = GetAspectRatioMode(size);

                if (sprite != null &&
                    preserveAspect &&
                    preserveAspectMode == PreserveAspectRatioModeEnum.Envelop &&
                    type == Type.Simple)
                {
                    GenerateSimpleMesh(toFill, preserveAspect);
                }
                else
                {
                    base.OnPopulateMesh(toFill);
                }
            }
        }

        #endregion

        #region Preserve Aspect Calc

        /// <summary>
        /// Generate vertices for a simple Image.
        /// </summary>
        protected virtual void GenerateSimpleMesh(VertexHelper vh, bool shouldPreserveAspect)
        {
            var size = sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);
            var preserveAspectMode = GetAspectRatioMode(size);

            Vector4 v = GetDrawingDimensions(shouldPreserveAspect, preserveAspectMode);
            var uv = GetDrawingUV(size, shouldPreserveAspect, preserveAspectMode);

            var color32 = color;
            vh.Clear();
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(uv.x, uv.y));
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(uv.x, uv.w));
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(uv.z, uv.w));
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(uv.z, uv.y));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        protected virtual PreserveAspectRatioModeEnum GetAspectRatioMode(Vector2 size)
        {
            var preserveAspectMode = m_PreserveAspectMode;

            //We adjust to envolop when image and drawing space has same orientation (Portrait/ Widescreen/ Square)
            if (preserveAspectMode == PreserveAspectRatioModeEnum.Auto)
            {
                var drawingRect = GetPixelAdjustedRect();
                if ((size.x > size.y && drawingRect.size.x >= drawingRect.size.y) ||
                    (size.x < size.y && drawingRect.size.x < drawingRect.size.y))
                {
                    preserveAspectMode = PreserveAspectRatioModeEnum.Envelop;
                }
                else
                {
                    preserveAspectMode = PreserveAspectRatioModeEnum.Default;
                }
            }

            return preserveAspectMode;
        }

        /// Image's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
        protected virtual Vector4 GetDrawingDimensions(bool shouldPreserveAspect, PreserveAspectRatioModeEnum aspectMode)
        {
            Rect r = GetPixelAdjustedRect();
            if (sprite == null)
            {
                return new Vector4(
                    r.xMin,
                    r.yMin,
                    r.xMax,
                    r.yMax
                );
            }
            else
            {
                var padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
                var size = new Vector2(sprite.rect.width, sprite.rect.height);

                int spriteW = Mathf.RoundToInt(size.x);
                int spriteH = Mathf.RoundToInt(size.y);

                var v = new Vector4(
                    padding.x / spriteW,
                    padding.y / spriteH,
                    (spriteW - padding.z) / spriteW,
                    (spriteH - padding.w) / spriteH);

                if (shouldPreserveAspect && size.sqrMagnitude > 0.0f && aspectMode == PreserveAspectRatioModeEnum.Default)
                {
                    PreserveRectAspectRatio(ref r, size);
                }

                v = new Vector4(
                    r.x + r.width * v.x,
                    r.y + r.height * v.y,
                    r.x + r.width * v.z,
                    r.y + r.height * v.w
                );

                return v;
            }
        }

        protected virtual Vector4 GetDrawingUV(Vector2 textureSize, bool shouldPreserveAspect, PreserveAspectRatioModeEnum aspectMode)
        {
            if (sprite == null)
                return new Vector4(0, 0, 1, 1);

            var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);

            if (shouldPreserveAspect && aspectMode == PreserveAspectRatioModeEnum.Envelop && textureSize.sqrMagnitude > 0)
            {
                var localRect = new Rect(Vector2.zero, new Vector2(Mathf.Abs(rectTransform.rect.width), Mathf.Abs(rectTransform.rect.height)));
                var minMaxRect = Rect.MinMaxRect(uv.x, uv.y, uv.z, uv.w);
                var normalizedRect = Rect.MinMaxRect(minMaxRect.xMin, minMaxRect.yMin, minMaxRect.xMax, minMaxRect.yMax);

                var pivot = rectTransform.pivot;

                if (localRect.width > 0 && localRect.height > 0)
                {
                    var textureProportion = textureSize.x / textureSize.y;
                    var localRectProportion = localRect.width / localRect.height;
                    if (localRectProportion > textureProportion)
                    {
                        var mult = localRect.width > 0 ? textureSize.x / localRect.width : 0;
                        normalizedRect = new Rect(minMaxRect.xMin, minMaxRect.yMin, minMaxRect.width, ((localRect.height * mult) / textureSize.y) * minMaxRect.height);
                        normalizedRect.y = Mathf.Max(minMaxRect.yMin, (minMaxRect.yMax - normalizedRect.height) * pivot.y);
                    }
                    else if (localRectProportion < textureProportion)
                    {
                        var mult = localRect.height > 0 ? textureSize.y / localRect.height : 0;
                        normalizedRect = new Rect(minMaxRect.xMin, minMaxRect.yMin, ((localRect.width * mult) / textureSize.x) * minMaxRect.width, minMaxRect.height);
                        normalizedRect.x = Mathf.Max(minMaxRect.xMin, (1 - normalizedRect.width) * pivot.x);
                    }
                }

                uv = new Vector4(normalizedRect.xMin, normalizedRect.yMin, normalizedRect.xMax, normalizedRect.yMax);
            }

            return uv;
        }

        protected virtual void PreserveRectAspectRatio(ref Rect rect, Vector2 spriteSize)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectRatio = rect.width / rect.height;

            if (spriteRatio > rectRatio)
            {
                var oldHeight = rect.height;
                rect.height = rect.width * (1.0f / spriteRatio);
                rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;
            }
            else
            {
                var oldWidth = rect.width;
                rect.width = rect.height * spriteRatio;
                rect.x += (oldWidth - rect.width) * rectTransform.pivot.x;
            }
        }

        #endregion

        #region Shape Generator Calc

        protected virtual void GenerateRectMesh(VertexHelper vh, bool shouldPreserveAspect)
        {
            var size = sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);
            var preserveAspectMode = GetAspectRatioMode(size);

            Vector4 v = GetDrawingDimensions(shouldPreserveAspect, preserveAspectMode);
            var uv = GetDrawingUV(size, shouldPreserveAspect, preserveAspectMode);
            var color32 = color;
            vh.Clear();

            Rect pixelRect = Rect.MinMaxRect(v.x, v.y, v.z, v.w);
            Rect uvRect = Rect.MinMaxRect(uv.x, uv.y, uv.z, uv.w);

            var outlineProperties = new OutlineProperties();
            var antiAliasingProperties = new AntiAliasingProperties();

            antiAliasingProperties.AntiAliasing = 0;
            outlineProperties.LineWeight = m_OutlineThickness;
            outlineProperties.Type = (LineType)m_OutlineType;

            outlineProperties.OnCheck();
            antiAliasingProperties.OnCheck();

            antiAliasingProperties.UpdateAdjusted(canvas);
            outlineProperties.UpdateAdjusted();

            var edgeGradientData = new UIGeometryUtils.EdgeGradientData();
            if (antiAliasingProperties.Adjusted > 0.0f)
            {
                edgeGradientData.SetActiveData(
                    1.0f,
                    0.0f,
                    antiAliasingProperties.Adjusted
                );
            }
            else
            {
                edgeGradientData.Reset();
            }

            if (m_ShapeFillMode == ShapeFillModeEnum.Fill)
            {
                RectShapeUtils.AddRect(
                    ref vh,
                    pixelRect,
                    color32,
                    uvRect,
                    edgeGradientData
                );
            }
            else
            {
                RectShapeUtils.AddRectRing(
                    ref vh,
                    outlineProperties,
                    pixelRect,
                    color32,
                    uvRect,
                    edgeGradientData
                );
            }
        }

        protected virtual void GenerateRoundedMesh(VertexHelper vh, bool shouldPreserveAspect)
        {
            var size = sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);
            var preserveAspectMode = GetAspectRatioMode(size);

            Vector4 v = GetDrawingDimensions(shouldPreserveAspect, preserveAspectMode);
            var uv = GetDrawingUV(size, shouldPreserveAspect, preserveAspectMode);
            var color32 = color;
            vh.Clear();

            Rect pixelRect = Rect.MinMaxRect(v.x, v.y, v.z, v.w);
            Rect uvRect = Rect.MinMaxRect(uv.x, uv.y, uv.z, uv.w);

            var roundedProperties = new RoundedRectShapeUtils.RoundedProperties();
            var outlineProperties = new OutlineProperties();
            var antiAliasingProperties = new AntiAliasingProperties();

            roundedProperties.Type = m_CornerMode == RoundedRectCornerModeEnum.PerCorner ?
                RoundedRectShapeUtils.RoundedProperties.RoundedType.Individual :
                RoundedRectShapeUtils.RoundedProperties.RoundedType.Uniform;

            roundedProperties.UniformRadius = m_CornerRoundness.bottomLeft;
            roundedProperties.BLRadius = m_CornerRoundness.bottomLeft;
            roundedProperties.BRRadius = m_CornerRoundness.bottomRight;
            roundedProperties.TLRadius = m_CornerRoundness.topLeft;
            roundedProperties.TRRadius = m_CornerRoundness.topRight;

            antiAliasingProperties.AntiAliasing = m_ShapeAntiAliasing;
            outlineProperties.LineWeight = m_OutlineThickness;
            outlineProperties.Type = (LineType)m_OutlineType;

            roundedProperties.OnCheck(pixelRect);
            outlineProperties.OnCheck();
            antiAliasingProperties.OnCheck();

            roundedProperties.UpdateAdjusted(pixelRect, 0.0f);
            antiAliasingProperties.UpdateAdjusted(canvas);
            outlineProperties.UpdateAdjusted();

            var edgeGradientData = new UIGeometryUtils.EdgeGradientData();
            if (antiAliasingProperties.Adjusted > 0.0f)
            {
                edgeGradientData.SetActiveData(
                    1.0f,
                    0.0f,
                    antiAliasingProperties.Adjusted
                );
            }
            else
            {
                edgeGradientData.Reset();
            }

            if (m_ShapeFillMode == ShapeFillModeEnum.Fill)
            {
                RoundedRectShapeUtils.AddRoundedRect(
                    ref vh,
                    pixelRect,
                    roundedProperties,
                    color32,
                    uvRect,
                    ref _roundedRectCachedData,
                    edgeGradientData
                );
            }
            else
            {
                RoundedRectShapeUtils.AddRoundedRectLine(
                    ref vh,
                    pixelRect,
                    outlineProperties,
                    roundedProperties,
                    color32,
                    uvRect,
                    ref _roundedRectCachedData,
                    edgeGradientData
                );
            }
        }

        protected virtual void GenerateEllipseMesh(VertexHelper vh, bool shouldPreserveAspect)
        {
            var size = sprite == null ? Vector2.zero : new Vector2(sprite.rect.width, sprite.rect.height);
            var preserveAspectMode = GetAspectRatioMode(size);

            Vector4 v = GetDrawingDimensions(shouldPreserveAspect, preserveAspectMode);
            var uv = GetDrawingUV(size, shouldPreserveAspect, preserveAspectMode);
            var color32 = color;
            vh.Clear();

            Rect pixelRect = Rect.MinMaxRect(v.x, v.y, v.z, v.w);
            Rect uvRect = Rect.MinMaxRect(uv.x, uv.y, uv.z, uv.w);

            var ellipseProperties = new EllipseShapeUtils.EllipseProperties();
            var outlineProperties = new OutlineProperties();
            var antiAliasingProperties = new AntiAliasingProperties();

            antiAliasingProperties.AntiAliasing = m_ShapeAntiAliasing;
            outlineProperties.LineWeight = m_OutlineThickness;
            outlineProperties.Type = (LineType)m_OutlineType;
            ellipseProperties.Fitting = (EllipseFitting)m_EllipseFittingMode;

            ellipseProperties.OnCheck();
            outlineProperties.OnCheck();
            antiAliasingProperties.OnCheck();

            var radius = new Vector2(pixelRect.size.x / 2, pixelRect.size.y / 2);
            EllipseShapeUtils.SetRadius(ref radius, pixelRect.width, pixelRect.height, ellipseProperties);

            ellipseProperties.UpdateAdjusted(radius, 0.0f);
            antiAliasingProperties.UpdateAdjusted(canvas);
            outlineProperties.UpdateAdjusted();

            var edgeGradientData = new UIGeometryUtils.EdgeGradientData();
            if (antiAliasingProperties.Adjusted > 0.0f)
            {
                edgeGradientData.SetActiveData(
                    1.0f,
                    0.0f,
                    antiAliasingProperties.Adjusted
                );
            }
            else
            {
                edgeGradientData.Reset();
            }

            var fullRect = pixelRect;
            pixelRect = UIGeometryUtils.RectFromCenter(pixelRect.center, new Vector2(radius.x * 2, radius.y * 2));
            if (m_ShapeFillMode == ShapeFillModeEnum.Fill)
            {
                EllipseShapeUtils.AddCircle(
                    ref vh,
                    pixelRect,
                    ellipseProperties,
                    color32,
                    fullRect,
                    uvRect,
                    ref _ellipseCachedData,
                    edgeGradientData
                );
            }
            else
            {
                EllipseShapeUtils.AddRing(
                    ref vh,
                    pixelRect,
                    outlineProperties,
                    ellipseProperties,
                    color32,
                    fullRect,
                    uvRect,
                    ref _ellipseCachedData,
                    edgeGradientData
                );
            }
        }

        #endregion
    }
}
