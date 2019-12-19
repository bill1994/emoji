using UnityEngine;
using System.Collections;

namespace Kyub
{
    public class MaskEnumAttribute : SpecificFieldAttribute
    {
        #region Constructor

#if UNITY_EDITOR || (!UNITY_WP8 && !UNITY_WP_8_1 && !UNITY_WINRT)
        public MaskEnumAttribute() : base(typeof(System.IConvertible))
#else
	public MaskEnumAttribute() : base(typeof(object))
#endif
        {
        }

#if UNITY_EDITOR || (!UNITY_WP8 && !UNITY_WP_8_1 && !UNITY_WINRT)
        public MaskEnumAttribute(bool p_readOnly) : base(p_readOnly, typeof(System.IConvertible))
#else
	public MaskEnumAttribute(bool p_readOnly) : base(p_readOnly, typeof(object))
#endif
        {
        }

        #endregion
    }
}