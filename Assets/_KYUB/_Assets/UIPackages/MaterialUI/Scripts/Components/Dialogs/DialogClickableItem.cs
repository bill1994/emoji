//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

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
            HandleOnItemClicked();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            HandleOnItemClicked();
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