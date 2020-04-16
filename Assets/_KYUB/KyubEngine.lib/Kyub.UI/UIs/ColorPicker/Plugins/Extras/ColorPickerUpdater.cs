using UnityEngine;
using System.Collections;

namespace Kyub.UI
{
    public class ColorPickerUpdater : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            UpdateColorPicker();
        }

        protected virtual void Update()
        {
            TryUpdateColorPicker();
        }

        protected bool _isDirty = true;
        public virtual void SetDirty()
        {
            _isDirty = true;
        }

        public void TryUpdateColorPicker(bool p_force = false)
        {
            if (_isDirty || p_force)
            {
                _isDirty = false;
                UpdateColorPicker();
            }
        }

        protected virtual void UpdateColorPicker()
        {
            var v_picker = GetComponent<Kyub.UI.ColorPicker>();
            if (v_picker != null)
            {
                v_picker.UpdateUI();
            }
        }
    }
}
