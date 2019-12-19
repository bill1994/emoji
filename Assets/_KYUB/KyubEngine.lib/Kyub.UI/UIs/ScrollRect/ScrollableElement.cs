using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Kyub.UI
{
    public class ScrollableElement : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        #region Properties

        ScrollRect _scrollRect = null;
        public ScrollRect ScrollRectComponent
        {
            get
            {
                if (_scrollRect == null)
                    _scrollRect = GetComponentInParent<ScrollRect>();
                return _scrollRect;
            }
        }

        #endregion

        #region Unity Functions

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if (ScrollRectComponent != null)
            {
                ScrollRectComponent.OnBeginDrag(eventData);
            }

        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (ScrollRectComponent != null)
            {
                ScrollRectComponent.OnDrag(eventData);
            }

        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (ScrollRectComponent != null)
            {
                ScrollRectComponent.OnEndDrag(eventData);
            }

        }

        #endregion
    }
}
