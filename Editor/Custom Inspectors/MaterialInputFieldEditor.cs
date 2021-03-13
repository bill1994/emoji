//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;
using UnityEngine;
using static MaterialUI.MaterialInputField;

namespace MaterialUI
{
    [CustomPropertyDrawer(typeof(DialogPromptAddress), true)]
    public class DialogPromptAddressDrawer : GenericAssetAddressDrawer<DialogPrompt>
    {
    }

    [CustomEditor(typeof(MaterialInputField), true)]
    [CanEditMultipleObjects]
    public class MaterialInputFieldEditor : BaseStyleElementEditor
    {
        private SerializedProperty m_InputPromptDisplayOption;
        private SerializedProperty m_CustomPromptDialogAddress;
        private SerializedProperty m_OpenPromptOnSelect;
        
        //  Fields
        private SerializedProperty m_Interactable;

        private SerializedProperty m_AnimationDuration;

        private SerializedProperty m_BackgroundLayoutMode;
        private SerializedProperty m_LineLayoutMode;

        //private SerializedProperty m_HintText;
        private SerializedProperty m_FloatingHint;
        private SerializedProperty m_FloatingHintFontSize;

        private SerializedProperty m_HasCharacterCounter;
        private SerializedProperty m_MatchInputFieldCharacterLimit;
        private SerializedProperty m_CharacterLimit;

        private SerializedProperty m_HasValidation;
        private SerializedProperty m_TextValidator;
        private SerializedProperty m_ValidateOnStart;

        private SerializedProperty m_FitWidthToContent;
        private SerializedProperty m_ManualPreferredWidth;
        private SerializedProperty m_FitHeightToContent;
        private SerializedProperty m_ManualPreferredHeight;
        private SerializedProperty m_ManualSizeX;
        private SerializedProperty m_ManualSizeY;
        private SerializedProperty m_ManualSize;

        private SerializedProperty m_LeftContentOffset;
        private SerializedProperty m_RightContentOffset;

        private SerializedProperty m_InputText;
        private SerializedProperty m_HintTextObject;
        private SerializedProperty m_CounterText;

        private SerializedProperty m_InputTextTransform;
        private SerializedProperty m_HintTextTransform;
        private SerializedProperty m_CounterTextTransform;
        private SerializedProperty m_ValidationText;
        private SerializedProperty m_ValidationTextTransform;
        private SerializedProperty m_BackgroundGraphic;
        private SerializedProperty m_OutlineGraphic;
        private SerializedProperty m_LineTransform;
        private SerializedProperty m_ActiveLineTransform;
        private SerializedProperty m_LeftContentTransform;
        private SerializedProperty m_RightContentTransform;
        private SerializedProperty m_TopContentTransform;
        private SerializedProperty m_BottomContentTransform;
        private SerializedProperty m_ClearButton;

        private SerializedProperty m_LeftContentActiveColor;
        private SerializedProperty m_LeftContentInactiveColor;
        private SerializedProperty m_RightContentActiveColor;
        private SerializedProperty m_RightContentInactiveColor;
        private SerializedProperty m_HintTextActiveColor;
        private SerializedProperty m_HintTextInactiveColor;
        private SerializedProperty m_LineActiveColor;
        private SerializedProperty m_LineInactiveColor;
        private SerializedProperty m_BackgroundActiveColor;
        private SerializedProperty m_BackgroundInactiveColor;
        private SerializedProperty m_OutlineActiveColor;
        private SerializedProperty m_OutlineInactiveColor;
        private SerializedProperty m_ValidationActiveColor;
        private SerializedProperty m_ValidationInactiveColor;
        private SerializedProperty m_CounterActiveColor;
        private SerializedProperty m_CounterInactiveColor;

        private SerializedProperty m_ClearButtonDisplayMode;
        private SerializedProperty m_Padding;
        private SerializedProperty m_LeftContentGraphic;
        private SerializedProperty m_RightContentGraphic;

        private SerializedProperty onValueChanged;
        private SerializedProperty onEndEdit;
        private SerializedProperty onReturnPressed;

        private SerializedProperty onActivate;
        private SerializedProperty onDeactivate;

