using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kyub.EventSystems
{
    public class StandaloneInputModuleCompat : StandaloneInputModule
    {
        protected override void Awake()
        {
            inputOverride = GetComponent<BaseInputCompat>();
            if (inputOverride == null)
                inputOverride = this.gameObject.AddComponent<BaseInputCompat>();
            base.Awake();
        }
    }
}
