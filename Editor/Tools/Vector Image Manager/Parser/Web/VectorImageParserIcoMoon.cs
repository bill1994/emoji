// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace MaterialUI
{
#if UNITY_EDITOR
    public class VectorImageParserIcoMoon : VectorImageFontParser
    {
        protected override string GetIconFontUrl()
        {
            return "https://github.com/Keyamoon/IcoMoon-Free/blob/master/Font/IcoMoon-Free.ttf?raw=true";
        }

        protected override string GetIconFontLicenseUrl()
        {
            return "https://github.com/Keyamoon/IcoMoon-Free/blob/master/License.txt?raw=true";
        }

        protected override string GetIconFontDataUrl()
        {
            return "https://github.com/Keyamoon/IcoMoon-Free/raw/master/Font/selection.json?raw=true";
        }

        public override string GetWebsite()
        {
            return "https://icomoon.io/#preview-free";
        }

        public override string GetFontName()
        {
            return "IcoMoon";
        }

        protected override VectorImageSet GenerateIconSet(string fontDataContent)
        {
            return GenerateSpecificIconSet(fontDataContent);
        }

        public static VectorImageSet GenerateSpecificIconSet(string fontDataContent)
        {
            VectorImageSet vectorImageSet = new VectorImageSet();
            //Glyph currentGlyph = null;
            // Pattern to Match
            // icon-my-icon-name:before {
            //    content: "\5a"; }
            var pattern = @"icon-(.*):before[\n ]*{[\n ]*content:[ '""\\]+([0-9a-fA-F]+)['"";]+";
            var matches = System.Text.RegularExpressions.Regex.Matches(fontDataContent, pattern);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count >= 3)
                {
                    var currentGlyph = new Glyph();
                    currentGlyph.name = match.Groups[1].ToString();

                    //Now convert to hex X4 format
                    var hexUnicode = match.Groups[2].ToString();
                    int intUnicode = 0;
                    int.TryParse(hexUnicode, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out intUnicode);
                    currentGlyph.unicode = intUnicode.ToString("X4").ToLower();

                    //add to ImageSet
                    vectorImageSet.iconGlyphList.Add(currentGlyph);
                }
            }

            return vectorImageSet;
        }

        protected override string ExtractLicense(string fontDataLicenseContent)
        {
            return fontDataLicenseContent;
        }
    }
#endif
}
