using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kyub.UI
{
    [ExecuteInEditMode]
    public class ToggleColor : MonoBehaviour
    {

        #region Private Variables

        [SerializeField]
        List<ToggleColorStruct> m_toggleColors = new List<ToggleColorStruct>();

        #endregion

        #region Public Properties

        public List<ToggleColorStruct> ToggleColors
        {
            get
            {
                return m_toggleColors;
            }
            set
            {
                if (m_toggleColors == value)
                    return;
                m_toggleColors = value;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            if (Application.isPlaying)
                RegisterEvents();
            else
                _oldToggle = GetToggleValue();
        }

        protected virtual void Start()
        {
            ApplyColors(GetToggleValue());
        }

        protected virtual void OnDestroy()
        {
            if (Application.isPlaying)
                UnregisterEvents();
        }

        protected virtual void Update()
        {
            if (!Application.isPlaying && Application.isEditor)
                ApplyColors();
        }

        #endregion

        #region Helper Functions

        public void RegisterEvents()
        {
            UnregisterEvents();
            Toggle v_toggle = GetComponent<Toggle>();
            if (v_toggle != null)
            {
                v_toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        public void UnregisterEvents()
        {
            Toggle v_toggle = GetComponent<Toggle>();
            if (v_toggle != null)
            {
                v_toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
        }

        bool _oldToggle = false;
        public void OnToggleChanged(bool p_toggleActive)
        {
            ApplyColors(p_toggleActive);
        }

        protected void ApplyColors()
        {
            Toggle v_toggle = GetComponent<Toggle>();
            if (v_toggle != null && _oldToggle != GetToggleValue())
            {
                _oldToggle = GetToggleValue();
                ApplyColors(v_toggle.isOn);
            }
        }

        protected virtual void ApplyColors(bool p_toggleActive)
        {
            foreach (ToggleColorStruct v_struct in ToggleColors)
            {
                if (v_struct != null)
                    v_struct.ApplyColor(p_toggleActive);
            }
        }

        protected virtual bool GetToggleValue()
        {
            Toggle v_toggle = GetComponent<Toggle>();
            if (v_toggle != null)
                return v_toggle.isOn;
            return false;
        }

        #endregion
    }

    [System.Serializable]
    public class ToggleColorStruct
    {
        #region Private Varibles

        [SerializeField]
        GameObject m_objectToApply = null;
        [SerializeField]
        Color m_colorEnabled = Color.white;
        [SerializeField]
        Color m_colorDisabled = Color.white;

        #endregion

        #region Public Properties

        public GameObject ObjectToApply
        {
            get
            {
                return m_objectToApply;
            }
            set
            {
                if (m_objectToApply == value)
                    return;
                m_objectToApply = value;
            }
        }

        public Color ColorEnabled
        {
            get
            {
                return m_colorEnabled;
            }
            set
            {
                if (m_colorEnabled == value)
                    return;
                m_colorEnabled = value;
            }
        }

        public Color ColorDisabled
        {
            get
            {
                return m_colorDisabled;
            }
            set
            {
                if (m_colorDisabled == value)
                    return;
                m_colorDisabled = value;
            }
        }

        #endregion

        #region Helper Functions

        public void ApplyColor(bool p_enabled)
        {
            if (ObjectToApply != null)
            {
                SpriteRenderer v_renderer = ObjectToApply.GetComponent<SpriteRenderer>();
                Material v_material = ObjectToApply.GetComponent<Material>();
                MaskableGraphic v_graphic = ObjectToApply.GetComponent<MaskableGraphic>();
                Color v_color = p_enabled ? ColorEnabled : ColorDisabled;
                if (v_renderer != null)
                    v_renderer.color = v_color;
                else if (v_material != null && v_material.HasProperty("_Color"))
                    v_material.color = v_color;
                else if (v_graphic != null)
                    v_graphic.color = v_color;
            }
        }

        #endregion
    }
}
