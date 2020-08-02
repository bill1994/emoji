using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Kyub.UI
{
    public enum ShowObjectActionEnum { Show, ShowFinish, Hide, HideFinish }

    public static class TweenContainerUtils
    {
        public static bool IsChildObject(GameObject p_possibleParent, GameObject p_child, bool p_includeSelf = false)
        {
            bool v_isChild = false;
            if (p_child != null && p_possibleParent != null)
            {
                if (p_includeSelf && p_possibleParent == p_child)
                    v_isChild = true;
                if (!v_isChild)
                {
                    FocusGroup[] v_focusContainares = p_child.GetComponentsInParent<FocusGroup>();
                    foreach (FocusGroup v_cont in v_focusContainares)
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

        public static void SetContainerVisibility(GameObject p_object, ShowObjectActionEnum p_action, float p_time)
        {
            if (p_object != null)
            {
                if (p_time > 0)
                {
                    ApplicationContext.RunOnMainThread(() =>
                    {
                       SetContainerVisibility(p_object, p_action);
                    }, p_time);
                }
                else
                {
                    SetContainerVisibility(p_object, p_action);
                }
            }
        }

        public static void SetContainerVisibility(TweenContainer p_panel, ShowObjectActionEnum p_action, float p_time)
        {
            if (p_panel != null)
            {
                if (p_time > 0)
                {
                    ApplicationContext.RunOnMainThread(() =>
                    {
                        SetContainerVisibility(p_panel, p_action);
                    }, p_time);
                }
                else
                {
                    SetContainerVisibility(p_panel, p_action);
                }
            }
        }

        public static void SetContainerVisibility(GameObject p_object, ShowObjectActionEnum p_action)
        {
            if (p_object != null)
            {
                TweenContainer v_panel = p_object.GetComponent<TweenContainer>();
                if (v_panel != null)
                {
                    SetContainerVisibility(v_panel, p_action);
                }
                else
                {
                    if (p_action == ShowObjectActionEnum.Show || p_action == ShowObjectActionEnum.ShowFinish)
                    {
                        p_object.SetActive(true);
                    }
                    else
                    {
                        p_object.SetActive(false);
                    }
                }
            }
        }

        public static void SetContainerVisibility(TweenContainer p_panel, ShowObjectActionEnum p_action)
        {
            if (p_panel != null)
            {
                if (p_action == ShowObjectActionEnum.Show)
                {
                    p_panel.Show(false);
                }
                else if (p_action == ShowObjectActionEnum.ShowFinish)
                {
                    p_panel.Show(true);
                }
                else if (p_action == ShowObjectActionEnum.Hide)
                {
                    p_panel.Hide(false);
                }
                else
                {
                    p_panel.Hide(true);
                }
            }
        }

        public static PanelStateEnum GetContainerVisibilityInHierarchy(GameObject p_object)
        {
            if (p_object != null)
            {
                if (!p_object.activeInHierarchy)
                    return PanelStateEnum.Closed;
                else
                {
                    TweenContainer[] v_componentsInParent = p_object.GetComponentsInParent<TweenContainer>();
                    PanelStateEnum v_return = PanelStateEnum.Opened;
                    foreach (TweenContainer v_cont in v_componentsInParent)
                    {
                        if (v_cont != null && v_cont.enabled && (v_cont.PanelState == PanelStateEnum.Closed || v_cont.PanelState == PanelStateEnum.Closing))
                            v_return = v_cont.PanelState;
                    }
                    if (v_return != PanelStateEnum.Closed && v_return != PanelStateEnum.Closing)
                        v_return = GetContainerVisibility(p_object);
                    return v_return;
                }
            }
            return PanelStateEnum.Closed;
        }

        public static PanelStateEnum GetContainerVisibilityInHierarchy(TweenContainer p_panel)
        {
            if (p_panel != null)
            {
                return GetContainerVisibilityInHierarchy(p_panel.gameObject);
            }
            return PanelStateEnum.Closed;
        }

        public static PanelStateEnum GetContainerVisibility(GameObject p_object)
        {
            if (p_object != null)
            {
                TweenContainer v_panel = p_object.GetComponent<TweenContainer>();
                if (v_panel != null)
                {
                    return GetContainerVisibility(v_panel);
                }
                else
                {
                    return p_object.activeSelf ? PanelStateEnum.Opened : PanelStateEnum.Closed;
                }
            }
            return PanelStateEnum.Closed;
        }

        public static PanelStateEnum GetContainerVisibility(TweenContainer p_panel)
        {
            if (p_panel != null)
            {
                return p_panel.PanelState;
            }
            return PanelStateEnum.Closed;
        }
    }
}
