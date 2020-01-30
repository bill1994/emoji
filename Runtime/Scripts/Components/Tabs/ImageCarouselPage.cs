//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Kyub.UI;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Image Carousel Page", 100)]
    public class ImageCarouselPage : TabPage
    {
        #region Private Variables

        [SerializeField] private Image m_Image = null;
        [SerializeField] private Text m_Text = null;

        #endregion

        #region Unity Functions

        #endregion

        #region  Helper Functions

        public void Initialize(OptionData optionData)
        {
            if (m_Image != null)
            {
                m_Image.SetImageData(optionData.imageData);
            }

            if (m_Text != null && !string.IsNullOrEmpty(optionData.text))
            {
                m_Text.text = optionData.text;
            }
        }

        #endregion
    }
}