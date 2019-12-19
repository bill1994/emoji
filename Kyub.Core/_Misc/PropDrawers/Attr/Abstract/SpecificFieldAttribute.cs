using UnityEngine;
using System.Collections;

public abstract class SpecificFieldAttribute : PropertyAttribute
{
	#region Properties

	[SerializeField]
	bool m_readOnly = false;
	public bool ReadOnly
	{
		get
		{
			return m_readOnly;
		}
		set
		{
			if(m_readOnly == value)
				return;
			m_readOnly = value;
		}
	}

	[SerializeField]
	System.Type m_acceptedType = default(System.Type);
	public System.Type AcceptedType
	{
		get
		{
			return m_acceptedType;
		}
		set
		{
			if(m_acceptedType == value)
				return;
			m_acceptedType = value;
		}
	}

	#endregion

	#region Constructor

	public SpecificFieldAttribute(System.Type p_acceptedType)
	{
		m_acceptedType = p_acceptedType == null? typeof(object) : p_acceptedType;
	}

	public SpecificFieldAttribute(bool p_readOnly, System.Type p_acceptedType)
	{
		m_readOnly = p_readOnly;
		m_acceptedType = p_acceptedType == null? typeof(object) : p_acceptedType;
	}

	#endregion
}