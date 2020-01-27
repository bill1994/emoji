//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaterialUI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ImageCarousel))]
    class ImageCarouselEditor : MaterialBaseEditor
    {
        private ImageCarousel m_Carousel = null;

        private SerializedProperty m_AnimateTabs;
        private SerializedProperty m_TabItemTemplate;
        private SerializedProperty m_TabsContainer;
        private SerializedProperty m_PagesContainer;
        private SerializedProperty m_PagesRect;
        private SerializedProperty m_Indicator;
        private SerializedProperty m_CanScrollBetweenTabs;
        private SerializedProperty m_CarouselPageTemplate = null;
        private SerializedProperty m_CarouselData = null;

        //private AnimBool m_PagesAnimBool;

        protected virtual void OnEnable()
        {
            OnBaseEnable();

            m_Carousel = (ImageCarousel)target;

            m_AnimateTabs = serializedObject.FindProperty("m_AnimateTabs");
            m_TabItemTemplate = serializedObject.FindProperty("m_TabItemTemplate");
            m_TabsContainer = serializedObject.FindProperty("m_TabsContainer");
            m_PagesContainer = serializedObject.FindProperty("m_PagesContainer");
            m_PagesRect = serializedObject.FindProperty("m_PagesRect");
            m_Indicator = serializedObject.FindProperty("m_Indicator");
            m_CanScrollBetweenTabs = serializedObject.FindProperty("m_CanScrollBetweenTabs");
            m_CarouselPageTemplate = serializedObject.FindProperty("m_CarouselPageTemplate");
            m_CarouselData = serializedObject.FindProperty("m_CarouselData");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_AnimateTabs);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_CanScrollBetweenTabs);

            if (EditorGUI.EndChangeCheck())
            {
                ((ImageCarousel)serializedObject.targetObject).canScrollBetweenTabs = m_CanScrollBetweenTabs.boolValue;
            }

            DrawFoldoutComponents(ComponentsSection);

            EditorGUILayout.PropertyField(m_CarouselPageTemplate);
            EditorGUILayout.PropertyField(m_CarouselData);

            serializedObject.ApplyModifiedProperties();
        }

        private void ComponentsSection()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_TabItemTemplate);
            EditorGUILayout.PropertyField(m_TabsContainer);
            EditorGUILayout.PropertyField(m_PagesRect);
            EditorGUILayout.PropertyField(m_PagesContainer);
            EditorGUILayout.PropertyField(m_Indicator);
            EditorGUI.indentLevel--;
        }
    }
}