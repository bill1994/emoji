// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEngine;
using MaterialUI;

public class Example12 : MonoBehaviour
{
    private readonly Color[] m_BarColors =
    {
        MaterialColor.teal500,
        Color.white,
        MaterialColor.grey800
    };

    [SerializeField]
    private MaterialAppBar m_MaterialAppBar = null;

    public void ChangeBarColor(int i)
    {
        Color barColor = i < m_BarColors.Length ? m_BarColors[i] : m_MaterialAppBar.panelGraphic.color.WithAlpha(0f);

        m_MaterialAppBar.SetPanelColor(barColor, true);
        m_MaterialAppBar.SetButtonsGraphicColors(MaterialColor.LightOrDarkElements(barColor) ? MaterialColor.iconLight : MaterialColor.iconDark, MaterialAppBar.ButtonElement.Icon, true);
        m_MaterialAppBar.SetTitleTextColor(MaterialColor.LightOrDarkElements(barColor) ? MaterialColor.textLight : MaterialColor.textDark, true);
    }
}
