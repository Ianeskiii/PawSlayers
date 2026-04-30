using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class MapManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public RunManager runManager;
        public TreasureManager treasureManager;
        public CardDatabase cardDatabase;
        public CardView cardViewPrefab;

        [Header("Map UI")]
        public Text titleText;
        public Text progressText;
        public Text infoText;
        public Text relicsText;
        public Button battleNodeButton;
        public Button treasureNodeButton;
        public Button campfireNodeButton;
        public Button bossNodeButton;

        [Header("Treasure UI")]
        public GameObject treasurePanel;
        public Transform treasureCardContainer;
        public Text treasureTitleText;
        public Text treasureInfoText;
        public Button treasureContinueButton;

        [Header("Campfire UI")]
        public GameObject campfirePanel;
        public Text campfireTitleText;
        public Text campfireInfoText;
        public Button restButton;
        public Button upgradeButton;
        public Button campfireContinueButton;
        public Transform campfireUpgradeContainer;

        private bool treasureChoiceLocked;
        private bool treasureResolved;
        private bool campfireResolved;

        private void Start()
        {
            EnsureRunManager();
            EnsureRuntimeUi();
            EnsureTreasureManager();
            BindButtons();
            RefreshMapUi();
        }

        public void RefreshMapUi()
        {
            if (runManager == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = "Dungeon Map";
            }

            if (progressText != null)
            {
                progressText.text = GetProgressText();
            }

            if (relicsText != null)
            {
                relicsText.text = runManager.GetResourceSummaryText();
            }

            if (treasurePanel != null)
            {
                treasurePanel.SetActive(false);
            }

            if (campfirePanel != null)
            {
                campfirePanel.SetActive(false);
            }

            List<MapNodeType> availableNodes = runManager.GetAvailableMapNodes();
            Debug.Log("Entered map.");
            Debug.Log("Available nodes: " + string.Join(", ", availableNodes));
            Debug.Log("Current battle index: " + runManager.CurrentBattleIndex);
            Debug.Log("Deck count: " + runManager.CurrentRunDeck.Count);

            SetButtonState(battleNodeButton, availableNodes.Contains(MapNodeType.Battle));
            SetButtonState(treasureNodeButton, availableNodes.Contains(MapNodeType.Treasure));
            SetButtonState(campfireNodeButton, availableNodes.Contains(MapNodeType.Campfire));
            SetButtonState(bossNodeButton, availableNodes.Contains(MapNodeType.Boss));

            if (infoText != null && string.IsNullOrWhiteSpace(infoText.text))
            {
                infoText.text = "Choose your next node.";
            }
        }

        private void EnsureRunManager()
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

            runManager.EnsureDirectBattleTestState();

            if (cardDatabase == null)
            {
                cardDatabase = runManager.cardDatabase;
            }
        }

        private void EnsureTreasureManager()
        {
            if (treasureManager == null)
            {
                treasureManager = GetComponent<TreasureManager>();
            }

            if (treasureManager == null)
            {
                treasureManager = gameObject.AddComponent<TreasureManager>();
            }

            treasureManager.Initialize(runManager, cardDatabase);
        }

        private void BindButtons()
        {
            BindButton(battleNodeButton, OnBattleNodeClicked);
            BindButton(treasureNodeButton, OnTreasureNodeClicked);
            BindButton(campfireNodeButton, OnCampfireNodeClicked);
            BindButton(bossNodeButton, OnBossNodeClicked);
            BindButton(treasureContinueButton, CloseTreasurePanel);
            BindButton(restButton, OnRestClicked);
            BindButton(upgradeButton, OnUpgradeCardClicked);
            BindButton(campfireContinueButton, CloseCampfirePanel);
        }

        private void OnBattleNodeClicked()
        {
            if (runManager == null || !runManager.GetAvailableMapNodes().Contains(MapNodeType.Battle))
            {
                return;
            }

            if (infoText != null)
            {
                infoText.text = "Heading to the next battle.";
            }

            runManager.StartNormalBattleFromMap();
        }

        private void OnBossNodeClicked()
        {
            if (runManager == null || !runManager.GetAvailableMapNodes().Contains(MapNodeType.Boss))
            {
                return;
            }

            if (infoText != null)
            {
                infoText.text = "Heading to the boss.";
            }

            runManager.StartBossBattleFromMap();
        }

        private void OnCampfireNodeClicked()
        {
            if (runManager == null || !runManager.GetAvailableMapNodes().Contains(MapNodeType.Campfire))
            {
                return;
            }

            campfireResolved = false;
            ShowCampfirePanel();
            Debug.Log("Selected node: Campfire");
        }

        private void OnRestClicked()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.ResolveCampfireNode();
            campfireResolved = true;
            ShowCampfireResult("Party rested.");
        }

        private void OnUpgradeCardClicked()
        {
            if (runManager == null)
            {
                return;
            }

            List<RuntimeCardState> upgradeableCards = runManager.GetUpgradeableCards();
            ClearChildren(campfireUpgradeContainer);

            if (upgradeableCards.Count == 0)
            {
                runManager.ResolveCampfireUpgradeNode();
                campfireResolved = true;
                ShowCampfireResult("No cards available to upgrade.");
                return;
            }

            if (campfireInfoText != null)
            {
                campfireInfoText.text = "Choose 1 card to upgrade.";
            }

            SetCampfireActionButtons(false);

            foreach (RuntimeCardState card in upgradeableCards)
            {
                CardView cardView = cardViewPrefab != null
                    ? Instantiate(cardViewPrefab, campfireUpgradeContainer)
                    : CreateRuntimeRewardCardView(campfireUpgradeContainer);

                cardView.Setup(card, SelectCampfireUpgrade, card.BuildUpgradePreview());
                cardView.SetDisabled(false, string.Empty);
            }

            if (campfireContinueButton != null)
            {
                campfireContinueButton.gameObject.SetActive(false);
            }
        }

        private void SelectCampfireUpgrade(RuntimeCardState card)
        {
            if (runManager == null || card == null)
            {
                return;
            }

            bool upgraded = runManager.UpgradeCard(card);
            ClearChildren(campfireUpgradeContainer);
            campfireResolved = true;

            if (!upgraded)
            {
                ShowCampfireResult("No cards available to upgrade.");
                return;
            }

            runManager.ResolveCampfireUpgradeNode();
            ShowCampfireResult("Upgraded " + card.baseCard.cardName + " to " + card.DisplayName);
        }

        private void ShowCampfirePanel()
        {
            if (campfirePanel == null)
            {
                return;
            }

            campfirePanel.SetActive(true);

            if (campfireTitleText != null)
            {
                campfireTitleText.text = "Campfire";
            }

            if (campfireInfoText != null)
            {
                campfireInfoText.text = "Choose Rest or Upgrade Card.";
            }

            ClearChildren(campfireUpgradeContainer);
            SetCampfireActionButtons(true);

            if (campfireContinueButton != null)
            {
                campfireContinueButton.gameObject.SetActive(false);
            }
        }

        private void ShowCampfireResult(string message)
        {
            if (campfireInfoText != null)
            {
                campfireInfoText.text = message;
            }

            if (infoText != null)
            {
                infoText.text = message;
            }

            SetCampfireActionButtons(false);

            if (campfireContinueButton != null)
            {
                campfireContinueButton.gameObject.SetActive(true);
                campfireContinueButton.interactable = true;
            }
        }

        private void CloseCampfirePanel()
        {
            if (campfirePanel != null)
            {
                campfirePanel.SetActive(false);
            }

            if (campfireResolved)
            {
                RefreshMapUi();
            }
        }

        private void SetCampfireActionButtons(bool isVisible)
        {
            SetButtonState(restButton, isVisible);
            SetButtonState(upgradeButton, isVisible);
        }

        private void OnTreasureNodeClicked()
        {
            if (runManager == null || cardDatabase == null)
            {
                return;
            }

            if (!runManager.GetAvailableMapNodes().Contains(MapNodeType.Treasure))
            {
                return;
            }

            ShowTreasureRewards();
        }

        private void ShowTreasureRewards()
        {
            if (treasurePanel == null || treasureCardContainer == null || treasureManager == null)
            {
                Debug.LogWarning("Treasure UI references are missing.");
                return;
            }

            treasureChoiceLocked = false;
            treasureResolved = false;
            ClearChildren(treasureCardContainer);
            List<TreasureRewardChoice> choices = treasureManager.GenerateRewardChoices(3);
            Debug.Log("Treasure node opened.");
            Debug.Log("Generated treasure choices: " + string.Join(", ", choices.Select(choice => choice.rewardType + (choice.rewardType == TreasureRewardType.Gold ? $" {choice.goldAmount}g" : string.Empty))));

            if (choices.Count == 0)
            {
                runManager.MarkTreasureNodeResolved();
                ShowTreasureResult("No treasure rewards available.");
                return;
            }

            foreach (TreasureRewardChoice choice in choices)
            {
                CreateTreasureChoiceButton(treasureCardContainer, choice, SelectTreasureChoice);
            }

            if (treasureTitleText != null)
            {
                treasureTitleText.text = "Choose 1 Treasure Reward";
            }

            if (treasureInfoText != null)
            {
                treasureInfoText.text = "Choose 1 treasure reward.";
            }

            if (infoText != null)
            {
                infoText.text = "Treasure found.";
            }

            if (treasureContinueButton != null)
            {
                treasureContinueButton.gameObject.SetActive(false);
            }

            treasurePanel.SetActive(true);
            Debug.Log("Selected node: Treasure");
        }

        private void SelectTreasureChoice(TreasureRewardChoice choice)
        {
            if (treasureChoiceLocked || choice == null || treasureManager == null)
            {
                return;
            }

            treasureChoiceLocked = true;
            Debug.Log("Selected treasure reward: " + choice.rewardType);

            switch (choice.rewardType)
            {
                case TreasureRewardType.Relic:
                    ShowTreasureRelicChoices(choice);
                    break;
                case TreasureRewardType.Gold:
                    treasureManager.ApplyGoldReward(choice.goldAmount);
                    Debug.Log("Reward applied: Gold");
                    LogTreasureState();
                    ShowTreasureResult($"Gained {choice.goldAmount} gold.");
                    break;
                case TreasureRewardType.Card:
                    ShowTreasureCardChoices();
                    break;
                case TreasureRewardType.Upgrade:
                    ShowTreasureUpgradeChoices();
                    break;
                case TreasureRewardType.Heal:
                    treasureManager.ApplyHealReward();
                    Debug.Log("Reward applied: Heal");
                    LogTreasureState();
                    ShowTreasureResult("Party recovered.");
                    break;
            }
        }

        private void ShowTreasureRelicChoices(TreasureRewardChoice choice)
        {
            ClearChildren(treasureCardContainer);
            List<RelicData> relicChoices = choice.relicChoices ?? new List<RelicData>();

            if (relicChoices.Count == 0)
            {
                runManager.MarkTreasureNodeResolved();
                ShowTreasureResult("No relics available.");
                return;
            }

            foreach (RelicData relic in relicChoices)
            {
                CreateRelicChoiceButton(treasureCardContainer, relic, SelectTreasureRelicReward);
            }

            if (treasureTitleText != null)
            {
                treasureTitleText.text = "Ancient Relic";
            }

            if (treasureInfoText != null)
            {
                treasureInfoText.text = "Choose 1 relic.";
            }
        }

        private void SelectTreasureRelicReward(RelicData relic)
        {
            if (relic == null || treasureManager == null)
            {
                return;
            }

            bool gained = treasureManager.ApplyRelicReward(relic.relicId);
            Debug.Log("Reward applied: Relic");
            LogTreasureState();
            ShowTreasureResult(gained ? "Gained relic: " + relic.relicName : "No relic gained.");
        }

        private void ShowTreasureCardChoices()
        {
            ClearChildren(treasureCardContainer);
            List<CardData> cardChoices = treasureManager.GetCardRewardChoices(3);

            if (cardChoices.Count == 0)
            {
                runManager.MarkTreasureNodeResolved();
                ShowTreasureResult("No cards available.");
                return;
            }

            if (treasureTitleText != null)
            {
                treasureTitleText.text = "Card Stash";
            }

            if (treasureInfoText != null)
            {
                treasureInfoText.text = "Choose 1 card to add to your deck.";
            }

            foreach (CardData card in cardChoices)
            {
                CardView cardView = cardViewPrefab != null
                    ? Instantiate(cardViewPrefab, treasureCardContainer)
                    : CreateRuntimeRewardCardView(treasureCardContainer);

                cardView.Setup(card, SelectTreasureCardReward);
                cardView.SetDisabled(false, string.Empty);
            }
        }

        private void SelectTreasureCardReward(CardData card)
        {
            if (card == null || treasureManager == null)
            {
                return;
            }

            treasureManager.ApplyCardReward(card);
            Debug.Log("Reward applied: Card");
            LogTreasureState();
            ShowTreasureResult("Added " + card.cardName + " to deck.");
        }

        private void ShowTreasureUpgradeChoices()
        {
            ClearChildren(treasureCardContainer);
            List<RuntimeCardState> upgradeChoices = treasureManager.GetUpgradeChoices();

            if (upgradeChoices.Count == 0)
            {
                runManager.MarkTreasureNodeResolved();
                ShowTreasureResult("No cards available to upgrade.");
                return;
            }

            if (treasureTitleText != null)
            {
                treasureTitleText.text = "Upgrade Scroll";
            }

            if (treasureInfoText != null)
            {
                treasureInfoText.text = "Choose 1 card to upgrade.";
            }

            foreach (RuntimeCardState card in upgradeChoices)
            {
                CardView cardView = cardViewPrefab != null
                    ? Instantiate(cardViewPrefab, treasureCardContainer)
                    : CreateRuntimeRewardCardView(treasureCardContainer);

                cardView.Setup(card, SelectTreasureUpgradeReward, card.BuildUpgradePreview());
                cardView.SetDisabled(false, string.Empty);
            }
        }

        private void SelectTreasureUpgradeReward(RuntimeCardState card)
        {
            if (card == null || treasureManager == null)
            {
                return;
            }

            bool upgraded = treasureManager.ApplyUpgradeReward(card);
            Debug.Log("Reward applied: Upgrade");
            LogTreasureState();
            ShowTreasureResult(upgraded
                ? "Upgraded " + card.baseCard.cardName + " to " + card.DisplayName
                : "No cards available to upgrade.");
        }

        private void ShowTreasureResult(string message)
        {
            treasureResolved = true;
            ClearChildren(treasureCardContainer);

            if (treasureInfoText != null)
            {
                treasureInfoText.text = message;
            }

            if (infoText != null)
            {
                infoText.text = message;
            }

            if (treasureContinueButton != null)
            {
                treasureContinueButton.gameObject.SetActive(true);
                treasureContinueButton.interactable = true;
            }
        }

        private void CloseTreasurePanel()
        {
            if (treasurePanel != null)
            {
                treasurePanel.SetActive(false);
            }

            if (treasureResolved)
            {
                treasureChoiceLocked = false;
                RefreshMapUi();
            }
        }

        private void LogTreasureState()
        {
            Debug.Log("Current gold: " + runManager.Gold);
            Debug.Log("Deck count: " + runManager.CurrentRunDeck.Count);
            Debug.Log("Owned relic count: " + runManager.OwnedRelics.Count);
        }

        private string GetProgressText()
        {
            if (runManager.RunWon)
            {
                return "Run won!";
            }

            if (runManager.CurrentBattleIndex >= runManager.TotalNormalBattlesBeforeBoss)
            {
                return "Boss Battle";
            }

            int nextBattle = Mathf.Clamp(runManager.CurrentBattleIndex + 1, 1, runManager.TotalNormalBattlesBeforeBoss);
            return $"Next: Battle {nextBattle}/{runManager.TotalNormalBattlesBeforeBoss}";
        }

        private void SetButtonState(Button button, bool isVisible)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(isVisible);
            button.interactable = isVisible;
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

        private void ClearChildren(Transform container)
        {
            if (container == null)
            {
                return;
            }

            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
        }

        private void EnsureRuntimeUi()
        {
            if (treasurePanel != null && treasureContinueButton == null)
            {
                EnsureTreasureContinueButton();
            }

            if (titleText != null && battleNodeButton != null && treasurePanel != null && treasureContinueButton != null && campfirePanel != null && relicsText != null)
            {
                return;
            }

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

            GameObject root = CreatePanel("MapRoot", canvas.transform, new Color(0.92f, 0.90f, 0.82f, 1f));
            StretchFull(root.GetComponent<RectTransform>(), 20f);

            titleText = CreateText("Title", root.transform, new Vector2(20f, -20f), new Vector2(420f, 40f), 32, FontStyle.Bold, TextAnchor.UpperLeft);
            progressText = CreateText("ProgressText", root.transform, new Vector2(20f, -68f), new Vector2(420f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            infoText = CreateText("InfoText", root.transform, new Vector2(20f, -102f), new Vector2(900f, 32f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            relicsText = CreateText("RelicsText", root.transform, new Vector2(980f, -20f), new Vector2(700f, 90f), 16, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject nodePanel = CreatePanel("NodePanel", root.transform, new Color(0.98f, 0.96f, 0.88f, 1f));
            RectTransform nodeRect = nodePanel.GetComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);
            nodeRect.sizeDelta = new Vector2(920f, 360f);
            nodeRect.anchoredPosition = new Vector2(0f, -20f);

            CreateText("NodeLabel", nodePanel.transform, new Vector2(20f, -20f), new Vector2(320f, 32f), 28, FontStyle.Bold, TextAnchor.UpperLeft).text = "Choose your path";

            Transform buttonRow = CreateLayoutContainer("NodeButtons", nodePanel.transform, false, new Vector2(20f, 30f), new Vector2(-20f, -80f));
            battleNodeButton = CreateButton("BattleButton", buttonRow, "Battle");
            treasureNodeButton = CreateButton("TreasureButton", buttonRow, "Treasure");
            campfireNodeButton = CreateButton("CampfireButton", buttonRow, "Campfire");
            bossNodeButton = CreateButton("BossButton", buttonRow, "Boss");

            treasurePanel = CreateOverlayPanel(root.transform, "TreasurePanel", "TreasureBox", out GameObject treasureBox);
            treasureTitleText = CreateText("TreasureTitle", treasureBox.transform, new Vector2(20f, -20f), new Vector2(300f, 32f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            treasureInfoText = CreateText("TreasureInfo", treasureBox.transform, new Vector2(20f, -58f), new Vector2(500f, 24f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            treasureCardContainer = CreateLayoutContainer("TreasureCards", treasureBox.transform, false, new Vector2(20f, 20f), new Vector2(-20f, -100f));
            treasureContinueButton = CreateButton("TreasureContinueButton", treasureBox.transform, "Continue");
            RectTransform treasureContinueRect = treasureContinueButton.GetComponent<RectTransform>();
            treasureContinueRect.anchorMin = new Vector2(1f, 0f);
            treasureContinueRect.anchorMax = new Vector2(1f, 0f);
            treasureContinueRect.pivot = new Vector2(1f, 0f);
            treasureContinueRect.anchoredPosition = new Vector2(-20f, 20f);
            treasureContinueRect.sizeDelta = new Vector2(180f, 48f);
            treasureContinueButton.gameObject.SetActive(false);

            campfirePanel = CreateOverlayPanel(root.transform, "CampfirePanel", "CampfireBox", out GameObject campfireBox);
            campfireTitleText = CreateText("CampfireTitle", campfireBox.transform, new Vector2(20f, -20f), new Vector2(300f, 32f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            campfireInfoText = CreateText("CampfireInfo", campfireBox.transform, new Vector2(20f, -58f), new Vector2(600f, 42f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            Transform campfireButtons = CreateLayoutContainer("CampfireButtons", campfireBox.transform, false, new Vector2(20f, 360f), new Vector2(-20f, -120f));
            restButton = CreateButton("RestButton", campfireButtons, "Rest");
            upgradeButton = CreateButton("UpgradeButton", campfireButtons, "Upgrade Card");
            campfireUpgradeContainer = CreateLayoutContainer("CampfireUpgradeCards", campfireBox.transform, false, new Vector2(20f, 20f), new Vector2(-20f, -150f));
            campfireContinueButton = CreateButton("CampfireContinueButton", campfireBox.transform, "Continue");
            RectTransform continueRect = campfireContinueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(1f, 0f);
            continueRect.anchorMax = new Vector2(1f, 0f);
            continueRect.pivot = new Vector2(1f, 0f);
            continueRect.anchoredPosition = new Vector2(-20f, 20f);
            continueRect.sizeDelta = new Vector2(180f, 48f);
            campfireContinueButton.gameObject.SetActive(false);
        }

        private void EnsureTreasureContinueButton()
        {
            if (treasurePanel == null || treasureContinueButton != null || treasurePanel.transform.childCount == 0)
            {
                return;
            }

            Transform treasureBox = treasurePanel.transform.GetChild(0);
            treasureContinueButton = CreateButton("TreasureContinueButton", treasureBox, "Continue");
            RectTransform treasureContinueRect = treasureContinueButton.GetComponent<RectTransform>();
            treasureContinueRect.anchorMin = new Vector2(1f, 0f);
            treasureContinueRect.anchorMax = new Vector2(1f, 0f);
            treasureContinueRect.pivot = new Vector2(1f, 0f);
            treasureContinueRect.anchoredPosition = new Vector2(-20f, 20f);
            treasureContinueRect.sizeDelta = new Vector2(180f, 48f);
            treasureContinueButton.gameObject.SetActive(false);
        }

        private GameObject CreateOverlayPanel(Transform parent, string panelName, string boxName, out GameObject box)
        {
            GameObject overlay = CreatePanel(panelName, parent, new Color(0f, 0f, 0f, 0.7f));
            StretchFull(overlay.GetComponent<RectTransform>(), 0f);
            overlay.SetActive(false);

            box = CreatePanel(boxName, overlay.transform, new Color(0.98f, 0.96f, 0.88f, 1f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(980f, 560f);
            boxRect.anchoredPosition = Vector2.zero;
            return overlay;
        }

        private CardView CreateRuntimeRewardCardView(Transform parent)
        {
            GameObject root = CreatePanel("RewardCardView", parent, Color.white);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 250f;
            layout.preferredHeight = 320f;

            Button button = root.AddComponent<Button>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            CardView view = root.AddComponent<CardView>();
            view.backgroundImage = root.GetComponent<Image>();
            view.button = button;
            view.canvasGroup = canvasGroup;
            view.cardNameText = CreateText("CardName", root.transform, new Vector2(10f, -10f), new Vector2(180f, 40f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            view.costText = CreateText("CostText", root.transform, new Vector2(198f, -10f), new Vector2(40f, 28f), 22, FontStyle.Bold, TextAnchor.UpperRight);
            view.ownerText = CreateText("OwnerText", root.transform, new Vector2(10f, -54f), new Vector2(220f, 22f), 16, FontStyle.Italic, TextAnchor.UpperLeft);
            view.typeText = CreateText("TypeText", root.transform, new Vector2(10f, -78f), new Vector2(220f, 22f), 16, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject art = CreatePanel("Art", root.transform, new Color(0.82f, 0.82f, 0.82f, 1f));
            RectTransform artRect = art.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0f, 1f);
            artRect.anchorMax = new Vector2(0f, 1f);
            artRect.pivot = new Vector2(0f, 1f);
            artRect.anchoredPosition = new Vector2(20f, -108f);
            artRect.sizeDelta = new Vector2(210f, 90f);
            view.artImage = art.GetComponent<Image>();

            view.descriptionText = CreateText("Description", root.transform, new Vector2(10f, -208f), new Vector2(230f, 92f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
            view.disabledReasonText = CreateText("DisabledReason", root.transform, new Vector2(10f, -300f), new Vector2(230f, 20f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            view.disabledReasonText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
            return view;
        }

        private void CreateRelicChoiceButton(Transform parent, RelicData relic, System.Action<RelicData> onClick)
        {
            GameObject root = CreatePanel("RelicChoice", parent, new Color(0.95f, 0.93f, 0.85f, 1f));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 180f);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 280f;
            layout.preferredHeight = 180f;
            layout.minWidth = 280f;
            layout.minHeight = 180f;

            Button button = root.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke(relic));

            CreateText("RelicName", root.transform, new Vector2(12f, -12f), new Vector2(240f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft).text = relic.relicName;
            CreateText("RelicRarity", root.transform, new Vector2(12f, -42f), new Vector2(240f, 22f), 16, FontStyle.Italic, TextAnchor.UpperLeft).text = "Rarity: " + relic.rarity;
            CreateText("RelicDescription", root.transform, new Vector2(12f, -70f), new Vector2(250f, 88f), 15, FontStyle.Normal, TextAnchor.UpperLeft).text = relic.description;
        }

        private void CreateTreasureChoiceButton(Transform parent, TreasureRewardChoice choice, System.Action<TreasureRewardChoice> onClick)
        {
            GameObject root = CreatePanel("TreasureChoice", parent, new Color(0.93f, 0.90f, 0.80f, 1f));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 180f);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 280f;
            layout.preferredHeight = 180f;
            layout.minWidth = 280f;
            layout.minHeight = 180f;

            Button button = root.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke(choice));

            CreateText("ChoiceTitle", root.transform, new Vector2(12f, -12f), new Vector2(240f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft).text = choice.title;
            CreateText("ChoiceType", root.transform, new Vector2(12f, -42f), new Vector2(240f, 22f), 16, FontStyle.Italic, TextAnchor.UpperLeft).text = choice.rewardType.ToString();
            CreateText("ChoiceDescription", root.transform, new Vector2(12f, -70f), new Vector2(250f, 88f), 15, FontStyle.Normal, TextAnchor.UpperLeft).text = choice.description;
        }

        private Transform CreateLayoutContainer(string name, Transform parent, bool vertical, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject container = CreateUiObject(name, parent, Vector2.zero);
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            if (vertical)
            {
                VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
            else
            {
                HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 10f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            return container.transform;
        }

        private Button CreateButton(string name, Transform parent, string label)
        {
            GameObject buttonObject = CreatePanel(name, parent, new Color(0.38f, 0.55f, 0.33f, 1f));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 80f);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 200f;
            layout.preferredHeight = 80f;

            Button button = buttonObject.AddComponent<Button>();
            CreateText("Label", buttonObject.transform, new Vector2(20f, -14f), new Vector2(160f, 40f), 24, FontStyle.Bold, TextAnchor.MiddleCenter).text = label;
            return button;
        }

        private Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(name, parent, size);
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

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreateUiObject(name, parent, Vector2.zero);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private GameObject CreateUiObject(string name, Transform parent, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return gameObject;
        }

        private void StretchFull(RectTransform rectTransform, float padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
