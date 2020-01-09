﻿#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;
using UnityEngine.Events;

namespace UnityEditorInternal
{
    public class BaseUnityEventExDrawer : PropertyDrawer
    {
        protected class State
        {
            internal ReorderableList m_ReorderableList;
            public int lastSelectedIndex;
        }

        private const string kNoFunctionString = "No Function";

        //Persistent Listener Paths
        internal const string kInstancePath = "m_Target";
        internal const string kInstanceTypePath = "m_TargetAssemblyTypeName";
        internal const string kCallStatePath = "m_CallState";
        internal const string kArgumentsPath = "m_Arguments";
        internal const string kModePath = "m_Mode";
        internal const string kMethodNamePath = "m_MethodName";

        //ArgumentCache paths
        internal const string kFloatArgument = "m_FloatArgument";
        internal const string kIntArgument = "m_IntArgument";
        internal const string kObjectArgument = "m_ObjectArgument";
        internal const string kStringArgument = "m_StringArgument";
        internal const string kBoolArgument = "m_BoolArgument";
        internal const string kObjectArgumentAssemblyTypeName = "m_ObjectArgumentAssemblyTypeName";
        internal const string kSerializedDataArgument = "m_SerializedDataArgument";
        internal const string kSerializedDataArgumentAssemblyTypeName = "m_SerializedDataArgumentAssemblyTypeName";
        string m_Text;
        UnityEventBaseEx m_DummyEvent;
        SerializedProperty m_Prop;
        SerializedProperty m_ListenersArray;

        private static readonly GUIContent s_MixedValueContent = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
        const int kSpacing = 5;
        const int kExtraSpacing = 9;

        //State:
        ReorderableList m_ReorderableList;
        int m_LastSelectedIndex;
        Dictionary<string, State> m_States = new Dictionary<string, State>();

