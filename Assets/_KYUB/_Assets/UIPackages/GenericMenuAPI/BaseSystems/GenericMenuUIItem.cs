using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Kyub.UI
{

    public class GenericMenuUIItem : ScrollDataViewElement, IEventSystemHandler, IPointerEnterHandler, IPointerClickHandler, ICancelHandler
    {
        [SerializeField]
        bool m_selectFolderOnPointerEnter = false;
        [SerializeField]
        private Text m_text = null;
        [SerializeField]
        private Transform m_folderIconContainer = null;
        [SerializeField]
        private Image m_image = null;
        [SerializeField]
        private RectTransform m_rectTransform = null;
        [SerializeField]
        private Toggle m_toggle = null;

        public Text Text
        {
            get
            {
                return this.m_text;
            }
            set
            {
                this.m_text = value;
            }
        }

        public Image Image
        {
            get
            {
                return this.m_image;
            }
            set
            {
                this.m_image = value;
            }
        }

        public RectTransform RectTransform
        {
            get
            {
                return this.m_rectTransform;
            }
            set
            {
                this.m_rectTransform = value;
            }
        }

        public Toggle Toggle
        {
            get
            {
                return this.m_toggle;
            }
            set
            {
                this.m_toggle = value;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
                Reload();
        }

        bool _started = false;
        protected virtual void Start()
        {
            _started = true;
            Reload();
        }

        public virtual void OnCancel(BaseEventData eventData)
        {
            /*Dropdown componentInParent = this.GetComponentInParent<Dropdown>();
            if (!(bool)((UnityEngine.Object)componentInParent))
                return;
            componentInParent.Hide();*/
        }

        protected override void ApplyReload(ScrollDataView.ReloadEventArgs p_oldArgs, ScrollDataView.ReloadEventArgs p_newArgs)
        {
            var v_data = p_newArgs.Data as GenericMenuElementData;
            var v_sender = p_newArgs.Sender != null ? p_newArgs.Sender.GetComponentInParent<GenericMenuUI>() : null;
            if (v_sender == null && p_newArgs.Sender != null)
            {
                var v_config = p_newArgs.Sender.GetComponentInParent<GenericMenuUIPage>();
                if (v_config != null)
                    v_sender = v_config.Parent;
            }
            if (v_data != null)
            {
                if (m_text != null)
                    m_text.text = v_data.Name;
                if (m_image != null)
                    m_image.sprite = v_data.Icon;
                if (m_toggle != null)
                {
                    var v_index = v_sender != null && v_sender != null ? v_sender.IndexOf(v_data) : -1;
                    if (v_data.IsFolder)
                    {
                        var v_item = v_sender != null? v_sender.GetCurrentSelectedItem() : null;
                        m_toggle.isOn = v_item != null && v_item.GetPath().StartsWith(v_data.GetPath());
                    }
                    else
                    {
                        
                        m_toggle.isOn = v_sender != null && v_sender != null && v_index >= 0 && v_sender.SelectedIndex == v_index;
                    }
                }
                if (m_folderIconContainer != null)
                    m_folderIconContainer.gameObject.SetActive(v_data.IsFolder);
            }
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            EventSystem.current.SetSelectedGameObject(this.gameObject);
            if (m_selectFolderOnPointerEnter)
            {
                var v_data = _cachedReloadEventArgs.Data as GenericMenuElementData;
                if(v_data != null && v_data.IsFolder)
                    SelectData();
            }

        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SelectData();
        }

        protected virtual void SelectData()
        {
            var v_data = _cachedReloadEventArgs.Data as GenericMenuElementData;
            var v_sender = _cachedReloadEventArgs.Sender != null ? _cachedReloadEventArgs.Sender.GetComponentInParent<GenericMenuUI>() : null;
            if (v_sender == null && _cachedReloadEventArgs.Sender != null)
            {
                var v_config = _cachedReloadEventArgs.Sender.GetComponentInParent<GenericMenuUIPage>();
                if (v_config != null)
                    v_sender = v_config.Parent;
            }

            if (v_sender != null && v_data != null)
            {
                if (v_data.IsFolder)
                {
                    if (m_toggle != null)
                    {
                        var v_item = v_sender != null ? v_sender.GetCurrentSelectedItem() : null;
                        m_toggle.isOn = v_item != null && v_item.GetPath().StartsWith(v_data.GetPath());
                    }
                    v_sender.SelectedFolderPath = v_data.GetPath();
                }
                else
                {
                    var v_index = v_sender.IndexOf(v_data);
                    if (m_toggle != null)
                        m_toggle.isOn = v_index >= 0;
                    v_sender.SelectedIndex = v_index;
                }
            }
        }
    }
}
