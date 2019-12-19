//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

namespace MaterialUI
{
    public interface ITweenBehaviour
    {
        Transform transform { get; }
        GameObject gameObject { get; }

        void Tween(string tag, Action<string> callback);
        bool IsDestroyed();
    }

    public abstract class AbstractTweenBehaviour : MonoBehaviour, ITweenBehaviour
    {
        public void Tween(string tag, Action callback)
        {
            Tween(tag, callback != null ? (tagResult) => { callback.InvokeIfNotNull(); } : (Action<string>)null);
        }

        public abstract void Tween(string tag, Action<string> callback);

        bool ITweenBehaviour.IsDestroyed()
        {
            return this == null;
        }
    }

    [AddComponentMenu("MaterialUI/Easy Tween", 100)]
    public class EasyTween : AbstractTweenBehaviour, IEventSystemHandler
    {
        #region Helper Classes

        [Serializable]
        public class EasyTweenObject
        {
            #region Private Variables

            [SerializeField]
            private string m_Tag = null;
            [SerializeField]
            private GameObject m_TargetGameObject = null;
            [SerializeField]
            private Tween.TweenType m_TweenType = MaterialUI.Tween.TweenType.Custom;
            [SerializeField]
            private AnimationCurve m_CustomCurve = null;
            [SerializeField]
            private float m_Duration = 0;
            [SerializeField]
            private float m_Delay = 0;
            [SerializeField]
            private bool m_TweenOnStart = false;
            [SerializeField]
            private bool m_HasCallback = false;
            [SerializeField]
            private GameObject m_CallbackGameObject = null;
            [SerializeField]
            private Component m_CallbackComponent = null;
            [SerializeField]
            private string m_CallbackComponentName = null;
            [SerializeField]
            private MethodInfo m_CallbackMethodInfo = null;
            [SerializeField]
            private string m_CallbackName = null;
            [SerializeField]
            private bool m_OptionsVisible = false;
            [SerializeField]
            private bool m_SubOptionsVisible = false;
            [SerializeField]
            private List<EasyTweenSubObject> m_SubTweens = null;

            #endregion

            #region Public Properties

            public string tag
            {
                get { return m_Tag; }
                set { m_Tag = value; }
            }
            public GameObject targetGameObject
            {
                get { return m_TargetGameObject; }
                set { m_TargetGameObject = value; }
            }
            public Tween.TweenType tweenType
            {
                get { return m_TweenType; }
                set { m_TweenType = value; }
            }
            public AnimationCurve customCurve
            {
                get { return m_CustomCurve; }
                set { m_CustomCurve = value; }
            }
            public float duration
            {
                get { return m_Duration; }
                set { m_Duration = value; }
            }
            public float delay
            {
                get { return m_Delay; }
                set { m_Delay = value; }
            }
            public bool tweenOnStart
            {
                get { return m_TweenOnStart; }
                set { m_TweenOnStart = value; }
            }
            public bool hasCallback
            {
                get { return m_HasCallback; }
                set { m_HasCallback = value; }
            }
            public GameObject callbackGameObject
            {
                get { return m_CallbackGameObject; }
                set { m_CallbackGameObject = value; }
            }
            public Component callbackComponent
            {
                get { return m_CallbackComponent; }
                set { m_CallbackComponent = value; }
            }
            public string callbackComponentName
            {
                get { return m_CallbackComponentName; }
                set { m_CallbackComponentName = value; }
            }
            public MethodInfo callbackMethodInfo
            {
                get { return m_CallbackMethodInfo; }
                set { m_CallbackMethodInfo = value; }
            }
            public string callbackName
            {
                get { return m_CallbackName; }
                set { m_CallbackName = value; }
            }
            public bool optionsVisible
            {
                get { return m_OptionsVisible; }
                set { m_OptionsVisible = value; }
            }
            public bool subOptionsVisible
            {
                get { return m_SubOptionsVisible; }
                set { m_SubOptionsVisible = value; }
            }
            public List<EasyTweenSubObject> subTweens
            {
                get { return m_SubTweens; }
                set { m_SubTweens = value; }
            }

            #endregion

            #region Constructors

            public EasyTweenObject()
            {
                tag = "New Tween";
                tweenType = MaterialUI.Tween.TweenType.EaseOutQuint;
                duration = 1f;
                callbackComponentName = "--NONE--";
                callbackName = "--NONE--";
                optionsVisible = true;
                subOptionsVisible = true;
                customCurve = new AnimationCurve(new[] { new Keyframe(0f, 0f), new Keyframe(1f, 1f) });
                subTweens = new List<EasyTweenSubObject>() { new EasyTweenSubObject() };
            }

