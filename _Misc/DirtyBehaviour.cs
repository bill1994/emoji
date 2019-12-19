using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub.Extensions;

namespace Kyub
{
    public class DirtyBehaviour : MonoBehaviour
    {
        #region Unity Functions

        protected virtual void OnEnable()
        {
            if (_started)
                TryApply(true);
        }

        protected virtual void OnDisable()
        {
            TryFixRoutineState();
        }

        protected bool _started = false;
        protected virtual void Start()
        {
            _started = true;
            TryApply(true);
        }

        /*protected virtual void Update()
        {
            TryApply();
        }*/

        #endregion

        #region Helper Functions

        protected bool _isDirty = false;
        public virtual void SetDirty()
        {
            _isDirty = true;
            TryFixRoutineState();
        }

        public virtual void TryApply(bool p_force = false)
        {
            if (_isDirty || p_force)
            {
                _isDirty = false;
                Apply();
            }

            TryFixRoutineState();
        }

        protected virtual void Apply()
        {
        }

        #endregion

        #region Routine Checker

        protected virtual void TryFixRoutineState()
        {
            if (this != null)
            {
                var v_isValid = this.gameObject.activeInHierarchy && enabled && _isDirty;

                //Try stop running routines if state is invalid
                if (_applyRoutineController != null)
                {
                    if (_applyRoutineController.state == CoroutineState.Paused ||
                        (!v_isValid && _applyRoutineController.state == CoroutineState.Running))
                    {
                        _applyRoutineController.Stop();
                    }
                }

                //Start routine if is valid
                if (v_isValid)
                {
                    if (_applyRoutineController == null || _applyRoutineController.state != CoroutineState.Running)
                        this.StartCoroutineEx(TryApply_TickRoutine(), out _applyRoutineController);
                }
                else
                    _applyRoutineController = null;
            }
        }

        protected CoroutineController _applyRoutineController = null;
        protected virtual IEnumerator TryApply_TickRoutine()
        {
            while (_isDirty)
            {
                yield return null;
                TryApply();
            }
            _applyRoutineController = null;
        }

        #endregion
    }
}
