using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kyub.UI.Experimental
{
    //axis to cache values
    [System.Flags]
    public enum DrivenAxis { None = 0, Horizontal = 1, Vertical = 2, Ignore = 4 }

    public interface IFastLayoutFeedback : ILayoutElement, ILayoutController
    {
        IFastLayoutGroup group { get; }
        RectTransform rectTransform { get; }
        float cachedMinWidth { get; }
        float cachedMinHeight { get; }
        float cachedPreferredWidth { get; }
        float cachedPreferredHeight { get; }
        float cachedFlexibleWidth { get; }
        float cachedFlexibleHeight { get; }

        float cachedRectWidth { get; }
        float cachedRectHeight { get; }
        bool cachedLayoutIgnore { get; }

        void SendFeedback();
    }

    public interface IFastLayoutGroup : ILayoutElement, ILayoutController
    {
        RectTransform rectTransform { get; }
        DrivenAxis parentControlledAxis { get; set; }
        DrivenAxis childrenControlledAxis { get; set; }
        bool isDirty { get; }

        void SetElementDirty(IFastLayoutFeedback driven, DrivenAxis dirtyAxis);
    }
}
