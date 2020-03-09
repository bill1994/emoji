using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using static MaterialUI.MaterialMultiDropdown;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(DialogCheckboxAddress), true)]
    public class DialogCheckboxAddressDrawer : GenericAssetAddressDrawer<DialogCheckboxList>
    {
    }

    [CustomEditor(typeof(MaterialMultiDropdown))]
    public class MaterialMultiDropdownEditor : BaseStyleElementEditor
    {
        private SerializedProperty m_SpinnerMode;
        private SerializedProperty m_DropdownExpandPivot;
        private SerializedProperty m_DropdownFramePivot;
        private SerializedProperty m_DropdownFramePreferredSize;
        private SerializedProperty m_CustomFramePrefabAddress;

        private SerializedProperty m_AlwaysDisplayHintOption;

        private SerializedProperty m_TextComponent;
        private SerializedProperty m_IconComponent;

        private SerializedProperty m_HintTextComponent;
        private SerializedProperty m_HintIconComponent;

        private SerializedProperty m_SelectedIndexes;

        private SerializedProperty OnCancelCallback;
        private SerializedProperty OnItemsSelected;

        SerializedProperty m_OptionDataList = null;
        SerializedProperty m_HintOption = null;
        SerializedProperty m_MixedOption = null;

        protected override void OnEnable()
        {
            OnBaseEnable();
            m_OptionDataList = serializedObject.FindProperty("m_OptionDataList");
            m_HintOption = serializedObject.FindProperty("m_HintOption");
            m_MixedOption = serializedObject.FindProperty("m_MixedOption");

            m_SpinnerMode = serializedObject.FindProperty("m_SpinnerMode");
            m_DropdownExpandPivot = serializedObject.FindProperty("m_DropdownExpandPivot");
            m_DropdownFramePivot = serializedObject.FindProperty("m_DropdownFramePivot");
            m_DropdownFramePreferredSize = serializedObject.FindProperty("m_DropdownFramePreferredSize");
            m_CustomFramePrefabAddress = serializedObject.FindProperty("m_CustomFramePrefabAddress");
            m_AlwaysDisplayHintOption = serializedObject.FindProperty("m_AlwaysDisplayHintOption");

            m_TextComponent = serializedObject.FindProperty("m_TextComponent");
            m_IconComponent = serializedObject.FindProperty("m_IconComponent");

            m_HintTextComponent = serializedObject.FindProperty("m_HintTextComponent");
            m_HintIconComponent = serializedObject.FindProperty("m_HintIconComponent");

            m_SelectedIndexes = serializedObject.FindProperty("m_SelectedIndexes");

            OnCancelCallback = serializedObject.FindProperty("OnCancelCallback");
            OnItemsSelected = serializedObject.FindProperty("OnItemsSelected");
        }

        protected override void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var optionsArray = m_OptionDataList.FindPropertyRelative("m_Options");
            var arraySize = optionsArray.arraySize;

            //Max Amount of EnumPopup value (display as int array)
            if (arraySize > 32)
            {
                EditorGUILayout.HelpBox("Max amount of elements to display as Popup is 32", MessageType.Info);
                EditorGUILayout.PropertyField(m_SelectedIndexes);
            }
            else
            {
                //Convert int array to mask
                int maskValue = 0;
                for (int i = 0; i < m_SelectedIndexes.arraySize; i++)
                {
                    maskValue |= (int)Mathf.Pow(2, m_SelectedIndexes.GetArrayElementAtIndex(i).intValue);
                }

                //Find the "Everything" value (when all options is selected)
                int everythingValue = 0;
                for (int i = 0; i < arraySize; i++)
                {
                    everythingValue |= (int)Mathf.Pow(2, i);
                }

                //Convert to unity everything (-1) if needed
                if (maskValue == everythingValue)
                    maskValue = -1;

                //Get options DisplayName
                List<string> displayedOptions = new List<string>();
                for (int i = 0; i < optionsArray.arraySize; i++)
                {
                    displayedOptions.Add(optionsArray.GetArrayElementAtIndex(i).FindPropertyRelative("m_Text").stringValue);
                }

                if (displayedOptions.Count > 0)
                {
                    var newMaskValue = EditorGUILayout.MaskField(m_SelectedIndexes.displayName, maskValue, displayedOptions.ToArray());

                    //Convert newMaskValue to unity everything (-1) if needed
                    if (newMaskValue == everythingValue)
                        newMaskValue = -1;

                    //Convert mask to int array
                    if (maskValue != newMaskValue)
                    {
                        List<int> indexArray = new List<int>();
                        for (int i = 0; i < Mathf.Max(optionsArray.arraySize, 32); i++)
                        {
                            var currentCheckingBitmask = (int)Mathf.Pow(2, i);
                            if ((newMaskValue & currentCheckingBitmask) == currentCheckingBitmask)
                            {
                                indexArray.Add(i);
                            }
                        }
                        //Recreate array with selected indexes
                        m_SelectedIndexes.ClearArray();
                        for (int i = 0; i < indexArray.Count; i++)
                        {
                            var selectedIndex = indexArray[i];
                            m_SelectedIndexes.InsertArrayElementAtIndex(i);
                            m_SelectedIndexes.GetArrayElementAtIndex(i).intValue = selectedIndex;
                        }
                    }
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_CustomFramePrefabAddress, new GUIContent("Custom Address"));
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_SpinnerMode);
            if (m_SpinnerMode.enumValueIndex != 1)
            {
                EditorGUI.indentLevel++;
                LayoutStyle_PropertyField(m_DropdownExpandPivot);
                LayoutStyle_PropertyField(m_DropdownFramePivot);
                LayoutStyle_PropertyField(m_DropdownFramePreferredSize);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            DrawFoldoutComponents(ComponentsSection);

            EditorGUILayout.Space();
            OptionsSection();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(OnItemsSelected);
            EditorGUILayout.PropertyField(OnCancelCallback);

            EditorGUILayout.Space();
            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }

        private void OptionsSection()
        {
            m_HintOption.FindPropertyRelative("m_ImageData.m_ImageDataType").enumValueIndex = m_OptionDataList.FindPropertyRelative("m_ImageType").enumValueIndex;
            EditorGUILayout.PropertyField(m_MixedOption);
            EditorGUILayout.PropertyField(m_HintOption);
            EditorGUILayout.PropertyField(m_AlwaysDisplayHintOption);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OptionDataList);
        }

        private void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_TextComponent);
            LayoutStyle_PropertyField(m_IconComponent);
            LayoutStyle_PropertyField(m_HintTextComponent);
            LayoutStyle_PropertyField(m_HintIconComponent);
            EditorGUI.indentLevel--;
        }
    }
}
