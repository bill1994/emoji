// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{
    /// <summary>
    /// Swaps a sprite in an Image component between 3 possible sprites, based on the MaterialUIScaler scale factor.
    /// </summary>
    /// <seealso cref="UnityEngine.EventSystems.UIBehaviour" />
    [ExecuteInEditMode]
    //[AddComponentMenu("MaterialUI/Sprite Swapper", 50)]
    [System.Obsolete("Useless Component. Remove this from your GameObjects")]
    public class SpriteSwapper : UIBehaviour
    {
        /// <summary>
        /// The target image to modify.
        /// </summary>
        [SerializeField]
        private Image m_TargetImage = null;
        /// <summary>
        /// The target image to modify.
        /// </summary>
        public Image targetImage
        {
            get { return m_TargetImage; }
            set
            {
                m_TargetImage = value;
                RefreshSprite();
            }
        }

        protected Canvas _RootCanvas;
        public Canvas rootCanvas
        {
            get
            {
                if (_RootCanvas == null)
                {
                    _RootCanvas = transform.GetRootCanvas();
                }
                return _RootCanvas;
            }
        }

        /// <summary>
        /// The sprite to use when scaling is less than or equal to 1.
        /// </summary>
        [SerializeField]
        private Sprite m_Sprite1X = null;
        /// <summary>
        /// The sprite to use when scaling is less than or equal to 1.
        /// </summary>
        public Sprite sprite1X
        {
            get { return m_Sprite1X; }
            set
            {
                m_Sprite1X = value;
                RefreshSprite();
            }
        }

        /// <summary>
        /// The sprite to use when scaling is more than 1 and less than or equal to 2.
        /// </summary>
        [SerializeField]
        private Sprite m_Sprite2X = null;
        /// <summary>
        /// The sprite to use when scaling is more than 1 and less than or equal to 2.
        /// </summary>
        public Sprite sprite2X
        {
            get { return m_Sprite2X; }
            set
            {
                m_Sprite2X = value;
                RefreshSprite();
            }
        }

        /// <summary>
        /// The sprite to use when scaling is more than 2.
        /// </summary>
        [SerializeField]
        private Sprite m_Sprite4X = null;
        /// <summary>
        /// The sprite to use when scaling is more than 2.
        /// </summary>
        public Sprite sprite4X
        {
            get { return m_Sprite4X; }
            set
            {
                m_Sprite4X = value;
                RefreshSprite();
            }
        }

        /// <summary>
        /// The last 1x sprite used, for caching purposes.
        /// </summary>
        private Sprite m_LastSprite1X;
        /// <summary>
        /// The last 2x sprite used, for caching purposes.
        /// </summary>
        private Sprite m_LastSprite2X;
        /// <summary>
        /// The last 4x sprite used, for caching purposes.
        /// </summary>
        private Sprite m_LastSprite4X;

        /// <summary>
        /// See MonoBehaviour.OnEnable.
        /// </summary>
        protected override void OnEnable()
        {
            if (!targetImage)
            {
                targetImage = gameObject.GetComponent<Image>();
            }

            RefreshSprite();
        }

        /// <summary>
        /// See MonoBehaviour.Start.
        /// </summary>
        protected override void Start()
        {
            if (rootCanvas == null) return;

            var scaler = rootCanvas.GetComponent<MaterialCanvasScaler>();
            if (scaler != null)
            {
                scaler.onCanvasAreaChanged.AddListener((scaleChanged, orientationChanged) =>
                {
                    if (scaleChanged)
                    {
                        SwapSprite(scaler.scaleFactor);
                    }
                });
                RefreshSprite();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// See MonoBehaviour.OnValidate.
        /// </summary>
        protected override void OnValidate()
        {
            RefreshSprite();
        }
#endif

        /// <summary>
        /// Checks to see if the sprite needs to be swapped.
        /// </summary>
        public void RefreshSprite()
        {
            if (rootCanvas == null) return;
            SwapSprite(rootCanvas.scaleFactor);
        }

        /// <summary>
        /// Swaps the sprite.
        /// </summary>
        /// <param name="scaleFactor">The scale factor.</param>
        private void SwapSprite(float scaleFactor)
        {
            if (!targetImage) return;

            if (scaleFactor > 2f && sprite4X)
            {
                targetImage.sprite = sprite4X;
            }
            else if (scaleFactor > 1f && sprite2X)
            {
                targetImage.sprite = sprite2X;
            }
            else
            {
                targetImage.sprite = sprite1X;
            }
        }
    }
}