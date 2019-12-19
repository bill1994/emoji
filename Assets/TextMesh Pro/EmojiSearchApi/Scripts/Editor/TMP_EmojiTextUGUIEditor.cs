using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace TMPro.EditorUtilities
{
    [CustomEditor(typeof(TMP_EmojiTextUGUI), true), CanEditMultipleObjects]
#if UNITY_2019_2_OR_NEWER
    public class TMP_EmojiTextUGUIEditor : TMP_EditorPanelUI
#else
    public class TMP_EmojiTextUGUIEditor : TMP_UiEditorPanel
#endif
    {
    }
}
