#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEditorInternal
{
	[CustomPropertyDrawer(typeof(UnityEventBaseEx), true)]
	public class UnityEventExDrawer : BaseUnityEventExDrawer
    {
        const BindingFlags kBfAll = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
		static readonly FieldInfo s_FiReorderableList = typeof(BaseUnityEventExDrawer).GetField("m_ReorderableList", kBfAll);
		static readonly FieldInfo s_FiCalls = typeof(UnityEventBaseEx).GetField("m_Calls", kBfAll);
		static readonly FieldInfo s_FiRuntimeCalls = typeof(UnityEngine.Events.InvokableCallList).GetField("m_RuntimeCalls", kBfAll);
		static GUIStyle s_CachedStyleToggle;
		static GUIStyle s_CachedStyleBg;


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Get the ReorderableList for default drawer.
			ReorderableList ro = s_FiReorderableList.GetValue(this) as ReorderableList;
			if (ro == null)
			{
				//base.GetPropertyHeight(property, label);
				ro = s_FiReorderableList.GetValue(this) as ReorderableList;
			}

            //ro.elementHeight = EditorGUIUtility.singleLineHeight * 2 + (EditorGUIUtility.singleLineHeight - 5);
            // If persistent calls is empty, display it compactry.
            //bool isEmpty = property.FindPropertyRelative("m_PersistentCalls").FindPropertyRelative("m_Calls").arraySize == 0;
            //ro.elementHeight = isEmpty
            //	? EditorGUIUtility.singleLineHeight
            //	: EditorGUIUtility.singleLineHeight * 2 + (EditorGUIUtility.singleLineHeight - 5);

            // If drawer is folded, skip drawing runtime calls.
            return property.isExpanded
                ? base.GetPropertyHeight(property, label) + (GetRuntimeCalls(property).Count * (EditorGUIUtility.singleLineHeight + 1))
                : base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Draw background and toggle.
			var RuntimeCalls = GetRuntimeCalls(property);
			float height = property.isExpanded
                ? RuntimeCalls.Count * (EditorGUIUtility.singleLineHeight + 1) + EditorGUIUtility.singleLineHeight
                : EditorGUIUtility.singleLineHeight;
			var r = new Rect(position.x + 2, position.y + position.height - 1 - height, position.width - 4, height);
			DrawRuntimeCallToggle(r, property, RuntimeCalls.Count);

			// Draw UnityEvent using default drawer.
			base.OnGUI(position, property, label);

			// If drawer is folded, skip drawing runtime calls.
			if (!property.isExpanded)
			{
				return;
			}

			// Draw runtime calls.
			r = new Rect(r.x + EditorGUIUtility.singleLineHeight, r.y + (EditorGUIUtility.singleLineHeight), r.width - EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var oldFontSize = EditorStyles.objectField.fontSize;
            EditorStyles.objectField.fontSize = oldFontSize - 2;
			foreach (var invokableCall in RuntimeCalls)
			{
				var fi = invokableCall.GetMemberInfo("Delegate", MemberTypes.Field) as FieldInfo;
				Delegate del = fi.GetValue(invokableCall) as Delegate;

				// Draw delegate.
				DrawDelegate(r, del);
				r.y += r.height + 1;
			}
			EditorStyles.objectField.fontSize = oldFontSize;
		}

		static void DrawRuntimeCallToggle(Rect position, SerializedProperty property, int count)
		{
			// Cache style.
			if (s_CachedStyleBg == null)
			{
				s_CachedStyleBg = new GUIStyle("ProgressBarBack");
				s_CachedStyleToggle = new GUIStyle("OL Toggle") { fontSize = 9 };
				s_CachedStyleToggle.onNormal.textColor =
					s_CachedStyleToggle.normal.textColor = 
						s_CachedStyleToggle.onActive.textColor = 
							s_CachedStyleToggle.active.textColor = EditorStyles.label.normal.textColor;
			}

			// Draw background.
			GUI.Label(position, "", s_CachedStyleBg);

			// Draw foldout with label.
			string text = string.Format("Show runtime calls ({0})", count);
            property.isExpanded = GUI.Toggle(new Rect(position.x, position.y, position.width - 80, 14), property.isExpanded, text, s_CachedStyleToggle);
		}

		static void DrawDelegate(Rect position, Delegate del)
		{
			try
			{
				Rect r = new Rect(position.x, position.y, position.width * 0.3f, position.height);
				MethodInfo method = del.Method;
				object target = del.Target;

				// Draw the target if possible.
				var obj = target as UnityEngine.Object;
                var oldGuiEnabled = GUI.enabled;
                GUI.enabled = false;
                if (obj)
				{
					EditorGUI.ObjectField(r, obj, obj.GetType(), true);
				}
				else if (target != null)
				{
				    EditorGUI.LabelField(r, string.Format("{0} ({1})", target.ToString(), target.GetType()), EditorStyles.miniLabel);
				}
				else
				{
					EditorGUI.LabelField(r, "null", EditorStyles.miniLabel);
				}
                GUI.enabled = oldGuiEnabled;

                // Draw the method name.
                r.x += r.width;
				r.width = position.width - r.width;
				EditorGUI.LabelField(r, method.ReflectedType + "." + method.Name, EditorStyles.miniLabel);
			}
			catch
			{
				EditorGUI.LabelField(position, "null delegate", EditorStyles.miniLabel);
			}
		}
		public static IList GetRuntimeCalls(SerializedProperty property)
		{
			var propertyInstance = property.GetInstance();

			return propertyInstance != null
				? s_FiRuntimeCalls.GetValue(s_FiCalls.GetValue(propertyInstance)) as IList
				: new List<object>() as IList;
		}
	}
}

#endif
