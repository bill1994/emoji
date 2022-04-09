// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using Kyub.UI;
using UnityEngine;
using UnityEngine.UI;

namespace MaterialUI
{
    public class ReloadableDialogDatePickerYearItem : DialogDatePickerYearItem
    {
        #region Overrides

        public override int index { get { return DataIndex; } set { } }

        #endregion

        #region Reload Functions

        protected override void ApplyReload(ScrollDataView.ReloadEventArgs oldArgs, ScrollDataView.ReloadEventArgs newArgs)
        {
            base.ApplyReload(oldArgs, newArgs);
            year = (int)Data;

            var sender = Sender != null ? Sender.GetComponentInParent<ReloadableDialogDatePickerYearList>() : null;
            if (sender != null)
            {
                //Update color
                selectedImage.color = sender.AccentColor;

                ///Update Toggle State
                UpdateState(sender.GetCurrentYear());
            }
        }

        #endregion

        #region Receivers Functions

        protected override void HandleOnItemClicked()
        {
            base.HandleOnItemClicked();

            var sender = Sender != null ? Sender.GetComponentInParent<ReloadableDialogDatePickerYearList>() : null;
            if (sender != null)
                sender.OnItemClick(DataIndex);
        }

        #endregion
    }
}