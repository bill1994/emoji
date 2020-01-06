﻿//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using UnityEngine;

namespace MaterialUI
{
    /// <summary>
    /// Singleton component that handles the creation of Dialogs.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [AddComponentMenu("MaterialUI/Managers/Dialog Manager")]
    public class DialogManager : MonoBehaviour
    {
        #region Instance

        /// <summary>
        /// The instance in the scene.
        /// </summary>
        private static DialogManager m_Instance;
        /// <summary>
        /// The instance in the scene.
        /// If null, automatically creates one in the scene.
        /// </summary>
        public static DialogManager instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new GameObject("DialogManager").AddComponent<DialogManager>();
                    m_Instance.InitDialogSystem();
                }

                return m_Instance;
            }
        }

        #endregion

        #region Canvas

        /// <summary>
        /// The parent canvas.
        /// </summary>
        [SerializeField]
        private Canvas m_ParentCanvas = null;

        /// <summary>
        /// The rect transform of the manager.
        /// </summary>
        private RectTransform m_RectTransform;
        /// <summary>
        /// The rect transform of the manager.
        /// If null, automatically gets the attached RectTransform.
        /// </summary>
        public static RectTransform rectTransform
        {
            get
            {
                if (instance.m_RectTransform == null && m_Instance.m_ParentCanvas != null)
                {
                    instance.m_RectTransform = m_Instance.m_ParentCanvas.transform as RectTransform;
                }

                return instance.m_RectTransform;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            if (!m_Instance)
            {
                m_Instance = this;
                m_Instance.InitDialogSystem();
            }
            else if (m_Instance != this)
            {
                //Debug.LogWarning("More than one DialogManager exist in the scene, destroying one.");
                Destroy(gameObject);
                return;
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_Instance == this)
                m_Instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            if (m_Instance == this)
                m_Instance = null;
        }

        #endregion

        #region Init Functions

        private void InitDialogSystem()
        {
            //m_RectTransform = gameObject.GetAddComponent<RectTransform>();

            if (m_ParentCanvas == null)
                m_ParentCanvas = FindObjectOfType<Canvas>().transform.GetRootCanvas();

            /*if (m_ParentCanvas != null)
            {
                CanvasSafeArea safeArea = m_ParentCanvas.GetComponent<CanvasSafeArea>();
                transform.SetParent(safeArea != null && safeArea.Content != null? safeArea.Content : m_ParentCanvas.transform, false);
            }*/
            //transform.localScale = Vector3.one;

            /*rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;*/
        }

        #endregion

        #region Alert

        public static void ShowAlertAsync(string bodyText, Action onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, System.Action<DialogAlert> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogAlert> initialize = (DialogAlert dialog) =>
            {
                dialog.Initialize(bodyText, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogAlert>(PrefabManager.ResourcePrefabs.dialogAlert.Name, initialize, progressIndicator); 
        }

        /// <summary>
        /// Shows an alert dialog with an optional title, optional icon, and optional body text.
        /// <para></para>
        /// For more customizability, use <see cref="CreateAlert"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogAlert ShowAlert(string bodyText, string titleText, ImageData icon)
        {
            return ShowAlert(bodyText, null, "OK", titleText, icon);
        }

        /// <summary>
        /// Shows an alert dialog with an optional title, optional icon, optional body text and an optional button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateAlert"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogAlert ShowAlert(string bodyText, Action onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
        {
            return ShowAlert(bodyText, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, null, null);
        }

        /// <summary>
        /// Shows an alert dialog with an optional title, optional icon, optional body text and 2 optional buttons.
        /// <para></para>
        /// For more customizability, use <see cref="CreateAlert"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="onDismissiveButtonClicked">Called when the dismissive button is clicked.</param>
        /// <param name="dismissiveButtonText">The dismissive button text. Make null for no dismissive button.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogAlert ShowAlert(string bodyText, Action onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            DialogAlert dialog = CreateAlert();
            dialog.Initialize(bodyText, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            dialog.Show();
            return dialog;
        }

        /// <summary>
        /// Creates an alert dialog that can be modified or stored before showing.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowAlert(string,Action,string,string,ImageData,Action,string)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogAlert CreateAlert()
        {
            DialogAlert dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogAlert, instance.transform).GetComponent<DialogAlert>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Modal

        /// <summary>
        /// Shows a modal circular progress dialog.
        /// <para></para>
        /// For more customizability, use <see cref="CreateProgressLinear"/>.
        /// </summary>
        /// <param name="startStationaryAtZero">Should the progress begin at zero and non-animated?</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogProgress ShowProgressModalCircular(bool startStationaryAtZero = false)
        {
            DialogProgress dialog = CreateProgressModalCircular();
            dialog.Initialize(null, null, null, startStationaryAtZero);
            dialog.Show();

            return dialog;
        }

        /// <summary>
        /// Creates a modal circular progress dialog.
        /// <para></para>
        /// Before calling <see cref="DialogProgress.Show"/>, call <see cref="DialogProgress.Initialize(string,string,ImageData,bool)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowProgressLinear(string,string,ImageData,bool)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogProgress CreateProgressModalCircular()
        {
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogModalCircleProgress, instance.transform).GetComponent<DialogProgress>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            return dialog;
        }

        /// <summary>
        /// Shows a linear progress dialog with an optional body text, and a required progress indicator.
        /// <para></para>
        /// For more customizability, use <see cref="CreateProgressLinear"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogProgress ShowProgressLinear(string bodyText)
        {
            return ShowProgressLinear(bodyText, null, null, false);
        }

        /// <summary>
        /// Shows a linear progress dialog with an optional title, optional icon, optional body text, and a required progress indicator.
        /// <para></para>
        /// For more customizability, use <see cref="CreateProgressLinear"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="startStationaryAtZero">Should the progress begin at zero and non-animated?</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogProgress ShowProgressLinear(string bodyText, string titleText, ImageData icon, bool startStationaryAtZero = false)
        {
            DialogProgress dialog = CreateProgressLinear();
            dialog.Initialize(bodyText, titleText, icon, startStationaryAtZero);
            dialog.ShowModal();
            return dialog;
        }

        /// <summary>
        /// Creates a linear progress dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogProgress.Show"/>, call <see cref="DialogProgress.Initialize(string,string,ImageData,bool)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowProgressLinear(string,string,ImageData,bool)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogProgress CreateProgressLinear()
        {
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogProgress, instance.transform).GetComponent<DialogProgress>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            dialog.SetupIndicator(true);
            //dialog.Initialize();
            return dialog;
        }

        /// <summary>
        /// Shows a circular progress dialog with an optional body text, and a required progress indicator.
        /// <para></para>
        /// For more customizability, use <see cref="CreateProgressCircular"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogProgress ShowProgressCircular(string bodyText)
        {
            return ShowProgressCircular(bodyText, null, null, false);
        }

        /// <summary>
        /// Shows a circular progress dialog with an optional title, optional icon, optional body text, and a required progress indicator.
        /// <para></para>
        /// For more customizability, use <see cref="CreateProgressCircular"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="startStationaryAtZero">Should the progress begin at zero and non-animated?</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogProgress ShowProgressCircular(string bodyText, string titleText, ImageData icon, bool startStationaryAtZero = false)
        {
            DialogProgress dialog = CreateProgressCircular();
            dialog.Initialize(bodyText, titleText, icon, startStationaryAtZero);
            dialog.ShowModal();
            return dialog;
        }

        /// <summary>
        /// Creates a circular progress dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogProgress.Show"/>, call <see cref="DialogProgress.Initialize(string,string,ImageData,bool)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowProgressCircular(string,string,ImageData,bool)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogProgress CreateProgressCircular()
        {
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogProgress, instance.transform).GetComponent<DialogProgress>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            dialog.SetupIndicator(false);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Simples List

        /// <summary>
        /// Shows an simple list dialog with a a required scrollable option list (label-only).
        /// <para></para>
        /// For more customizability, use <see cref="CreateSimpleList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onItemClick">Called when an option is selected.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogSimpleList ShowSimpleList(string[] options, Action<int> onItemClick)
        {
            return ShowSimpleList(options, onItemClick, null, null);
        }

        /// <summary>
        /// Shows an simple list dialog with an optional title, optional icon, and a required scrollable option list (label-only).
        /// <para></para>
        /// For more customizability, use <see cref="CreateSimpleList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onItemClick">Called when an option is selected.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogSimpleList ShowSimpleList(string[] options, Action<int> onItemClick, string titleText, ImageData icon)
        {
            OptionDataList optionDataList = new OptionDataList();

            for (int i = 0; i < options.Length; i++)
            {
                OptionData optionData = new OptionData(options[i], null);
                optionDataList.options.Add(optionData);
            }

            return ShowSimpleList(optionDataList, onItemClick, titleText, icon);
        }

        /// <summary>
        /// Shows an simple list dialog with a required scrollable option list.
        /// <para></para>
        /// For more customizability, use <see cref="CreateSimpleList"/>.
        /// </summary>
        /// <param name="optionDataList">The data to use for the option list.</param>
        /// <param name="onItemClick">Called when an option is selected.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogSimpleList ShowSimpleList(OptionDataList optionDataList, Action<int> onItemClick)
        {
            return ShowSimpleList(optionDataList, onItemClick, null, null);
        }

        /// <summary>
        /// Shows an simple list dialog with an optional title, optional icon, and a required scrollable option list.
        /// <para></para>
        /// For more customizability, use <see cref="CreateSimpleList"/>.
        /// </summary>
        /// <param name="optionDataList">The data to use for the option list.</param>
        /// <param name="onItemClick">Called when an option is selected.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogSimpleList ShowSimpleList(OptionDataList optionDataList, Action<int> onItemClick, string titleText, ImageData icon)
        {
            DialogSimpleList dialog = CreateSimpleList();
            dialog.Initialize(optionDataList, onItemClick, titleText, icon);
            dialog.Show();
            return dialog;
        }

        /// <summary>
        /// Creates a simple list dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogSimpleList.Show"/>, call <see cref="DialogSimpleList.Initialize(OptionDataList,Action{int},string,ImageData)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowSimpleList(OptionDataList,Action{int},string,ImageData)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogSimpleList CreateSimpleList()
        {
            DialogSimpleList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogSimpleList, instance.transform).GetComponent<DialogSimpleList>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Checkbox List

        /// <summary>
        /// Shows a checkbox list dialog with a required scrollable checkbox list.
        /// <para></para>
        /// For more customizability, use <see cref="CreateCheckboxList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogCheckboxList ShowCheckboxList(string[] options, Action<bool[]> onAffirmativeButtonClicked, string affirmativeButtonText = "OK")
        {
            return ShowCheckboxList(options, onAffirmativeButtonClicked, affirmativeButtonText, null, null);
        }

        /// <summary>
        /// Shows a checkbox list dialog with an optional title, optional icon, and a required scrollable checkbox list.
        /// <para></para>
        /// For more customizability, use <see cref="CreateCheckboxList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogCheckboxList ShowCheckboxList(string[] options, Action<bool[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
        {
            return ShowCheckboxList(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, null, null);
        }

        /// <summary>
        /// Shows a checkbox list dialog with an optional title, optional icon, a required scrollable checkbox list, a required button, and an optional button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateCheckboxList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="onDismissiveButtonClicked">Called when the dismissive button is clicked.</param>
        /// <param name="dismissiveButtonText">The dismissive button text. Make null for no dismissive button.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogCheckboxList ShowCheckboxList(string[] options, Action<bool[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            DialogCheckboxList dialog = CreateCheckboxList();
            dialog.Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            dialog.Show();
            return dialog;
        }

        /// <summary>
        /// Creates a checkbox list dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogCheckboxList.Show"/>, call <see cref="DialogCheckboxList.Initialize(string[],Action{bool[]},string,string,ImageData,Action,string)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowCheckboxList(string[],Action{bool[]},string,string,ImageData,Action,string)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogCheckboxList CreateCheckboxList()
        {
            DialogCheckboxList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogCheckboxList, instance.transform).GetComponent<DialogCheckboxList>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Radio List

        /// <summary>
        /// Shows a radiobutton list dialog with a required scrollable radiobutton list, and a required button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateRadioList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText = "OK")
        {
            return ShowRadioList(options, onAffirmativeButtonClicked, affirmativeButtonText, 0);
        }

        /// <summary>
        /// Shows a radiobutton list dialog with a required scrollable radiobutton list, and a required button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateRadioList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="selectedIndexStart">The index of the option that will be selected when the dialog is shown.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, int selectedIndexStart)
        {
            return ShowRadioList(options, onAffirmativeButtonClicked, affirmativeButtonText, null, null, selectedIndexStart);
        }

        /// <summary>
        /// Shows a radiobutton list dialog with an optional title, optional icon, a required scrollable radiobutton list, a required button, and an optional button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateRadioList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
        {
            return ShowRadioList(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, null, null, 0);
        }

        /// <summary>
        /// Shows a radiobutton list dialog with an optional title, optional icon, a required scrollable radiobutton list, and a required button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateRadioList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="selectedIndexStart">The index of the option that will be selected when the dialog is shown.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, int selectedIndexStart)
        {
            return ShowRadioList(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, null, null, selectedIndexStart);
        }

        /// <summary>
        /// Shows a radiobutton list dialog with an optional title, optional icon, a required scrollable radiobutton list, a required button, and an optional button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateRadioList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="onDismissiveButtonClicked">Called when the dismissive button is clicked.</param>
        /// <param name="dismissiveButtonText">The dismissive button text. Make null for no dismissive button.</param>
        /// <param name="selectedIndexStart">The index of the option that will be selected when the dialog is shown.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart = 0)
        {
            DialogRadioList dialog = CreateRadioList();
            dialog.Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexStart);
            dialog.Show();
            return dialog;
        }

        /// <summary>
        /// Creates a radiobutton list dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogRadioList.Show"/>, call <see cref="DialogRadioList.Initialize(string[],Action{int},string,string,ImageData,Action,string,int)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowRadioList(string[],Action{int},string,string,ImageData,Action,string,int)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogRadioList CreateRadioList()
        {
            DialogRadioList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogRadioList, instance.transform).GetComponent<DialogRadioList>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Custom Dialog

        public static void ShowCustomDialogAsync<T>(string dialogPrefabPath, System.Action<T> initializeCallback, DialogProgress progressIndicator = null) where T : MaterialDialogFrame
        {
            ShowCustomDialogAsync(dialogPrefabPath, initializeCallback, false, progressIndicator);
        }

        public static void ShowModalCustomDialogAsync<T>(string dialogPrefabPath, System.Action<T> initializeCallback, DialogProgress progressIndicator = null) where T : MaterialDialogFrame
        {
            ShowCustomDialogAsync(dialogPrefabPath, initializeCallback, true, progressIndicator);
        }

        protected static void ShowCustomDialogAsync<T>(string dialogPrefabPath, System.Action<T> initializeCallback, bool m_isModal, DialogProgress progressIndicator = null) where T : MaterialDialogFrame
        {
            System.Action<T> internalShowCallback = (dialog) =>
            {
                if (dialog != null)
                {
                    //Init
                    if (initializeCallback != null)
                        initializeCallback.Invoke(dialog);
                    if (m_isModal)
                        dialog.ShowModal();
                    else
                        dialog.Show();
                }
            };
            DialogProgress currentProgress = progressIndicator;

            System.Action<string, T> internalLoadCallback = (path, dialog) =>
            {
                //_dialogGenericDialog = dialog;
                if (dialog != null)
                    dialog.gameObject.SetActive(false);
                System.Action callbackDelayed = () =>
                {
                    //Show
                    if (internalShowCallback != null)
                        internalShowCallback.Invoke(dialog);

                    //Hide Progress Indicator
                    currentProgress.Hide();
                };
                Kyub.DelayedFunctionUtils.CallFunction(callbackDelayed, 0.5f);
            };

            if (currentProgress == null)
                currentProgress = ShowProgressModalCircular();
            else
                currentProgress.Show();
            CreateCustomDialogAsync<T>(dialogPrefabPath, internalLoadCallback);
        }

        /// <summary>
        /// Creates a custom dialog that can be modified or stored before showing.
        /// </summary>
        /// <typeparam name="T">The type of dialog to show, must inherit from <see cref="MaterialFrame"/>.</typeparam>
        /// <param name="dialogPrefabPath">The path to the dialog prefab.</param>
        /// <returns>The instance of the created dialog.</returns>
        public static T CreateCustomDialog<T>(string dialogPrefabPath) where T : MaterialFrame
        {
            T dialog = PrefabManager.InstantiateGameObject(dialogPrefabPath, instance.transform).GetComponent<T>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            return dialog;
        }

        /// <summary>
        /// Creates a custom dialog that can be modified or stored before showing.
        /// </summary>
        /// <typeparam name="T">The type of dialog to show, must inherit from <see cref="MaterialFrame"/>.</typeparam>
        /// <param name="dialogPrefabPath">The path to the dialog prefab.</param>
        /// <returns>The instance of the created dialog.</returns>
        public static void CreateCustomDialogAsync<T>(string dialogPrefabPath, System.Action<string, T> callback) where T : MaterialFrame
        {
            System.Action<string, GameObject> internalCallback = (path, dialog) =>
            {
                T assetComponent = null;
                if (dialog != null)
                    assetComponent = dialog.GetComponent<T>();

                CreateActivity(assetComponent, instance.m_ParentCanvas);
                callback(path, assetComponent);
            };
            PrefabManager.InstantiateGameObjectAsync(dialogPrefabPath, instance.transform, internalCallback);
        }

        #endregion

        #region Time Picker

        public static void ShowTimePickerAsync(DateTime time, Action<DateTime> onAffirmativeClicked, Color accentColor, System.Action<DialogTimePicker> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogTimePicker> initialize = (DialogTimePicker dialog) =>
            {
                dialog.Initialize(time, onAffirmativeClicked, accentColor);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogTimePicker>(PrefabManager.ResourcePrefabs.dialogTimePicker.Name, initialize, progressIndicator);
        }

        /// <summary>
        /// Shows a time picker dialog with a required time picker, and a required button.
        /// Accent color is <see cref="MaterialColor.teal500"/> by default.
        /// </summary>
        /// <param name="time">The time selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
        public static DialogTimePicker ShowTimePicker(DateTime time, Action<DateTime> onAffirmativeClicked)
        {
			return ShowTimePicker(time, onAffirmativeClicked, MaterialColor.teal500);
        }

        /// <summary>
        /// Shows a time picker dialog with a required time picker, and a required button.
        /// </summary>
        /// <param name="time">The time selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
        /// <param name="accentColor">Color of the accent of the picker.</param>
		public static DialogTimePicker ShowTimePicker(DateTime time, Action<DateTime> onAffirmativeClicked, Color accentColor)
        {
            DialogTimePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogTimePicker, instance.transform).GetComponent<DialogTimePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            dialog.Initialize(time, onAffirmativeClicked, accentColor);
            dialog.Show();
			return dialog;
        }

        /// <summary>
        /// Creates a time picker dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogTimePicker.Show"/>, call <see cref="DialogTimePicker.Initialize(DateTime time, Action{DateTime}, Color)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowDatePicker(DateTime time, Action{DateTime}, Color)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogTimePicker CreateTimePicker()
        {
            DialogTimePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogTimePicker, instance.transform).GetComponent<DialogTimePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Month Picker

        public static void ShowMonthPickerAsync(int year, int month, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor, System.Action<DialogDatePicker> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogDatePicker> initialize = (DialogDatePicker dialog) =>
            {
                dialog.Initialize(year, month, 1, onAffirmativeClicked, onDismissiveClicked, accentColor);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogDatePicker>(PrefabManager.ResourcePrefabs.dialogMonthPicker.Name, initialize, progressIndicator);
        }

        public static DialogDatePicker ShowMontPicker(int year, int month, Action<DateTime> onAffirmativeClicked)
        {
            return ShowMonthPicker(year, month, onAffirmativeClicked, MaterialColor.teal500);
        }

		public static DialogDatePicker ShowMonthPicker(int year, int month, Action<DateTime> onAffirmativeClicked, Color accentColor)
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogMonthPicker, instance.transform).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            dialog.Initialize(year, month, 1, onAffirmativeClicked, null, accentColor);
            dialog.Show();
            return dialog;
        }

		public static DialogDatePicker ShowMonthPicker(int year, int month, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor)
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogMonthPicker, instance.transform).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            dialog.Initialize(year, month, 1, onAffirmativeClicked, onDismissiveClicked, accentColor);
            dialog.Show();
            return dialog;
        }

        public static DialogDatePicker CreateMonthPicker()
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogMonthPicker, instance.transform).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Date Picker

        public static void ShowDatePickerAsync(int year, int month, int day, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor, System.Action<DialogDatePicker> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogDatePicker> initialize = (DialogDatePicker dialog) =>
            {
                dialog.Initialize(year, month, day, onAffirmativeClicked, onDismissiveClicked, accentColor);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogDatePicker>(PrefabManager.ResourcePrefabs.dialogDatePicker.Name, initialize, progressIndicator);
        }

        /// <summary>
        /// Shows a date picker dialog with a required date picker, and a required button.
        /// Accent color is <see cref="MaterialColor.teal500"/> by default.
        /// </summary>
        /// <param name="year">The year selected when the dialog is shown.</param>
        /// <param name="month">The month selected when the dialog is shown.</param>
        /// <param name="day">The day selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
        public static DialogDatePicker ShowDatePicker(int year, int month, int day, Action<DateTime> onAffirmativeClicked)
        {
            return ShowDatePicker(year, month, day, onAffirmativeClicked, MaterialColor.teal500);
        }

        /// <summary>
        /// Shows a date picker dialog with a required date picker, and a required button.
        /// </summary>
        /// <param name="year">The year selected when the dialog is shown.</param>
        /// <param name="month">The month selected when the dialog is shown.</param>
        /// <param name="day">The day selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
        /// <param name="accentColor">Color of the accent of the picker.</param>
		public static DialogDatePicker ShowDatePicker(int year, int month, int day, Action<DateTime> onAffirmativeClicked, Color accentColor)
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, instance.transform).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            dialog.Initialize(year, month, day, onAffirmativeClicked, null, accentColor);
            dialog.Show();
			return dialog;
        }

        /// <summary>
        /// Shows a date picker dialog with a required date picker, and a required button.
        /// </summary>
        /// <param name="year">The year selected when the dialog is shown.</param>
        /// <param name="month">The month selected when the dialog is shown.</param>
        /// <param name="day">The day selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
        /// <param name="onDismissiveClicked">Called when the negative button is clicked.</param>
        /// <param name="accentColor">Color of the accent of the picker.</param>
		public static DialogDatePicker ShowDatePicker(int year, int month, int day, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor)
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, instance.transform).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            dialog.Initialize(year, month, day, onAffirmativeClicked, onDismissiveClicked, accentColor);
            dialog.Show();
			return dialog;
        }

        /// <summary>
        /// Creates a date picker dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogDatePicker.Show"/>, call <see cref="DialogDatePicker.Initialize(int,int,int,Action{DateTime},Action,Color)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowDatePicker(int,int,int,Action{DateTime},Color)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogDatePicker CreateDatePicker()
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, instance.transform).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, instance.m_ParentCanvas);

            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Prompt

        public static void ShowPromptAsync(DialogPrompt.InputFieldConfigData firstFieldConfig, Action<string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, System.Action<DialogPrompt> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogPrompt> initialize = (DialogPrompt dialog) =>
            {
                dialog.Initialize(firstFieldConfig, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogPrompt>(PrefabManager.ResourcePrefabs.dialogPrompt.Name, initialize, progressIndicator);
        }

        public static void ShowPromptAsync(DialogPrompt.InputFieldConfigData firstFieldConfig, DialogPrompt.InputFieldConfigData secondFieldConfig, Action<string, string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, System.Action<DialogPrompt> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {

            System.Action<DialogPrompt> initialize = (DialogPrompt dialog) =>
            {
                dialog.Initialize(firstFieldConfig, secondFieldConfig, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogPrompt>(PrefabManager.ResourcePrefabs.dialogPrompt.Name, initialize, progressIndicator);
        }

        public static void ShowPromptAsync(string firstFieldName, Action<string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, System.Action<DialogPrompt> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogPrompt> initialize = (DialogPrompt dialog) =>
            {
                dialog.Initialize(firstFieldName, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogPrompt>(PrefabManager.ResourcePrefabs.dialogPrompt.Name, initialize, progressIndicator);
        }

        public static void ShowPromptAsync(string firstFieldName, string secondFieldName, Action<string, string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, System.Action<DialogPrompt> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogPrompt> initialize = (DialogPrompt dialog) =>
            {
                dialog.Initialize(firstFieldName, secondFieldName, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogPrompt>(PrefabManager.ResourcePrefabs.dialogPrompt.Name, initialize, progressIndicator);
        }

        public static DialogPrompt ShowPrompt(DialogPrompt.InputFieldConfigData firstFieldConfig, Action<string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            DialogPrompt dialog = CreatePrompt();
            dialog.Initialize(firstFieldConfig, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            dialog.Show();
            return dialog;
        }

        public static DialogPrompt ShowPrompt(DialogPrompt.InputFieldConfigData firstFieldConfig, DialogPrompt.InputFieldConfigData secondFieldConfig, Action<string, string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            DialogPrompt dialog = CreatePrompt();
            dialog.Initialize(firstFieldConfig, secondFieldConfig, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            dialog.Show();
            return dialog;
        }

        /// <summary>
        /// Shows a prompt dialog with an optional title, optional icon, a required input field, a required button, and an optional button.
        /// <para></para>
        /// For more customizability, use <see cref="CreatePrompt"/>.
        /// </summary>
        /// <param name="firstFieldName">Name of the first field.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="onDismissiveButtonClicked">Called when the dismissive button is clicked.</param>
        /// <param name="dismissiveButtonText">The dismissive button text. Make null for no dismissive button.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogPrompt ShowPrompt(string firstFieldName, Action<string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            DialogPrompt dialog = CreatePrompt();
            dialog.Initialize(firstFieldName, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            dialog.Show();
            return dialog;
        }

        /// <summary>
        /// Shows a prompt dialog with an optional title, optional icon, a required input field, an optional input field, a required button, and an optional button.
        /// <para></para>
        /// For more customizability, use <see cref="CreatePrompt"/>.
        /// </summary>
        /// <param name="firstFieldName">Name of the first field.</param>
        /// <param name="secondFieldName">Name of the second field. Make null for no second field.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <param name="titleText">The title text. Make null for no title.</param>
        /// <param name="icon">The icon next to the title. Make null for no icon.</param>
        /// <param name="onDismissiveButtonClicked">Called when the dismissive button is clicked.</param>
        /// <param name="dismissiveButtonText">The dismissive button text. Make null for no dismissive button.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public static DialogPrompt ShowPrompt(string firstFieldName, string secondFieldName, Action<string, string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            DialogPrompt dialog = CreatePrompt();
            dialog.Initialize(firstFieldName, secondFieldName, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            dialog.Show();
            return dialog;
        }

        /// <summary>
        /// Creates a prompt dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogPrompt.Show"/>, call <see cref="DialogPrompt.Initialize(string,string,Action{string, string},string,string,ImageData,Action,string)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowPrompt(string,string,Action{string, string},string,string,ImageData,Action,string)"/>.
        /// </summary>
        /// <returns>The instance of the created dialog.</returns>
        public static DialogPrompt CreatePrompt()
        {
            DialogPrompt dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogPrompt, instance.transform).GetComponent<DialogPrompt>();
            CreateActivity(dialog, instance.m_ParentCanvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Static Functions

        public static MaterialDialogActivity CreateActivity<TDialogFrame>(TDialogFrame frame, Canvas parent = null) where TDialogFrame : MaterialFrame
        {
            if (frame == null)
                return null;

            if (parent == null)
                parent = instance.m_ParentCanvas;

            return CreateActivity(frame, parent.transform);
        }

        public static MaterialDialogActivity CreateActivity<TDialogFrame>(TDialogFrame frame, Transform parent) where TDialogFrame : MaterialFrame
        {
            MaterialDialogActivity activity = new GameObject(frame.name + " (Activity)").AddComponent<MaterialDialogActivity>();
            activity.Build(parent);
            activity.SetFrame(frame, false);

            return activity;
        }

        #endregion
    }
}