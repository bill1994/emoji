using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialStylePanel : StyleElement<MaterialStylePanel.PanelStyleProperty>
    {
        public override void RefreshVisualStyles(bool canAnimate = true)
        {
            SetStylePropertyColorsActive_Internal(canAnimate, 0);
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

            public PanelStyleProperty(string name, Component target, Color color, bool useStyleGraphic)
            {
                m_target = target != null ? target.transform : null;
                m_name = name;
                m_color = color;
                m_useStyleGraphic = useStyleGraphic;
            }

            #endregion

            #region Helper Functions

            public override void Tween(BaseStyleElement sender, bool canAnimate, float animationDuration)
            {
                TweenManager.EndTween(_tweenId);

                var graphic = GetTarget<Graphic>();
                if (graphic != null)
                {
                    var endColor = m_color;
                    if (canAnimate && Application.isPlaying)
                    {
                        _tweenId = TweenManager.TweenColor(
                                (color) =>
                                {
                                    if (graphic != null)
                                        graphic.color = color;
                                },
                                graphic.color,
                                endColor,
                                animationDuration
                            );
                    }
                    else
                    {
                        graphic.color = endColor;
                    }
                }
            }

            #endregion
        }

        #endregion
    }
}
