using UnityEngine.EventSystems;
using UnityEngine;

namespace Kyub.EventSystems
{
    /// <summary>
    /// Interface to the Input system used by the BaseInputModule. With this it is possible to bypass the Input system with your own but still use the same InputModule. For example this can be used to feed fake input into the UI or interface with a different input system.
    /// </summary>
    public class BaseInputCompat : BaseInput
    {
        #region Overriden Properties

        public override string compositionString
        {
            get { return InputCompat.compositionString; }
        }

        public override IMECompositionMode imeCompositionMode
        {
            get { return InputCompat.imeCompositionMode; }
            set
            {
                InputCompat.imeCompositionMode = value;
            }
        }

        public override Vector2 compositionCursorPos
        {
            get { return InputCompat.compositionCursorPos; }
            set
            {
                InputCompat.compositionCursorPos = value;
            }
        }

        public override bool mousePresent
        {
            get { return InputCompat.mousePresent; }
        }

        public override Vector2 mousePosition
        {
            get
            {
                return InputCompat.mousePosition;
            }
        }

        public override Vector2 mouseScrollDelta
        {
            get
            {
                return InputCompat.mouseScrollDelta;
            }
        }

        public override bool touchSupported
        {
            get
            {
                return InputCompat.touchSupported;
            }
        }

        public override int touchCount
        {
            get { return InputCompat.touchCount; }
        }

        #endregion

        #region Overriden Functions

        public override bool GetMouseButtonDown(int button)
        {
            return InputCompat.GetMouseButtonDown(button);
        }

        public override bool GetMouseButtonUp(int button)
        {
            return InputCompat.GetMouseButtonUp(button);
        }

        public override bool GetMouseButton(int button)
        {
            return InputCompat.GetMouseButton(button);
        }

        public override Touch GetTouch(int index)
        {
            return InputCompat.GetTouch(index);
        }

        public override float GetAxisRaw(string axisName)
        {
            return InputCompat.GetAxisRaw(axisName);
        }

        public override bool GetButtonDown(string buttonName)
        {
            return InputCompat.GetButtonDown(buttonName);
        }

        public virtual bool GetButtonUp(string buttonName)
        {
            return InputCompat.GetButtonUp(buttonName);
        }

        public virtual bool GetButton(string buttonName)
        {
            return InputCompat.GetButton(buttonName);
        }

        public virtual bool GetKeyDown(KeyCode key)
        {
            return InputCompat.GetKeyDown(key);
        }

        public virtual bool GetKeyUp(KeyCode key)
        {
            return InputCompat.GetKeyUp(key);
        }

        public virtual bool GetKey(KeyCode key)
        {
            return InputCompat.GetKey(key);
        }

        #endregion
    }
}