using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class MapManager : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color(0.10f, 0.15f, 0.12f, 1f);
        private static readonly Color ParchmentColor = new Color(0.96f, 0.92f, 0.84f, 1f);
        private static readonly Color DarkPanelColor = new Color(0.20f, 0.17f, 0.13f, 0.88f);
        private static readonly Color TextLightColor = new Color(0.98f, 0.95f, 0.86f, 1f);
        private static readonly Color TextMutedColor = new Color(0.82f, 0.84f, 0.80f, 1f);
        private static readonly Color TextDarkColor = new Color(0.18f, 0.15f, 0.12f, 1f);
        private static readonly Color GoldColor = new Color(0.88f, 0.73f, 0.26f, 1f);
        private static readonly Color HealColor = new Color(0.43f, 0.68f, 0.46f, 1f);
        private static readonly Color UpgradeColor = new Color(0.33f, 0.55f, 0.78f, 1f);
        private static readonly Color RelicColor = new Color(0.55f, 0.39f, 0.68f, 1f);
        private static readonly Color DangerColor = new Color(0.55f, 0.24f, 0.22f, 1f);
        private static readonly Color DisabledColor = new Color(0.46f, 0.46f, 0.46f, 0.9f);

        [Header("Dependencies")]
        public RunManager runManager;
        public TreasureManager treasureManager;
        public ShopManager shopManager;
        public CardDatabase cardDatabase;
        public CardView cardViewPrefab;

        [Header("Map UI")]
        public Text titleText;
        public Text progressText;
        public Text infoText;
        public Text goldText;
        public Text relicsText;
        public Text partySummaryTitleText;
        public Text partySummaryText;
        public Button battleNodeButton;
        public Button treasureNodeButton;
        public Button campfireNodeButton;
        public Button shopNodeButton;
        public Button bossNodeButton;

        [Header("Treasure UI")]
        public GameObject treasurePanel;
        public Transform treasureCardContainer;
        public Text treasureTitleText;
        public Text treasureInfoText;
        public Button treasureContinueButton;

        [Header("Shop UI")]
        public GameObject shopPanel;
        public Text shopTitleText;
        public Text shopGoldText;
        public Text shopInfoText;
        public Transform shopOfferContainer;
        public Transform shopSelectionContainer;
        public Button shopLeaveButton;

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
        private bool shopVisited;
        private ShopOffer activeShopOffer;
        private List<ShopOffer> currentShopOffers = new List<ShopOffer>();
        private bool campfireResolved;

        private void Start()
        {
            EnsureRunManager();
            EnsureRuntimeUi();
            EnsureTreasureManager();
            EnsureShopManager();
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
                progressText.color = TextMutedColor;
            }

            if (goldText != null)
            {
                goldText.text = $"Gold: {runManager.Gold}";
                goldText.color = GoldColor;
            }

            if (relicsText != null)
            {
                relicsText.text = $"Relics: {runManager.OwnedRelics.Count}";
                relicsText.color = RelicColor;
            }

            if (partySummaryTitleText != null)
            {
                partySummaryTitleText.text = "Party";
                partySummaryTitleText.color = TextLightColor;
            }

            if (partySummaryText != null)
            {
                partySummaryText.text = BuildPartySummary();
                partySummaryText.color = TextMutedColor;
            }

            if (treasurePanel != null)
            {
                treasurePanel.SetActive(false);
            }

            if (campfirePanel != null)
            {
                campfirePanel.SetActive(false);
            }

            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            List<MapNodeType> availableNodes = runManager.GetAvailableMapNodes();
            Debug.Log("Entered map.");
            Debug.Log("Available nodes: " + string.Join(", ", availableNodes));
            Debug.Log("Current battle index: " + runManager.CurrentBattleIndex);
            Debug.Log("Deck count: " + runManager.CurrentRunDeck.Count);

            SetButtonState(battleNodeButton, availableNodes.Contains(MapNodeType.Battle));
            SetButtonState(treasureNodeButton, availableNodes.Contains(MapNodeType.Treasure));
            SetButtonState(campfireNodeButton, availableNodes.Contains(MapNodeType.Campfire));
            SetButtonState(shopNodeButton, availableNodes.Contains(MapNodeType.Shop));
            SetButtonState(bossNodeButton, availableNodes.Contains(MapNodeType.Boss));
            RefreshNodeLabels();

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

        private void EnsureShopManager()
        {
            if (shopManager == null)
            {
                shopManager = GetComponent<ShopManager>();
            }

            if (shopManager == null)
            {
                shopManager = gameObject.AddComponent<ShopManager>();
            }

            shopManager.Initialize(runManager, cardDatabase);
        }

        private void BindButtons()
        {
            BindButton(battleNodeButton, OnBattleNodeClicked);
            BindButton(treasureNodeButton, OnTreasureNodeClicked);
            BindButton(campfireNodeButton, OnCampfireNodeClicked);
            BindButton(shopNodeButton, OnShopNodeClicked);
            BindButton(bossNodeButton, OnBossNodeClicked);
            BindButton(treasureContinueButton, CloseTreasurePanel);
            BindButton(shopLeaveButton, CloseShopPanel);
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

        private void OnShopNodeClicked()
        {
            if (runManager == null || !runManager.GetAvailableMapNodes().Contains(MapNodeType.Shop))
            {
                return;
            }

            ShowShopPanel();
            Debug.Log("Selected node: Shop");
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
                campfireInfoText.text = "Rest or improve your deck.";
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
                treasureTitleText.text = "Treasure Found";
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
                treasureTitleText.text = "Treasure Found";
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
                treasureTitleText.text = "Treasure Found";
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
                treasureTitleText.text = "Treasure Found";
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

        private void ShowShopPanel()
        {
            if (shopPanel == null || shopOfferContainer == null || shopManager == null)
            {
                Debug.LogWarning("Shop UI references are missing.");
                return;
            }

            shopVisited = true;
            activeShopOffer = null;
            currentShopOffers = shopManager.GenerateOffers();
            ClearChildren(shopOfferContainer);
            ClearChildren(shopSelectionContainer);

            if (shopTitleText != null)
            {
                shopTitleText.text = "Shop";
            }

            UpdateShopGoldText();

            if (shopInfoText != null)
            {
                shopInfoText.text = "Spend gold on cards, relics, and services.";
            }

            if (infoText != null)
            {
                infoText.text = "Shop opened.";
            }

            foreach (ShopOffer offer in currentShopOffers)
            {
                CreateShopOfferButton(shopOfferContainer, offer);
            }

            shopPanel.SetActive(true);
            Debug.Log("Shop opened");
            Debug.Log("Offers generated: " + string.Join(", ", currentShopOffers.Select(offer => offer.title)));
        }

        private void CreateShopOfferButton(Transform parent, ShopOffer offer)
        {
            GameObject root = CreatePanel("ShopOffer", parent, new Color(0.93f, 0.89f, 0.80f, 1f));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 232f);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 280f;
            layout.preferredHeight = 232f;
            layout.minWidth = 280f;
            layout.minHeight = 232f;

            Color accent = GetShopOfferColor(offer.offerType);
            GameObject accentStrip = CreatePanel("AccentStrip", root.transform, accent);
            RectTransform accentRect = accentStrip.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.offsetMin = new Vector2(0f, -10f);
            accentRect.offsetMax = new Vector2(0f, 0f);

            Text title = CreateText("OfferTitle", root.transform, new Vector2(12f, -12f), new Vector2(240f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            title.text = offer.title;
            title.color = TextDarkColor;
            Text description = CreateText("OfferDescription", root.transform, new Vector2(12f, -48f), new Vector2(248f, 68f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            description.text = offer.description;
            description.color = new Color(0.28f, 0.24f, 0.20f, 1f);

            if (offer.offerType == ShopOfferType.Card && offer.cardData != null)
            {
                CreateMiniCardPreview(root.transform, offer.cardData, new Vector2(12f, -118f));
            }

            Text cost = CreateText("OfferCost", root.transform, new Vector2(12f, -164f), new Vector2(140f, 24f), 18, FontStyle.Bold, TextAnchor.UpperLeft);
            cost.text = "Cost: " + offer.cost;
            cost.color = GoldColor;

            Button buyButton = CreateButton("BuyButton", root.transform, "Buy");
            RectTransform buyRect = buyButton.GetComponent<RectTransform>();
            buyRect.anchorMin = new Vector2(1f, 0f);
            buyRect.anchorMax = new Vector2(1f, 0f);
            buyRect.pivot = new Vector2(1f, 0f);
            buyRect.anchoredPosition = new Vector2(-12f, 12f);
            buyRect.sizeDelta = new Vector2(110f, 42f);

            Text soldText = CreateText("SoldText", root.transform, new Vector2(12f, -194f), new Vector2(120f, 24f), 18, FontStyle.Bold, TextAnchor.UpperLeft);
            soldText.text = offer.isPurchased ? "Sold" : string.Empty;
            soldText.color = offer.isPurchased ? DisabledColor : accent;

            buyButton.interactable = !offer.isPurchased && runManager.Gold >= offer.cost;
            StyleButton(buyButton, accent, false);
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => TryBuyShopOffer(offer));
        }

        private void CreateMiniCardPreview(Transform parent, CardData card, Vector2 anchoredPosition)
        {
            GameObject preview = CreatePanel("CardPreview", parent, new Color(0.96f, 0.93f, 0.84f, 1f));
            RectTransform previewRect = preview.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0f, 1f);
            previewRect.anchorMax = new Vector2(0f, 1f);
            previewRect.pivot = new Vector2(0f, 1f);
            previewRect.anchoredPosition = anchoredPosition;
            previewRect.sizeDelta = new Vector2(248f, 58f);

            GameObject accent = CreatePanel("OwnerAccent", preview.transform, GetCardOwnerColor(card.ownerHeroId));
            RectTransform accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(8f, 0f);

            GameObject typeBadge = CreatePanel("TypeBadge", preview.transform, GetCardTypeColor(card.cardType));
            RectTransform typeRect = typeBadge.GetComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0f, 1f);
            typeRect.anchorMax = new Vector2(0f, 1f);
            typeRect.pivot = new Vector2(0f, 1f);
            typeRect.anchoredPosition = new Vector2(16f, -10f);
            typeRect.sizeDelta = new Vector2(52f, 20f);

            Text typeLabel = CreateText("TypeLabel", typeBadge.transform, Vector2.zero, new Vector2(52f, 20f), 11, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(typeLabel.rectTransform, 0f);
            typeLabel.text = card.cardType.ToString().ToUpperInvariant();
            typeLabel.color = TextLightColor;

            Text nameText = CreateText("CardName", preview.transform, new Vector2(76f, -10f), new Vector2(118f, 20f), 17, FontStyle.Bold, TextAnchor.UpperLeft);
            nameText.text = card.cardName;
            nameText.color = TextDarkColor;
            Text ownerText = CreateText("CardOwner", preview.transform, new Vector2(76f, -30f), new Vector2(126f, 16f), 12, FontStyle.Italic, TextAnchor.UpperLeft);
            ownerText.text = card.ownerHeroId == HeroId.Neutral ? "Neutral" : card.ownerHeroId.ToString();
            ownerText.color = new Color(0.33f, 0.28f, 0.22f, 1f);

            GameObject costBadge = CreatePanel("CostBadge", preview.transform, new Color(0.22f, 0.41f, 0.71f, 1f));
            RectTransform costRect = costBadge.GetComponent<RectTransform>();
            costRect.anchorMin = new Vector2(1f, 0.5f);
            costRect.anchorMax = new Vector2(1f, 0.5f);
            costRect.pivot = new Vector2(1f, 0.5f);
            costRect.anchoredPosition = new Vector2(-10f, 0f);
            costRect.sizeDelta = new Vector2(36f, 36f);

            Text costText = CreateText("CostText", costBadge.transform, Vector2.zero, new Vector2(36f, 36f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(costText.rectTransform, 0f);
            costText.text = card.cost.ToString();
            costText.color = TextLightColor;
        }

        private void TryBuyShopOffer(ShopOffer offer)
        {
            if (offer == null || offer.isPurchased || shopManager == null)
            {
                return;
            }

            if (runManager.Gold < offer.cost)
            {
                ShowShopMessage("Not enough gold.");
                Debug.Log("Not enough gold");
                return;
            }

            string message;
            bool success = false;

            switch (offer.offerType)
            {
                case ShopOfferType.Card:
                    success = shopManager.TryBuyCard(offer, out message);
                    break;
                case ShopOfferType.Relic:
                    success = shopManager.TryBuyRelic(offer, out message);
                    break;
                case ShopOfferType.Heal:
                    success = shopManager.TryBuyHeal(offer, out message);
                    break;
                case ShopOfferType.RemoveCard:
                    ShowShopRemoveSelection(offer);
                    return;
                case ShopOfferType.UpgradeCard:
                    ShowShopUpgradeSelection(offer);
                    return;
                default:
                    message = "Offer unavailable.";
                    break;
            }

            ShowShopPurchaseResult(success, message, offer);
        }

        private void ShowShopRemoveSelection(ShopOffer offer)
        {
            activeShopOffer = offer;
            ClearChildren(shopSelectionContainer);

            if (runManager.CurrentRunDeck.Count <= 5)
            {
                ShowShopMessage("Deck is too small to remove more cards.");
                return;
            }

            if (shopInfoText != null)
            {
                shopInfoText.text = "Remove 1 card permanently from this run.";
            }

            foreach (RuntimeCardState card in runManager.CurrentRunDeck.ToList())
            {
                CardView cardView = cardViewPrefab != null
                    ? Instantiate(cardViewPrefab, shopSelectionContainer)
                    : CreateRuntimeRewardCardView(shopSelectionContainer);

                cardView.Setup(card, SelectShopRemoveCard);
                cardView.SetDisabled(false, string.Empty);
            }
        }

        private void SelectShopRemoveCard(RuntimeCardState card)
        {
            if (activeShopOffer == null || shopManager == null || card == null)
            {
                return;
            }

            bool success = shopManager.TryBuyRemoveCard(activeShopOffer, card, out string message);
            ClearChildren(shopSelectionContainer);
            ShowShopPurchaseResult(success, message, activeShopOffer);
            activeShopOffer = null;
        }

        private void ShowShopUpgradeSelection(ShopOffer offer)
        {
            activeShopOffer = offer;
            ClearChildren(shopSelectionContainer);
            List<RuntimeCardState> upgradeChoices = runManager.GetUpgradeableCards();

            if (upgradeChoices.Count == 0)
            {
                ShowShopMessage("No cards available to upgrade.");
                return;
            }

            if (shopInfoText != null)
            {
                shopInfoText.text = "Choose 1 card to upgrade.";
            }

            foreach (RuntimeCardState card in upgradeChoices)
            {
                CardView cardView = cardViewPrefab != null
                    ? Instantiate(cardViewPrefab, shopSelectionContainer)
                    : CreateRuntimeRewardCardView(shopSelectionContainer);

                cardView.Setup(card, SelectShopUpgradeCard, card.BuildUpgradePreview());
                cardView.SetDisabled(false, string.Empty);
            }
        }

        private void SelectShopUpgradeCard(RuntimeCardState card)
        {
            if (activeShopOffer == null || shopManager == null || card == null)
            {
                return;
            }

            bool success = shopManager.TryBuyUpgrade(activeShopOffer, card, out string message);
            ClearChildren(shopSelectionContainer);
            ShowShopPurchaseResult(success, message, activeShopOffer);
            activeShopOffer = null;
        }

        private void ShowShopPurchaseResult(bool success, string message, ShopOffer offer)
        {
            if (success && offer != null)
            {
                offer.isPurchased = true;
                RebuildShopOffers();
            }

            ShowShopMessage(message);
        }

        private void ShowShopMessage(string message)
        {
            if (shopInfoText != null)
            {
                shopInfoText.text = message;
            }

            if (infoText != null)
            {
                infoText.text = message;
            }

            UpdateShopGoldText();
        }

        private void UpdateShopGoldText()
        {
            if (shopGoldText != null)
            {
                shopGoldText.text = "Gold: " + runManager.Gold;
            }
        }

        private void RebuildShopOffers()
        {
            ClearChildren(shopOfferContainer);
            foreach (ShopOffer offer in currentShopOffers)
            {
                CreateShopOfferButton(shopOfferContainer, offer);
            }

            UpdateShopGoldText();
            Debug.Log("Current gold after purchase: " + runManager.Gold);
            Debug.Log("Current deck count: " + runManager.CurrentRunDeck.Count);
        }

        private void CloseShopPanel()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            ClearChildren(shopSelectionContainer);
            activeShopOffer = null;

            if (shopVisited && runManager != null)
            {
                runManager.MarkShopNodeResolved();
                shopVisited = false;
                RefreshMapUi();
            }
        }

        private string BuildPartySummary()
        {
            if (runManager == null || runManager.ActiveHeroesRuntime == null || runManager.ActiveHeroesRuntime.Count == 0)
            {
                return "No party data available.";
            }

            List<string> lines = new List<string>();
            foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime)
            {
                if (hero == null || hero.heroData == null)
                {
                    continue;
                }

                string state = hero.IsAlive ? "Alive" : "Down";
                string heroName = hero.heroData != null && !string.IsNullOrWhiteSpace(hero.heroData.heroName)
                                ? hero.heroData.heroName
                                : hero.heroData != null
                                    ? hero.heroData.heroId.ToString()
                                    : "Hero";

                            lines.Add($"{heroName}\nHP {hero.currentHp}/{hero.MaxHp}  -  {state}");
            }

            return lines.Count > 0 ? string.Join("\n\n", lines) : "No party data available.";
        }

        private void RefreshNodeLabels()
        {
            RefreshNodeButton(battleNodeButton, "Battle", "Fight enemies and earn rewards.");
            RefreshNodeButton(treasureNodeButton, "Treasure", "Find gold, cards, or relics.");
            RefreshNodeButton(campfireNodeButton, "Campfire", "Rest or upgrade a card.");
            RefreshNodeButton(shopNodeButton, "Shop", "Spend gold on cards, relics, and services.");
            RefreshNodeButton(bossNodeButton, "Boss", "Face the Briar King.");
        }

        private void RefreshNodeButton(Button button, string title, string description)
        {
            if (button == null)
            {
                return;
            }

            Text[] texts = button.GetComponentsInChildren<Text>(true);
            foreach (Text text in texts)
            {
                if (text.name == "Title")
                {
                    text.text = title;
                    text.color = TextLightColor;
                }
                else if (text.name == "Description")
                {
                    text.text = description;
                    text.color = new Color(0.93f, 0.89f, 0.79f, 1f);
                }
            }
        }

        private Color GetNodeColor(Button button)
        {
            if (button == bossNodeButton)
            {
                return DangerColor;
            }

            if (button == treasureNodeButton)
            {
                return GoldColor;
            }

            if (button == campfireNodeButton)
            {
                return HealColor;
            }

            if (button == shopNodeButton)
            {
                return new Color(0.49f, 0.37f, 0.20f, 1f);
            }

            return new Color(0.34f, 0.47f, 0.35f, 1f);
        }

        private string GetProgressText()
        {
            if (runManager.RunWon)
            {
                return "Run won!";
            }

            if (runManager.IsBossBattle)
            {
                return "Boss Battle";
            }

            if (runManager.CurrentBattleIndex >= runManager.TotalNormalBattlesBeforeBoss)
            {
                return "Boss Available";
            }

            int nextBattle = Mathf.Clamp(runManager.CurrentBattleIndex + 1, 1, runManager.TotalNormalBattlesBeforeBoss);
            return $"Battle {nextBattle}/{runManager.TotalNormalBattlesBeforeBoss}";
        }

        private void ApplyScenePolish()
        {
            RectTransform topBar = titleText != null && titleText.transform.parent != null
                ? titleText.transform.parent as RectTransform
                : null;
            RectTransform mapRoot = topBar != null && topBar.parent != null
                ? topBar.parent as RectTransform
                : null;

            if (mapRoot == null)
            {
                return;
            }

            Image rootImage = mapRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = BackgroundColor;
            }

            if (goldText == null && topBar != null)
            {
                goldText = CreateText("GoldText", topBar, new Vector2(-250f, -24f), new Vector2(220f, 24f), 20, FontStyle.Bold, TextAnchor.UpperRight);
                goldText.rectTransform.anchorMin = new Vector2(1f, 1f);
                goldText.rectTransform.anchorMax = new Vector2(1f, 1f);
                goldText.rectTransform.pivot = new Vector2(1f, 1f);
            }

            if (relicsText == null && topBar != null)
            {
                relicsText = CreateText("RelicsText", topBar, new Vector2(-24f, -24f), new Vector2(200f, 24f), 20, FontStyle.Bold, TextAnchor.UpperRight);
                relicsText.rectTransform.anchorMin = new Vector2(1f, 1f);
                relicsText.rectTransform.anchorMax = new Vector2(1f, 1f);
                relicsText.rectTransform.pivot = new Vector2(1f, 1f);
            }

            if ((partySummaryText == null || partySummaryTitleText == null) && mapRoot.Find("PartyPanel") == null)
            {
                GameObject partyPanel = CreatePanel("PartyPanel", mapRoot, DarkPanelColor);
                RectTransform partyRect = partyPanel.GetComponent<RectTransform>();
                partyRect.anchorMin = new Vector2(0f, 0f);
                partyRect.anchorMax = new Vector2(0f, 1f);
                partyRect.pivot = new Vector2(0f, 1f);
                partyRect.anchoredPosition = new Vector2(0f, -140f);
                partyRect.sizeDelta = new Vector2(320f, 0f);
                partyRect.offsetMin = new Vector2(0f, 20f);
                partySummaryTitleText = CreateText("PartyTitle", partyPanel.transform, new Vector2(20f, -18f), new Vector2(220f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
                partySummaryText = CreateText("PartySummary", partyPanel.transform, new Vector2(20f, -56f), new Vector2(280f, 520f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            }
            else if (partySummaryTitleText == null || partySummaryText == null)
            {
                Transform partyPanel = mapRoot.Find("PartyPanel");
                if (partyPanel != null)
                {
                    if (partySummaryTitleText == null)
                    {
                        partySummaryTitleText = partyPanel.Find("PartyTitle")?.GetComponent<Text>();
                    }

                    if (partySummaryText == null)
                    {
                        partySummaryText = partyPanel.Find("PartySummary")?.GetComponent<Text>();
                    }
                }
            }

            if (titleText != null)
            {
                titleText.color = TextLightColor;
                titleText.text = "Dungeon Map";
            }

            if (infoText != null)
            {
                infoText.color = TextMutedColor;
            }

            StyleNodeButton(battleNodeButton, GetNodeColor(battleNodeButton), battleNodeButton != null && battleNodeButton.interactable);
            StyleNodeButton(treasureNodeButton, GetNodeColor(treasureNodeButton), treasureNodeButton != null && treasureNodeButton.interactable);
            StyleNodeButton(campfireNodeButton, GetNodeColor(campfireNodeButton), campfireNodeButton != null && campfireNodeButton.interactable);
            StyleNodeButton(shopNodeButton, GetNodeColor(shopNodeButton), shopNodeButton != null && shopNodeButton.interactable);
            StyleNodeButton(bossNodeButton, GetNodeColor(bossNodeButton), bossNodeButton != null && bossNodeButton.interactable);

            StyleButton(treasureContinueButton, GoldColor, false);
            StyleButton(shopLeaveButton, new Color(0.40f, 0.33f, 0.27f, 1f), false);
            StyleButton(restButton, HealColor, false);
            StyleButton(upgradeButton, UpgradeColor, false);
            StyleButton(campfireContinueButton, GoldColor, false);

            StyleOverlayPanel(treasurePanel, "Treasure Found", treasureTitleText, treasureInfoText);
            StyleOverlayPanel(shopPanel, "Shop", shopTitleText, shopInfoText);
            StyleOverlayPanel(campfirePanel, "Campfire", campfireTitleText, campfireInfoText);
        }

        private void StyleOverlayPanel(GameObject overlay, string fallbackTitle, Text title, Text info)
        {
            if (overlay == null)
            {
                return;
            }

            Image overlayImage = overlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0f, 0f, 0f, 0.74f);
            }

            if (overlay.transform.childCount > 0)
            {
                Image boxImage = overlay.transform.GetChild(0).GetComponent<Image>();
                if (boxImage != null)
                {
                    boxImage.color = ParchmentColor;
                }
            }

            if (title != null)
            {
                title.text = string.IsNullOrWhiteSpace(title.text) ? fallbackTitle : title.text;
                title.color = TextDarkColor;
            }

            if (info != null)
            {
                info.color = new Color(0.36f, 0.30f, 0.24f, 1f);
            }
        }

        private void StyleNodeButton(Button button, Color baseColor, bool isAvailable)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = isAvailable ? baseColor : DisabledColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = isAvailable ? baseColor : DisabledColor;
            colors.highlightedColor = isAvailable ? baseColor * 1.08f : DisabledColor;
            colors.pressedColor = isAvailable ? baseColor * 0.9f : DisabledColor;
            colors.disabledColor = DisabledColor;
            button.colors = colors;
        }

        private void SetButtonState(Button button, bool isAvailable)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);
            button.interactable = isAvailable;
            StyleNodeButton(button, GetNodeColor(button), isAvailable);
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

            if (shopPanel != null && shopLeaveButton == null)
            {
                EnsureShopLeaveButton();
            }

            if (titleText != null && battleNodeButton != null && shopNodeButton != null && treasurePanel != null && treasureContinueButton != null && shopPanel != null && shopLeaveButton != null && campfirePanel != null && relicsText != null)
            {
                ApplyScenePolish();
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

            GameObject root = CreatePanel("MapRoot", canvas.transform, BackgroundColor);
            StretchFull(root.GetComponent<RectTransform>(), 20f);

            GameObject topBar = CreatePanel("TopBar", root.transform, DarkPanelColor);
            RectTransform topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.sizeDelta = new Vector2(0f, 120f);
            topBarRect.anchoredPosition = Vector2.zero;

            titleText = CreateText("Title", topBar.transform, new Vector2(24f, -18f), new Vector2(420f, 40f), 34, FontStyle.Bold, TextAnchor.UpperLeft);
            progressText = CreateText("ProgressText", topBar.transform, new Vector2(24f, -60f), new Vector2(420f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            infoText = CreateText("InfoText", topBar.transform, new Vector2(24f, -90f), new Vector2(720f, 24f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            goldText = CreateText("GoldText", topBar.transform, new Vector2(-250f, -24f), new Vector2(220f, 24f), 20, FontStyle.Bold, TextAnchor.UpperRight);
            goldText.rectTransform.anchorMin = new Vector2(1f, 1f);
            goldText.rectTransform.anchorMax = new Vector2(1f, 1f);
            goldText.rectTransform.pivot = new Vector2(1f, 1f);
            relicsText = CreateText("RelicsText", topBar.transform, new Vector2(-24f, -24f), new Vector2(200f, 24f), 20, FontStyle.Bold, TextAnchor.UpperRight);
            relicsText.rectTransform.anchorMin = new Vector2(1f, 1f);
            relicsText.rectTransform.anchorMax = new Vector2(1f, 1f);
            relicsText.rectTransform.pivot = new Vector2(1f, 1f);

            GameObject partyPanel = CreatePanel("PartyPanel", root.transform, DarkPanelColor);
            RectTransform partyRect = partyPanel.GetComponent<RectTransform>();
            partyRect.anchorMin = new Vector2(0f, 0f);
            partyRect.anchorMax = new Vector2(0f, 1f);
            partyRect.pivot = new Vector2(0f, 1f);
            partyRect.anchoredPosition = new Vector2(0f, -140f);
            partyRect.sizeDelta = new Vector2(320f, 0f);
            partyRect.offsetMin = new Vector2(0f, 20f);
            partySummaryTitleText = CreateText("PartyTitle", partyPanel.transform, new Vector2(20f, -18f), new Vector2(220f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            partySummaryText = CreateText("PartySummary", partyPanel.transform, new Vector2(20f, -56f), new Vector2(280f, 520f), 18, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject nodePanel = CreatePanel("NodePanel", root.transform, ParchmentColor);
            RectTransform nodeRect = nodePanel.GetComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0f, 0f);
            nodeRect.anchorMax = new Vector2(1f, 1f);
            nodeRect.offsetMin = new Vector2(340f, 20f);
            nodeRect.offsetMax = new Vector2(-20f, -140f);

            Text nodeLabel = CreateText("NodeLabel", nodePanel.transform, new Vector2(24f, -20f), new Vector2(420f, 32f), 30, FontStyle.Bold, TextAnchor.UpperLeft);
            nodeLabel.text = "Choose your path";
            nodeLabel.color = TextDarkColor;

            Transform buttonRow = CreateGridContainer("NodeButtons", nodePanel.transform, new Vector2(24f, 24f), new Vector2(-24f, -72f), new Vector2(260f, 180f), new Vector2(20f, 20f), 3);
            battleNodeButton = CreateNodeButton("BattleButton", buttonRow, "Battle", "Fight enemies and earn rewards.");
            treasureNodeButton = CreateNodeButton("TreasureButton", buttonRow, "Treasure", "Find gold, cards, or relics.");
            campfireNodeButton = CreateNodeButton("CampfireButton", buttonRow, "Campfire", "Rest or upgrade a card.");
            shopNodeButton = CreateNodeButton("ShopButton", buttonRow, "Shop", "Spend gold on cards, relics, and services.");
            bossNodeButton = CreateNodeButton("BossButton", buttonRow, "Boss", "Face the Briar King.");

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

            shopPanel = CreateOverlayPanel(root.transform, "ShopPanel", "ShopBox", out GameObject shopBox);
            shopTitleText = CreateText("ShopTitle", shopBox.transform, new Vector2(20f, -20f), new Vector2(300f, 32f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            shopGoldText = CreateText("ShopGold", shopBox.transform, new Vector2(20f, -58f), new Vector2(240f, 24f), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            shopInfoText = CreateText("ShopInfo", shopBox.transform, new Vector2(280f, -58f), new Vector2(520f, 42f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            shopOfferContainer = CreateLayoutContainer("ShopOffers", shopBox.transform, false, new Vector2(20f, 240f), new Vector2(-20f, -120f));
            shopSelectionContainer = CreateLayoutContainer("ShopSelection", shopBox.transform, false, new Vector2(20f, 20f), new Vector2(-20f, -340f));
            shopLeaveButton = CreateButton("ShopLeaveButton", shopBox.transform, "Leave Shop");
            RectTransform shopLeaveRect = shopLeaveButton.GetComponent<RectTransform>();
            shopLeaveRect.anchorMin = new Vector2(1f, 0f);
            shopLeaveRect.anchorMax = new Vector2(1f, 0f);
            shopLeaveRect.pivot = new Vector2(1f, 0f);
            shopLeaveRect.anchoredPosition = new Vector2(-20f, 20f);
            shopLeaveRect.sizeDelta = new Vector2(180f, 48f);

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

            ApplyScenePolish();
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

        private void EnsureShopLeaveButton()
        {
            if (shopPanel == null || shopLeaveButton != null || shopPanel.transform.childCount == 0)
            {
                return;
            }

            Transform shopBox = shopPanel.transform.GetChild(0);
            shopLeaveButton = CreateButton("ShopLeaveButton", shopBox, "Leave Shop");
            RectTransform shopLeaveRect = shopLeaveButton.GetComponent<RectTransform>();
            shopLeaveRect.anchorMin = new Vector2(1f, 0f);
            shopLeaveRect.anchorMax = new Vector2(1f, 0f);
            shopLeaveRect.pivot = new Vector2(1f, 0f);
            shopLeaveRect.anchoredPosition = new Vector2(-20f, 20f);
            shopLeaveRect.sizeDelta = new Vector2(180f, 48f);
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
            GameObject root = CreatePanel("RewardCardView", parent, new Color(0.96f, 0.93f, 0.84f, 1f));
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
            GameObject root = CreatePanel("RelicChoice", parent, new Color(0.93f, 0.88f, 0.80f, 1f));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 180f);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 280f;
            layout.preferredHeight = 180f;
            layout.minWidth = 280f;
            layout.minHeight = 180f;

            Button button = root.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke(relic));
            StyleButton(button, RelicColor, false);

            CreatePanel("Icon", root.transform, new Color(0.58f, 0.43f, 0.70f, 1f)).GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 12f, 54f);
            RectTransform iconRect = root.transform.Find("Icon").GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(12f, -12f);
            iconRect.sizeDelta = new Vector2(54f, 54f);

            Text nameText = CreateText("RelicName", root.transform, new Vector2(78f, -12f), new Vector2(180f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            nameText.text = relic.relicName;
            nameText.color = TextDarkColor;
            Text rarityText = CreateText("RelicRarity", root.transform, new Vector2(78f, -42f), new Vector2(180f, 22f), 16, FontStyle.Italic, TextAnchor.UpperLeft);
            rarityText.text = "Relic";
            rarityText.color = RelicColor;
            Text descText = CreateText("RelicDescription", root.transform, new Vector2(12f, -78f), new Vector2(250f, 88f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            descText.text = relic.description;
            descText.color = new Color(0.28f, 0.24f, 0.20f, 1f);
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
            Color accent = GetTreasureTypeColor(choice.rewardType);
            StyleButton(button, accent, false);

            GameObject icon = CreatePanel("Icon", root.transform, accent);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(12f, -12f);
            iconRect.sizeDelta = new Vector2(54f, 54f);

            Text title = CreateText("ChoiceTitle", root.transform, new Vector2(78f, -12f), new Vector2(180f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            title.text = choice.title;
            title.color = TextDarkColor;
            Text type = CreateText("ChoiceType", root.transform, new Vector2(78f, -42f), new Vector2(180f, 22f), 16, FontStyle.Italic, TextAnchor.UpperLeft);
            type.text = choice.rewardType.ToString();
            type.color = accent;
            Text desc = CreateText("ChoiceDescription", root.transform, new Vector2(12f, -78f), new Vector2(250f, 88f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            desc.text = choice.description;
            desc.color = new Color(0.28f, 0.24f, 0.20f, 1f);
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
            GameObject buttonObject = CreatePanel(name, parent, new Color(0.47f, 0.35f, 0.18f, 1f));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200f, 80f);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 200f;
            layout.preferredHeight = 80f;

            Button button = buttonObject.AddComponent<Button>();
            Text labelText = CreateText("Label", buttonObject.transform, new Vector2(20f, -14f), new Vector2(160f, 40f), 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            labelText.text = label;
            labelText.color = TextLightColor;
            StyleButton(button, new Color(0.47f, 0.35f, 0.18f, 1f), false);
            return button;
        }

        private Button CreateNodeButton(string name, Transform parent, string title, string description)
        {
            GameObject buttonObject = CreatePanel(name, parent, new Color(0.34f, 0.47f, 0.35f, 1f));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 180f);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 260f;
            layout.preferredHeight = 180f;

            Button button = buttonObject.AddComponent<Button>();
            StyleNodeButton(button, new Color(0.34f, 0.47f, 0.35f, 1f), true);

            GameObject icon = CreatePanel("Icon", buttonObject.transform, new Color(1f, 1f, 1f, 0.16f));
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 1f);
            iconRect.anchorMax = new Vector2(0f, 1f);
            iconRect.pivot = new Vector2(0f, 1f);
            iconRect.anchoredPosition = new Vector2(16f, -16f);
            iconRect.sizeDelta = new Vector2(56f, 56f);

            Text titleText = CreateText("Title", buttonObject.transform, new Vector2(86f, -18f), new Vector2(150f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            titleText.text = title;
            titleText.color = TextLightColor;
            Text descriptionText = CreateText("Description", buttonObject.transform, new Vector2(16f, -86f), new Vector2(228f, 62f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            descriptionText.text = description;
            descriptionText.color = new Color(0.93f, 0.89f, 0.79f, 1f);

            return button;
        }

        private Transform CreateGridContainer(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, Vector2 cellSize, Vector2 spacing, int columns)
        {
            GameObject container = CreateUiObject(name, parent, Vector2.zero);
            RectTransform rect = container.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            GridLayoutGroup grid = container.AddComponent<GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperLeft;
            return container.transform;
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
            colors.highlightedColor = normalColor * 1.08f;
            colors.pressedColor = normalColor * 0.9f;
            colors.disabledColor = DisabledColor;
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = isDanger ? new Color(1f, 0.93f, 0.90f, 1f) : TextLightColor;
            }
        }

        private Color GetTreasureTypeColor(TreasureRewardType rewardType)
        {
            switch (rewardType)
            {
                case TreasureRewardType.Gold:
                    return GoldColor;
                case TreasureRewardType.Relic:
                    return RelicColor;
                case TreasureRewardType.Upgrade:
                    return UpgradeColor;
                case TreasureRewardType.Heal:
                    return HealColor;
                default:
                    return new Color(0.49f, 0.37f, 0.20f, 1f);
            }
        }

        private Color GetCardTypeColor(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Attack:
                    return new Color(0.72f, 0.31f, 0.22f, 1f);
                case CardType.Skill:
                    return new Color(0.24f, 0.50f, 0.42f, 1f);
                default:
                    return new Color(0.45f, 0.39f, 0.57f, 1f);
            }
        }

        private Color GetCardOwnerColor(HeroId heroId)
        {
            switch (heroId)
            {
                case HeroId.Capybara:
                    return new Color(0.58f, 0.38f, 0.24f, 1f);
                case HeroId.Koala:
                    return new Color(0.47f, 0.42f, 0.58f, 1f);
                case HeroId.Sloth:
                    return new Color(0.36f, 0.57f, 0.39f, 1f);
                case HeroId.Panda:
                    return new Color(0.64f, 0.59f, 0.36f, 1f);
                case HeroId.Kangaroo:
                    return new Color(0.74f, 0.42f, 0.20f, 1f);
                default:
                    return new Color(0.66f, 0.58f, 0.43f, 1f);
            }
        }

        private Color GetShopOfferColor(ShopOfferType offerType)
        {
            switch (offerType)
            {
                case ShopOfferType.Card:
                    return new Color(0.49f, 0.37f, 0.20f, 1f);
                case ShopOfferType.Relic:
                    return RelicColor;
                case ShopOfferType.RemoveCard:
                    return DangerColor;
                case ShopOfferType.Heal:
                    return HealColor;
                case ShopOfferType.UpgradeCard:
                    return UpgradeColor;
                default:
                    return new Color(0.47f, 0.35f, 0.18f, 1f);
            }
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
