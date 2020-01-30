using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MaterialUI
{
    public class CanvasStyleGroup : UIBehaviour
    {
        #region Private Variables

        [SerializeField]
        private StyleSheetAsset m_styleAsset = null;

        HashSet<BaseStyleElement> _behavioursToReapplyStyle = new HashSet<BaseStyleElement>();
        HashSet<BaseStyleElement> _registeredBehaviours = new HashSet<BaseStyleElement>();

        #endregion

        #region Public Properties

        public StyleSheetAsset StyleAsset
        {
            get
            {
                return m_styleAsset;
            }

            set
            {
                m_styleAsset = value;
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnDestroy()
        {
            base.OnDestroy();
#if UNITY_EDITOR
            CancelInvoke();
#endif
            ForceUnregisterStyleBehaviours();
        }

        protected virtual void Update()
        {
            TryReapplyStyles();
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            //Register Validate Delayed
            UnityEditor.EditorApplication.update -= EditorOnValidateDelayed;
            if (this.gameObject.activeInHierarchy)
            {
                UnityEditor.EditorApplication.update += EditorOnValidateDelayed;
            }
        }

        protected virtual void OnValidateDelayed()
        {
            Invoke("TryReapplyStyles", 0.05f);
        }

        [System.NonSerialized] bool _isFirstOnValidate = true;
        //Internal functions to manage validate invoke
        void EditorOnValidateDelayed()
        {
            //Unregister Validate Delayed
            UnityEditor.EditorApplication.update -= EditorOnValidateDelayed;
            if (this != null)
            {
                OnValidateDelayed();
                //Force Layout Rebuild when loading scene in editor for the first time
                if (_isFirstOnValidate)
                {
                    _isFirstOnValidate = false;
                    if (!Application.isPlaying)
                        UnityEditor.EditorApplication.update += EditorOnValidateDelayed;
                }
            }
        }

#endif

        #endregion

        #region Helper Functions

        public bool TryGetStyleData(string p_name, System.Type p_acceptedType, out StyleData p_style)
        {
            if (m_styleAsset != null)
                return m_styleAsset.TryGetStyleData(p_name, p_acceptedType, out p_style);
            else
            {
                p_style = null;
                return false;
            }
        }

        public bool TryGetStyleDataOrFirstValid(string p_name, System.Type p_acceptedType, out StyleData p_style)
        {
            if (m_styleAsset != null)
                return m_styleAsset.TryGetStyleDataOrFirstValid(p_name, p_acceptedType, out p_style);
            else
            {
                p_style = null;
                return false;
            }
        }

        protected virtual void ForceRegisterStyleBehaviours()
        {
            var v_styleBehaviours = GetComponentsInChildren<BaseStyleElement>();

            //Force register again
            foreach (var v_styleBehaviour in v_styleBehaviours)
            {
                if (v_styleBehaviour != null && v_styleBehaviour.StyleGroup != this)
                {
                    v_styleBehaviour.UnregisterFromStyleGroup();
                    v_styleBehaviour.RegisterToStyleGroup();
                }
            }
        }

        protected virtual void ForceUnregisterStyleBehaviours()
        {
            var v_styleBehaviours = GetComponentsInChildren<BaseStyleElement>();

            //Force register again
            foreach (var v_styleBehaviour in v_styleBehaviours)
            {
                if (v_styleBehaviour != null && v_styleBehaviour.StyleGroup != this)
                {
                    v_styleBehaviour.UnregisterFromStyleGroup();
                }
            }
        }

        protected virtual void Invalidate(BaseStyleElement p_behaviour)
        {
            InvalidateWithoutCallEvents(p_behaviour);
            CancelInvoke("TryReapplyStyles");
            Invoke("TryReapplyStyles", 0.1f);
        }

        protected virtual void InvalidateWithoutCallEvents(BaseStyleElement p_behaviour)
        {
            if (p_behaviour != null && _registeredBehaviours.Contains(p_behaviour))
                _behavioursToReapplyStyle.Add(p_behaviour);
        }

        protected virtual void InvalidateAll()
        {
            foreach (var v_styleBehaviour in _registeredBehaviours)
            {
                InvalidateWithoutCallEvents(v_styleBehaviour);
            }
            CancelInvoke("TryReapplyStyles");
            Invoke("TryReapplyStyles", 0.1f);
        }

        protected virtual void TryReapplyStyles()
        {
            if (this != null && _behavioursToReapplyStyle.Count > 0)
            {
                foreach (var v_styleBehaviour in _behavioursToReapplyStyle)
                {
                    if (v_styleBehaviour != null)
                    {
                        if (v_styleBehaviour.SupportStyleGroup)
                        {
                            v_styleBehaviour.LoadStyles();
                        }
                        else if (!v_styleBehaviour.UnregisterFromStyleGroup() && _registeredBehaviours.Contains(v_styleBehaviour))
                        {
                            UnregisterStyleBehaviour(v_styleBehaviour);
                        }
                    }
                }
                _behavioursToReapplyStyle.Clear();
            }
        }

        protected internal bool RegisterStyleBehaviour(BaseStyleElement p_styleBehavior)
        {
            if (p_styleBehavior != null && p_styleBehavior.SupportStyleGroup)
            {
                //Unregister from previous group
                if (p_styleBehavior.StyleGroup != null && p_styleBehavior.StyleGroup != this)
                    p_styleBehavior.UnregisterFromStyleGroup();

                if (!_registeredBehaviours.Contains(p_styleBehavior))
                {
                    var v_sucess = _registeredBehaviours.Add(p_styleBehavior);
                    Invalidate(p_styleBehavior);

                    return v_sucess;
                }
            }
            return false;
        }

        protected internal bool UnregisterStyleBehaviour(BaseStyleElement p_styleBehavior)
        {
            if (p_styleBehavior != null && _registeredBehaviours.Contains(p_styleBehavior))
                return _registeredBehaviours.Remove(p_styleBehavior);
            return false;
        }

        #endregion
    }
}
