using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    public class BetterToggle : Toggle
    {
        [SerializeField]
        bool m_changeToggleOnPressDown = false;

        public bool ChangeToggleOnPressDown
        {
            get
            {
                return m_changeToggleOnPressDown;
            }
            set
            {
                if (m_changeToggleOnPressDown == value)
                    return;
                m_changeToggleOnPressDown = value;
            }
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!m_changeToggleOnPressDown)
            {
                base.OnPointerClick(eventData);
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (m_changeToggleOnPressDown)
            {
                base.OnPointerClick(eventData);
            }
        }
    }
}
