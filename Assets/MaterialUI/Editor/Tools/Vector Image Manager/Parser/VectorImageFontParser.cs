// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;

namespace MaterialUI
{
    public abstract class VectorImageFontParser
    {
        protected abstract string GetIconFontUrl();
        protected abstract string GetIconFontLicenseUrl();
        protected abstract string GetIconFontDataUrl();
        public abstract string GetFontName();
        public abstract string GetWebsite();
        protected abstract VectorImageSet GenerateIconSet(string fontDataContent);
        protected abstract string ExtractLicense(string fontLicenseDataContent);
        protected virtual void CleanUp() { }

        private VectorImageSet m_CachedVectorImageSet;
        private Action m_OnDoneDownloading;

        public void DownloadIcons(Action onDoneDownloading = null)
        {
            this.m_OnDoneDownloading = onDoneDownloading;
            EditorCoroutine.Start(DownloadIconFontCoroutine());
        }

        private IEnumerator DownloadIconFontCoroutine()
        {
            string iconFontURL = GetIconFontUrl();
            if (string.IsNullOrEmpty(iconFontURL))
            {
                yield break;
            }

            UnityWebRequest www = UnityWebRequest.Get(iconFontURL);
            www.SendWebRequest();

            while (!www.isDone)
            {
                yield return null;
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                ClearProgressBar();
				throw new Exception("Error downloading icon font (" + GetFontName() + ") at path: " + GetIconFontUrl() + " - " + www.error);
            }

            CreateFolderPath();

            File.WriteAllBytes(GetIconFontPath(), www.downloadHandler.data);
			EditorCoroutine.Start(DownloadFontLicenseCoroutine());
        }

        private IEnumerator DownloadFontLicenseCoroutine()
        {
            if (!string.IsNullOrEmpty(GetIconFontLicenseUrl()))
            {
                UnityWebRequest www = UnityWebRequest.Get(GetIconFontLicenseUrl());
                www.SendWebRequest();

                while (!www.isDone)
                {
                    yield return null;
                }

                if (!string.IsNullOrEmpty(www.error))
                {
                    ClearProgressBar();
                    throw new Exception("Error downloading icon font license (" + GetFontName() + ") at path: " + GetIconFontLicenseUrl());
                }

                CreateFolderPath();

                string licenseData = ExtractLicense(www.downloadHandler.text);

                File.WriteAllText(GetIconFontLicensePath(), licenseData);
            }

			EditorCoroutine.Start(DownloadIconFontData());
        }

        private IEnumerator DownloadIconFontData()
        {
            UnityWebRequest www = UnityWebRequest.Get(GetIconFontDataUrl());
            www.SendWebRequest();

            while (!www.isDone)
            {
                yield return null;
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                ClearProgressBar();
                throw new Exception("Error downloading icon font data (" + GetFontName() + ") at path: " + GetIconFontDataUrl() + "\n" + www.error);
            }

            CreateFolderPath();

            VectorImageSet vectorImageSet = GenerateIconSet(www.downloadHandler.text);
            FormatNames(vectorImageSet);

			string codePointJson = JsonUtility.ToJson(vectorImageSet);
            File.WriteAllText(GetIconFontDataPath(), codePointJson);

            if (m_OnDoneDownloading != null)
            {
                m_OnDoneDownloading();
            }

            CleanUp();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        private void CreateFolderPath()
        {
            if (!Directory.Exists(GetFolderPath()))
            {
                Directory.CreateDirectory(GetFolderPath());
            }
        }

        public string GetFolderPath()
        {
			string path = System.IO.Path.Combine(MaterialUIEditorTools.GetEssentialsFolderPath(), VectorImageManager.fontDestinationFolder, GetFontName());
            path = path.Replace("//", "/") + "/";

            return path;
        }

        private string GetIconFontPath()
        {
            return GetFolderPath() + GetFontName() + ".ttf";
        }

        public string GetIconFontLicensePath()
        {
            return GetFolderPath() + "LICENSE.txt";
        }

        private string GetIconFontDataPath()
        {
            return GetFolderPath() + GetFontName() + ".json";
        }

        public bool IsFontAvailable()
        {
            bool isFontAvailable = File.Exists(GetIconFontPath());
            bool isFontDataAvailable = File.Exists(GetIconFontDataPath());

            return isFontAvailable && isFontDataAvailable;
        }

        private void FormatNames(VectorImageSet set)
        {
            for (int i = 0; i < set.iconGlyphList.Count; i++)
            {
                string name = set.iconGlyphList[i].name;
                name = name.Replace("-", "_");
                name = name.Replace(" ", "_");
                name = name.ToLower();
				set.iconGlyphList[i].name = name;

				string unicode = set.iconGlyphList[i].unicode;
				unicode = unicode.Replace("\\u", "");
				unicode = unicode.Replace("\\\\u", "");
				set.iconGlyphList[i].unicode = unicode;
            }
        }

        public VectorImageSet GetIconSet()
        {
            if (!IsFontAvailable())
            {
                throw new Exception("Can't get the icon set because the font has not been downloaded");
            }

			VectorImageSet vectorImageSet = JsonUtility.FromJson<VectorImageSet>(File.ReadAllText(GetIconFontDataPath()));
            return vectorImageSet;
		}

		public VectorImageSet GetCachedIconSet()
		{
			if (m_CachedVectorImageSet == null)
			{
				m_CachedVectorImageSet = GetIconSet();
			}

			return m_CachedVectorImageSet;
		}

		public void Delete()
		{
			string path = GetFolderPath();

			// Delete folder
			Directory.Delete(path, true);

			// Sync AssetDatabase with the delete operation.
			string metaPath = path.Substring(Application.dataPath.IndexOf("/Assets") + 1);
			if (metaPath.EndsWith("/"))
			{
				metaPath = metaPath.Substring(0, metaPath.Length - 1);
			}

			AssetDatabase.DeleteAsset(metaPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
    }
}
