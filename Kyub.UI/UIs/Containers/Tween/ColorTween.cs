using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Kyub;

namespace Kyub.UI
{
    public class ColorTween : TimeTween
    {

        #region Private Variables

        [SerializeField]
        Color m_from = new Color(1, 1, 1, 1);
        [SerializeField]
        Color m_to = new Color(0, 1, 0, 1);
        [SerializeField]
        bool m_propagateToChildrens = false; // used to Color Childrens
        [SerializeField]
        bool m_setInitialValueWhenStart = false;

        #endregion

        #region Public Properties

        public Color From { get { return m_from; } set { m_from = value; } }
        public Color To { get { return m_to; } set { m_to = value; } }
        public bool PropagateToChildrens { get { return m_propagateToChildrens; } set { m_propagateToChildrens = value; TrackChildrenComponents(); } }
        public bool SetInitialValueWhenStart { get { return m_setInitialValueWhenStart; } set { m_setInitialValueWhenStart = value; } }

        #endregion

        #region Protected Properties

        Color _currentColor = Color.white;
        protected Color Color
        {
            get
            {
                if (OwnerMaterial != null) return OwnerMaterial.color;
                if (uiGraphics != null) return uiGraphics.color;
                return _currentColor;
            }
            set
            {
#if NGUI_DLL
			if (Widget != null) Widget.color = value;
#endif
                if (SpriteRendererComponent != null)
                {
                    SpriteRendererComponent.color = value;
                }
                if (OwnerMaterial != null)
                {
                    OwnerMaterial.color = value;
                }
                if (uiGraphics != null)
                {
                    uiGraphics.color = value;
                }
                SetColorInChildrens(value);
                _currentColor = value;
            }
        }

        SpriteRenderer _spriteRendererComponent = null;
        protected SpriteRenderer SpriteRendererComponent
        {
            get
            {
                if (Target != null && _spriteRendererComponent == null)
                    _spriteRendererComponent = Target.GetComponent<SpriteRenderer>();
                return _spriteRendererComponent;
            }
        }

        MaskableGraphic _uiGraphics = null;
        protected MaskableGraphic uiGraphics
        {
            get
            {
                if (Target != null && _uiGraphics == null)
                    _uiGraphics = Target.GetComponent<MaskableGraphic>();
                return _uiGraphics;
            }
        }

        Material _ownerMaterial = null;
        protected Material OwnerMaterial
        {
            get
            {
                if (Target != null && Target.GetComponent<Renderer>() != null && _ownerMaterial == null)
                {
                    if (Target.GetComponent<Renderer>().material.HasProperty("_Color"))
                        _ownerMaterial = Target.GetComponent<Renderer>().material;
                }
                return _ownerMaterial;
            }
        }

        List<MaskableGraphic> _uiGraphicsChildrens = new List<MaskableGraphic>();
        List<SpriteRenderer> _spriteRendererChildrens = new List<SpriteRenderer>();
        List<Material> _materialChildrens = new List<Material>();

        #endregion

        #region Helper Functions

        public Color GetCurrentColor()
        {
            return Color;
        }

        protected virtual void TrackChildrenComponents()
        {
            if (PropagateToChildrens)
            {
                if (Target != null)
                {
                    _spriteRendererChildrens = new List<SpriteRenderer>(Target.GetComponentsInChildren<SpriteRenderer>());
                    _uiGraphicsChildrens = new List<MaskableGraphic>(Target.GetComponentsInChildren<MaskableGraphic>());
                    Renderer[] v_rendererChildrens = Target.GetComponentsInChildren<Renderer>();
                    foreach (Renderer v_renderer in v_rendererChildrens)
                    {
                        if (v_renderer != null && v_renderer.material != null)
                        {
                            if (v_renderer.material.HasProperty("_Color"))
                                _materialChildrens.Add(v_renderer.material);
                        }
                    }
                }
            }
            else
            {
                _spriteRendererChildrens.Clear();
                _materialChildrens.Clear();
                _uiGraphicsChildrens.Clear();
            }
        }

        protected void SetColorInChildrens(Color p_colorValue)
        {
            foreach (SpriteRenderer v_renderer in _spriteRendererChildrens)
            {
                if (v_renderer != null)
                {
                    Color v_tempColor = p_colorValue;
                    v_renderer.color = v_tempColor;
                }
            }
            foreach (Material v_material in _materialChildrens)
            {
                if (v_material != null)
                {
                    Color v_tempColor = p_colorValue;
                    v_material.color = v_tempColor;
                }
            }
            foreach (MaskableGraphic v_graphic in _uiGraphicsChildrens)
            {
                if (v_graphic != null)
                {
                    Color v_tempColor = p_colorValue;
                    v_graphic.color = v_tempColor;
                }
            }
        }

        #endregion

        #region Overridden Functions

        Color _initialValue = Color.white;
        protected override void OnPingStart()
        {
            TrackChildrenComponents();
            _initialValue = SetInitialValueWhenStart ? Color : From;
            Color = Color.Lerp(_initialValue, To, GetTimeScale());
        }

        protected override void OnPongStart()
        {
            TrackChildrenComponents();
            _initialValue = SetInitialValueWhenStart ? Color : To;
            Color = Color.Lerp(From, _initialValue, GetTimeScale());
        }

        protected override void OnPingUpdate()
        {
            try
            {
                Color = Color.Lerp(_initialValue, To, GetTimeScale());
            }
            catch { }
        }

        protected override void OnPongUpdate()
        {
            try
            {
                Color = Color.Lerp(From, _initialValue, GetTimeScale());
            }
            catch { }
        }

        #endregion
    }
}
