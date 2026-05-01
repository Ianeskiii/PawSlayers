using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PawSlayers
{
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public RunManager runManager;

        [Header("UI")]
        public RectTransform menuRoot;
        public Text titleText;
        public Text subtitleText;
        public Text versionText;
        public Text saveSummaryText;
        public Text infoMessageText;
        public Button newRunButton;
        public Button continueRunButton;
        public Button heroProgressionButton;
        public Button settingsButton;
        public Button quitButton;
        public GameObject newRunConfirmPanel;
        public Button confirmNewRunButton;
        public Button cancelNewRunButton;
        public GameObject settingsPanel;
        public Button settingsBackButton;

        private void Start()
        {
            runManager = ResolveRunManager();
            runManager.EnsurePrototypeData();

            Debug.Log("Main menu loaded.");
            EnsureRuntimeUi();
            BindButtons();
            RefreshUi();
        }

        public void OnNewRunClicked()
        {
            Debug.Log("New Run clicked.");

            if (runManager != null && runManager.HasSavedRun())
            {
                Debug.Log("Existing save detected.");
                SetInfo("Starting a new run will overwrite your current saved run. Continue?");
                SetPanelActive(newRunConfirmPanel, true);
                return;
            }

            ConfirmStartNewRun();
        }

        public void ConfirmStartNewRun()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.DeleteCurrentRunSave();
            Debug.Log("Current run save deleted.");
            runManager.PrepareForNewRun();
            SetPanelActive(newRunConfirmPanel, false);
            runManager.LoadSceneByName(runManager.heroSelectionSceneName);
        }

        public void CancelStartNewRun()
        {
            SetPanelActive(newRunConfirmPanel, false);
            SetInfo(string.Empty);
            RefreshUi();
        }

        public void OnContinueRunClicked()
        {
            Debug.Log("Continue Run clicked.");

            if (runManager == null || !runManager.HasSavedRun())
            {
                SetInfo("No saved run.");
                RefreshUi();
                return;
            }

            bool continued = runManager.ContinueSavedRun();
            if (!continued)
            {
                SetInfo("Could not load saved run.");
                RefreshUi();
            }
            else
            {
                Debug.Log("Save loaded successfully.");
            }
        }

        public void OpenHeroProgression()
        {
            if (runManager == null)
            {
                return;
            }

            Debug.Log("Hero Progression opened.");
            runManager.LoadSceneByName(runManager.heroProgressionSceneName);
        }

        public void OpenSettings()
        {
            Debug.Log("Settings opened.");
            SetPanelActive(settingsPanel, true);
        }

        public void CloseSettings()
        {
            SetPanelActive(settingsPanel, false);
        }

        public void QuitGame()
        {
            Debug.Log("Quit clicked.");
#if UNITY_EDITOR
            Debug.Log("Quit clicked. Application.Quit only works in builds.");
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private RunManager ResolveRunManager()
        {
            if (runManager != null)
            {
                return runManager;
            }

            runManager = RunManager.Instance;
            if (runManager != null)
            {
                return runManager;
            }

            GameObject runManagerObject = new GameObject("RunManager");
            runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();
            return runManager;
        }

        private void BindButtons()
        {
            BindButton(newRunButton, OnNewRunClicked);
            BindButton(continueRunButton, OnContinueRunClicked);
            BindButton(heroProgressionButton, OpenHeroProgression);
            BindButton(settingsButton, OpenSettings);
            BindButton(quitButton, QuitGame);
            BindButton(confirmNewRunButton, ConfirmStartNewRun);
            BindButton(cancelNewRunButton, CancelStartNewRun);
            BindButton(settingsBackButton, CloseSettings);
        }

        private void RefreshUi()
        {
            bool hasSavedRun = runManager != null && runManager.HasSavedRun();

            if (continueRunButton != null)
            {
                continueRunButton.interactable = hasSavedRun;
            }

            if (saveSummaryText != null)
            {
                saveSummaryText.text = hasSavedRun ? BuildSaveSummary() : "No saved run.";
                saveSummaryText.color = hasSavedRun ? Color.black : new Color(0.35f, 0.35f, 0.35f, 1f);
            }

            SetPanelActive(newRunConfirmPanel, false);
            SetPanelActive(settingsPanel, false);
        }

        private string BuildSaveSummary()
        {
            if (runManager == null || runManager.SaveManager == null)
            {
                return "No saved run.";
            }

            RunSaveData saveData = runManager.SaveManager.LoadCurrentRun();
            if (saveData == null || !saveData.hasActiveRun)
            {
                return "No saved run.";
            }

            return $"Saved Run: Battle {Mathf.Max(1, saveData.currentBattleIndex)}, Gold {saveData.gold}, Relics {saveData.ownedRelics.Count}";
        }

        private void SetInfo(string message)
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = message;
            }
        }

        private void EnsureRuntimeUi()
        {
            RectTransform root = menuRoot;
            if (root == null)
            {
                root = transform as RectTransform;
            }

            if (root == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = canvasObject.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                }

                GameObject rootObject = CreateUiObject("MainMenuRoot", canvas.transform as RectTransform, Vector2.zero);
                root = rootObject.GetComponent<RectTransform>();
                StretchFull(root, 24f);
            }

            menuRoot = root;

            if (titleText == null)
            {
                titleText = CreateText("Title", root, new Vector2(0f, -120f), new Vector2(800f, 64f), 48, FontStyle.Bold, TextAnchor.MiddleCenter);
                SetCenteredRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(800f, 64f));
                titleText.text = "Paw Slayers";
            }

            if (subtitleText == null)
            {
                subtitleText = CreateText("Subtitle", root, new Vector2(0f, -182f), new Vector2(800f, 36f), 24, FontStyle.Italic, TextAnchor.MiddleCenter);
                SetCenteredRect(subtitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -182f), new Vector2(800f, 36f));
                subtitleText.text = "Roguelite Deckbuilder Prototype";
            }

            RectTransform buttonStack = FindChildRect(root, "ButtonStack");
            if (buttonStack == null)
            {
                GameObject stackObject = CreateUiObject("ButtonStack", root, new Vector2(360f, 420f));
                buttonStack = stackObject.GetComponent<RectTransform>();
                SetCenteredRect(buttonStack, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(360f, 420f));

                VerticalLayoutGroup layout = stackObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 14f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            if (newRunButton == null)
            {
                newRunButton = CreateButton("NewRunButton", buttonStack, "New Run");
            }

            if (continueRunButton == null)
            {
                continueRunButton = CreateButton("ContinueRunButton", buttonStack, "Continue Run");
            }

            if (heroProgressionButton == null)
            {
                heroProgressionButton = CreateButton("HeroProgressionButton", buttonStack, "Hero Progression");
            }

            if (settingsButton == null)
            {
                settingsButton = CreateButton("SettingsButton", buttonStack, "Settings");
            }

            if (quitButton == null)
            {
                quitButton = CreateButton("QuitButton", buttonStack, "Quit");
            }

            if (saveSummaryText == null)
            {
                saveSummaryText = CreateText("SaveSummaryText", root, new Vector2(0f, 210f), new Vector2(560f, 52f), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
                SetCenteredRect(saveSummaryText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(560f, 52f));
            }

            if (infoMessageText == null)
            {
                infoMessageText = CreateText("InfoMessageText", root, new Vector2(0f, -320f), new Vector2(760f, 44f), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
                SetCenteredRect(infoMessageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f), new Vector2(760f, 44f));
                infoMessageText.color = new Color(0.55f, 0.16f, 0.16f, 1f);
            }

            if (versionText == null)
            {
                versionText = CreateText("VersionText", root, new Vector2(-24f, 24f), new Vector2(220f, 28f), 16, FontStyle.Normal, TextAnchor.LowerRight);
                versionText.rectTransform.anchorMin = new Vector2(1f, 0f);
                versionText.rectTransform.anchorMax = new Vector2(1f, 0f);
                versionText.rectTransform.pivot = new Vector2(1f, 0f);
                versionText.rectTransform.anchoredPosition = new Vector2(-24f, 24f);
                versionText.text = "Prototype v0.1";
            }

            EnsureConfirmationPanel(root);
            EnsureSettingsPanel(root);
        }

        private void EnsureConfirmationPanel(RectTransform root)
        {
            if (newRunConfirmPanel != null)
            {
                return;
            }

            GameObject overlay = CreatePanel("NewRunConfirmPanel", root, new Color(0f, 0f, 0f, 0.5f));
            StretchFull(overlay.GetComponent<RectTransform>(), 0f);

            GameObject box = CreatePanel("ConfirmBox", overlay.transform as RectTransform, new Color(0.96f, 0.94f, 0.88f, 1f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            SetCenteredRect(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 240f));

            Text prompt = CreateText("Prompt", boxRect, new Vector2(30f, -36f), new Vector2(620f, 80f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            prompt.text = "Starting a new run will overwrite your current saved run. Continue?";

            confirmNewRunButton = CreateButton("ConfirmNewRunButton", boxRect, "Yes, Start New Run", new Vector2(260f, 52f));
            confirmNewRunButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-140f, 34f);
            confirmNewRunButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            confirmNewRunButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            confirmNewRunButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

            cancelNewRunButton = CreateButton("CancelNewRunButton", boxRect, "Cancel", new Vector2(180f, 52f));
            cancelNewRunButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(140f, 34f);
            cancelNewRunButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0f);
            cancelNewRunButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0f);
            cancelNewRunButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0f);

            newRunConfirmPanel = overlay;
            newRunConfirmPanel.SetActive(false);
        }

        private void EnsureSettingsPanel(RectTransform root)
        {
            if (settingsPanel != null)
            {
                return;
            }

            GameObject overlay = CreatePanel("SettingsPanel", root, new Color(0f, 0f, 0f, 0.45f));
            StretchFull(overlay.GetComponent<RectTransform>(), 0f);

            GameObject box = CreatePanel("SettingsBox", overlay.transform as RectTransform, new Color(0.92f, 0.95f, 0.96f, 1f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            SetCenteredRect(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 260f));

            CreateText("SettingsTitle", boxRect, new Vector2(24f, -24f), new Vector2(260f, 36f), 30, FontStyle.Bold, TextAnchor.UpperLeft).text = "Settings";
            CreateText("SettingsBody", boxRect, new Vector2(24f, -84f), new Vector2(500f, 56f), 22, FontStyle.Normal, TextAnchor.UpperLeft).text = "Audio settings coming soon";

            settingsBackButton = CreateButton("SettingsBackButton", boxRect, "Back", new Vector2(180f, 52f));
            RectTransform backRect = settingsBackButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 26f);

            settingsPanel = overlay;
            settingsPanel.SetActive(false);
        }

        private void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void SetPanelActive(GameObject panel, bool isActive)
        {
            if (panel != null)
            {
                panel.SetActive(isActive);
            }
        }

        private RectTransform FindChildRect(RectTransform parent, string childName)
        {
            Transform child = parent != null ? parent.Find(childName) : null;
            return child as RectTransform;
        }

        private GameObject CreateUiObject(string objectName, RectTransform parent, Vector2 size)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return gameObject;
        }

        private GameObject CreatePanel(string objectName, RectTransform parent, Color color)
        {
            GameObject panel = CreateUiObject(objectName, parent, Vector2.zero);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private Button CreateButton(string objectName, RectTransform parent, string label, Vector2? sizeOverride = null)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent, sizeOverride ?? new Vector2(320f, 56f));
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.36f, 0.55f, 0.31f, 1f);

            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = buttonObject.GetComponent<RectTransform>().sizeDelta.x;
            layoutElement.preferredHeight = buttonObject.GetComponent<RectTransform>().sizeDelta.y;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.46f, 0.65f, 0.41f, 1f);
            colors.pressedColor = new Color(0.26f, 0.45f, 0.21f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);
            button.colors = colors;

            Text labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), Vector2.zero, buttonObject.GetComponent<RectTransform>().sizeDelta, 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(labelText.rectTransform, 0f);
            labelText.text = label;
            return button;
        }

        private Text CreateText(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(objectName, parent, size);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.black;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private void StretchFull(RectTransform rectTransform, float padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }

        private void SetCenteredRect(RectTransform rectTransform, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
        }
    }
}
