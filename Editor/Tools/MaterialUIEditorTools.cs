#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.SceneManagement;

namespace MaterialUI
{
    [ExecuteInEditMode]
    public static class MaterialUIEditorTools
    {
        private static GameObject m_LastInstance;
        private static GameObject m_SelectedObject;
        private static bool m_NotCanvas;

        #region GeneralCreationMethods

        public static bool IsPackage()
        {
            return GetDefaultPrefabsFolderPath().Contains("Package/");
        }

        static string s_cachedDefaultPrefabsFolderPath = null;
        public static string GetDefaultPrefabsFolderPath()
        {
            if (s_cachedDefaultPrefabsFolderPath == null)
                s_cachedDefaultPrefabsFolderPath = GetSubfolderInAPI("DefaultAssets/Prefabs/");

            return s_cachedDefaultPrefabsFolderPath;
        }

        static string s_cachedEssentialsFolderPath = null;
        public static string GetEssentialsFolderPath()
        {
            if (s_cachedEssentialsFolderPath == null)
            {
                var essentialsKey = "MaterialUIEssentials";
                s_cachedEssentialsFolderPath = Application.dataPath + "/" + essentialsKey;
                /*if (IsPackage())
                {
                    s_cachedEssentialsFolderPath = Application.dataPath + "/" + essentialsKey;
                }
                else
                {
                    s_cachedEssentialsFolderPath = GetSubfolderInAPI(essentialsKey);
                }*/
            }

            return s_cachedEssentialsFolderPath;
        }

        public static string GetSubfolderInAPI(string relativeFolderPath)
        {
            var packagePath = "Packages/com.kyub.materialui";
            //Try discover if script is inside Package or in AssetFolder
            var fullPackagePath = System.IO.Path.GetFullPath(packagePath).Replace("\\", "/");
            if (!System.IO.Directory.Exists(fullPackagePath))
                fullPackagePath = "";

            string[] files = System.IO.Directory.GetFiles(string.IsNullOrEmpty(fullPackagePath) ? Application.dataPath : fullPackagePath, "MaterialUIEditorTools.cs", System.IO.SearchOption.AllDirectories);

            var folderPath = files.Length > 0 ? files[0].Replace("\\", "/") : "";
            if (!string.IsNullOrEmpty(folderPath))
            {
                var keyFolderPath = "MaterialUI/";
                relativeFolderPath = relativeFolderPath.StartsWith("\\") || relativeFolderPath.StartsWith("/") ? relativeFolderPath.Substring(1, relativeFolderPath.Length - 1) : relativeFolderPath;
                if (folderPath.Contains(keyFolderPath))
                    folderPath = System.IO.Path.Combine(folderPath.Split(new string[] { keyFolderPath }, System.StringSplitOptions.None)[0] + System.IO.Path.Combine(keyFolderPath, relativeFolderPath)).Replace("\\", "/");
                else
                    folderPath = System.IO.Path.Combine(packagePath, relativeFolderPath).Replace("\\", "/");

                if (string.IsNullOrEmpty(fullPackagePath))
                    folderPath = folderPath.Replace(Application.dataPath, "Assets");
                else
                    folderPath = folderPath.Replace(fullPackagePath, packagePath); //Support new Package Manager file system
            }

            return folderPath;
        }

        public static void CreateInstance(string assetPath, string objectName)
        {
            string mainFolderPath = GetDefaultPrefabsFolderPath();
            m_LastInstance = Object.Instantiate(AssetDatabase.LoadAssetAtPath(mainFolderPath + assetPath + ".prefab", typeof(GameObject))) as GameObject;
            m_LastInstance.name = objectName;

            CreateCanvasIfNeeded();

            // Setting the element to be a child of an element already in the scene should
            // be sufficient to also move the element to that scene.
            SceneManager.MoveGameObjectToScene(m_LastInstance, m_SelectedObject.scene);

            m_LastInstance.transform.SetParent(m_SelectedObject.transform);
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            m_LastInstance.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
            Selection.activeObject = m_LastInstance;



            Undo.RegisterCreatedObjectUndo(m_LastInstance, "create " + m_LastInstance.name);
        }

