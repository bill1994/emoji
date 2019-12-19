using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace MaterialUI
{
    [CreateAssetMenu(fileName = "ResourcePrefabsDatabase", menuName = "MaterialUI/Resources Address Database")]
    public class ResourcePrefabsDatabase : ScriptableObject
    {
        #region Private Variables

        /// <summary>
        /// Path to the circular progress indicator prefab.
        /// </summary>
        public const string CIRCLE_PROGRESS_INDICATOR_PATH = "Progress Indicators/Circle Progress Indicator";
        
        /// <summary>
        /// Path to the linear progress indicator prefab.
        /// </summary>
        public const string LINEAR_PROGRESS_INDICATOR_PATH = "Progress Indicators/Linear Progress Indicator";

        /// Path to the modal progress circle dialog prefab.
        /// </summary>
        public const string DIALOG_MODAL_CIRCLE_PROGRESS_PATH = "Dialogs/DialogCircleModalProgress"; //"Progress Indicators/Modal Circle Progress Indicator";

        /// <summary>
        /// Path to the alert dialog prefab.
        /// </summary>
        public const string DIALOG_ALERT_PATH = "Dialogs/DialogAlert";

        /// <summary>
        /// Path to the progress dialog prefab.
        /// </summary>
        public const string DIALOG_PROGRESS_PATH = "Dialogs/DialogProgress";

        /// <summary>
        /// Path to the simple list dialog prefab.
        /// </summary>
        public const string DIALOG_SIMPLE_LIST_PATH = "Dialogs/DialogSimpleList";

        /// <summary>
        /// Path to the checkbox list dialog prefab.
        /// </summary>
        public const string DIALOG_CHECKBOX_LIST_PATH = "Dialogs/DialogCheckboxList";

        /// <summary>
        /// Path to the radio list dialog prefab.
        /// </summary>
        public const string DIALOG_RADIO_LIST_PATH = "Dialogs/DialogRadioList";

        /// <summary>
        /// Path to the time picker dialog prefab.
        /// </summary>
        public const string DIALOG_TIME_PICKER_PATH = "Dialogs/Pickers/DialogTimePicker";

        /// <summary>
        /// Path to the date picker dialog prefab.
        /// </summary>
        public const string DIALOG_DATE_PICKER_PATH = "Dialogs/Pickers/DialogDatePicker";

        /// <summary>
        /// Path to the month picker dialog prefab.
        /// </summary>
        public const string DIALOG_MONTH_PICKER_PATH = "Dialogs/Pickers/DialogMonthPicker";

        /// <summary>
        /// Path to the prompt dialog prefab.
        /// </summary>
        public const string DIALOG_PROMPT_PATH = "Dialogs/DialogPrompt";

        /// <summary>
        /// Path to the disabled panel prefab.
        /// </summary>
        public const string DISABLED_PANEL_PATH = "DisabledPanel";

        /// <summary>
        /// Path to the slider dot prefab.
        /// </summary>
        public const string SLIDER_DOT_PATH = "SliderDot";
        /// <summary>
        /// Path to the dropdown panel prefab.
        /// </summary>
        public const string DROPDOWN_PANEL_PATH = "Menus/Dropdown Panel";

        /// <summary>
        /// Path to the snackbar prefab.
        /// </summary>
        public const string SNACKBAR_PATH = "Snackbar";
        /// <summary>
        /// Path to the toast prefab.
        /// </summary>
        public const string TOAST_PATH = "Toast";

        /*/// <summary>
        /// Path to the circular progress indicator prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_progressIndicatorCircular = new PrefabAddress() { AssetPath = "Progress Indicators/Circle Progress Indicator" };
        
        /// <summary>
        /// Path to the linear progress indicator prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_progressIndicatorLinear = new PrefabAddress() { AssetPath = "Progress Indicators/Linear Progress Indicator" };
        
        /// Path to the modal progress indicator prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_progressIndicatorModalCircular = new PrefabAddress() { AssetPath = "Progress Indicators/Modal Circle Progress Indicator" };
        
        /// <summary>
        /// Path to the alert dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogAlert = new PrefabAddress() { AssetPath = "Dialogs/DialogAlert" };
        
        /// <summary>
        /// Path to the progress dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogProgress = new PrefabAddress() { AssetPath = "Dialogs/DialogProgress" };
        
        /// <summary>
        /// Path to the simple list dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogSimpleList = new PrefabAddress() { AssetPath = "Dialogs/DialogSimpleList" };
        
        /// <summary>
        /// Path to the checkbox list dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogCheckboxList = new PrefabAddress() { AssetPath = "Dialogs/DialogCheckboxList" };
        
        /// <summary>
        /// Path to the radio list dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogRadioList = new PrefabAddress() { AssetPath = "Dialogs/DialogRadioList" };
        
        /// <summary>
        /// Path to the time picker dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogTimePicker = new PrefabAddress() { AssetPath = "Dialogs/Pickers/DialogTimePicker" };
        
        /// <summary>
        /// Path to the date picker dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogDatePicker = new PrefabAddress() { AssetPath = "Dialogs/Pickers/DialogDatePicker" };
        
        /// <summary>
        /// Path to the month picker dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogMonthPicker = new PrefabAddress() { AssetPath = "Dialogs/Pickers/DialogMonthPicker" };
        
        /// <summary>
        /// Path to the prompt dialog prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dialogPrompt = new PrefabAddress() { AssetPath = "Dialogs/DialogPrompt" };

        /// <summary>
        /// Path to the disabled panel prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_disabledPanel = new PrefabAddress() { AssetPath = "DisabledPanel" };
        
        /// <summary>
        /// Path to the slider dot prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_sliderDot = new PrefabAddress() { AssetPath = "SliderDot" };
        /// <summary>
        /// Path to the dropdown panel prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_dropdownPanel = new PrefabAddress() { AssetPath = "Menus/Dropdown Panel" };

        /// <summary>
        /// Path to the snackbar prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_snackbar = new PrefabAddress() { AssetPath = "Snackbar" };
        /// <summary>
        /// Path to the toast prefab.
        /// </summary>
        [SerializeField]
        PrefabAddress m_toast = new PrefabAddress() { AssetPath = "Toast" };*/

        [SerializeField]
        List<PrefabAddress> m_AssetAddresses = new List<PrefabAddress>()
        {
            new PrefabAddress() { AssetPath = CIRCLE_PROGRESS_INDICATOR_PATH },
            new PrefabAddress() { AssetPath = LINEAR_PROGRESS_INDICATOR_PATH },
            new PrefabAddress() { AssetPath = DIALOG_MODAL_CIRCLE_PROGRESS_PATH },
            new PrefabAddress() { AssetPath = DIALOG_ALERT_PATH },
            new PrefabAddress() { AssetPath = DIALOG_PROGRESS_PATH },
            new PrefabAddress() { AssetPath = DIALOG_SIMPLE_LIST_PATH },
            new PrefabAddress() { AssetPath = DIALOG_CHECKBOX_LIST_PATH },
            new PrefabAddress() { AssetPath = DIALOG_RADIO_LIST_PATH },
            new PrefabAddress() { AssetPath = DIALOG_TIME_PICKER_PATH },
            new PrefabAddress() { AssetPath = DIALOG_DATE_PICKER_PATH },
            new PrefabAddress() { AssetPath = DIALOG_MONTH_PICKER_PATH },
            new PrefabAddress() { AssetPath = DIALOG_PROMPT_PATH },
            new PrefabAddress() { AssetPath = DISABLED_PANEL_PATH },
            new PrefabAddress() { AssetPath = SLIDER_DOT_PATH},
            new PrefabAddress() { AssetPath = DROPDOWN_PANEL_PATH },
            new PrefabAddress() { AssetPath = SNACKBAR_PATH },
            new PrefabAddress() { AssetPath = TOAST_PATH },
        };

        [System.NonSerialized]
        Dictionary<string, PrefabAddress> _AddressCache = null;

        #endregion

        #region Public Properties

        public PrefabAddress progressIndicatorCircular { get => GetOrCreatePrefabAddressWithPath_Internal(CIRCLE_PROGRESS_INDICATOR_PATH); }
        public PrefabAddress progressIndicatorLinear { get => GetOrCreatePrefabAddressWithPath_Internal(LINEAR_PROGRESS_INDICATOR_PATH); }
        public PrefabAddress dialogModalCircleProgress { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_MODAL_CIRCLE_PROGRESS_PATH); }
        public PrefabAddress dialogAlert { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_ALERT_PATH); }
        public PrefabAddress dialogProgress { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_PROGRESS_PATH); }
        public PrefabAddress dialogSimpleList { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_SIMPLE_LIST_PATH); }
        public PrefabAddress dialogCheckboxList { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_CHECKBOX_LIST_PATH); }
        public PrefabAddress dialogRadioList { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_RADIO_LIST_PATH); }
        public PrefabAddress dialogTimePicker { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_TIME_PICKER_PATH); }
        public PrefabAddress dialogDatePicker { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_DATE_PICKER_PATH); }
        public PrefabAddress dialogMonthPicker { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_MONTH_PICKER_PATH); }
        public PrefabAddress dialogPrompt { get => GetOrCreatePrefabAddressWithPath_Internal(DIALOG_PROMPT_PATH); }
        public PrefabAddress disabledPanel { get => GetOrCreatePrefabAddressWithPath_Internal(DISABLED_PANEL_PATH); }
        public PrefabAddress sliderDot { get => GetOrCreatePrefabAddressWithPath_Internal(SLIDER_DOT_PATH); }
        public PrefabAddress dropdownPanel { get => GetOrCreatePrefabAddressWithPath_Internal(DROPDOWN_PANEL_PATH); }
        public PrefabAddress snackbar { get => GetOrCreatePrefabAddressWithPath_Internal(SNACKBAR_PATH); }
        public PrefabAddress toast { get => GetOrCreatePrefabAddressWithPath_Internal(TOAST_PATH); }

        public IReadOnlyList<PrefabAddress> assetAddresses { get => m_AssetAddresses.AsReadOnly(); }

        #endregion

        #region Unity Functions

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            RecreateCache();
        }
