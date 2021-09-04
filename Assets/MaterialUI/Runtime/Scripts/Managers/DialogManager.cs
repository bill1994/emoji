//  Copyright 2017 MaterialUI for Unity http://materialunity.com
//  Please see license file for terms and conditions of use, and more information.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MaterialUI
{
    /// <summary>
    /// Singleton component that handles the creation of Dialogs.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [AddComponentMenu("MaterialUI/Managers/Dialog Manager")]
    public class DialogManager : Kyub.Singleton<DialogManager>
    {
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
                if (Instance != null && s_instance.m_RectTransform == null)
                {
                    if (s_instance.m_ParentCanvas == null)
                        s_instance.InitDialogSystem();

                    if (s_instance.m_ParentCanvas != null)
                    {
                        CanvasSafeArea safeArea = s_instance.m_ParentCanvas.GetComponent<CanvasSafeArea>();
                        Instance.m_RectTransform = safeArea != null && safeArea.Content != null ? safeArea.Content : s_instance.m_ParentCanvas.transform as RectTransform;
                    }
                }

                return Instance.m_RectTransform;
            }
        }

        public static Canvas parentCanvas
        {
            get
            {
                if (Instance != null && s_instance.m_ParentCanvas == null)
                    s_instance.InitDialogSystem();

                return Instance.m_ParentCanvas;
            }
        }

        #endregion

        #region Unity Functions

        protected override void Awake()
        {
            base.Awake();
            if (s_instance == this)
            {
                s_instance.InitDialogSystem();
            }
        }

        protected override void OnSceneWasLoaded(Scene p_scene, LoadSceneMode p_mode)
        {
            if (s_instance == this)
            {
                if (m_ParentCanvas == null)
                    s_instance.InitDialogSystem();
            }
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

        public static void ShowAlertAsync(string bodyText, string titleText, ImageData icon, System.Action<DialogAlert> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            ShowAlertAsync(bodyText, null, "OK", titleText, icon, onCreateCallback, progressIndicator);
        }

        public static void ShowAlertAsync(string bodyText, Action onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, System.Action<DialogAlert> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            ShowAlertAsync(bodyText, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, null, null, onCreateCallback, progressIndicator);
        }

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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogAlert CreateAlert()
        {
            var canvas = DialogManager.parentCanvas;
            DialogAlert dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogAlert, Instance.transform, false).GetComponent<DialogAlert>();
            CreateActivity(dialog, canvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Banner

        public static DialogAlert ShowBanner(string titleText)
        {
            return ShowBanner(titleText, null);
        }

        public static DialogAlert ShowBanner(string titleText, ImageData icon)
        {
            return ShowBanner(titleText, icon, null, "DISMISS");
        }

        public static DialogAlert ShowBanner(string titleText, Action onAffirmativeButtonClicked, string affirmativeButtonText)
        {
            return ShowBanner(titleText, null, onAffirmativeButtonClicked, affirmativeButtonText);
        }

        public static DialogAlert ShowBanner(string titleText, ImageData icon, Action onAffirmativeButtonClicked, string affirmativeButtonText)
        {
            return ShowBanner(titleText, icon, onAffirmativeButtonClicked, affirmativeButtonText, null, null);
        }

        public static DialogAlert ShowBanner(string titleText, Action onAffirmativeButtonClicked, string affirmativeButtonText, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            return ShowBanner(titleText, null, onAffirmativeButtonClicked, affirmativeButtonText, onDismissiveButtonClicked, dismissiveButtonText);
        }

        /// <summary>
        /// Shows an banner dialog with an optional optional icon and 2 optional buttons.
        /// <para></para>
        /// For more customizability, use <see cref="CreateBanner"/>.
        /// </summary>
        public static DialogAlert ShowBanner(string titleText, ImageData icon, Action onAffirmativeButtonClicked, string affirmativeButtonText, Action onDismissiveButtonClicked, string dismissiveButtonText)
        {
            DialogAlert dialog = CreateBanner();
            dialog.Initialize(null, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText);
            dialog.ShowModal();
            return dialog;
        }

        /// <summary>
        /// Creates an alert dialog that can be modified or stored before showing.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowAlert(string,Action,string,string,ImageData,Action,string)"/>.
        /// </summary>
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogAlert CreateBanner()
        {
            var canvas = DialogManager.parentCanvas;
            DialogAlert dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogBanner, Instance.transform, false).GetComponent<DialogAlert>();
            CreateActivity(dialog, canvas);
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogProgress CreateProgressModalCircular()
        {
            var canvas = DialogManager.parentCanvas;
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogModalCircleProgress, Instance.transform, false).GetComponent<DialogProgress>();
            CreateActivity(dialog, canvas);

            return dialog;
        }

        /// <summary>
        /// Shows a linear progress dialog with an optional body text, and a required progress indicator.
        /// <para></para>
        /// For more customizability, use <see cref="CreateProgressLinear"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogProgress CreateProgressLinear()
        {
            var canvas = DialogManager.parentCanvas;
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogProgress, Instance.transform, false).GetComponent<DialogProgress>();
            CreateActivity(dialog, canvas);
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogProgress CreateProgressCircular()
        {
            var canvas = DialogManager.parentCanvas;
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogProgress, Instance.transform, false).GetComponent<DialogProgress>();
            CreateActivity(dialog, canvas);
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowSimpleList(IList<string> options, Action<int> onItemClick)
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowSimpleList(IList<string> options, Action<int> onItemClick, string titleText, ImageData icon)
        {
            List<OptionData> optionsData = new List<OptionData>();

            for (int i = 0; i < options.Count; i++)
            {
                OptionData optionData = new OptionData(options[i], null);
                optionsData.Add(optionData);
            }

            return ShowSimpleList(optionsData.ToArray(), onItemClick, titleText, icon);
        }

        /// <summary>
        /// Shows an simple list dialog with a required scrollable option list.
        /// <para></para>
        /// For more customizability, use <see cref="CreateSimpleList"/>.
        /// </summary>
        /// <param name="optionDataList">The data to use for the option list.</param>
        /// <param name="onItemClick">Called when an option is selected.</param>
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowSimpleList<TOptionData>(IList<TOptionData> options, Action<int> onItemClick) where TOptionData : OptionData, new()
        {
            return ShowSimpleList(options, onItemClick, null, null);
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowSimpleList<TOptionData>(IList<TOptionData> options, Action<int> onItemClick, string titleText, ImageData icon) where TOptionData : OptionData, new()
        {
            DialogRadioList dialog = CreateSimpleList();
            dialog.Initialize(options, onItemClick, null, titleText, icon, null, null, -1, true);
            dialog.Show();
            return dialog;
        }

        public static void ShowSimpleListAsync<TOptionData>(IList<TOptionData> options, Action<int> onItemClick, string titleText, ImageData icon, Action onDismiss, System.Action<DialogRadioList> onCreateCallback = null, ProgressIndicator progressIndicator = null) where TOptionData : OptionData, new()
        {
            System.Action<DialogRadioList> initialize = (DialogRadioList dialog) =>
            {
                dialog.Initialize(options, onItemClick, null, titleText, icon, null, null, -1, true);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogRadioList>(PrefabManager.ResourcePrefabs.dialogSimpleList.Name, initialize, progressIndicator);
        }

        /// <summary>
        /// Creates a simple list dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogRadioList.Show"/>, call <see cref="DialogRadioList.Initialize(OptionDataList,Action{int},string,ImageData)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowSimpleList(OptionDataList,Action{int},string,ImageData)"/>.
        /// </summary>
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogRadioList CreateSimpleList()
        {
            var canvas = DialogManager.parentCanvas;
            DialogRadioList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogSimpleList, Instance.transform, false).GetComponent<DialogRadioList>();
            CreateActivity(dialog, canvas);
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogCheckboxList ShowCheckboxList(IList<string> options, Action<int[]> onAffirmativeButtonClicked, string affirmativeButtonText = "OK")
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogCheckboxList ShowCheckboxList(IList<string> options, Action<int[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
        {
            return ShowCheckboxList(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, null, null, null);
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogCheckboxList ShowCheckboxList(IList<string> options, Action<int[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, ICollection<int> selectedIndexes = null)
        {
            DialogCheckboxList dialog = CreateCheckboxList();
            dialog.Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexes);
            dialog.Show();
            return dialog;
        }

        public static void ShowCheckboxListAsync<TOptionData>(IList<TOptionData> options, Action<int[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, ICollection<int> selectedIndexes = null, System.Action<DialogCheckboxList> onCreateCallback = null, ProgressIndicator progressIndicator = null) where TOptionData : OptionData, new()
        {
            System.Action<DialogCheckboxList> initialize = (DialogCheckboxList dialog) =>
            {
                dialog.Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexes);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogCheckboxList>(PrefabManager.ResourcePrefabs.dialogRadioList.Name, initialize, progressIndicator);
        }

        /// <summary>
        /// Creates a checkbox list dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogCheckboxList.Show"/>, call <see cref="DialogCheckboxList.Initialize(IList<string>,Action{bool[]},string,string,ImageData,Action,string)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowCheckboxList(IList<string>,Action{bool[]},string,string,ImageData,Action,string)"/>.
        /// </summary>
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogCheckboxList CreateCheckboxList()
        {
            var canvas = DialogManager.parentCanvas;
            DialogCheckboxList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogCheckboxList, Instance.transform, false).GetComponent<DialogCheckboxList>();
            CreateActivity(dialog, canvas);
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(IList<string> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText = "OK")
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(IList<string> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, int selectedIndexStart)
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(IList<string> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(IList<string> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, int selectedIndexStart)
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
        public static DialogRadioList ShowRadioList(IList<string> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart = 0)
        {
            DialogRadioList dialog = CreateRadioList();
            dialog.Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexStart);
            dialog.Show();
            return dialog;
        }

        public static DialogRadioList ShowRadioList<TOptionData>(IList<TOptionData> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart = 0, bool allowSwitchOff = false) where TOptionData : OptionData, new()
        {
            DialogRadioList dialog = CreateRadioList();
            dialog.Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexStart, allowSwitchOff);
            dialog.Show();
            return dialog;
        }

        public static void ShowRadioListAsync<TOptionData>(IList<TOptionData> options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart, bool allowSwitchOff, System.Action<DialogRadioList> onCreateCallback = null, ProgressIndicator progressIndicator = null) where TOptionData : OptionData, new()
        {
            System.Action<DialogRadioList> initialize = (DialogRadioList dialog) =>
            {
                dialog.Initialize(options, onAffirmativeButtonClicked, affirmativeButtonText, titleText, icon, onDismissiveButtonClicked, dismissiveButtonText, selectedIndexStart, allowSwitchOff);
                if (onCreateCallback != null)
                    onCreateCallback(dialog);
            };
            ShowCustomDialogAsync<DialogRadioList>(PrefabManager.ResourcePrefabs.dialogRadioList.Name, initialize, progressIndicator);
        }

        /// <summary>
        /// Creates a radiobutton list dialog that can be modified or stored before showing.
        /// <para></para>
        /// Before calling <see cref="DialogRadioList.Show"/>, call <see cref="DialogRadioList.Initialize(IList<string>,Action{int},string,string,ImageData,Action,string,int)"/>.
        /// <para></para>
        /// For a simpler solution with less customizability, use <see cref="ShowRadioList(IList<string>,Action{int},string,string,ImageData,Action,string,int)"/>.
        /// </summary>
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogRadioList CreateRadioList()
        {
            var canvas = DialogManager.parentCanvas;
            DialogRadioList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogRadioList, Instance.transform, false).GetComponent<DialogRadioList>();
            CreateActivity(dialog, canvas);
            //dialog.Initialize();
            return dialog;
        }

        #endregion

        #region Custom Dialog

        public static T ShowCustomDialog<T>(string dialogPrefabPath, System.Action<T> initializeCallback) where T : MaterialDialogFrame
        {
            return ShowCustomDialog(dialogPrefabPath, initializeCallback, false);
        }

        public static T ShowModalCustomDialog<T>(string dialogPrefabPath, System.Action<T> initializeCallback) where T : MaterialDialogFrame
        {
            return ShowCustomDialog(dialogPrefabPath, initializeCallback, true);
        }

        protected static T ShowCustomDialog<T>(string dialogPrefabPath, System.Action<T> initializeCallback, bool m_isModal) where T : MaterialDialogFrame
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

            var createdDialog = CreateCustomDialog<T>(dialogPrefabPath);
            if (createdDialog != null)
                internalShowCallback(createdDialog);

            return createdDialog;
        }

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
                //if (dialog != null)
                //    dialog.gameObject.SetActive(false);

                System.Action callbackDelayed = () =>
                {
                    //Show
                    if (internalShowCallback != null)
                        internalShowCallback.Invoke(dialog);

                    //Hide Progress Indicator
                    currentProgress.Hide();
                };
                Kyub.ApplicationContext.RunOnMainThread(callbackDelayed, 0.5f);
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
        /// <returns>The Instance of the created dialog.</returns>
        public static T CreateCustomDialog<T>(string dialogPrefabPath) where T : MaterialFrame
        {
            var canvas = DialogManager.parentCanvas;
            T dialog = PrefabManager.InstantiateGameObject(dialogPrefabPath, Instance.transform, false).GetComponent<T>();
            CreateActivity(dialog, canvas);
            return dialog;
        }

        /// <summary>
        /// Creates a custom dialog that can be modified or stored before showing.
        /// </summary>
        /// <typeparam name="T">The type of dialog to show, must inherit from <see cref="MaterialFrame"/>.</typeparam>
        /// <param name="dialogPrefabPath">The path to the dialog prefab.</param>
        /// <returns>The Instance of the created dialog.</returns>
        public static void CreateCustomDialogAsync<T>(string dialogPrefabPath, System.Action<string, T> callback) where T : MaterialFrame
        {
            var canvas = DialogManager.parentCanvas;
            System.Action<string, GameObject> internalCallback = (path, dialog) =>
            {
                T assetComponent = null;
                if (dialog != null)
                    assetComponent = dialog.GetComponent<T>();
                CreateActivity(assetComponent, canvas);
                callback(path, assetComponent);
            };
            PrefabManager.InstantiateGameObjectAsync(dialogPrefabPath, Instance.transform, internalCallback, false);
        }

        #endregion

        #region Time Picker

        public static void ShowTimePickerAsync(DateTime time, Action<DateTime> onAffirmativeClicked, Color accentColor, System.Action<DialogTimePicker> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            ShowTimePickerAsync(time, onAffirmativeClicked, null, accentColor, onCreateCallback, progressIndicator);
        }

        public static void ShowTimePickerAsync(DateTime time, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor, System.Action<DialogTimePicker> onCreateCallback = null, ProgressIndicator progressIndicator = null)
        {
            System.Action<DialogTimePicker> initialize = (DialogTimePicker dialog) =>
            {
                dialog.Initialize(time, onAffirmativeClicked, onDismissiveClicked, accentColor);
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

        public static DialogTimePicker ShowTimePicker(DateTime time, Action<DateTime> onAffirmativeClicked, Color accentColor)
        {
            return ShowTimePicker(time, onAffirmativeClicked, null, accentColor);
        }

        /// <summary>
        /// Shows a time picker dialog with a required time picker, and a required button.
        /// </summary>
        /// <param name="time">The time selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
        /// <param name="accentColor">Color of the accent of the picker.</param>
		public static DialogTimePicker ShowTimePicker(DateTime time, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor)
        {
            var canvas = DialogManager.parentCanvas;
            DialogTimePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogTimePicker, Instance.transform, false).GetComponent<DialogTimePicker>();
            CreateActivity(dialog, canvas);

            dialog.Initialize(time, onAffirmativeClicked, onDismissiveClicked, accentColor);
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
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogTimePicker CreateTimePicker()
        {
            var canvas = DialogManager.parentCanvas;
            DialogTimePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogTimePicker, Instance.transform, false).GetComponent<DialogTimePicker>();
            CreateActivity(dialog, canvas);

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

        public static DialogDatePicker ShowMonthPicker(int year, int month, Action<DateTime> onAffirmativeClicked)
        {
            return ShowMonthPicker(year, month, onAffirmativeClicked, MaterialColor.teal500);
        }

        public static DialogDatePicker ShowMonthPicker(int year, int month, Action<DateTime> onAffirmativeClicked, Color accentColor)
        {
            var canvas = DialogManager.parentCanvas;
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogMonthPicker, Instance.transform, false).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, canvas);

            dialog.Initialize(year, month, 1, onAffirmativeClicked, null, accentColor);
            dialog.Show();
            return dialog;
        }

        public static DialogDatePicker ShowMonthPicker(int year, int month, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor)
        {
            var canvas = DialogManager.parentCanvas;
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogMonthPicker, Instance.transform, false).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, canvas);

            dialog.Initialize(year, month, 1, onAffirmativeClicked, onDismissiveClicked, accentColor);
            dialog.Show();
            return dialog;
        }

        public static DialogDatePicker CreateMonthPicker()
        {
            var canvas = DialogManager.parentCanvas;
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogMonthPicker, Instance.transform, false).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, canvas);
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
            var canvas = DialogManager.parentCanvas;
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, Instance.transform, false).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, canvas);

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
            var canvas = DialogManager.parentCanvas;
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, Instance.transform, false).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, canvas);

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
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogDatePicker CreateDatePicker()
        {
            var canvas = DialogManager.parentCanvas;
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, Instance.transform, false).GetComponent<DialogDatePicker>();
            CreateActivity(dialog, canvas);

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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the initialized, shown dialog.</returns>
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
        /// <returns>The Instance of the created dialog.</returns>
        public static DialogPrompt CreatePrompt()
        {
            var canvas = DialogManager.parentCanvas;
            DialogPrompt dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogPrompt, Instance.transform, false).GetComponent<DialogPrompt>();
            CreateActivity(dialog, canvas);
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
                parent = Instance.m_ParentCanvas;

            Transform parentTransform = null;
            if (parent != null)
            {
                CanvasSafeArea safeArea = parent.GetComponent<CanvasSafeArea>();
                parentTransform = safeArea != null && safeArea.Content != null ? safeArea.Content : parent.transform;
            }
            return CreateActivity(frame, parentTransform);
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