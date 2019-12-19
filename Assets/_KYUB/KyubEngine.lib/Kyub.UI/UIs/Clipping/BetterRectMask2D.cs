using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine;
using System.Reflection;
using Kyub.Reflection;

namespace Kyub.UI
{
    public class BetterRectMask2D : RectMask2D
    {
        #region Private Variables

        [SerializeField]
        bool m_clipInsideOverriddenSortingCanvas = false;

        #endregion

        #region Properties

        public bool ClipInsideOverriddenSortingCanvas
        {
            get
            {
                return m_clipInsideOverriddenSortingCanvas;
            }
            set
            {
                if (m_clipInsideOverriddenSortingCanvas == value)
                    return;
                m_clipInsideOverriddenSortingCanvas = value;
                ShouldRecalculateClipRects = true;
            }
        }

        FieldInfo _shouldRecalculateClipRectsField = null;
        protected bool ShouldRecalculateClipRects
        {
            get
            {
                if (_shouldRecalculateClipRectsField == null)
                {
                    _shouldRecalculateClipRectsField = typeof(RectMask2D).GetField("m_ShouldRecalculateClipRects", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                }

                try
                {
                    return _shouldRecalculateClipRectsField != null ? (bool)_shouldRecalculateClipRectsField.GetValue(this) : false;
                }
                catch { }
                return false;
            }
            set
            {
                if (_shouldRecalculateClipRectsField == null)
                {
                    _shouldRecalculateClipRectsField = typeof(RectMask2D).GetField("m_ShouldRecalculateClipRects", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                try
                {
                    _shouldRecalculateClipRectsField.SetValue(this, value);
                }
                catch { }
            }
        }

        #endregion

        #region Helper Functions

        public override void PerformClipping()
        {
            try
            {
                if (ShouldRecalculateClipRects)
                {
                    UpdateClipabbles();
                }
                base.PerformClipping();
            }
            catch { ShouldRecalculateClipRects = true; }
        }

        protected virtual void UpdateClipabbles()
        {
            var v_clippables = GetComponentsInChildren<IClippable>(true);
            foreach (var v_clipabble in v_clippables)
            {
                if (ClipInsideOverriddenSortingCanvas)
                    AddClippable(v_clipabble);
                else
                    RemoveClippable(v_clipabble);
            }
            if(!ClipInsideOverriddenSortingCanvas)
                MaskUtilities.Notify2DMaskStateChanged(this);
        }

        #endregion
    }
}