        protected override void OnEnable()
        {
            OnBaseEnable();

            m_InputPromptDisplayOption = serializedObject.FindProperty("m_InputPromptDisplayOption");
            m_CustomPromptDialogAddress = serializedObject.FindProperty("m_CustomPromptDialogAddress");
            m_OpenPromptOnSelect = serializedObject.FindProperty("m_OpenPromptOnSelect");

            //  Fields
            m_Interactable = serializedObject.FindProperty("m_Interactable");

            m_AnimationDuration = serializedObject.FindProperty("m_AnimationDuration");
            m_BackgroundLayoutMode = serializedObject.FindProperty("m_BackgroundLayoutMode");
            m_LineLayoutMode = serializedObject.FindProperty("m_LineLayoutMode");

            //m_HintText = serializedObject.FindProperty("m_HintText");
            m_FloatingHint = serializedObject.FindProperty("m_FloatingHint");
            m_FloatingHintFontSize = serializedObject.FindProperty("m_FloatingHintFontSize");

            m_HasCharacterCounter = serializedObject.FindProperty("m_HasCharacterCounter");
            m_MatchInputFieldCharacterLimit = serializedObject.FindProperty("m_MatchInputFieldCharacterLimit");
            m_CharacterLimit = serializedObject.FindProperty("m_CharacterLimit");

            m_InputText = serializedObject.FindProperty("m_InputText");
            m_HintTextObject = serializedObject.FindProperty("m_HintTextObject");
            m_CounterText = serializedObject.FindProperty("m_CounterText");

            m_HasValidation = serializedObject.FindProperty("m_HasValidation");
            m_TextValidator = serializedObject.FindProperty("m_TextValidator");
            m_ValidateOnStart = serializedObject.FindProperty("m_ValidateOnStart");

            m_FitWidthToContent = serializedObject.FindProperty("m_FitWidthToContent");
            m_ManualPreferredWidth = serializedObject.FindProperty("m_ManualPreferredWidth");
            m_FitHeightToContent = serializedObject.FindProperty("m_FitHeightToContent");
            m_ManualPreferredHeight = serializedObject.FindProperty("m_ManualPreferredHeight");
            m_ManualSizeX = serializedObject.FindProperty("m_ManualSize.x");
            m_ManualSizeY = serializedObject.FindProperty("m_ManualSize.y");
            m_ManualSize = serializedObject.FindProperty("m_ManualSize");

            m_LeftContentOffset = serializedObject.FindProperty("m_LeftContentOffset");
            m_RightContentOffset = serializedObject.FindProperty("m_RightContentOffset");

            m_InputTextTransform = serializedObject.FindProperty("m_InputTextTransform");
            m_HintTextTransform = serializedObject.FindProperty("m_HintTextTransform");
            m_CounterTextTransform = serializedObject.FindProperty("m_CounterTextTransform");
            m_ValidationText = serializedObject.FindProperty("m_ValidationText");
            m_ValidationTextTransform = serializedObject.FindProperty("m_ValidationTextTransform");
            m_BackgroundGraphic = serializedObject.FindProperty("m_BackgroundGraphic");
            m_OutlineGraphic = serializedObject.FindProperty("m_OutlineGraphic");
            m_LineTransform = serializedObject.FindProperty("m_LineTransform");
            m_ActiveLineTransform = serializedObject.FindProperty("m_ActiveLineTransform");
            m_LeftContentTransform = serializedObject.FindProperty("m_LeftContentTransform");
            m_RightContentTransform = serializedObject.FindProperty("m_RightContentTransform");
            m_TopContentTransform = serializedObject.FindProperty("m_TopContentTransform");
            m_BottomContentTransform = serializedObject.FindProperty("m_BottomContentTransform");
            m_ClearButton = serializedObject.FindProperty("m_ClearButton");

            m_LeftContentActiveColor = serializedObject.FindProperty("m_LeftContentActiveColor");
            m_LeftContentInactiveColor = serializedObject.FindProperty("m_LeftContentInactiveColor");
            m_RightContentActiveColor = serializedObject.FindProperty("m_RightContentActiveColor");
            m_RightContentInactiveColor = serializedObject.FindProperty("m_RightContentInactiveColor");
            m_HintTextActiveColor = serializedObject.FindProperty("m_HintTextActiveColor");
            m_HintTextInactiveColor = serializedObject.FindProperty("m_HintTextInactiveColor");
            m_LineActiveColor = serializedObject.FindProperty("m_LineActiveColor");
            m_LineInactiveColor = serializedObject.FindProperty("m_LineInactiveColor");
            m_BackgroundActiveColor = serializedObject.FindProperty("m_BackgroundActiveColor");
            m_BackgroundInactiveColor = serializedObject.FindProperty("m_BackgroundInactiveColor");
            m_OutlineActiveColor = serializedObject.FindProperty("m_OutlineActiveColor");
            m_OutlineInactiveColor = serializedObject.FindProperty("m_OutlineInactiveColor");
            m_ValidationActiveColor = serializedObject.FindProperty("m_ValidationActiveColor");
            m_ValidationInactiveColor = serializedObject.FindProperty("m_ValidationInactiveColor");
            m_CounterActiveColor = serializedObject.FindProperty("m_CounterActiveColor");
            m_CounterInactiveColor = serializedObject.FindProperty("m_CounterInactiveColor");

            m_LeftContentGraphic = serializedObject.FindProperty("m_LeftContentGraphic");
            m_RightContentGraphic = serializedObject.FindProperty("m_RightContentGraphic");

            m_ClearButtonDisplayMode = serializedObject.FindProperty("m_ClearButtonDisplayMode");
            m_Padding = serializedObject.FindProperty("m_Padding");

            onValueChanged = serializedObject.FindProperty("onValueChanged");
            onEndEdit = serializedObject.FindProperty("onEndEdit");
            onReturnPressed = serializedObject.FindProperty("onReturnPressed");

            onActivate = serializedObject.FindProperty("onActivate");
            onDeactivate = serializedObject.FindProperty("onDeactivate");

            foreach (var target in targets)
            {
                if(target is MaterialInputField)
                    (target as MaterialInputField).SetLayoutDirty();
            }
        }

