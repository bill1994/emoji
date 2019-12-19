using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Kyub;

namespace Kyub.UI
{
    public class AlphaTween : TimeTween
    {
        #region Private Variables

        [SerializeField]
        float m_from = 0f;
        [SerializeField]
        float m_to = 1f;
        [SerializeField]
        bool m_propagateToChildrens = false; // used to Color Childrens
        [SerializeField]
        bool m_setInitialValueWhenStart = false; //Value returned by component, not by From or To

        #endregion

        #region Public Properties

        public float From { get { return m_from; } set { m_from = value; } }
        public float To { get { return m_to; } set { m_to = value; } }
        public bool PropagateToChildrens { get { return m_propagateToChildrens; } set { m_propagateToChildrens = value; TrackChildrenComponents(); } }
        public bool SetInitialValueWhenStart { get { return m_setInitialValueWhenStart; } set { m_setInitialValueWhenStart = value; } }

        #endregion

        #region Protected Properties

        protected float Alpha
        {
            get
            {
                if (SpriteRendererComponent != null) return SpriteRendererComponent.color.a;
                if (OwnerMaterial != null) return OwnerMaterial.color.a;
                if (CanvasGroupComponent) return CanvasGroupComponent.alpha;
                if (uiGraphics != null) return uiGraphics.color.a;
                return 0f;
            }
            set
            {
                if (SpriteRendererComponent != null)
                {
                    Color v_tempColor = SpriteRendererComponent.color;
                    v_tempColor.a = value;
                    SpriteRendererComponent.color = v_tempColor;
                }
                if (OwnerMaterial != null)
                {
                    Color v_tempColor = OwnerMaterial.color;
                    v_tempColor.a = value;
                    OwnerMaterial.color = v_tempColor;
                }
                if (CanvasGroupComponent != null)
                {
                    CanvasGroupComponent.alpha = value;
                }
                else if (uiGraphics != null)
                {
                    Color v_tempColor = uiGraphics.color;
                    v_tempColor.a = value;
                    uiGraphics.color = v_tempColor;
                }
                SetAlphaInChildrens(value);
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

        CanvasGroup _canvasGroupComponent = null;
        protected CanvasGroup CanvasGroupComponent
        {
            get
            {
                if (Target != null && _canvasGroupComponent == null)
                    _canvasGroupComponent = Target.GetComponent<CanvasGroup>();
                return _canvasGroupComponent;
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

        List<CanvasGroup> _uiCanvasGroupChildrens = new List<CanvasGroup>();
        List<MaskableGraphic> _uiGraphicsChildrens = new List<MaskableGraphic>();
        List<SpriteRenderer> _spriteRendererChildrens = new List<SpriteRenderer>();
        List<Material> _materialChildrens = new List<Material>();

        #endregion

        #region Helper Functions

        protected virtual void TrackChildrenComponents()
        {
            if (PropagateToChildrens)
            {
                if (Target != null)
                {
                    _spriteRendererChildrens = new List<SpriteRenderer>(Target.GetComponentsInChildren<SpriteRenderer>());
                    _uiGraphicsChildrens = new List<MaskableGraphic>(Target.GetComponentsInChildren<MaskableGraphic>());
                    _uiCanvasGroupChildrens = new List<CanvasGroup>(Target.GetComponentsInChildren<CanvasGroup>());
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
                _uiCanvasGroupChildrens.Clear();
            }
        }

        protected void SetAlphaInChildrens(float p_alphaValue)
        {
            foreach (SpriteRenderer v_renderer in _spriteRendererChildrens)
            {
                if (v_renderer != null)
                {
                    Color v_tempColor = v_renderer.color;
                    v_tempColor.a = p_alphaValue;
                    v_renderer.color = v_tempColor;
                }
            }
            foreach (CanvasGroup v_group in _uiCanvasGroupChildrens)
            {
                if (v_group != null)
                {
                    v_group.alpha = p_alphaValue;
                }
            }
            foreach (MaskableGraphic v_graphic in _uiGraphicsChildrens)
            {
                if (v_graphic != null)
                {
                    Color v_tempColor = v_graphic.color;
                    v_tempColor.a = p_alphaValue;
                    v_graphic.color = v_tempColor;
                }
            }
            foreach (Material v_material in _materialChildrens)
            {
                if (v_material != null)
                {
                    Color v_tempColor = v_material.color;
                    v_tempColor.a = p_alphaValue;
                    v_material.color = v_tempColor;
                }
            }
        }

        #endregion

        #region Overridden Functions

        float _initialValue = 0f;
        protected override void OnPingStart()
        {
            TrackChildrenComponents();
            _initialValue = SetInitialValueWhenStart ? Alpha : From;
            Alpha = Mathf.Lerp(_initialValue, To, GetTimeScale());
        }

        protected override void OnPongStart()
        {
            TrackChildrenComponents();
            _initialValue = SetInitialValueWhenStart ? Alpha : To;
            Alpha = Mathf.Lerp(From, _initialValue, GetTimeScale());
        }

        protected override void OnPingUpdate()
        {
            try
            {
                Alpha = Mathf.Lerp(_initialValue, To, GetTimeScale());
            }
            catch { }
        }

        protected override void OnPongUpdate()
        {
            try
            {
                Alpha = Mathf.Lerp(From, _initialValue, GetTimeScale());
            }
            catch { }
        }

        #endregion
    }
}
