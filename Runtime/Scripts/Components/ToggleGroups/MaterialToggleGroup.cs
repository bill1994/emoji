using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace MaterialUI
{

    public class MaterialToggleGroup : UIBehaviour
    {
        [System.Serializable]
        public class IntUnityEvent : UnityEngine.Events.UnityEvent<int> { };

        public enum EnsureStateModeEnum { None, OnEnable, Start, Both }

        #region Private Variables

        [SerializeField]
        protected bool m_AllowSwitchOff = false;
        [SerializeField]
        protected int m_SelectedIndex = -1;
        [Space]
        [SerializeField, Tooltip("When script try prevent wrong states")]
        EnsureStateModeEnum m_EnsureStateMode = EnsureStateModeEnum.Both;
        [SerializeField, Tooltip("Sort registered members by hierarchy depth, so the selected index will be the same as sibling index")]
        bool m_SortByHierarchyDepth = true;

        protected List<ToggleBase> _Toggles = new List<ToggleBase>();
        protected bool _indexIsDirty = false;
        protected bool _HasStarted = false;

        #endregion

        #region Callbacks

        [Header("Callbacks")]
        public IntUnityEvent onSelectedIndexChanged = new IntUnityEvent();

        #endregion

        #region Properties

        public int selectedIndex
        {
            get
            {
                return selectedIndexInternal;
            }
            set
            {
                if (selectedIndexInternal == value)
                    return;

                selectedIndexInternal = value;
                _indexIsDirty = true;

                if (gameObject.activeInHierarchy && enabled && !IsInvoking("TryApplyIndexDirty"))
                    Invoke("TryApplyIndexDirty", 0);
            }
        }

        public EnsureStateModeEnum ensureStateMode
        {
            get
            {
                return m_EnsureStateMode;
            }
            set
            {
                if (m_EnsureStateMode == value)
                    return;

                m_EnsureStateMode = value;
            }
        }

        public bool allowSwitchOff
        {
            get
            {
                return m_AllowSwitchOff;
            }
            set
            {
                if (m_AllowSwitchOff == value)
                    return;
                m_AllowSwitchOff = value;
                ApplyGroupAllowSwitchOffOnAll();
            }
        }

        public bool sortByHierarchyDepth
        {
            get
            {
                return m_SortByHierarchyDepth;
            }
            set
            {
                if (m_SortByHierarchyDepth == value)
                    return;
                m_SortByHierarchyDepth = value;
                if (_HasStarted && m_SortByHierarchyDepth && Application.isPlaying)
                    SortRegisteredMembersDelayed();
            }
        }

        protected virtual int selectedIndexInternal
        {
            get
            {
                return m_SelectedIndex;
            }
            set
            {
                m_SelectedIndex = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            if (ensureStateMode == EnsureStateModeEnum.OnEnable ||
                (_HasStarted && ensureStateMode == EnsureStateModeEnum.Both))
            {
                EnsureValidStateDelayed();
            }
            else if (_HasStarted)
            {
                TryApplyIndexDirty();
            }

            base.OnEnable();
        }

        protected override void Start()
        {
            _HasStarted = true;

            if (!_indexIsDirty)
            {
                if (GetCurrentSelectedToggle(false) != null)
                    SetToggleValue(selectedIndexInternal, true, true);
                else if (!m_AllowSwitchOff)
                    SetToggleValue(ActiveToggles().FirstOrDefault(), true, true);
            }

            //CallOnISelectedIndexChangedEvent();
            if (ensureStateMode == EnsureStateModeEnum.Start || ensureStateMode == EnsureStateModeEnum.Both)
            {
                EnsureValidState();
            }
            else
            {
                TryApplyIndexDirty();
            }

            base.Start();
        }

        protected override void OnDisable()
        {
            CancelInvoke();
            base.OnDisable();
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            UnityEditor.EditorApplication.update -= EditorOnValidateDelayed;
            if (this.gameObject.activeInHierarchy)
                UnityEditor.EditorApplication.update += EditorOnValidateDelayed;
        }

        protected virtual void OnValidateDelayed()
        {
            if (Application.isPlaying)
            {
                if (gameObject.activeInHierarchy && enabled)
                {
                    if (GetCurrentSelectedToggle(false) != null || m_AllowSwitchOff)
                        SetToggleValue(selectedIndexInternal, true, true);
                    else if (!m_AllowSwitchOff)
                        SetToggleValue(ActiveToggles().FirstOrDefault(), true, true);
                }
                else
                {
                    TryApplyIndexDirty(true);
                }
                ApplyGroupAllowSwitchOffOnAll();
            }
        }

        void EditorOnValidateDelayed()
        {
            //Unregister Validate Delayed
            UnityEditor.EditorApplication.update -= EditorOnValidateDelayed;
            if (this != null)
            {
                OnValidateDelayed();
            }
        }

#endif

        #endregion

        #region Public Functions

        public int GetToggleIndex(ToggleBase toggle)
        {
            return _Toggles.IndexOf(toggle);
        }

        public virtual bool IsActiveAndEnabledInHierarchy()
        {
            return enabled && gameObject.activeInHierarchy;
        }

        public virtual ToggleBase[] GetRegisteredToggles()
        {
            return _Toggles.ToArray();
        }

        public virtual ToggleBase GetCurrentSelectedToggle(bool ensureValidToggle)
        {
            if (_Toggles == null || _Toggles.Count == 0)
            {
                if (ensureValidToggle)
                    SetSelectedIndexInternal(-1);
                return null;
            }

            var toggle = selectedIndexInternal >= 0 && selectedIndexInternal < _Toggles.Count ? _Toggles[selectedIndexInternal] : null;
            //Valid Toggle
            if (!ensureValidToggle || (toggle != null && toggle.isOn))
            {
                return toggle;
            }
            else
            {
                //No Toggles On
                if (!AnyTogglesOn())
                {
                    //the current toggle in index is null
                    if (toggle == null)
                    {
                        EnsureValidState();
                        toggle = selectedIndexInternal >= 0 && selectedIndexInternal < _Toggles.Count ? _Toggles[selectedIndexInternal] : null;
                    }
                    //the current toggle in index is not null so we can activate him
                    else
                    {
                        toggle.isOn = true;
                        NotifyToggleValueChanged(toggle, true);
                    }
                }
                //Has a toggle On so we must change the index
                else
                {
                    for (int i = 0; i < _Toggles.Count; i++)
                    {
                        if (_Toggles[i] != null && _Toggles[i].isOn)
                        {
                            SetSelectedIndexInternal(i);
                            toggle = _Toggles[i];
                            break;
                        }
                    }
                }
            }
            return toggle;
        }

        public virtual bool IsToggleInGroup(ToggleBase toggle)
        {
            if (toggle == null || !_Toggles.Contains(toggle))
            {
                return false;
            }
            return true;
        }

        public bool CanToggleValueChange(ToggleBase toggle, bool newValue)
        {
            if (toggle == null)
                return false;

            if (!IsActiveAndEnabledInHierarchy() || !IsToggleInGroup(toggle))
                return true;

            //Can't change value 
            if (!newValue && !allowSwitchOff && _Toggles.Count != 0)
            {
                var currentSelectedToggle = GetCurrentSelectedToggle(true);
                return currentSelectedToggle != toggle;
            }

            return true;

        }

        public virtual bool NotifyToggleValueChanged(ToggleBase toggle, bool sendCallback = true)
        {
            if (!IsActiveAndEnabledInHierarchy() || !IsToggleInGroup(toggle))
                return false;

            //Only make sense when disabled toggle is the current selected
            if (!toggle.isOn)
            {
                if (toggle == GetCurrentSelectedToggle(false))
                {
                    if (m_AllowSwitchOff)
                    {
                        SetSelectedIndexInternal(-1);
                        return true;
                    }
                    //Invalid State, we must revert this to true
                    /*else
                    {
                        toggle.isOn = true;
                    }*/
                }
                return false;
            }
            else
            {
                //Update current index
                SetSelectedIndexInternal(_Toggles.IndexOf(toggle));
                // disable all toggles in the group
                for (var i = 0; i < _Toggles.Count; i++)
                {
                    //Update current index
                    if (_Toggles[i] == toggle)
                        continue;

                    if (sendCallback)
                        _Toggles[i].isOn = false;
                    else
                        _Toggles[i].SetIsOnWithoutNotify(false);
                }
            }

            return true;
        }

        public bool UnregisterToggle(ToggleBase toggle)
        {
            if (toggle != null && Application.isPlaying && _Toggles.Contains(toggle))
            {
                var toggleIndex = _Toggles.IndexOf(toggle);
                return UnregisterToggle(toggleIndex);
            }
            return false;
        }

        protected bool UnregisterToggle(int toggleIndex)
        {
            if (toggleIndex >= 0 && toggleIndex < _Toggles.Count && Application.isPlaying)
            {
                _Toggles.RemoveAt(toggleIndex);

                if (toggleIndex >= 0)
                {
                    //We must pick another toggle to be the index selected
                    if (selectedIndexInternal == toggleIndex)
                        selectedIndexInternal = -1;
                    //Just correct old selected index to new one
                    else if (toggleIndex < selectedIndexInternal)
                        selectedIndexInternal--;
                }

                EnsureValidStateDelayed();

                return true;
            }
            return false;
        }

        public bool RegisterToggle(ToggleBase toggle)
        {
            if (toggle != null && Application.isPlaying && !_Toggles.Contains(toggle))
            {
                _Toggles.Add(toggle);
                toggle.ApplyGroupAllowSwitchOff();
                if (m_SortByHierarchyDepth)
                    SortRegisteredMembersDelayed();
                EnsureValidStateDelayed();

                return true;
            }
            return false;
        }

        public virtual void EnsureValidStateDelayed()
        {
            if (!IsActiveAndEnabledInHierarchy())
                return;

            //Invoke in Next Update
            CancelInvoke("EnsureValidState");
            Invoke("EnsureValidState", 0);
        }

        public virtual void EnsureValidState()
        {
            if (!IsActiveAndEnabledInHierarchy())
            {
                CancelInvoke("EnsureValidState");
                return;
            }

            TryApplyIndexDirty();

            //Remove Self Disabled Toggles or Nulls
            for (int i = 0; i < _Toggles.Count; i++)
            {
                if (_Toggles[i] == null || !_Toggles[i].enabled || !_Toggles[i].gameObject.activeSelf)
                {
                    if (UnregisterToggle(i))
                    {
                        i--;
                    }
                }
            }

            CancelInvoke("EnsureValidState");

            if (!allowSwitchOff && !AnyTogglesOn() && _Toggles.Count != 0)
            {
                int toggleIndex = 0;

                //Get first Non-null toggle
                while (toggleIndex >= 0 && toggleIndex < _Toggles.Count && _Toggles[toggleIndex] == null)
                {
                    toggleIndex++;
                }

                var toggle = _Toggles[toggleIndex];
                SetSelectedIndexInternal(toggleIndex);
                if (toggle != null)
                {
                    toggle.isOn = true;
                    NotifyToggleValueChanged(toggle);
                }
            }
            SortRegisteredMembers();
        }

        public bool AnyTogglesOn()
        {
            var currentToggle = GetCurrentSelectedToggle(false);
            if (currentToggle == null || !currentToggle.isOn)
                return _Toggles.Find(x => x.isOn) != null;

            return true;

        }

        public IEnumerable<ToggleBase> ActiveToggles()
        {
            return _Toggles.Where(x => x.isOn);
        }

        public bool SetToggleValue(int toggleIndex, bool isOn, bool sendCallback = true)
        {
            var toggle = toggleIndex >= 0 && toggleIndex < _Toggles.Count ? _Toggles[toggleIndex] : null;

            return SetToggleValue(toggle, isOn, sendCallback);
        }

        public bool SetToggleValue(ToggleBase toggle, bool isOn, bool sendCallback = true)
        {
            //Prevent problem when switching off
            if (toggle == null && isOn && !m_AllowSwitchOff)
            {
                toggle = GetCurrentSelectedToggle(false);
                if (toggle == null)
                {
                    //Pick first active
                    toggle = ActiveToggles().FirstOrDefault();
                    //Pick first possible toggle
                    if (toggle == null)
                        toggle = _Toggles.Find((a) => a != null);
                }
            }

            //Disable all toggles (no other option
            if (toggle == null && isOn)
            {
                SetAllTogglesOff(sendCallback);
                return true;
            }
            else
            {
                if (!IsToggleInGroup(toggle))
                    return false;

                var oldValue = toggle.isOn;
                if (sendCallback)
                    toggle.isOn = isOn;
                else
                    toggle.SetIsOnWithoutNotify(isOn);

                NotifyToggleValueChanged(toggle, sendCallback);

                return true;
            }
        }

        #endregion

        #region  Helper Functions

        protected virtual void TryApplyIndexDirty()
        {
            TryApplyIndexDirty(true);
        }

        //Delayed SetCurrentIndex
        protected virtual void TryApplyIndexDirty(bool force)
        {
            if (IsInvoking("TryApplyIndexDirty"))
                CancelInvoke("TryApplyIndexDirty");

            if (_indexIsDirty || force)
            {
                _indexIsDirty = false;
                SortRegisteredMembers(false);
                SetToggleValue(selectedIndexInternal, true, true);
            }
        }

        protected void SetAllTogglesOff(bool sendCallback = true)
        {
            SetSelectedIndexInternal(-1);
            bool oldAllowSwitchOff = m_AllowSwitchOff;
            m_AllowSwitchOff = true;

            if (sendCallback)
            {
                for (var i = 0; i < _Toggles.Count; i++)
                    _Toggles[i].isOn = false;
            }
            else
            {
                for (var i = 0; i < _Toggles.Count; i++)
                    _Toggles[i].SetIsOnWithoutNotify(false);
            }

            m_AllowSwitchOff = oldAllowSwitchOff;
        }

        protected void SetSelectedIndexInternal(int value)
        {
            _indexIsDirty = false;
            if (selectedIndexInternal == value)
                return;

            selectedIndexInternal = value;
            CancelInvoke("CallOnSelectedIndexChangedEvent");
            Invoke("CallOnSelectedIndexChangedEvent", 0);
        }

        protected void CallOnSelectedIndexChangedEvent()
        {
            CancelInvoke("CallOnSelectedIndexChangedEvent");
            if (onSelectedIndexChanged != null)
                onSelectedIndexChanged.Invoke(selectedIndexInternal);
        }

        protected void SortRegisteredMembersDelayed()
        {
            CancelInvoke("SortRegisteredMembers");
            Invoke("SortRegisteredMembers", 0);
        }

        protected void SortRegisteredMembers()
        {
            SortRegisteredMembers(true);
        }

        protected void SortRegisteredMembers(bool keepSameToggle)
        {
            //CancelInvoke("CallOnSelectedIndexChangedEvent");
            var toggle = GetCurrentSelectedToggle(false);

            _Toggles.Sort((a, b) =>
            {
                var aPair = GetHierarchyDepth(a);
                var bPair = GetHierarchyDepth(b);

                if (aPair.Key == bPair.Key)
                    return aPair.Value.CompareTo(bPair.Value);
                else
                    return aPair.Key.CompareTo(bPair.Key);
            });

            if (keepSameToggle)
            {
                //Keep same selected toggle
                if (!allowSwitchOff && Application.isPlaying && toggle == null)
                    toggle = GetCurrentSelectedToggle(true);
                selectedIndexInternal = toggle != null ? _Toggles.IndexOf(toggle) : -1;
            }
        }

        protected KeyValuePair<int, int> GetHierarchyDepth(ToggleBase toggle)
        {
            int depth = -1;
            int sibling = toggle != null ? toggle.transform.GetSiblingIndex() : -1;
            var transform = toggle != null ? toggle.transform : null;
            while (transform != null)
            {
                depth++;
                transform = transform.parent;
            }

            return new KeyValuePair<int, int>(depth, sibling);
        }

        protected virtual void ApplyGroupAllowSwitchOffOnAll()
        {
            foreach (var toggle in _Toggles)
            {
                toggle.ApplyGroupAllowSwitchOff();
            }
        }

        #endregion
    }
}
