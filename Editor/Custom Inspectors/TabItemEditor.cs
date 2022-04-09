// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using UnityEditor;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TabItem))]
	class TabItemEditor : MaterialToggleBaseEditor
    {
        private SerializedProperty m_ChangeIconColor;
        private SerializedProperty m_OnColor;
        private SerializedProperty m_OffColor;
        private SerializedProperty m_DisabledColor;

        private SerializedProperty m_ItemIndex;
        private SerializedProperty m_ItemIcon;
		private SerializedProperty m_TabView;

        protected override void OnEnable()
        {
			base.OnEnable();

            m_ItemIndex = serializedObject.FindProperty("m_ItemIndex");
            m_ItemIcon = serializedObject.FindProperty("m_ItemIcon");
			m_TabView = serializedObject.FindProperty("m_TabView");

            m_ChangeIconColor = serializedObject.FindProperty("m_ChangeIconColor");
            m_OnColor = serializedObject.FindProperty("m_OnColor");
            m_OffColor = serializedObject.FindProperty("m_OffColor");
            m_DisabledColor = serializedObject.FindProperty("m_DisabledColor");
        }

        protected override void ColorsSection()
        {
            EditorGUI.indentLevel++;

            if (m_ItemIcon.objectReferenceValue != null)
            {
                LayoutStyle_PropertyField(m_ChangeIconColor);

                if (m_ChangeIconColor.boolValue)
                {
                    LayoutStyle_PropertyField(m_OnColor);
                    LayoutStyle_PropertyField(m_OffColor);
                    LayoutStyle_PropertyField(m_DisabledColor);
                }
            }
            EditorGUI.indentLevel--;

            base.ColorsSection();
        }

        protected override void ComponentsSection()
		{
            base.ComponentsSection();
			EditorGUI.indentLevel++;
            LayoutStyle_PropertyField(m_ItemIcon);
            LayoutStyle_PropertyField(m_TabView);
            LayoutStyle_PropertyField(m_ItemIndex);
            EditorGUI.indentLevel--;
		}
    }
}