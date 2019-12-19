using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Kyub.UI
{
    public class ColorPresets : MonoBehaviour
    {
        #region Helper Classes

        [System.Serializable]
        public class ColorUnityEvent : UnityEvent<Color> { }

        #endregion

        #region Private Variables

        [SerializeField]
        List<Color> m_colors = new List<Color>();
        [SerializeField]
        Button m_colorPrefab = null;
        [SerializeField]
        Transform m_content = null;

        #endregion

        #region Callbacks

        public ColorUnityEvent OnColorChanged = new ColorUnityEvent();

        #endregion

        #region Public Properties

        public List<Color> Colors
        {
            get
            {
                return m_colors;
            }
            set
            {
                if (m_colors == value)
                    return;
                m_colors = value;
            }
        }

        public Button ColorPrefab
        {
            get
            {
                return m_colorPrefab;
            }
            set
            {
                if (m_colorPrefab == value)
                    return;
                m_colorPrefab = value;
            }
        }

        public Transform Content
        {
            get
            {
                if (m_content == null)
                    return this.transform;
                return m_content;
            }
            set
            {
                if (m_content == value)
                    return;
                m_content = value;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            if (_started)
                TryApply(true);
        }

        protected bool _started = false;
        protected virtual void Start()
        {
            _started = true;
            TryApply(true);
        }

        protected virtual void Update()
        {
            TryApply();
        }

        #endregion

        #region Helper Functions

        protected bool _isDirty = false;
        public virtual void SetDirty()
        {
            _isDirty = true;
        }

        protected void TryApply(bool p_force = false)
        {
            if (_isDirty || p_force)
            {
                _isDirty = false;
                Apply();
            }
        }

        protected virtual void Apply()
        {
            var v_buttons = Content.GetComponentsInChildren<Button>(true);
            List<Button> v_buttonsUsed = new List<Button>();
            for (int i = 0; i < Colors.Count; i++)
            {
                Button v_buttonToUse = null;
                if (v_buttons.Length > i)
                    v_buttonToUse = v_buttons[i];
                if (v_buttonToUse == null && m_colorPrefab != null)
                {
                    v_buttonToUse = GameObject.Instantiate(m_colorPrefab);
                    v_buttonToUse.transform.SetParent(Content);
                    v_buttonToUse.transform.localPosition = Vector3.zero;
                    v_buttonToUse.transform.localScale = Vector3.one;
                }

                if (v_buttonToUse != null)
                {
                    if (v_buttonToUse.targetGraphic != null)
                        v_buttonToUse.targetGraphic.color = Colors[i];
                    v_buttonToUse.onClick.RemoveAllListeners();
                    v_buttonToUse.onClick.AddListener(delegate ()
                    {
                        Color v_color = v_buttonToUse.targetGraphic != null ? v_buttonToUse.targetGraphic.color : Color.white;
                        if (OnColorChanged != null)
                            OnColorChanged.Invoke(v_color);
                    });
                    v_buttonsUsed.Add(v_buttonToUse);
                }
            }

            foreach (var v_button in v_buttons)
            {
                if (v_button != null && !v_buttonsUsed.Contains(v_button))
                {
                    if (Application.isEditor)
                        GameObject.DestroyImmediate(v_button.gameObject);
                    else
                        GameObject.Destroy(v_button.gameObject);
                }
            }
        }

        #endregion
    }
}
