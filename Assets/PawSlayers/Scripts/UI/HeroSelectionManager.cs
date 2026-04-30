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
        public Text selectedCountText;
        public Text infoMessageText;
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
                RefreshUi();
                return;
            }

            if (selectedHeroIds.Contains(heroId))
            {
                selectedHeroIds.Remove(heroId);
                SetInfoMessage(string.Empty);
            }
            else
            {
                if (selectedHeroIds.Count >= 3)
                {
                    SetInfoMessage("Max 3 heroes per run.");
                    RefreshUi();
                    return;
                }

                selectedHeroIds.Add(heroId);
                SetInfoMessage(string.Empty);
            }

            RefreshUi();
        }

        public void StartRun()
        {
            if (selectedHeroIds.Count != 3 || runManager == null)
            {
                return;
            }

            Debug.Log("Selected hero count: " + selectedHeroIds.Count);
            Debug.Log("Selected hero IDs: " + string.Join(", ", selectedHeroIds));
            Debug.Log("Loading BattleScene");
            runManager.StartRunWithSelection(new List<HeroId>(selectedHeroIds));
        }

        public void ContinueRun()
        {
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

            if (runManager.HasSavedRun() && !pendingNewRunConfirmation)
            {
                pendingNewRunConfirmation = true;
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
                    : null;

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
            selectedCountText.text = $"Selected: {selectedHeroIds.Count}/3";
            startRunButton.interactable = selectedHeroIds.Count == 3;
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
            RectTransform root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            if (newRunButton == null)
            {
                newRunButton = CreateButton("NewRunButton", root, new Vector2(-260f, 20f), new Vector2(180f, 54f), "New Run");
            }

            if (continueRunButton == null)
            {
                continueRunButton = CreateButton("ContinueRunButton", root, new Vector2(-460f, 20f), new Vector2(180f, 54f), "Continue Run");
            }

            if (deleteSavedRunButton == null)
            {
                deleteSavedRunButton = CreateButton("DeleteRunSaveButton", root, new Vector2(-660f, 20f), new Vector2(180f, 54f), "Delete Saved Run");
            }

            if (savedRunSummaryText == null)
            {
                savedRunSummaryText = CreateText("SavedRunSummaryText", root, new Vector2(980f, -20f), new Vector2(360f, 60f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
                savedRunSummaryText.text = "No saved run.";
            }
        }

        private Button CreateButton(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, string label)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.38f, 0.55f, 0.33f, 1f);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text labelText = CreateText("Label", rect, new Vector2(20f, -14f), new Vector2(size.x - 40f, 30f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            labelText.text = label;

            return buttonObject.GetComponent<Button>();
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
