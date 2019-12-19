using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Kyub.Extensions;

namespace Kyub.UI
{
    public class TabNavigatorFocusGroup : FocusGroup
    {
        #region Unity Functions

        protected override void Update()
        {
            base.Update();
            CheckInput();
        }

        #endregion

        #region Helper Functions

        public virtual void CheckInput(bool p_force = false)
        {
            if ((p_force || Input.GetKeyDown(KeyCode.Tab)) && FocusGroup.IsUnderFocus(this.gameObject))
            {
                StartCoroutine(ChangeInput());
            }
        }

        bool _checking = false;
        protected virtual IEnumerator ChangeInput()
        {
            if (!_checking)
            {
                _checking = true;
                EventSystem v_eventSystem = EventSystem.current;
                if (v_eventSystem != null)
                {
                    bool v_moveBack = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    GameObject v_currentSelectedGameObject = v_eventSystem.currentSelectedGameObject;
                    Selectable v_currentSelectedComponent = v_currentSelectedGameObject != null ? v_currentSelectedGameObject.GetComponent<Selectable>() : null;

                    if (!(v_currentSelectedComponent is InputField) || !(((InputField)v_currentSelectedComponent).multiLine))
                    {
                        List<Selectable> v_currentSelectables = new List<Selectable>();
#if UNITY_2019_1_OR_NEWER
                        IList<Selectable> v_allSelectables = Selectable.allSelectablesArray;
#else
                        IList<Selectable> v_allSelectables = Selectable.allSelectables;
#endif
                        for (int i = 0; i < v_allSelectables.Count; i++)
                        {
                            Selectable v_selectableComponent = v_allSelectables[i];
                            v_currentSelectables.RemoveChecking(v_selectableComponent);
                            if (v_selectableComponent != null && v_selectableComponent.enabled && v_selectableComponent.gameObject.activeInHierarchy && v_selectableComponent.gameObject.activeSelf && v_selectableComponent.navigation.mode != Navigation.Mode.None && FocusGroup.IsUnderFocus(v_selectableComponent.gameObject))
                            {
                                //Sort with SelectionUpDown Index
                                int v_indexDown = v_currentSelectables.IndexOf(v_selectableComponent.navigation.selectOnDown);
                                int v_indexUp = v_currentSelectables.IndexOf(v_selectableComponent.navigation.selectOnUp);
                                bool v_insertingComplete = false;
                                int v_indexToInsert = v_indexDown >= 0 && v_indexDown < v_currentSelectables.Count ? v_indexDown : (v_indexUp >= 0 && v_indexUp < v_currentSelectables.Count ? v_indexUp + 1 : -1);
                                if (v_indexToInsert >= 0 && v_indexToInsert < v_currentSelectables.Count + 1)
                                {
                                    try
                                    {
                                        v_currentSelectables.Insert(v_indexToInsert, v_selectableComponent);
                                        v_insertingComplete = true;
                                    }
                                    catch { }
                                }
                                if (!v_insertingComplete)
                                {
                                    v_insertingComplete = true;
                                    v_currentSelectables.Add(v_selectableComponent);
                                }
                            }
                        }

                        if (v_currentSelectedComponent != null)
                        {
                            int v_index = v_currentSelectables.IndexOf(v_currentSelectedComponent);
                            v_index = v_moveBack ? v_index - 1 : v_index + 1;
                            v_currentSelectedComponent = v_index >= 0 && v_index < v_currentSelectables.Count ? v_currentSelectables[v_index] : null;
                            v_currentSelectedGameObject = v_currentSelectedComponent != null ? v_currentSelectedComponent.gameObject : null;
                        }
                        if (v_currentSelectedComponent == null)
                        {
                            v_currentSelectedComponent = v_moveBack ? v_currentSelectables.GetLast() : v_currentSelectables.GetFirst();
                            v_currentSelectedGameObject = v_currentSelectedComponent != null ? v_currentSelectedComponent.gameObject : null;
                        }

                        v_eventSystem.SetSelectedGameObject(v_currentSelectedGameObject);
                        if (v_currentSelectedComponent != null)
                        {
                            InputField v_inputfield = v_currentSelectedComponent.GetComponent<InputField>();
                            if (v_inputfield != null)
                                v_inputfield.OnPointerClick(new PointerEventData(v_eventSystem));
                            yield return new WaitForSecondsRealtime(0.05f);
                        }
                    }
                }
                _checking = false;
            }
        }

#endregion
    }
}
