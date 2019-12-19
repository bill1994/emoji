using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{

    /// <summary>
    /// Contains data about a dropdown list item in the scene.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class DropdownListItem : DropdownTrigger
    {
        public RectTransform rectTransform
        {
            get { return this.transform as RectTransform; }
        }

        public CanvasGroup m_CanvasGroup;
        public CanvasGroup canvasGroup
        {
            get
            {
                if (m_CanvasGroup == null)
                    m_CanvasGroup = GetComponent<CanvasGroup>();
                return m_CanvasGroup;
            }
            set { m_CanvasGroup = value; }
        }

        public Graphic m_Text;
        public Graphic text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        public Graphic m_Image;
        public Graphic image
        {
            get { return m_Image; }
            set { m_Image = value; }
        }

        public Graphic m_VectorImage;
        public IVectorImage vectorImage
        {
            get { return m_VectorImage as IVectorImage; }
            set { m_VectorImage = value as Graphic; }
        }
    }
}
