//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using Kyub.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    public class ReloadableDialogDatePickerYearItem : DialogDatePickerYearItem, IReloadableDataViewElement
    {
        #region Helper Functions

        public void Reload(ScrollDataView.ReloadEventArgs p_args)
        {
            year = (int)p_args.Data;
            index = p_args.DataIndex;
            
            var sender = p_args.Sender != null ? p_args.Sender.GetComponentInParent<ReloadableDialogDatePickerYearList>() : null;
            if (sender != null)
            {
                //Register click event
                onClickAction -= sender.OnItemClick;
                onClickAction += sender.OnItemClick;
                //Update color
                selectedImage.color = sender.AccentColor;

                ///Update Toggle State
                UpdateState(sender.GetCurrentYear());
            }
        }

        #endregion

        #region IReloadableDataViewElement Functions

        bool IReloadableDataViewElement.IsDestroyed()
        {
            return this == null;
        }

        #endregion
    }
}