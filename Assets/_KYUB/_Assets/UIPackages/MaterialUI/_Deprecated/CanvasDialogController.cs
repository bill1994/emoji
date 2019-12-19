using System;
using UnityEngine;

namespace MaterialUI
{
    /// <summary>
    /// Canvas component that handles the creation of Dialogs.
    /// </summary>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [System.Obsolete("Use DialogManager Instead")]
    [RequireComponent(typeof(Canvas))]
    //[AddComponentMenu("MaterialUI/Managers/Dialog Manager")]
    public class CanvasDialogController : MonoBehaviour
    {
        /// <summary>
        /// The parent canvas.
        /// </summary>
        //private Canvas m_ParentCanvas;

        /// <summary>
        /// The rect transform.
        /// </summary>
        private RectTransform m_RectTransform;
        /// <summary>
        /// The rect transform.
        /// If null, automatically gets the attached RectTransform.
        /// </summary>
        public RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    m_RectTransform = transform as RectTransform;
                }

                return m_RectTransform;
            }
        }

        /// <summary>
        /// See Monobehaviour.Awake.
        /// </summary>
        void Awake()
        {
            InitDialogSystem();
        }

        /// <summary>
        /// Initializes the dialog system.
        /// </summary>
        private void InitDialogSystem()
        {
            //m_ParentCanvas = gameObject.GetComponent<Canvas>();
            m_RectTransform = gameObject.GetAddComponent<RectTransform>();

            /*rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = Vector2.zero;*/
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
        public DialogAlert ShowAlert(string bodyText, string titleText, ImageData icon)
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
        public DialogAlert ShowAlert(string bodyText, Action onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
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
        public DialogAlert ShowAlert(string bodyText, Action onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
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
        public DialogAlert CreateAlert()
        {
            DialogAlert dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogAlert, GetContentTransform()).GetComponent<DialogAlert>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            //dialog.Initialize();
            return dialog;
        }

        /// <summary>
        /// Shows a linear progress dialog with an optional body text, and a required progress indicator.
        /// <para></para>
        /// For more customizability, use <see cref="CreateProgressLinear"/>.
        /// </summary>
        /// <param name="bodyText">The body text. Make null for no body.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public DialogProgress ShowProgressLinear(string bodyText)
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
        public DialogProgress ShowProgressLinear(string bodyText, string titleText, ImageData icon, bool startStationaryAtZero = false)
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
        public DialogProgress CreateProgressLinear()
        {
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogProgress, GetContentTransform()).GetComponent<DialogProgress>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
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
        public DialogProgress ShowProgressCircular(string bodyText)
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
        public DialogProgress ShowProgressCircular(string bodyText, string titleText, ImageData icon, bool startStationaryAtZero = false)
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
        public DialogProgress CreateProgressCircular()
        {
            DialogProgress dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogProgress, GetContentTransform()).GetComponent<DialogProgress>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            dialog.SetupIndicator(false);
            //dialog.Initialize();
            return dialog;
        }
        /// <summary>
        /// Shows an simple list dialog with a a required scrollable option list (label-only).
        /// <para></para>
        /// For more customizability, use <see cref="CreateSimpleList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onItemClick">Called when an option is selected.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public DialogSimpleList ShowSimpleList(string[] options, Action<int> onItemClick)
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
        public DialogSimpleList ShowSimpleList(string[] options, Action<int> onItemClick, string titleText, ImageData icon)
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
        public DialogSimpleList ShowSimpleList(OptionDataList optionDataList, Action<int> onItemClick)
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
        public DialogSimpleList ShowSimpleList(OptionDataList optionDataList, Action<int> onItemClick, string titleText, ImageData icon)
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
        public DialogSimpleList CreateSimpleList()
        {
            DialogSimpleList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogSimpleList, GetContentTransform()).GetComponent<DialogSimpleList>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            //dialog.Initialize();
            return dialog;
        }

        /// <summary>
        /// Shows a checkbox list dialog with a required scrollable checkbox list.
        /// <para></para>
        /// For more customizability, use <see cref="CreateCheckboxList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public DialogCheckboxList ShowCheckboxList(string[] options, Action<bool[]> onAffirmativeButtonClicked, string affirmativeButtonText = "OK")
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
        public DialogCheckboxList ShowCheckboxList(string[] options, Action<bool[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
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
        public DialogCheckboxList ShowCheckboxList(string[] options, Action<bool[]> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
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
        public DialogCheckboxList CreateCheckboxList()
        {
            DialogCheckboxList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogCheckboxList, GetContentTransform()).GetComponent<DialogCheckboxList>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            //dialog.Initialize();
            return dialog;
        }

        /// <summary>
        /// Shows a radiobutton list dialog with a required scrollable radiobutton list, and a required button.
        /// <para></para>
        /// For more customizability, use <see cref="CreateRadioList"/>.
        /// </summary>
        /// <param name="options">The strings to use for the list item labels.</param>
        /// <param name="onAffirmativeButtonClicked">Called when the affirmative button is clicked.</param>
        /// <param name="affirmativeButtonText">The affirmative button text.</param>
        /// <returns>The instance of the initialized, shown dialog.</returns>
        public DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText = "OK")
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
        public DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, int selectedIndexStart)
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
        public DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon)
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
        public DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, int selectedIndexStart)
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
        public DialogRadioList ShowRadioList(string[] options, Action<int> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText, int selectedIndexStart = 0)
        {
            DialogRadioList dialog = CreateRadioList();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
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
        public DialogRadioList CreateRadioList()
        {
            DialogRadioList dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogRadioList, GetContentTransform()).GetComponent<DialogRadioList>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            //dialog.Initialize();
            return dialog;
        }

        /// <summary>
        /// Creates a custom dialog that can be modified or stored before showing.
        /// </summary>
        /// <typeparam name="T">The type of dialog to show, must inherit from <see cref="MaterialDialogCompat"/>.</typeparam>
        /// <param name="dialogPrefabPath">The path to the dialog prefab.</param>
        /// <returns>The instance of the created dialog.</returns>
        public T CreateCustomDialog<T>(string dialogPrefabPath) where T : MaterialDialogCompat
        {
            T dialog = PrefabManager.InstantiateGameObject(dialogPrefabPath, GetContentTransform()).GetComponent<T>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            return dialog;
        }

        /// <summary>
        /// Creates a custom dialog that can be modified or stored before showing.
        /// </summary>
        /// <typeparam name="T">The type of dialog to show, must inherit from <see cref="MaterialDialogCompat"/>.</typeparam>
        /// <param name="dialogPrefabPath">The path to the dialog prefab.</param>
        /// <returns>The instance of the created dialog.</returns>
        public void CreateCustomDialogAsync<T>(string dialogPrefabPath, System.Action<string, T> callback) where T : MaterialDialogCompat
        {
            System.Action<string, GameObject> internalcallback = (path, dialog) =>
            {
                T assetComponent = null;
                if (dialog != null)
                {
                    assetComponent = dialog.GetComponent<T>();
                    DialogManager.CreateActivity(assetComponent, dialog.transform.parent);
                }
                callback(path, assetComponent);
            };
            PrefabManager.InstantiateGameObjectAsync(dialogPrefabPath, GetContentTransform(), internalcallback);
        }

        /// <summary>
        /// Shows a time picker dialog with a required time picker, and a required button.
        /// Accent color is <see cref="MaterialColor.teal500"/> by default.
        /// </summary>
        /// <param name="time">The time selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
		public DialogTimePicker ShowTimePicker(DateTime time, Action<DateTime> onAffirmativeClicked)
        {
			return ShowTimePicker(time, onAffirmativeClicked, MaterialColor.teal500);
        }

        /// <summary>
        /// Shows a time picker dialog with a required time picker, and a required button.
        /// </summary>
        /// <param name="time">The time selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
        /// <param name="accentColor">Color of the accent of the picker.</param>
		public DialogTimePicker ShowTimePicker(DateTime time, Action<DateTime> onAffirmativeClicked, Color accentColor)
        {
            DialogTimePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogTimePicker, GetContentTransform()).GetComponent<DialogTimePicker>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            dialog.Initialize(time, onAffirmativeClicked, accentColor);
            dialog.Show();
			return dialog;
        }

        /// <summary>
        /// Shows a date picker dialog with a required date picker, and a required button.
        /// Accent color is <see cref="MaterialColor.teal500"/> by default.
        /// </summary>
        /// <param name="year">The year selected when the dialog is shown.</param>
        /// <param name="month">The month selected when the dialog is shown.</param>
        /// <param name="day">The day selected when the dialog is shown.</param>
        /// <param name="onAffirmativeClicked">Called when the affirmative button is clicked.</param>
		public DialogDatePicker ShowDatePicker(int year, int month, int day, Action<DateTime> onAffirmativeClicked)
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
		public DialogDatePicker ShowDatePicker(int year, int month, int day, Action<DateTime> onAffirmativeClicked, Color accentColor)
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, GetContentTransform()).GetComponent<DialogDatePicker>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
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
		public DialogDatePicker ShowDatePicker(int year, int month, int day, Action<DateTime> onAffirmativeClicked, Action onDismissiveClicked, Color accentColor)
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, GetContentTransform()).GetComponent<DialogDatePicker>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
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
        public DialogDatePicker CreateDatePicker()
        {
            DialogDatePicker dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogDatePicker, GetContentTransform()).GetComponent<DialogDatePicker>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            //dialog.Initialize();
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
        public DialogPrompt ShowPrompt(string firstFieldName, Action<string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
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
        public DialogPrompt ShowPrompt(string firstFieldName, string secondFieldName, Action<string, string> onAffirmativeButtonClicked, string affirmativeButtonText, string titleText, ImageData icon, Action onDismissiveButtonClicked, string dismissiveButtonText)
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
        public DialogPrompt CreatePrompt()
        {
            DialogPrompt dialog = PrefabManager.InstantiateGameObject(PrefabManager.ResourcePrefabs.dialogPrompt, GetContentTransform()).GetComponent<DialogPrompt>();
            DialogManager.CreateActivity(dialog, dialog.transform.parent);
            //dialog.Initialize();
            return dialog;
        }

        protected Transform GetContentTransform()
        {
            var safeArea = transform.GetComponent<CanvasSafeArea>();
            return safeArea != null && safeArea.Content != null ? safeArea.Content : transform;
        }
    }
}