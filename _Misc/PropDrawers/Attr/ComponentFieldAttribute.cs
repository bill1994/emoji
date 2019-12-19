using UnityEngine;
using System.Collections;

namespace Kyub
{
    public class ComponentFieldAttribute : SpecificFieldAttribute
    {
        #region Constructor

        public ComponentFieldAttribute() : base(typeof(Component))
        {
        }

        public ComponentFieldAttribute(bool p_readOnly) : base(p_readOnly, typeof(Component))
        {
        }

        #endregion
    }
}