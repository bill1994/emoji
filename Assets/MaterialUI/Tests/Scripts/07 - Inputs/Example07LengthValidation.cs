//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using MaterialUI;
using UnityEngine.UI;
using UnityEngine;

public class Example07LengthValidation : BaseAutoFormatTextValidator, ITextValidator
{
	public override bool IsTextValid()
    {
		if (m_MaterialInputField.text.Length <= 10)
        {
            return true;
        }
        else
        {
			m_MaterialInputField.validationText.SetGraphicText("Must be at most 10 characters");
            return false;
        }
    }
}