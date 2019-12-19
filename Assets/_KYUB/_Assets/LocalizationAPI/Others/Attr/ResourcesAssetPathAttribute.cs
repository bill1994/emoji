using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kyub.Localization
{
    public class ResourcesAssetPathAttribute : PropertyAttribute
    {
        System.Type _filterType = typeof(Object);
        public System.Type FilterType
        {
            get
            {
                return _filterType;
            }
        }

        public ResourcesAssetPathAttribute() : this(typeof(Object))
        {
        }

        public ResourcesAssetPathAttribute(System.Type p_type)
        {
            if (Kyub.Extensions.TypeExtensions.IsSameOrSubClassOrImplementInterface(p_type, typeof(Object)))
                _filterType = p_type;
            else
                _filterType = typeof(Object);
        }
    }
}
