using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using Kyub.Extensions;

namespace Kyub.UI
{
    // Image according to the label inside the name attribute to load, read from the Resources directory. The size of the image is controlled by the size property.
    // Use: <quad name=NAME size=25 width=1 />
    [AddComponentMenu("Kyub/UI/HtmlText")]
    [ExecuteInEditMode] // Needed for culling images that are not used //
    public class HtmlText : Text, IPointerClickHandler, IPointerExitHandler, IPointerEnterHandler, ISelectHandler
    {
        #region Helper Classes

        [System.Serializable]
        public struct IconName
        {
            public string name;
            public Sprite sprite;
        }

        [Serializable]
        public class HrefClickEvent : UnityEvent<string> { }

        public enum HrefMouseEvent { None, Hover, Pressed }

        [Serializable]
        public class SpecialHrefEvent
        {
            public string hrefName = "";
            public HrefClickEvent OnHrefClicked = new HrefClickEvent();
        }

        [Serializable]
        public class HrefInfo
        {
            public int startIndex;
            public int endIndex;
            public string name;
            public List<Rect> boxes = new List<Rect>();
            public HrefMouseEvent mouseEvent = HrefMouseEvent.None;
        }

        #endregion

        #region ReadOnly

        private readonly List<Image> m_ImagesPool = new List<Image>();
        private readonly List<GameObject> culled_ImagesPool = new List<GameObject>();
        private readonly List<int> m_ImagesVertexIndex = new List<int>();
        private static readonly Regex s_Regex = new Regex(@"<quad name=(.+?) size=(\d*\.?\d+%?) width=(\d*\.?\d+%?) />", RegexOptions.Singleline);
        private readonly List<HrefInfo> m_HrefInfos = new List<HrefInfo>();
        private static readonly StringBuilder s_TextBuilder = new StringBuilder();
        private static readonly Regex s_HrefRegex = new Regex(@"<a href=([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline);
        private static readonly Regex s_HrefAfterParseRegex = new Regex(@"<color=#([a-zA-Z0-9_]+)>\r(.*?)(\r</color>)", RegexOptions.Singleline);

        #endregion

        #region Variables

        private bool clearImages = false;
        private UnityEngine.Object thisLock = new UnityEngine.Object();
        private string fixedString;
        private string m_OutputText;
        public IconName[] inspectorIconList;
        private Dictionary<string, Sprite> iconList = new Dictionary<string, Sprite>();
        public float ImageScalingFactor = 1;
        public Color hyperlinkColor = new Color(51 / 255.0f, 102 / 255.0f, 187 / 255.0f);
        public Color hyperlinkHoverMultiplier = new Color(0.9f, 0.9f, 0.9f);
        public Color hyperlinkPressedMultiplier = new Color(0.7f, 0.7f, 0.7f);
        public Vector2 imageOffset = Vector2.zero;
        private Button button;
        private List<Vector2> positions = new List<Vector2>();
        private string previousText = "";
        public bool isCreating_m_HrefInfos = true;

        string _layoutText = "";

        #endregion

        #region Callbacks

        public HrefClickEvent OnHrefClick = new HrefClickEvent();
        [SerializeField]
        List<SpecialHrefEvent> m_specialHrefClickEvents = new List<SpecialHrefEvent>();

        #endregion

        #region Properties

        public List<SpecialHrefEvent> SpecialHrefClickEvents
        {
            get
            {
                if (m_specialHrefClickEvents == null)
                    m_specialHrefClickEvents = new List<SpecialHrefEvent>();
                return m_specialHrefClickEvents;
            }
            set
            {
                if (m_specialHrefClickEvents == value)
                    return;
                m_specialHrefClickEvents = value;
            }
        }

        public override float preferredWidth
        {
            get
            {
                var settings = GetGenerationSettings(Vector2.zero);
                return cachedTextGeneratorForLayout.GetPreferredWidth(LayoutText, settings) / pixelsPerUnit;
            }
        }

        public override float preferredHeight
        {
            get
            {
                var settings = GetGenerationSettings(new Vector2(rectTransform.rect.size.x, 0.0f));
                return cachedTextGeneratorForLayout.GetPreferredHeight(LayoutText, settings) / pixelsPerUnit;
            }
        }

        protected virtual string LayoutText
        {
            get
            {
                if (_layoutText == null)
                    _layoutText = "";
                return _layoutText;
            }
            set
            {
                if (string.Equals(_layoutText, value))
                    return;
                _layoutText = value;
                SetLayoutDirty();
            }
        }

        #endregion

        #region Unity Functions

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateQuadImage();
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            SetVerticesDirty();
        }

