#if UNITY_EDITOR

using System;
using UnityEditor;

#if UNITY_EDITOR_WIN
using System.Linq;
using System.Runtime.InteropServices;
#endif

namespace SFB {
    public class StandaloneFileBrowserEditor : IStandaloneFileBrowser  {

#if UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();
#endif

        public string[] OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect) {
#if UNITY_EDITOR_WIN
            if (multiselect)
            {
                var shellFilters = GetShellFilterFromFileExtensionList(extensions);
                var paths = ShellFileDialogs.FileOpenDialog.ShowMultiSelectDialog(GetActiveWindow(), title, directory, string.Empty, shellFilters, null);

                return paths == null || paths.Count == 0 ? new string[0] : paths.ToArray();
            }
#endif
            string path = "";
            if (extensions == null) {
                path = EditorUtility.OpenFilePanel(title, directory, "");
            }
            else {
                path = EditorUtility.OpenFilePanelWithFilters(title, directory, GetFilterFromFileExtensionList(extensions));
            }

            return string.IsNullOrEmpty(path) ? new string[0] : new[] { path };
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb) {
            cb.Invoke(OpenFilePanel(title, directory, extensions, multiselect));
        }

        public string[] OpenFolderPanel(string title, string directory, bool multiselect) {
            var path = EditorUtility.OpenFolderPanel(title, directory, "");
            return string.IsNullOrEmpty(path) ? new string[0] : new[] {path};
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<string[]> cb) {
            cb.Invoke(OpenFolderPanel(title, directory, multiselect));
        }

        public string SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions) {
            var ext = extensions != null ? extensions[0].Extensions[0] : "";
            var name = string.IsNullOrEmpty(ext) ? defaultName : defaultName + "." + ext;
            return EditorUtility.SaveFilePanel(title, directory, name, ext);
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb) {
            cb.Invoke(SaveFilePanel(title, directory, defaultName, extensions));
        }

        // EditorUtility.OpenFilePanelWithFilters extension filter format
        private static string[] GetFilterFromFileExtensionList(ExtensionFilter[] extensions) {
            var filters = new string[extensions.Length * 2];
            for (int i = 0; i < extensions.Length; i++) {
                filters[(i * 2)] = extensions[i].Name;
                filters[(i * 2) + 1] = string.Join(",", extensions[i].Extensions);
            }
            return filters;
        }

#if UNITY_EDITOR_WIN
        private static ShellFileDialogs.Filter[] GetShellFilterFromFileExtensionList(ExtensionFilter[] extensions)
        {
            var shellFilters = new System.Collections.Generic.List<ShellFileDialogs.Filter>();
            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    if (extension.Extensions == null || extension.Extensions.Length == 0)
                        continue;

                    var displayName = extension.Name;
                    if (string.IsNullOrEmpty(displayName))
                    {
                        System.Text.StringBuilder extensionFormatted = new System.Text.StringBuilder();
                        foreach (var extensionStr in extension.Extensions)
                        {
                            if (extensionFormatted.Length > 0)
                                extensionFormatted.Append(";");
                            extensionFormatted.Append($"*.{extensionStr}");
                        }
                        displayName = $"({extensionFormatted})";
                    }
                    var filter = new ShellFileDialogs.Filter(displayName, extension.Extensions);
                    shellFilters.Add(filter);
                }
            }
            if (shellFilters.Count == 0)
            {
                shellFilters.AddRange(ShellFileDialogs.Filter.ParseWindowsFormsFilter(@"All files (*.*)|*.*"));
            }

            return shellFilters.ToArray();
        }
#endif

    }
}

#endif
