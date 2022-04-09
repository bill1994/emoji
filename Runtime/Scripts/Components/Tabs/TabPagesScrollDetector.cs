// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Tab Pages Scroll Detector", 100)]
    public class TabPagesScrollDetector : MonoBehaviour, IDragHandler, IEndDragHandler
    {
        [SerializeField] private BaseTabView m_TabView = null;
        public BaseTabView tabView
        {
            get
            {
                if (m_TabView == null)
                    m_TabView = GetComponentInParent<BaseTabView>();
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