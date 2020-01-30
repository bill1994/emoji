using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialUI
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class SerializeStyleProperty : System.Attribute
    {
        #region Private Variables

        private bool m_canApplyGraphicResources = true;

        #endregion

        #region Properties

        public bool CanApplyGraphicResources
        {
            get
            {
                return m_canApplyGraphicResources;
            }

            set
            {
                m_canApplyGraphicResources = value;
            }
        }

        #endregion
    }
}
