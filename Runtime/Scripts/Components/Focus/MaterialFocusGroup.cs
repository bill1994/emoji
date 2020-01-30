using Kyub.EventSystems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    [DisallowMultipleComponent]
    public class MaterialFocusGroup : MonoBehaviour
    {
        #region Helper Functions

        [System.Serializable]
        public class KeyTriggerData
        {
            public enum KeyTriggerType { KeyDown, KeyUp }

            public string Name = "";
            public KeyCode Key = KeyCode.None;
            public KeyTriggerType TriggerType = KeyTriggerType.KeyDown;
            public UnityEvent OnCallTrigger = new UnityEvent();
        }

        #endregion

        #region Static Properties

        static List<MaterialFocusGroup> _focusOrder = new List<MaterialFocusGroup>();
        protected static List<MaterialFocusGroup> FocusOrder
        {
            get
            {
                if (_focusOrder == null)
                    _focusOrder = new List<MaterialFocusGroup>();
                return _focusOrder;
            }
            set
            {
                if (_focusOrder == value)
                    return;
                _focusOrder = value;
            }
        }

        #endregion

        #region Private Variables

        [Header("Trigger Fields")]
        [SerializeField]
        protected List<KeyTriggerData> m_keyTriggers = new List<KeyTriggerData>();

        #endregion

        #region Callbacks

        [Header("Focus Callbacks")]
        public UnityEvent OnGainFocusCallback = new UnityEvent();
        public UnityEvent OnLoseFocusCallback = new UnityEvent();

        #endregion

        #region Properties

        protected BaseInput input
        {
            get
            {
                if (EventSystem.current && EventSystem.current.currentInputModule)
                    return EventSystem.current.currentInputModule.input;
                return null;
            }
        }

        public List<KeyTriggerData> KeyTriggers
        {
            get
            {
                if (m_keyTriggers == null)
                    m_keyTriggers = new List<KeyTriggerData>();
                return m_keyTriggers;
            }
            set
            {
                if (m_keyTriggers == value)
                    return;
                m_keyTriggers = value;
            }
        }

        #endregion

        #region Unity Functions

        protected bool _started = false;
        protected virtual void Start()
        {
            _started = true;
            if (Application.isPlaying && enabled && gameObject.activeSelf && gameObject.activeInHierarchy)
            {
                CheckFocus(GetGroupVisibilityInHierarchy(), false);
            }
        }

        protected virtual void OnEnable()
        {
            if (Application.isPlaying && enabled && gameObject.activeSelf && gameObject.activeInHierarchy)
            {
                if (_started)
                    CheckFocus(GetGroupVisibilityInHierarchy());
            }
        }

        protected virtual void OnDisable()
        {
            if (Application.isPlaying)
            {
                CheckFocus(false, true, false);
            }
        }

        #endregion

        #region Key Trigger Helper Functions

        //Check Key Presses
        protected virtual void CheckKeyTriggers()
        {
            var v_focus = -1;
            foreach (var v_keyTrigger in m_keyTriggers)
            {
                //No Focus
                if (v_focus == 0)
                    return;

                if (v_keyTrigger != null && v_keyTrigger.OnCallTrigger != null && v_keyTrigger.Key != KeyCode.None)
                {
                    bool v_callTrigger = false;
                    if (v_keyTrigger.TriggerType == KeyTriggerData.KeyTriggerType.KeyDown)
                    {
                        if (InputCompat.GetKeyDown(v_keyTrigger.Key))
                            v_callTrigger = true;
                    }
                    if (v_keyTrigger.TriggerType == KeyTriggerData.KeyTriggerType.KeyUp)
                    {
                        if (InputCompat.GetKeyUp(v_keyTrigger.Key))
                            v_callTrigger = true;
                    }

                    if (v_callTrigger)
                    {
                        //Only check focus if any key is triggered (very expensive check)
                        if (v_focus < 0)
                            v_focus = IsUnderFocus(this.gameObject) ? 1 : 0;
                        if (v_focus > 0)
                        {
                            v_keyTrigger.OnCallTrigger.Invoke();
                        }
                    }
                }
            }
        }

        protected IEnumerator CheckKeyTrigger_UpdateRoutine()
        {
            while (true)
            {
                yield return null;
                CheckKeyTriggers();
            }
        }

        protected virtual void CancelKeyTriggerUpdateRoutine()
        {
            StopCoroutine("CheckKeyTrigger_UpdateRoutine");
        }

        protected virtual void StartKeyTriggerUpdateRoutine()
        {
            CancelKeyTriggerUpdateRoutine();
            if (enabled && gameObject.activeInHierarchy && IsUnderFocus(this.gameObject))
            {
                StartCoroutine("CheckKeyTrigger_UpdateRoutine");
            }
        }

        #endregion

        #region Helper Functions

        protected bool GetGroupVisibilityInHierarchy()
        {
            return this.gameObject.activeInHierarchy || enabled;
        }

        protected void CheckFocus(bool p_active, bool p_ignoreChildrenFocus = true)
        {
            CheckFocus(p_active, p_ignoreChildrenFocus, enabled && gameObject.activeSelf && gameObject.activeInHierarchy);
        }

        protected virtual void CheckFocus(bool p_active, bool p_ignoreChildrenFocus, bool p_keepInFocusList)
        {
            if (p_active && enabled)
            {
                MaterialFocusGroup v_currentFocus = MaterialFocusGroup.GetFocus();
                bool v_canIgnoreCurrentFocus = p_ignoreChildrenFocus || v_currentFocus == null || !MaterialFocusGroup.IsChildObject(this.gameObject, v_currentFocus.gameObject, false);
                if (v_canIgnoreCurrentFocus)
                {
                    if(!MaterialFocusGroup.SetFocus(this))
                        StartKeyTriggerUpdateRoutine();
                }
                else
                {
                    //Find index to add self to Focus (Index after your last children)
                    int v_indexToAddThis = 0;
                    var v_index = MaterialFocusGroup.FocusOrder.IndexOf(this);
                    if (v_index >= 0)
                        MaterialFocusGroup.FocusOrder.RemoveAt(v_index);
                    for (int i = 0; i < MaterialFocusGroup.FocusOrder.Count; i++)
                    {
                        MaterialFocusGroup v_container = MaterialFocusGroup.FocusOrder[i];
                        bool v_isChildrenContainer = v_container != null && MaterialFocusGroup.IsChildObject(this.gameObject, v_container.gameObject, false);
                        if (v_isChildrenContainer)
                            v_indexToAddThis = i + 1;
                    }
                    MaterialFocusGroup.FocusOrder.Insert(v_indexToAddThis, this);
                    StartKeyTriggerUpdateRoutine();
                }
                
            }
            else
            {
                if (!MaterialFocusGroup.RemoveFocus(this, p_keepInFocusList))
                    CancelKeyTriggerUpdateRoutine();
            }
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnGainFocus()
        {
            StartKeyTriggerUpdateRoutine();
            if (OnGainFocusCallback != null)
                OnGainFocusCallback.Invoke();
        }

        protected virtual void HandleOnLoseFocus()
        {
            CancelKeyTriggerUpdateRoutine();
            if (OnLoseFocusCallback != null)
                OnLoseFocusCallback.Invoke();
        }

        #endregion

        #region Static Functions

        public static bool IsChildObject(GameObject p_possibleParent, GameObject p_child, bool p_includeSelf = false)
        {
            bool v_isChild = false;
            if (p_child != null && p_possibleParent != null)
            {
                if (p_includeSelf && p_possibleParent == p_child)
                    v_isChild = true;
                if (!v_isChild)
                {
                    MaterialFocusGroup[] v_focusContainares = p_child.GetComponentsInParent<MaterialFocusGroup>();
                    foreach (MaterialFocusGroup v_cont in v_focusContainares)
                    {
                        if (v_cont != null && v_cont.gameObject == p_possibleParent)
                        {
                            v_isChild = true;
                            break;
                        }
                    }
                }
            }
            return v_isChild;
        }

        //If Any Parent or Self contain Focus, Or Focus equal null and panel is Opened or GameObject is Active
        public static bool IsUnderFocus(GameObject p_object)
        {
            if (p_object != null)
            {
                MaterialFocusGroup v_focus = MaterialFocusGroup.GetFocus();
                var v_state = v_focus == null ? true : v_focus.GetGroupVisibilityInHierarchy();
                if (v_state && (MaterialFocusGroup.GetDirectFocusGroupComponent(p_object) == v_focus))
                    return true;
            }
            return false;
        }

        public static MaterialFocusGroup GetDirectFocusGroupComponent(GameObject p_child)
        {
            if (p_child != null)
            {
                MaterialFocusGroup[] v_parentsFocus = p_child.GetComponentsInParent<MaterialFocusGroup>();
                MaterialFocusGroup v_directParentFocus = null;
                foreach (MaterialFocusGroup v_parentFocus in v_parentsFocus)
                {
                    if (v_parentFocus != null && v_parentFocus.enabled)
                    {
                        v_directParentFocus = v_parentFocus;
                        break;
                    }
                }
                return v_directParentFocus;
            }
            return null;
        }

        public static bool ParentContainFocus(GameObject p_child)
        {
            if (p_child != null)
            {
                MaterialFocusGroup v_directParentFocus = GetDirectFocusGroupComponent(p_child);
                return ContainFocus(v_directParentFocus);
            }
            return false;
        }

        public static bool ContainFocus(MaterialFocusGroup p_container)
        {
            if (p_container != null && p_container == GetFocus())
            {
                return true;
            }
            return false;
        }

        public static bool RemoveFocus(MaterialFocusGroup p_container, bool p_keepInList)
        {
            if (p_container != null)
            {
                if (!p_container.enabled)
                    p_keepInList = false;
                MaterialFocusGroup v_oldFocus = GetFocus();
                var v_index = FocusOrder.IndexOf(p_container);
                if (v_index >= 0)
                    FocusOrder.RemoveAt(v_index);
                if (p_keepInList)
                {
                    FocusOrder.Add(p_container);
                    var v_newIndex = FocusOrder.IndexOf(p_container);
                    //Focus cant change because we have only one object in list
                    if (v_newIndex == v_index)
                        return false;
                }
                //Call Focus Events
                if (v_oldFocus == p_container)
                {
                    MaterialFocusGroup v_newFocus = GetFocus();
                    if (v_oldFocus != null)
                        v_oldFocus.HandleOnLoseFocus();

                    if (v_newFocus != null)
                        v_newFocus.HandleOnGainFocus();

                    return true;
                }
                else if (v_oldFocus != null)
                    v_oldFocus.StartKeyTriggerUpdateRoutine();
            }
            return false;
        }

        public static bool SetFocus(MaterialFocusGroup p_container)
        {
            MaterialFocusGroup v_oldFocus = GetFocus();
            if (p_container != null)
            {
                if (v_oldFocus != p_container)
                {
                    var v_index = FocusOrder.IndexOf(p_container);
                    if (v_index >= 0)
                        FocusOrder.RemoveAt(v_index);

                    if (FocusOrder.Count > 0)
                        FocusOrder.Insert(0, p_container);
                    else
                        FocusOrder.Add(p_container);
                    //Call Focus Events
                    if (v_oldFocus != null)
                        v_oldFocus.HandleOnLoseFocus();

                    p_container.HandleOnGainFocus();

                    return true;
                }
            }
            return false;
        }

        public static MaterialFocusGroup GetFocus()
        {
            for (int i = 0; i < FocusOrder.Count; i++)
            {
                if (FocusOrder[i] == null || !FocusOrder[i].enabled)
                {
                    FocusOrder.RemoveAt(i);
                    i--;
                }
            }
            MaterialFocusGroup v_container = FocusOrder.Count > 0 ? FocusOrder[0] : null;
            return v_container;
        }

        #endregion
    }
}
