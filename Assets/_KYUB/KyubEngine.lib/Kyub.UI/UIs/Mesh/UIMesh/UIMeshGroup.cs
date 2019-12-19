using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class UIMeshGroup : DirtyBehaviour
    {
        #region Helper Classes

        [System.Serializable]
        public class MaterialMapper
        {
            #region Private Variables

            [SerializeField]
            Material m_originalMaterial = null;
            [SerializeField]
            Material m_newMaterial = null;

            #endregion

            #region Public Properties

            public Material OriginalMaterial
            {
                get
                {
                    return m_originalMaterial;
                }
                set
                {
                    if (m_originalMaterial == value)
                        return;
                    m_originalMaterial = value;
                }
            }

            public Material NewMaterial
            {
                get
                {
                    return m_newMaterial;
                }
                set
                {
                    if (m_newMaterial == value)
                        return;
                    m_newMaterial = value;
                }
            }

            #endregion
        }

        #endregion

        #region Public Properties

        [SerializeField]
        Material m_defaultMaterial = null;
        [SerializeField]
        List<MaterialMapper> m_materialsToReplace = new List<MaterialMapper>();

        #endregion

        #region Public Properties

        public List<MaterialMapper> MaterialsToReplace
        {
            get
            {
                if (m_materialsToReplace == null)
                    m_materialsToReplace = new List<MaterialMapper>();
                return m_materialsToReplace;
            }
            set
            {
                if (m_materialsToReplace == value)
                    return;
                m_materialsToReplace = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            SetUIRendererActive(true);
            base.OnEnable();
        }

        protected override void Start()
        {
            base.Start();
        }

        protected override void OnDisable()
        {
            SetUIRendererActive(false);
            base.OnDisable();
        }

        protected virtual void OnDestroy()
        {
            DestroyUIGraphics();
        }

        #endregion

        #region Helper Functions

        protected virtual void DestroyUIGraphics()
        {
            UIMeshGraphic[] v_graphics = GetComponentsInChildren<UIMeshGraphic>(true);
            foreach (var v_graphic in v_graphics)
            {
                if (v_graphic != null && (v_graphic is UIMeshFilter || v_graphic is UISkinnedMeshFilter))
                {
                    if (Application.isPlaying)
                        Object.Destroy(v_graphic);
                    else
                        Object.DestroyImmediate(v_graphic);
                }
            }
        }

        protected virtual void SetUIRendererActive(bool p_active)
        {
            UIMeshGraphic[] v_graphics = GetComponentsInChildren<UIMeshGraphic>(true);
            foreach (var v_graphic in v_graphics)
            {
                if (v_graphic != null && (v_graphic is UIMeshFilter || v_graphic is UISkinnedMeshFilter))
                    v_graphic.enabled = p_active;
            }
        }

        protected override void Apply()
        {
            CorrectAnimator();
            ConvertToUIMesh();
        }

        protected virtual void CorrectAnimator()
        {
            var v_animators = new List<Animator>(GetComponentsInChildren<Animator>(true));
            v_animators.AddRange(GetComponentsInParent<Animator>(true));
            foreach (var v_animator in v_animators)
            {
                if (v_animator != null)
                    v_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        protected virtual void ConvertToUIMesh()
        {
            var v_skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var v_meshFilters = GetComponentsInChildren<MeshFilter>(true);

            foreach (var v_skinnedRenderer in v_skinnedRenderers)
            {
                if (v_skinnedRenderer != null)
                    UpdateUIMeshFromComponent(v_skinnedRenderer);
            }

            foreach (var v_meshFilter in v_meshFilters)
            {
                if (v_meshFilter != null)
                    UpdateUIMeshFromComponent(v_meshFilter);
            }
        }

        protected virtual void UpdateUIMeshFromComponent(Component p_meshComponent)
        {
            UIMeshGraphic v_meshGraphic = null;
            if (p_meshComponent != null)
            {
                var v_meshFilter = p_meshComponent as MeshFilter;
                var v_skinnedRenderer = p_meshComponent as SkinnedMeshRenderer;

                if (v_meshFilter != null)
                {
                    v_meshGraphic = v_meshFilter.GetComponent<UIMeshFilter>();
                    if (v_meshGraphic == null)
                    {
                        v_meshGraphic = v_meshFilter.gameObject.AddComponent<UIMeshFilter>();
                        v_meshGraphic.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
                    }
                    var v_renderer = v_meshFilter.GetComponent<Renderer>();
                    v_meshGraphic.material = TryGetMaterialToReplace(v_renderer != null ? v_renderer.sharedMaterial : null);
                    //Try Pick Color Component From Renderer MaterialPropertyBlock
                    if (v_renderer != null)
                    {
                        MaterialPropertyBlock v_propertyBlock = new MaterialPropertyBlock();
                        v_renderer.GetPropertyBlock(v_propertyBlock);
                        if (!v_propertyBlock.isEmpty)
                        {
                            Vector4 v_colorVector = v_propertyBlock.GetVector("_Color");
                            if (v_colorVector != Vector4.zero)
                                v_meshGraphic.color = new Color(v_colorVector.x, v_colorVector.y, v_colorVector.z, v_colorVector.w);
                            v_meshGraphic.Texture = v_propertyBlock.GetTexture("_MainTex") as Texture2D;
                        }
                    }
                }
                if (v_skinnedRenderer != null)
                {
                    v_meshGraphic = v_skinnedRenderer.GetComponent<UISkinnedMeshFilter>();
                    if (v_meshGraphic == null)
                        v_meshGraphic = v_skinnedRenderer.gameObject.AddComponent<UISkinnedMeshFilter>();
                    v_meshGraphic.material = TryGetMaterialToReplace(v_skinnedRenderer.sharedMaterial);

                    //Try Pick Color Component From SkinnedRenderer MaterialPropertyBlock
                    MaterialPropertyBlock v_propertyBlock = new MaterialPropertyBlock();
                    v_skinnedRenderer.GetPropertyBlock(v_propertyBlock);
                    if (!v_propertyBlock.isEmpty)
                    {
                        Vector4 v_colorVector = v_propertyBlock.GetVector("_Color");
                        if(v_colorVector != Vector4.zero)
                            v_meshGraphic.color = new Color(v_colorVector.x, v_colorVector.y, v_colorVector.z, v_colorVector.w);
                        v_meshGraphic.Texture = v_propertyBlock.GetTexture("_MainTex") as Texture2D;
                    }
                }
            }
        }

        protected virtual Material TryGetMaterialToReplace(Material p_originalMaterial)
        {
            if (m_materialsToReplace != null)
            {
                foreach (var v_mapper in m_materialsToReplace)
                {
                    if (v_mapper != null && v_mapper.OriginalMaterial == p_originalMaterial)
                        return v_mapper.NewMaterial;
                }
            }
            return m_defaultMaterial != null? m_defaultMaterial : p_originalMaterial;
        }

        #endregion
    }
}
