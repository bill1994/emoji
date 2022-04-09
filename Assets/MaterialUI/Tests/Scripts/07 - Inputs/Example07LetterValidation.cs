// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System.Text.RegularExpressions;
using MaterialUI;
using UnityEngine.UI;
using UnityEngine;

public class Example07LetterValidation : BaseTextValidator, ITextValidator
{
	public override bool IsTextValid()
    {
		if (new Regex("[^a-zA-Z ]").IsMatch(m_MaterialInputField.text))
        {
			m_MaterialInputField.validationText.SetGraphicText("Must only contain letters");
            return false;
        }
        else
        {
            return true;
        }
    }

    public virtual ITextValidator Clone()
    {
        return new Example07LetterValidation();
    }
}