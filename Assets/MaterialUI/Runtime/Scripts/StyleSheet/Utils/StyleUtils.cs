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

        public static Color32 GetStyleColor(Color32 color)
        {
            var colorNoAlpha = new Color32(color.r, color.g, color.b, byte.MaxValue);
            return colorNoAlpha;
        }

        public static object GetStyleResource(Component target)
        {
            if (target != null)
            {
                var targetGraphic = target is Graphic ? target as Graphic : target.GetComponent<Graphic>();
                if (targetGraphic != null)
                {
                    if (targetGraphic is IVectorImage)
                    {
                        return (targetGraphic as IVectorImage).vectorImageData;
                    }
                    else if (targetGraphic is Text)
                    {
                        return (targetGraphic as Text).font;
                    }
                    else if (targetGraphic is TMPro.TMP_Text)
                    {
                        return (targetGraphic as TMPro.TMP_Text).font;
                    }
                    else if (targetGraphic is Image)
                    {
                        return (targetGraphic as Image).sprite;
                    }
                    else if (targetGraphic is RawImage)
                    {
                        return (targetGraphic as RawImage).texture;
                    }
                }
            }
            return null;
        }

        public static bool ReplaceStyleColor(ref Color32 target, Color32 oldColor, Color32 newColor)
        {
            if (target.r == oldColor.r &&
                target.g == oldColor.g &&
                target.b == oldColor.b)
            {
                target = new Color32(newColor.r, newColor.g, newColor.b, target.a);
                return true;
            }
            return false;
        }

        public static bool TryReplaceStyleResource(Component target, object oldResource, object newResource)
        {
            if (target != null)
            {
                var targetGraphic = target is Graphic? target as Graphic : target.GetComponent<Graphic>();
                if (targetGraphic != null)
                {
                    var targetResource = GetStyleResource(target);
                    if (targetResource != null && Object.Equals(targetResource, oldResource))
                    {
                        if (targetGraphic is IVectorImage)
                        {
                            (targetGraphic as IVectorImage).vectorImageData = newResource as VectorImageData;
                            return true;
                        }
                        else if (targetGraphic is Text)
                        {
                            (targetGraphic as Text).font = newResource as Font;
                            return true;
                        }
                        else if (targetGraphic is TMPro.TMP_Text)
                        {
                            (targetGraphic as TMPro.TMP_Text).font = newResource as TMPro.TMP_FontAsset;
                            return true;
                        }
                        else if (targetGraphic is Image)
                        {
                            (targetGraphic as Image).sprite = newResource as Sprite;
                            return true;
                        }
                        else if (targetGraphic is RawImage)
                        {
                            (targetGraphic as RawImage).texture = newResource as Texture2D;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        #endregion

        #region Apply Target in Another

        public static void ApplyObjectActive(Object target, Object template)
        {
            ApplyObjectActive_Internal(target, template);
        }

        public static void ApplyStyleElement(Component target, Component template)
        {
            ApplyStyleElement_Internal(target, template);
        }

        public static void ApplyGraphicData(Component target, object data)
        {
            ApplyGraphicData_Internal(target, data, true, true);
        }

        public static void ApplyImgData(Component target, object data)
        {
            ApplyGraphicData_Internal(target, data, true, false);
        }

        public static void ApplyTextData(Component target, object data)
        {
            ApplyGraphicData_Internal(target, data, false, true);
        }

        public static object GetGraphicData(Component target)
        {
            return GetGraphicData_Internal(target, true, true);
        }

        public static object GetImgData(Component target)
        {
            return GetGraphicData_Internal(target, true, false);
        }

        public static object GetTextData(Component target)
        {
            return GetGraphicData_Internal(target, false, true);
        }

        public static void ApplyGraphic(Component target, Component template, bool applyResources = true)
        {
            ApplyGraphic_Internal(target, template, applyResources, applyResources);
        }

        public static void ApplyImgGraphic(Component target, Component template)
        {
            ApplyGraphic_Internal(target, template, true, false);
        }

        public static void ApplyTextGraphic(Component target, Component template)
        {
            ApplyGraphic_Internal(target, template, false, true);
        }

        static void ApplyStyleElement_Internal(Component target, Component template)
        {
            if (target != null && template != null)
            {
                var targetStyle = target is BaseStyleElement? target as BaseStyleElement : target.GetComponent<BaseStyleElement>();
                var templateStyle = template is BaseStyleElement ? template as BaseStyleElement : template.GetComponent<BaseStyleElement>();

                if (targetStyle != null && templateStyle != null)
                {
                    if (targetStyle.IsSupportedStyleElement(templateStyle))
                        targetStyle.StyleDataName = templateStyle.StyleDataName;
                }
            }
        }
        static void ApplyGraphicData_Internal(Component target, object data, bool supportImg, bool supportText)
        {
            if (target != null)
            {
                var targetGraphic = target is Graphic ? target as Graphic : target.GetComponent<Graphic>();

                if (targetGraphic != null)
                {
                    if (supportText)
                    {
                        if (targetGraphic is Text && !(targetGraphic is IVectorImage))
                        {
                            var textTarget = targetGraphic as Text;
                            if(data is string || data == null)
                                textTarget.SetGraphicText(data as string);
                        }
                        else if (targetGraphic is TMPro.TMP_Text && !(targetGraphic is IVectorImage))
                        {
                            var textTarget = targetGraphic as TMPro.TMP_Text;
                            if (data is string || data == null)
                                textTarget.SetGraphicText(data as string);
                        }
                    }
                    if (supportImg)
                    {
                        if (targetGraphic is IVectorImage)
                        {
                            var vectorTarget = targetGraphic as IVectorImage;
                            if (data is VectorImageData || data == null)
                                vectorTarget.vectorImageData = data as VectorImageData;
                        }
                        else if (targetGraphic is Image)
                        {
                            var imageTarget = targetGraphic as Image;
                            if (data is Sprite || data == null)
                                imageTarget.sprite = data as Sprite;
                        }
                        else if (targetGraphic is RawImage)
                        {
                            var rawTarget = targetGraphic as RawImage;
                            if (data is Texture || data == null)
                                rawTarget.texture = data as Texture;
                        }
                    }
                }
            }
        }

        static object GetGraphicData_Internal(Component target, bool supportImg, bool supportText)
        {
            if (target != null)
            {
                var targetGraphic = target is Graphic ? target as Graphic : target.GetComponent<Graphic>();

                if (targetGraphic != null)
                {
                    if (supportText)
                    {
                        if (targetGraphic is Text && !(targetGraphic is IVectorImage))
                        {
                            var textTarget = targetGraphic as Text;
                            return textTarget.GetGraphicText();
                        }
                        else if (targetGraphic is TMPro.TMP_Text && !(targetGraphic is IVectorImage))
                        {
                            var textTarget = targetGraphic as TMPro.TMP_Text;
                            return textTarget.GetGraphicText();
                        }
                    }
                    if (supportImg)
                    {
                        if (targetGraphic is IVectorImage)
                        {
                            var vectorTarget = targetGraphic as IVectorImage;
                            return vectorTarget.vectorImageData;
                        }
                        else if (targetGraphic is Image)
                        {
                            var imageTarget = targetGraphic as Image;
                            return imageTarget.sprite;
                        }
                        else if (targetGraphic is RawImage)
                        {
                            var rawTarget = targetGraphic as RawImage;
                            return rawTarget.texture;
                        }
                    }
                }
            }
            return null;
        }

        static void ApplyGraphic_Internal(Component target, Component template, bool supportImg, bool supportText)
        {
            if (target != null && template != null)
            {
                var targetGraphic = target is Graphic ? target as Graphic : target.GetComponent<Graphic>();
                var templateGraphic = template is Graphic ? template as Graphic : template.GetComponent<Graphic>();

                if (targetGraphic != null && templateGraphic != null)
                {
                    //Disable/Enable Graphic based in Template
                    //targetGraphic.enabled = templateGraphic.enabled;

                    var targetType = targetGraphic.GetType();
                    var templateType = templateGraphic.GetType();

                    //Clone Asset based in Graphic Type (Support Text / TMP_Text / Image / Raw Image / Vector Image)
                    if (targetType == templateType || targetType.IsSubclassOf(templateType) || templateType.IsSubclassOf(targetType))
                    {
                        targetGraphic.raycastTarget = templateGraphic.raycastTarget;
                        targetGraphic.color = templateGraphic.color;

                        if (supportText)
                        {
                            if (targetGraphic is Text && !(targetGraphic is IVectorImage))
                            {
                                var textTarget = targetGraphic as Text;
                                var textTemplate = templateGraphic as Text;

                                if (textTemplate != null)
                                {
                                    textTarget.font = textTemplate.font;
                                    textTarget.fontStyle = textTemplate.fontStyle;
                                    textTarget.supportRichText = textTemplate.supportRichText;
                                }
                            }
                            else if (targetGraphic is TMPro.TMP_Text && !(targetGraphic is IVectorImage))
                            {
                                var textTarget = targetGraphic as TMPro.TMP_Text;
                                var textTemplate = templateGraphic as TMPro.TMP_Text;

                                if (textTemplate != null)
                                {
                                    textTarget.font = textTemplate.font;
                                    textTarget.fontStyle = textTemplate.fontStyle;
                                    textTarget.richText = textTemplate.richText;
                                }
                            }
                        }
                        if (supportImg)
                        {
                            if (targetGraphic is IVectorImage)
                            {
                                var vectorTarget = targetGraphic as IVectorImage;
                                var vectorTemplate = templateGraphic as IVectorImage;

                                if (vectorTarget != null && vectorTarget.gameObject != null)
                                {
                                    vectorTarget.vectorImageData = vectorTemplate.vectorImageData;
                                    vectorTarget.sizeMode = vectorTemplate.sizeMode;
                                    vectorTarget.size = vectorTemplate.size;
                                    targetGraphic.material = templateGraphic.material;
                                }
                            }
                            else if (targetGraphic is Image)
                            {
                                var imageTarget = targetGraphic as Image;
                                var imageTemplate = templateGraphic as Image;

                                if (imageTarget != null)
                                {
                                    imageTarget.sprite = imageTemplate.sprite;
                                    imageTarget.pixelsPerUnitMultiplier = imageTemplate.pixelsPerUnitMultiplier;
                                    imageTarget.preserveAspect = imageTemplate.preserveAspect;
                                    imageTarget.type = imageTemplate.type;
                                    imageTarget.fillAmount = imageTemplate.fillAmount;
                                    imageTarget.fillCenter = imageTemplate.fillCenter;
                                    imageTarget.fillClockwise = imageTemplate.fillClockwise;
                                    imageTarget.fillMethod = imageTemplate.fillMethod;
                                    imageTarget.material = imageTemplate.material;
                                }
                            }
                            else if (targetGraphic is RawImage)
                            {
                                var rawTarget = targetGraphic as RawImage;
                                var rawTemplate = templateGraphic as RawImage;

                                if (rawTarget != null)
                                {
                                    rawTarget.texture = rawTemplate.texture;
                                    rawTarget.uvRect = rawTemplate.uvRect;
                                    rawTarget.material = rawTemplate.material;
                                }
                            }
                        }
                    }
                }
            }
        }

        static void ApplyObjectActive_Internal(Object target, Object template)
        {
            if (target != null && template != null)
            {
                if (target is GameObject && template is GameObject)
                {
                    var goTarget = target as GameObject;
                    var goTemplate = template as GameObject;

                    goTarget.SetActive(goTemplate.activeSelf);
                }

                else if (target is Behaviour && template is Behaviour)
                {
                    var behaviourTarget = target as Behaviour;
                    var behaviourTemplate = template as Behaviour;

                    //behaviourTarget.enabled = behaviourTemplate.enabled;
                    behaviourTarget.gameObject.SetActive(behaviourTemplate.gameObject.activeSelf);
                }

                else if (target is Component && template is Component)
                {
                    var componentTarget = target as Component;
                    var componentTemplate = template as Component;

                    componentTarget.gameObject.SetActive(componentTemplate.gameObject.activeSelf);
                }
            }
        }

        #endregion
    }
}
