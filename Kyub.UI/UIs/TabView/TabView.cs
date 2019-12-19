using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Kyub.UI
{
	public class TabView : MonoBehaviour 
	{
		#region Private Variables
		[SerializeField]
		bool m_canChangeSimblingIndex = true;
		[SerializeField]
		bool m_acceptEmptySelectedTabs = false;
		[SerializeField]
		int m_selectedTab = 0;
        [SerializeField]
        int m_defaultTab = -1;
		[SerializeField]
		List<TabStruct> m_tabs = new List<TabStruct>();

		bool _needCheck = true;

		#endregion

		#region Public Properties

		public bool CanChangeSimblingIndex
		{
			get
			{
				return m_canChangeSimblingIndex;
			}
			set
			{
				if(m_canChangeSimblingIndex == value)
					return;
				m_canChangeSimblingIndex = value;
				if(Application.isPlaying)
					RegisterTabsEvents();
			}
		}

		public bool AcceptEmptySelectedTabs
		{
			get
			{
				return m_acceptEmptySelectedTabs;
			}
			set
			{
				if(m_acceptEmptySelectedTabs == value)
					return;
				m_acceptEmptySelectedTabs = value;
				m_selectedTab = ClampSelectionValues(m_selectedTab, m_defaultTab);
			}
		}

		public int SelectedTab
		{
			get
			{
				m_selectedTab = ClampSelectionValues(m_selectedTab, m_defaultTab);
				return m_selectedTab;
			}
			set
			{
				int v_value = ClampSelectionValues(value, m_defaultTab);
				if(m_selectedTab == v_value)
					return;
				m_selectedTab = v_value;
                ActivateTab(m_selectedTab, Application.isEditor && !Application.isPlaying);
			}
		}

        public int DefaultTab
        {
            get
            {
                m_defaultTab = ClampSelectionValues(m_defaultTab);
                return m_defaultTab;
            }
            set
            {
                int v_value = ClampSelectionValues(value);
                if (m_defaultTab == v_value)
                    return;
                m_defaultTab = v_value;
            }
        }

        public List<TabStruct> Tabs
		{
			get
			{
				if(m_tabs == null)
					m_tabs = new List<TabStruct>();
				return m_tabs;
			}
			set
			{
				if(m_tabs == value)
					return;
				m_tabs = value;
			}
		}

		#endregion

		#region Unity Functions

		protected virtual void Awake()
		{
			PerformAwakeActivation();
			RegisterTabsEvents();
		}

		protected virtual void Start () 
		{
			//ActivateTab(SelectedTab, true);
		}

		bool _firstUpdate = true;
		protected virtual void Update()
		{
			if(_firstUpdate)
			{
				_firstUpdate = false;
				ActivateTab(SelectedTab, true);
			}
			else if(_needCheck)
			{
				_needCheck = false;
                //DelayedFunctionUtils.CallFunction(new System.Action<int, bool>(ActivateTab), new object[] { SelectedTab, Application.isEditor && !Application.isPlaying }, 0.1f, true);
                ActivateTab(SelectedTab, Application.isEditor && !Application.isPlaying);
            }
        }

		#endregion

		#region Events Receivers

		public virtual void OnPanelOpening(TabStruct p_struct)
		{
			if(p_struct != null && p_struct.Panel != null)
			{
                if (p_struct.Toggle != null)
                    p_struct.Toggle.interactable = false;
				SelectedTab = GetIndexOfTab(p_struct);
			}
		}

		public virtual void OnPanelOpened(TabStruct p_struct)
		{
			if(p_struct != null && p_struct.Panel != null)
			{
                if (p_struct.Toggle != null)
                    p_struct.Toggle.interactable = true;
				SelectedTab = GetIndexOfTab(p_struct);
				_needCheck = true;
			}
		}

		public virtual void OnPanelClosing(TabStruct p_struct)
		{
			if(p_struct != null && p_struct.Panel != null)
			{
                if(p_struct.Toggle != null)
				    p_struct.Toggle.interactable = false;
				int v_newSelectedTab = GetIndexOfTab(p_struct);
				if(v_newSelectedTab == SelectedTab)
				{
					_oldSelected = SelectedTab;
					SelectedTab = -1;
				}
			}
		}

		public virtual void OnPanelClosed(TabStruct p_struct)
		{
			if(p_struct != null && p_struct.Panel != null)
			{
                if(p_struct.Toggle != null)
				    p_struct.Toggle.interactable = true;
				int v_newSelectedTab = GetIndexOfTab(p_struct);
				if(v_newSelectedTab == SelectedTab)
				{
					_oldSelected = SelectedTab;
					SelectedTab = -1;
					_needCheck = true;
				}
			}
		}

		int _oldSelected = -1;
		public virtual void OnToggleChanged(TabStruct p_struct, bool p_enabled)
		{
			if(p_struct != null && p_struct.Toggle != null)
			{
				int v_newSelectedTab = GetIndexOfTab(p_struct);
				if(p_enabled)
				{
					SelectedTab = v_newSelectedTab;
					_needCheck = true;
				}
				else
				{
					if(v_newSelectedTab == SelectedTab)
					{
						_oldSelected = SelectedTab;
						SelectedTab = -1;
						_needCheck = true;
					}
				}

			}
		}

		#endregion

		#region Helper Functions

		protected virtual void PerformAwakeActivation()
		{
			for(int i=0; i<Tabs.Count; i++)
			{
				if(Tabs[i] != null && Tabs[i].Panel != null)
					Tabs[i].Panel.Show(true);
			}
		}

		protected virtual int GetIndexOfTab(TabStruct p_tab)
		{
			for(int i=0; i<Tabs.Count; i++)
			{
				if(Tabs[i] == p_tab)
				   return i;
			}
			return -1;
		}

		protected virtual int ClampSelectionValues(int p_value, int p_defaultValue = -1)
		{
			int v_value = Tabs.Count > 0? Mathf.Clamp(p_value, m_acceptEmptySelectedTabs? -1 : 0, Tabs.Count-1) : -1;
            if (v_value == -1)
                v_value = p_defaultValue;
            return v_value;
		}

		protected virtual void RegisterTabsEvents()
		{
			foreach(TabStruct v_tab in Tabs)
			{
				if(v_tab != null)
				{
					v_tab.RegisterEvents(this);
				}
			}
		}

		protected virtual void UnregisterTabsEvents()
		{
			foreach(TabStruct v_tab in Tabs)
			{
				if(v_tab != null)
				{
					v_tab.UnregisterEvents(this);
				}
			}
		}

		public virtual void ForceActivateTab()
		{
			ForceActivateTab(SelectedTab);
		}

		public virtual void ForceActivateTab(int p_index)
		{
			//We Must Deactivate all toggles before activate selected one!
			int v_index = ClampSelectionValues(p_index);
			foreach(TabStruct v_tab in Tabs)
			{
				if(v_tab != null && v_tab.Toggle != null)
				{
                    if (Tabs[v_index] != v_tab && v_tab.Toggle != null)
                    {
                        v_tab.Toggle.isOn = !v_tab.Toggle.isOn;
                        v_tab.Toggle.isOn = false;
                    }
				}
			}
			if(v_index >=0 && v_index < Tabs.Count && Tabs[v_index] != null)
			{
                if (Tabs[v_index].Toggle != null)
                {
                    Tabs[v_index].Toggle.isOn = !Tabs[v_index].Toggle.isOn;
                    Tabs[v_index].Toggle.isOn = true;
                }
                else
                    ActivateTab(p_index);
			}
		}

        public void ActivateTabDelayed(int v_index)
        {
            ActivateTabDelayed(v_index, false);
        }

        public virtual void ActivateTabDelayed(int v_index, bool p_finish)
        {
            if (enabled && gameObject.activeInHierarchy && gameObject.activeSelf)
            {
                StopCoroutine("ActivateTabDelayedRoutine");
                StartCoroutine(ActivateTabDelayedRoutine(v_index, p_finish));
            }
            else
                ActivateTab(v_index, p_finish);
        }

        protected virtual IEnumerator ActivateTabDelayedRoutine(int v_index, bool p_finish)
        {
            yield return null;
            ActivateTab(v_index, p_finish);
        }

        public void ActivateTab(int v_index)
		{
			ActivateTab(v_index, false);
		}

		public virtual void ActivateTab(int v_index, bool p_finish)
		{
			v_index = ClampSelectionValues(v_index);
			if(Application.isPlaying)
			{
				for(int i=0; i< Tabs.Count; i++)
				{
					if(i != v_index && Tabs[i].Toggle != null)
					{
						Tabs[i].Toggle.isOn = false;
					}
				}
			}
			for(int i=0; i< Tabs.Count; i++)
			{
				if(i != v_index)
				{
					if(i != _oldSelected || !Application.isPlaying)
						Tabs[i].Hide(false);
					else
						Tabs[i].Hide();
				}
			}
			_oldSelected = -1;
			if(Tabs.Count > v_index && v_index >= 0)
			{
				Tabs[v_index].Show((Application.isEditor && !Application.isPlaying) || p_finish);
			}
		}

		#endregion
	}

	[System.Serializable]
	public class TabStruct
	{
		#region Private Variables

		[SerializeField]
		Toggle m_toggle = null;
		[SerializeField]
		TweenContainer m_panel = null;

		TabView _tabViewReference = null;
		#endregion

		#region Public Properties

		public Toggle Toggle
		{
			get
			{
				return m_toggle;
			}
			set
			{
				if(m_toggle == value)
					return;
				if(Application.isPlaying)
					UnregisterEvents();
				m_toggle = value;
				if(Application.isPlaying)
					RegisterEvents();
			}
		}

		public TweenContainer Panel
		{
			get
			{
				return m_panel;
			}
			set
			{
				if(m_panel == value)
					return;
				if(Application.isPlaying)
					UnregisterEvents();
				m_panel = value;
				if(Application.isPlaying)
					RegisterEvents();
			}
		}

		#endregion

		#region Helper Functions

		public void Show()
		{
			Show(false);
		}

        public virtual void Show(bool p_finish)
        {
            if (Panel != null)
            {
                Panel.Show(p_finish);
                if (_tabViewReference != null && _tabViewReference.CanChangeSimblingIndex)
                    Panel.transform.SetAsLastSibling();
            }
            if (Application.isEditor && !Application.isPlaying)
            {
                if (Toggle != null)
                    Toggle.isOn = true;
                if (Panel != null && !Panel.gameObject.activeSelf)
                    Panel.gameObject.SetActive(true);
            }
            else
            {
                UnregisterEvents();
                if (Toggle != null && !Toggle.isOn)
                    Toggle.isOn = true;
                RegisterEvents();
            }
        }

		public void Hide()
		{
			Hide(false);
		}

        public virtual void Hide(bool p_finish)
        {
            if (Panel != null)
            {
                Panel.Hide(p_finish);
                if (_tabViewReference != null && _tabViewReference.CanChangeSimblingIndex)
                    Panel.transform.SetAsFirstSibling();
            }
            if (Application.isEditor && !Application.isPlaying)
            {
                if (Toggle != null)
                    Toggle.isOn = false;
                if (Panel != null && Panel.CloseSpecialAction == CloseSpecialActionEnum.Deactivate && Panel.gameObject.activeSelf)
                    Panel.gameObject.SetActive(false);
            }
            else
            {
                UnregisterEvents();
                if (Toggle != null && Toggle.isOn)
                    Toggle.isOn = false;
                RegisterEvents();
            }
        }

		public void RegisterEvents()
		{
			RegisterEvents(_tabViewReference);
		}

		public virtual void RegisterEvents(TabView p_tabViewReference)
		{
            UnregisterEvents(p_tabViewReference);
            _tabViewReference = p_tabViewReference;
			if(_tabViewReference != null)
			{
                //Toggle
                if (Toggle != null)
                    Toggle.onValueChanged.AddListener(OnToggleChanged);
                //Panel
                if (Panel != null)
                {
                    AddListener(Panel.OnOpeningCallBack, OnPanelOpening);
                    AddListener(Panel.OnOpenedCallBack, OnPanelOpened);
                    AddListener(Panel.OnClosingCallBack, OnPanelClosing);
                    AddListener(Panel.OnClosedCallBack, OnPanelClosed);
                }
			}
		}

		public void UnregisterEvents()
		{
			UnregisterEvents(_tabViewReference);
		}

		public virtual void UnregisterEvents(TabView p_tabViewReference)
		{
			_tabViewReference = p_tabViewReference;
			if(_tabViewReference != null)
			{
                //Toggle
                if (Toggle != null)
                    Toggle.onValueChanged.RemoveListener(OnToggleChanged);
                //Panel
                if (Panel != null)
                {
                    RemoveListener(Panel.OnOpeningCallBack, OnPanelOpening);
                    RemoveListener(Panel.OnOpenedCallBack, OnPanelOpened);
                    RemoveListener(Panel.OnClosingCallBack, OnPanelClosing);
                    RemoveListener(Panel.OnClosedCallBack, OnPanelClosed);
                }
			}
		}

		protected virtual void AddListener(UnityEvent p_event, UnityAction p_action)
		{
			if(p_event != null && p_action != null)
			{
				p_event.RemoveListener(p_action);
				p_event.AddListener(p_action);
			}
		}

		protected virtual void RemoveListener(UnityEvent p_event, UnityAction p_action)
		{
			if(p_event != null && p_action != null)
			{
				p_event.RemoveListener(p_action);
			}
		}

		protected virtual void OnToggleChanged(bool p_value)
		{
			if(Toggle != null && _tabViewReference != null)
			{
				_tabViewReference.OnToggleChanged(this, p_value);
			}
		}
		
		protected virtual void OnPanelOpening()
		{
			if(Panel != null && _tabViewReference != null)
			{
				_tabViewReference.OnPanelOpening(this);
			}
		}
		
		protected virtual void OnPanelOpened()
		{
			if(Panel != null && _tabViewReference != null)
			{
				_tabViewReference.OnPanelOpened(this);
			}
		}
		
		protected virtual void OnPanelClosing()
		{
			if(Panel != null && _tabViewReference != null)
			{
				_tabViewReference.OnPanelClosing(this);
			}
		}
		
		protected virtual void OnPanelClosed()
		{
			if(Panel != null && _tabViewReference != null)
			{
				_tabViewReference.OnPanelClosed(this);
			}
		}

		#endregion
	}
}
