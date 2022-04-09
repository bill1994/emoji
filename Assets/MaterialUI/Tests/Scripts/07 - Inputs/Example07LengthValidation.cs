// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

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

    public virtual ITextValidator Clone()
    {
        return new Example07LengthValidation();
    }
}