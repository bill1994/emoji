using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Kyub.Reflection;
using System.Reflection;

namespace Kyub.Extensions
{
    public static class TypeExtensions
    {
        #region Type Utils

        public static bool IsSameOrSubclass(this System.Type p_potentialDescendant, System.Type p_potentialBase)
        {
            if (p_potentialBase != null && p_potentialDescendant != null)
            {
                return p_potentialDescendant.IsSubclassOf(p_potentialBase)
                    || p_potentialDescendant == p_potentialBase;
            }
            return false;
        }

        public static bool IsSameOrSubClassOrImplementInterface(this System.Type p_potentialDescendant, System.Type p_potentialBase)
        {
            if (p_potentialBase != null && p_potentialDescendant != null)
            {
                bool v_sucess = p_potentialBase.IsAssignableFrom(p_potentialDescendant) || (new List<System.Type>(p_potentialDescendant.GetInterfaces())).Contains(p_potentialBase);
                if (!v_sucess)
                    v_sucess = IsSameOrSubclass(p_potentialDescendant, p_potentialBase);
                return v_sucess;
            }
            return false;
        }

        #endregion
    }
}