#endif

        #endregion

        #region Public Functions

        public bool ClearCache()
        {
            var v_result = false;
            if (m_AssetAddresses != null)
            {
                foreach (var v_address in m_AssetAddresses)
                {
                    if(v_address.KeepLoaded)
                        v_result = v_address.ClearCache() || v_result;
                }
            }

            return v_result;
        }

        public PrefabAddress GetPrefabAddressWithName(string p_addressName)
        {
            if (_AddressCache == null)
                RecreateCache();

            if (_AddressCache != null)
            {
                PrefabAddress v_address = null;
                _AddressCache.TryGetValue(p_addressName, out v_address);

                return v_address;
            }

            return null;
        }

        public bool ContainsName(string p_addressName)
        {
            if (_AddressCache == null)
                RecreateCache();

            return _AddressCache.ContainsKey(p_addressName);
        }

        #endregion

        #region Internal Helper Functions

        //Used only to create default assets
        protected PrefabAddress GetOrCreatePrefabAddressWithPath_Internal(string p_addressName)
        {
            var v_address = GetPrefabAddressWithName(System.IO.Path.GetFileNameWithoutExtension(p_addressName));

            if ((v_address == null || v_address.IsEmpty()) && !string.IsNullOrEmpty(p_addressName))
            {
                if (v_address == null)
                {
                    v_address = new PrefabAddress() { AssetPath = p_addressName, KeepLoaded = true };
                    m_AssetAddresses.Insert(0, v_address);
                }
                else
                    v_address.AssetPath = p_addressName;

                //v_address.Validate();
                _AddressCache[v_address.Name] = v_address;
            }
                
            return v_address;
        }

        protected virtual void RecreateCache()
        {
            _AddressCache = new Dictionary<string, PrefabAddress>();

            foreach (var v_address in m_AssetAddresses)
            {
                if (v_address != null && !string.IsNullOrEmpty(v_address.Name))
                {
                    _AddressCache[v_address.Name] = v_address;
                }
            }
        }


        #endregion
    }
}
