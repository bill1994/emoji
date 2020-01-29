#if UNITY_NEW_INPUT_SYSTEM
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
#endif

using UnityEngine;

namespace Kyub.EventSystems
{
    public sealed class InputCompat
    {
        #region Static Properties

        public static string compositionString
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                return s_lastCompositionString.ToString();
#else
                return Input.compositionString;
#endif
            }
        }

        public static IMECompositionMode imeCompositionMode
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.imeSelected != null ? IMECompositionMode.On : IMECompositionMode.Off;
#else
                return Input.imeCompositionMode;
#endif
            }
            set
            {
#if UNITY_NEW_INPUT_SYSTEM
                var isOn = value == IMECompositionMode.On;
                if (Keyboard.current != null)
                    Keyboard.current.SetIMEEnabled(isOn);
#else
                Input.imeCompositionMode = value;
#endif
            }
        }

        public static Vector2 compositionCursorPos
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                //Impossible to get CursorPosition in new system
                return Vector2.zero;
#else
                return Input.compositionCursorPos;
#endif
            }
            set
            {
#if UNITY_NEW_INPUT_SYSTEM
                if (Keyboard.current != null)
                    Keyboard.current.SetIMECursorPosition(value);
#else
                Input.compositionCursorPos = value;
#endif
            }
        }

        public static bool mousePresent
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                return Mouse.current != null;
#else
                return Input.mousePresent;
#endif
            }
        }

        public static Vector2 mousePosition
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                return Pointer.current != null && Pointer.current.position != null ? Pointer.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static Vector2 mouseScrollDelta
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.scroll != null ? Mouse.current.scroll.ReadValue() : Vector2.zero;
#else
                return Input.mouseScrollDelta;
#endif
            }
        }

        public static bool touchSupported
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                return Touchscreen.current != null;
#else
                return Input.touchSupported;
#endif
            }
        }

        public static int touchCount
        {
            get
            {
#if UNITY_NEW_INPUT_SYSTEM
                return Touchscreen.current != null ? Touchscreen.current.touches.Count : 0;
#else
                return Input.touchCount;
#endif
            }
        }


        #endregion

        #region Static Constructors

#if UNITY_NEW_INPUT_SYSTEM
        static InputCompat()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += HandleOnSceneLoaded;
        }
#endif

        #endregion

        #region Public Static Functions

        public static bool GetMouseButtonDown(int button)
        {
#if UNITY_NEW_INPUT_SYSTEM
            ButtonControl control = GetMouseButtonControl(button);
            return control != null ? control.wasPressedThisFrame : false;
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        public static bool GetMouseButtonUp(int button)
        {
#if UNITY_NEW_INPUT_SYSTEM
            ButtonControl control = GetMouseButtonControl(button);
            return control != null ? control.wasReleasedThisFrame : false;
#else
            return Input.GetMouseButtonUp(button);
#endif
        }

        public static bool GetMouseButton(int button)
        {
#if UNITY_NEW_INPUT_SYSTEM
            ButtonControl control = GetMouseButtonControl(button);
            return control != null ? control.isPressed : false;
#else
            return Input.GetMouseButton(button);
#endif
        }

        public static Touch GetTouch(int index)
        {
#if UNITY_NEW_INPUT_SYSTEM
            TouchControl touch = null;
            if (Touchscreen.current != null && index >= 0 && index < Touchscreen.current.touches.Count)
                touch = Touchscreen.current.touches[index];

            return ConvertToLegacyTouch(touch);
#else
            return Input.GetTouch(index);
#endif
        }

        public static float GetAxisRaw(string axisName)
        {
#if UNITY_NEW_INPUT_SYSTEM
            if (axisName == "Horizontal")
            {
                if (Joystick.current != null)
                    return Joystick.current.stick.ReadValue().x;
                else if (Keyboard.current != null)
                {
                    var leftKey = Keyboard.current[Key.A];
                    var leftArrow = Keyboard.current[Key.LeftArrow];

                    var rightKey = Keyboard.current[Key.D];
                    var rightArrow = Keyboard.current[Key.RightArrow];

                    if (leftKey != null && leftKey.isPressed)
                        return leftKey.ReadValue();
                    else if (leftArrow != null && leftArrow.isPressed)
                        return leftArrow.ReadValue();
                    else if (rightKey != null && rightKey.isPressed)
                        return rightKey.ReadValue();
                    else if (rightArrow != null && rightArrow.isPressed)
                        return rightArrow.ReadValue();
                }
            }
            else if (axisName == "Vertical")
            {
                if (Joystick.current != null)
                    return Joystick.current.stick.ReadValue().y;
                else if (Keyboard.current != null)
                {
                    var upKey = Keyboard.current[Key.W];
                    var upArrow = Keyboard.current[Key.UpArrow];

                    var downKey = Keyboard.current[Key.S];
                    var downArrow = Keyboard.current[Key.DownArrow];

                    if (upKey != null && upKey.isPressed)
                        return upKey.ReadValue();
                    else if (upArrow != null && upArrow.isPressed)
                        return upArrow.ReadValue();
                    else if (downKey != null && downKey.isPressed)
                        return downKey.ReadValue();
                    else if (downArrow != null && downArrow.isPressed)
                        return downArrow.ReadValue();
                }
            }
            if (axisName == "Mouse X")
            {
                return Pointer.current != null && Pointer.current.delta != null ? Pointer.current.delta.ReadValue().x : 0;
            }
            else if (axisName == "Mouse Y")
            {
                return Pointer.current != null && Pointer.current.delta != null ? Pointer.current.delta.ReadValue().y : 0;
            }
            else if (axisName == "Mouse ScrollWheel")
            {
                if (Mouse.current != null)
                    return Mouse.current.scroll != null ? Mouse.current.scroll.ReadValue().y : 0;
                else if (Joystick.current != null)
                    return Joystick.current.twist != null ? Joystick.current.twist.ReadValue() : 0;
            }

            return 0;
#else
            return Input.GetAxisRaw(axisName);
#endif
        }

        public static bool GetButtonDown(string buttonName)
        {
#if UNITY_NEW_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key keyWithName = ConvertKeyFromName(buttonName, true);
                return keyWithName != Key.None && Keyboard.current[keyWithName] != null ? Keyboard.current[keyWithName].wasPressedThisFrame : false;
            }
            return false;
#else
            return Input.GetButtonDown(buttonName);
#endif
        }

        public static bool GetButtonUp(string buttonName)
        {
#if UNITY_NEW_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key keyWithName = ConvertKeyFromName(buttonName, true);
                return keyWithName != Key.None && Keyboard.current[keyWithName] != null ? Keyboard.current[keyWithName].wasReleasedThisFrame : false;
            }
            return false;
