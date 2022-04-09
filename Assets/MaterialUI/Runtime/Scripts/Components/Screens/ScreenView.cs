// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaterialUI
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [ExecuteInEditMode]
    [AddComponentMenu("MaterialUI/Screen View", 100)]
    public class ScreenView : UIBehaviour
    {
        #region Helper Classes/Enums

        public enum SlideDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        public enum RippleType
        {
            MousePosition,
            Manual,
            Center,
        }

        public enum Type
        {
            In,
            Out,
            InOut
        }


        [Serializable]
        public class OnScreenTransitionUnityEvent : UnityEvent<int> { }

        #endregion

        #region Private Variables

#if UNITY_EDITOR
        [SerializeField]
        private bool m_AutoTrackScreens = true;
        [SerializeField]
        private bool m_OnlyShowSelectedScreen = false;

        private bool m_ScreensDirty = true;

        private GameObject m_OldSelectionObjects;
#endif
        [SerializeField]
        Transform m_Content = null;
        [SerializeField]
        private List<MaterialScreen> m_MaterialScreens = new List<MaterialScreen>();
        [SerializeField]
        private int m_CurrentScreenIndex = 0;


        //  Transition In
        [SerializeField]
        private bool m_FadeIn = true;
        [SerializeField]
        private Tween.TweenType m_FadeInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_FadeInAlpha = 0;
        [SerializeField]
        private AnimationCurve m_FadeInCustomCurve = null;
        [SerializeField]
        private bool m_ScaleIn = false;
        [SerializeField]
        private Tween.TweenType m_ScaleInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_ScaleInScale = 0;
        [SerializeField]
        private AnimationCurve m_ScaleInCustomCurve = null;
        [SerializeField]
        private bool m_SlideIn = false;
        [SerializeField]
        private Tween.TweenType m_SlideInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private SlideDirection m_SlideInDirection = SlideDirection.Right;
        [SerializeField]
        private bool m_AutoSlideInAmount = true;
        [SerializeField]
        private float m_SlideInAmount = 0;
        [SerializeField]
        private float m_SlideInPercent = 100f;
        [SerializeField]
        private AnimationCurve m_SlideInCustomCurve = null;
        [SerializeField]
        private bool m_RippleIn = false;
        [SerializeField]
        private Tween.TweenType m_RippleInTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private RippleType m_RippleInType = RippleType.MousePosition;
        [SerializeField]
        private Vector2 m_RippleInPosition = Vector2.zero;
        [SerializeField]
        private AnimationCurve m_RippleInCustomCurve = null;

        //  Transition Out
        [SerializeField]
        private bool m_FadeOut = false;
        [SerializeField]
        private Tween.TweenType m_FadeOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_FadeOutAlpha = 0;
        [SerializeField]
        private AnimationCurve m_FadeOutCustomCurve = null;
        [SerializeField]
        private bool m_ScaleOut = false;
        [SerializeField]
        private Tween.TweenType m_ScaleOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private float m_ScaleOutScale = 0;
        [SerializeField]
        private AnimationCurve m_ScaleOutCustomCurve = null;
        [SerializeField]
        private bool m_SlideOut = false;
        [SerializeField]
        private Tween.TweenType m_SlideOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private SlideDirection m_SlideOutDirection = SlideDirection.Left;
        [SerializeField]
        private bool m_AutoSlideOutAmount = true;
        [SerializeField]
        private float m_SlideOutAmount = 0;
        [SerializeField]
        private float m_SlideOutPercent = 100f;
        [SerializeField]
        private AnimationCurve m_SlideOutCustomCurve = null;
        [SerializeField]
        private bool m_RippleOut = false;
        [SerializeField]
        private Tween.TweenType m_RippleOutTweenType = Tween.TweenType.EaseOutQuint;
        [SerializeField]
        private RippleType m_RippleOutType = RippleType.MousePosition;
        [SerializeField]
        private Vector2 m_RippleOutPosition = Vector2.zero;
        [SerializeField]
        private AnimationCurve m_RippleOutCustomCurve = null;
        [SerializeField]
        private float m_TransitionDuration = 0.5f;
        [SerializeField]
        private Type m_TransitionType = Type.In;

        private Canvas m_Canvas;
        private GraphicRaycaster m_GraphicRaycaster;
        //private int m_ScreensTransitioning;

        protected List<MaterialScreen> m_ScreenStack = new List<MaterialScreen>();

        #endregion

        #region Callbacks

        [UnityEngine.Serialization.FormerlySerializedAs("m_OnScreenEndTransition")]
        public OnScreenTransitionUnityEvent onScreenEndTransition = new OnScreenTransitionUnityEvent();
        [UnityEngine.Serialization.FormerlySerializedAs("m_OnScreenBeginTransition")]
        public OnScreenTransitionUnityEvent onScreenBeginTransition = new OnScreenTransitionUnityEvent();

        #endregion

        #region Properties

#if UNITY_EDITOR
        public bool screensDirty
        {
            set { m_ScreensDirty = value; }
        }
#endif

        public Transform content
        {
            get { return m_Content == null && this != null ? this.transform : m_Content; }
            set { m_Content = value; }
        }

        public List<MaterialScreen> materialScreen
        {
            get { return m_MaterialScreens; }
            set { m_MaterialScreens = value; }
        }

        public int currentScreenIndex
        {
            get { return m_CurrentScreenIndex; }
            set
            {
                if (m_CurrentScreenIndex == value)
                    return;

                if (Application.isPlaying)
                    TransitionImmediate(value);
                else
                    m_CurrentScreenIndex = value;
            }
        }

        public MaterialScreen currentScreen
        {
            get
            {
                return m_MaterialScreens != null && m_MaterialScreens.Count > m_CurrentScreenIndex && m_CurrentScreenIndex >= 0 ?
                    m_MaterialScreens[m_CurrentScreenIndex] :
                    null;
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<MaterialScreen> screenStack
        {
            get
            {
                if (m_ScreenStack == null)
                    m_ScreenStack = new List<MaterialScreen>();
                return m_ScreenStack.AsReadOnly();
            }
        }

        public MaterialScreen lastScreen
        {
            get
            {
                MaterialScreen lastScreen = null;
                while (m_ScreenStack.Count > 0)
                {
                    var lastScreenIndex = m_ScreenStack.Count - 1;
                    lastScreen = m_ScreenStack[lastScreenIndex];

                    if (m_MaterialScreens.IndexOf(lastScreen) >= 0)
                    {
                        break;
                    }
                    //Invalid Screen
                    else
                    {
                        lastScreen = null;
                        m_ScreenStack.RemoveAt(lastScreenIndex);
                    }
                }
                return lastScreen;
            }
        }

        public bool fadeIn
        {
            get { return m_FadeIn; }
            set { m_FadeIn = value; }
        }

        public Tween.TweenType fadeInTweenType
        {
            get { return m_FadeInTweenType; }
            set { m_FadeInTweenType = value; }
        }

        public float fadeInAlpha
        {
            get { return m_FadeInAlpha; }
            set { m_FadeInAlpha = value; }
        }

        public AnimationCurve fadeInCustomCurve
        {
            get { return m_FadeInCustomCurve; }
            set { m_FadeInCustomCurve = value; }
        }

        public bool scaleIn
        {
            get { return m_ScaleIn; }
            set { m_ScaleIn = value; }
        }

        public Tween.TweenType scaleInTweenType
        {
            get { return m_ScaleInTweenType; }
            set { m_ScaleInTweenType = value; }
        }

        public float scaleInScale
        {
            get { return m_ScaleInScale; }
            set { m_ScaleInScale = value; }
        }

        public AnimationCurve scaleInCustomCurve
        {
            get { return m_ScaleInCustomCurve; }
            set { m_ScaleInCustomCurve = value; }
        }

        public bool slideIn
        {
            get { return m_SlideIn; }
            set { m_SlideIn = value; }
        }

        public Tween.TweenType slideInTweenType
        {
            get { return m_SlideInTweenType; }
            set { m_SlideInTweenType = value; }
        }

        public SlideDirection slideInDirection
        {
            get { return m_SlideInDirection; }
            set { m_SlideInDirection = value; }
        }

        public bool autoSlideInAmount
        {
            get { return m_AutoSlideInAmount; }
            set { m_AutoSlideInAmount = value; }
        }

        public float slideInAmount
        {
            get { return m_SlideInAmount; }
            set { m_SlideInAmount = value; }
        }

        public float slideInPercent
        {
            get { return m_SlideInPercent; }
            set { m_SlideInPercent = value; }
        }

        public AnimationCurve slideInCustomCurve
        {
            get { return m_SlideInCustomCurve; }
            set { m_SlideInCustomCurve = value; }
        }

        public bool rippleIn
        {
            get { return m_RippleIn; }
            set { m_RippleIn = value; }
        }

        public Tween.TweenType rippleInTweenType
        {
            get { return m_RippleInTweenType; }
            set { m_RippleInTweenType = value; }
        }

        public RippleType rippleInType
        {
            get { return m_RippleInType; }
            set { m_RippleInType = value; }
        }

        public Vector2 rippleInPosition
        {
            get { return m_RippleInPosition; }
            set { m_RippleInPosition = value; }
        }

        public AnimationCurve rippleInCustomCurve
        {
            get { return m_RippleInCustomCurve; }
            set { m_RippleInCustomCurve = value; }
        }

        public bool fadeOut
        {
            get { return m_FadeOut; }
            set { m_FadeOut = value; }
        }

        public Tween.TweenType fadeOutTweenType
        {
            get { return m_FadeOutTweenType; }
            set { m_FadeOutTweenType = value; }
        }

        public float fadeOutAlpha
        {
            get { return m_FadeOutAlpha; }
            set { m_FadeOutAlpha = value; }
        }

        public AnimationCurve fadeOutCustomCurve
        {
            get { return m_FadeOutCustomCurve; }
            set { m_FadeOutCustomCurve = value; }
        }

        public bool scaleOut
        {
            get { return m_ScaleOut; }
            set { m_ScaleOut = value; }
        }

        public Tween.TweenType scaleOutTweenType
        {
            get { return m_ScaleOutTweenType; }
            set { m_ScaleOutTweenType = value; }
        }

        public float scaleOutScale
        {
            get { return m_ScaleOutScale; }
            set { m_ScaleOutScale = value; }
        }

        public AnimationCurve scaleOutCustomCurve
        {
            get { return m_ScaleOutCustomCurve; }
            set { m_ScaleOutCustomCurve = value; }
        }

        public bool slideOut
        {
            get { return m_SlideOut; }
            set { m_SlideOut = value; }
        }

        public Tween.TweenType slideOutTweenType
        {
            get { return m_SlideOutTweenType; }
            set { m_SlideOutTweenType = value; }
        }

        public SlideDirection slideOutDirection
        {
            get { return m_SlideOutDirection; }
            set { m_SlideOutDirection = value; }
        }

        public bool autoSlideOutAmount
        {
            get { return m_AutoSlideOutAmount; }
            set { m_AutoSlideOutAmount = value; }
        }

        public float slideOutAmount
        {
            get { return m_SlideOutAmount; }
            set { m_SlideOutAmount = value; }
        }

        public float slideOutPercent
        {
            get { return m_SlideOutPercent; }
            set { m_SlideOutPercent = value; }
        }

        public AnimationCurve slideOutCustomCurve
        {
            get { return m_SlideOutCustomCurve; }
            set { m_SlideOutCustomCurve = value; }
        }

        public bool rippleOut
        {
            get { return m_RippleOut; }
            set { m_RippleOut = value; }
        }

        public Tween.TweenType rippleOutTweenType
        {
            get { return m_RippleOutTweenType; }
            set { m_RippleOutTweenType = value; }
        }

        public RippleType rippleOutType
        {
            get { return m_RippleOutType; }
            set { m_RippleOutType = value; }
        }

        public Vector2 rippleOutPosition
        {
            get { return m_RippleOutPosition; }
            set { m_RippleOutPosition = value; }
        }

        public AnimationCurve rippleOutCustomCurve
        {
            get { return m_RippleOutCustomCurve; }
            set { m_RippleOutCustomCurve = value; }
        }

        public float transitionDuration
        {
            get { return m_TransitionDuration; }
            set { m_TransitionDuration = value; }
        }

        public Type transitionType
        {
            get { return m_TransitionType; }
            set { m_TransitionType = value; }
        }

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Application.isPlaying)
            {
                RemoveInvalidScreens();
            }
#if UNITY_EDITOR
            Selection.selectionChanged -= OnValidate;
            Selection.selectionChanged += OnValidate;
#endif
        }

        protected override void Start()
        {
            base.Start();
            if (Application.isPlaying)
            {
                if (m_MaterialScreens.Count > 0)
                {
                    for (int i = 0; i < materialScreen.Count; i++)
                    {
                        if (i != m_CurrentScreenIndex)
                        {
                            materialScreen[i].gameObject.SetActive(!materialScreen[i].disableWhenNotVisible);
                        }
                    }

                    //m_MaterialScreens[m_CurrentScreenIndex].gameObject.SetActive(true);
                    //m_MaterialScreens[m_CurrentScreenIndex].rectTransform.SetAsLastSibling();
                    var currentScreen = m_MaterialScreens[m_CurrentScreenIndex];
                    m_CurrentScreenIndex = -1;
                    TransitionImmediate(currentScreen);
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
#if UNITY_EDITOR
            Selection.selectionChanged -= OnValidate;
            CancelInvoke();
#endif
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (enabled && gameObject.activeInHierarchy)
                Invoke("TrackScreens", 0.05f);
        }
#endif

        #endregion

        #region ScreenStack Functions

        public virtual MaterialScreen PushToScreenStack(string screenName)
        {
            var materialScreen = GetScreenWithName(screenName);
            return PushToScreenStack(materialScreen) ? materialScreen : null;
        }

        public virtual MaterialScreen PushToScreenStack(int screenIndex)
        {
            var materialScreen = m_MaterialScreens.Count > screenIndex && screenIndex >= 0 ? m_MaterialScreens[screenIndex] : null;
            return PushToScreenStack(materialScreen) ? materialScreen : null;
        }

        public virtual bool PushToScreenStack(MaterialScreen screen)
        {
            //Only accept screens of this ScreenView
            if (screen != null && m_MaterialScreens.IndexOf(screen) >= 0)
            {
                TryRemoveFromScreenStack(screen);
                m_ScreenStack.Add(screen);
                return true;
            }
            return false;
        }

        public virtual MaterialScreen PopFromScreenStack()
        {
            MaterialScreen screen = null;
            while (m_ScreenStack.Count > 0)
            {
                //Try remove last screen (do it until remove)
                var screenIndex = m_ScreenStack.Count - 1;
                screen = m_ScreenStack[screenIndex];
                //We removed the last screen
                if (m_MaterialScreens.IndexOf(screen) >= 0 && TryRemoveFromScreenStack(screen))
                {
                    break;
                }
                //Invalid Screen index, remove it and try again
                else
                {
                    screen = null;
                    m_ScreenStack.RemoveAt(screenIndex);
                }
            }
            return screen;
        }

        public bool TryRemoveFromScreenStack(string screenName, out MaterialScreen materialScreen)
        {
            materialScreen = GetScreenWithName(screenName);
            var result = TryRemoveFromScreenStack(materialScreen);
            if (!result)
                materialScreen = null;

            return result;
        }

        public bool TryRemoveFromScreenStack(int screenIndex, out MaterialScreen materialScreen)
        {
            materialScreen = m_MaterialScreens.Count > screenIndex && screenIndex >= 0 ? m_MaterialScreens[screenIndex] : null;
            var result = TryRemoveFromScreenStack(materialScreen);
            if (!result)
                materialScreen = null;

            return result;
        }

        public virtual bool TryRemoveFromScreenStack(MaterialScreen screen)
        {
            if (screen != null)
            {
                var index = m_ScreenStack.IndexOf(screen);
                if (index >= 0)
                {
                    m_ScreenStack.RemoveAt(index);
                    return true;
                }
            }
            return false;
        }

        public void RemoveFromScreenStack(string screenName)
        {
            MaterialScreen dummy;
            TryRemoveFromScreenStack(screenName, out dummy);
        }

        public void RemoveFromScreenStack(int screenIndex)
        {
            MaterialScreen dummy;
            TryRemoveFromScreenStack(screenIndex, out dummy);
        }

        public virtual void RemoveFromScreenStack(MaterialScreen screen)
        {
            TryRemoveFromScreenStack(screen);
        }

        public virtual void ClearScreenStack()
        {
            m_ScreenStack.Clear();
        }

        public virtual void RevertStackToScreenWithIndex(int screenIndex)
        {
            var materialScreen = m_MaterialScreens.Count > screenIndex && screenIndex >= 0 ? m_MaterialScreens[screenIndex] : null;
            RevertStackToScreen(materialScreen);
        }

        public virtual void RevertStackToScreenWithName(string screenName)
        {
            var materialScreen = GetScreenWithName(screenName);
            RevertStackToScreen(materialScreen);
        }

        public virtual void RevertStackToIndex(int stackIndex)
        {
            var materialScreen = stackIndex < m_ScreenStack.Count && stackIndex >= 0 ? m_ScreenStack[stackIndex] : null;
            RevertStackToScreen(materialScreen);
        }

        public virtual void RevertStackToScreen(MaterialScreen materialScreen)
        {
            if (materialScreen != null)
            {
                var indexToRevert = m_ScreenStack.IndexOf(materialScreen);
                while (indexToRevert >= 0 && indexToRevert < m_ScreenStack.Count - 1)
                {
                    m_ScreenStack.RemoveAt(m_ScreenStack.Count - 1);
                }
            }
        }

        #endregion

        #region Public Helper Functions


        /// <summary>
        /// A valid screens is enabled one in materialScreen list
        /// </summary>
        /// <returns></returns>
        public virtual MaterialScreen[] GetValidScreens()
        {
            var validScreens = m_MaterialScreens != null ? m_MaterialScreens.FindAll((a) => a != null && a.enabled) : null;

            return validScreens != null ? validScreens.ToArray() : new MaterialScreen[0];
        }

        public virtual MaterialScreen GetScreenWithName(string screenPrefabPath)
        {
            MaterialScreen screenWithSameName = null;
            var partialNamePath = screenPrefabPath != null ? System.IO.Path.GetFileName(screenPrefabPath) : string.Empty;
            foreach (var screen in materialScreen)
            {
                var fullFind = screen.name == screenPrefabPath;
                var partialFind = !fullFind && screenWithSameName == null && screenPrefabPath != null && screen.name == partialNamePath;
                if (fullFind || partialFind)
                {
                    screenWithSameName = screen;

                    //We only stop match if screen.name exactly equals screenPrefabPath
                    if (fullFind)
                        break;
                }
            }
            return screenWithSameName;
        }

        public virtual void BackToScreen(int screenIndex)
        {
            BackToScreen(screenIndex, Type.Out, true);
        }

        public virtual void BackToScreen(int screenIndex, Type transitionType)
        {
            BackToScreen(screenIndex, transitionType, true);
        }

        public virtual MaterialScreen BackToScreen(int screenIndex, Type transitionType, bool canAnimate)
        {
            var materialScreen = m_MaterialScreens.Count > screenIndex && screenIndex >= 0 ? m_MaterialScreens[screenIndex] : null;
            BackToScreen(materialScreen, transitionType, canAnimate);
            return materialScreen;
        }

        public virtual void BackToScreen(string screenName)
        {
            BackToScreen(screenName, Type.Out, true);
        }

        public virtual void BackToScreen(string screenName, Type transitionType)
        {
            BackToScreen(screenName, transitionType, true);
        }

        public virtual MaterialScreen BackToScreen(string screenName, Type transitionType, bool canAnimate)
        {
            var materialScreen = GetScreenWithName(screenName);
            BackToScreen(materialScreen, transitionType, canAnimate);
            return materialScreen;
        }

        public virtual void BackToScreen(MaterialScreen screen)
        {
            BackToScreen(screen, Type.Out, true);
        }

        public virtual void BackToScreen(MaterialScreen screen, Type transitionType)
        {
            BackToScreen(screen, transitionType, true);
        }

        /// <summary>
        /// Special Function to Rollback in Screenstack 
        /// </summary>
        public virtual void BackToScreen(MaterialScreen screen, Type transitionType, bool animate)
        {
            var stackIndex = screen != null ? m_ScreenStack.IndexOf(screen) : -1;

            //Revert all screens before target screen in stack
            var processingScreenStack = new List<MaterialScreen>(m_ScreenStack);
            for (int i = processingScreenStack.Count - 1; i > stackIndex; i--)
            {
                if (processingScreenStack[i] != null && processingScreenStack[i] != currentScreen)
                {
                    processingScreenStack[i].TransitionOut();
                    processingScreenStack[i].Interrupt(true);
                    TryRemoveFromScreenStack(processingScreenStack[i]);
                }
            }

            //Back to screen
            if (screen != null)
            {
                var currentScreenIndex = m_CurrentScreenIndex;
                var screenIndex = m_MaterialScreens.IndexOf(screen);
                Transition(screenIndex, transitionType, animate);
                //We dont want to stack the last screen
                RemoveFromScreenStack(currentScreenIndex);
            }
        }

        public virtual void Back()
        {
            Back(Type.Out);
        }

        public virtual void Back(Type transitionType)
        {
            Back(transitionType, true);
        }

        public virtual void Back(Type transitionType, bool animate)
        {
            var currentScreenIndex = m_CurrentScreenIndex;
            var lastScreen = PopFromScreenStack();
            var lastScreenIndex = m_MaterialScreens.IndexOf(lastScreen);
            Transition(lastScreenIndex, transitionType, animate);
            //We dont want to stack the last screen
            RemoveFromScreenStack(currentScreenIndex);
        }

        public virtual void Transition(string screenName)
        {
            Transition(screenName, transitionType);
        }

        public virtual void Transition(string screenName, Type transitionType, bool animate = true)
        {
            var materialScreen = GetScreenWithName(screenName);
            Transition(materialScreen, transitionType, animate);
        }

        public virtual void Transition(MaterialScreen screen)
        {
            Transition(screen, transitionType);
        }

        public virtual void Transition(MaterialScreen screen, Type transitionType, bool animate = true)
        {
            var screenIndex = m_MaterialScreens.IndexOf(screen);
            if (screenIndex >= 0)
                Transition(screenIndex, transitionType, animate);
        }

        public virtual void Transition(int screenIndex)
        {
            Transition(screenIndex, transitionType);
        }

        public virtual void Transition(int screenIndex, Type transitionType, bool animate = true)
        {
            if (0 > screenIndex || screenIndex >= materialScreen.Count || screenIndex == currentScreenIndex) return;

            var lastScreenIndex = m_CurrentScreenIndex;
            PushToScreenStack(m_CurrentScreenIndex);
            m_CurrentScreenIndex = screenIndex;
            RemoveFromScreenStack(screenIndex);

            //Check if screens to active/deactive is valid
            var isValidLastScreen = lastScreenIndex >= 0 && lastScreenIndex < m_MaterialScreens.Count && m_MaterialScreens[lastScreenIndex] != null;
            var isValidCurrentScreen = m_CurrentScreenIndex >= 0 && m_CurrentScreenIndex < m_MaterialScreens.Count && m_MaterialScreens[m_CurrentScreenIndex] != null;

            if (isValidLastScreen)
                m_MaterialScreens[lastScreenIndex].InterruptAnimation();
            if (isValidCurrentScreen)
                m_MaterialScreens[m_CurrentScreenIndex].InterruptAnimation();

            if (transitionType == Type.In)
            {
                if (isValidCurrentScreen)
                {
                    m_MaterialScreens[m_CurrentScreenIndex].rectTransform.SetAsLastSibling();
                    m_MaterialScreens[m_CurrentScreenIndex].TransitionIn();

                    if (!animate)
                        m_MaterialScreens[m_CurrentScreenIndex].Interrupt();
                }
                if (isValidLastScreen)
                {
                    m_MaterialScreens[lastScreenIndex].TransitionOutImmediate();
                }
            }
            else if (transitionType == Type.Out)
            {
                if (isValidCurrentScreen)
                {
                    m_MaterialScreens[m_CurrentScreenIndex].rectTransform.SetAsLastSibling();
                    m_MaterialScreens[m_CurrentScreenIndex].gameObject.SetActive(true);
                }

                if (isValidLastScreen)
                {
                    m_MaterialScreens[lastScreenIndex].rectTransform.SetAsLastSibling();
                    m_MaterialScreens[lastScreenIndex].TransitionOut();

                    if (!animate)
                        m_MaterialScreens[lastScreenIndex].Interrupt(true);
                }
            }
            else if (transitionType == Type.InOut)
            {
                if (isValidCurrentScreen)
                {
                    m_MaterialScreens[m_CurrentScreenIndex].rectTransform.SetAsLastSibling();
                    m_MaterialScreens[m_CurrentScreenIndex].TransitionIn();

                    if (!animate)
                        m_MaterialScreens[m_CurrentScreenIndex].Interrupt();
                }
                if (isValidLastScreen)
                {
                    m_MaterialScreens[lastScreenIndex].TransitionOut();

                    if (!animate)
                        m_MaterialScreens[lastScreenIndex].Interrupt(true);
                }
            }

            //m_ScreensTransitioning += 2;

            onScreenBeginTransition.InvokeIfNotNull(screenIndex);
        }

        public virtual void TransitionImmediate(int screenIndex)
        {
            Transition(screenIndex, transitionType, false);
        }

        public virtual void TransitionImmediate(int screenIndex, Type transitionType)
        {
            Transition(screenIndex, transitionType, false);
        }

        public virtual void TransitionImmediate(string screenName)
        {
            Transition(screenName, transitionType, false);
        }

        public virtual void TransitionImmediate(string screenName, Type transitionType)
        {
            Transition(screenName, transitionType, false);
        }

        public virtual void TransitionImmediate(MaterialScreen screen)
        {
            TransitionImmediate(screen, transitionType);
        }

        public virtual void TransitionImmediate(MaterialScreen screen, Type transitionType)
        {
            var screenIndex = m_MaterialScreens.IndexOf(screen);
            if (screenIndex >= 0)
                Transition(screenIndex, transitionType, false);
        }

        public virtual void NextScreen(bool wrap = true)
        {
            var validScreenIndex = GetNextScreenIndex(wrap);
            if (validScreenIndex >= 0)
            {
                Transition(validScreenIndex);
            }
        }

        public virtual void PreviousScreen(bool wrap = true)
        {
            var validScreenIndex = GetPreviousScreenIndex(wrap);
            if (validScreenIndex >= 0)
            {
                Transition(validScreenIndex);
            }
        }

        public virtual void NextValidScreen(bool wrap = true)
        {
            var validScreenIndex = GetNextValidScreenIndex(wrap);
            if (validScreenIndex >= 0)
            {
                Transition(validScreenIndex);
            }
        }

        public virtual void PreviousValidScreen(bool wrap = true)
        {
            var validScreenIndex = GetPreviousValidScreenIndex(wrap);
            if (validScreenIndex >= 0)
            {
                Transition(validScreenIndex);
            }
        }

        public virtual int GetNextScreenIndex(bool wrap = true)
        {
            return GetNextScreenIndexInternal(wrap, false);
        }

        public virtual int GetNextValidScreenIndex(bool wrap = true)
        {
            return GetNextScreenIndexInternal(wrap, true);
        }

        public virtual int GetPreviousScreenIndex(bool wrap = true)
        {
            return GetPreviousScreenIndexInternal(wrap, false);
        }

        public virtual int GetPreviousValidScreenIndex(bool wrap = true)
        {
            return GetPreviousScreenIndexInternal(wrap, true);
        }

        public virtual void RemoveInvalidScreens()
        {
            RemoveInvalidScreens(false);
        }

        public virtual void RemoveInvalidScreens(bool removeDisabledScreens)
        {
            //Fix Material Screens
            if (m_MaterialScreens == null)
                m_MaterialScreens = new List<MaterialScreen>();

            for (int i = 0; i < m_MaterialScreens.Count; i++)
            {
                if (m_MaterialScreens[i] == null || (removeDisabledScreens && !m_MaterialScreens[i].enabled))
                {
                    m_MaterialScreens.RemoveAt(i);

                    //Prevent error while removing screens
                    if (m_CurrentScreenIndex > i)
                        m_CurrentScreenIndex--;
                    if (m_CurrentScreenIndex == i)
                        m_CurrentScreenIndex = -1;

                    i--;
                }
            }

            m_CurrentScreenIndex = Mathf.Clamp(m_CurrentScreenIndex, 0, Math.Max(0, m_MaterialScreens.Count - 1));
        }

        protected virtual int GetNextScreenIndexInternal(bool wrap, bool checkIfIsValid)
        {
            int defaultWrapScreenIndex = 0;
            var validIndex = -1;

            if (currentScreenIndex < materialScreen.Count - 1 && currentScreenIndex >= 0)
            {
                validIndex = currentScreenIndex + 1;
            }
            else if (wrap)
            {
                validIndex = defaultWrapScreenIndex;
            }

            if (checkIfIsValid)
            {
                while (validIndex >= 0 && validIndex < materialScreen.Count && (materialScreen == null || !materialScreen[validIndex].enabled))
                {
                    validIndex++;
                }
            }

            if (validIndex < 0 || validIndex >= materialScreen.Count)
                validIndex = wrap ? defaultWrapScreenIndex : -1;

            return validIndex;
        }

        protected virtual int GetPreviousScreenIndexInternal(bool wrap, bool checkIfIsValid)
        {
            int defaultWrapScreenIndex = materialScreen.Count - 1;
            var validIndex = -1;

            if (currentScreenIndex >= 1)
            {
                validIndex = currentScreenIndex - 1;
            }
            else if (wrap)
            {
                validIndex = defaultWrapScreenIndex;
            }

            if (checkIfIsValid)
            {
                while (validIndex >= 0 && validIndex < materialScreen.Count && (materialScreen == null || !materialScreen[validIndex].enabled))
                {
                    validIndex--;
                }
            }

            if (validIndex < 0 || validIndex >= materialScreen.Count)
                validIndex = wrap ? defaultWrapScreenIndex : -1;

            return validIndex;
        }

        #endregion

        #region Receivers

        protected internal void OnScreenEndTransition(int screenIndex)
        {
            if (screenIndex == m_CurrentScreenIndex && m_MaterialScreens != null)
            {
                for (int i = 0; i < m_MaterialScreens.Count; i++)
                {
                    if (i != m_CurrentScreenIndex)
                    {
                        m_MaterialScreens[i].gameObject.SetActive(!materialScreen[i].disableWhenNotVisible);
                    }
                }
            }
            onScreenEndTransition.InvokeIfNotNull(screenIndex);
        }

        #endregion

        #region Editor Only Functions

#if UNITY_EDITOR
        public void TrackScreens()
        {
            if (IsDestroyed() || Application.isPlaying) return;

            if (m_AutoTrackScreens && enabled && gameObject.activeInHierarchy)
            {
                MaterialScreen[] tempMaterialScreens = GetComponentsInChildren<MaterialScreen>(true);

                List<MaterialScreen> ownedTempScreens = new List<MaterialScreen>();

                for (int i = 0; i < tempMaterialScreens.Length; i++)
                {
                    if (tempMaterialScreens[i].transform.parent == content)
                    {
                        ownedTempScreens.Add(tempMaterialScreens[i]);
                    }
                }

                materialScreen = new List<MaterialScreen>(new MaterialScreen[ownedTempScreens.Count]);

                for (int i = 0; i < ownedTempScreens.Count; i++)
                {
                    materialScreen[i] = ownedTempScreens[i];
                }
            }

            if (m_OldSelectionObjects != Selection.activeGameObject)
            {
                m_OldSelectionObjects = Selection.activeGameObject;
                m_ScreensDirty = true;
            }

            if (m_MaterialScreens.Count > 0 && m_ScreensDirty)
            {
                m_ScreensDirty = false;

                bool screenSelected = false;

                if (m_OnlyShowSelectedScreen)
                {
                    for (int i = 0; i < materialScreen.Count; i++)
                    {
                        RectTransform[] children = materialScreen[i].GetComponentsInChildren<RectTransform>(true);

                        bool objectSelected = false;

                        for (int j = 0; j < children.Length; j++)
                        {
                            if (Selection.Contains(children[j].gameObject))
                            {
                                materialScreen[i].gameObject.SetActive(true);
                                //SetActiveDelayed(materialScreen[i].gameObject, true);
                                screenSelected = true;
                                objectSelected = true;
                            }
                        }
                        if (!objectSelected)
                        {
                            materialScreen[i].gameObject.SetActive(false);
                            //SetActiveDelayed(materialScreen[i].gameObject, false);
                        }
                    }

                    if (!screenSelected && !m_MaterialScreens[m_CurrentScreenIndex].gameObject.activeSelf)
                    {
                        m_MaterialScreens[m_CurrentScreenIndex].gameObject.SetActive(true);
                        //SetActiveDelayed(m_MaterialScreens[m_CurrentScreenIndex].gameObject, true);
                    }
                }
            }
            if (m_CurrentScreenIndex < 0)
            {
                m_CurrentScreenIndex = 0;
            }
            else if (m_MaterialScreens.Count > 0 && m_CurrentScreenIndex >= m_MaterialScreens.Count)
            {
                m_CurrentScreenIndex = m_MaterialScreens.Count - 1;
            }
        }
#endif

        #endregion
    }
}
