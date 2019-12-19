using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ScrollReceiver : MonoBehaviour 
{	
	#region Private Variables

	[SerializeField, Range(0,1)]
	float m_minValue = 0.001f;
	[SerializeField, Range(0,1)]
	float m_maxValue = 0.999f;

	#endregion

	#region Callbacks
	
	public FloatUnityEvent OnValueChanged;
	public UnityEvent OnValueMin;
	public UnityEvent OnValueMax;
	
	#endregion

	#region Public Properties

	public float MinValue
	{
		get
		{
			m_minValue = Mathf.Clamp(m_minValue, 0, 1);
			return m_minValue;
		}
		set
		{
			if(m_minValue == value)
				return;
			m_minValue = value;
		}
	}

	public float MaxValue
	{
		get
		{
			m_maxValue = Mathf.Clamp(m_maxValue, 0, 1);
			return m_maxValue;
		}
		set
		{
			if(m_maxValue == value)
				return;
			m_maxValue = value;
		}
	}

	#endregion

	#region Unity Functions
	
	protected virtual void Awake()
	{
		if(Application.isPlaying)
			RegisterEvents();
		else
			_oldValue = GetScrollValue();
	}

	protected virtual void OnDestroy()
	{
		if(Application.isPlaying)
			UnregisterEvents();
	}
	
	protected virtual void Update()
	{
		if(!Application.isPlaying && Application.isEditor)
			CheckValue();
	}
	
	#endregion
	
	#region Events Receiver

	public virtual void HandleOnValueChanged(Vector2 p_value)
	{
		ScrollRect v_rect = GetComponent<ScrollRect>();
		if(v_rect.verticalScrollbar != null)
			HandleOnValueChanged(p_value.y);
		else if(v_rect.horizontalScrollbar != null)
			HandleOnValueChanged(p_value.x);
	}
	
	public virtual void HandleOnValueChanged(float p_value)
	{
		if(OnValueChanged != null)
			OnValueChanged.Invoke(p_value);
		if(_oldValue > MinValue && _oldValue < MaxValue)
		{
			if(p_value <=  MinValue)
			{
				if(OnValueMin != null)
					OnValueMin.Invoke();
			}
			else if(p_value >= MaxValue)
			{
				if(OnValueMax != null)
					OnValueMax.Invoke();
			}
		}
		else
		{
			_oldValue = p_value;
		}
	}
	
	#endregion
	
	#region Helper Functions
	
	public void RegisterEvents()
	{
		UnregisterEvents();
		ScrollRect v_rect = GetComponent<ScrollRect>();
		if(v_rect != null)
		{
			v_rect.onValueChanged.AddListener(HandleOnValueChanged);
		}
		Scrollbar v_scroll = GetComponent<Scrollbar>();
		if(v_scroll != null)
		{
			v_scroll.onValueChanged.AddListener(HandleOnValueChanged);
		}
	}
	
	public void UnregisterEvents()
	{
		ScrollRect v_rect = GetComponent<ScrollRect>();
		if(v_rect != null)
		{
			v_rect.onValueChanged.RemoveListener(HandleOnValueChanged);
		}
		Scrollbar v_scroll = GetComponent<Scrollbar>();
		if(v_scroll != null)
		{
			v_scroll.onValueChanged.RemoveListener(HandleOnValueChanged);
		}
	}
	
	float _oldValue = 0;
	protected virtual void CheckValue()
	{
		float v_getScroll = GetScrollValue();
		if(_oldValue != v_getScroll && v_getScroll >= 0)
		{
			HandleOnValueChanged(v_getScroll);
			_oldValue = v_getScroll;
		}
	}
	
	public virtual float GetScrollValue()
	{
		ScrollRect v_rect = GetComponent<ScrollRect>();
		if(v_rect != null)
		{
			if(v_rect.verticalScrollbar != null)
				return v_rect.verticalScrollbar.value;
			else if(v_rect.horizontalScrollbar != null)
				return v_rect.horizontalScrollbar.value;
		}
		Scrollbar v_scroll = GetComponent<Scrollbar>();
		if(v_scroll != null)
			return v_scroll.value;
		return -1;
	}

    public virtual void IncrementScrollValue(float p_value)
    {
        SetScrollValue(GetScrollValue() + p_value, true);
    }

    public virtual void SetScrollValue(float p_value)
	{
		SetScrollValue(p_value, true);
	}
	
	public virtual void SetScrollValue(float p_value, bool p_callEvents)
	{
		if(!p_callEvents)
			UnregisterEvents();
		else
			RegisterEvents();
		ScrollRect v_rect = GetComponent<ScrollRect>();
		if(v_rect != null)
		{
			if(v_rect.verticalScrollbar != null)
				v_rect.verticalScrollbar.value = p_value;
			else if(v_rect.horizontalScrollbar != null)
				v_rect.horizontalScrollbar.value = p_value;
			if(Application.isPlaying && p_callEvents)
				CheckValue();
		}
		Scrollbar v_scroll = GetComponent<Scrollbar>();
		if(v_scroll != null)
		{
			v_scroll.value = p_value;
			if(Application.isPlaying && p_callEvents)
				CheckValue();
		}
		if(!p_callEvents)
			RegisterEvents();
	}
	
	#endregion

	#region Helper Classes
	
	[System.Serializable]
	public class FloatUnityEvent : UnityEvent<float>
	{
	}
	
	#endregion
}
