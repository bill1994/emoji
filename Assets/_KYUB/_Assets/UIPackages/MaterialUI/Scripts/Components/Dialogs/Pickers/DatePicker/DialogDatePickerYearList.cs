//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Date Picker Year List", 100)]
    public class DialogDatePickerYearList : MonoBehaviour
    {
        #region Private Variables

        [SerializeField]
        protected DialogDatePicker m_DatePicker = null;
		[SerializeField]
        protected GameObject m_YearTemplate = null;
        [SerializeField]
        protected ScrollRect m_ScrollRect = null;
        [SerializeField]
        protected int m_minYear = 1900;
        [SerializeField]
        protected int m_maxYear = 2100;
        [SerializeField]
        protected bool m_maxYearIsCurrentYear = false;

        private List<DialogDatePickerYearItem> m_YearItems = new List<DialogDatePickerYearItem>();

        #endregion

        #region Public Properties

        protected DialogDatePicker DatePicker
        {
            get
            {
                return m_DatePicker;
            }
        }

        protected GameObject YearTemplate
        {
            get { return m_YearTemplate; }
        }

        protected int MinYear
        {
            get { return m_minYear; }
        }

        protected int MaxYear
        {
            get { return m_maxYear; }
            set { m_maxYear = value; }
        }

        protected bool MaxYearIsCurrentYear
        {
            get { return m_maxYearIsCurrentYear; }
            set { m_maxYearIsCurrentYear = value; }
        }

        protected List<DialogDatePickerYearItem> YearItems
        {
            get { return m_YearItems; }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            if(m_maxYearIsCurrentYear)
            {
                m_maxYear = DateTime.Now.Year;
            }

            List<int> yearList = new List<int>();
            for (int i = m_minYear; i < m_maxYear; i++)
            {
                yearList.Add(i);
            }

			m_YearItems.Clear();
            for (int i = 0; i < yearList.Count; i++)
            {
				m_YearItems.Add(CreateYearItem(i, yearList[i]));
            }

			Destroy(m_YearTemplate);
        }

        #endregion

        #region Helper Functions

        private DialogDatePickerYearItem CreateYearItem(int i, int year)
		{
			DialogDatePickerYearItem yearItem = Instantiate(m_YearTemplate).GetComponent<DialogDatePickerYearItem>();

			RectTransform yearRectTransform = yearItem.GetComponent<RectTransform>();
			yearRectTransform.SetParent(m_YearTemplate.transform.parent);
			yearRectTransform.localScale = Vector3.one;
			yearRectTransform.localEulerAngles = Vector3.zero;

			yearItem.year = year;
			yearItem.index = i;
			yearItem.onClickAction += OnItemClick;
			
			return yearItem;
		}

		public virtual void SetColor(Color accentColor)
		{
			for (int i = 0; i < m_YearItems.Count; i++)
			{
				m_YearItems[i].selectedImage.color = accentColor;
			}
		}

		public virtual void OnItemClick(int index)
		{
			Toggle toggle = m_YearItems[index].toggle;
			toggle.isOn = !toggle.isOn;

			if (!toggle.isOn)
			{
				return;
			}

			m_DatePicker.SetYear(m_YearItems[index].year);
		}

        public virtual void CenterToSelectedYear(int year)
        {
            int selectedIndex = 0;

			for (int i = 0; i < m_YearItems.Count; i++)
            {
				m_YearItems[i].UpdateState(year);

				if (m_YearItems[i].toggle.isOn)
                {
                    selectedIndex = i;
                }
            }

            float verticalPosition = 0;
            if (selectedIndex <= 3)
            {
                verticalPosition = 0;
            }
			else if (selectedIndex >= m_YearItems.Count - 3)
            {
                verticalPosition = 1;
            }
            else
            {
                verticalPosition = selectedIndex - 3; // Padding 3 elements
				verticalPosition /= m_YearItems.Count - 6; // We remove 6 elements, because the 3 first and 3 last can't be centered
            }

            m_ScrollRect.verticalNormalizedPosition = 1 - verticalPosition;
        }

        #endregion
    }
}