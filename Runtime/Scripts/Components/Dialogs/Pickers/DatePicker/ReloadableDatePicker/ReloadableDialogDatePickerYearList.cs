// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using Kyub.UI;

namespace MaterialUI
{
    public class ReloadableDialogDatePickerYearList : DialogDatePickerYearList
    {
        #region Private Variables

        [SerializeField]
        ScrollDataView m_dataView = null;
        [SerializeField]
        int m_selectedIndex = 0;
        [SerializeField]
        Color m_accentColor = MaterialUI.MaterialColor.teal500;

        #endregion

        #region Public Properties

        public int SelectedIndex
        {
            get
            {
                return m_selectedIndex;
            }

            set
            {
                m_selectedIndex = value;
            }
        }

        public ScrollDataView DataView
        {
            get
            {
                return m_dataView;
            }

            set
            {
                m_dataView = value;
            }
        }

        public Color AccentColor
        {
            get
            {
                return m_accentColor;
            }

            set
            {
                m_accentColor = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            if (m_dataView != null)
            {
                if (MaxYearIsCurrentYear)
                {
                    MaxYear = DateTime.Now.Year;
                }

                List<int> yearList = new List<int>();
                for (int i = MinYear; i < MaxYear; i++)
                {
                    yearList.Add(i);
                }

                YearItems.Clear();
                
                m_dataView.DefaultTemplate = YearTemplate;
                m_dataView.Setup(yearList);
            }
            else
            {
                base.Awake();
            }
        }

        #endregion

        #region Helper Functions

        public int GetCurrentYear()
        {
            return m_dataView != null && m_dataView.Data != null && m_selectedIndex >=0 && m_selectedIndex < m_dataView.Data.Count ? (int)m_dataView.Data[m_selectedIndex] : -1;
        }

        public override void SetColor(Color accentColor)
        {
            m_accentColor = accentColor;
            if (m_dataView != null)
            {
                for (int i = 0; i < m_dataView.Data.Count; i++)
                {
                    var element = m_dataView.GetElementAtDataIndex(i);
                    if (element != null)
                    {
                        var item = element.GetComponent<DialogDatePickerYearItem>();
                        item.selectedImage.color = accentColor;
                    }
                }
            }
            else
                base.SetColor(accentColor);
        }

        public override void OnItemClick(int index)
        {
            m_selectedIndex = index;
            if (m_dataView != null)
            {
                var element = m_dataView.GetElementAtDataIndex(index);
                if (element != null)
                {
                    var item = element.GetComponent<DialogDatePickerYearItem>();
                    if (item != null)
                    {
                        Toggle toggle = item.toggle;
                        toggle.isOn = true;

                        if (!toggle.isOn)
                        {
                            return;
                        }
                    }

                    if (m_dataView.Data.Count > index)
                        DatePicker.SetYear((int)m_dataView.Data[index]);
                }
            }
            else
                base.OnItemClick(index);
        }

        public override void CenterToSelectedYear(int year)
        {
            m_selectedIndex = m_dataView.Data.IndexOf(year);
            if (m_dataView != null)
            {
                for (int i = 0; i < m_dataView.Data.Count; i++)
                {
                    var element = m_dataView.GetElementAtDataIndex(i);
                    if (element != null)
                    {
                        var item = element.GetComponent<DialogDatePickerYearItem>();
                        if (item != null)
                        {
                            item.UpdateState(year);
                        }
                    }
                }

                float verticalPosition = 0;
                if (m_selectedIndex <= 3)
                {
                    verticalPosition = 0;
                }
                else if (m_selectedIndex >= m_dataView.Data.Count - 3)
                {
                    verticalPosition = 1;
                }
                else
                {
                    verticalPosition = m_selectedIndex - 3; // Padding 3 elements
                    verticalPosition /= m_dataView.Data.Count - 6; // We remove 6 elements, because the 3 first and 3 last can't be centered
                }

                m_ScrollRect.verticalNormalizedPosition = 1 - verticalPosition;
                m_dataView.SetLayoutDirty();
            }
            else
                base.CenterToSelectedYear(year);
        }

        #endregion
    }
}