//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Kyub.UI;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MaterialUI
{
    public static class GraphicExtensions
    {
        static TMPro.TextAlignmentOptions TextAnchorToTMPTextAlign(TextAnchor align)
        {
            if (align == TextAnchor.LowerCenter)
                return TMPro.TextAlignmentOptions.Bottom;
            else if (align == TextAnchor.LowerLeft)
                return TMPro.TextAlignmentOptions.BottomLeft;
            else if (align == TextAnchor.LowerRight)
                return TMPro.TextAlignmentOptions.BottomRight;
            if (align == TextAnchor.UpperCenter)
                return TMPro.TextAlignmentOptions.Top;
            else if (align == TextAnchor.UpperLeft)
                return TMPro.TextAlignmentOptions.TopLeft;
            else if (align == TextAnchor.UpperRight)
                return TMPro.TextAlignmentOptions.TopRight;
            else if (align == TextAnchor.MiddleLeft)
                return TMPro.TextAlignmentOptions.Left;
            else if (align == TextAnchor.UpperRight)
                return TMPro.TextAlignmentOptions.Right;

            return TMPro.TextAlignmentOptions.Center;
        }

        static TextAnchor TMPTextAlignToTextAnchor(TMPro.TextAlignmentOptions align)
        {
            if (align == TMPro.TextAlignmentOptions.Bottom)
                return TextAnchor.LowerCenter;
            else if (align == TMPro.TextAlignmentOptions.BottomLeft)
                return TextAnchor.LowerLeft;
            else if (align == TMPro.TextAlignmentOptions.BottomRight)
                return TextAnchor.LowerRight;
            if (align == TMPro.TextAlignmentOptions.Top)
                return TextAnchor.UpperCenter;
            else if (align == TMPro.TextAlignmentOptions.TopLeft)
                return TextAnchor.UpperLeft;
            else if (align == TMPro.TextAlignmentOptions.TopRight)
                return TextAnchor.UpperRight;
            else if (align == TMPro.TextAlignmentOptions.Left)
                return TextAnchor.MiddleLeft;
            else if (align == TMPro.TextAlignmentOptions.Right)
                return TextAnchor.MiddleRight;

            return TextAnchor.MiddleCenter;
        }

        static TMPro.FontStyles FontTypeToTMPFontType(FontStyle style)
        {
            if (style == FontStyle.Bold)
                return TMPro.FontStyles.Bold;
            else if (style == FontStyle.BoldAndItalic)
                return TMPro.FontStyles.Bold | TMPro.FontStyles.Italic;
            else if (style == FontStyle.Italic)
                return TMPro.FontStyles.Italic;
            return TMPro.FontStyles.Normal;
        }

        static FontStyle TMPFontStyleToFontStyle(TMPro.FontStyles tmpStyle)
        {
            if (tmpStyle == TMPro.FontStyles.Bold)
                return FontStyle.Bold;
            else if (tmpStyle == (TMPro.FontStyles.Bold | TMPro.FontStyles.Italic))
                return FontStyle.BoldAndItalic;
            else if (tmpStyle == TMPro.FontStyles.Italic)
                return FontStyle.Italic;
            return FontStyle.Normal;
        }

        public static void SetGraphicFontStyle(this Graphic textGraphic, FontStyle style)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                    ((TMPro.TMP_Text)textGraphic).fontStyle = FontTypeToTMPFontType(style);
                else if (textGraphic is Text)
                    ((Text)textGraphic).fontStyle = style;
            }
        }

        public static void SetGraphicText(this Graphic textGraphic, string text, params object[] textParameters)
        {
            if (textGraphic != null)
            {
                if (text == null)
                    text = "";
                try
                {
                    text = textParameters == null || textParameters.Length == 0 ? text : string.Format(text, textParameters);
                }
                catch { }
                if (textGraphic is TMPro.TMP_Text)
                {
                    var oldText = ((TMPro.TMP_Text)textGraphic).text;
                    if (oldText != text)
                    {
                        ((TMPro.TMP_Text)textGraphic).text = text;
                        ((TMPro.TMP_Text)textGraphic).SetAllDirty();
                    }
                }
                else if (textGraphic is Text)
                    ((Text)textGraphic).text = text;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.SetDirty(textGraphic);
#endif
            }
        }

        /*public static void SetGraphicTextLocalized(this Graphic textGraphic, string text, params object[] parameters)
        {
            //Localize all parameters
            string[] localizedParameters = parameters != null && parameters.Length > 0? new string[parameters.Length] : null;
            if (localizedParameters != null)
            {
                for (int i = 0; i < localizedParameters.Length; i++)
                {
                    var paramAsString = parameters[i] is string ? 
                        parameters[i] as string : 
                        (parameters[i] == null? "" : parameters[i].ToString());

                    localizedParameters[i] = string.IsNullOrEmpty(paramAsString)? paramAsString : Kyub.Localization.LocaleManager.GetLocalizedText(paramAsString);
                }
            }
            SetGraphicText(textGraphic, string.IsNullOrEmpty(text)? text : Kyub.Localization.LocaleManager.GetLocalizedText(text), localizedParameters);
        }*/

        public static string GetGraphicText(this Graphic textGraphic)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                    return ((TMPro.TMP_Text)textGraphic).text;
                else if (textGraphic is Text)
                    return ((Text)textGraphic).text;
            }

            return null;
        }

        public static FontStyle GetGraphicFontStyle(this Graphic textGraphic)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                    return TMPFontStyleToFontStyle(((TMPro.TMP_Text)textGraphic).fontStyle);
                else if (textGraphic is Text)
                    return ((Text)textGraphic).fontStyle;
            }

            return FontStyle.Normal;
        }

        public static TextAnchor GetGraphicTextAnchor(this Graphic textGraphic)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                    return TMPTextAlignToTextAnchor(((TMPro.TMP_Text)textGraphic).alignment);
                else if (textGraphic is Text)
                    return ((Text)textGraphic).alignment;
            }

            return TextAnchor.MiddleCenter;
        }

        public static float GetGraphicFontSize(this Graphic textGraphic)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                    return ((TMPro.TMP_Text)textGraphic).fontSize;
                else if (textGraphic is Text)
                    return ((Text)textGraphic).fontSize;
            }
            return 0;
        }

        public static void SetGraphicFontSize(this Graphic textGraphic, float size)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                    ((TMPro.TMP_Text)textGraphic).fontSize = size;
                else if (textGraphic is Text)
                    ((Text)textGraphic).fontSize = (int)size;
            }
        }

        public static float GetGraphicFontAssetFontSize(this Graphic textGraphic)
        {
            if (textGraphic != null)
            {
                if (textGraphic is TMPro.TMP_Text)
                {
#if UNITY_2018_3_OR_NEWER
                    return ((TMPro.TMP_Text)textGraphic).font.faceInfo.pointSize;
#else
                    return ((TMPro.TMP_Text)textGraphic).font.fontInfo.PointSize;
#endif
                }
                else if (textGraphic is Text)
                    return ((Text)textGraphic).font.fontSize;
            }
            return 0;
        }
    }

    /// <summary>
    /// Static class with <see cref="Func{T}"/> extension methods.
    /// </summary>
    public static class FuncExtension
    {
        /// <summary>
        /// Invokes a Func if not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func">The function to invoke.</param>
        /// <returns></returns>
        public static T InvokeIfNotNull<T>(this Func<T> func)
        {
            if (func != null)
            {
                return func();
            }

            return default(T);
        }
    }

    /// <summary>
    /// Static class with <see cref="Transform"/> extension methods.
    /// </summary>
    public static class TransformExtension
    {
        /// <summary>
        /// Sets the parent and scale of a Transform.
        /// </summary>
        /// <param name="transform">The transform to modify.</param>
        /// <param name="parent">The new parent to set.</param>
        /// <param name="localScale">The local scale to set.</param>
        /// <param name="worldPositionStays">if set to <c>true</c> [world position stays].</param>
        public static void SetParentAndScale(this Transform transform, Transform parent, Vector3 localScale, bool worldPositionStays = false)
        {
            transform.SetParent(parent, worldPositionStays);
            transform.localScale = localScale;
        }

        /// <summary>
        /// Gets the root canvas from a transform.
        /// </summary>
        /// <param name="transform">The transform to use.</param>
        /// <returns>Returns root canvas if one found, otherwise returns null.</returns>
        public static Canvas GetRootCanvas(this Transform transform)
        {
            if (transform == null)
            {
                return null;
            }

            Canvas[] parentCanvases = transform.GetComponentsInParent<Canvas>();

            if (parentCanvases == null || parentCanvases.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < parentCanvases.Length; i++)
            {
                Canvas canvas = parentCanvases[i];
                if (canvas.isRootCanvas)
                {
                    return canvas;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Static class with <see cref="Canvas"/> extension methods.
    /// </summary>
    public static class CanvasExtension
    {
        /// <summary>
        /// Copies a Canvas to another GameObject.
        /// </summary>
        /// <param name="canvas">The canvas to copy.</param>
        /// <param name="gameObjectToAddTo">The game object to add the new Canvas to.</param>
        /// <returns>The new Canvas instance.</returns>
        public static Canvas Copy(this Canvas canvas, GameObject gameObjectToAddTo)
        {
            Canvas dupCanvas = gameObjectToAddTo.GetAddComponent<Canvas>();

            RectTransform mainCanvasRectTransform = canvas.GetComponent<RectTransform>();
            RectTransform dropdownCanvasRectTransform = dupCanvas.GetComponent<RectTransform>();

            dropdownCanvasRectTransform.position = mainCanvasRectTransform.position;
            dropdownCanvasRectTransform.sizeDelta = mainCanvasRectTransform.sizeDelta;
            dropdownCanvasRectTransform.anchorMin = mainCanvasRectTransform.anchorMin;
            dropdownCanvasRectTransform.anchorMax = mainCanvasRectTransform.anchorMax;
            dropdownCanvasRectTransform.pivot = mainCanvasRectTransform.pivot;
            dropdownCanvasRectTransform.rotation = mainCanvasRectTransform.rotation;
            dropdownCanvasRectTransform.localScale = mainCanvasRectTransform.localScale;

            dupCanvas.gameObject.GetAddComponent<GraphicRaycaster>();
            CanvasScaler mainScaler = canvas.GetComponent<CanvasScaler>();
            if (mainScaler != null)
            {
                CanvasScaler scaler = dupCanvas.gameObject.GetAddComponent<MaterialCanvasScaler>();
                scaler.uiScaleMode = mainScaler.uiScaleMode;
                scaler.referenceResolution = mainScaler.referenceResolution;
                scaler.screenMatchMode = mainScaler.screenMatchMode;
                scaler.matchWidthOrHeight = mainScaler.matchWidthOrHeight;
                scaler.referencePixelsPerUnit = mainScaler.referencePixelsPerUnit;
            }
            MaterialCanvasScaler mainMaterialScaler = mainScaler as MaterialCanvasScaler;
            if (mainMaterialScaler != null)
            {
                MaterialCanvasScaler materialScaler = dupCanvas.gameObject.GetAddComponent<MaterialCanvasScaler>();
                materialScaler.useLegacyPhysicalSize = mainMaterialScaler.useLegacyPhysicalSize;
                materialScaler.supportSafeArea = mainMaterialScaler.supportSafeArea;
            }

            Kyub.Performance.SustainedCanvasView mainSustainedCanvasView = canvas.GetComponent<Kyub.Performance.SustainedCanvasView>();
            if (mainSustainedCanvasView)
            {
                Kyub.Performance.SustainedCanvasView sustainedCanvasView = dupCanvas.gameObject.GetAddComponent<Kyub.Performance.SustainedCanvasView>();
                sustainedCanvasView.RequiresConstantRepaint = mainSustainedCanvasView.RequiresConstantRepaint;
                sustainedCanvasView.UseRenderBuffer = mainSustainedCanvasView.UseRenderBuffer;
                sustainedCanvasView.MinimumSupportedFps = mainSustainedCanvasView.MinimumSupportedFps;
            }

            dupCanvas.renderMode = canvas.renderMode;

            return dupCanvas;
        }
        /// <summary>
        /// Copies the settings to other canvas.
        /// </summary>
        /// <param name="canvas">The canvas to copy from.</param>
        /// <param name="otherCanvas">The canvas to copy to.</param>
        public static void CopySettingsToOtherCanvas(this Canvas canvas, Canvas otherCanvas)
        {
            RectTransform mainCanvasRectTransform = canvas.GetComponent<RectTransform>();
            RectTransform dropdownCanvasRectTransform = otherCanvas.GetComponent<RectTransform>();

            dropdownCanvasRectTransform.position = mainCanvasRectTransform.position;
            dropdownCanvasRectTransform.sizeDelta = mainCanvasRectTransform.sizeDelta;
            dropdownCanvasRectTransform.anchorMin = mainCanvasRectTransform.anchorMin;
            dropdownCanvasRectTransform.anchorMax = mainCanvasRectTransform.anchorMax;
            dropdownCanvasRectTransform.pivot = mainCanvasRectTransform.pivot;
            dropdownCanvasRectTransform.rotation = mainCanvasRectTransform.rotation;
            dropdownCanvasRectTransform.localScale = mainCanvasRectTransform.localScale;

            otherCanvas.gameObject.GetAddComponent<GraphicRaycaster>();
            CanvasScaler mainScaler = canvas.GetComponent<CanvasScaler>();
            if (mainScaler != null)
            {
                CanvasScaler scaler = otherCanvas.gameObject.GetAddComponent<MaterialCanvasScaler>();
                scaler.uiScaleMode = mainScaler.uiScaleMode;
                scaler.referenceResolution = mainScaler.referenceResolution;
                scaler.screenMatchMode = mainScaler.screenMatchMode;
                scaler.matchWidthOrHeight = mainScaler.matchWidthOrHeight;
                scaler.referencePixelsPerUnit = mainScaler.referencePixelsPerUnit;
                scaler.scaleFactor = mainScaler.scaleFactor;
                otherCanvas.scaleFactor = scaler.scaleFactor;
            }
            MaterialCanvasScaler mainMaterialScaler = mainScaler as MaterialCanvasScaler;
            if (mainMaterialScaler != null)
            {
                MaterialCanvasScaler materialScaler = otherCanvas.gameObject.GetAddComponent<MaterialCanvasScaler>();
                materialScaler.useLegacyPhysicalSize = mainMaterialScaler.useLegacyPhysicalSize;
                materialScaler.supportSafeArea = mainMaterialScaler.supportSafeArea;
            }
            otherCanvas.renderMode = canvas.renderMode;
            otherCanvas.targetDisplay = canvas.targetDisplay;
            otherCanvas.worldCamera = canvas.worldCamera;
            otherCanvas.planeDistance = canvas.planeDistance;
        }
    }

    /// <summary>
    /// Static class with <see cref="Action"/> extension methods.
    /// </summary>
    public static class ActionExtension
    {
        /// <summary>
        /// Invokes an <see cref="Action"/> if not null.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        public static void InvokeIfNotNull(this Action action)
        {
            if (action != null)
            {
                action.Invoke();
            }
        }

        /// <summary>
        /// Invokes an <see cref="Action{T}"/> if not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action">The action.</param>
        /// <param name="parameter">The parameter.</param>
        public static void InvokeIfNotNull<T>(this Action<T> action, T parameter)
        {
            if (action != null)
            {
                action.Invoke(parameter);
            }
        }
    }

    /// <summary>
    /// Static class with <see cref="UnityEvent"/> extension methods.
    /// </summary>
    public static class UnityEventExtension
    {
        /// <summary>
        /// Invokes a <see cref="UnityEvent"/> if not null.
        /// </summary>
        /// <param name="unityEvent">The UnityEvent to invoke.</param>
        public static void InvokeIfNotNull(this UnityEvent unityEvent)
        {
            if (unityEvent != null)
            {
                unityEvent.Invoke();
            }
        }

        /// <summary>
        /// Invokes a <see cref="UnityEvent{T}"/> if not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="unityEvent">The UnityEvent to invoke.</param>
        /// <param name="parameter">The argument used in the invocation.</param>
        public static void InvokeIfNotNull<T>(this UnityEvent<T> unityEvent, T parameter)
        {
            if (unityEvent != null)
            {
                unityEvent.Invoke(parameter);
            }
        }
    }

    /// <summary>
    /// Static class with <see cref="GameObject"/> extension methods.
    /// </summary>
    public static class GameObjectExtension
    {
        /// <summary>
        /// Gets a Component on a GameObject if it exists, otherwise add one.
        /// </summary>
        /// <typeparam name="T">The type of Component to add.</typeparam>
        /// <param name="gameObject">The game object to check/add to.</param>
        /// <returns>The Component instance.</returns>
        public static T GetAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.GetComponent<T>() != null)
            {
                return gameObject.GetComponent<T>();
            }
            else
            {
                return gameObject.AddComponent<T>();
            }

        }

        /// <summary>
        /// Gets a child Component by name and type.
        /// </summary>
        /// <typeparam name="T">The type of Component.</typeparam>
        /// <param name="gameObject">The game object.</param>
        /// <param name="name">The name to search.</param>
        /// <returns>The Component found, otherwise null.</returns>
        public static T GetChildByName<T>(this GameObject gameObject, string name)
        {
            T[] items = gameObject.GetComponentsInChildren<T>(true);

            for (int i = 0; i < items.Length; i++)
            {
                Component component = items[i] as Component;
                if (component != null && component.name == name)
                {
                    return items[i];
                }
            }

            return default(T);
        }

#if UNITY_EDITOR
        public static bool IsPrefabInstance(this GameObject gameObject)
        {
#if UNITY_2018_3_OR_NEWER
            var status = PrefabUtility.GetPrefabInstanceStatus(gameObject);
            bool prefab = status == PrefabInstanceStatus.Connected || status == PrefabInstanceStatus.Disconnected;
#else
            bool prefab =  PrefabUtility.GetPrefabParent(gameObject) != null || PrefabUtility.GetPrefabObject(gameObject) != null;
#endif
            return prefab;
        }
#endif
    }

    /// <summary>
    /// Static class with <see cref="MonoBehaviour"/> extension methods.
    /// </summary>
    public static class MonoBehaviourExtension
    {
        /// <summary>
        /// Gets a Component on a GameObject if it exists, otherwise add one.
        /// </summary>
        /// <typeparam name="T">The type of Component to add.</typeparam>
        /// <returns>The Component instance.</returns>
        public static T GetAddComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
        {
            if (monoBehaviour.GetComponent<T>() != null)
            {
                return monoBehaviour.GetComponent<T>();
            }

            return monoBehaviour.gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Gets a child Component by name and type.
        /// </summary>
        /// <typeparam name="T">The type of Component.</typeparam>
        /// <param name="monoBehaviour">The MonoBehaviour.</param>
        /// <param name="name">The name to search.</param>
        /// <returns>The Component found, otherwise null.</returns>
        public static T GetChildByName<T>(this MonoBehaviour monoBehaviour, string name)
        {
            return monoBehaviour.gameObject.GetChildByName<T>(name);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public static class ComponentExtension
    {
        /// <summary>
        /// Gets the name of the child by.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="component">The component.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static T GetChildByName<T>(this Component component, string name)
        {
            return component.gameObject.GetChildByName<T>(name);
        }
    }

    /// <summary>
    /// Static class with <see cref="Color"/> extension methods.
    /// </summary>
    public static class ColorExtension
    {
        /// <summary>
        /// Gets a color with a specified alpha level.
        /// </summary>
        /// <param name="color">The color to get.</param>
        /// <param name="alpha">The desired alpha level.</param>
        /// <returns>A Color with 'rgb' values from color argument, and 'a' value from alpha argument.</returns>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// Uses <see cref="Mathf.Approximately"/> on the color level values of two colors to compare them.
        /// </summary>
        /// <param name="thisColor">The first Color to compare.</param>
        /// <param name="otherColor">The second Color to compare.</param>
        /// <param name="compareAlpha">Should the alpha levels also be compared?</param>
        /// <returns>True if the first Color is approximately the second Color, otherwise false.</returns>
        public static bool Approximately(this Color thisColor, Color otherColor, bool compareAlpha = false)
        {
            if (!Mathf.Approximately(thisColor.r, otherColor.r)) return false;
            if (!Mathf.Approximately(thisColor.g, otherColor.g)) return false;
            if (!Mathf.Approximately(thisColor.b, otherColor.b)) return false;
            if (!compareAlpha) return true;
            return Mathf.Approximately(thisColor.a, otherColor.a);
        }
    }

    /// <summary>
    /// Static class with <see cref="RectTransform"/> extension methods.
    /// </summary>
    public static class RectTransformExtension
    {
        /// <summary>Sometimes sizeDelta works, sometimes rect works, sometimes neither work and you need to get the layout properties.
        ///	This method provides a simple way to get the size of a RectTransform, no matter what's driving it or what the anchor values are.
        /// </summary>
        /// <param name="rectTransform">The rect transform to check.</param>
        /// <returns>The proper size of the RectTransform.</returns>
        public static Vector2 GetProperSize(this RectTransform rectTransform) //, bool attemptToRefreshLayout = false)
        {
            Vector2 size = new Vector2(rectTransform.rect.width, rectTransform.rect.height);

            if (size.x == 0 && size.y == 0)
            {
                LayoutElement layoutElement = rectTransform.GetComponent<LayoutElement>();

                if (layoutElement != null)
                {
                    size.x = layoutElement.preferredWidth;
                    size.y = layoutElement.preferredHeight;
                }
            }
            if (size.x == 0 && size.y == 0)
            {
                LayoutGroup layoutGroup = rectTransform.GetComponent<LayoutGroup>();

                if (layoutGroup != null)
                {
                    size.x = layoutGroup.preferredWidth;
                    size.y = layoutGroup.preferredHeight;
                }
            }

            if (size.x == 0 && size.y == 0)
            {
                size.x = LayoutUtility.GetPreferredWidth(rectTransform);
                size.y = LayoutUtility.GetPreferredHeight(rectTransform);
            }

            return size;
        }

        /// <summary>
        /// Gets the position regardless of pivot.
        /// </summary>
        /// <param name="rectTransform">The rect transform.</param>
        /// <returns>The position in world space.</returns>
        public static Vector3 GetPositionRegardlessOfPivot(this RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return (corners[0] + corners[2]) / 2;
        }

        /// <summary>
        /// Gets the local position regardless of pivot.
        /// </summary>
        /// <param name="rectTransform">The rect transform.</param>
        /// <returns>The position in local space.</returns>
        public static Vector3 GetLocalPositionRegardlessOfPivot(this RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetLocalCorners(corners);
            return (corners[0] + corners[2]) / 2;
        }

        /// <summary>
        /// Sets the x value of a RectTransform's anchor.
        /// </summary>
        /// <param name="rectTransform">The rect transform.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        public static void SetAnchorX(this RectTransform rectTransform, float min, float max)
        {
            rectTransform.anchorMin = new Vector2(min, rectTransform.anchorMin.y);
            rectTransform.anchorMax = new Vector2(max, rectTransform.anchorMax.y);
        }

        /// <summary>
        /// Sets the y value of a RectTransform's anchor
        /// </summary>
        /// <param name="rectTransform">The rect transform.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        public static void SetAnchorY(this RectTransform rectTransform, float min, float max)
        {
            rectTransform.anchorMin = new Vector2(rectTransform.anchorMin.x, min);
            rectTransform.anchorMax = new Vector2(rectTransform.anchorMax.x, max);
        }

        /// <summary>
        /// Gets the root canvas of a RectTransform.
        /// </summary>
        /// <param name="rectTransform">The rect transform to get the root canvas of.</param>
        //public static Canvas GetRootCanvas(this RectTransform rectTransform)
        //{
        //    Canvas[] parentCanvases = rectTransform.GetComponentsInParent<Canvas>();

        //    for (int i = 0; i < parentCanvases.Length; i++)
        //    {
        //        Canvas canvas = parentCanvases[i];
        //        if (canvas.isRootCanvas)
        //        {
        //            return canvas;
        //        }
        //    }

        //    return null;
        //}
    }

    /// <summary>
    /// Static class with <see cref="Graphic"/> extension methods.
    /// </summary>
    public static class GraphicExtension
    {
        /// <summary>
        /// Determines whether a Graphic is of type Image or VectorImage.
        /// </summary>
        /// <param name="graphic">The graphic to check.</param>
        /// <returns>True if the Graphic is of type Image or VectorImage, otherwise false.</returns>
        public static bool IsSpriteOrVectorImage(this Graphic graphic)
        {
            return (graphic is Image || graphic is IVectorImage);
        }

        /// <summary>
        /// Sets the image of a Graphic (must be of type Image).
        /// </summary>
        /// <param name="graphic">The graphic to modify.</param>
        /// <param name="sprite">The sprite to set.</param>
        public static void SetImageData(this Graphic graphic, Sprite sprite)
        {
            Image imageToSet = graphic as Image;

            if (imageToSet != null)
            {
                imageToSet.sprite = sprite;
            }
        }
        /// <summary>
        /// Sets the image of a Graphic (must be of type VectorImage).
        /// </summary>
        /// <param name="graphic">The graphic to modify.</param>
        /// <param name="vectorImageData">The vector image data to set.</param>
        public static void SetImageData(this Graphic graphic, VectorImageData vectorImageData)
        {
            IVectorImage imageToSet = graphic as IVectorImage;

            if (imageToSet != null)
            {
                imageToSet.vectorImageData = vectorImageData;
            }
        }
        /// <summary>
        /// Sets the image of a Graphic (must be of type Image if imageData has type Sprite, or VectorImage if imageData has type VectorImageData).
        /// </summary>
        /// <param name="graphic">The graphic to modify.</param>
        /// <param name="imageData">The image data to set.</param>
        public static void SetImageData(this Graphic graphic, ImageData imageData)
        {
            IVectorImage vectorImage = graphic as IVectorImage;

            if (vectorImage != null)
            {
                if (imageData != null && imageData.imageDataType == ImageDataType.VectorImage)
                    vectorImage.vectorImageData = imageData.vectorImageData;
                else
                    vectorImage.vectorImageData = null;
                return;
            }

            Image spriteImage = graphic as Image;

            if (spriteImage != null)
            {
                if (imageData.imageDataType == ImageDataType.Sprite)
                    spriteImage.sprite = imageData.sprite;
                else
                    spriteImage.sprite = null;
            }

            ExternalImage externalImage = graphic != null ? graphic.GetComponent<ExternalImage>() : null;
            if (externalImage != null)
            {
                if (imageData.imageDataType == ImageDataType.Sprite)
                {
                    externalImage.Key = imageData.imgUrl;
                    externalImage.DefaultSprite = imageData.sprite;
                }
                else
                {
                    externalImage.Key = "";
                    externalImage.DefaultSprite = null;
                }
            }
        }

        /// <summary>
        /// Gets the sprite image.
        /// </summary>
        /// <param name="graphic">The graphic to check.</param>
        /// <returns>The Sprite of the Graphic, if applicable and one exists.</returns>
        public static Sprite GetSpriteImage(this Graphic graphic)
        {
            Image imageToGet = graphic as Image;

            if (imageToGet != null)
            {
                return imageToGet.sprite;
            }

            return null;
        }

        /// <summary>
        /// Gets the vector image.
        /// </summary>
        /// <param name="graphic">The graphic to check.</param>
        /// <returns>The VectorImageData of the Graphic, if applicable and one exists.</returns>
        public static VectorImageData GetVectorImage(this Graphic graphic)
        {
            IVectorImage imageToGet = graphic as IVectorImage;

            if (imageToGet != null)
            {
                return imageToGet.vectorImageData;
            }

            return null;
        }

        /// <summary>
        /// Gets the image data.
        /// </summary>
        /// <param name="graphic">The graphic to check.</param>
        /// <returns>The ImageData, if applicable and one exists.</returns>
        public static ImageData GetImageData(this Graphic graphic)
        {
            Sprite sprite = graphic.GetSpriteImage();

            ExternalImage externalImage = graphic != null ? graphic.GetComponent<ExternalImage>() : null;
            var key = externalImage != null ? externalImage.Key : null;
            sprite = externalImage != null && externalImage.DefaultSprite != null ? externalImage.DefaultSprite : sprite;

            if (!string.IsNullOrEmpty(key))
            {
                return new ImageData(key, sprite);
            }
            else if (sprite != null)
            {
                return new ImageData(sprite);
            }

            VectorImageData vectorImageData = graphic.GetVectorImage();

            if (vectorImageData != null)
            {
                return new ImageData(vectorImageData);
            }

            return null;
        }
    }
}