            #endregion
        }

        [Serializable]
        public class EasyTweenSubObject
        {
            #region Fields

            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetComponent")]
            private Component m_TargetComponent;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetComponentName")]
            private string m_TargetComponentName;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetVariableName")]
            private string m_TargetVariableName;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetFieldInfo")]
            private FieldInfo m_TargetFieldInfo;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetPropertyInfo")]
            private PropertyInfo m_TargetPropertyInfo;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("isProperty")]
            private bool m_IsProperty;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("variableType")]
            private string m_VariableType;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetValue")]
            private Vector4 m_TargetValue;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetValueLength")]
            private int m_TargetValueLength;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("modifyParameters")]
            private bool[] m_ModifyParameters;
            [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("currentMaterial")]
            private Material m_CurrentMaterial;

            #endregion

            #region Public Properties

            public Component targetComponent { get => m_TargetComponent; set => m_TargetComponent = value; }
            public string targetComponentName { get => m_TargetComponentName; set => m_TargetComponentName = value; }
            public string targetVariableName { get => m_TargetVariableName; set => m_TargetVariableName = value; }
            public FieldInfo targetFieldInfo { get => m_TargetFieldInfo; set => m_TargetFieldInfo = value; }
            public PropertyInfo targetPropertyInfo { get => m_TargetPropertyInfo; set => m_TargetPropertyInfo = value; }
            public bool isProperty { get => m_IsProperty; set => m_IsProperty = value; }
            public string variableType { get => m_VariableType; set => m_VariableType = value; }
            public Vector4 targetValue { get => m_TargetValue; set => m_TargetValue = value; }
            public int targetValueLength { get => m_TargetValueLength; set => m_TargetValueLength = value; }
            public bool[] modifyParameters { get => m_ModifyParameters; set => m_ModifyParameters = value; }
            public Material currentMaterial { get => m_CurrentMaterial; set => m_CurrentMaterial = value; }

            #endregion

            #region Constructors

            public EasyTweenSubObject()
            {
                modifyParameters = new[] { true, true, true, true };
                targetComponent = null;
                targetComponentName = "--NONE--";
                targetVariableName = "--NONE--";
                targetFieldInfo = null;
                targetPropertyInfo = null;
                isProperty = false;
                variableType = null;
                targetValueLength = 0;
                targetValue = new Vector4();
            }

            #endregion
        }

        #endregion

        #region Private Variables

        [SerializeField]
        private List<EasyTweenObject> m_Tweens = null;

        #endregion

        #region Public Properties

        public List<EasyTweenObject> tweens
        {
            get { return m_Tweens; }
            set { m_Tweens = value; }
        }

        #endregion

        #region Unity Functions

        protected virtual void Start()
        {
            for (int i = 0; i < tweens.Count; i++)
            {
                if (tweens[i].tweenOnStart)
                {
                    Tween(i);
                }
            }
        }

        #endregion

        #region Public Functions

        public void TweenAll()
        {
            for (int i = 0; i < tweens.Count; i++)
            {
                TweenSet(tweens[i]);
            }
        }

        public void Tween(int index)
        {
            TweenSet(tweens[index]);
        }

        public override void Tween(string tag, Action<string> callback)
        {
            var tagTweens = tweens.Where((t) => t != null && string.Equals(t.tag, tag, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            int counter = tagTweens.Length;

            for (int i = 0; i < tagTweens.Length; i++)
            {
                TweenSet(tagTweens[i], () =>
                {
                    counter--;
                    if(callback != null && counter <= 0)
                    {
                        callback(tag);
                    }
                });
            }

            //Request callback when empty amount of tags
            if (callback != null && tagTweens.Length <= 0)
                callback(tag);
        }

        #endregion

        #region Helper Functions

        protected virtual void TweenSet(EasyTweenObject tweenObject, Action callback = null)
        {
            for (int i = 0; i < tweenObject.subTweens.Count; i++)
            {
                EasyTweenSubObject subObject = tweenObject.subTweens[i];

                Action callbackAction = null;
                Action tweenObjactCallback = null;
                if (tweenObject.callbackName != "--NONE--" && tweenObject.hasCallback)
                    tweenObjactCallback = (Action)Delegate.CreateDelegate(typeof(Action), tweenObject.callbackComponent, tweenObject.callbackName);

                if (tweenObjactCallback != null || callback != null)
                    callbackAction = () =>
                    {
                        if (tweenObjactCallback != null)
                            tweenObjactCallback();
                        else if (callback != null)
                            callback();
                    };


                switch (subObject.variableType)
                {
                    case "Int32":
                        int tempInt = Mathf.RoundToInt(subObject.targetValue.x);
                        TweenManager.TweenValue(x => SetField(subObject.targetComponent, subObject.targetVariableName, x), GetValue<int>(subObject.targetComponent, subObject.targetVariableName), tempInt, tweenObject.duration, tweenObject.delay, callbackAction, false, tweenObject.tweenType);
                        break;
                    case "Single":
                        float tempFloat = subObject.targetValue.x;
                        TweenManager.TweenValue(f => SetField(subObject.targetComponent, subObject.targetVariableName, f), GetValue<float>(subObject.targetComponent, subObject.targetVariableName), tempFloat, tweenObject.duration, tweenObject.delay, callbackAction, false, tweenObject.tweenType);
                        break;
                    case "Vector2":
                        Vector2 tempVector2 = new Vector2(subObject.targetValue.x, subObject.targetValue.y);
                        TweenManager.TweenValue(v2 => SetField(subObject.targetComponent, subObject.targetVariableName, v2), GetValue<Vector2>(subObject.targetComponent, subObject.targetVariableName), tempVector2, tweenObject.duration, tweenObject.delay, callbackAction, false, tweenObject.tweenType);
                        break;
                    case "Vector3":
                        Vector3 tempVector3 = new Vector3(subObject.targetValue.x, subObject.targetValue.y, subObject.targetValue.z);
                        TweenManager.TweenValue(v3 => SetField(subObject.targetComponent, subObject.targetVariableName, v3), GetValue<Vector3>(subObject.targetComponent, subObject.targetVariableName), tempVector3, tweenObject.duration, tweenObject.delay, callbackAction, false, tweenObject.tweenType);
                        break;
                    case "Vector4":
                        TweenManager.TweenValue(v4 => SetField(subObject.targetComponent, subObject.targetVariableName, v4), GetValue<Vector4>(subObject.targetComponent, subObject.targetVariableName), subObject.targetValue, tweenObject.duration, tweenObject.delay, callbackAction, false, tweenObject.tweenType);
                        break;
                    case "Rect":
                        Rect tempRect = new Rect(subObject.targetValue.x, subObject.targetValue.y, subObject.targetValue.z, subObject.targetValue.w);
                        TweenManager.TweenValue(r => SetField(subObject.targetComponent, subObject.targetVariableName, r), GetValue<Rect>(subObject.targetComponent, subObject.targetVariableName), tempRect, tweenObject.duration, tweenObject.delay, callbackAction, false, tweenObject.tweenType);
                        break;
                    case "Color":
                        Color tempColor = new Color(subObject.targetValue.x, subObject.targetValue.y, subObject.targetValue.z, subObject.targetValue.w);
                        TweenManager.TweenValue(c => SetField(subObject.targetComponent, subObject.targetVariableName, c), GetValue<Color>(subObject.targetComponent, subObject.targetVariableName), tempColor, tweenObject.duration, tweenObject.delay, callbackAction, false, tweenObject.tweenType);
                        break;
                }
            }
        }

        protected virtual T GetReference<T>(object inObj, string fieldName) where T : class
        {
            return GetField(inObj, fieldName) as T;
        }

        protected virtual T GetValue<T>(object inObj, string fieldName) where T : struct
        {
            return (T)GetField(inObj, fieldName);
        }

        protected virtual void SetField(object inObj, string fieldName, object newValue)
        {
            PropertyInfo propertyInfo = inObj.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);

            if (propertyInfo != null)
            {
                propertyInfo.SetValue(inObj, newValue, null);
            }
            else
            {
                FieldInfo fieldInfo = inObj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);

                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(inObj, newValue);
                }
            }
        }

        protected virtual object GetField(object inObj, string fieldName)
        {
            object ret = null;
            PropertyInfo propertyInfo = inObj.GetType().GetProperty(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (propertyInfo != null)
            {
                ret = propertyInfo.GetValue(inObj, null);
            }
            else
            {
                FieldInfo fieldInfo = inObj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (fieldInfo != null)
                {
                    ret = fieldInfo.GetValue(inObj);
                }
            }

            return ret;
        }

        #endregion
    }
}