        private static void CreateCanvasIfNeeded()
        {
            string mainFolderPath = GetDefaultPrefabsFolderPath();
            if (Selection.activeObject != null && Selection.activeObject.GetType() == (typeof(GameObject)))
            {
                m_SelectedObject = (GameObject)Selection.activeObject;
            }

            if (m_SelectedObject)
            {
                if (m_SelectedObject.scene.IsValid())
                {
                    m_NotCanvas = m_SelectedObject.GetComponentInParent<Canvas>() == null;
                }
                else
                {
                    m_NotCanvas = true;
                }
            }
            else
            {
                m_NotCanvas = true;
            }

            if (m_NotCanvas)
            {
                Canvas[] canvasArray = StageUtility.GetCurrentStageHandle().FindComponentsOfType<Canvas>();
                for (int i = 0; i < canvasArray.Length; i++)
                {
                    if (canvasArray[i].isRootCanvas)
                    {
                        m_SelectedObject = canvasArray[i].gameObject;
                    }
                }

                // If in Prefab Mode, Canvas has to be part of Prefab contents,
                // otherwise use Prefab root instead.
                PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null && !prefabStage.IsPartOfPrefabContents(m_SelectedObject))
                    m_SelectedObject = prefabStage.prefabContentsRoot;

                if (!m_SelectedObject)
                {
                    m_SelectedObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath(mainFolderPath + "Common/Canvas.prefab", typeof(GameObject))) as GameObject;
                    StageUtility.PlaceGameObjectInCurrentStage(m_SelectedObject);
                    m_SelectedObject.name = "Canvas";
                }

