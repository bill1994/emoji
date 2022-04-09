// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEditor;
using UnityEngine.UI;

namespace MaterialUI
{
    public class MaterialToggleBaseEditor : BaseStyleElementEditor
    {
        protected ToggleBase m_Toggle;

        protected SerializedProperty m_Interactable;
        protected SerializedProperty m_AnimationDuration;
        protected SerializedProperty m_AutoRegisterInParentGroup;
        protected SerializedProperty m_Group;

        protected SerializedProperty m_OnToggleOn;
        protected SerializedProperty m_OnToggleOff;

        protected SerializedProperty m_Graphic;
        //protected SerializedProperty m_iconData;
        //protected SerializedProperty m_LabelText;
        protected SerializedProperty m_GraphicChangesWithToggleState;
        protected SerializedProperty m_ToggleOnLabel;
        protected SerializedProperty m_ToggleOffLabel;
        protected SerializedProperty m_ToggleOnIcon;
        protected SerializedProperty m_ToggleOffIcon;

        protected SerializedProperty m_ChangeGraphicColor;
        protected SerializedProperty m_GraphicOnColor;
        protected SerializedProperty m_GraphicOffColor;
        protected SerializedProperty m_GraphicDisabledColor;
        protected SerializedProperty m_ChangeRippleColor;
        protected SerializedProperty m_RippleOnColor;
        protected SerializedProperty m_RippleOffColor;

        protected SerializedProperty m_ForceUseDisableColor;

        protected bool m_IsControllingChildren = false;

        protected override void OnEnable()
        {
            OnBaseEnable();

            m_Toggle = (ToggleBase)serializedObject.targetObject;

			m_Interactable = serializedObject.FindProperty("m_Interactable");
            m_AnimationDuration = serializedObject.FindProperty("m_AnimationDuration");

            m_Group = serializedObject.FindProperty("m_Group");
            m_AutoRegisterInParentGroup = serializedObject.FindProperty("m_AutoRegisterInParentGroup");

            m_Graphic = serializedObject.FindProperty("m_Graphic");
            //m_iconData = serializedObject.FindProperty("m_Icon");
            //m_LabelText = serializedObject.FindProperty("m_Label");
            m_GraphicChangesWithToggleState = serializedObject.FindProperty("m_ToggleGraphic");
            m_ToggleOnLabel = serializedObject.FindProperty("m_ToggleOnLabel");
            m_ToggleOffLabel = serializedObject.FindProperty("m_ToggleOffLabel");
            m_ToggleOnIcon = serializedObject.FindProperty("m_ToggleOnIcon");
            m_ToggleOffIcon = serializedObject.FindProperty("m_ToggleOffIcon");

            m_ChangeGraphicColor = serializedObject.FindProperty("m_ChangeGraphicColor");
            m_GraphicOnColor = serializedObject.FindProperty("m_GraphicOnColor");
            m_GraphicOffColor = serializedObject.FindProperty("m_GraphicOffColor");
            m_GraphicDisabledColor = serializedObject.FindProperty("m_GraphicDisabledColor");
            m_ChangeRippleColor = serializedObject.FindProperty("m_ChangeRippleColor");
            m_RippleOnColor = serializedObject.FindProperty("m_RippleOnColor");
            m_RippleOffColor = serializedObject.FindProperty("m_RippleOffColor");

            m_OnToggleOn = serializedObject.FindProperty("onToggleOn");
            m_OnToggleOff = serializedObject.FindProperty("onToggleOff");

            m_ForceUseDisableColor = serializedObject.FindProperty("m_ForceUseDisableColor");
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
                m_Toggle.interactable = m_Interactable.boolValue;
            }

            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_AnimationDuration);
            EditorGUILayout.Space();

            LayoutStyle_PropertyField(m_AutoRegisterInParentGroup);
            LayoutStyle_PropertyField(m_Group);
            EditorGUILayout.Space();

            if (m_Graphic.objectReferenceValue != null)
            {
                LayoutStyle_PropertyField(m_GraphicChangesWithToggleState);

                if (m_GraphicChangesWithToggleState.boolValue)
                {
                    if (m_Graphic.objectReferenceValue is Image || m_Graphic.objectReferenceValue is IVectorImage)
                    {
                        LayoutStyle_PropertyField(m_ToggleOnIcon);
                        LayoutStyle_PropertyField(m_ToggleOffIcon);
                    }
                    else
                    {
                        LayoutStyle_PropertyField(m_ToggleOnLabel);
                        LayoutStyle_PropertyField(m_ToggleOffLabel);
                    }
                }
                else
                {
                    var v_target = target as ToggleBase;
                    InspectorFields.GraphicMultiField("Graphic Value", new Graphic[] { v_target.graphic });
                }
            }

            EditorGUILayout.Space();
            InheritedFieldsSection();
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(m_IsControllingChildren);
			{
				EditorGUI.BeginChangeCheck();
				{
					DrawFoldoutColors(ColorsSection);
				}
				if (EditorGUI.EndChangeCheck())
				{
					m_Toggle.EditorValidate();
				}
			}
			EditorGUI.EndDisabledGroup(); 

            EditorGUI.BeginChangeCheck();
            {
                DrawFoldoutComponents(ComponentsSection);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_Toggle.EditorValidate();
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OnToggleOn);
            EditorGUILayout.PropertyField(m_OnToggleOff);

            DrawStyleGUIFolder();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void InheritedFieldsSection()
        {
        }

        protected virtual void ColorsSection()
        {
            EditorGUI.indentLevel++;
            if (m_Graphic.objectReferenceValue != null)
            {
                LayoutStyle_PropertyField(m_ChangeGraphicColor);

                if (m_ChangeGraphicColor.boolValue)
                {
                    LayoutStyle_PropertyField(m_GraphicOnColor);
                    LayoutStyle_PropertyField(m_GraphicOffColor);
                    LayoutStyle_PropertyField(m_GraphicDisabledColor);
                }
            }

            if (m_Toggle.GetComponent<MaterialRipple>())
            {
                LayoutStyle_PropertyField(m_ChangeRippleColor);
                if (m_ChangeRippleColor.boolValue)
                {
                    LayoutStyle_PropertyField(m_RippleOnColor);
                    LayoutStyle_PropertyField(m_RippleOffColor);
                }
            }
            EditorGUILayout.Space();
            LayoutStyle_PropertyField(m_ForceUseDisableColor);
            EditorGUI.indentLevel--;
        }

        protected virtual void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_Graphic);
            EditorGUI.indentLevel--;
        }

        protected virtual bool CanUseDisabledColor()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var toggleBase = targets[i] as ToggleBase;
                if (toggleBase != null && !toggleBase.CanUseDisabledColor())
                    return false;
            }
            return true;
        }
    }
}