#else
            return Input.GetButtonUp(buttonName);
#endif
        }

        public static bool GetButton(string buttonName)
        {
#if UNITY_NEW_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key keyWithName = ConvertKeyFromName(buttonName, true);
                return keyWithName != Key.None && Keyboard.current[keyWithName] != null ? Keyboard.current[keyWithName].isPressed : false;
            }
            return false;
#else
            return Input.GetButton(buttonName);
#endif
        }

        public static bool GetKeyDown(KeyCode key)
        {
#if UNITY_NEW_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key keyWithName = ConvertKeyFromName(key.ToString());
                return keyWithName != Key.None && Keyboard.current[keyWithName] != null ? Keyboard.current[keyWithName].wasPressedThisFrame : false;
            }
            return false;
#else
            return Input.GetKeyDown(key);
#endif
        }

        public static bool GetKeyUp(KeyCode key)
        {
#if UNITY_NEW_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key keyWithName = ConvertKeyFromName(key.ToString());
                return keyWithName != Key.None && Keyboard.current[keyWithName] != null ? Keyboard.current[keyWithName].wasReleasedThisFrame : false;
            }
            return false;
#else
            return Input.GetKeyUp(key);
#endif
        }

        public static bool GetKey(KeyCode key)
        {
#if UNITY_NEW_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                Key keyWithName = ConvertKeyFromName(key.ToString());
                return keyWithName != Key.None && Keyboard.current[keyWithName] != null ? Keyboard.current[keyWithName].isPressed : false;
            }
            return false;
#else
            return Input.GetKey(key);
#endif
        }

        #endregion

        #region Helper Static Functions

