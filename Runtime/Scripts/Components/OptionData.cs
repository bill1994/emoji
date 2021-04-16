//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace MaterialUI
{
    /// <summary>
    /// Contains information about the data of a list of options.
    /// </summary>
    [Serializable]
    public class OptionDataList
    {
        /// <summary>
        /// The type of the images in the list.
        /// </summary>
        [SerializeField]
		private ImageDataType m_ImageType = ImageDataType.VectorImage;
        /// <summary>
        /// The type of the images in the list.
        /// </summary>
        public ImageDataType imageType
		{
			get { return m_ImageType; }
			set { m_ImageType = value; }
		}

        /// <summary>
        /// The list of options.
        /// </summary>
        [SerializeField]
        private List<OptionData> m_Options = new List<OptionData>();
        /// <summary>
        /// The list of options.
        /// </summary>
        public List<OptionData> options
		{
			get {
                if (m_Options == null)
                    m_Options = new List<OptionData>();
                return m_Options; }
			set { m_Options = value; }
		}
    }

    /// <summary>
    /// Contains information about a list option's data.
    /// </summary>
    [Serializable]
    public class OptionData
    {
        /// <summary>
        /// The option's text.
        /// </summary>
        [SerializeField]
        private string m_Text = string.Empty;
        /// <summary>
        /// The option's text.
        /// </summary>
        public string text
		{
			get
            {
                if (m_Text == null)
                    m_Text = string.Empty;
                return m_Text;
            }
			set { m_Text = value; }
		}

        /// <summary>
        /// The option's ImageData.
        /// </summary>
        [SerializeField]
        private ImageData m_ImageData = null;
        /// <summary>
        /// The option's ImageData.
        /// </summary>
        public ImageData imageData
        {
            get { return m_ImageData; }
            set { m_ImageData = value; }
        }

        public Action OnOptionSelectedAction
        {
            get { return _OnOptionSelectedAction; }
            set { _OnOptionSelectedAction = value; }
        }

        /// <summary>
        /// Called when the option is selected.
        /// </summary>
        public UnityEvent onOptionSelected = new UnityEvent();

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionData"/> class.
        /// </summary>
        public OptionData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionData"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="imageData">The image data.</param>
        /// <param name="onOptionSelected">Called when the option is selected.</param>

        Action _OnOptionSelectedAction = null;
        public OptionData(string text, ImageData imageData, Action onOptionSelectedAction = null)
        {
            m_Text = text;
            m_ImageData = imageData;
            if (onOptionSelectedAction != null)
            {
                _OnOptionSelectedAction = onOptionSelectedAction;
                if (this.onOptionSelected == null)
                    this.onOptionSelected = new UnityEvent();
                this.onOptionSelected.AddListener((UnityAction)(() =>
                {
                    if (this._OnOptionSelectedAction != null)
                        this._OnOptionSelectedAction();
                }));
            }
        }
    }
}