using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class BattleUIManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public RunManager runManager;
        public RewardCardManager rewardCardManager;
        public Transform heroContainer;
        public BattleHeroView heroViewPrefab;
        public Transform enemyContainer;
        public EnemyView enemyViewPrefab;
        public Transform handContainer;
        public CardView cardViewPrefab;

        [Header("Prototype Battle")]
        public Text titleText;
        public Text turnText;
        public Text energyText;
        public Text battleLogText;
        public Button drawButton;
        public Button endTurnButton;
        public Button killHero1Button;
        public Button killHero2Button;
        public Button killHero3Button;
        public Button healAllButton;
        public Button winBattleButton;

        private readonly List<BattleHeroView> heroViews = new List<BattleHeroView>();
        private readonly List<EnemyView> enemyViews = new List<EnemyView>();
        private readonly List<CardView> handViews = new List<CardView>();
        private readonly List<EnemyRuntimeState> enemies = new List<EnemyRuntimeState>();

        private const int MaxEnergy = 3;

        private int currentEnergy;
        private bool battleEnded;
        private CardData selectedCard;

        private void Start()
        {
            EnsureRunManager();
            EnsureRuntimeUi();
            BindButtons();
            InitializeBattle();
            RefreshAllUi();
        }

        public void RefreshAllUi()
        {
            if (selectedCard != null && IsCardDisabled(selectedCard, out _))
            {
                ClearCardSelection();
            }

            RefreshTopBar();
            RefreshHeroViews();
            RefreshEnemyViews();
            RefreshHandViews();
            RefreshTargetHighlights();
        }

        public void DrawOneCard()
        {
            if (runManager == null || battleEnded)
            {
                return;
            }

            int drawn = runManager.DrawCards(1).Count;
            AddLog("Player drew " + drawn + " card.");
            RefreshAllUi();
        }

        public void EndTurn()
        {
            if (runManager == null || battleEnded)
            {
                return;
            }

            ClearCardSelection();
            runManager.DiscardHand();
            RunEnemyTurn();
            CheckBattleState();

            if (battleEnded)
            {
                RefreshAllUi();
                return;
            }

            StartPlayerTurn();
            CheckBattleState();
        }

        public void WinBattle()
        {
            if (battleEnded)
            {
                return;
            }

            battleEnded = true;
            AddLog("Battle won.");

            if (rewardCardManager != null)
            {
                rewardCardManager.ShowRewards();
            }

            if (endTurnButton != null)
            {
                endTurnButton.interactable = false;
            }
        }

        private void InitializeBattle()
        {
            runManager.EnsureDirectBattleTestState();

            Debug.Log($"Selected heroes: {runManager.SelectedHeroIds.Count}");

            CreateDefaultEnemies();
            Debug.Log($"Enemies spawned: {enemies.Count}");

            runManager.BuildAndShuffleRunDeck();
            Debug.Log($"Run deck cards: {runManager.RunDeck.Count}");
            Debug.Log($"Draw pile count: {runManager.DrawPile.Count}");

            BuildHeroViews();
            BuildEnemyViews();
            AddLog("Battle started.");
            StartPlayerTurn();
            Debug.Log($"Starting hand: {runManager.Hand.Count}");
        }

        private void StartPlayerTurn()
        {
            ResetHeroBlock();
            currentEnergy = MaxEnergy;
            ClearCardSelection();
            int drawn = runManager.DrawCards(5).Count;
            AddLog($"Player turn started. Drew {drawn} cards.");
            RefreshAllUi();
        }

        private void ResetHeroBlock()
        {
            foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime)
            {
                hero.block = 0;
            }
        }

        private void CreateDefaultEnemies()
        {
            enemies.Clear();
            enemies.Add(CreateEnemy("sporeling", "Sporeling", 50, 6));
            enemies.Add(CreateEnemy("fungus_brute", "Fungus Brute", 78, 12));
            enemies.Add(CreateEnemy("batty", "Batty", 40, 8));
        }

        private EnemyRuntimeState CreateEnemy(string enemyId, string enemyName, int maxHp, int attackDamage)
        {
            return new EnemyRuntimeState
            {
                enemyId = enemyId,
                enemyName = enemyName,
                maxHp = maxHp,
                currentHp = maxHp,
                attackDamage = attackDamage
            };
        }

        private void BuildHeroViews()
        {
            if (heroContainer == null)
            {
                return;
            }

            foreach (Transform child in heroContainer)
            {
                Destroy(child.gameObject);
            }

            heroViews.Clear();

            foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime)
            {
                BattleHeroView heroView = heroViewPrefab != null
                    ? Instantiate(heroViewPrefab, heroContainer)
                    : CreateRuntimeHeroView(heroContainer);

                EnsureHeroViewInteractive(heroView);
                heroView.Setup(HandleHeroClicked);
                heroView.Refresh(hero);
                heroViews.Add(heroView);
            }
        }

        private void BuildEnemyViews()
        {
            if (enemyContainer == null)
            {
                return;
            }

            foreach (Transform child in enemyContainer)
            {
                Destroy(child.gameObject);
            }

            enemyViews.Clear();

            foreach (EnemyRuntimeState enemy in enemies)
            {
                EnemyView enemyView = enemyViewPrefab != null
                    ? Instantiate(enemyViewPrefab, enemyContainer)
                    : CreateRuntimeEnemyView(enemyContainer);

                EnsureEnemyViewInteractive(enemyView);
                enemyView.Setup(HandleEnemyClicked);
                enemyView.Refresh(enemy);
                enemyViews.Add(enemyView);
            }
        }

        private void RefreshHeroViews()
        {
            if (runManager == null)
            {
                return;
            }

            for (int index = 0; index < heroViews.Count && index < runManager.ActiveHeroesRuntime.Count; index++)
            {
                heroViews[index].Refresh(runManager.ActiveHeroesRuntime[index]);
            }
        }

        private void RefreshEnemyViews()
        {
            for (int index = 0; index < enemyViews.Count && index < enemies.Count; index++)
            {
                enemyViews[index].Refresh(enemies[index]);
            }
        }

        private void RefreshHandViews()
        {
            if (runManager == null || handContainer == null)
            {
                return;
            }

            foreach (Transform child in handContainer)
            {
                Destroy(child.gameObject);
            }

            handViews.Clear();

            foreach (CardData card in runManager.Hand)
            {
                CardView cardView = cardViewPrefab != null
                    ? Instantiate(cardViewPrefab, handContainer)
                    : CreateRuntimeCardView(handContainer);

                EnsureCardViewInteractive(cardView);
                cardView.Setup(card, OnCardClicked);

                bool isDisabled = IsCardDisabled(card, out string disabledReason);
                cardView.SetDisabled(isDisabled, disabledReason);
                cardView.SetSelected(selectedCard == card);
                handViews.Add(cardView);
            }
        }

        private void OnCardClicked(CardData card)
        {
            if (battleEnded || card == null)
            {
                return;
            }

            if (selectedCard == card)
            {
                ClearCardSelection();
                RefreshAllUi();
                return;
            }

            if (IsCardDisabled(card, out string disabledReason))
            {
                AddLog(disabledReason + ".");
                RefreshAllUi();
                return;
            }

            if (card.cost > currentEnergy)
            {
                AddLog("Not enough energy.");
                return;
            }

            selectedCard = card;

            if (card.targetType == TargetType.None)
            {
                ResolveCardPlay(card, null, null);
                return;
            }

            RefreshAllUi();
        }

        private void HandleHeroClicked(RuntimeHeroState hero)
        {
            if (selectedCard == null || hero == null)
            {
                return;
            }

            if (!IsValidHeroTarget(selectedCard, hero))
            {
                AddLog("Invalid target.");
                return;
            }

            ResolveCardPlay(selectedCard, hero, null);
        }

        private void HandleEnemyClicked(EnemyRuntimeState enemy)
        {
            if (selectedCard == null || enemy == null)
            {
                return;
            }

            if (!IsValidEnemyTarget(selectedCard, enemy))
            {
                AddLog("Invalid target.");
                return;
            }

            ResolveCardPlay(selectedCard, null, enemy);
        }

        private void ResolveCardPlay(CardData card, RuntimeHeroState chosenHeroTarget, EnemyRuntimeState chosenEnemyTarget)
        {
            if (card == null)
            {
                return;
            }

            if (IsCardDisabled(card, out string disabledReason))
            {
                AddLog(disabledReason + ".");
                return;
            }

            if (card.cost > currentEnergy)
            {
                AddLog("Not enough energy.");
                return;
            }

            RuntimeHeroState ownerHero = GetOwnerHero(card);
            RuntimeHeroState heroTarget = ResolveHeroTarget(card, ownerHero, chosenHeroTarget);
            EnemyRuntimeState enemyTarget = ResolveEnemyTarget(card, chosenEnemyTarget);

            currentEnergy -= card.cost;

            string sourceName = ownerHero != null ? ownerHero.heroData.heroName : "Neutral";
            string targetName = enemyTarget != null
                ? enemyTarget.enemyName
                : heroTarget != null
                    ? heroTarget.heroData.heroName
                    : "no target";

            AddLog($"{sourceName} used {card.cardName} on {targetName}.");

            if (card.damage > 0 && enemyTarget != null)
            {
                enemyTarget.TakeDamage(card.damage);
            }

            if (card.block > 0 && heroTarget != null)
            {
                heroTarget.GainBlock(card.block);
                AddLog($"{heroTarget.heroData.heroName} gained {card.block} block.");
            }

            if (card.heal > 0 && heroTarget != null)
            {
                int beforeHp = heroTarget.currentHp;
                heroTarget.Heal(card.heal);
                int healedAmount = heroTarget.currentHp - beforeHp;
                AddLog($"{heroTarget.heroData.heroName} healed {healedAmount} HP.");
            }

            if (card.drawAmount > 0)
            {
                int drawn = runManager.DrawCards(card.drawAmount).Count;
                AddLog($"Player drew {drawn} cards.");
            }

            if (card.strengthAmount > 0)
            {
                AddLog($"{sourceName} gained {card.strengthAmount} Strength.");
            }

            if (card.weakAmount > 0 && enemyTarget != null)
            {
                AddLog($"{enemyTarget.enemyName} received {card.weakAmount} Weak.");
            }

            if (card.taunt && heroTarget != null)
            {
                AddLog($"{heroTarget.heroData.heroName} gained Taunt.");
            }

            runManager.DiscardCard(card);
            ClearCardSelection();
            RefreshAllUi();
            CheckBattleState();
        }

        private RuntimeHeroState ResolveHeroTarget(CardData card, RuntimeHeroState ownerHero, RuntimeHeroState chosenHeroTarget)
        {
            if (card.targetType == TargetType.Self)
            {
                return ownerHero;
            }

            if (card.targetType == TargetType.Ally)
            {
                return chosenHeroTarget ?? runManager.ActiveHeroesRuntime.FirstOrDefault(hero => hero.IsAlive);
            }

            if (card.targetType == TargetType.AllAllies)
            {
                return ownerHero ?? runManager.ActiveHeroesRuntime.FirstOrDefault(hero => hero.IsAlive);
            }

            return ownerHero ?? chosenHeroTarget;
        }

        private EnemyRuntimeState ResolveEnemyTarget(CardData card, EnemyRuntimeState chosenEnemyTarget)
        {
            if (card.targetType == TargetType.Enemy || card.targetType == TargetType.AllEnemies)
            {
                return chosenEnemyTarget ?? enemies.FirstOrDefault(enemy => enemy.IsAlive);
            }

            return null;
        }

        private void RunEnemyTurn()
        {
            RuntimeHeroState targetHero = runManager.ActiveHeroesRuntime.FirstOrDefault(hero => hero.IsAlive);
            if (targetHero == null)
            {
                return;
            }

            foreach (EnemyRuntimeState enemy in enemies.Where(enemy => enemy.IsAlive))
            {
                targetHero.TakeDamage(enemy.attackDamage);
                AddLog($"{enemy.enemyName} attacked {targetHero.heroData.heroName} for {enemy.attackDamage}.");

                if (!targetHero.IsAlive)
                {
                    AddLog($"{targetHero.heroData.heroName} is down.");
                    targetHero = runManager.ActiveHeroesRuntime.FirstOrDefault(hero => hero.IsAlive);
                    if (targetHero == null)
                    {
                        break;
                    }
                }
            }
        }

        private void CheckBattleState()
        {
            if (!battleEnded && enemies.Count > 0 && enemies.All(enemy => !enemy.IsAlive))
            {
                WinBattle();
                return;
            }

            if (!battleEnded && runManager.ActiveHeroesRuntime.Count > 0 && runManager.ActiveHeroesRuntime.All(hero => !hero.IsAlive))
            {
                battleEnded = true;
                AddLog("Battle lost.");
            }

            if (battleEnded && endTurnButton != null)
            {
                endTurnButton.interactable = false;
            }
        }

        private void RefreshTopBar()
        {
            if (titleText != null)
            {
                titleText.text = "Paw Slayers - Battle Prototype";
            }

            if (turnText != null)
            {
                turnText.text = battleEnded ? "Turn: Battle Ended" : "Turn: Player";
            }

            if (energyText != null)
            {
                energyText.text = $"Energy: {currentEnergy}/{MaxEnergy}";
            }
        }

        private bool IsCardDisabled(CardData card, out string reason)
        {
            if (!runManager.CanPlayCard(card, out reason))
            {
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private bool IsValidHeroTarget(CardData card, RuntimeHeroState hero)
        {
            if (card == null || hero == null || !hero.IsAlive)
            {
                return false;
            }

            RuntimeHeroState ownerHero = GetOwnerHero(card);
            switch (card.targetType)
            {
                case TargetType.Self:
                    return ownerHero == hero;
                case TargetType.Ally:
                case TargetType.AllAllies:
                    return true;
                default:
                    return false;
            }
        }

        private bool IsValidEnemyTarget(CardData card, EnemyRuntimeState enemy)
        {
            if (card == null || enemy == null || !enemy.IsAlive)
            {
                return false;
            }

            return card.targetType == TargetType.Enemy || card.targetType == TargetType.AllEnemies;
        }

        private void RefreshTargetHighlights()
        {
            foreach (BattleHeroView heroView in heroViews)
            {
                bool isHighlighted = selectedCard != null && heroView.HeroState != null && IsValidHeroTarget(selectedCard, heroView.HeroState);
                heroView.SetTargetHighlight(isHighlighted);
            }

            foreach (EnemyView enemyView in enemyViews)
            {
                bool isHighlighted = selectedCard != null && enemyView.EnemyState != null && IsValidEnemyTarget(selectedCard, enemyView.EnemyState);
                enemyView.SetTargetHighlight(isHighlighted);
            }
        }

        private RuntimeHeroState GetOwnerHero(CardData card)
        {
            if (card == null || card.ownerHeroId == HeroId.Neutral)
            {
                return null;
            }

            return runManager.GetHeroState(card.ownerHeroId);
        }

        private void ClearCardSelection()
        {
            selectedCard = null;
        }

        private void AddLog(string message)
        {
            if (battleLogText == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(battleLogText.text))
            {
                battleLogText.text = message;
            }
            else
            {
                battleLogText.text = message + "\n" + battleLogText.text;
            }
        }

        private void EnsureRunManager()
        {
            if (runManager == null)
            {
                runManager = RunManager.Instance;
            }

            if (runManager != null)
            {
                return;
            }

            GameObject runManagerObject = new GameObject("RunManager");
            runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();
        }

        private void BindButtons()
        {
            if (drawButton != null)
            {
                drawButton.onClick.RemoveAllListeners();
                drawButton.onClick.AddListener(DrawOneCard);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(EndTurn);
            }

            if (killHero1Button != null)
            {
                killHero1Button.onClick.RemoveAllListeners();
                killHero1Button.onClick.AddListener(() => MarkHeroDownByIndex(0));
            }

            if (killHero2Button != null)
            {
                killHero2Button.onClick.RemoveAllListeners();
                killHero2Button.onClick.AddListener(() => MarkHeroDownByIndex(1));
            }

            if (killHero3Button != null)
            {
                killHero3Button.onClick.RemoveAllListeners();
                killHero3Button.onClick.AddListener(() => MarkHeroDownByIndex(2));
            }

            if (healAllButton != null)
            {
                healAllButton.onClick.RemoveAllListeners();
                healAllButton.onClick.AddListener(HealAllHeroes);
            }

            if (winBattleButton != null)
            {
                winBattleButton.onClick.RemoveAllListeners();
                winBattleButton.onClick.AddListener(WinBattle);
            }
        }

        private void MarkHeroDownByIndex(int index)
        {
            if (index < 0 || index >= runManager.ActiveHeroesRuntime.Count)
            {
                return;
            }

            runManager.MarkHeroDown(runManager.ActiveHeroesRuntime[index].heroData.heroId);
            AddLog($"{runManager.ActiveHeroesRuntime[index].heroData.heroName} is down.");
            RefreshAllUi();
            CheckBattleState();
        }

        private void HealAllHeroes()
        {
            runManager.HealAllHeroes();
            AddLog("All heroes healed.");
            RefreshAllUi();
        }

        private void EnsureRuntimeUi()
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
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            if (titleText != null && heroContainer != null && enemyContainer != null && handContainer != null)
            {
                return;
            }

            foreach (Transform child in canvas.transform)
            {
                Destroy(child.gameObject);
            }

            GameObject root = CreatePanel("BattleRoot", canvas.transform, new Color(0.89f, 0.93f, 0.96f, 1f));
            StretchFull(root.GetComponent<RectTransform>(), 18f);

            titleText = CreateText("Title", root.transform, new Vector2(20f, -20f), new Vector2(600f, 40f), 32, FontStyle.Bold, TextAnchor.UpperLeft);
            turnText = CreateText("TurnText", root.transform, new Vector2(20f, -68f), new Vector2(220f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            energyText = CreateText("EnergyText", root.transform, new Vector2(260f, -68f), new Vector2(220f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);

            GameObject fieldArea = CreateUiObject("FieldArea", root.transform, Vector2.zero);
            RectTransform fieldRect = fieldArea.GetComponent<RectTransform>();
            fieldRect.anchorMin = new Vector2(0f, 0f);
            fieldRect.anchorMax = new Vector2(1f, 1f);
            fieldRect.offsetMin = new Vector2(20f, 290f);
            fieldRect.offsetMax = new Vector2(-240f, -120f);

            GameObject heroPanel = CreatePanel("HeroPanel", fieldArea.transform, new Color(0.85f, 0.91f, 0.84f, 1f));
            RectTransform heroPanelRect = heroPanel.GetComponent<RectTransform>();
            heroPanelRect.anchorMin = new Vector2(0f, 0f);
            heroPanelRect.anchorMax = new Vector2(0.48f, 1f);
            heroPanelRect.offsetMin = Vector2.zero;
            heroPanelRect.offsetMax = new Vector2(-10f, 0f);
            CreateText("HeroesLabel", heroPanel.transform, new Vector2(12f, -12f), new Vector2(240f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft).text = "Heroes";
            heroContainer = CreateLayoutContainer("HeroContainer", heroPanel.transform, true, new Vector2(12f, 12f), new Vector2(-12f, -48f));

            GameObject enemyPanel = CreatePanel("EnemyPanel", fieldArea.transform, new Color(0.94f, 0.85f, 0.85f, 1f));
            RectTransform enemyPanelRect = enemyPanel.GetComponent<RectTransform>();
            enemyPanelRect.anchorMin = new Vector2(0.52f, 0f);
            enemyPanelRect.anchorMax = new Vector2(1f, 1f);
            enemyPanelRect.offsetMin = new Vector2(10f, 0f);
            enemyPanelRect.offsetMax = Vector2.zero;
            CreateText("EnemiesLabel", enemyPanel.transform, new Vector2(12f, -12f), new Vector2(240f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft).text = "Enemies";
            enemyContainer = CreateLayoutContainer("EnemyContainer", enemyPanel.transform, true, new Vector2(12f, 12f), new Vector2(-12f, -48f));

            GameObject handPanel = CreatePanel("HandPanel", root.transform, new Color(0.95f, 0.93f, 0.87f, 1f));
            RectTransform handPanelRect = handPanel.GetComponent<RectTransform>();
            handPanelRect.anchorMin = new Vector2(0f, 0f);
            handPanelRect.anchorMax = new Vector2(1f, 0f);
            handPanelRect.pivot = new Vector2(0.5f, 0f);
            handPanelRect.offsetMin = new Vector2(20f, 20f);
            handPanelRect.offsetMax = new Vector2(-20f, 260f);
            CreateText("HandLabel", handPanel.transform, new Vector2(12f, -12f), new Vector2(200f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft).text = "Hand";
            handContainer = CreateLayoutContainer("HandContainer", handPanel.transform, false, new Vector2(12f, 12f), new Vector2(-12f, -48f));

            GameObject logPanel = CreatePanel("LogPanel", root.transform, new Color(0.85f, 0.89f, 0.94f, 1f));
            RectTransform logRect = logPanel.GetComponent<RectTransform>();
            logRect.anchorMin = new Vector2(0f, 0f);
            logRect.anchorMax = new Vector2(0f, 0f);
            logRect.pivot = new Vector2(0f, 0f);
            logRect.anchoredPosition = new Vector2(20f, 260f);
            logRect.sizeDelta = new Vector2(520f, 130f);
            battleLogText = CreateText("BattleLog", logPanel.transform, new Vector2(12f, -12f), new Vector2(496f, 106f), 16, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject debugPanel = CreatePanel("DebugPanel", root.transform, new Color(0.82f, 0.82f, 0.82f, 1f));
            RectTransform debugRect = debugPanel.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(1f, 0.5f);
            debugRect.anchorMax = new Vector2(1f, 0.5f);
            debugRect.pivot = new Vector2(1f, 0.5f);
            debugRect.anchoredPosition = new Vector2(-20f, 0f);
            debugRect.sizeDelta = new Vector2(200f, 420f);
            CreateText("DebugLabel", debugPanel.transform, new Vector2(12f, -12f), new Vector2(180f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft).text = "Debug";

            Transform debugButtons = CreateLayoutContainer("DebugButtons", debugPanel.transform, true, new Vector2(12f, 12f), new Vector2(-12f, -48f));
            drawButton = CreateButton("DrawButton", debugButtons, "Draw Card");
            endTurnButton = CreateButton("EndTurnButton", debugButtons, "End Turn");
            killHero1Button = CreateButton("KillHero1Button", debugButtons, "Kill Hero 1");
            killHero2Button = CreateButton("KillHero2Button", debugButtons, "Kill Hero 2");
            killHero3Button = CreateButton("KillHero3Button", debugButtons, "Kill Hero 3");
            healAllButton = CreateButton("HealAllButton", debugButtons, "Heal All");
            winBattleButton = CreateButton("WinBattleButton", debugButtons, "Win Battle");

            if (rewardCardManager == null)
            {
                rewardCardManager = gameObject.GetComponent<RewardCardManager>();
                if (rewardCardManager == null)
                {
                    rewardCardManager = gameObject.AddComponent<RewardCardManager>();
                }
            }

            GameObject rewardPanel = CreatePanel("RewardPanel", root.transform, new Color(0f, 0f, 0f, 0.7f));
            StretchFull(rewardPanel.GetComponent<RectTransform>(), 0f);
            rewardPanel.SetActive(false);

            GameObject rewardBox = CreatePanel("RewardBox", rewardPanel.transform, new Color(0.97f, 0.95f, 0.88f, 1f));
            RectTransform rewardBoxRect = rewardBox.GetComponent<RectTransform>();
            rewardBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            rewardBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            rewardBoxRect.pivot = new Vector2(0.5f, 0.5f);
            rewardBoxRect.sizeDelta = new Vector2(760f, 420f);
            rewardBoxRect.anchoredPosition = Vector2.zero;

            rewardCardManager.rewardTitleText = CreateText("RewardTitle", rewardBox.transform, new Vector2(20f, -20f), new Vector2(300f, 32f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            rewardCardManager.rewardContainer = CreateLayoutContainer("RewardContainer", rewardBox.transform, false, new Vector2(20f, 20f), new Vector2(-20f, -70f));
            rewardCardManager.rewardPanel = rewardPanel;
            rewardCardManager.rewardCardPrefab = cardViewPrefab;
        }

        private void EnsureHeroViewInteractive(BattleHeroView view)
        {
            if (view.button == null)
            {
                view.button = view.gameObject.GetComponent<Button>();
                if (view.button == null)
                {
                    view.button = view.gameObject.AddComponent<Button>();
                }
            }

            if (view.highlightOutline == null)
            {
                view.highlightOutline = view.gameObject.GetComponent<Outline>();
                if (view.highlightOutline == null)
                {
                    view.highlightOutline = view.gameObject.AddComponent<Outline>();
                }

                view.highlightOutline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
                view.highlightOutline.effectDistance = new Vector2(4f, -4f);
                view.highlightOutline.enabled = false;
            }
        }

        private void EnsureEnemyViewInteractive(EnemyView view)
        {
            if (view.button == null)
            {
                view.button = view.gameObject.GetComponent<Button>();
                if (view.button == null)
                {
                    view.button = view.gameObject.AddComponent<Button>();
                }
            }

            if (view.highlightOutline == null)
            {
                view.highlightOutline = view.gameObject.GetComponent<Outline>();
                if (view.highlightOutline == null)
                {
                    view.highlightOutline = view.gameObject.AddComponent<Outline>();
                }

                view.highlightOutline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
                view.highlightOutline.effectDistance = new Vector2(4f, -4f);
                view.highlightOutline.enabled = false;
            }
        }

        private void EnsureCardViewInteractive(CardView view)
        {
            if (view.button == null)
            {
                view.button = view.gameObject.GetComponent<Button>();
                if (view.button == null)
                {
                    view.button = view.gameObject.AddComponent<Button>();
                }
            }

            if (view.canvasGroup == null)
            {
                view.canvasGroup = view.gameObject.GetComponent<CanvasGroup>();
                if (view.canvasGroup == null)
                {
                    view.canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (view.selectionOutline == null)
            {
                view.selectionOutline = view.gameObject.GetComponent<Outline>();
                if (view.selectionOutline == null)
                {
                    view.selectionOutline = view.gameObject.AddComponent<Outline>();
                }

                view.selectionOutline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
                view.selectionOutline.effectDistance = new Vector2(4f, -4f);
                view.selectionOutline.enabled = false;
            }
        }

        private BattleHeroView CreateRuntimeHeroView(Transform parent)
        {
            GameObject root = CreatePanel("BattleHeroView", parent, new Color(0.86f, 0.93f, 0.86f, 1f));
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 140f;
            Button button = root.AddComponent<Button>();
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            BattleHeroView view = root.AddComponent<BattleHeroView>();
            view.backgroundImage = root.GetComponent<Image>();
            view.button = button;
            view.highlightOutline = outline;
            view.heroNameText = CreateText("HeroName", root.transform, new Vector2(12f, -12f), new Vector2(220f, 24f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            view.heroClassText = CreateText("HeroClass", root.transform, new Vector2(12f, -38f), new Vector2(220f, 22f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            view.hpText = CreateText("HpText", root.transform, new Vector2(12f, -72f), new Vector2(220f, 22f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            view.blockText = CreateText("BlockText", root.transform, new Vector2(12f, -98f), new Vector2(120f, 22f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            view.stateText = CreateText("StateText", root.transform, new Vector2(12f, -120f), new Vector2(160f, 22f), 18, FontStyle.Bold, TextAnchor.UpperLeft);

            GameObject portrait = CreatePanel("Portrait", root.transform, new Color(0.75f, 0.75f, 0.75f, 1f));
            RectTransform portraitRect = portrait.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(1f, 1f);
            portraitRect.anchorMax = new Vector2(1f, 1f);
            portraitRect.pivot = new Vector2(1f, 1f);
            portraitRect.anchoredPosition = new Vector2(-12f, -12f);
            portraitRect.sizeDelta = new Vector2(60f, 60f);
            view.portraitImage = portrait.GetComponent<Image>();
            return view;
        }

        private EnemyView CreateRuntimeEnemyView(Transform parent)
        {
            GameObject root = CreatePanel("EnemyView", parent, new Color(0.93f, 0.84f, 0.84f, 1f));
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 120f;
            Button button = root.AddComponent<Button>();
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            EnemyView view = root.AddComponent<EnemyView>();
            view.backgroundImage = root.GetComponent<Image>();
            view.button = button;
            view.highlightOutline = outline;
            view.enemyNameText = CreateText("EnemyName", root.transform, new Vector2(12f, -12f), new Vector2(220f, 24f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            view.hpText = CreateText("HpText", root.transform, new Vector2(12f, -48f), new Vector2(220f, 22f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            view.intentText = CreateText("IntentText", root.transform, new Vector2(12f, -76f), new Vector2(220f, 22f), 18, FontStyle.Bold, TextAnchor.UpperLeft);
            return view;
        }

        private CardView CreateRuntimeCardView(Transform parent)
        {
            GameObject root = CreatePanel("CardView", parent, Color.white);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 250f;
            layout.preferredHeight = 320f;

            Button button = root.AddComponent<Button>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            CardView view = root.AddComponent<CardView>();
            view.backgroundImage = root.GetComponent<Image>();
            view.button = button;
            view.canvasGroup = canvasGroup;
            view.selectionOutline = outline;
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

            view.descriptionText = CreateText("Description", root.transform, new Vector2(10f, -208f), new Vector2(230f, 74f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            view.disabledReasonText = CreateText("DisabledReason", root.transform, new Vector2(10f, -286f), new Vector2(230f, 28f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            view.disabledReasonText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
            return view;
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
            GameObject buttonObject = CreatePanel(name, parent, new Color(0.36f, 0.55f, 0.31f, 1f));
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 42f;

            Button button = buttonObject.AddComponent<Button>();
            CreateText("Label", buttonObject.transform, new Vector2(0f, 0f), new Vector2(180f, 42f), 18, FontStyle.Bold, TextAnchor.MiddleCenter).text = label;
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
