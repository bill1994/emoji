// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace MaterialUI
{
    public class VectorImagePickerWindow : EditorWindow
    {
        private Vector2 m_ScrollPosition;
        private Vector2 m_IconViewScrollPosition;
        private static VectorImageData[] m_VectorImageDatas;
        private static Action m_RefreshAction;
        private static Object[] m_ObjectsToRefresh;
        private static int m_PreviewSize = 48;
        private VectorImageFont m_IconFont;
        private GUIStyle m_GuiStyle;
        private static Texture2D m_BackdropTexture;

        private float m_LastClickTime = float.MinValue;
        private GUIStyle m_BottomBarBg = "ProjectBrowserBottomBarBg";

        private string m_SearchText;
        private Glyph[] m_GlyphArray;

        public static void Show(VectorImageData data, Object objectToRefresh)
        {
            Show(new[] { data }, new[] { objectToRefresh }, null);
        }

        public static void Show(VectorImageData data, Object objectToRefresh, Action refreshAction)
        {
            Show(new[] { data }, new[] { objectToRefresh }, refreshAction);
        }

        public static void Show(VectorImageData[] datas, Object[] objectsToRefresh)
        {
            Show(datas, objectsToRefresh, null);
        }

        public static void Show(VectorImageData[] datas, Object[] objectsToRefresh, Action refreshAction)
        {
            m_VectorImageDatas = datas;
            m_ObjectsToRefresh = objectsToRefresh;
            m_RefreshAction = refreshAction;

            VectorImagePickerWindow window = CreateInstance<VectorImagePickerWindow>();
            window.ShowAuxWindow();
            window.minSize = new Vector2(397, 446);
            window.titleContent = new GUIContent("Icon Picker");

            m_PreviewSize = EditorPrefs.GetInt("ICON_CONFIG_PREVIEW_SIZE", 48);
        }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDisable()
        {
            //Destroy cached inverted texture
            CreateGlyphInvertedTexture(null);
            foreach (var pair in _fontsPerAsset)
            {
                if(pair.Value != null)
                    Resources.UnloadAsset(pair.Value);
            }
            _fontsPerAsset.Clear();
        }

        private void OnFocus()
        {
            Repaint();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        void OnGUI()
        {
            if (Event.current.isKey) // If we detect the user pressed the keyboard
            {
                EditorGUI.FocusTextInControl("SearchInputField");
            }

            if (Event.current.type == EventType.KeyDown)
            {
                KeyCode keyCode = Event.current.keyCode;
                if (keyCode != KeyCode.Return)
                {
                    if (keyCode == KeyCode.Escape)
                    {
                        base.Close();
                        GUIUtility.ExitGUI();
                        return;
                    }
                }
                else
                {
                    Close();
                    GUIUtility.ExitGUI();
                    return;
                }
            }

            using (GUILayout.ScrollViewScope scrollViewScope = new GUILayout.ScrollViewScope(m_ScrollPosition))
            {
                m_ScrollPosition = scrollViewScope.scrollPosition;

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Space(5f);

                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Space(10f);
                        DrawSearchTextField();
                        DrawPicker();
                    }

                    GUILayout.Space(5f);
                }
            }
        }

        public static void DrawIconPickLine(VectorImageData data, Object objectToRefresh, bool indent = false)
        {
            using (new GUILayout.HorizontalScope())
            {
                if (data.font == null)
                {
                    data.vectorFont = VectorImageManager.GetIconFont(VectorImageManager.GetAllIconSetNames()[0]);
                }

                GUIStyle iconGuiStyle = new GUIStyle { font = VectorImageManager.GetIconFont(data.font.name) };

                EditorGUILayout.PrefixLabel("Icon");

                if (indent)
                {
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.LabelField(IconDecoder.Decode(data.glyph.unicode), iconGuiStyle, GUILayout.Width(18f));
                EditorGUILayout.LabelField(data.glyph.name, GUILayout.MaxWidth(100f), GUILayout.MinWidth(0f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Pick icon", EditorStyles.miniButton, GUILayout.MaxWidth(60f)))
                {
                    Show(data, objectToRefresh);
                    return;
                }

                if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.MaxWidth(20f)))
                {
                    for (int i = 0; i < m_VectorImageDatas.Length; i++)
                    {
                        m_VectorImageDatas[i] = null;
                    }
                    return;
                }

                if (indent)
                {
                    EditorGUI.indentLevel++;
                }
            }
        }

        private bool SupportTMProFont()
        {
            foreach (var v_object in m_ObjectsToRefresh)
            {
                var v_vectorImage = v_object as IVectorImage;
                if (v_vectorImage != null && !v_vectorImage.SupportTMProFont())
                    return false;
            }
            return true;
        }

        private bool SupportUnityFont()
        {
            foreach (var v_object in m_ObjectsToRefresh)
            {
                var v_vectorImage = v_object as IVectorImage;
                if (v_vectorImage != null && !v_vectorImage.SupportUnityFont())
                    return false;
            }
            return true;
        }

        string[] validIconSetNames = null;
        protected void CacheIconSets()
        {
            var names = new System.Collections.Generic.List<string>(VectorImageManager.GetAllIconSetNames());

            var supportTmp = SupportTMProFont();
            var supportUnity = SupportUnityFont();
            if (!supportTmp || !supportUnity)
            {
                for(int i=0; i< names.Count; i++)
                {
                    var v_font = VectorImageManager.GetIconFont(names[i]);
                    if (v_font == null || (v_font.SupportTMProFont() != supportTmp && v_font.SupportUnityFont() != supportUnity))
                    {
                        names.RemoveAt(i);
                        i--;
                    }
                }
            }
            validIconSetNames = names.ToArray();
        }

        private void DrawPicker()
        {
            if (m_VectorImageDatas[0] == null)
            {
                GUILayout.Label("Invalid vector image");
                return;
            }

            if (m_VectorImageDatas[0].glyph == null)
            {
                GUILayout.Label("Invalid glyph");
                return;
            }

            if (m_VectorImageDatas[0].vectorFont == null)
            {
                m_VectorImageDatas[0].vectorFont = VectorImageManager.GetIconFont(VectorImageManager.GetAllIconSetNames()[1]);
            }

            if (validIconSetNames == null)
                CacheIconSets();

            if (validIconSetNames.Length > 0)
            {
                EditorGUI.BeginChangeCheck();
                GUIContent[] namesContents = new GUIContent[validIconSetNames.Length];
                for (int i = 0; i < validIconSetNames.Length; i++)
                {
                    namesContents[i] = new GUIContent(validIconSetNames[i]);
                }

                m_VectorImageDatas[0].vectorFont = VectorImageManager.GetIconFont(validIconSetNames[EditorGUILayout.Popup(new GUIContent("Current Pack"), validIconSetNames.ToList().IndexOf(m_VectorImageDatas[0].vectorFont.FontName), namesContents)]);

                bool changed = EditorGUI.EndChangeCheck();

                if (changed)
                {
                    m_IconViewScrollPosition = Vector2.zero;
                }

                if (changed || m_IconFont == null)
                {
                    UpdateFontPackInfo();
                }

                DrawIconList();
            }
            else
            {
                EditorGUILayout.HelpBox("No VectorImage fonts detected!", MessageType.Warning);
            }

            DrawBottomBar();
        }

        private void DrawBottomBar()
        {
            Rect bottomBarRect = new Rect(0f, base.position.height - 17f, base.position.width, 17f);
            GUI.Label(bottomBarRect, GUIContent.none, m_BottomBarBg);

			if (m_VectorImageDatas.Length > 0)
			{
				Rect labelRect = new Rect(bottomBarRect.x + 4f, bottomBarRect.y + 1f, bottomBarRect.width - 55f, 17f);
				GUI.Label(labelRect, m_VectorImageDatas[0].glyph.name);
			}

            Rect sliderRect = new Rect(bottomBarRect.x + bottomBarRect.width - 55f - 16f, bottomBarRect.y + bottomBarRect.height - 17f, 55f, 17f);
            EditorGUI.BeginChangeCheck();
            m_PreviewSize = (int)GUI.HorizontalSlider(sliderRect, m_PreviewSize, 15, 100);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt("ICON_CONFIG_PREVIEW_SIZE", m_PreviewSize);
            }
        }

        private void DrawSearchTextField()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUI.SetNextControlName("SearchInputField");
                EditorGUI.BeginChangeCheck();
                m_SearchText = EditorGUILayout.TextField("", m_SearchText, "SearchTextField");
                if (EditorGUI.EndChangeCheck())
                {
                    m_SearchText = m_SearchText.Trim();
                    UpdateGlyphList();
                }

                if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18f)))
                {
                    m_SearchText = String.Empty;
                    UpdateGlyphList();
                    GUI.FocusControl(null);
                }
            }

            GUILayout.Space(5f);
        }

        Dictionary<int, Font> _fontsPerAsset = new Dictionary<int, Font>();
        private Font GetSourceFont(TMPro.TMP_FontAsset font)
        {
            if (font != null)
            {
                var instanceId = font.GetInstanceID();
                if (!_fontsPerAsset.ContainsKey(instanceId))
                {
                    _fontsPerAsset[instanceId] = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(font.creationSettings.sourceFontFileGUID));
                    this.Repaint();
                }
                return _fontsPerAsset[instanceId];
            }
            return null;
        }

        Texture2D invertedTexture = null;
        private void CreateGlyphInvertedTexture(GUIStyle style)
        {
            if (invertedTexture != null)
                DestroyImmediate(invertedTexture);

            if (style != null && style.font != null && !style.font.dynamic && (style.font.fontSize > m_PreviewSize || style.font.fontSize < 0.5 * m_PreviewSize))
            {
                invertedTexture = CreateInvertedTexture(style.font.material.mainTexture as Texture2D, -1);
            }
        }

        protected virtual Texture2D CreateInvertedTexture(Texture2D textureToInvert, float alphaCulloff)
        {
            //Copy the new texture

            // Create a temporary RenderTexture of the same size as the texture
            RenderTexture tmp = RenderTexture.GetTemporary(
                                textureToInvert.width,
                                textureToInvert.height,
                                0,
                                RenderTextureFormat.Default,
                                RenderTextureReadWrite.Linear);

            // Blit the pixels on texture to the RenderTexture
            Graphics.Blit(textureToInvert, tmp);
            // Backup the currently set RenderTexture
            RenderTexture previous = RenderTexture.active;
            // Set the current RenderTexture to the temporary one we created
            RenderTexture.active = tmp;
            // Create a new readable Texture2D to copy the pixels to it
            var invertedTexture = new Texture2D(textureToInvert.width, textureToInvert.height);
            // Copy the pixels from the RenderTexture to the new Texture
            invertedTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            invertedTexture.Apply();
            // Reset the active RenderTexture
            RenderTexture.active = previous;
            // Release the temporary RenderTexture
            RenderTexture.ReleaseTemporary(tmp);

            for (int m = 0; m < invertedTexture.mipmapCount; m++)
            {
                Color[] c = invertedTexture.GetPixels(m);
                for (int i = 0; i < c.Length; i++)
                {
                    c[i].r = 1 - c[i].r;
                    c[i].g = 1 - c[i].g;
                    c[i].b = 1 - c[i].b;
                    if(alphaCulloff > 0)
                        c[i].a = c[i].a > alphaCulloff ? 1 : 0;
                }
                invertedTexture.SetPixels(c, m);
            }
            invertedTexture.Apply();
            return invertedTexture;
        }
        
        private void DrawIconList()
        {
            if (m_GlyphArray.Length == 0)
            {
                GUIStyle guiStyle = new GUIStyle(EditorStyles.boldLabel);
                guiStyle.alignment = TextAnchor.MiddleCenter;

                EditorGUILayout.LabelField("No icon found for your search term: " + m_SearchText, guiStyle, GUILayout.Height(Screen.height - 80f));
                return;
            }

            float padded = m_PreviewSize + 5f;
            int columns = Mathf.FloorToInt((Screen.width - 25f) / padded);
            if (columns < 1) columns = 1;

            int offset = 0;
            Rect rect = new Rect(0f, 0, m_PreviewSize, m_PreviewSize);

            using (GUILayout.ScrollViewScope scrollViewScope = new GUILayout.ScrollViewScope(m_IconViewScrollPosition, GUILayout.Height(Screen.height - 80f)))
            {
                m_IconViewScrollPosition = scrollViewScope.scrollPosition;

                //Calculate MinMax
                int initialRow = (int)(m_IconViewScrollPosition.y / padded);
                var initialOffset = (int)(initialRow * columns);
                int finalRow = (int)(Screen.height / padded) + 1;
                var minMaxVisibleIndexes = new Vector2Int(
                    Mathf.Clamp(initialOffset, 0, m_GlyphArray.Length - 1),
                    Mathf.Clamp(initialOffset + ((int)(finalRow * columns)), 0, m_GlyphArray.Length - 1));

                while (offset < m_GlyphArray.Length)
                {
                    if (offset >= minMaxVisibleIndexes.x && offset < (minMaxVisibleIndexes.y + 1))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            int col = 0;
                            rect.x = 0f;

                            for (; offset < m_GlyphArray.Length; ++offset)
                            {
                                // Change color of the selected VectorImage
                                if (m_VectorImageDatas[0].glyph.name == m_GlyphArray[offset].name)
                                {
                                    GUI.backgroundColor = MaterialColor.iconDark;
                                }

                                if (GUI.Button(rect, new GUIContent("", m_GlyphArray[offset].name)))
                                {
                                    if (Event.current.button == 0)
                                    {
                                        SetGlyph(offset);

                                        if (Time.realtimeSinceStartup - m_LastClickTime < 0.3f)
                                        {
                                            Close();
                                        }

                                        m_LastClickTime = Time.realtimeSinceStartup;
                                    }
                                }

                                if (Event.current.type == EventType.Repaint)
                                {
                                    drawTiledTexture(rect);

                                    if (m_GuiStyle.font != null && m_GuiStyle.font.dynamic)
                                        m_GuiStyle.fontSize = m_PreviewSize;

                                    string iconText = IconDecoder.Decode(@"\u" + m_GlyphArray[offset].unicode);
                                    Vector2 size = m_GuiStyle.font != null? m_GuiStyle.CalcSize(new GUIContent(iconText)) : new Vector2(m_PreviewSize, m_PreviewSize);

                                    float maxSide = size.x > size.y ? size.x : size.y;
                                    float scaleFactor = (m_PreviewSize / maxSide) * 0.9f;

                                    if (m_GuiStyle.font != null && m_GuiStyle.font.dynamic)
                                        m_GuiStyle.fontSize = Mathf.RoundToInt(m_PreviewSize * scaleFactor);
                                    size *= scaleFactor;

                                    Vector2 padding = new Vector2(rect.width - size.x, rect.height - size.y);
                                    Rect iconRect = new Rect(rect.x + (padding.x / 2f), rect.y + (padding.y / 2f), rect.width - padding.x, rect.height - padding.y);

                                    if (invertedTexture != null)
                                    {
                                        CharacterInfo charInfo;
                                        m_GuiStyle.font.GetCharacterInfo(iconText != null && iconText.Length > 0 ? iconText[0] : ' ', out charInfo);
                                        var uvRect = Rect.MinMaxRect(charInfo.uvBottomLeft.x, charInfo.uvBottomLeft.y, charInfo.uvTopRight.x, charInfo.uvTopRight.y);
                                        GUI.DrawTextureWithTexCoords(iconRect, invertedTexture, uvRect);
                                    }
                                    else
                                    {
                                        GUI.Label(iconRect, new GUIContent(iconText), m_GuiStyle);
                                    }
                                }

                                GUI.backgroundColor = Color.white;

                                if (++col >= columns)
                                {
                                    ++offset;
                                    break;
                                }
                                rect.x += padded;
                            }
                        }
                    }
                    //Increment CollumsAmount
                    else
                    {
                        offset += columns;
                    }


                    GUILayout.Space(padded);
                    rect.y += padded;
                }
            }
        }

        private void drawTiledTexture(Rect rect)
        {
            createCheckerTexture();

            GUI.BeginGroup(rect);
            {
                int width = Mathf.RoundToInt(rect.width);
                int height = Mathf.RoundToInt(rect.height);

                for (int y = 0; y < height; y += m_BackdropTexture.height)
                {
                    for (int x = 0; x < width; x += m_BackdropTexture.width)
                    {
                        GUI.DrawTexture(new Rect(x, y, m_BackdropTexture.width, m_BackdropTexture.height), m_BackdropTexture);
                    }
                }
            }
            GUI.EndGroup();
        }

        private static void createCheckerTexture()
        {
            if (m_BackdropTexture != null)
            {
                return;
            }

            Color c0 = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            Color c1 = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            m_BackdropTexture = new Texture2D(16, 16);
            m_BackdropTexture.name = "[Generated] Checker Texture";
            m_BackdropTexture.hideFlags = HideFlags.DontSave;

            for (int y = 0; y < 8; ++y) for (int x = 0; x < 8; ++x) m_BackdropTexture.SetPixel(x, y, c1);
            for (int y = 8; y < 16; ++y) for (int x = 0; x < 8; ++x) m_BackdropTexture.SetPixel(x, y, c0);
            for (int y = 0; y < 8; ++y) for (int x = 8; x < 16; ++x) m_BackdropTexture.SetPixel(x, y, c0);
            for (int y = 8; y < 16; ++y) for (int x = 8; x < 16; ++x) m_BackdropTexture.SetPixel(x, y, c1);

            m_BackdropTexture.Apply();
            m_BackdropTexture.filterMode = FilterMode.Point;
        }

        private void UpdateFontPackInfo()
        {
            string name = m_VectorImageDatas[0].vectorFont.FontName;
            m_IconFont = VectorImageManager.GetIconFont(name);
            m_GuiStyle = new GUIStyle { font = m_IconFont };
            if (m_GuiStyle.font == null)
                m_GuiStyle.font = GetSourceFont(m_VectorImageDatas[0].vectorFont.fontTMPro);
            m_GuiStyle.normal.textColor = Color.white;
            CreateGlyphInvertedTexture(m_GuiStyle);

            UpdateGlyphList();

            // Assign the very first icon of the imageSet if the glyph is null
            Glyph glyph = m_IconFont != null ? m_IconFont.GetGlyphByName(m_VectorImageDatas[0].glyph.name) : null;
            if (glyph == null)
            {
                SetGlyph(0);
            }
            Repaint();
        }

        private void UpdateGlyphList()
        {
            if (string.IsNullOrEmpty(m_SearchText))
            {
                m_GlyphArray = m_IconFont != null ? m_IconFont.iconSet.iconGlyphList.ToArray() : new Glyph[0];
            }
            else
            {
                m_GlyphArray = m_IconFont != null ? m_IconFont.iconSet.iconGlyphList.Where(x => x.name.Contains(m_SearchText)).ToArray() : new Glyph[0];
            }
        }

        private void SetGlyph(int index)
        {
            if (m_VectorImageDatas != null)
            {
                if (m_ObjectsToRefresh != null)
                {
                    Undo.RecordObjects(m_ObjectsToRefresh, "Set Icon");
                }

                Glyph glyph = m_GlyphArray.Length > index ? new Glyph(m_GlyphArray[index].name, m_GlyphArray[index].unicode, true) : new Glyph();

                for (int i = 0; i < m_VectorImageDatas.Length; i++)
                {
                    m_VectorImageDatas[i].glyph = glyph;
                    m_VectorImageDatas[i].vectorFont = m_IconFont;
                }

                m_RefreshAction.InvokeIfNotNull();
            }
        }
    }
}