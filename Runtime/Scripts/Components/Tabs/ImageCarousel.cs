using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Image Carousel", 100)]
    [RequireComponent(typeof(TabView))]
    public class ImageCarousel : BaseTabView
    {
        #region Private Variables

        [SerializeField] private GameObject m_CarouselPageTemplate = null;
        [SerializeField] private OptionDataList m_CarouselData = new OptionDataList();
        
        private List<TabPage> m_Pages = new List<TabPage>();
        public override List<TabPage> pages { get { return m_Pages; } set { m_Pages = value; } }

        #endregion

        #region Unity Functions

        protected override void Start()
        {
            SetupCarouselData();
        }

        #endregion

        #region Initialize Methods

        public void SetupCarouselData()
        {
            InstantiatePages(m_CarouselData);
        }

        public void SetupCarouselData(List<Sprite> sprites)
        {
            if (sprites == null || sprites.Count == 0) return;

            OptionDataList optionDataList = new OptionDataList();
            optionDataList.imageType = ImageDataType.Sprite;

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null) continue;
                optionDataList.options.Add(new OptionData(null, new ImageData(sprite)));
            }

            InstantiatePages(optionDataList);
        }

        public void SetupCarouselData(List<string> imagesPaths)
        {
            if (imagesPaths == null || imagesPaths.Count == 0) return;

            OptionDataList optionDataList = new OptionDataList();
            optionDataList.imageType = ImageDataType.Sprite;

            foreach (string path in imagesPaths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                optionDataList.options.Add(new OptionData(null, new ImageData(path, null)));
            }

            InstantiatePages(optionDataList);
        }

        #endregion

        #region Helper Functions

        private void InstantiatePages(OptionDataList optionDataList)
        {
            if (!Application.isPlaying || m_CarouselPageTemplate == null || m_PagesContainer == null) return;

            foreach (var item in optionDataList.options)
            {
                GameObject carouselPage = Instantiate(m_CarouselPageTemplate, m_PagesContainer.transform) as GameObject;
                ImageCarouselPage pageScript = carouselPage.GetComponent<ImageCarouselPage>();

                carouselPage.SetActive(true);

                if (pageScript != null)
                {
                    pageScript.Initialize(item);
                    pages.Add(pageScript);
                }
                else
                {
                    Destroy(carouselPage);
                }
            }

            m_CarouselPageTemplate.SetActive(false);
            InitializeTabsAndPagesDelayed();
        }

        #endregion
    }
}