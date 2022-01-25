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
        [System.Flags]
        public enum OptionsHiddenFlagEnum : int { Hidden = 1, Disabled = 2 }

        #region Private Variables

        [SerializeField]
        private string m_Text = string.Empty;
        [SerializeField]
        private ImageData m_ImageData = null;
        [SerializeField]
        OptionsHiddenFlagEnum m_HiddenFlags = (OptionsHiddenFlagEnum)0;

        Action _OnOptionSelectedAction = null;

        #endregion

        #region Callback

        public UnityEvent onOptionSelected = new UnityEvent();

        #endregion

        #region Properties

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

        public ImageData imageData
        {
            get { return m_ImageData; }
            set { m_ImageData = value; }
        }

        public OptionsHiddenFlagEnum hiddenFlags
        {
            get { return m_HiddenFlags; }
            set { m_HiddenFlags = value; }
        }

        public bool visible
        {
            get
            {
                return !m_HiddenFlags.HasFlag(OptionsHiddenFlagEnum.Hidden);
            }
            set
            {

                if (value)
                {
                    m_HiddenFlags |= OptionsHiddenFlagEnum.Hidden;
                }
                else
                {
                    m_HiddenFlags &= ~OptionsHiddenFlagEnum.Hidden;
                }
            }
        }

        public bool interactable
        {
            get
            {
                return !m_HiddenFlags.HasFlag(OptionsHiddenFlagEnum.Disabled);
            }
            set
            {

                if (value)
                {
                    m_HiddenFlags |= OptionsHiddenFlagEnum.Disabled;
                }
                else
                {
                    m_HiddenFlags &= ~OptionsHiddenFlagEnum.Disabled;
                }
            }
        }

        public Action OnOptionSelectedAction
        {
            get { return _OnOptionSelectedAction; }
            set { _OnOptionSelectedAction = value; }
        }

        #endregion

        #region Constructors

        public OptionData() { }

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

        #endregion
    }
}