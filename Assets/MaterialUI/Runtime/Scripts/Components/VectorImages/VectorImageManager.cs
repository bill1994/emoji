// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MaterialUI
{
    public static class VectorImageManager
    {
        public static int currentPack;

        //public const string materialDesignIconsFolderPath = "Assets/MaterialUI/Fonts/Resources/Material Design Icons";

        public const string rootFolder = "/MaterialUIEssentials/";
        
        public const string materialUIIconsFontName = "MaterialUI Icons";
        public const string materialDesignIconsFontName = "Material Design Icons";

        public const string defaultFontDestinationFolder = "Resources/Vector Fonts/";

        private const string prefFontDestinationFolder = "PREF_CUSTOM_VECTOR_FONT_DESTINATION_FOLDER";
		public static string fontDestinationFolder
		{
			get { return PlayerPrefs.GetString(prefFontDestinationFolder, "Resources/Custom Vector Fonts"); /*+ "/Resources";*/ }
			set
			{
				PlayerPrefs.SetString(prefFontDestinationFolder, value);
				PlayerPrefs.Save();
			}
		}

        private static string[] GetFontDirectories(string fontsPath)
        {
            if (Directory.Exists(fontsPath))
            {
                return Directory.GetDirectories(fontsPath);
            }
            return new string[0];
        }

        public static string[] GetAllIconSetNames()
        {
            string fontsPath = Application.dataPath + rootFolder + fontDestinationFolder;
            string defaultFontsPath = Application.dataPath + rootFolder + defaultFontDestinationFolder;

            List<string> fontStringNames = new List<string>(GetFontDirectories(defaultFontsPath));
            if (defaultFontsPath != fontsPath)
            {
                fontStringNames.AddRange(GetFontDirectories(fontsPath));
            }

            for (int i = 0; i < fontStringNames.Count; i++)
            {
                fontStringNames[i] = new DirectoryInfo(fontStringNames[i].Replace("\\", "/")).Name;
            }
            return fontStringNames.ToArray();
        }

        public static bool IsMaterialDesignIconFont(string fontName)
        {
            return fontName == materialDesignIconsFontName;
        }

        public static bool IsMaterialUIIconFont(string fontName)
        {
            return fontName == materialUIIconsFontName;
        }

        public static VectorImageFont GetIconFont(string name)
        {
            var trueName = !name.EndsWith(VectorImageFont.VECTOR_FONT_SUFIX) ? name + VectorImageFont.VECTOR_FONT_SUFIX : name;
            var folder = GetPathRelativeToResources(defaultFontDestinationFolder);

            var asset = Resources.Load<VectorImageFont>(name + "/" + trueName);
            if(asset == null && !string.IsNullOrEmpty(folder))
                asset = Resources.Load<VectorImageFont>(folder + "/" + name + "/" + trueName);
            if (asset == null)
            {
                var customfolder = GetPathRelativeToResources(fontDestinationFolder);
                if (!string.IsNullOrEmpty(customfolder))
                    asset = Resources.Load<VectorImageFont>(customfolder + "/" + name + "/" + trueName);
            }

            return asset;
        }

        public static VectorImageSet GetIconSet(string name)
        {
            var folder = GetPathRelativeToResources(defaultFontDestinationFolder);

            var asset = Resources.Load<TextAsset>(name + "/" + name);
            if (asset == null && !string.IsNullOrEmpty(folder))
                asset = Resources.Load<TextAsset>(folder + "/" + name + "/" + name);
            if (asset == null)
            {
                var customfolder = GetPathRelativeToResources(fontDestinationFolder);
                if(!string.IsNullOrEmpty(customfolder))
                    asset = Resources.Load<TextAsset>(customfolder + "/" + name + "/" + name);
            }

            return JsonUtility.FromJson<VectorImageSet>(asset != null? asset.text : string.Empty);
        }

        public static string GetIconCodeFromName(string name, string setName = "*")
        {
            bool noPackSpecified = (setName == "*");

            if (noPackSpecified)
            {
                string[] setNames = GetAllIconSetNames();
                VectorImageSet[] sets = new VectorImageSet[setNames.Length];

                for (int i = 0; i < setNames.Length; i++)
                {
                    sets[i] = GetIconSet(setNames[i]);
                }

                for (int i = 0; i < sets.Length; i++)
                {
                    for (int j = 0; j < sets[i].iconGlyphList.Count; j++)
                    {
                        if (name == sets[i].iconGlyphList[j].name)
                        {
                            return sets[i].iconGlyphList[j].unicode;
                        }
                    }
                }
            }
            else
            {
                VectorImageSet set = GetIconSet(setName);

                for (int j = 0; j < set.iconGlyphList.Count; j++)
                {
                    if (name == set.iconGlyphList[j].name)
                    {
                        return set.iconGlyphList[j].unicode;
                    }
                }
            }
            return null;
        }

        public static string GetIconNameFromCode(string code, string setName = "*")
        {
            bool noPackSpecified = (setName == "*");

            if (noPackSpecified)
            {
                string[] setNames = GetAllIconSetNames();
                VectorImageSet[] sets = new VectorImageSet[setNames.Length];

                for (int i = 0; i < setNames.Length; i++)
                {
                    sets[i] = GetIconSet(setNames[i]);
                }

                for (int i = 0; i < sets.Length; i++)
                {
                    for (int j = 0; j < sets[i].iconGlyphList.Count; j++)
                    {
                        if (code == sets[i].iconGlyphList[j].unicode)
                        {
                            return sets[i].iconGlyphList[j].name;
                        }
                    }
                }
            }
            else
            {
                VectorImageSet set = GetIconSet(setName);

                for (int j = 0; j < set.iconGlyphList.Count; j++)
                {
                    if (code == set.iconGlyphList[j].unicode)
                    {
                        return set.iconGlyphList[j].name;
                    }
                }
            }
            return null;
        }

        static string GetPathRelativeToResources(string path)
        {
            if (path.Contains("Resources"))
            {
                var customFolderArr = path.Split(new string[] { "Resources" }, System.StringSplitOptions.RemoveEmptyEntries);
                var customfolder = customFolderArr.Length > 0 ? customFolderArr[customFolderArr.Length - 1] : string.Empty;

                while (customfolder.Length > 0 && (customfolder.EndsWith("/") || customfolder.EndsWith("\\")))
                {
                    customfolder = customfolder.Length - 1 == 0 ? string.Empty : customfolder.Substring(0, customfolder.Length - 1);
                }
                while (customfolder.Length > 0 && (customfolder.StartsWith("/") || customfolder.StartsWith("\\")))
                {
                    customfolder = customfolder.Length - 1 == 0 ? string.Empty : customfolder.Substring(1, customfolder.Length - 1);
                }
                return customfolder;
            }
            return null;
        }
    }
}