        protected override void Start()
        {
            base.Start();
            button = GetComponent<Button>();
            if (inspectorIconList != null && inspectorIconList.Length > 0)
            {
                foreach (IconName icon in inspectorIconList)
                {
                    // Debug.Log(icon.sprite.name);
                    iconList.Add(icon.name, icon.sprite);
                }
            }
            Reset_m_HrefInfos();
        }

        protected virtual void Update()
        {
            lock (thisLock)
            {
                if (clearImages)
                {
                    for (int i = 0; i < culled_ImagesPool.Count; i++)
                    {
                        DestroyImmediate(culled_ImagesPool[i]);
                    }
                    culled_ImagesPool.Clear();
                    clearImages = false;
                }
            }
            if (previousText != text)
                Reset_m_HrefInfos();
        }

        protected virtual void LateUpdate()
        {
            CheckIfTextChanged();
            if (_pointerEnterCamera != null)
                OnPointerOver(Input.mousePosition);
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            var orignText = m_Text;
            LayoutText = GetDisplayedText();
            m_Text = GetOutputText();
            base.OnPopulateMesh(toFill);
            m_Text = orignText;
            //var v_displayedText = GetDisplayedText();
            //Debug.Log(v_displayedText);
            positions.Clear();

            UIVertex vert = new UIVertex();
            for (var i = 0; i < m_ImagesVertexIndex.Count; i++)
            {
                var endIndex = m_ImagesVertexIndex[i];
                var rt = m_ImagesPool[i].rectTransform;
                var size = rt.sizeDelta;
                if (endIndex < toFill.currentVertCount)
                {
                    toFill.PopulateUIVertex(ref vert, endIndex);
                    positions.Add(new Vector2((vert.position.x + size.x / 2), (vert.position.y + size.y / 2)) + imageOffset);

                    // Erase the lower left corner of the black specks
                    toFill.PopulateUIVertex(ref vert, endIndex - 3);
                    var pos = vert.position;
                    for (int j = endIndex, m = endIndex - 3; j > m; j--)
                    {
                        toFill.PopulateUIVertex(ref vert, endIndex);
                        vert.position = pos;
                        toFill.SetUIVertex(vert, j);
                    }
                }
            }

            if (m_ImagesVertexIndex.Count != 0)
            {
                m_ImagesVertexIndex.Clear();
            }

            // Hyperlinks surround processing box
            foreach (var hrefInfo in m_HrefInfos)
            {
                hrefInfo.boxes.Clear();
                if (hrefInfo.startIndex >= toFill.currentVertCount)
                {
                    continue;
                }

                // Hyperlink inside the text is added to surround the vertex index coordinate frame
                toFill.PopulateUIVertex(ref vert, hrefInfo.startIndex);
                var pos = vert.position;
                var bounds = new Bounds(pos, Vector3.zero);
                for (int i = hrefInfo.startIndex, m = hrefInfo.endIndex; i < m; i++)
                {
                    if (i >= toFill.currentVertCount)
                    {
                        break;
                    }

                    toFill.PopulateUIVertex(ref vert, i);
                    pos = vert.position;
                    if (pos.x < bounds.min.x) // Wrap re-add surround frame
                    {
                        hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                        bounds = new Bounds(pos, Vector3.zero);
                    }
                    else
                    {
                        bounds.Encapsulate(pos); // Extended enclosed box
                    }
                }
                hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
            }
            UpdateQuadImage();
        }

        #endregion

        #region Unity Input Events

