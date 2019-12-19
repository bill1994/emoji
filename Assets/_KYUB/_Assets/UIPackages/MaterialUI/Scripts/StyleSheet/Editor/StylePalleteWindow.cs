using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaterialUI
{
    /// <summary>
    /// Window used to replace asset/color of style to another one
    /// </summary>
    public class StylePalleteWindow : EditorWindow
    {
        #region Fields

        [SerializeField]
        StyleSheetAsset m_target = null;

        [SerializeField]
        Color32[] m_originalColors = new Color32[0];
        [SerializeField]
        Object[] m_originalGraphicAssets = new Object[0];
        [SerializeField]
        VectorImageData[] m_originalVectorImageDatas = new VectorImageData[0];

        [SerializeField]
        Color32[] m_replacedColors = new Color32[0];
        [SerializeField]
        Object[] m_replacedGraphicAssets = new Object[0];
        [SerializeField]
        VectorImageData[] m_replacedVectorImageDatas = new VectorImageData[0];

        Vector2 _position = Vector2.zero;

        #endregion

        #region Static Functions

        public static void Init(StyleSheetAsset p_target)
        {
            var window = CreateInstance<StylePalleteWindow>();
            window.m_target = p_target;
            window.OnEnable();
            window.ShowUtility();
            window.minSize = new Vector2(500, 250);
            window.titleContent = new GUIContent("Replace Style Assets");
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            if (m_target != null)
            {
                //Init Resources Editor
                CollectStyleResources(out m_originalColors, out m_originalGraphicAssets, out m_originalVectorImageDatas);
                Revert();
            }
        }

        protected virtual void OnFocus()
        {
            Repaint();
        }

        protected virtual void OnGUI()
        {
            var v_oldGuiEnabled = GUI.enabled;
            if (m_target == null || (m_originalColors.Length == 0 && m_originalGraphicAssets.Length == 0))
            {
                EditorGUILayout.HelpBox("No assets to show.", MessageType.Info);
            }
            else
            {
                GUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = false;
                    EditorGUILayout.LabelField("Target: ", GUILayout.Width(50));
                    EditorGUILayout.ObjectField(m_target, typeof(StyleSheetAsset), false);

                    GUILayout.FlexibleSpace();

                    GUI.enabled = _changed;
                    if (GUILayout.Button("Revert"))
                    {
                        Revert();
                    }
                    if (GUILayout.Button("Submit Changes"))
                    {
                        Submit();
                    }
                    GUI.enabled = v_oldGuiEnabled;
                }
                GUILayout.Space(5);

                using (var v_scrollScope = new EditorGUILayout.ScrollViewScope(_position))
                {
                    _position = v_scrollScope.scrollPosition;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawResources("Original", m_originalColors, m_originalGraphicAssets, m_originalVectorImageDatas, true, GUILayout.MaxWidth(30));
                        DrawResources("Replaced", m_replacedColors, m_replacedGraphicAssets, m_replacedVectorImageDatas, false);
                    }
                    EditorGUILayout.Space();
                }
            }
        }

        #endregion

        #region Helper Draw Functions

        protected virtual void DrawResources(string p_name, Color32[] p_colors, Object[] p_graphicAssets, VectorImageData[] p_vectorImageDatas, bool p_readOnly, params GUILayoutOption[] p_options)
        {
            using (new EditorGUILayout.VerticalScope("Box", p_options))
            {
                //Draw Title
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.LabelField(p_name, EditorStyles.boldLabel);
                }

                var v_oldGui = GUI.enabled;
                if (p_readOnly)
                    GUI.enabled = false;

                if(p_colors.Length > 0)
                    EditorGUILayout.LabelField("Color Palette");
                //Draw Colors
                for (int i = 0; i < p_colors.Length; i++)
                {
                    var v_newColor = EditorGUILayout.ColorField(new GUIContent(), p_colors[i], false, false, false);
                    if (v_newColor != p_colors[i])
                    {
                        _changed = true;
                        if (this != null)
                            Undo.RecordObject(this, "Color Changed");
                        p_colors[i] = v_newColor;
                    }
                }

                if (p_graphicAssets.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Graphic Assets");
                }
                //Draw Object Assets
                for (int i = 0; i < p_graphicAssets.Length; i++)
                {
                    var v_assetObject = p_graphicAssets[i] as Object;

                    if (v_assetObject != null)
                    {
                        var v_newAssetObject = EditorGUILayout.ObjectField(v_assetObject, v_assetObject.GetType(), false);
                        if (v_newAssetObject != null && v_newAssetObject != v_assetObject)
                        {
                            _changed = true;
                            if (this != null)
                                Undo.RecordObject(this, "Asset Changed");
                            p_graphicAssets[i] = v_newAssetObject;
                        }
                    }
                }
                if (p_vectorImageDatas.Length > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("VectorImage Assets");
                }
                //Draw VectorImageDatas
                for (int i = 0; i < p_vectorImageDatas.Length; i++)
                {
                    var v_vectorImgData = p_vectorImageDatas[i] as VectorImageData;

                    if (v_vectorImgData != null)
                    {
                        var v_indexToSet = i;
                        var v_newImgData = new VectorImageData(new Glyph(v_vectorImgData.glyph.name, v_vectorImgData.glyph.unicode, false), v_vectorImgData.vectorFont);
                        MaterialUI.InspectorFields.VectorImageDataField("", v_newImgData, m_target, () =>
                        {
                            if (v_newImgData != v_vectorImgData)
                            {
                                _changed = true;
                                if (this != null)
                                    Undo.RecordObject(this, "VectorImage Changed");
                                p_vectorImageDatas[v_indexToSet] = v_newImgData;
                                Repaint();
                            }
                        });
                    }
                }
                GUI.enabled = v_oldGui;
            }
        }

        #endregion

        #region Internal Helper Functions

        protected virtual void Submit()
        {
            if (m_target != null)
            {
                //Fill changed assets
                List<KeyValuePair<object, object>> v_replaceResourcesMap = new List<KeyValuePair<object, object>>();
                for (int i = 0; i < m_originalColors.Length; i++)
                {
                    var v_original = m_originalColors[i];
                    var v_replaced = m_replacedColors[i];
                    if (v_original.r != v_replaced.r || v_original.g != v_replaced.g || v_original.b != v_replaced.b)
                    {
                        v_replaceResourcesMap.Add(new KeyValuePair<object, object>(v_original, v_replaced));
                    }
                }
                for (int i = 0; i < m_originalGraphicAssets.Length; i++)
                {
                    var v_original = m_originalGraphicAssets[i];
                    var v_replaced = m_replacedGraphicAssets[i];
                    if (v_original != v_replaced)
                    {
                        v_replaceResourcesMap.Add(new KeyValuePair<object, object>(v_original, v_replaced));
                    }
                }
                for (int i = 0; i < m_originalVectorImageDatas.Length; i++)
                {
                    var v_original = m_originalVectorImageDatas[i];
                    var v_replaced = m_replacedVectorImageDatas[i];
                    if (v_original != v_replaced)
                    {
                        v_replaceResourcesMap.Add(new KeyValuePair<object, object>(v_original, v_replaced));
                    }
                }

                //Submit changes
                foreach (var v_pair in m_target.StyleObjectsMap)
                {
                    var v_styleData = v_pair.Value;
                    if (v_styleData != null && v_styleData.Asset != null)
                    {
                        v_styleData.Asset.TryReplaceStyleResources(v_replaceResourcesMap);
                    }
                }

                Close();
            }
        }

        bool _changed = false;
        protected virtual void Revert()
        {
            _changed = false;
            m_replacedColors = new List<Color32>(m_originalColors).ToArray();
            m_replacedGraphicAssets = new List<Object>(m_originalGraphicAssets).ToArray();
            m_replacedVectorImageDatas = new List<VectorImageData>(m_originalVectorImageDatas).ToArray();
        }

        public void CollectStyleResources(out Color32[] p_colorPalette, out Object[] p_graphicAssets, out VectorImageData[] p_imageDatas)
        {
            var v_graphicAssetsList = new List<Object>();
            var v_imageDatasList = new List<VectorImageData>();

            object[] v_graphicAssetsBoxed;
            CollectStyleResources(out p_colorPalette, out v_graphicAssetsBoxed);

            foreach (var v_graphicBoxed in v_graphicAssetsBoxed)
            {
                if (v_graphicBoxed is Object && ((Object)v_graphicBoxed) != null)
                    v_graphicAssetsList.Add(v_graphicBoxed as Object);
                else if (v_graphicBoxed is VectorImageData)
                    v_imageDatasList.Add(v_graphicBoxed as VectorImageData);
            }

            p_graphicAssets = v_graphicAssetsList.ToArray();
            p_imageDatas = v_imageDatasList.ToArray();

        }

        public void CollectStyleResources(out Color32[] p_colorPalette, out object[] p_graphicAssets)
        {
            if (m_target != null)
            {
                HashSet<object> v_resources = new HashSet<object>();
                m_target.Optimize();

                foreach (var v_pair in m_target.StyleObjectsMap)
                {
                    var v_styleData = v_pair.Value;
                    if (v_styleData != null && v_styleData.Asset != null)
                    {
                        var v_assetResources = v_styleData.Asset.CollectStyleResources();
                        foreach (var v_assetResource in v_assetResources)
                        {
                            v_resources.Add(v_assetResource);
                        }
                    }
                }

                //Segregate Resources by Type
                var v_colorPalette = new List<Color32>();
                var v_graphicAssets = new List<object>();
                foreach (var v_resource in v_resources)
                {
                    if (v_resource is Color || v_resource is Color32)
                        v_colorPalette.Add((Color32)v_resource);
                    else if (v_resource is Object && ((Object)v_resource) != null)
                        v_graphicAssets.Add(v_resource);
                    else if (v_resource is VectorImageData)
                        v_graphicAssets.Add(v_resource);
                }

                //Sort colors by hue
                System.Func<Color32, float> getHue = (Color32 p_color) =>
                {
                    float h, s, v;
                    Color.RGBToHSV(p_color, out h, out s, out v);
                    return h;
                };
                v_colorPalette.Sort((Color32 a, Color32 b) => { return getHue(a).CompareTo(getHue(b)); });

                //Sort assets by type and by initial name letter
                v_graphicAssets.Sort((object a, object b) =>
                {
                    long v_typeA = a.GetType().GetHashCode();
                    long v_typeB = b.GetType().GetHashCode();

                    var v_valueA = a is Object ? ((Object)a).name[0] : (a is VectorImageData ? ((VectorImageData)a).glyph.name[0] : -1);
                    var v_valueB = b is Object ? ((Object)b).name[0] : (b is VectorImageData ? ((VectorImageData)b).glyph.name[0] : -1);
                    return (v_typeA + v_valueA).CompareTo(v_typeB + v_valueB);
                });

                //Return values
                p_colorPalette = v_colorPalette.ToArray();
                p_graphicAssets = v_graphicAssets.ToArray();
            }
            else
            {
                p_colorPalette = new Color32[0];
                p_graphicAssets = new object[0];
            }
        }

        #endregion
    }
}
