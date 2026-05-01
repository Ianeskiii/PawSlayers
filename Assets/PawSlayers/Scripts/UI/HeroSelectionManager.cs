using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroSelectionManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public RunManager runManager;
        public Transform heroCardContainer;
        public HeroSelectionCardView heroCardPrefab;

        [Header("UI")]
        public RectTransform screenRoot;
        public Text titleText;
        public Text subtitleText;
        public Text selectedCountText;
        public Text infoMessageText;
        public Text currentPartyTitleText;
        public Text currentPartyText;
        public Button startRunButton;
        public Button resetProgressionButton;
        public Button continueRunButton;
        public Button deleteSavedRunButton;
        public Button newRunButton;
        public Text savedRunSummaryText;

        private readonly List<HeroId> selectedHeroIds = new List<HeroId>();
        private readonly Dictionary<HeroId, HeroSelectionCardView> cardViews = new Dictionary<HeroId, HeroSelectionCardView>();
        private bool pendingNewRunConfirmation;

        private void Start()
        {
            if (runManager == null)
            {
                runManager = RunManager.Instance;
            }

            if (runManager == null)
            {
                GameObject runManagerObject = new GameObject("RunManager");
                runManager = runManagerObject.AddComponent<RunManager>();
                runManagerObject.AddComponent<DeckManager>();
            }

            runManager.EnsurePrototypeData();
            EnsureRuntimeUi();
            Debug.Log("Hero Selection loaded.");

            if (runManager.heroDatabase == null)
            {
                if (infoMessageText != null)
                {
                    infoMessageText.text = "RunManager or HeroDatabase is missing.";
                }

                if (startRunButton != null)
                {
                    startRunButton.interactable = false;
                }

                return;
            }

            BuildRosterUi();
            BindDebugButtons();
            RefreshUi();
        }

        public void ToggleHeroSelection(HeroId heroId)
        {
            if (runManager != null && runManager.ProgressionManager != null && !runManager.ProgressionManager.IsHeroUnlocked(heroId))
            {
                SetInfoMessage("Locked");
                Debug.Log("Hero selection blocked: " + heroId + " is locked.");
                RefreshUi();
                return;
            }

            if (selectedHeroIds.Contains(heroId))
            {
                selectedHeroIds.Remove(heroId);
                Debug.Log("Hero deselected: " + heroId);
                SetInfoMessage(string.Empty);
            }
            else
            {
                if (selectedHeroIds.Count >= 3)
                {
                    SetInfoMessage("Max 3 heroes per run.");
                    Debug.Log("Hero selection blocked: max 3 heroes.");
                    RefreshUi();
                    return;
                }

                selectedHeroIds.Add(heroId);
                Debug.Log("Hero selected: " + heroId);
                SetInfoMessage(string.Empty);
            }

            RefreshUi();
        }

        public void StartRun()
{
    Debug.Log("Start Run clicked.");

    if (runManager == null)
    {
        runManager = RunManager.Instance;
    }

    if (runManager == null)
    {
        runManager = FindObjectOfType<RunManager>();
    }

    if (runManager == null)
    {
        GameObject runManagerObject = new GameObject("RunManager");
        runManager = runManagerObject.AddComponent<RunManager>();

        if (runManagerObject.GetComponent<DeckManager>() == null)
        {
            runManagerObject.AddComponent<DeckManager>();
        }

        if (runManagerObject.GetComponent<HeroProgressionManager>() == null)
        {
            runManagerObject.AddComponent<HeroProgressionManager>();
        }

        if (runManagerObject.GetComponent<RunSaveManager>() == null)
        {
            runManagerObject.AddComponent<RunSaveManager>();
        }

        Debug.LogWarning("RunManager was missing, so one was created at runtime.");
    }

    runManager.EnsurePrototypeData();

    if (selectedHeroIds.Count != 3)
    {
        Debug.LogWarning("Start Run blocked: select exactly 3 heroes first. Current count: " + selectedHeroIds.Count);
        SetInfoMessage("Select exactly 3 heroes first.");
        RefreshUi();
        return;
    }

    Debug.Log("Selected hero count: " + selectedHeroIds.Count);
    Debug.Log("Selected hero IDs: " + string.Join(", ", selectedHeroIds));
    Debug.Log("Loading Battle scene.");

    runManager.StartRunWithSelection(new List<HeroId>(selectedHeroIds));
}

        public void ContinueRun()
        {
            Debug.Log("Continue Run clicked.");
            if (runManager == null || !runManager.HasSavedRun())
            {
                SetInfoMessage("No saved run.");
                RefreshUi();
                return;
            }

            runManager.ContinueSavedRun();
        }

        public void StartNewRunFlow()
        {
            if (runManager == null)
            {
                return;
            }

            Debug.Log("New Run clicked.");

            if (runManager.HasSavedRun() && !pendingNewRunConfirmation)
            {
                pendingNewRunConfirmation = true;
                Debug.Log("Existing save detected.");
                SetInfoMessage("Starting a new run will overwrite your current saved run. Continue?");
                return;
            }

            pendingNewRunConfirmation = false;
            runManager.DeleteCurrentRunSave();
            selectedHeroIds.Clear();
            SetInfoMessage("New run ready.");
            RefreshUi();
        }

        public void DeleteSavedRun()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.DeleteCurrentRunSave();
            Debug.Log("Current run save deleted.");
            pendingNewRunConfirmation = false;
            SetInfoMessage("Saved run deleted.");
            RefreshUi();
        }

        private void BuildRosterUi()
        {
            if (heroCardContainer == null)
            {
                return;
            }

            cardViews.Clear();

            foreach (Transform child in heroCardContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (HeroData hero in runManager.heroDatabase.heroes)
            {
                if (hero == null || hero.heroId == HeroId.Neutral)
                {
                    continue;
                }

                HeroSelectionCardView cardView = heroCardPrefab != null
                    ? Instantiate(heroCardPrefab, heroCardContainer)
                    : CreateRuntimeHeroCard(heroCardContainer);

                if (cardView == null)
                {
                    continue;
                }

                cardView.Setup(hero, this, runManager.ProgressionManager, false);
                cardViews[hero.heroId] = cardView;
            }

            startRunButton.onClick.RemoveAllListeners();
            startRunButton.onClick.AddListener(StartRun);
        }

        private void BindDebugButtons()
        {
            if (resetProgressionButton == null)
            {
                BindRunButtons();
                return;
            }

            resetProgressionButton.onClick.RemoveAllListeners();
            resetProgressionButton.onClick.AddListener(ResetProgressionForTesting);
            BindRunButtons();
        }

        private void RefreshUi()
        {
            if (selectedCountText != null)
            {
                selectedCountText.text = $"Selected: {selectedHeroIds.Count}/3";
                selectedCountText.color = selectedHeroIds.Count == 3
                    ? new Color(0.24f, 0.46f, 0.27f, 1f)
                    : new Color(0.98f, 0.92f, 0.76f, 1f);
            }

            if (startRunButton != null)
            {
                startRunButton.interactable = selectedHeroIds.Count == 3;
            }

            Debug.Log("Selected hero count: " + selectedHeroIds.Count);

            foreach (KeyValuePair<HeroId, HeroSelectionCardView> pair in cardViews)
            {
                HeroProgressionManager progressionManager = runManager != null ? runManager.ProgressionManager : null;
                bool isUnlocked = progressionManager == null || progressionManager.IsHeroUnlocked(pair.Key);
                pair.Value.RefreshProgression(progressionManager);
                pair.Value.SetSelected(isUnlocked && selectedHeroIds.Contains(pair.Key));
                pair.Value.SetLocked(!isUnlocked);
            }

            bool hasSavedRun = runManager != null && runManager.HasSavedRun();
            if (continueRunButton != null)
            {
                continueRunButton.interactable = hasSavedRun;
            }

            if (deleteSavedRunButton != null)
            {
                deleteSavedRunButton.interactable = hasSavedRun;
            }

            if (savedRunSummaryText != null)
            {
                savedRunSummaryText.text = hasSavedRun && runManager.SaveManager != null
                    ? BuildSavedRunSummary()
                    : "No saved run.";
                savedRunSummaryText.color = hasSavedRun
                    ? new Color(0.98f, 0.95f, 0.86f, 1f)
                    : new Color(0.70f, 0.72f, 0.74f, 1f);
            }

            if (currentPartyText != null)
            {
                currentPartyText.text = BuildCurrentPartySummary();
            }
        }

        private void SetInfoMessage(string message)
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = message;
            }
        }

        private void ResetProgressionForTesting()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.ResetHeroProgressionForTesting();
            BuildRosterUi();
            selectedHeroIds.Clear();
            SetInfoMessage("Hero progression reset.");
            RefreshUi();
        }

        private void BindRunButtons()
        {
            if (startRunButton != null)
            {
                startRunButton.onClick.RemoveAllListeners();
                startRunButton.onClick.AddListener(StartRun);
            }

            if (continueRunButton != null)
            {
                continueRunButton.onClick.RemoveAllListeners();
                continueRunButton.onClick.AddListener(ContinueRun);
            }

            if (deleteSavedRunButton != null)
            {
                deleteSavedRunButton.onClick.RemoveAllListeners();
                deleteSavedRunButton.onClick.AddListener(DeleteSavedRun);
            }

            if (newRunButton != null)
            {
                newRunButton.onClick.RemoveAllListeners();
                newRunButton.onClick.AddListener(StartNewRunFlow);
            }
        }

        private string BuildSavedRunSummary()
        {
            RunSaveData saveData = runManager.SaveManager != null ? runManager.SaveManager.LoadCurrentRun() : null;
            if (saveData == null || !saveData.hasActiveRun)
            {
                return "No saved run.";
            }

            return $"Saved Run: Battle {Mathf.Max(1, saveData.currentBattleIndex)}, Gold {saveData.gold}, Relics {saveData.ownedRelics.Count}";
        }

        private void EnsureRuntimeUi()
        {
            RectTransform root = screenRoot;
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

                GameObject rootObject = new GameObject("SelectionRoot", typeof(RectTransform), typeof(Image));
                rootObject.transform.SetParent(canvas.transform, false);
                root = rootObject.GetComponent<RectTransform>();
                StretchFull(root, 20f);
                rootObject.GetComponent<Image>().color = new Color(0.10f, 0.15f, 0.12f, 1f);
            }

            screenRoot = root;
            EnsurePanelBackground(root);

            if (titleText == null)
            {
                titleText = CreateText("TitleText", root, new Vector2(44f, -36f), new Vector2(560f, 42f), 40, FontStyle.Bold, TextAnchor.UpperLeft);
                titleText.text = "Choose Your Party";
                titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            }

            if (subtitleText == null)
            {
                subtitleText = CreateText("SubtitleText", root, new Vector2(46f, -84f), new Vector2(620f, 26f), 20, FontStyle.Italic, TextAnchor.UpperLeft);
                subtitleText.text = "Select exactly 3 heroes for this run";
                subtitleText.color = new Color(0.82f, 0.84f, 0.80f, 1f);
            }

            if (selectedCountText == null)
            {
                selectedCountText = CreateText("SelectedCountText", root, new Vector2(46f, -126f), new Vector2(220f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            }

            if (infoMessageText == null)
            {
                infoMessageText = CreateText("InfoMessageText", root, new Vector2(280f, -128f), new Vector2(520f, 28f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
                infoMessageText.color = new Color(0.80f, 0.43f, 0.38f, 1f);
            }

            if (newRunButton == null)
            {
                newRunButton = CreateButton("NewRunButton", root, new Vector2(-42f, -40f), new Vector2(170f, 48f), "New Run");
            }

            if (continueRunButton == null)
            {
                continueRunButton = CreateButton("ContinueRunButton", root, new Vector2(-228f, -40f), new Vector2(170f, 48f), "Continue Run");
            }

            if (deleteSavedRunButton == null)
            {
                deleteSavedRunButton = CreateButton("DeleteRunSaveButton", root, new Vector2(-414f, -40f), new Vector2(170f, 48f), "Delete Save");
            }

            if (savedRunSummaryText == null)
            {
                savedRunSummaryText = CreateText("SavedRunSummaryText", root, new Vector2(-44f, -94f), new Vector2(420f, 26f), 16, FontStyle.Normal, TextAnchor.UpperRight);
                savedRunSummaryText.rectTransform.anchorMin = new Vector2(1f, 1f);
                savedRunSummaryText.rectTransform.anchorMax = new Vector2(1f, 1f);
                savedRunSummaryText.rectTransform.pivot = new Vector2(1f, 1f);
                savedRunSummaryText.rectTransform.anchoredPosition = new Vector2(-44f, -94f);
                savedRunSummaryText.text = "No saved run.";
            }

            titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            subtitleText.color = new Color(0.82f, 0.84f, 0.80f, 1f);
            if (infoMessageText != null)
            {
                infoMessageText.color = new Color(0.86f, 0.56f, 0.46f, 1f);
            }

            if (heroCardContainer == null)
            {
                GameObject rosterPanel = CreatePanel("RosterPanel", root, new Vector2(44f, -170f), new Vector2(880f, 760f), new Color(0.93f, 0.89f, 0.80f, 0.97f));
                RectTransform rosterPanelRect = rosterPanel.GetComponent<RectTransform>();
                rosterPanelRect.anchorMin = new Vector2(0f, 1f);
                rosterPanelRect.anchorMax = new Vector2(0f, 1f);
                rosterPanelRect.pivot = new Vector2(0f, 1f);

                GameObject rosterContainer = new GameObject("HeroCardContainer", typeof(RectTransform), typeof(GridLayoutGroup));
                rosterContainer.transform.SetParent(rosterPanel.transform, false);
                RectTransform rosterRect = rosterContainer.GetComponent<RectTransform>();
                rosterRect.anchorMin = new Vector2(0f, 0f);
                rosterRect.anchorMax = new Vector2(1f, 1f);
                rosterRect.offsetMin = new Vector2(18f, 18f);
                rosterRect.offsetMax = new Vector2(-18f, -18f);
                GridLayoutGroup grid = rosterContainer.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(390f, 210f);
                grid.spacing = new Vector2(18f, 18f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
                heroCardContainer = rosterContainer.transform;
            }
            else
            {
                GridLayoutGroup grid = heroCardContainer.GetComponent<GridLayoutGroup>();
                if (grid == null)
                {
                    VerticalLayoutGroup oldVertical = heroCardContainer.GetComponent<VerticalLayoutGroup>();
                    if (oldVertical != null)
                    {
                        Destroy(oldVertical);
                    }

                    HorizontalLayoutGroup oldHorizontal = heroCardContainer.GetComponent<HorizontalLayoutGroup>();
                    if (oldHorizontal != null)
                    {
                        Destroy(oldHorizontal);
                    }

                    grid = heroCardContainer.gameObject.AddComponent<GridLayoutGroup>();
                }

                grid.cellSize = new Vector2(390f, 210f);
                grid.spacing = new Vector2(18f, 18f);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 2;
                grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            }

            if (currentPartyTitleText == null)
            {
                currentPartyTitleText = CreateText("CurrentPartyTitleText", root, new Vector2(-428f, -170f), new Vector2(280f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
                currentPartyTitleText.rectTransform.anchorMin = new Vector2(1f, 1f);
                currentPartyTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
                currentPartyTitleText.rectTransform.pivot = new Vector2(1f, 1f);
                currentPartyTitleText.rectTransform.anchoredPosition = new Vector2(-428f, -170f);
                currentPartyTitleText.text = "Current Party";
                currentPartyTitleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            }

            if (currentPartyText == null)
            {
                currentPartyText = CreateText("CurrentPartyText", root, new Vector2(-428f, -208f), new Vector2(360f, 220f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
                currentPartyText.rectTransform.anchorMin = new Vector2(1f, 1f);
                currentPartyText.rectTransform.anchorMax = new Vector2(1f, 1f);
                currentPartyText.rectTransform.pivot = new Vector2(1f, 1f);
                currentPartyText.rectTransform.anchoredPosition = new Vector2(-428f, -208f);
                currentPartyText.color = new Color(0.92f, 0.92f, 0.90f, 1f);
            }

            if (startRunButton == null)
            {
                startRunButton = CreateButton("StartRunButton", root, new Vector2(-44f, 40f), new Vector2(240f, 60f), "Start Run");
            }

            if (resetProgressionButton != null)
            {
                StyleButton(resetProgressionButton, new Color(0.52f, 0.22f, 0.20f, 1f), true);
            }

            StyleButton(startRunButton, new Color(0.73f, 0.53f, 0.15f, 1f), false);
            StyleButton(newRunButton, new Color(0.72f, 0.53f, 0.16f, 1f), false);
            StyleButton(continueRunButton, new Color(0.47f, 0.35f, 0.18f, 1f), false);
            StyleButton(deleteSavedRunButton, new Color(0.46f, 0.20f, 0.18f, 1f), true);
        }

        private string BuildCurrentPartySummary()
        {
            if (selectedHeroIds.Count == 0 || runManager == null || runManager.heroDatabase == null)
            {
                return "No heroes selected yet.";
            }

            List<string> lines = new List<string>();
            foreach (HeroId heroId in selectedHeroIds)
            {
                HeroData hero = runManager.heroDatabase.heroes.Find(entry => entry != null && entry.heroId == heroId);
                if (hero != null)
                {
                    lines.Add($"{hero.heroName} - {hero.heroClass}");
                }
            }

            return lines.Count > 0 ? string.Join("\n", lines) : "No heroes selected yet.";
        }

        private void EnsurePanelBackground(RectTransform root)
        {
            Image rootImage = root.GetComponent<Image>();
            if (rootImage == null)
            {
                rootImage = root.gameObject.AddComponent<Image>();
            }

            rootImage.color = new Color(0.10f, 0.15f, 0.12f, 1f);
        }

        private HeroSelectionCardView CreateRuntimeHeroCard(Transform parent)
        {
            GameObject root = new GameObject("HeroSelectionCard", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(390f, 210f);
            Image background = root.GetComponent<Image>();
            background.color = new Color(0.92f, 0.88f, 0.78f, 1f);
            Button button = root.GetComponent<Button>();

            GameObject outline = new GameObject("SelectionOutline", typeof(RectTransform), typeof(Image));
            outline.transform.SetParent(root.transform, false);
            RectTransform outlineRect = outline.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = new Vector2(-3f, -3f);
            outlineRect.offsetMax = new Vector2(3f, 3f);
            Image outlineImage = outline.GetComponent<Image>();
            outlineImage.color = new Color(0f, 0f, 0f, 0f);
            outline.transform.SetAsFirstSibling();

            Image portrait = CreatePanel("Portrait", rect, new Vector2(18f, -20f), new Vector2(96f, 96f), new Color(0.74f, 0.74f, 0.74f, 1f)).GetComponent<Image>();
            Text heroName = CreateText("HeroName", rect, new Vector2(132f, -18f), new Vector2(220f, 26f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            Text heroClass = CreateText("HeroClass", rect, new Vector2(132f, -48f), new Vector2(220f, 22f), 18, FontStyle.Italic, TextAnchor.UpperLeft);
            Text description = CreateText("Description", rect, new Vector2(132f, -142f), new Vector2(230f, 44f), 15, FontStyle.Normal, TextAnchor.UpperLeft);

            HeroSelectionCardView view = root.AddComponent<HeroSelectionCardView>();
            view.heroNameText = heroName;
            view.heroClassText = heroClass;
            view.descriptionText = description;
            view.portraitImage = portrait;
            view.selectionOutline = outlineImage;
            view.backgroundImage = background;
            view.button = button;
            return view;
        }

        private Button CreateButton(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, string label)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.47f, 0.35f, 0.18f, 1f);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text labelText = CreateText("Label", rect, new Vector2(20f, -14f), new Vector2(size.x - 40f, 30f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            labelText.text = label;
            labelText.color = new Color(0.98f, 0.95f, 0.86f, 1f);

            return buttonObject.GetComponent<Button>();
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

        private GameObject CreatePanel(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);
            RectTransform rect = panelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            Image image = panelObject.GetComponent<Image>();
            image.color = color;
            return panelObject;
        }

        private void StretchFull(RectTransform rectTransform, float padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }

        private Text CreateText(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
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
    }
}
