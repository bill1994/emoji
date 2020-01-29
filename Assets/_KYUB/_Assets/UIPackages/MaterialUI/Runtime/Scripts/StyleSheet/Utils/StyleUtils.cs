using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MaterialUI
{
    public static class StyleUtils
    {
        #region Resources Get/Set/Replace

        public static Color32 GetStyleColor(Color32 p_color)
        {
            var v_colorNoAlpha = new Color32(p_color.r, p_color.g, p_color.b, byte.MaxValue);
            return v_colorNoAlpha;
        }

        public static object GetStyleResource(Component p_target)
        {
            if (p_target != null)
            {
                var v_targetGraphic = p_target is Graphic ? p_target as Graphic : p_target.GetComponent<Graphic>();
                if (v_targetGraphic != null)
                {
                    if (v_targetGraphic is IVectorImage)
                    {
                        return (v_targetGraphic as IVectorImage).vectorImageData;
                    }
                    else if (v_targetGraphic is Text)
                    {
                        return (v_targetGraphic as Text).font;
                    }
                    else if (v_targetGraphic is TMPro.TMP_Text)
                    {
                        return (v_targetGraphic as TMPro.TMP_Text).font;
                    }
                    else if (v_targetGraphic is Image)
                    {
                        return (v_targetGraphic as Image).sprite;
                    }
                    else if (v_targetGraphic is RawImage)
                    {
                        return (v_targetGraphic as RawImage).texture;
                    }
                }
            }
            return null;
        }

        public static bool ReplaceStyleColor(ref Color32 p_target, Color32 p_oldColor, Color32 p_newColor)
        {
            if (p_target.r == p_oldColor.r &&
                p_target.g == p_oldColor.g &&
                p_target.b == p_oldColor.b)
            {
                p_target = new Color32(p_newColor.r, p_newColor.g, p_newColor.b, p_target.a);
                return true;
            }
            return false;
        }

        public static bool TryReplaceStyleResource(Component p_target, object p_oldResource, object p_newResource)
        {
            if (p_target != null)
            {
                var v_targetGraphic = p_target is Graphic? p_target as Graphic : p_target.GetComponent<Graphic>();
                if (v_targetGraphic != null)
                {
                    var v_targetResource = GetStyleResource(p_target);
                    if (v_targetResource != null && Object.Equals(v_targetResource, p_oldResource))
                    {
                        if (v_targetGraphic is IVectorImage)
                        {
                            (v_targetGraphic as IVectorImage).vectorImageData = p_newResource as VectorImageData;
                            return true;
                        }
                        else if (v_targetGraphic is Text)
                        {
                            (v_targetGraphic as Text).font = p_newResource as Font;
                            return true;
                        }
                        else if (v_targetGraphic is TMPro.TMP_Text)
                        {
                            (v_targetGraphic as TMPro.TMP_Text).font = p_newResource as TMPro.TMP_FontAsset;
                            return true;
                        }
                        else if (v_targetGraphic is Image)
                        {
                            (v_targetGraphic as Image).sprite = p_newResource as Sprite;
                            return true;
                        }
                        else if (v_targetGraphic is RawImage)
                        {
                            (v_targetGraphic as RawImage).texture = p_newResource as Texture2D;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Apply Target in Another

        public static void ApplyObjectActive(Object p_target, Object p_template)
        {
            ApplyObjectActive_Internal(p_target, p_template);
        }

        public static void ApplyStyleElement(Component p_target, Component p_template)
        {
            ApplyStyleElement_Internal(p_target, p_template);
        }

        public static void ApplyGraphic(Component p_target, Component p_template, bool p_applyResources = true)
        {
            ApplyGraphic_Internal(p_target, p_template, p_applyResources, p_applyResources);
        }

        public static void ApplyImgGraphic(Component p_target, Component p_template)
        {
            ApplyGraphic_Internal(p_target, p_template, true, false);
        }

        public static void ApplyTextGraphic(Component p_target, Component p_template)
        {
            ApplyGraphic_Internal(p_target, p_template, false, true);
        }

        static void ApplyStyleElement_Internal(Component p_target, Component p_template)
        {
            if (p_target != null && p_template != null)
            {
                var v_targetStyle = p_target is BaseStyleElement? p_target as BaseStyleElement : p_target.GetComponent<BaseStyleElement>();
                var v_templateStyle = p_template is BaseStyleElement ? p_template as BaseStyleElement : p_template.GetComponent<BaseStyleElement>();

                if (v_targetStyle != null && v_templateStyle != null)
                {
                    if (v_targetStyle.IsSupportedStyleElement(v_templateStyle))
                        v_targetStyle.StyleDataName = v_templateStyle.StyleDataName;
                }
            }
        }

        static void ApplyGraphic_Internal(Component p_target, Component p_template, bool p_supportImg, bool p_supportText)
        {
            if (p_target != null && p_template != null)
            {
                var v_targetGraphic = p_target is Graphic ? p_target as Graphic : p_target.GetComponent<Graphic>();
                var v_templateGraphic = p_template is Graphic ? p_template as Graphic : p_template.GetComponent<Graphic>();

                if (v_targetGraphic != null && v_templateGraphic != null)
                {
                    //Disable/Enable Graphic based in Template
                    //v_targetGraphic.enabled = v_templateGraphic.enabled;

                    var v_targetType = v_targetGraphic.GetType();
                    var v_templateType = v_templateGraphic.GetType();

                    //Clone Asset based in Graphic Type (Support Text / TMP_Text / Image / Raw Image / Vector Image)
                    if (v_targetType == v_templateType || v_targetType.IsSubclassOf(v_templateType) || v_templateType.IsSubclassOf(v_targetType))
                    {
                        v_targetGraphic.raycastTarget = v_templateGraphic.raycastTarget;
                        v_targetGraphic.color = v_templateGraphic.color;

                        if (p_supportText)
                        {
                            if (v_targetGraphic is Text && !(v_targetGraphic is IVectorImage))
                            {
                                var v_textTarget = v_targetGraphic as Text;
                                var v_textTemplate = v_templateGraphic as Text;

                                if (v_textTemplate != null)
                                {
                                    v_textTarget.font = v_textTemplate.font;
                                    v_textTarget.fontStyle = v_textTemplate.fontStyle;
                                    v_textTarget.supportRichText = v_textTemplate.supportRichText;
                                }
                            }
                            else if (v_targetGraphic is TMPro.TMP_Text && !(v_targetGraphic is IVectorImage))
                            {
                                var v_textTarget = v_targetGraphic as TMPro.TMP_Text;
                                var v_textTemplate = v_templateGraphic as TMPro.TMP_Text;

                                if (v_textTemplate != null)
                                {
                                    v_textTarget.font = v_textTemplate.font;
                                    v_textTarget.fontStyle = v_textTemplate.fontStyle;
                                    v_textTarget.richText = v_textTemplate.richText;
                                }
                            }
                        }
                        if (p_supportImg)
                        {
                            if (v_targetGraphic is IVectorImage)
                            {
                                var v_vectorTarget = v_targetGraphic as IVectorImage;
                                var v_vectorTemplate = v_templateGraphic as IVectorImage;

                                if (v_vectorTarget != null && v_vectorTarget.gameObject != null)
                                {
                                    v_vectorTarget.vectorImageData = v_vectorTemplate.vectorImageData;
                                    v_vectorTarget.sizeMode = v_vectorTemplate.sizeMode;
                                    v_vectorTarget.size = v_vectorTemplate.size;
                                    v_targetGraphic.material = v_templateGraphic.material;
                                }
                            }
                            else if (v_targetGraphic is Image)
                            {
                                var v_imageTarget = v_targetGraphic as Image;
                                var v_imageTemplate = v_templateGraphic as Image;

                                if (v_imageTarget != null)
                                {
                                    v_imageTarget.sprite = v_imageTemplate.sprite;
                                    v_imageTarget.pixelsPerUnitMultiplier = v_imageTemplate.pixelsPerUnitMultiplier;
                                    v_imageTarget.preserveAspect = v_imageTemplate.preserveAspect;
                                    v_imageTarget.type = v_imageTemplate.type;
                                    v_imageTarget.fillAmount = v_imageTemplate.fillAmount;
                                    v_imageTarget.fillCenter = v_imageTemplate.fillCenter;
                                    v_imageTarget.fillClockwise = v_imageTemplate.fillClockwise;
                                    v_imageTarget.fillMethod = v_imageTemplate.fillMethod;
                                    v_imageTarget.material = v_imageTemplate.material;
                                }
                            }
                            else if (v_targetGraphic is RawImage)
                            {
                                var v_rawTarget = v_targetGraphic as RawImage;
                                var v_rawTemplate = v_templateGraphic as RawImage;

                                if (v_rawTarget != null)
                                {
                                    v_rawTarget.texture = v_rawTemplate.texture;
                                    v_rawTarget.uvRect = v_rawTemplate.uvRect;
                                    v_rawTarget.material = v_rawTemplate.material;
                                }
                            }
                        }
                    }
                }
            }
        }

        static void ApplyObjectActive_Internal(Object p_target, Object p_template)
        {
            if (p_target != null && p_template != null)
            {
                if (p_target is GameObject && p_template is GameObject)
                {
                    var v_goTarget = p_target as GameObject;
                    var v_goTemplate = p_template as GameObject;

                    v_goTarget.SetActive(v_goTemplate.activeSelf);
                }

                else if (p_target is Behaviour && p_template is Behaviour)
                {
                    var v_behaviourTarget = p_target as Behaviour;
                    var v_behaviourTemplate = p_template as Behaviour;

                    //v_behaviourTarget.enabled = v_behaviourTemplate.enabled;
                    v_behaviourTarget.gameObject.SetActive(v_behaviourTemplate.gameObject.activeSelf);
                }

                else if (p_target is Component && p_template is Component)
                {
                    var v_componentTarget = p_target as Component;
                    var v_componentTemplate = p_template as Component;

                    v_componentTarget.gameObject.SetActive(v_componentTemplate.gameObject.activeSelf);
                }
            }
        }

        #endregion
    }
}
