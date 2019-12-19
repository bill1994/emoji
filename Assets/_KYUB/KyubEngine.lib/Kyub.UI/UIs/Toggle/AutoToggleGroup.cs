using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Kyub.UI
{
    public class AutoToggleGroup : MonoBehaviour
    {

        protected virtual void OnEnable()
        {
            TryApplyToggleGroup();
        }

        protected virtual void Start()
        {
            TryApplyToggleGroup();
        }

        protected virtual void TryApplyToggleGroup()
        {
            Toggle v_toggle = GetComponent<Toggle>();
            if (v_toggle != null && v_toggle.group == null)
            {
                v_toggle.group = GetComponentInParent<ToggleGroup>();
            }
        }
    }
}
