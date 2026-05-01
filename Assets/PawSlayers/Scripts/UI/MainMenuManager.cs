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
            Debug.Log("Back clicked.");
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
                saveSummaryText.color = hasSavedRun ? new Color(0.98f, 0.95f, 0.86f, 1f) : new Color(0.72f, 0.74f, 0.76f, 1f);
            }

            SetPanelActive(newRunConfirmPanel, false);
            SetPanelActive(settingsPanel, false);
            StyleButton(newRunButton, new Color(0.76f, 0.57f, 0.18f, 1f), false);
            StyleButton(continueRunButton, new Color(0.47f, 0.35f, 0.18f, 1f), false);
            StyleButton(heroProgressionButton, new Color(0.41f, 0.31f, 0.16f, 1f), false);
            StyleButton(settingsButton, new Color(0.35f, 0.31f, 0.27f, 1f), false);
            StyleButton(quitButton, new Color(0.46f, 0.20f, 0.18f, 1f), true);
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
            EnsureMenuBackground(root);

            if (titleText == null)
            {
                titleText = CreateText("Title", root, new Vector2(0f, -118f), new Vector2(800f, 64f), 54, FontStyle.Bold, TextAnchor.MiddleCenter);
                SetCenteredRect(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(800f, 64f));
                titleText.text = "Paw Slayers";
                titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            }

            if (subtitleText == null)
            {
                subtitleText = CreateText("Subtitle", root, new Vector2(0f, -182f), new Vector2(800f, 36f), 24, FontStyle.Italic, TextAnchor.MiddleCenter);
                SetCenteredRect(subtitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -182f), new Vector2(800f, 36f));
                subtitleText.text = "A cozy roguelite deckbuilder";
                subtitleText.color = new Color(0.84f, 0.86f, 0.80f, 1f);
            }

            EnsureMainPanel(root);

            RectTransform buttonStack = FindChildRect(root, "ButtonStack");
            if (buttonStack == null)
            {
                GameObject stackObject = CreateUiObject("ButtonStack", root, new Vector2(380f, 420f));
                buttonStack = stackObject.GetComponent<RectTransform>();
                SetCenteredRect(buttonStack, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(380f, 420f));

                VerticalLayoutGroup layout = stackObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 16f;
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
                saveSummaryText = CreateText("SaveSummaryText", root, new Vector2(0f, 232f), new Vector2(560f, 52f), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
                SetCenteredRect(saveSummaryText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 210f), new Vector2(560f, 52f));
            }

            if (infoMessageText == null)
            {
                infoMessageText = CreateText("InfoMessageText", root, new Vector2(0f, -320f), new Vector2(760f, 44f), 18, FontStyle.Normal, TextAnchor.MiddleCenter);
                SetCenteredRect(infoMessageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -320f), new Vector2(760f, 44f));
                infoMessageText.color = new Color(0.86f, 0.56f, 0.46f, 1f);
            }

            if (versionText == null)
            {
                versionText = CreateText("VersionText", root, new Vector2(-24f, 24f), new Vector2(220f, 28f), 16, FontStyle.Normal, TextAnchor.LowerRight);
                versionText.rectTransform.anchorMin = new Vector2(1f, 0f);
                versionText.rectTransform.anchorMax = new Vector2(1f, 0f);
                versionText.rectTransform.pivot = new Vector2(1f, 0f);
                versionText.rectTransform.anchoredPosition = new Vector2(-24f, 24f);
                versionText.text = "Prototype v0.1";
                versionText.color = new Color(0.74f, 0.77f, 0.78f, 1f);
            }

            titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            subtitleText.color = new Color(0.84f, 0.86f, 0.80f, 1f);
            versionText.color = new Color(0.74f, 0.77f, 0.78f, 1f);

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

            GameObject box = CreatePanel("ConfirmBox", overlay.transform as RectTransform, new Color(0.96f, 0.92f, 0.84f, 1f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            SetCenteredRect(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 240f));

            Text prompt = CreateText("Prompt", boxRect, new Vector2(30f, -36f), new Vector2(620f, 80f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            prompt.text = "Starting a new run will overwrite your current saved run. Continue?";
            prompt.color = new Color(0.18f, 0.15f, 0.12f, 1f);

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
            StyleButton(confirmNewRunButton, new Color(0.76f, 0.57f, 0.18f, 1f), false);
            StyleButton(cancelNewRunButton, new Color(0.40f, 0.33f, 0.27f, 1f), false);
        }

        private void EnsureSettingsPanel(RectTransform root)
        {
            if (settingsPanel != null)
            {
                return;
            }

            GameObject overlay = CreatePanel("SettingsPanel", root, new Color(0f, 0f, 0f, 0.45f));
            StretchFull(overlay.GetComponent<RectTransform>(), 0f);

            GameObject box = CreatePanel("SettingsBox", overlay.transform as RectTransform, new Color(0.96f, 0.92f, 0.84f, 1f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            SetCenteredRect(boxRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 260f));

            Text title = CreateText("SettingsTitle", boxRect, new Vector2(24f, -24f), new Vector2(260f, 36f), 30, FontStyle.Bold, TextAnchor.UpperLeft);
            title.text = "Settings";
            Text body = CreateText("SettingsBody", boxRect, new Vector2(24f, -72f), new Vector2(500f, 32f), 20, FontStyle.Normal, TextAnchor.UpperLeft);
            body.text = "Audio and gameplay settings coming soon.";
            CreateText("SettingsRow1", boxRect, new Vector2(24f, -118f), new Vector2(500f, 24f), 18, FontStyle.Normal, TextAnchor.UpperLeft).text = "Music Volume  -  Coming soon";
            CreateText("SettingsRow2", boxRect, new Vector2(24f, -146f), new Vector2(500f, 24f), 18, FontStyle.Normal, TextAnchor.UpperLeft).text = "SFX Volume  -  Coming soon";
            CreateText("SettingsRow3", boxRect, new Vector2(24f, -174f), new Vector2(500f, 24f), 18, FontStyle.Normal, TextAnchor.UpperLeft).text = "Screen Shake  -  Coming soon";

            settingsBackButton = CreateButton("SettingsBackButton", boxRect, "Back", new Vector2(180f, 52f));
            RectTransform backRect = settingsBackButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 26f);

            settingsPanel = overlay;
            settingsPanel.SetActive(false);
            StyleButton(settingsBackButton, new Color(0.47f, 0.35f, 0.18f, 1f), false);
        }

        private void EnsureMenuBackground(RectTransform root)
        {
            Image rootImage = root.GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = root.gameObject.AddComponent<Image>();
            }

            rootImage.color = new Color(0.10f, 0.15f, 0.12f, 1f);
        }

        private void EnsureMainPanel(RectTransform root)
        {
            RectTransform panel = FindChildRect(root, "MainPanel");
            if (panel != null)
            {
                return;
            }

            GameObject panelObject = CreatePanel("MainPanel", root, new Color(0.26f, 0.22f, 0.17f, 0.84f));
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            SetCenteredRect(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(520f, 580f));
            panelObject.transform.SetAsFirstSibling();
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
            image.color = new Color(0.47f, 0.35f, 0.18f, 1f);

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
            labelText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            return button;
        }

        private void StyleButton(Button button, Color normalColor, bool isDanger)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = normalColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = normalColor * 1.12f;
            colors.pressedColor = normalColor * 0.9f;
            colors.disabledColor = new Color(0.40f, 0.40f, 0.40f, 0.85f);
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = isDanger
                    ? new Color(1f, 0.93f, 0.90f, 1f)
                    : new Color(0.98f, 0.95f, 0.86f, 1f);
            }
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