#if UNITY_NEW_INPUT_SYSTEM
        static void RegisterEvents()
        {
            UnregisterEvents();
            if (Keyboard.current != null)
                Keyboard.current.onIMECompositionChange += HandleOnIMECompositionChange;
        }

        static void UnregisterEvents()
        {
            if (Keyboard.current != null)
                Keyboard.current.onIMECompositionChange -= HandleOnIMECompositionChange;
        }

        static Key ConvertKeyFromName(string name, bool supportCustom = false)
        {
            Key keyWithName = Key.None;
            if (Enum.TryParse(name, out keyWithName))
                return keyWithName;
            else if (supportCustom)
            {
                if (name == "Submit")
                    keyWithName = Key.Enter;
                else if (name == "Cancel")
                    keyWithName = Key.Escape;
            }

            return keyWithName;
        }

        static Touch ConvertToLegacyTouch(TouchControl touch)
        {
            Touch uiTouch = new Touch();
            if (touch != null)
            {
                var isPrimary = Touchscreen.current != null && touch == Touchscreen.current.primaryTouch;
                uiTouch = new Touch();
                uiTouch.type = isPrimary ? TouchType.Direct : TouchType.Indirect;
                uiTouch.altitudeAngle = 0;
                uiTouch.azimuthAngle = 0;
                uiTouch.deltaPosition = touch.delta != null ? touch.delta.ReadValue() : Vector2.zero;
                uiTouch.deltaTime = touch.startTime != null ? Time.realtimeSinceStartup - (float)touch.startTime.ReadValue() : 0;
                uiTouch.fingerId = touch.touchId != null ? touch.touchId.ReadValue() : 0;
                uiTouch.maximumPossiblePressure = 1f;

                UnityEngine.TouchPhase touchPhase;
                if (touch == null || !Enum.TryParse(touch.phase.ReadValue().ToString(), out touchPhase))
                    touchPhase = (UnityEngine.TouchPhase)0;
                uiTouch.phase = touchPhase;
                uiTouch.position = touch.position != null ? touch.position.ReadValue() : Vector2.zero;
                uiTouch.pressure = touch.pressure != null ? touch.pressure.ReadValue() : 0;

                var pressureRadiusValue = touch.pressure != null ? touch.radius.ReadValue() : Vector2.zero;
                uiTouch.radius = Mathf.Max(pressureRadiusValue.x, pressureRadiusValue.y);
                uiTouch.radiusVariance = Mathf.Abs(pressureRadiusValue.x - pressureRadiusValue.y);
                uiTouch.rawPosition = uiTouch.position;
                uiTouch.tapCount = touch.tapCount != null ? touch.tapCount.ReadValue() : 0;
            }
            return uiTouch;
        }

        static ButtonControl GetMouseButtonControl(int button)
        {
            ButtonControl control = null;
            if (Mouse.current != null)
            {
                if (button == (int)MouseButton.Left)
                    control = Mouse.current.leftButton;
                else if (button == (int)MouseButton.Right)
                    control = Mouse.current.rightButton;
                else if (button == (int)MouseButton.Middle)
                    control = Mouse.current.middleButton;
                else if (button == (int)MouseButton.Forward)
                    control = Mouse.current.forwardButton;
                else if (button == (int)MouseButton.Back)
                    control = Mouse.current.backButton;
            }
            else if (Touchscreen.current != null)
            {
                if (button >= 0 && button < Touchscreen.current.touches.Count)
                {
                    var touch = Touchscreen.current.touches[button];
                    if (touch != null)
                        control = touch.press;
                }
            }
            else if (button == 0 && Pointer.current != null)
            {
                control = Pointer.current.press;
            }
            return control;
        }
#endif

        #endregion

        #region Receivers

#if UNITY_NEW_INPUT_SYSTEM
        static void HandleOnSceneLoaded(Scene scene1, Scene scene2)
        {
            RegisterEvents();
        }

        static IMECompositionString s_lastCompositionString = new IMECompositionString(string.Empty);
        static void HandleOnIMECompositionChange(IMECompositionString compositionString)
        {
            s_lastCompositionString = compositionString;
        }
#endif

        #endregion

        #region Unity Compiler

#if UNITY_EDITOR

        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded()
        {
            UnityEditor.SerializedProperty enableNativePlatformBackendsForNewInputSystem = null;
            UnityEditor.SerializedProperty disableOldInputManagerSupport = null;
            var allPlayerSettings = Resources.FindObjectsOfTypeAll<UnityEditor.PlayerSettings>();
            if (allPlayerSettings.Length > 0)
            {
                var playerSettings = Resources.FindObjectsOfTypeAll<UnityEditor.PlayerSettings>()[0];
                var so = new UnityEditor.SerializedObject(playerSettings);
                enableNativePlatformBackendsForNewInputSystem = so.FindProperty("enableNativePlatformBackendsForNewInputSystem");
                disableOldInputManagerSupport = so.FindProperty("disableOldInputManagerSupport");
            }

            var supportNewInputSystem = enableNativePlatformBackendsForNewInputSystem == null || enableNativePlatformBackendsForNewInputSystem.boolValue;
            var supportOldInputSystem = !(disableOldInputManagerSupport == null || disableOldInputManagerSupport.boolValue);

            if (supportNewInputSystem && !supportOldInputSystem)
                AddDefineSymbols(new string[] { "UNITY_NEW_INPUT_SYSTEM" });
            else
                RemoveDefineSymbols(new string[] { "UNITY_NEW_INPUT_SYSTEM" });
        }

        static void AddDefineSymbols(string[] symbols)
        {
            string definesString = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            allDefines.AddRange(symbols.Except(allDefines));
            UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(
                UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", allDefines.ToArray()));
        }

        static void RemoveDefineSymbols(string[] symbols)
        {
            string definesString = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);
            List<string> allDefines = definesString.Split(';').ToList();
            foreach (var symbol in symbols)
            {
                allDefines.Remove(symbol);
            }
            UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(
                UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup,
                string.Join(";", allDefines.ToArray()));
        }
#endif

        #endregion
    }
}