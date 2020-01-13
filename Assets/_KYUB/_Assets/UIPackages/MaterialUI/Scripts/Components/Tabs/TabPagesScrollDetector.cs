//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Tab Pages Scroll Detector", 100)]
    public class TabPagesScrollDetector : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TabView m_TabView = null;
        public TabView tabView
        {
            get
            {
                if (m_TabView == null)
                    m_TabView = GetComponentInParent<TabView>();
                return m_TabView;
            }
            set { m_TabView = value; }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if(tabView != null)
                m_TabView.TabPageDrag();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if(tabView != null)
                m_TabView.TabPagePointerUp(eventData.delta.x);
        }
    }
}