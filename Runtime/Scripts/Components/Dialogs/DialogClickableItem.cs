// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using Kyub.UI;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [AddComponentMenu("MaterialUI/Dialogs/Clickable Option", 100)]
    public class DialogClickableOption : ScrollDataViewElement, IPointerClickHandler, ISubmitHandler
    {
        [System.Serializable]
        public class IntUnityEvent : UnityEvent<int> { }

        #region Callbacks

        public IntUnityEvent onItemClicked = new IntUnityEvent();
        public IntUnityEvent onAfterItemClicked = new IntUnityEvent();

        #endregion

        #region Unity Functions

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (IsInteractable())
            {
                HandleOnItemClicked();
            }
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable())
            {
                HandleOnItemClicked();
            }
        }

        public virtual bool IsInteractable()
        {
            bool interactable = true;
            if (interactable)
            {
                var allCanvas = GetComponentsInParent<CanvasGroup>();
                for (int i = 0; i < allCanvas.Length; i++)
                {
                    var canvas = allCanvas[i];

                    interactable = interactable && canvas.interactable;
                    if (!interactable || canvas.ignoreParentGroups)
                        break;
                }
            }
            return interactable;
        }

        #endregion

        #region Receivers Functions

        protected virtual void HandleOnItemClicked()
        {
            var dataIndex = DataIndex;
            if (onItemClicked != null)
            {
                onItemClicked.Invoke(dataIndex);
            }
            if (onAfterItemClicked != null)
            {
                onAfterItemClicked.Invoke(dataIndex);
            }
        }

        #endregion
    }
}