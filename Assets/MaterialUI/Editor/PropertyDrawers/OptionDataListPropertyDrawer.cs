//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(OptionDataList), true)]
    class DropdownOptionListDrawer : PropertyDrawer
    {
        private ReorderableList m_ReorderableList;
        private SerializedProperty m_ImageType;
        private float m_ImageTypeHeight = 0;

        private void Init(SerializedProperty property)
        {
            if (m_ReorderableList != null)
            {
                return;
            }

            m_ImageType = property.FindPropertyRelative("m_ImageType");
            m_ImageTypeHeight = EditorGUI.GetPropertyHeight(m_ImageType);

            SerializedProperty array = property.FindPropertyRelative("m_Options");
            m_ReorderableList = new ReorderableList(property.serializedObject, array);
            m_ReorderableList.drawElementCallback = DrawOptionData;
            m_ReorderableList.drawHeaderCallback = DrawHeader;
            m_ReorderableList.elementHeightCallback += ElementHeightCallback;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, m_ImageTypeHeight), m_ImageType);

            position.y += m_ImageTypeHeight;

            m_ReorderableList.DoList(position);
        }

        private void DrawHeader(Rect rect)
        {
            GUI.Label(rect, "Options");
        }

        private float ElementHeightCallback(int index)
        {
            SerializedProperty itemData = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty itemImageType = itemData.FindPropertyRelative("m_ImageData.m_ImageDataType");
            SerializedProperty onSelectEvent = itemData.FindPropertyRelative("onOptionSelected");

            var isExpanded = itemData.isExpanded;
            var size = isExpanded ? (m_ImageTypeHeight + EditorGUI.GetPropertyHeight(onSelectEvent) + 6) : m_ImageTypeHeight;

            if (isExpanded)
            {
                size += (EditorGUIUtility.singleLineHeight * 2) + 7;
            }

            if (m_ImageType.enumValueIndex == 0)
            {
                return size + (itemData.isExpanded ? m_ImageTypeHeight + 7 : 0);
            }
            else
            {
                return size;
            }
        }

        private void DrawOptionData(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty itemData = m_ReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            SerializedProperty itemText = itemData.FindPropertyRelative("m_Text");
            SerializedProperty onSelectEvent = itemData.FindPropertyRelative("onOptionSelected");
            SerializedProperty hiddenProperties = itemData.FindPropertyRelative("m_HiddenFlags");

            SerializedProperty itemSprite = itemData.FindPropertyRelative("m_ImageData.m_Sprite");
            SerializedProperty imgUrl = itemData.FindPropertyRelative("m_ImageData.m_ImgUrl");

            SerializedProperty itemCode = itemData.FindPropertyRelative("m_ImageData.m_VectorImageData.m_Glyph.m_Unicode");
            SerializedProperty itemName = itemData.FindPropertyRelative("m_ImageData.m_VectorImageData.m_Glyph.m_Name");
            GUIStyle iconStyle = null;
            if (iconStyle == null)
            {
                object v_asset = itemData.FindPropertyRelative("m_ImageData.m_VectorImageData.m_Font").objectReferenceValue;
                VectorImageFont vectorFont = v_asset as VectorImageFont;
                iconStyle = new GUIStyle { font = vectorFont != null ? vectorFont.font : v_asset as Font };
                if (iconStyle.font == null && vectorFont != null && vectorFont.fontTMPro != null)
                    iconStyle.font = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(vectorFont.fontTMPro.creationSettings.sourceFontFileGUID));
            }

            SerializedProperty itemImageType = itemData.FindPropertyRelative("m_ImageData.m_ImageDataType");

            RectOffset offset = new RectOffset(-5, 0, -1, -3);
            rect = offset.Add(rect);
            rect.height = EditorGUIUtility.singleLineHeight;

            float offsetH = 2;

            EditorGUI.LabelField(new Rect(rect.x + offsetH, rect.y, 16, rect.height), index.ToString());
            offsetH += 16;

			EditorGUI.LabelField(new Rect(rect.x + offsetH, rect.y, 35, rect.height), "Text", EditorStyles.boldLabel);
            offsetH += 35;

            EditorGUI.PropertyField(new Rect(rect.x + offsetH, rect.y, (rect.width / 3) - offsetH, rect.height), itemText, GUIContent.none);
            offsetH += (rect.width / 3) - offsetH + 8;

			EditorGUI.LabelField(new Rect(rect.x + offsetH, rect.y, 32, rect.height), "Icon", EditorStyles.boldLabel);
            offsetH += 32;

            itemImageType.enumValueIndex = m_ImageType.enumValueIndex;

            
            if (m_ImageType.enumValueIndex == 0)
            {
                var itemSpriteRect = new Rect(rect.x + offsetH, rect.y, rect.width - offsetH, rect.height);
                EditorGUI.PropertyField(itemSpriteRect, itemSprite, GUIContent.none);
            }
            else
            {
                if (!string.IsNullOrEmpty(itemName.stringValue))
                {
                    var iconText = IconDecoder.Decode(itemCode.stringValue);
                    //EditorGUI.LabelField(new Rect(rect.x + offsetH, rect.y, 16, rect.height), IconDecoder.Decode(itemCode.stringValue), iconStyle);
                    if (iconStyle.font != null && !iconStyle.font.dynamic)
                    {
                        CharacterInfo charInfo;
                        iconStyle.font.GetCharacterInfo(iconText[0], out charInfo);
                        var uvRect = Rect.MinMaxRect(charInfo.uvBottomLeft.x, charInfo.uvBottomLeft.y, charInfo.uvTopRight.x, charInfo.uvTopRight.y);

                        GUI.DrawTextureWithTexCoords(new Rect(rect.x + offsetH, rect.y, 16, rect.height), iconStyle.font.material.mainTexture, uvRect);
                    }
                    else
                    {
                        iconStyle.fontSize = (int)rect.height;
                        EditorGUI.LabelField(new Rect(rect.x + offsetH, rect.y, 16, rect.height), iconText, iconStyle);
                    }

                    offsetH += 16;
                    EditorGUI.LabelField(new Rect(rect.x + offsetH, rect.y, rect.width - offsetH - 80, rect.height), itemName.stringValue);
                }
                else
                {
                    EditorGUI.LabelField(new Rect(rect.x + offsetH, rect.y, rect.width - offsetH - 80, rect.height), "No icon selected");
                }

                if (GUI.Button(new Rect(rect.width - 60, rect.y, 70, rect.height), "Pick Icon"))
                {

                    IOptionDataListContainer optionDataListContainer = itemData.serializedObject.targetObject as IOptionDataListContainer;
                    VectorImagePickerWindow.Show(optionDataListContainer.optionDataList.options[index].imageData.vectorImageData, itemData.serializedObject.targetObject);

                }

                var fontStyle = new GUIStyle { font = VectorImageManager.GetIconFont(VectorImageManager.materialDesignIconsFontName) };
                if (fontStyle.font != null && fontStyle.font.dynamic)
                    fontStyle.fontSize = (int)rect.height;
                if (GUI.Button(new Rect(rect.width + 16, rect.y, 18, rect.height), IconDecoder.Decode(@"\ue14c"), fontStyle))
                {
                    IOptionDataListContainer optionDataListContainer = itemData.serializedObject.targetObject as IOptionDataListContainer;
                    if (optionDataListContainer != null && optionDataListContainer.optionDataList.options[index].imageData != null)
                    {
                        VectorImageData data = optionDataListContainer.optionDataList.options[index].imageData.vectorImageData;
                        if (data != null)
                        {
                            data.vectorFont = null;
                            data.glyph = null;
                        }
                    }
                    EditorUtility.SetDirty(itemData.serializedObject.targetObject);
                }
            }

            EditorGUI.PropertyField(rect, itemData, new GUIContent(string.Empty), false);
            //itemData.isExpanded = EditorGUI.Foldout(rect, itemData.isExpanded, string.Empty);
            if (itemData.isExpanded)
            {
                var drawUrl = m_ImageType.enumValueIndex == 0;
                rect.y += EditorGUIUtility.singleLineHeight + 3;
                if (drawUrl)
                {
                    var itemSpriteRect = new Rect(rect.x + offsetH, rect.y, rect.width - offsetH, rect.height);

                    EditorGUI.LabelField(new Rect(itemSpriteRect.x - 32, rect.y, 32, rect.height), "Url", EditorStyles.boldLabel);
                    EditorGUI.PropertyField(itemSpriteRect, imgUrl, GUIContent.none);

                    rect.y += EditorGUIUtility.singleLineHeight + 7;
                }

                var cachedValue = EditorGUI.showMixedValue;
                var hiddenPropertiesValue = (OptionData.OptionsHiddenFlagEnum)hiddenProperties.intValue;
                var newHiddenPropertiesValue = hiddenPropertiesValue;
                EditorGUI.BeginChangeCheck();
                {
                    EditorGUI.showMixedValue = hiddenProperties.hasMultipleDifferentValues;

                    var cachedVisible = !hiddenPropertiesValue.HasFlag(OptionData.OptionsHiddenFlagEnum.Hidden);
                    var visible = EditorGUI.Toggle(rect, "Visible", cachedVisible);
                    if (cachedVisible != visible)
                    {
                        if (!visible)
                        {
                            newHiddenPropertiesValue |= OptionData.OptionsHiddenFlagEnum.Hidden;
                        }
                        else
                        {
                            newHiddenPropertiesValue &= ~OptionData.OptionsHiddenFlagEnum.Hidden;
                        }
                    }

                    rect.y += EditorGUIUtility.singleLineHeight;
                    var cachedInteractable = !hiddenPropertiesValue.HasFlag(OptionData.OptionsHiddenFlagEnum.Disabled);
                    var interactable = EditorGUI.Toggle(rect, "Interactable", cachedInteractable);
                    if (cachedInteractable != interactable)
                    {
                        if (!interactable)
                        {
                            newHiddenPropertiesValue |= OptionData.OptionsHiddenFlagEnum.Disabled;
                        }
                        else
                        {
                            newHiddenPropertiesValue &= ~OptionData.OptionsHiddenFlagEnum.Disabled;
                        }
                    }
                    rect.y += EditorGUIUtility.singleLineHeight + 7;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (hiddenPropertiesValue != newHiddenPropertiesValue)
                    {
                        hiddenProperties.intValue = (int)newHiddenPropertiesValue;
                    }
                }
                EditorGUI.showMixedValue = cachedValue;

                EditorGUI.PropertyField(rect, onSelectEvent);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);

            return m_ReorderableList.GetHeight() + m_ImageTypeHeight;
        }
    }
}