                if (canvasArray.Length == 0)
                {
                    var eventSystem = Object.Instantiate(AssetDatabase.LoadAssetAtPath(mainFolderPath + "Common/EventSystem.prefab", typeof(GameObject))) as GameObject;
                    eventSystem.name = "EventSystem";
                    StageUtility.PlaceGameObjectInCurrentStage(eventSystem);
                }

            }
        }

        #endregion

        #region CreateObjects

        [MenuItem("GameObject/MaterialUI/Sliders/Slider", false, 030)]
        private static void CreateDefaultSlider()
        {
            CreateInstance("Components/Sliders/Slider", "Slider");
        }

        [MenuItem("GameObject/MaterialUI/Sliders/Range Slider", false, 030)]
        private static void CreateRangeSlider()
        {
            CreateInstance("Components/Sliders/RangeSlider", "Range Slider");
        }

        [MenuItem("GameObject/MaterialUI/Vector Image", false, 014)]
        private static void CreateVectorImage()
        {
            CreateInstance("Components/VectorImage", "Icon");
        }

        [MenuItem("GameObject/MaterialUI/Buttons/Button", false, 030)]
        private static void CreateDefaultButton()
        {
            CreateInstance("Components/Buttons/Button", "Button");
        }

        [MenuItem("GameObject/MaterialUI/Buttons/Button Outline", false, 030)]
        private static void CreateDefaultButtonOutline()
        {
            CreateInstance("Components/Buttons/Button Outline", "Button - Outline");
        }

        [MenuItem("GameObject/MaterialUI/Buttons/Icon Button ", false, 030)]
        private static void CreateIconButton()
        {
            CreateInstance("Components/Buttons/Icon Button", "Icon Button");
        }

        [MenuItem("GameObject/MaterialUI/Buttons/Icon Button Outline ", false, 030)]
        private static void CreateIconButtonOutline()
        {
            CreateInstance("Components/Buttons/Icon Button Outline", "Icon Button Outline");
        }

        [MenuItem("GameObject/MaterialUI/Dropdowns/Drowdown", false, 030)]
        private static void CreateDefaultDropdown()
        {
            CreateInstance("Components/Dropdowns/Dropdown", "Dropdown");
        }

        [MenuItem("GameObject/MaterialUI/Dropdowns/MultiDropdown", false, 030)]
        private static void CreateMultiDropdown()
        {
            CreateInstance("Components/Dropdowns/MultiDropdown", "MultiDropdown");
        }

        [MenuItem("GameObject/MaterialUI/Dropdowns/MenuSpinner", false, 030)]
        private static void CreateMenuSpinner()
        {
            CreateInstance("Components/Dropdowns/MenuSpinner", "MenuSpinner");
        }

        [MenuItem("GameObject/MaterialUI/Toggles/Checkbox", false, 040)]
        private static void CreateCheckboxText()
        {
            CreateInstance("Components/Toggles/Checkbox", "Checkbox");
        }

        [MenuItem("GameObject/MaterialUI/Toggles/Switch", false, 050)]
        private static void CreateSwitchLabel()
        {
            CreateInstance("Components/Toggles/Switch", "Switch");
        }

        [MenuItem("GameObject/MaterialUI/Toggles/Radio Button", false, 060)]
        private static void CreateRadioButtonsLabel()
        {
            CreateInstance("Components/Toggles/RadioButton", "Radio Button");
        }

        [MenuItem("GameObject/MaterialUI/InputFields/Basic", false, 070)]
        private static void CreateSimpleInputFieldBasic()
        {
            CreateInstance("Components/InputFields/InputField", "InputField");
        }

        [MenuItem("GameObject/MaterialUI/InputFields/Outline", false, 070)]
        private static void CreateSimpleInputFieldOutline()
        {
            CreateInstance("Components/InputFields/InputField Outline", "InputField - Outline");
        }

        [MenuItem("GameObject/MaterialUI/InputFields/Filled", false, 070)]
        private static void CreateSimpleInputFieldFilled()
        {
            CreateInstance("Components/InputFields/InputField Filled", "InputField - Filled");
        }

        [MenuItem("GameObject/MaterialUI/InputFields/Filled PermaHint", false, 070)]
        private static void CreateSimpleInputFieldFilledPermaHint()
        {
            CreateInstance("Components/InputFields/InputField Filled PermaHint", "InputField - Filled PermaHint");
        }

        [MenuItem("GameObject/MaterialUI/Progress Indicators/Circle", false, 082)]
        private static void CreateProgressCircle()
        {
            CreateInstance("Resources/Progress Indicators/Circle Progress Indicator", "Circle Progress Indicator");
        }

        [MenuItem("GameObject/MaterialUI/Progress Indicators/Linear", false, 082)]
        private static void CreateProgressLinear()
        {
            CreateInstance("Resources/Progress Indicators/Linear Progress Indicator", "Linear Progress Indicator");
        }

        [MenuItem("GameObject/MaterialUI/Divider", false, 120)]
        private static void CreateDividerHorizontal()
        {
            CreateInstance("Components/Divider", "Divider");
        }

        [MenuItem("GameObject/MaterialUI/Nav Drawer", false, 200)]
        private static void CreateNavDrawer()
        {
            CreateInstance("Components/Nav Drawer", "Nav Drawer");
            m_LastInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(m_LastInstance.GetComponent<RectTransform>().sizeDelta.x, 8f);
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(-m_LastInstance.GetComponent<RectTransform>().sizeDelta.x / 2f, 0f);
        }

        [MenuItem("GameObject/MaterialUI/App Bar", false, 210)]
        private static void CreateAppBar()
        {
            CreateInstance("Components/App Bar", "App Bar");
            m_LastInstance.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        [MenuItem("GameObject/MaterialUI/TabViews/Basic", false, 210)]
        private static void CreateTabView()
        {
            CreateInstance("Components/TabViews/TabView", "Tab View");
        }

        [MenuItem("GameObject/MaterialUI/TabViews/Text Only", false, 210)]
        private static void CreateTabViewText()
        {
            CreateInstance("Components/TabViews/TabView TextOnly", "Tab View - Text Only");
        }

        [MenuItem("GameObject/MaterialUI/Screens/Screen View", false, 220)]
        private static void CreateScreenView()
        {
            CreateInstance("Components/ScreenView", "Screen View");
            m_LastInstance.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        [MenuItem("GameObject/MaterialUI/Screens/Screen", false, 220)]
        private static void CreateScreen()
        {
            CreateInstance("Components/Screen", "Screen");
            m_LastInstance.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        [MenuItem("GameObject/MaterialUI/Managers/Toast Manager", false, 1000)]
        private static void CreateToastManager()
        {
            CreateInstance("Managers/ToastManager", "Toast Manager");
            m_LastInstance.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        [MenuItem("GameObject/MaterialUI/Managers/Snackbar Manager", false, 1000)]
        private static void CreateSnackbarManager()
        {
            CreateInstance("Managers/SnackbarManager", "Snackbar Manager");
            m_LastInstance.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        [MenuItem("GameObject/MaterialUI/Managers/Dialog Manager", false, 1000)]
        private static void CreateDialogManager()
        {
            CreateInstance("Managers/DialogManager", "Dialog Manager");
            m_LastInstance.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            m_LastInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        #endregion

        #region Tools

        [MenuItem("GameObject/MaterialUI/Optimize Hierarchy Drawcalls", true)]
        [MenuItem("Assets/MaterialUI/Optimize Hierarchy Drawcalls", true)]
        static bool ValidateOptimizeImageDrawcalls()
        {
            // Return false if no GameObject is selected or if more then one is selected
            return Selection.gameObjects != null && Selection.gameObjects.Length == 1 && Selection.activeGameObject != null;
        }

        static Sprite s_FillSquareSprite = null;
        [MenuItem("GameObject/MaterialUI/Optimize Hierarchy Drawcalls", false, 3000)]
        [MenuItem("Assets/MaterialUI/Optimize Hierarchy Drawcalls", false, 3000)]
        static void OptimizeImageDrawcalls()
        {
            if (s_FillSquareSprite == null)
            {
                var defaultImagesAPIFolder = GetSubfolderInAPI("DefaultAssets/Images/Fills");
                var fillSquarePath = System.IO.Path.Combine(defaultImagesAPIFolder, "Fill Square.psd").Replace("\\", "/");
                s_FillSquareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(fillSquarePath);
            }

            var count = 0;
            if (s_FillSquareSprite != null)
            {
                var sceneChanged = false;
                var selectedGameObjects = Selection.gameObjects;
                foreach (var gameObject in selectedGameObjects)
                {
                    var images = gameObject.GetComponentsInChildren<Image>(true);
                    var changed = false;
                    foreach (var image in images)
                    {
                        if (image.sprite == null)
                        {
                            var serializedObject = new SerializedObject(image);
                            var spriteProp = serializedObject.FindProperty("m_Sprite");
                            spriteProp.objectReferenceValue = s_FillSquareSprite;
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(image);
                            changed = true;
                            count++;
                        }
                    }
                    if (changed)
                        EditorUtility.SetDirty(gameObject);

                    sceneChanged = changed || sceneChanged;
                }

                if (sceneChanged)
                {
                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null)
                        EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                }
            }

            Debug.Log("Optimized Images: " + count);
        }

        [MenuItem("Window/MaterialUI/Import Essential Resources", false, 2050)]
        public static void ImportEssentialResources()
        {
            var rootAPIFolder = GetSubfolderInAPI(string.Empty);
            var files = System.IO.Directory.GetFiles(rootAPIFolder, "MaterialUIEssentials_*.unitypackage");
            if (files.Length > 0)
            {
                AssetDatabase.ImportPackage(files[0], true);
            }
        }

        [MenuItem("GameObject/MaterialUI/Tools/Attach and Setup Shadow", true, 3000)]
        public static bool CheckAttachAndSetupShadow()
        {
            if (Selection.activeGameObject != null)
            {
                return true;
            }
            return false;
        }

        [MenuItem("GameObject/MaterialUI/Tools/Attach and Setup Shadow", false, 3000)]
        public static void AttachAndSetupShadow()
        {
            GameObject sourceGameObject = Selection.activeGameObject;
            Undo.RecordObject(sourceGameObject, sourceGameObject.name);
            RectTransform sourceRectTransform = sourceGameObject.GetComponent<RectTransform>();
            Vector3 sourcePos = sourceRectTransform.position;
            Vector2 sourceSize = sourceRectTransform.sizeDelta;
            Vector2 sourceLayoutSize = sourceRectTransform.GetProperSize();
            Image sourceImage = sourceGameObject.GetAddComponent<Image>();

            CreateShadow();

            GameObject shadowGameObject = Selection.activeGameObject;
            shadowGameObject.name = sourceGameObject.name + " Shadow";
            ShadowGenerator shadowGenerator = shadowGameObject.GetAddComponent<ShadowGenerator>();
            shadowGenerator.sourceImage = sourceImage;

            RectTransform shadowTransform = shadowGameObject.GetAddComponent<RectTransform>();
            shadowTransform.anchorMin = sourceRectTransform.anchorMin;
            shadowTransform.anchorMax = sourceRectTransform.anchorMax;
            shadowTransform.pivot = sourceRectTransform.pivot;

            bool probablyHasLayout = ((sourceGameObject.GetComponent<ILayoutController>() as Behaviour) != null || (sourceGameObject.GetComponent<ILayoutElement>() as Behaviour) != null);

            GameObject newParentGameObject = new GameObject(sourceGameObject.name);
            newParentGameObject.transform.SetParent(sourceRectTransform.parent);
            RectTransform newParentRectTransform = newParentGameObject.GetAddComponent<RectTransform>();
            newParentRectTransform.SetSiblingIndex(sourceRectTransform.GetSiblingIndex());
            newParentRectTransform.anchorMin = sourceRectTransform.anchorMin;
            newParentRectTransform.anchorMax = sourceRectTransform.anchorMax;
            newParentRectTransform.pivot = sourceRectTransform.pivot;
            newParentRectTransform.position = sourcePos;
            newParentRectTransform.sizeDelta = sourceSize;
            LayoutElement layoutElement = null;

            if (probablyHasLayout)
            {
                layoutElement = newParentGameObject.AddComponent<LayoutElement>();
                layoutElement.preferredWidth = sourceLayoutSize.x;
                layoutElement.preferredHeight = sourceLayoutSize.y;
            }

            shadowGameObject.GetComponent<RectTransform>().SetParent(newParentRectTransform, true);
            sourceRectTransform.SetParent(newParentRectTransform, true);

            sourceGameObject.name = sourceGameObject.name + " Image";

            if (probablyHasLayout)
            {
                layoutElement.CalculateLayoutInputHorizontal();
                layoutElement.CalculateLayoutInputVertical();
            }

            shadowGenerator.GenerateShadowFromImage();

            Selection.activeObject = newParentGameObject;
            Undo.RegisterCreatedObjectUndo(shadowGameObject, shadowGameObject.name);
            Undo.RegisterCreatedObjectUndo(newParentGameObject, newParentGameObject.name);
        }

        //[MenuItem("GameObject/MaterialUI/Shadow", false, 011)]
        private static void CreateShadow()
        {
            CreateInstance("Components/Shadow", "Shadow");
        }

        #endregion
    }
}

#endif