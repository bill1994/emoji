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

        public ResourcesAssetPathAttribute(System.Type type)
        {
            if (Kyub.Extensions.TypeExtensions.IsSameOrSubClassOrImplementInterface(type, typeof(Object)))
                _filterType = type;
            else
                _filterType = typeof(Object);
        }
    }
}
