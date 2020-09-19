// This File will try to copy all fonts used in project to StreamingAssets/res/font before build (and remove after build).
// Doing this we can access TTF files in Native Platforms to draw using NativeInputField.

#if UNITY_EDITOR && UNITY_ANDROID
#define SUPPORT_PROCESSOR
#endif

#if SUPPORT_PROCESSOR
using KyubEditor.Typography.OpenFont;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Kyub.Internal.NativeInputPlugin;

namespace KyubEditor.Internal.NativeInputPlugin
{
    class NativeFontsProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        static HashSet<string> s_lastCopyFiles = null;

        public int callbackOrder { get { return 0; } }
        public void OnPreprocessBuild(BuildReport report)
        {
            var fontFiles = GetAllUsedFont();
            s_lastCopyFiles = CopyToPath(GetFontDirectoryPath(), fontFiles);
            AssetDatabase.Refresh();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (DeleteCopies(s_lastCopyFiles))
            {
                AssetDatabase.Refresh();
                var foldersToDelete = new string[] { GetFontDirectoryPath(), GetPostStriptTableDirectoryPath() };
                foreach (var folder in foldersToDelete)
                {
                    try
                    {
                        var parentFolder = new DirectoryInfo(folder).Parent;
                        DeleteFolderAndContents(folder);
                        if (parentFolder != null)
                            ClearAssetFolderRecursiveUp(parentFolder.FullName);
                    }
                    catch { }
                }
                s_lastCopyFiles = null;
            }
        }

        #region Helper Static Functions

        protected virtual void DeleteFolderAndContents(string folderFullPath)
        {
            try
            {
                var applicationDataPath = Application.dataPath;
                var folderInfoToTrack = new DirectoryInfo(folderFullPath);
                var parent = folderInfoToTrack.Parent;
                var directoryAssetPath = folderInfoToTrack.FullName.Replace("\\", "/").Replace(applicationDataPath, "Assets");
                //Try Delete Asset using Unity API
                if (!UnityEditor.AssetDatabase.DeleteAsset(directoryAssetPath))
                    folderInfoToTrack.Delete(true);
            }
            catch { }
        }

        protected virtual void ClearAssetFolderRecursiveUp(string folderFullPath)
        {
            var applicationDataPath = Application.dataPath;
            var assetInfo = new DirectoryInfo(applicationDataPath);
            var folderInfoToTrack = new DirectoryInfo(folderFullPath);
            //We can only delete folders until Assets
            if (folderInfoToTrack == null || assetInfo.FullName == folderInfoToTrack.FullName || !folderInfoToTrack.FullName.Contains(assetInfo.FullName))
                return;

            while (folderInfoToTrack != null && folderInfoToTrack.GetFiles().Length == 0 && folderInfoToTrack.GetDirectories().Length == 0)
            {
                try
                {
                    var parent = folderInfoToTrack.Parent;
                    var directoryAssetPath = folderInfoToTrack.FullName.Replace("\\", "/").Replace(applicationDataPath, "Assets");
                    //Try Delete Asset using Unity API
                    if (!UnityEditor.AssetDatabase.DeleteAsset(directoryAssetPath))
                        folderInfoToTrack.Delete();
                    folderInfoToTrack = parent;
                    //We can only delete folders until Assets
                    if (folderInfoToTrack == null || assetInfo.FullName == folderInfoToTrack.FullName || !folderInfoToTrack.FullName.Contains(assetInfo.FullName))
                        return;
                }
                catch
                {
                    return;
                }
            }
        }

        protected static bool DeleteCopies(HashSet<string> copyFiles)
        {
            var sucess = false;
            var applicationDataPath = Application.dataPath;
            if (copyFiles != null)
            {
                foreach (var copyFile in copyFiles)
                {
                    try
                    {
                        var copyAssetPath = copyFile.Replace("\\", "/").Replace(applicationDataPath, "Assets");
                        if (!UnityEditor.AssetDatabase.DeleteAsset(copyAssetPath))
                        {
                            File.Delete(copyFile);
                            sucess = true;
                        }
                        else
                            sucess = true;
                    }
                    catch { }
                }
            }
            return sucess;
        }

        protected static HashSet<string> CopyToPath(string rootPath, HashSet<Font> fonts)
        {

            PostScriptTableData table = new PostScriptTableData();
            HashSet<string> copyFiles = new HashSet<string>();
            if (fonts != null)
            {
                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);
                foreach (var font in fonts)
                {
                    var filePath = AssetDatabase.GetAssetPath(font);
                    var postScriptName = GetPostScriptName(filePath);
                    table.AddEntry(font, postScriptName);
                    var copyPath = Path.Combine(rootPath, postScriptName).Replace("\\", "/");
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
            }

            //Save PostScript Table
            var postScriptResourcesPath = GetPostStriptTableDirectoryPath();
            if (!Directory.Exists(postScriptResourcesPath))
                Directory.CreateDirectory(postScriptResourcesPath);
            var postScriptTablePath = postScriptResourcesPath + "/" + PostScriptNameUtils.POST_SCRIPT_TABLE;
            copyFiles.Add(postScriptTablePath);
            File.WriteAllText(postScriptTablePath, JsonUtility.ToJson(table));

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

        protected static string GetPostScriptName(string fontFile)
        {
            using (var filestream = new FileStream(fontFile, FileMode.Open))
            {
                var fontReader = new OpenFontReader();
                var preview = fontReader.ReadPreview(filestream);
                return preview.NameEntry.PostScriptName;
            }
        }

        protected static string GetFontDirectoryPath()
        {
            return Application.streamingAssetsPath + "/res/font";
        }

        public static string GetPostStriptTableDirectoryPath()
        {
            var tableFile = Application.dataPath + "/Resources/res/font";
            return tableFile;
        }

        #endregion
    }
}

#endif