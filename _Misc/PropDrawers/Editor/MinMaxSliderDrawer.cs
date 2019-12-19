#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Kyub;

namespace KyubEditor
{
    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    class MinMaxSliderDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                position.width -= 5;
                float textFieldWidth = 50;
                
                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

                var v_indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                Vector2 range = property.vector2Value;
                float min = range.x;
                float max = range.y;
                MinMaxSliderAttribute attr = attribute as MinMaxSliderAttribute;

                EditorGUI.BeginChangeCheck();
                Rect minPos = new Rect(position);
                //minPos.x += v_labelWidth;
                minPos.width = textFieldWidth;
                min = EditorGUI.FloatField(minPos, min);

                var v_padding = 5;
                Rect sliderPos = new Rect(position);
                sliderPos.width -= (textFieldWidth*2) + (v_padding);
                sliderPos.x += v_padding + textFieldWidth;

                EditorGUI.MinMaxSlider(sliderPos, ref min, ref max, attr.min, attr.max);

                Rect maxPos = new Rect(sliderPos.xMax + v_padding, sliderPos.y, textFieldWidth, sliderPos.height);
                max = EditorGUI.FloatField(maxPos, max);

                if (min > max)
                    min = max;
                if (EditorGUI.EndChangeCheck())
                {
                    range.x = min;
                    range.y = max;
                    property.vector2Value = range;
                }
                EditorGUI.indentLevel = v_indentLevel;
            }
            else
            {
                EditorGUI.LabelField(position, label, "Use only with Vector2");
            }
        }
    }
}
#endif