        public void OnPointerOver(Vector2 p_screenMousePosition)
        {
            if (_pointerEnterCamera != null)
            {
                UpdateHrefMouseEvents(p_screenMousePosition, _pointerEnterCamera, HrefMouseEvent.Hover);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector2 lp;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, eventData.position, eventData.pressEventCamera, out lp);

            var v_hrefInfo = GetHrefInsidePosition(lp);
            if (v_hrefInfo != null)
            {
                Debug.Log("HrefInfoClicked: " + v_hrefInfo.name);
                SpecialHrefEvent v_specialHrefSelected = null;
                foreach (var v_specialHref in SpecialHrefClickEvents)
                {
                    if (v_specialHref.hrefName == v_hrefInfo.name)
                    {
                        v_specialHrefSelected = v_specialHref;
                        break;
                    }
                }
                if (v_specialHrefSelected != null)
                {
                    if (v_specialHrefSelected.OnHrefClicked != null)
                        v_specialHrefSelected.OnHrefClicked.Invoke(v_specialHrefSelected.hrefName);
                }
                else
                {
                    if (OnHrefClick != null)
                        OnHrefClick.Invoke(v_hrefInfo.name);
                }
            }
        }

        Camera _pointerEnterCamera = null;
        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerEnterCamera = eventData.enterEventCamera;
            //do your stuff when highlighted
            //selected = true;
            if (m_ImagesPool.Count >= 1)
            {
                foreach (Image img in m_ImagesPool)
                {
                    if (button != null && button.isActiveAndEnabled)
                    {
                        img.color = button.colors.highlightedColor;
                    }
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerEnterCamera = null;
            UpdateHrefMouseEvents(eventData.position, _pointerEnterCamera, HrefMouseEvent.None);
            //do your stuff when highlighted
            //selected = false;
            if (m_ImagesPool.Count >= 1)
            {
                foreach (Image img in m_ImagesPool)
                {
                    if (button != null && button.isActiveAndEnabled)
                    {
                        img.color = button.colors.normalColor;
                    }
                    else
                    {
                        img.color = color;
                    }
                }
            }
        }
        public void OnSelect(BaseEventData eventData)
        {
            //do your stuff when selected
            //selected = true;
            if (m_ImagesPool.Count >= 1)
            {
                foreach (Image img in m_ImagesPool)
                {
                    if (button != null && button.isActiveAndEnabled)
                    {
                        img.color = button.colors.highlightedColor;
                    }
                }
            }
        }

        #endregion

        #region Helper Functions

        protected void UpdateHrefMouseEvents(Vector2 p_screenMousePosition, Camera p_pointerCamera, HrefMouseEvent p_mouseEventType)
        {
            Vector2 v_localPosition = Vector2.zero;
            if (p_pointerCamera != null)
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, p_screenMousePosition, p_pointerCamera, out v_localPosition);

            bool v_needUpdateMesh = false;
            var v_hrefInfoMouseOver = p_pointerCamera != null ? GetHrefInsidePosition(v_localPosition) : null;
            foreach (var v_href in m_HrefInfos)
            {
                var v_newEvent = v_href != v_hrefInfoMouseOver ? HrefMouseEvent.None : p_mouseEventType;
                if (v_newEvent == HrefMouseEvent.Hover && Input.GetMouseButton(0))
                    v_newEvent = HrefMouseEvent.Pressed;
                if (v_href.mouseEvent != v_newEvent)
                {
                    v_href.mouseEvent = v_newEvent;
                    v_needUpdateMesh = true;
                }
            }

            if (v_needUpdateMesh)
                SetVerticesDirty();
        }

        protected HrefInfo GetHrefInsidePosition(Vector2 p_localPosition)
        {
            HrefInfo v_hrefInfoSelected = null;
            foreach (var hrefInfo in m_HrefInfos)
            {
                var boxes = hrefInfo.boxes;
                for (var i = 0; i < boxes.Count; ++i)
                {
                    if (boxes[i].Contains(p_localPosition))
                    {
                        v_hrefInfoSelected = hrefInfo;
                        break;
                    }
                }
            }
            return v_hrefInfoSelected;
        }

        public override void SetVerticesDirty()
        {
            base.SetVerticesDirty();
            UpdateQuadImage();
        }

        protected virtual void CheckIfTextChanged()
        {
            if (isCreating_m_HrefInfos)
            {
                SetVerticesDirty();
            }
        }

        // Reseting m_HrefInfos array if there is any change in text
        void Reset_m_HrefInfos()
        {
            previousText = text;
            m_HrefInfos.Clear();
            isCreating_m_HrefInfos = true;
        }

