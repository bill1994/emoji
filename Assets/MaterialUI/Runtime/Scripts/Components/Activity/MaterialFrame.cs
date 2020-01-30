using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [DisallowMultipleComponent]
    public class MaterialFrame : UIBehaviour
    {
        #region Public Functions

        public virtual void OnActivityBeginShow()
        {
        }

        public virtual void OnActivityEndShow()
        {
        }

        public virtual void OnActivityBeginHide()
        {
        }

        public virtual void OnActivityEndHide()
        {
        }

        #endregion
    }
}