        protected override void OnDisable()
        {
            OnBaseDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            {
                LayoutStyle_PropertyField(m_Interactable);
            }
            if (EditorGUI.EndChangeCheck())
            {
                ((MaterialInputField)m_Interactable.serializedObject.targetObject).interactable = m_Interactable.boolValue;
            }

            if (m_HintTextTransform.objectReferenceValue != null)
            {
                string hintText = null;
                string newHintText = null;
                EditorGUI.BeginChangeCheck();
                {
                    var showMixed = false;
                    foreach (var target in targets)
                    {
                        var input = target as MaterialInputField;
                        if (input != null)
                        {
                            if (!showMixed && hintText != null && hintText != input.hintText)
                            {
                                showMixed = true;
                                break;
                            }
                            hintText = input.hintText;
                        }
                    }
                    if (hintText == null)
                        hintText = "";
                    var oldShowMixed = EditorGUI.showMixedValue;
                    EditorGUI.showMixedValue = showMixed;
                    newHintText = EditorGUILayout.TextField("Hint Text", hintText);
                    EditorGUI.showMixedValue = oldShowMixed;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    if (newHintText != hintText)
                    {
                        foreach (var target in targets)
                        {
                            var input = target as MaterialInputField;
                            if (input != null)
                            {
                                input.hintText = newHintText;
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_AnimationDuration);
            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_ClearButtonDisplayMode);
            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_BackgroundLayoutMode);
            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_LineLayoutMode);
            EditorGUILayout.Space();

            using (new GUILayout.HorizontalScope())
            {
                LayoutStyle_PropertyField(m_FloatingHint);
                if (m_FloatingHint.boolValue)
                {
                    EditorGUILayout.LabelField("Font Size", GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent("Font Size")).x));
                    LayoutStyle_PropertyField(m_FloatingHintFontSize, new GUIContent(""));
                }
            }

            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_HasCharacterCounter);
            if (m_HasCharacterCounter.boolValue)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.HorizontalScope())
                {
                    LayoutStyle_PropertyField(m_MatchInputFieldCharacterLimit);
                    if (!m_MatchInputFieldCharacterLimit.boolValue)
                    {
                        EditorGUI.indentLevel--;
                        EditorGUILayout.LabelField("Limit", GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent("Limit")).x + 10));
                        EditorGUI.indentLevel++;
                        LayoutStyle_PropertyField(m_CharacterLimit, new GUIContent(""));
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_HasValidation);
            if (m_HasValidation.boolValue)
            {
                EditorGUI.indentLevel++;
                LayoutStyle_PropertyField(m_TextValidator);
                LayoutStyle_PropertyField(m_ValidateOnStart);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_FitWidthToContent);
            if (!m_FitWidthToContent.boolValue)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.HorizontalScope())
                {
                    LayoutStyle_PropertyField(m_ManualPreferredWidth);
                    if (m_ManualPreferredWidth.boolValue)
                    {
                        LayoutStyle_PropertyField(m_ManualSizeX, m_ManualSize, new GUIContent(""), false);
                    }
                }
                EditorGUI.indentLevel--;
            }

            LayoutStyle_PropertyField(m_FitHeightToContent);
            if (!m_FitHeightToContent.boolValue)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.HorizontalScope())
                {
                    LayoutStyle_PropertyField(m_ManualPreferredHeight);
                    if (m_ManualPreferredHeight.boolValue)
                    {
                        LayoutStyle_PropertyField(m_ManualSizeY, m_ManualSize, new GUIContent(""), false);
                    }
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            GUI.enabled = m_LeftContentTransform.objectReferenceValue != null;
            LayoutStyle_PropertyField(m_LeftContentOffset);
            GUI.enabled = true;

            GUI.enabled = m_RightContentTransform.objectReferenceValue != null;
            LayoutStyle_PropertyField(m_RightContentOffset);
            GUI.enabled = true;

            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_Padding, true);
            EditorGUILayout.Space();

            DrawFoldoutCustom1("Prompt Behaviour", PromptSection);
            EditorGUILayout.Space();

            DrawFoldoutColors(ColorsSection);
            DrawFoldoutComponents(ComponentsSection);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(onActivate);
            EditorGUILayout.PropertyField(onDeactivate);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(onValueChanged);
            EditorGUILayout.PropertyField(onEndEdit);
            EditorGUILayout.PropertyField(onReturnPressed);

            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }

        private bool PromptSection()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("This property controls when MaterialInputField should diplay an DialogPrompt onClick/onActivateInputField/onSelect", MessageType.Info);
            LayoutStyle_PropertyField(m_InputPromptDisplayOption);
            EditorGUILayout.PropertyField(m_CustomPromptDialogAddress);
            LayoutStyle_PropertyField(m_OpenPromptOnSelect);
            EditorGUI.indentLevel--;

            return true;
        }

        private void ColorsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_LeftContentActiveColor);
            LayoutStyle_PropertyField(m_LeftContentInactiveColor);
            LayoutStyle_PropertyField(m_RightContentActiveColor);
            LayoutStyle_PropertyField(m_RightContentInactiveColor);
            LayoutStyle_PropertyField(m_HintTextActiveColor);
            LayoutStyle_PropertyField(m_HintTextInactiveColor);
            LayoutStyle_PropertyField(m_LineActiveColor);
            LayoutStyle_PropertyField(m_LineInactiveColor);
            LayoutStyle_PropertyField(m_BackgroundActiveColor);
            LayoutStyle_PropertyField(m_BackgroundInactiveColor);
            LayoutStyle_PropertyField(m_OutlineActiveColor);
            LayoutStyle_PropertyField(m_OutlineInactiveColor);
            LayoutStyle_PropertyField(m_ValidationActiveColor);
            LayoutStyle_PropertyField(m_ValidationInactiveColor);
            LayoutStyle_PropertyField(m_CounterActiveColor);
            LayoutStyle_PropertyField(m_CounterInactiveColor);
            EditorGUI.indentLevel--;
        }

        private void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_BackgroundGraphic);
            LayoutStyle_PropertyField(m_OutlineGraphic);
            LayoutStyle_PropertyField(m_LineTransform);
            LayoutStyle_PropertyField(m_ActiveLineTransform);
            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_InputTextTransform);
            LayoutStyle_PropertyField(m_InputText);
            LayoutStyle_PropertyField(m_HintTextTransform);
            LayoutStyle_PropertyField(m_HintTextObject);
            LayoutStyle_PropertyField(m_CounterTextTransform);
            LayoutStyle_PropertyField(m_CounterText);
            LayoutStyle_PropertyField(m_ValidationTextTransform);
            LayoutStyle_PropertyField(m_ValidationText);
            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_LeftContentTransform);
            LayoutStyle_PropertyField(m_LeftContentGraphic);
            LayoutStyle_PropertyField(m_RightContentTransform);
            LayoutStyle_PropertyField(m_RightContentGraphic);
            LayoutStyle_PropertyField(m_TopContentTransform);
            LayoutStyle_PropertyField(m_BottomContentTransform);
            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_ClearButton);
            EditorGUI.indentLevel--;
        }
    }
}