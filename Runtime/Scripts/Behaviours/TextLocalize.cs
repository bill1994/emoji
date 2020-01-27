//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2013 Tasharen Entertainment
//----------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyub.Localization.UI
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    public class TextLocalize : DirtyBehaviour
    {
        #region Consts

        public const string LOCALE_TAG_PATTERN = @"(<locale>(.*)<\/locale>)|(<localeparam=([0-9])+>(.*)<\/localeparam>)";

        public static System.Text.RegularExpressions.Regex s_localeRegexCompiled = new System.Text.RegularExpressions.Regex(LOCALE_TAG_PATTERN);

        #endregion

        #region Private Variables
        [SerializeField, Tooltip("Used to update the Key when external components change the graphic text without changing the Key in this component")]
        bool m_autoTrackKey = true;
        [Space]
        [SerializeField, TextArea]
        string m_key;

        [Tooltip("support use of:\n *<locale>...</locale> to localize part of text instead of the full text.\n" +
                                " *<skiplocale> in begining of the text force ignore locale in this object (will clear locale tags before return).\n" +
                                " *<localeparam=number>...</localeparam> replace the text by {number} before localize.\n" +
                                "ex: 'my text <localeparam=15>parameter value</localeparam>' will be localized as 'my text {15}' and after the value {15} will be replaced back")]
        [SerializeField]
        bool m_supportLocaleRichTextTags = false;

        string _language;
        string _lastLocalizedValue = "";

        Graphic _cachedGraphic = null;

        #endregion

        #region Public Propertoes

        public string Key
        {
            get
            {
                return m_key;
            }
            set
            {
                if (m_key == value)
                    return;
                m_key = value;

                _lastLocalizedValue = Text;
                SetDirty();
            }
        }

        public bool SupportLocaleRichTextTags
        {
            get
            {
                return m_supportLocaleRichTextTags;
            }
            set
            {
                if (m_supportLocaleRichTextTags == value)
                    return;
                m_supportLocaleRichTextTags = value;
                SetDirty();
            }
        }

        public bool AutoTrackKey
        {
            get
            {
                return m_autoTrackKey;
            }
            set
            {
                if (m_autoTrackKey == value)
                    return;
                m_autoTrackKey = value;
                HandleOnVerticesDirty();
            }
        }

        #endregion

        #region Protected Functions

        protected string Language
        {
            get
            {
                if (_language == null)
                    _language = "";
                return _language;
            }
            set
            {
                if (_language == value)
                    return;
                _language = value;
            }
        }

        protected Graphic GraphicComponent
        {
            get
            {
                if(_cachedGraphic == null)
                    _cachedGraphic = GetComponent<Graphic>();

                return _cachedGraphic;
            }
        }

        protected string Text
        {
            get
            {
                var v_graphic = GraphicComponent;
                if (v_graphic != null)
                {
                    if (v_graphic is TMPro.TMP_Text)
                        return ((TMPro.TMP_Text)v_graphic).text;
                    else if (v_graphic is Text)
                        return ((Text)v_graphic).text;
                }

                return null;
            }
            set
            {
                var v_graphic = GraphicComponent;
                if (v_graphic != null)
                {
                    if (value == null)
                        value = "";

                    if (v_graphic is TMPro.TMP_Text)
                        ((TMPro.TMP_Text)v_graphic).text = value;
                    else if (v_graphic is Text)
                        ((Text)v_graphic).text = value;
                }
            }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            RegisterEvents();

            if (!Application.isPlaying)
                return;

            if (_started)
            {
                //Update AutoTrack Key Value
                HandleOnVerticesDirty();
                //_lastLocalizedValue = Text;
                if (LocaleManager.InstanceExists() && !string.Equals(Language, LocaleManager.Instance.CurrentLanguage, StringComparison.InvariantCultureIgnoreCase))
                    SetDirty(); //prevent localize bugs when changing scene
            }
        }

        protected override void Start()
        {
            if (!Application.isPlaying)
                return;

            //Update AutoTrack Key Value
            HandleOnVerticesDirty();

            base.Start();
        }

        protected override void OnDisable()
        {
            UnregisterEvents();

            if (!Application.isPlaying)
                return;

            base.OnDisable();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
                SetDirty();
            else
            {
                RegisterEvents();
                HandleEditorReplaceLocaleTagsDelayed();
            }
        }
#endif

#endregion

        #region Helper Functions

        protected virtual void RegisterEvents()
        {
            UnregisterEvents();

            if (!Application.isPlaying)
            {
                var v_graphic = GraphicComponent;
                if (v_graphic != null)
                    v_graphic.RegisterDirtyVerticesCallback(HandleEditorReplaceLocaleTagsDelayed);
            }
            else
            {
                LocaleManager.OnLocalizeChanged += HandleOnLocalize;

                if (m_autoTrackKey)
                {
                    var v_graphic = GraphicComponent;
                    if (v_graphic != null)
                        v_graphic.RegisterDirtyVerticesCallback(HandleOnVerticesDirty);
                }
            }
        }

        protected virtual void UnregisterEvents()
        {
            if (!Application.isPlaying)
            {
                var v_graphic = GraphicComponent;
                if (v_graphic != null)
                    v_graphic.UnregisterDirtyVerticesCallback(HandleEditorReplaceLocaleTagsDelayed);
            }
            else
            {
                LocaleManager.OnLocalizeChanged -= HandleOnLocalize;

                var v_graphic = GraphicComponent;
                if (v_graphic != null)
                    v_graphic.UnregisterDirtyVerticesCallback(HandleOnVerticesDirty);
            }
        }

        protected override void Apply()
        {
            if (!Application.isPlaying)
                return;

            if (LocaleManager.InstanceExists())
            {
                UnregisterEvents();
                
                string v_localizedValue;
                if (TryGetLocalizedText(out v_localizedValue))
                    Text = v_localizedValue;

                _language = LocaleManager.Instance.CurrentLanguage;

                if(enabled && gameObject.activeInHierarchy)
                    RegisterEvents();
            }

            _lastLocalizedValue = Text;
        }

        protected bool TryGetLocalizedText(out string p_localizedValue)
        {
            bool v_sucess = false;
            if ((!string.IsNullOrEmpty(m_key) || m_autoTrackKey) && LocaleManager.InstanceExists())
            {
                // If no localization key has been specified, use the label's/Input's text as the key
                if (m_autoTrackKey && string.IsNullOrEmpty(_language) && string.IsNullOrEmpty(m_key))
                {
                    m_key = Text;
                }

                // If we still don't have a key, leave the value as blank
                if (string.IsNullOrEmpty(m_key))
                    p_localizedValue = "";
                else
                {
                    v_sucess = LocaleManager.TryGetLocalizedText(m_key, out p_localizedValue, m_supportLocaleRichTextTags);
                }

                //Return Key value if localization is empty
                if(!v_sucess)
                    p_localizedValue = m_key;
            }
            else
                p_localizedValue = m_key != null? m_key : "";

            return v_sucess;
        }

        #endregion

        #region Receivers

        protected virtual void HandleOnLocalize(bool p_forceReapply)
        {
            if (p_forceReapply || !string.Equals(Language, LocaleManager.Instance.CurrentLanguage, StringComparison.InvariantCultureIgnoreCase))
                SetDirty();
        }

        protected virtual void HandleOnVerticesDirty()
        {
            if (!m_autoTrackKey || !Application.isPlaying)
                return;

            string v_componentText = Text;
            if (_lastLocalizedValue != v_componentText)
            {
                //Update the key based in the value of GraphicComponent.text
                Key = v_componentText;
            }
        }

        private void HandleEditorReplaceLocaleTagsDelayed()
        {
            HandleEditorReplaceLocaleTags();
            CancelInvoke("HandleEditorReplaceLocaleTags");
            Invoke("HandleEditorReplaceLocaleTags", 0.1f);
        }

        private void HandleEditorReplaceLocaleTags()
        {
            if (this != null && !Application.isPlaying && m_autoTrackKey)
            {
                var v_text = RegexUtils.BulkReplace(Text, new Dictionary<string, string> {
                    { @"<locale>", "" },
                    { @"<\/locale>", "" },
                    { @"<localeparam=([0-9])+>", "" },
                    { @"<\/localeparam>", "" },
                    { @"<skiplocale>", "" }});
                if (v_text != Text)
                    Text = v_text;
            }
        }

        #endregion
    }
}