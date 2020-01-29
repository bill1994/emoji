#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEditor;

namespace MaterialUI
{
    public class EditorSafeAreaSimulator
    {
        #region Helper Classes/Enums

        public enum SimDevice
        {
            None = 0,
            iPhoneX = 1,
            iPhoneXsMax = 2
        }

        public class SimDeviceInfo
        {
            public bool IsValid = true;
            public Rect SafeAreaTall = new Rect();
            public Rect SafeAreaWide = new Rect();
            public string SpritePathTall = "";
            public string SpritePathWide = "";

            public string EditorMenuItemPath = "";

            Sprite _loadedTallSprite = null;
            Sprite _loadedWideSprite = null;

            public Sprite GetOrLoadSimulatorSprite()
            {
                if (Application.isPlaying && IsValid)
                {
                    if (Screen.height > Screen.width)
                    {
                        if (_loadedTallSprite == null)
                            _loadedTallSprite = Resources.Load<Sprite>(SpritePathTall);
                        return _loadedTallSprite;
                    }
                    else
                    {
                        if (_loadedWideSprite == null)
                            _loadedWideSprite = Resources.Load<Sprite>(SpritePathWide);
                        return _loadedWideSprite;
                    }
                }
                return null;
            }

            public Rect GetNormalizedSafeArea()
            {
                if (Application.isPlaying && IsValid)
                {
                    if (Screen.height > Screen.width)
                    {
                        return SafeAreaTall;
                    }
                    else
                    {
                        return SafeAreaWide;
                    }
                }
                return new Rect(0, 0, 1, 1);
            }
        }

        #endregion

        #region Static Fields

        const string EDITORPREFS_SIMDEVICE_KEY = "CanvasSafeArea_SimDevice";
        const string SIMDEVICE_MENUITEM_NONE_KEY = "Window/MaterialUI/Notch Simulator/None";
        const string SIMDEVICE_MENUITEM_IPHONEX_KEY = "Window/MaterialUI/Notch Simulator/Iphone X";
        const string SIMDEVICE_MENUITEM_IPHONEXSMAX_KEY = "Window/MaterialUI/Notch Simulator/Iphone Xs Max";

        static Dictionary<SimDevice, SimDeviceInfo> s_SimulatorDevices = new Dictionary<SimDevice, SimDeviceInfo>()
        {
            {
                SimDevice.None,
                new SimDeviceInfo()
                {
                    IsValid = false,
                    EditorMenuItemPath = SIMDEVICE_MENUITEM_NONE_KEY
                }
            },
            {
                SimDevice.iPhoneX,
                new SimDeviceInfo()
                {
                    SafeAreaTall = new Rect (0f, 102f / 2436f, 1f, 2202f / 2436f),
                    SafeAreaWide = new Rect (132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f),
                    SpritePathTall = "simdevice_iphonex_tall",
                    SpritePathWide = "simdevice_iphonex_wide",
                    EditorMenuItemPath = SIMDEVICE_MENUITEM_IPHONEX_KEY
                }
            },
            {
                SimDevice.iPhoneXsMax,
                new SimDeviceInfo()
                {
                    SafeAreaTall = new Rect (0f, 102f / 2688f, 1f, 2454f / 2688f),
                    SafeAreaWide = new Rect (132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f),
                    SpritePathTall = "simdevice_iphonex_tall",
                    SpritePathWide = "simdevice_iphonex_wide",
                    EditorMenuItemPath = SIMDEVICE_MENUITEM_IPHONEXSMAX_KEY
                }
            }
        };

        static SimDevice s_SelectedSim = SimDevice.None;  // Editor Simulator Device

        private static List<CanvasSafeArea> s_safeAreasInScene = new List<CanvasSafeArea>();
        public static ReadOnlyCollection<CanvasSafeArea> SafeAreasInScene
        {
            get
            {
                return s_safeAreasInScene.AsReadOnly();
            }
        }

        #endregion

        #region Public Functions

        public static void RegisterSafeAreaComponent(CanvasSafeArea safeArea)
        {
            if (safeArea != null)
            {
                if (!s_safeAreasInScene.Contains(safeArea))
                {
                    s_safeAreasInScene.Add(safeArea);

                    if (s_safeAreasInScene.Count == 1)
                        LoadSimFromEditorPrefs();
                }
            }
        }

        public static void UnregisterSafeAreaComponent(CanvasSafeArea safeArea)
        {
            if (safeArea != null)
            {
                var index = s_safeAreasInScene.IndexOf(safeArea);
                if (index >= 0)
                    s_safeAreasInScene.RemoveAt(index);
            }
        }

        public static Rect GetNormalizedSafeArea()
        {
            SimDeviceInfo info = null;
            s_SimulatorDevices.TryGetValue(s_SelectedSim, out info);

            if (info != null && info.IsValid)
                return info.GetNormalizedSafeArea();

            return new Rect(0, 0, 1, 1);
        }

        public static Sprite GetOrLoadSimulatorSprite()
        {
            SimDeviceInfo info = null;
            s_SimulatorDevices.TryGetValue(s_SelectedSim, out info);

            if (info != null && info.IsValid)
                return info.GetOrLoadSimulatorSprite();

            return null;
        }

        public static void SetSelectedSimulator(SimDevice device)
        {
            //Save Editor Prefs
            if (device == SimDevice.None && UnityEditor.EditorPrefs.HasKey(EDITORPREFS_SIMDEVICE_KEY))
                UnityEditor.EditorPrefs.DeleteKey(EDITORPREFS_SIMDEVICE_KEY);
            else
                UnityEditor.EditorPrefs.SetInt(EDITORPREFS_SIMDEVICE_KEY, (int)device);

            s_SelectedSim = device;
            if (Application.isPlaying)
            {
                foreach (var safeAreas in s_safeAreasInScene)
                {
                    safeAreas.Refresh();
                }
            }
        }

        #endregion

        #region Helper Functions

        public static void LoadSimFromEditorPrefs()
        {
            if (UnityEditor.EditorPrefs.HasKey(EDITORPREFS_SIMDEVICE_KEY))
            {
                var value = UnityEditor.EditorPrefs.GetInt(EDITORPREFS_SIMDEVICE_KEY);
                if (System.Enum.IsDefined(typeof(SimDevice), value))
                {
                    SetSelectedSimulator((SimDevice)value);
                    return;
                }
            }
            SetSelectedSimulator(SimDevice.None);
        }

        #endregion

        #region MenuItems

        [MenuItem(SIMDEVICE_MENUITEM_NONE_KEY, true)]
        static bool CheckMenuItemSimulator_Active()
        {
            foreach (var pair in s_SimulatorDevices)
            {
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Value.EditorMenuItemPath))
                {
                    Menu.SetChecked(pair.Value.EditorMenuItemPath, pair.Key == s_SelectedSim);
                }
            }
            return true;
        }

        [MenuItem(SIMDEVICE_MENUITEM_NONE_KEY)]
        static void MenuItemSimulator_None()
        {
            SetSelectedSimulator(SimDevice.None);
        }

        [MenuItem(SIMDEVICE_MENUITEM_IPHONEX_KEY)]
        static void MenuItemSimulator_IPhoneX()
        {
            SetSelectedSimulator(SimDevice.iPhoneX);
        }

        [MenuItem(SIMDEVICE_MENUITEM_IPHONEXSMAX_KEY)]
        static void MenuItemSimulator_IPhoneXsMax()
        {
            SetSelectedSimulator(SimDevice.iPhoneXsMax);
        }

        #endregion
    }
}

#endif