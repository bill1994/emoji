// This File will try to copy all fonts used in project to StreamingAssets/res/font before build (and remove after build).
// Doing this we can access TTF files in Native Platforms to draw using NativeInputField.

using KyubEditor.Typography.OpenFont;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Kyub.Internal.NativeInputPlugin;
using UnityEditor.iOS.Xcode;
using UnityEditor.Android;
using System.Runtime.CompilerServices;

namespace KyubEditor.Internal.NativeInputPlugin
{
    class NativeFontsProcessor : IPostprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {

        #region Fields and Properties

        public int callbackOrder { get { return 0; } }

        #endregion

        #region Processor Functions

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS)
            {
                var xcodePath = report.summary.outputPath;
                //Copy FontAssets to XCodeProject StreamingAssetPath
                var fontPath = PathCombine(xcodePath, GetXCodeFontRelativePath());
                var fontFiles = GetAllUsedFont();
                CopyToPath(fontPath, fontFiles);

                //Modify PList
                AddFontsToXCodePlist(xcodePath);
            }
        }

        public void OnPostGenerateGradleAndroidProject(string grandlePath)
        {
            //Copy FontAssets to Gradle StreamingAssetPath
            var fontPath = PathCombine(grandlePath, GetGradleFontRelativePath());
            var fontFiles = GetAllUsedFont();
            CopyToPath(fontPath, fontFiles);
        }



        #endregion

        #region Helper Static Functions

        protected static void AddFontsToXCodePlist(string xcodePath)
        {
            try
            {
                // Get plist
                string plistPath = PathCombine(xcodePath, "Info.plist");
                PlistDocument plist = new PlistDocument();
                plist.ReadFromString(File.ReadAllText(plistPath));

                // Get root
                PlistElementDict rootDict = plist.root;

                // Change value of CFBundleVersion in Xcode plist
                const string uiFontKey = "UIAppFonts";
                PlistElement appFontsListUncasted = null;
                if (!rootDict.values.TryGetValue(uiFontKey, out appFontsListUncasted) || appFontsListUncasted == null)
                    appFontsListUncasted = rootDict.CreateArray(uiFontKey);
                PlistElementArray uiFontArray = appFontsListUncasted.AsArray();

                //Pick previous added elements in PList (someone already changed this)
                HashSet<string> fontFiles = new HashSet<string>();
                foreach (var element in uiFontArray.values)
                {
                    var fontFile = element != null ? element.AsString() : string.Empty;
                    if (!string.IsNullOrEmpty(fontFile))
                        fontFiles.Add(fontFile);
                }
                uiFontArray.values.Clear();

                var xcodeFontRelativePath = GetXCodeFontRelativePath();
                var xcodeFontFullpath = PathCombine(xcodePath, xcodeFontRelativePath);
                var files = Directory.GetFiles(xcodeFontFullpath);
                //Add Fonts Previous
                if (files != null)
                {
                    HashSet<string> validExtensions = new HashSet<string>() { ".ttf", ".otf" };
                    foreach (var copyElement in files)
                    {
                        var extension = Path.GetFileName(copyElement).ToLower();
                        if (!string.IsNullOrEmpty(extension) && (validExtensions.Count == 0 || validExtensions.Contains(extension)))
                        {
                            var copyElementFileName = PathCombine(xcodeFontRelativePath, Path.GetFileName(copyElement));
                            fontFiles.Add(copyElementFileName);
                        }
                    }
                }

                //Merge Plist with new CustomFonts
                foreach (var element in fontFiles)
                {
                    if (!string.IsNullOrEmpty(element))
                        uiFontArray.AddString(element);
                }

                // Write to file
                File.WriteAllText(plistPath, plist.WriteToString());
            }
            catch { }
        }

        protected static HashSet<string> CopyToPath(string rootPath, HashSet<Font> fonts)
        {
            HashSet<string> copyFiles = new HashSet<string>();
            if (fonts != null)
            {
                PostScriptTableData table = new PostScriptTableData();
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);
                foreach (var font in fonts)
                {
                    var filePath = AssetDatabase.GetAssetPath(font);
                    var postScriptName = GeneratePostScriptName(filePath);
                    table.AddEntry(font, postScriptName);
                    var copyPath = PathCombine(rootPath, postScriptName);
                    try
                    {
                        if (!File.Exists(copyPath))
                        {
                            File.Copy(filePath, copyPath, false);
                            copyFiles.Add(copyPath);
                        }
                    }
                    catch { }
                }

                var postScriptTablePath = PathCombine(rootPath, PostScriptNameUtils.POST_SCRIPT_TABLE);
                copyFiles.Add(postScriptTablePath);
                File.WriteAllText(postScriptTablePath, JsonUtility.ToJson(table));
            }

            return copyFiles;
        }

        protected static HashSet<Font> GetAllUsedFont()
        {
            List<Object> objectsToTrack = new List<Object>();
            objectsToTrack.AddRange(Resources.LoadAll<GameObject>(""));
            foreach (var editorScene in EditorBuildSettings.scenes)
            {
                if (editorScene.enabled)
                {
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(editorScene.path);
                    if (sceneAsset != null)
                        objectsToTrack.Add(sceneAsset);
                }
            }

            var possibleFonts = new HashSet<Object>(EditorUtility.CollectDependencies(objectsToTrack.ToArray()));
            possibleFonts.UnionWith(Resources.LoadAll<Font>(""));

            HashSet<Font> fonts = new HashSet<Font>();
            foreach (var content in possibleFonts)
            {
                var font = content as Font;
                if (font != null && font.dynamic)
                {
                    //Only Include Dynamic fonts with includeFontData (this property can only be obtained in editor using TrueTypeFontImporter)
                    var fontPath = AssetDatabase.GetAssetPath(font);
                    var assetImporter = TrueTypeFontImporter.GetAtPath(fontPath) as TrueTypeFontImporter;
                    if (assetImporter != null && assetImporter.includeFontData)
                        fonts.Add(font);
                }
            }

            return fonts;
        }

        protected static string GeneratePostScriptName(string fontFile)
        {
            using (var filestream = new FileStream(fontFile, FileMode.Open))
            {
                var fontReader = new OpenFontReader();
                var preview = fontReader.ReadPreview(filestream);
                var extension = Path.GetExtension(fontFile);
                return preview.NameEntry.PostScriptName + extension;
            }
        }

        protected static string GetXCodeFontRelativePath()
        {
            return "Data/Raw/res/font";
        }

        protected static string GetGradleFontRelativePath()
        {
            return "src/main/assets/res/font";
        }

        protected static string PathCombine(string path1, string path2)
        {
            var mergePath = Path.Combine(path1, path2).Replace("\\", "/");
            return mergePath;
        }

        #endregion
    }
}