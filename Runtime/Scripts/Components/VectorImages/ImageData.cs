//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;

namespace MaterialUI
{
    public enum ImageDataType
    {
        Sprite,
        VectorImage
    }

    /// <summary>
    /// Allows Sprite and VectorImageData to be used interchangably
    /// </summary>
    [Serializable]
    public class ImageData
    {
        [SerializeField]
        private ImageDataType m_ImageDataType = ImageDataType.VectorImage;
        public ImageDataType imageDataType
        {
            get { return m_ImageDataType; }
            set { m_ImageDataType = value; }
        }

        [SerializeField]
        private Sprite m_Sprite = null;
        public Sprite sprite
        {
            get { return m_Sprite; }
            set { m_Sprite = value; }
        }

        [SerializeField]
        private VectorImageData m_VectorImageData = null;
        public VectorImageData vectorImageData
        {
            get { return m_VectorImageData; }
            set { m_VectorImageData = value; }
        }

        [SerializeField]
        private string m_ImgUrl = "";
        public string imgUrl
        {
            get { return m_ImgUrl; }
            set { m_ImgUrl = value; }
        }

        public ImageData(string imgUrl, Sprite defaultSprite)
        {
            m_ImgUrl = imgUrl;
            m_Sprite = defaultSprite;
            m_ImageDataType = ImageDataType.Sprite;
        }

        public ImageData(Sprite sprite)
        {
            m_Sprite = sprite;
            m_ImageDataType = ImageDataType.Sprite;
        }

        public ImageData(VectorImageData vectorImageData)
        {
            m_VectorImageData = vectorImageData;
            m_ImageDataType = ImageDataType.VectorImage;
        }

        public bool ContainsData(bool checkCurrentTypeOnly)
        {
            if (checkCurrentTypeOnly)
            {
                if (m_ImageDataType == ImageDataType.Sprite)
                {
                    return !string.IsNullOrEmpty(m_ImgUrl) || m_Sprite != null;
                }
                else
                {
                    return m_VectorImageData != null && m_VectorImageData.ContainsData();
                }
            }
            else
            {
                return m_Sprite != null || (m_VectorImageData != null && m_VectorImageData.ContainsData()) || !string.IsNullOrEmpty(m_ImgUrl);
            }
        }

        public static ImageData[] ArrayFromSpriteArray(Sprite[] spriteArray)
        {
            ImageData[] array = new ImageData[spriteArray.Length];

            for (int i = 0; i < spriteArray.Length; i++)
            {
                array[i] = new ImageData(spriteArray[i]);
            }

            return array;
        }

        public static ImageData[] ArrayFromVectorArray(VectorImageData[] vectorArray)
        {
            ImageData[] array = new ImageData[vectorArray.Length];

            for (int i = 0; i < vectorArray.Length; i++)
            {
                array[i] = new ImageData(vectorArray[i]);
            }

            return array;
        }
    }
}