        protected void UpdateQuadImage()
        {
#if UNITY_EDITOR
            if (this == null || !this.gameObject.scene.IsValid())
            {
                return;
            }
#endif
            m_OutputText = GetOutputText();
            m_ImagesVertexIndex.Clear();
            foreach (Match match in s_Regex.Matches(m_OutputText))
            {
                var picIndex = match.Index;
                var endIndex = picIndex * 4 + 3;
                m_ImagesVertexIndex.Add(endIndex);

                m_ImagesPool.RemoveAll(image => image == null);
                if (m_ImagesPool.Count == 0)
                {
                    GetComponentsInChildren<Image>(m_ImagesPool);
                }
                if (m_ImagesVertexIndex.Count > m_ImagesPool.Count)
                {
                    var resources = new DefaultControls.Resources();
                    var go = DefaultControls.CreateImage(resources);
                    go.layer = gameObject.layer;
                    var rt = go.transform as RectTransform;
                    if (rt)
                    {
                        rt.SetParent(rectTransform);
                        rt.localPosition = Vector3.zero;
                        rt.localRotation = Quaternion.identity;
                        rt.localScale = Vector3.one;
                    }
                    m_ImagesPool.Add(go.GetComponent<Image>());
                }

                var spriteName = match.Groups[1].Value;
                //var size = float.Parse(match.Groups[2].Value);
                var img = m_ImagesPool[m_ImagesVertexIndex.Count - 1];
                if (img.sprite == null || img.sprite.name != spriteName)
                {
                    // img.sprite = Resources.Load<Sprite>(spriteName);
                    if (inspectorIconList != null && inspectorIconList.Length > 0)
                    {
                        foreach (IconName icon in inspectorIconList)
                        {
                            if (icon.name == spriteName)
                            {
                                img.sprite = icon.sprite;
                                break;
                            }
                        }
                    }
                }
                img.rectTransform.sizeDelta = new Vector2(fontSize * ImageScalingFactor, fontSize * ImageScalingFactor);
                img.enabled = true;
                if (positions.Count == m_ImagesPool.Count)
                {
                    img.rectTransform.anchoredPosition = positions[m_ImagesVertexIndex.Count - 1];
                }
            }

            for (var i = m_ImagesVertexIndex.Count; i < m_ImagesPool.Count; i++)
            {
                if (m_ImagesPool[i])
                {
                    /* TEMPORARY FIX REMOVE IMAGES FROM POOL DELETE LATER SINCE CANNOT DESTROY */
                    // m_ImagesPool[i].enabled = false;
                    m_ImagesPool[i].gameObject.SetActive(false);
                    m_ImagesPool[i].gameObject.hideFlags = HideFlags.HideAndDontSave;
                    culled_ImagesPool.Add(m_ImagesPool[i].gameObject);
                    m_ImagesPool.Remove(m_ImagesPool[i]);
                }
            }
            if (culled_ImagesPool.Count > 1)
            {
                clearImages = true;
            }
        }

        private string ColorToHex(Color32 color)
        {
            return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2") + color.a.ToString("X2");
        }

        protected string GetDisplayedText()
        {
            var v_text = GetOutputText();
            return GetDisplayedText(v_text);
        }

        protected virtual string GetDisplayedText(string p_text)
        {
            if (p_text == null)
                p_text = "";
            if (p_text.Contains("\r"))
                p_text = p_text.Replace("\r", "");
            if (supportRichText)
            {
                p_text = Regex.Replace(p_text, "(<size=[0-9]+>|</size>|<i>|</i>|<color=#?[a-zA-Z0-9_]+>|</color>|<b>|</b>)", "");
                return p_text;
            }
            return p_text;
        }