        static string GetEventParams(UnityEventBaseEx evt)
        {
            var methodInfo = evt.FindMethod("Invoke", evt.GetType(), UnityEngine.Events.PersistentListenerMode.EventDefined, null);

            var sb = new StringBuilder();
            sb.Append(" (");

            var types = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
            for (int i = 0; i < types.Length; i++)
            {
                sb.Append(types[i].Name);
                if (i < types.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        private State GetState(SerializedProperty prop)
        {
            State state;
            string key = prop.propertyPath;
            m_States.TryGetValue(key, out state);
            // ensure the cached SerializedProperty is synchronized (case 974069)
            if (state == null || state.m_ReorderableList.serializedProperty.serializedObject != prop.serializedObject)
            {
                if (state == null)
                    state = new State();

                SerializedProperty listenersArray = prop.FindPropertyRelative("m_PersistentCalls.m_Calls");
                state.m_ReorderableList =
                    new ReorderableList(prop.serializedObject, listenersArray, true, true, true, true)
                    {
                        drawHeaderCallback = DrawEventHeader,
                        drawElementCallback = DrawEvent,
                        onSelectCallback = OnSelectEvent,
                        onReorderCallback = OnReorderEvent,
                        onAddCallback = OnAddEvent,
                        onRemoveCallback = OnRemoveEvent
                    };
                SetupReorderableList(state.m_ReorderableList);

                m_States[key] = state;
            }
            return state;
        }

        private State RestoreState(SerializedProperty property)
        {
            State state = GetState(property);

            m_ListenersArray = state.m_ReorderableList.serializedProperty;
            m_ReorderableList = state.m_ReorderableList;
            m_LastSelectedIndex = state.lastSelectedIndex;
            m_ReorderableList.index = m_LastSelectedIndex;

            return state;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            m_Prop = property;
            m_Text = label.text;

            State state = RestoreState(property);

            OnGUI(position);
            state.lastSelectedIndex = m_LastSelectedIndex;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            //TODO: Also we need to have a constructor or initializer called for this property Drawer, before OnGUI or GetPropertyHeight
            //otherwise, we get Restore the State twice, once here and again in OnGUI. Maybe we should only do it here?
            RestoreState(property);

            float height = 0f;
            if (m_ReorderableList != null)
            {
                height = m_ReorderableList.GetHeight();
            }
            return height;
        }

        public void OnGUI(Rect position)
        {
            if (m_ListenersArray == null || !m_ListenersArray.isArray)
                return;

            m_DummyEvent = GetDummyEvent(m_Prop);
            if (m_DummyEvent == null)
                return;

            if (m_ReorderableList != null)
            {
                var oldIdentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                m_ReorderableList.DoList(position);
                EditorGUI.indentLevel = oldIdentLevel;
            }
        }

        protected virtual void SetupReorderableList(ReorderableList list)
        {
            // Two standard lines with standard spacing between and extra spacing below to better separate items visually.
            list.elementHeight = EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing + kExtraSpacing;
        }

        protected virtual void DrawEventHeader(Rect headerRect)
        {
            headerRect.height = EditorGUIUtility.singleLineHeight;
            string text = (string.IsNullOrEmpty(m_Text) ? "Event Ex" : m_Text + " Ex") + GetEventParams(m_DummyEvent);
            GUI.Label(headerRect, text);
        }

        static UnityEngine.Events.PersistentListenerMode GetMode(SerializedProperty mode)
        {
            return (UnityEngine.Events.PersistentListenerMode)mode.enumValueIndex;
        }

        protected virtual void DrawEvent(Rect rect, int index, bool isActive, bool isFocused)
        {
            var pListener = m_ListenersArray.GetArrayElementAtIndex(index);

            rect.y++;
            Rect[] subRects = GetRowRects(rect);
            Rect enabledRect = subRects[0];
            Rect goRect = subRects[1];
            Rect functionRect = subRects[2];
            Rect argRect = subRects[3];

            // find the current event target...
            var callState = pListener.FindPropertyRelative(kCallStatePath);
            var mode = pListener.FindPropertyRelative(kModePath);
            var arguments = pListener.FindPropertyRelative(kArgumentsPath);
            var listenerTarget = pListener.FindPropertyRelative(kInstancePath);
            var methodName = pListener.FindPropertyRelative(kMethodNamePath);

            Color c = GUI.backgroundColor;
            GUI.backgroundColor = Color.white;

            EditorGUI.PropertyField(enabledRect, callState, GUIContent.none);

            EditorGUI.BeginChangeCheck();
            {
                GUI.Box(goRect, GUIContent.none);
                EditorGUI.PropertyField(goRect, listenerTarget, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                    methodName.stringValue = null;
            }

            SerializedProperty argument;
            var modeEnum = GetMode(mode);
            //only allow argument if we have a valid target / method
            if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
                modeEnum = UnityEngine.Events.PersistentListenerMode.Void;

            System.Type desiredDataType = null;
            switch (modeEnum)
            {
                case UnityEngine.Events.PersistentListenerMode.Float:
                    argument = arguments.FindPropertyRelative(kFloatArgument);
                    break;
                case UnityEngine.Events.PersistentListenerMode.Int:
                    argument = arguments.FindPropertyRelative(kIntArgument);
                    break;
                case UnityEngine.Events.PersistentListenerMode.Object:
                    argument = arguments.FindPropertyRelative(kObjectArgument);
                    break;
                case UnityEngine.Events.PersistentListenerMode.String:
                    {
                        var desiredDataTypeName = arguments.FindPropertyRelative(kSerializedDataArgumentAssemblyTypeName).stringValue;
                        desiredDataType = typeof(object);
                        if (!string.IsNullOrEmpty(desiredDataTypeName))
                            desiredDataType = Type.GetType(desiredDataTypeName, false) ?? typeof(object);
                        else
                            desiredDataType = typeof(string);

                        if (desiredDataType != null && !desiredDataType.IsSubclassOf(typeof(string)) && desiredDataType != typeof(string))
                            argument = arguments.FindPropertyRelative(kSerializedDataArgument);
                        else
                            argument = arguments.FindPropertyRelative(kStringArgument);
                        break;
                    }
                case UnityEngine.Events.PersistentListenerMode.Bool:
                    argument = arguments.FindPropertyRelative(kBoolArgument);
                    break;
                default:
                    argument = arguments.FindPropertyRelative(kIntArgument);
                    break;
            }

            var desiredArgTypeName = arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName).stringValue;
            var desiredType = typeof(Object);
            if (!string.IsNullOrEmpty(desiredArgTypeName))
                desiredType = Type.GetType(desiredArgTypeName, false) ?? typeof(Object);

            if (modeEnum == UnityEngine.Events.PersistentListenerMode.Object)
            {
                EditorGUI.BeginChangeCheck();
                var result = EditorGUI.ObjectField(argRect, GUIContent.none, argument.objectReferenceValue, desiredType, true);
                if (EditorGUI.EndChangeCheck())
                    argument.objectReferenceValue = result;
            }
            else if (modeEnum != UnityEngine.Events.PersistentListenerMode.Void && modeEnum != UnityEngine.Events.PersistentListenerMode.EventDefined)
            {
                var drawDefault = false;
                if (modeEnum == UnityEngine.Events.PersistentListenerMode.String && desiredDataType != null && !desiredDataType.IsSubclassOf(typeof(string)) && desiredDataType != typeof(string))
                {
                    try
                    {
                        if (desiredDataType.IsSubclassOf(typeof(Color)) || desiredDataType == typeof(Color) || desiredDataType.IsSubclassOf(typeof(Color32)) || desiredDataType == typeof(Color32))
                        {
                            var color = string.IsNullOrEmpty(argument.stringValue)? Color.white : (Color)ArgumentCacheEx.FromJson(argument.stringValue, desiredDataType);
                            var newColor = EditorGUI.ColorField(argRect, color);
                            if (color != newColor)
                                argument.stringValue = ArgumentCacheEx.ToJson(newColor);
                        }
                        else if (desiredDataType.IsEnum)
                        {
                            var enumValue = string.IsNullOrEmpty(argument.stringValue) ? (System.Enum)System.Enum.ToObject(desiredDataType, 0): (System.Enum)ArgumentCacheEx.FromJson(argument.stringValue, desiredDataType);
                            var newValue = EditorGUI.EnumPopup(argRect, enumValue);
                            if (enumValue != newValue)
                                argument.stringValue = ArgumentCacheEx.ToJson(newValue);
                        }
                        else
                            drawDefault = true;
                    }
                    catch
                    {
                        drawDefault = true;
                    }
                }
                else
                    drawDefault = true;

                if(drawDefault)
                {
                    EditorGUI.PropertyField(argRect, argument, GUIContent.none);
                }
            }

            using (new EditorGUI.DisabledScope(listenerTarget.objectReferenceValue == null))
            {
                EditorGUI.BeginProperty(functionRect, GUIContent.none, methodName);
                {
                    GUIContent buttonContent;
                    if (EditorGUI.showMixedValue)
                    {
                        buttonContent = s_MixedValueContent;
                    }
                    else
                    {
                        var buttonLabel = new StringBuilder();
                        if (listenerTarget.objectReferenceValue == null || string.IsNullOrEmpty(methodName.stringValue))
                        {
                            buttonLabel.Append(kNoFunctionString);
                        }
                        else if (!IsPersistantListenerValid(m_DummyEvent, methodName.stringValue, listenerTarget.objectReferenceValue, GetMode(mode), GetMode(mode) == UnityEngine.Events.PersistentListenerMode.String? desiredDataType : desiredType))
                        {
                            var instanceString = "UnknownComponent";
                            var instance = listenerTarget.objectReferenceValue;
                            if (instance != null)
                                instanceString = instance.GetType().Name;

                            buttonLabel.Append(string.Format("<Missing {0}.{1}>", instanceString, methodName.stringValue));
                        }
                        else
                        {
                            buttonLabel.Append(listenerTarget.objectReferenceValue.GetType().Name);

                            if (!string.IsNullOrEmpty(methodName.stringValue))
                            {
                                buttonLabel.Append(".");
                                if (methodName.stringValue.StartsWith("set_"))
                                    buttonLabel.Append(methodName.stringValue.Substring(4));
                                else
                                    buttonLabel.Append(methodName.stringValue);
                            }
                        }
                        buttonContent = new GUIContent(buttonLabel.ToString());
                    }

                    if (GUI.Button(functionRect, buttonContent, EditorStyles.popup))
                        BuildPopupList(listenerTarget.objectReferenceValue, m_DummyEvent, pListener).DropDown(functionRect);
                }
                EditorGUI.EndProperty();
            }
            GUI.backgroundColor = c;
        }

        Rect[] GetRowRects(Rect rect)
        {
            Rect[] rects = new Rect[4];

            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 2;

            Rect enabledRect = rect;
            enabledRect.width *= 0.3f;

            Rect goRect = enabledRect;
            goRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            Rect functionRect = rect;
            functionRect.xMin = goRect.xMax + kSpacing;

            Rect argRect = functionRect;
            argRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            rects[0] = enabledRect;
            rects[1] = goRect;
            rects[2] = functionRect;
            rects[3] = argRect;
            return rects;
        }

        protected virtual void OnRemoveEvent(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            m_LastSelectedIndex = list.index;
        }

        protected virtual void OnAddEvent(ReorderableList list)
        {
            if (m_ListenersArray.hasMultipleDifferentValues)
            {
                //When increasing a multi-selection array using Serialized Property
                //Data can be overwritten if there is mixed values.
                //The Serialization system applies the Serialized data of one object, to all other objects in the selection.
                //We handle this case here, by creating a SerializedObject for each object.
                //Case 639025.
                foreach (var targetObject in m_ListenersArray.serializedObject.targetObjects)
                {
                    var temSerialziedObject = new SerializedObject(targetObject);
                    var listenerArrayProperty = temSerialziedObject.FindProperty(m_ListenersArray.propertyPath);
                    listenerArrayProperty.arraySize += 1;
                    temSerialziedObject.ApplyModifiedProperties();
                }
                m_ListenersArray.serializedObject.SetIsDifferentCacheDirty();
                m_ListenersArray.serializedObject.Update();
                list.index = list.serializedProperty.arraySize - 1;
            }
            else
            {
                ReorderableList.defaultBehaviours.DoAddButton(list);
            }

            m_LastSelectedIndex = list.index;
            var pListener = m_ListenersArray.GetArrayElementAtIndex(list.index);

            var callState = pListener.FindPropertyRelative(kCallStatePath);
            var listenerTarget = pListener.FindPropertyRelative(kInstancePath);
            var methodName = pListener.FindPropertyRelative(kMethodNamePath);
            var mode = pListener.FindPropertyRelative(kModePath);
            var arguments = pListener.FindPropertyRelative(kArgumentsPath);

            callState.enumValueIndex = (int)UnityEngine.Events.UnityEventCallState.RuntimeOnly;
            listenerTarget.objectReferenceValue = null;
            methodName.stringValue = null;
            mode.enumValueIndex = (int)UnityEngine.Events.PersistentListenerMode.Void;
            arguments.FindPropertyRelative(kFloatArgument).floatValue = 0;
            arguments.FindPropertyRelative(kIntArgument).intValue = 0;
            arguments.FindPropertyRelative(kObjectArgument).objectReferenceValue = null;
            arguments.FindPropertyRelative(kStringArgument).stringValue = null;
            arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName).stringValue = null;
        }

        protected virtual void OnSelectEvent(ReorderableList list)
        {
            m_LastSelectedIndex = list.index;
        }

        protected virtual void OnReorderEvent(ReorderableList list)
        {
            m_LastSelectedIndex = list.index;
        }

        static UnityEventBaseEx GetDummyEvent(SerializedProperty prop)
        {
            //Use the SerializedProperty path to iterate through the fields of the inspected targetObject
            Object tgtobj = prop.serializedObject.targetObject;
            if (tgtobj == null)
                return new UnityEventEx();

            UnityEventBaseEx ret = null;
            Type ft = tgtobj.GetType();
            var bindflags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            do
            {
                ret = GetDummyEventHelper(prop.propertyPath, ft, bindflags);
                //no need to look for public members again since the base type covered that
                bindflags = BindingFlags.Instance | BindingFlags.NonPublic;
                ft = ft.BaseType;
            }
            while (ret == null && ft != null);
            // go up the class hierarchy if it exists and the property is not found on the child
            return (ret == null) ? new UnityEventEx() : ret;
        }

        private static UnityEventBaseEx GetDummyEventHelper(string propPath, Type targetObjectType, BindingFlags flags)
        {
            while (propPath.Length != 0)
            {
                //we could have a leftover '.' if the previous iteration handled an array element
                if (propPath.StartsWith("."))
                    propPath = propPath.Substring(1);

                var splits = propPath.Split(new[] { '.' }, 2);
                var newField = targetObjectType.GetField(splits[0], flags);
                if (newField == null)
                    break;

                targetObjectType = newField.FieldType;
                if (IsArrayOrList(targetObjectType))
                    targetObjectType = GetArrayOrListElementType(targetObjectType);

                //the last item in the property path could have been an array element
                //bail early in that case
                if (splits.Length == 1)
                    break;

                propPath = splits[1];
                if (propPath.StartsWith("Array.data["))
                    propPath = propPath.Split(new[] { ']' }, 2)[1];
            }
            if (targetObjectType.IsSubclassOf(typeof(UnityEventBaseEx)))
                return Activator.CreateInstance(targetObjectType) as UnityEventBaseEx;
            return null;
        }

        struct ValidMethodMap
        {
            public Object target;
            public MethodInfo methodInfo;
            public UnityEngine.Events.PersistentListenerMode mode;
        }

        static IEnumerable<ValidMethodMap> CalculateMethodMap(Object target, Type[] t, bool allowSubclasses)
        {
            var validMethods = new List<ValidMethodMap>();
            if (target == null || t == null)
                return validMethods;

            // find the methods on the behaviour that match the signature
            Type componentType = target.GetType();
            var componentMethods = componentType.GetMethods().Where(x => !x.IsSpecialName).ToList();

            var wantedProperties = componentType.GetProperties().AsEnumerable();
            wantedProperties = wantedProperties.Where(x => x.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0 && x.GetSetMethod() != null);
            componentMethods.AddRange(wantedProperties.Select(x => x.GetSetMethod()));

            foreach (var componentMethod in componentMethods)
            {
                //Debug.Log ("Method: " + componentMethod);
                // if the argument length is not the same, no match
                var componentParamaters = componentMethod.GetParameters();
                if (componentParamaters.Length != t.Length)
                    continue;

                // Don't show obsolete methods.
                if (componentMethod.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length > 0)
                    continue;

                if (componentMethod.ReturnType != typeof(void))
                    continue;

                // if the argument types do not match, no match
                bool paramatersMatch = true;
                for (int i = 0; i < t.Length; i++)
                {
                    if (!componentParamaters[i].ParameterType.IsAssignableFrom(t[i]))
                        paramatersMatch = false;

                    if (allowSubclasses && t[i].IsAssignableFrom(componentParamaters[i].ParameterType))
                        paramatersMatch = true;
                }

                // valid method
                if (paramatersMatch)
                {
                    var vmm = new ValidMethodMap
                    {
                        target = target,
                        methodInfo = componentMethod
                    };
                    validMethods.Add(vmm);
                }
            }
            return validMethods;
        }

        public static bool IsPersistantListenerValid(UnityEventBaseEx dummyEvent, string methodName, Object uObject, UnityEngine.Events.PersistentListenerMode modeEnum, Type argumentType)
        {
            if (uObject == null || string.IsNullOrEmpty(methodName))
                return false;

            return dummyEvent.FindMethod(methodName, uObject.GetType(), modeEnum, argumentType) != null;
        }

        static GenericMenu BuildPopupList(Object target, UnityEventBaseEx dummyEvent, SerializedProperty listener)
        {
            //special case for components... we want all the game objects targets there!
            var targetToUse = target;
            if (targetToUse is Component)
                targetToUse = (target as Component).gameObject;

            // find the current event target...
            var methodName = listener.FindPropertyRelative(kMethodNamePath);

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(kNoFunctionString),
                string.IsNullOrEmpty(methodName.stringValue),
                ClearEventFunction,
                new UnityEventFunction(listener, null, null, UnityEngine.Events.PersistentListenerMode.EventDefined));

            if (targetToUse == null)
                return menu;

            menu.AddSeparator("");

            // figure out the signature of this delegate...
            // The property at this stage points to the 'container' and has the field name
            Type delegateType = dummyEvent.GetType();

            // check out the signature of invoke as this is the callback!
            MethodInfo delegateMethod = delegateType.GetMethod("Invoke");
            var delegateArgumentsTypes = delegateMethod.GetParameters().Select(x => x.ParameterType).ToArray();

            GeneratePopUpForType(menu, targetToUse, false, listener, delegateArgumentsTypes);
            if (targetToUse is GameObject)
            {
                Component[] comps = (targetToUse as GameObject).GetComponents<Component>();
                var duplicateNames = comps.Where(c => c != null).Select(c => c.GetType().Name).GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                foreach (Component comp in comps)
                {
                    if (comp == null)
                        continue;

                    GeneratePopUpForType(menu, comp, duplicateNames.Contains(comp.GetType().Name), listener, delegateArgumentsTypes);
                }
            }

            return menu;
        }

        private static void GeneratePopUpForType(GenericMenu menu, Object target, bool useFullTargetName, SerializedProperty listener, Type[] delegateArgumentsTypes)
        {
            var methods = new List<ValidMethodMap>();
            string targetName = useFullTargetName ? target.GetType().FullName : target.GetType().Name;

            bool didAddDynamic = false;

            // skip 'void' event defined on the GUI as we have a void prebuilt type!
            if (delegateArgumentsTypes.Length != 0)
            {
                GetMethodsForTargetAndMode(target, delegateArgumentsTypes, methods, UnityEngine.Events.PersistentListenerMode.EventDefined);
                if (methods.Count > 0)
                {
                    menu.AddDisabledItem(new GUIContent(targetName + "/Dynamic " + string.Join(", ", delegateArgumentsTypes.Select(e => GetTypeName(e)).ToArray())));
                    AddMethodsToMenu(menu, listener, methods, targetName);
                    didAddDynamic = true;
                }
            }

            methods.Clear();
            GetMethodsForTargetAndMode(target, new[] { typeof(float) }, methods, UnityEngine.Events.PersistentListenerMode.Float);
            GetMethodsForTargetAndMode(target, new[] { typeof(int) }, methods, UnityEngine.Events.PersistentListenerMode.Int);
            GetMethodsForTargetAndMode(target, new[] { typeof(string) }, methods, UnityEngine.Events.PersistentListenerMode.String);
            GetMethodsForTargetAndMode(target, new[] { typeof(bool) }, methods, UnityEngine.Events.PersistentListenerMode.Bool);
            GetMethodsForTargetAndMode(target, new[] { typeof(Object) }, methods, UnityEngine.Events.PersistentListenerMode.Object);
            GetMethodsForTargetAndMode(target, new Type[] { }, methods, UnityEngine.Events.PersistentListenerMode.Void);

            GetMethodsForTargetAndMode(target, new[] { typeof(Color) }, methods, UnityEngine.Events.PersistentListenerMode.String);
            GetMethodsForTargetAndMode(target, new[] { typeof(Color32) }, methods, UnityEngine.Events.PersistentListenerMode.String);
            GetMethodsForTargetAndMode(target, new[] { typeof(Enum) }, methods, UnityEngine.Events.PersistentListenerMode.String);
            if (methods.Count > 0)
            {
                if (didAddDynamic)
                    // AddSeperator doesn't seem to work for sub-menus, so we have to use this workaround instead of a proper separator for now.
                    menu.AddItem(new GUIContent(targetName + "/ "), false, null);
                if (delegateArgumentsTypes.Length != 0)
                    menu.AddDisabledItem(new GUIContent(targetName + "/Static Parameters"));
                AddMethodsToMenu(menu, listener, methods, targetName);
            }
        }

        private static void AddMethodsToMenu(GenericMenu menu, SerializedProperty listener, List<ValidMethodMap> methods, string targetName)
        {
            // Note: sorting by a bool in OrderBy doesn't seem to work for some reason, so using numbers explicitly.
            IEnumerable<ValidMethodMap> orderedMethods = methods.OrderBy(e => e.methodInfo.Name.StartsWith("set_") ? 0 : 1).ThenBy(e => e.methodInfo.Name);
            foreach (var validMethod in orderedMethods)
                AddFunctionsForScript(menu, listener, validMethod, targetName);
        }

        private static void GetMethodsForTargetAndMode(Object target, Type[] delegateArgumentsTypes, List<ValidMethodMap> methods, UnityEngine.Events.PersistentListenerMode mode)
        {
            IEnumerable<ValidMethodMap> newMethods = CalculateMethodMap(target, delegateArgumentsTypes, mode == UnityEngine.Events.PersistentListenerMode.Object || mode == UnityEngine.Events.PersistentListenerMode.String);
            foreach (var m in newMethods)
            {
                var method = m;
                method.mode = mode;
                methods.Add(method);
            }
        }

        static void AddFunctionsForScript(GenericMenu menu, SerializedProperty listener, ValidMethodMap method, string targetName)
        {
            UnityEngine.Events.PersistentListenerMode mode = method.mode;

            // find the current event target...
            var listenerTarget = listener.FindPropertyRelative(kInstancePath).objectReferenceValue;
            var methodName = listener.FindPropertyRelative(kMethodNamePath).stringValue;
            var setMode = GetMode(listener.FindPropertyRelative(kModePath));
            var typeName = listener.FindPropertyRelative(kArgumentsPath).FindPropertyRelative(kObjectArgumentAssemblyTypeName);

            var args = new StringBuilder();
            var count = method.methodInfo.GetParameters().Length;
            for (int index = 0; index < count; index++)
            {
                var methodArg = method.methodInfo.GetParameters()[index];
                args.Append(string.Format("{0}", GetTypeName(methodArg.ParameterType)));

                if (index < count - 1)
                    args.Append(", ");
            }

            var isCurrentlySet = listenerTarget == method.target
                && methodName == method.methodInfo.Name
                && mode == setMode;

            if (isCurrentlySet && mode == UnityEngine.Events.PersistentListenerMode.Object && method.methodInfo.GetParameters().Length == 1)
            {
                isCurrentlySet &= (method.methodInfo.GetParameters()[0].ParameterType.AssemblyQualifiedName == typeName.stringValue);
            }

            string path = GetFormattedMethodName(targetName, method.methodInfo.Name, args.ToString(), mode == UnityEngine.Events.PersistentListenerMode.EventDefined);
            menu.AddItem(new GUIContent(path),
                isCurrentlySet,
                SetEventFunction,
                new UnityEventFunction(listener, method.target, method.methodInfo, mode));
        }

        private static string GetTypeName(Type t)
        {
            if (t == typeof(int))
                return "int";
            if (t == typeof(float))
                return "float";
            if (t == typeof(string))
                return "string";
            if (t == typeof(bool))
                return "bool";
            return t.Name;
        }

        static string GetFormattedMethodName(string targetName, string methodName, string args, bool dynamic)
        {
            if (dynamic)
            {
                if (methodName.StartsWith("set_"))
                    return string.Format("{0}/{1}", targetName, methodName.Substring(4));
                else
                    return string.Format("{0}/{1}", targetName, methodName);
            }
            else
            {
                if (methodName.StartsWith("set_"))
                    return string.Format("{0}/{2} {1}", targetName, methodName.Substring(4), args);
                else
                    return string.Format("{0}/{1} ({2})", targetName, methodName, args);
            }
        }

        static void SetEventFunction(object source)
        {
            ((UnityEventFunction)source).Assign();
        }

        static void ClearEventFunction(object source)
        {
            ((UnityEventFunction)source).Clear();
        }

        struct UnityEventFunction
        {
            readonly SerializedProperty m_Listener;
            readonly Object m_Target;
            readonly MethodInfo m_Method;
            readonly UnityEngine.Events.PersistentListenerMode m_Mode;

            public UnityEventFunction(SerializedProperty listener, Object target, MethodInfo method, UnityEngine.Events.PersistentListenerMode mode)
            {
                m_Listener = listener;
                m_Target = target;
                m_Method = method;
                m_Mode = mode;
            }

            public void Assign()
            {
                // find the current event target...
                var listenerTarget = m_Listener.FindPropertyRelative(kInstancePath);
                var listenerTargetType = m_Listener.FindPropertyRelative(kInstanceTypePath);
                var methodName = m_Listener.FindPropertyRelative(kMethodNamePath);
                var mode = m_Listener.FindPropertyRelative(kModePath);
                var arguments = m_Listener.FindPropertyRelative(kArgumentsPath);

                listenerTarget.objectReferenceValue = m_Target;
                listenerTargetType.stringValue = m_Method.DeclaringType.AssemblyQualifiedName;
                methodName.stringValue = m_Method.Name;
                mode.enumValueIndex = (int)m_Mode;

                if (m_Mode == UnityEngine.Events.PersistentListenerMode.Object || m_Mode == UnityEngine.Events.PersistentListenerMode.String)
                {
                    var fullArgumentType = arguments.FindPropertyRelative(m_Mode == UnityEngine.Events.PersistentListenerMode.String? kObjectArgumentAssemblyTypeName : kSerializedDataArgumentAssemblyTypeName);
                    var argParams = m_Method.GetParameters();
                    if (argParams.Length == 1 && typeof(Object).IsAssignableFrom(argParams[0].ParameterType))
                        fullArgumentType.stringValue = argParams[0].ParameterType.AssemblyQualifiedName;
                    else
                        fullArgumentType.stringValue = typeof(Object).AssemblyQualifiedName;
                }
                if (m_Mode == UnityEngine.Events.PersistentListenerMode.String)
                {
                    var fullArgumentType = arguments.FindPropertyRelative(kSerializedDataArgumentAssemblyTypeName);
                    var argParams = m_Method.GetParameters();
                    if (argParams.Length == 1)
                        fullArgumentType.stringValue = argParams[0].ParameterType.AssemblyQualifiedName;
                    else
                        fullArgumentType.stringValue = string.Empty;
                }

                ValidateObjectParamater(arguments, m_Mode);

                m_Listener.serializedObject.ApplyModifiedProperties();
            }

            private void ValidateObjectParamater(SerializedProperty arguments, UnityEngine.Events.PersistentListenerMode mode)
            {
                var fullArgumentType = arguments.FindPropertyRelative(kObjectArgumentAssemblyTypeName);
                var argument = arguments.FindPropertyRelative(kObjectArgument);
                var argumentObj = argument.objectReferenceValue;

                if (mode != UnityEngine.Events.PersistentListenerMode.Object)
                {
                    fullArgumentType.stringValue = typeof(Object).AssemblyQualifiedName;
                    argument.objectReferenceValue = null;
                    return;
                }

                if (argumentObj == null)
                    return;

                Type t = Type.GetType(fullArgumentType.stringValue, false);
                if (!typeof(Object).IsAssignableFrom(t) || !t.IsInstanceOfType(argumentObj))
                    argument.objectReferenceValue = null;
            }

            public void Clear()
            {
                // find the current event target...
                var methodName = m_Listener.FindPropertyRelative(kMethodNamePath);
                methodName.stringValue = null;

                var mode = m_Listener.FindPropertyRelative(kModePath);
                mode.enumValueIndex = (int)UnityEngine.Events.PersistentListenerMode.Void;

                m_Listener.serializedObject.ApplyModifiedProperties();
            }
        }

        internal static bool IsArrayOrList(Type listType)
        {
            if (listType.IsArray)
            {
                return true;
            }
            else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return true;
            }
            return false;
        }

        internal static Type GetArrayOrListElementType(Type listType)
        {
            if (listType.IsArray)
            {
                return listType.GetElementType();
            }
            else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return listType.GetGenericArguments()[0];
            }
            return null;
        }
    }
}

#endif