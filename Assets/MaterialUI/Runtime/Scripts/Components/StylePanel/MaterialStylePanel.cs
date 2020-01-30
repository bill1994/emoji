using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialStylePanel : StyleElement<MaterialStylePanel.PanelStyleProperty>
    {
        public override void RefreshVisualStyles(bool p_canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(p_canAnimate, 0);
        }

        #region BaseStyleElement Helper Classes

        [System.Serializable]
        public class PanelStyleProperty : StyleProperty
        {
            #region Private Variables

            [SerializeField, SerializeStyleProperty]
            protected Color m_color = Color.white;

            #endregion

            #region Public Properties

            public Color Color
            {
                get
                {
                    return m_color;
                }

                set
                {
                    m_color = value;
                }
            }

            #endregion

            #region Constructor

            public PanelStyleProperty()
            {
            }

            public PanelStyleProperty(string p_name, Component p_target, Color p_color, bool p_useStyleGraphic)
            {
                m_target = p_target != null ? p_target.transform : null;
                m_name = p_name;
                m_color = p_color;
                m_useStyleGraphic = p_useStyleGraphic;
            }

            #endregion

            #region Helper Functions

            public override void Tween(BaseStyleElement p_sender, bool p_canAnimate, float p_animationDuration)
            {
                TweenManager.EndTween(_tweenId);

                var v_graphic = GetTarget<Graphic>();
                if (v_graphic != null)
                {
                    var v_endColor = m_color;
                    if (p_canAnimate && Application.isPlaying)
                    {
                        _tweenId = TweenManager.TweenColor(
                                (color) =>
                                {
                                    if (v_graphic != null)
                                        v_graphic.color = color;
                                },
                                v_graphic.color,
                                v_endColor,
                                p_animationDuration
                            );
                    }
                    else
                    {
                        v_graphic.color = v_endColor;
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