        /// <summary>
        /// Finally, the output text hyperlinks get parsed
        /// </summary>
        /// <returns></returns>
        protected string GetOutputText()
        {
            s_TextBuilder.Length = 0;

            var indexText = 0;
            fixedString = this.text.Replace("\r", "");
            if (inspectorIconList != null && inspectorIconList.Length > 0)
            {
                foreach (IconName icon in inspectorIconList)
                {
                    if (icon.name != null && icon.name != "")
                    {
                        fixedString = fixedString.Replace(icon.name, "<quad name=" + icon.name + " size=" + fontSize + " width=1 />");
                    }
                }
            }
            int count = 0;
            var v_hrefMatches = s_HrefRegex.Matches(fixedString);
            if (m_HrefInfos.Count != v_hrefMatches.Count && !isCreating_m_HrefInfos)
            {
                m_HrefInfos.Clear();
                isCreating_m_HrefInfos = true;
            }
            foreach (Match v_match in v_hrefMatches)
            {
                var v_groupName = v_match.Groups.Count > 1 ? v_match.Groups[1].Value : "";
                if (isCreating_m_HrefInfos)
                {
                    var hrefInfo = new HrefInfo();
                    hrefInfo.name = v_groupName;
                    m_HrefInfos.Add(hrefInfo);
                    SetLayoutDirty();
                }
                Color v_currentLinkColor = hyperlinkColor;
                if (m_HrefInfos.Count > count)
                {
                    v_currentLinkColor = m_HrefInfos[count].mouseEvent == HrefMouseEvent.Hover ? new Color(v_currentLinkColor.r * hyperlinkHoverMultiplier.r, v_currentLinkColor.g * hyperlinkHoverMultiplier.g, v_currentLinkColor.b * hyperlinkHoverMultiplier.b) :
                        (m_HrefInfos[count].mouseEvent == HrefMouseEvent.Pressed ? new Color(v_currentLinkColor.r * hyperlinkPressedMultiplier.r, v_currentLinkColor.g * hyperlinkPressedMultiplier.g, v_currentLinkColor.b * hyperlinkPressedMultiplier.b) : v_currentLinkColor);
                }
                s_TextBuilder.Append(fixedString.Substring(indexText, v_match.Index - indexText));
                string v_currentHyperlinkColorHex= ColorToHex(v_currentLinkColor);
                s_TextBuilder.Append("<color=#" + v_currentHyperlinkColorHex + ">\r");  // Hyperlink color (Place \r to mark hyperlink location)
                s_TextBuilder.Append(v_match.Groups[2].Value);
                s_TextBuilder.Append("\r</color>");
                indexText = v_match.Index + v_match.Length;
                count++;
            }
            

            s_TextBuilder.Append(fixedString.Substring(indexText, fixedString.Length - indexText));

            var v_finalText = s_TextBuilder.ToString().Replace("<br>", "\n");

            //Replace HTML color regex to unity color
            Regex v_regexColor = new Regex("<font[ ]*color[ ]*=[ ]*\".*\"");
            var v_matches = v_regexColor.Matches(v_finalText);
            foreach (Match v_match in v_matches)
            {
                var v_matchValue = v_match.Value;
                if (!string.IsNullOrEmpty(v_matchValue))
                {
                    var v_replacedResult = v_matchValue.Replace("<font ", "<").Replace("\"", "");
                    v_finalText = v_finalText.Replace(v_matchValue, v_replacedResult);
                }
            }
            v_finalText = v_finalText.Replace("</font>", "</color>");
            TryUpdateHrefs(v_finalText);
            v_finalText = v_finalText.Replace("\r", "");
            return v_finalText;
        }

        protected virtual void TryUpdateHrefs(string p_finalText)
        {
            if (p_finalText != null)
            {
                var v_count = 0;
                MatchCollection v_matchCollection = s_HrefAfterParseRegex.Matches(p_finalText);
                foreach (Match v_match in v_matchCollection)
                {
                    var v_groupDisplayText = v_match.Groups.Count > 2? v_match.Groups[2].Value : "";
                    var v_initialIndex = v_match.Groups.Count > 2? Math.Max(0, (v_match.Groups[2].Index  - 1 - (2*v_count))) : 0; // we must remove /r counts
                    if (m_HrefInfos.Count > 0 && m_HrefInfos.Count > v_count)
                    {
                        m_HrefInfos[v_count].startIndex = v_initialIndex * 4; // Hyperlinks in text starting vertex indices;
                        m_HrefInfos[v_count].endIndex = (v_initialIndex + v_groupDisplayText.Length - 1) * 4 + 3;
                        v_count++;
                    }
                }
            }
            if (isCreating_m_HrefInfos)
                isCreating_m_HrefInfos = false;    
        }

        public void CallOpenUrl(string p_url)
        {
            bool v_isUrlEvent = (p_url.StartsWith("'") && p_url.EndsWith("'")) || (p_url.StartsWith("\"") && p_url.EndsWith("\""));
            if (v_isUrlEvent)
            {
                var v_urlLink = p_url.Length - 2 > 0 ? p_url.Substring(1, p_url.Length - 2) : "";
                if (!string.IsNullOrEmpty(v_urlLink))
                    Application.OpenURL(v_urlLink);
            }
        }

        #endregion
    }
}