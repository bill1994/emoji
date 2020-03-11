using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kyub.Performance
{
    public class IntPopupAttribute : PropertyAttribute
    {
        public int[] OptionValues { get; protected set; }
        public GUIContent[] DisplayOptions { get; protected set; }

        public IntPopupAttribute(params object[] args)
        {
            List<int> values = new List<int>();
            List<GUIContent> names = new List<GUIContent>();
            foreach (var arg in args)
            {
                if (IsNumber(arg))
                    values.Add((int)arg);
                else if (arg is string)
                    names.Add(new GUIContent(arg as string));
                else if(arg != null)
                    names.Add(new GUIContent(arg.ToString()));
            }
            while (names.Count != values.Count)
            {
                if (names.Count > values.Count)
                    names.RemoveAt(names.Count - 1);
                else
                {
                    names.Add(new GUIContent(values[names.Count].ToString()));
                }
            }
            OptionValues = values.ToArray();
            DisplayOptions = names.ToArray();
        }

        static bool IsNumber(object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(IntPopupAttribute))]
    public class IntPopupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var intPopupAttr = this.attribute as IntPopupAttribute;
            if (intPopupAttr == null || intPopupAttr.OptionValues == null || intPopupAttr.OptionValues.Length == 0)
                base.OnGUI(position, property, label);
            else
            {
                var index = System.Array.IndexOf(intPopupAttr.OptionValues, property.intValue);
                EditorGUI.IntPopup(position, property, intPopupAttr.DisplayOptions, intPopupAttr.OptionValues);
            }
        }
    }
#endif
}
