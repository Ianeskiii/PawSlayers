using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PawSlayers
{
    public class RunManager : MonoBehaviour
    {
        private static readonly HashSet<string> PrototypeStarterCardIds = new HashSet<string>
        {
            "swift_slash",
            "guard_stance",
            "pommel_tap",
            "shadow_strike",
            "smoke_step",
            "muzzle_trick",
            "staff_tap",
            "soothing_light",
            "quiet_blessing",
            "shield_bash",
            "barkskin_guard",
            "power_combo",
            "battle_focus",
            "snack_time",
            "quick_guard"
        };

        public static RunManager Instance { get; private set; }

        [Header("Data")]
        public HeroDatabase heroDatabase;
        public CardDatabase cardDatabase;
        public List<RelicData> relicDatabase = new List<RelicData>();
        public HeroProgressionManager progressionManager;
        public RunSaveManager runSaveManager;

        [Header("Scenes")]
        public string mainMenuSceneName = "MainMenu";
        public string heroSelectionSceneName = "HeroSelection";
        public string battleSceneName = "Battle";
        public string mapSceneName = "Map";
        public string heroProgressionSceneName = "HeroProgression";

        [Header("Run State")]
        [SerializeField] private List<HeroId> selectedHeroIds = new List<HeroId>();
        [SerializeField] private List<RuntimeHeroState> activeHeroesRuntime = new List<RuntimeHeroState>();
        [SerializeField] private List<RuntimeCardState> currentRunDeck = new List<RuntimeCardState>();
        [SerializeField] private int currentBattleIndex;
        [SerializeField] private int totalNormalBattlesBeforeBoss = 3;
        [SerializeField] private bool bossBattleStarted;
        [SerializeField] private bool runWon;
        [SerializeField] private bool runLost;
        [SerializeField] private MapNodeType selectedMapNodeType = MapNodeType.None;
        [SerializeField] private bool supportNodeUsedThisStage;
        [SerializeField] private bool pendingBetweenBattleRecovery;
        [SerializeField] private List<RelicId> ownedRelics = new List<RelicId>();
        [SerializeField] private List<RelicId> availableRelicsPool = new List<RelicId>();
        [SerializeField] private int gold;

        private DeckManager deckManager;
        private readonly Dictionary<HeroId, RunHeroProgressSummary> runProgressSummaries = new Dictionary<HeroId, RunHeroProgressSummary>();

        public List<HeroId> SelectedHeroIds => selectedHeroIds;
        public List<RuntimeHeroState> ActiveHeroesRuntime => activeHeroesRuntime;
        public List<RuntimeCardState> CurrentRunDeck => currentRunDeck;
        public int CurrentBattleIndex => currentBattleIndex;
        public int TotalNormalBattlesBeforeBoss => totalNormalBattlesBeforeBoss;
        public bool BossBattleStarted => bossBattleStarted;
        public bool RunWon => runWon;
        public bool RunLost => runLost;
        public bool IsBossBattle => bossBattleStarted;
        public MapNodeType SelectedMapNodeType => selectedMapNodeType;
        public bool SupportNodeUsedThisStage => supportNodeUsedThisStage;
        public bool PendingBetweenBattleRecovery => pendingBetweenBattleRecovery;
        public List<RelicId> OwnedRelics => ownedRelics;
        public int Gold => gold;
        public HeroProgressionManager ProgressionManager => progressionManager;
        public RunSaveManager SaveManager => runSaveManager;
        public List<RuntimeCardState> RunDeck => deckManager == null ? new List<RuntimeCardState>() : deckManager.RunDeck.ToList();
        public List<RuntimeCardState> DiscardPile => deckManager == null ? new List<RuntimeCardState>() : deckManager.DiscardPile.ToList();
        public List<RuntimeCardState> DrawPile => deckManager == null ? new List<RuntimeCardState>() : deckManager.DrawPile.ToList();
        public List<RuntimeCardState> Hand => deckManager == null ? new List<RuntimeCardState>() : deckManager.Hand.ToList();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            deckManager = GetComponent<DeckManager>();
            if (deckManager == null)
            {
                deckManager = gameObject.AddComponent<DeckManager>();
            }

            progressionManager = GetComponent<HeroProgressionManager>();
            if (progressionManager == null)
            {
                progressionManager = gameObject.AddComponent<HeroProgressionManager>();
            }

            runSaveManager = GetComponent<RunSaveManager>();
            if (runSaveManager == null)
            {
                runSaveManager = gameObject.AddComponent<RunSaveManager>();
            }

            EnsurePrototypeData();
            runSaveManager.Initialize(this);
        }

        public void StartRunWithSelection(List<HeroId> selectedHeroes)
        {
            EnsurePrototypeData();
            ResetRunState();
            SetSelectedHeroes(selectedHeroes);
            InitializeCurrentRunDeckIfNeeded();
            currentBattleIndex = 1;
            bossBattleStarted = false;
            runWon = false;
            runLost = false;
            selectedMapNodeType = MapNodeType.None;
            supportNodeUsedThisStage = false;
            pendingBetweenBattleRecovery = false;
            InitializeRelicPool();
            PrepareEncounterDeck();
            AutoSaveCurrentRun(battleSceneName);

            Debug.Log("Loading BattleScene with selected heroes: " + string.Join(", ", selectedHeroIds));
            LoadConfiguredScene(battleSceneName);
        }

        public void PrepareForNewRun()
        {
            EnsurePrototypeData();
            ResetRunState();
            Debug.Log("Run state reset for new run.");
        }

        public void ReturnToMainMenu()
        {
            LoadConfiguredScene(mainMenuSceneName);
        }

        public void ResetRunAndReturnToSelection()
        {
            DeleteCurrentRunSave();
            ResetRunState();
            LoadConfiguredScene(heroSelectionSceneName);
        }

        public void EnterMapAfterBattleReward()
        {
            if (runWon)
            {
                return;
            }

            pendingBetweenBattleRecovery = true;
            selectedMapNodeType = MapNodeType.None;
            Debug.Log("Entering map.");
            Debug.Log("Current battle index: " + currentBattleIndex);
            Debug.Log("Deck count: " + currentRunDeck.Count);
            Debug.Log("Owned relics: " + string.Join(", ", ownedRelics));
            Debug.Log("Current gold: " + gold);
            AutoSaveCurrentRun(mapSceneName);
            LoadConfiguredScene(mapSceneName);
        }

        public RuntimeHeroState GetHeroState(HeroId heroId)
        {
            return activeHeroesRuntime.FirstOrDefault(hero => hero.heroData != null && hero.heroData.heroId == heroId);
        }

        public bool CanPlayCard(RuntimeCardState card, out string reason)
        {
            return CardRules.CanPlayCard(card, selectedHeroIds, activeHeroesRuntime, out reason);
        }

        public List<RuntimeCardState> DrawCards(int amount)
        {
            return deckManager.DrawCards(amount);
        }

        public void DiscardCard(RuntimeCardState card)
        {
            deckManager.DiscardCard(card);
        }

        public void AddTemporaryCardToDiscard(RuntimeCardState card)
        {
            if (deckManager == null)
            {
                return;
            }

            deckManager.AddCardToDiscard(card);
        }

        public void RemoveTemporaryCards(string cardId)
        {
            if (deckManager == null || string.IsNullOrWhiteSpace(cardId))
            {
                return;
            }

            deckManager.RemoveCardsWhere(card => card != null && card.baseCard != null && card.baseCard.cardId == cardId);
        }

        public void DiscardHand()
        {
            deckManager.DiscardHand();
        }

        public void MarkHeroDown(HeroId heroId)
        {
            RuntimeHeroState hero = GetHeroState(heroId);
            if (hero == null)
            {
                return;
            }

            hero.currentHp = 0;
            hero.isDown = true;
            hero.block = 0;
        }

        public void AddRewardCard(CardData card)
        {
            if (card == null)
            {
                return;
            }

            RuntimeCardState runtimeCard = RuntimeCardState.Create(card);
            AddRewardCard(runtimeCard);
        }

        public void AddRewardCard(RuntimeCardState runtimeCard)
        {
            if (runtimeCard == null || runtimeCard.baseCard == null)
            {
                return;
            }

            InitializeCurrentRunDeckIfNeeded();
            currentRunDeck.Add(runtimeCard);

            if (deckManager != null)
            {
                deckManager.AddCardToDeck(runtimeCard);
            }

            Debug.Log("Reward selected: " + runtimeCard.DisplayName);
            Debug.Log("Updated run deck count: " + currentRunDeck.Count);
            AutoSaveCurrentRun(mapSceneName);
        }

        public bool UpgradeCard(RuntimeCardState card)
        {
            if (card == null)
            {
                return false;
            }

            bool upgraded = card.Upgrade();
            if (!upgraded)
            {
                return false;
            }

            int upgradedCount = currentRunDeck.Count(runCard => runCard != null && runCard.isUpgraded);
            Debug.Log("Card upgraded: " + card.DisplayName);
            Debug.Log("Upgraded card values: " + card.Description);
            Debug.Log("Current deck upgraded count: " + upgradedCount);
            AutoSaveCurrentRun(mapSceneName);
            return true;
        }

        public List<RuntimeCardState> GetUpgradeableCards()
        {
            return currentRunDeck.Where(card => card != null && card.CanUpgrade).ToList();
        }

        public bool RemoveCardFromRunDeck(RuntimeCardState card)
        {
            if (card == null || currentRunDeck == null || currentRunDeck.Count <= 5)
            {
                return false;
            }

            bool removed = currentRunDeck.Remove(card);
            if (!removed)
            {
                return false;
            }

            if (deckManager != null)
            {
                deckManager.RemoveCardsWhere(runCard => runCard != null && runCard.runtimeId == card.runtimeId);
            }

            Debug.Log("Current deck count: " + currentRunDeck.Count);
            AutoSaveCurrentRun(mapSceneName);
            return true;
        }

        public bool HasRelic(RelicId relicId)
        {
            return ownedRelics.Contains(relicId);
        }

        public List<RelicData> GetOwnedRelicData()
        {
            return ownedRelics
                .Select(GetRelicData)
                .Where(relic => relic != null)
                .ToList();
        }

        public RelicData GetRelicData(RelicId relicId)
        {
            EnsurePrototypeData();
            return relicDatabase.FirstOrDefault(relic => relic != null && relic.relicId == relicId);
        }

        public List<RelicData> GetAvailableRelicChoices(int count)
        {
            EnsurePrototypeData();
            InitializeRelicPool();

            return availableRelicsPool
                .Select(GetRelicData)
                .Where(relic => relic != null)
                .OrderBy(_ => UnityEngine.Random.value)
                .Take(count)
                .ToList();
        }

        public bool GainRelic(RelicId relicId)
        {
            if (relicId == RelicId.None)
            {
                return false;
            }

            InitializeRelicPool();

            if (ownedRelics.Contains(relicId))
            {
                Debug.Log("Relic already owned skipped: " + relicId);
                return false;
            }

            ownedRelics.Add(relicId);
            availableRelicsPool.Remove(relicId);

            RelicData relic = GetRelicData(relicId);
            Debug.Log("Relic gained: " + (relic != null ? relic.relicName : relicId.ToString()));

            if (relicId == RelicId.IronSnack)
            {
                ApplyIronSnack();
            }

            AutoSaveCurrentRun(mapSceneName);

            return true;
        }

        public void AddGold(int amount)
        {
            gold += Mathf.Max(0, amount);
            Debug.Log("Current gold: " + gold);
            AutoSaveCurrentRun(mapSceneName);
        }

        public bool TrySpendGold(int amount)
        {
            int spendAmount = Mathf.Max(0, amount);
            if (gold < spendAmount)
            {
                Debug.Log("Not enough gold.");
                Debug.Log("Current gold: " + gold);
                return false;
            }

            gold -= spendAmount;
            Debug.Log("Current gold: " + gold);
            AutoSaveCurrentRun(mapSceneName);
            return true;
        }

        public void MarkTreasureNodeResolved()
        {
            SelectMapNode(MapNodeType.Treasure);
            supportNodeUsedThisStage = true;
            AutoSaveCurrentRun(mapSceneName);
        }

        public void MarkShopNodeResolved()
        {
            SelectMapNode(MapNodeType.Shop);
            supportNodeUsedThisStage = true;
            AutoSaveCurrentRun(mapSceneName);
        }

        public void ApplyTreasureHealReward()
        {
            foreach (RuntimeHeroState hero in activeHeroesRuntime)
            {
                if (hero == null || hero.heroData == null)
                {
                    continue;
                }

                hero.block = 0;

                if (hero.IsAlive)
                {
                    int healAmount = Mathf.CeilToInt(hero.MaxHp * 0.2f);
                    hero.Heal(healAmount);
                }
                else
                {
                    hero.currentHp = Mathf.Max(1, Mathf.CeilToInt(hero.MaxHp * 0.15f));
                    hero.isDown = false;
                }
            }

            AutoSaveCurrentRun(mapSceneName);
        }

        public void ApplyShopHealReward()
        {
            foreach (RuntimeHeroState hero in activeHeroesRuntime)
            {
                if (hero == null || hero.heroData == null)
                {
                    continue;
                }

                hero.block = 0;

                if (hero.IsAlive)
                {
                    int healAmount = Mathf.CeilToInt(hero.MaxHp * 0.25f);
                    hero.Heal(healAmount);
                }
                else
                {
                    hero.currentHp = Mathf.Max(1, Mathf.CeilToInt(hero.MaxHp * 0.2f));
                    hero.isDown = false;
                }
            }

            AutoSaveCurrentRun(mapSceneName);
        }

        public string GetRelicSummaryText()
        {
            List<RelicData> relics = GetOwnedRelicData();
            if (relics.Count == 0)
            {
                return "Relics: None";
            }

            return "Relics: " + string.Join(" | ", relics.Select(relic => relic.relicName));
        }

        public string GetResourceSummaryText()
        {
            return $"Gold: {gold}\n{GetRelicSummaryText()}";
        }

        public void HealAllHeroes()
        {
            foreach (RuntimeHeroState hero in activeHeroesRuntime)
            {
                hero?.Heal(999);
                if (hero != null)
                {
                    hero.block = 0;
                    hero.statuses.ClearAll();
                }
            }
        }

        public void GrantNormalBattleXp()
        {
            GrantXpToSelectedHeroes(10);
        }

        public void GrantBossBattleXp()
        {
            GrantXpToSelectedHeroes(40);
        }

        public void GrantRunLossXp()
        {
            GrantXpToSelectedHeroes(5);
        }

        public string GetRunSummaryText(bool wasRunWon)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine(wasRunWon ? "Run Won!" : "Run Lost!");
            summary.AppendLine("Your party's journey has ended.");
            summary.AppendLine();
            summary.AppendLine($"Gold Collected: {gold}");
            summary.AppendLine($"Relics Found: {ownedRelics.Count}");
            summary.AppendLine();
            summary.AppendLine("Party Progress:");

            foreach (HeroId heroId in selectedHeroIds)
            {
                if (!runProgressSummaries.TryGetValue(heroId, out RunHeroProgressSummary heroSummary))
                {
                    HeroProgressionState progress = progressionManager != null ? progressionManager.GetProgress(heroId) : null;
                    string heroName = progress != null ? progress.heroName : heroId.ToString();
                    summary.AppendLine($"{heroName} - +0 XP");
                    continue;
                }

                string line = $"{heroSummary.heroName} - +{heroSummary.totalXpGained} XP";
                if (heroSummary.endLevel > heroSummary.startLevel)
                {
                    line += $" - Level {heroSummary.endLevel} reached!";
                }

                summary.AppendLine(line);

                foreach (string unlockedMessage in heroSummary.unlockedMessages)
                {
                    summary.AppendLine("  " + unlockedMessage);
                }
            }

            return summary.ToString().TrimEnd();
        }

        public void ResetHeroProgressionForTesting()
        {
            progressionManager?.ResetProgressionForTesting();
        }

        public void AddXpToSelectedHeroesDebug(int amount)
        {
            GrantXpToSelectedHeroes(amount);
        }

        public void AddXpToAllHeroesDebug(int amount)
        {
            if (progressionManager == null || heroDatabase == null)
            {
                return;
            }

            List<HeroId> allHeroes = heroDatabase.heroes
                .Where(hero => hero != null && hero.heroId != HeroId.Neutral)
                .Select(hero => hero.heroId)
                .ToList();

            RecordProgressionResults(progressionManager.AddXpToHeroes(allHeroes, amount));
        }

        public void PrintHeroProgressionDebug()
        {
            progressionManager?.PrintProgression();
        }

        public void UnlockAllHeroesDebug()
        {
            progressionManager?.UnlockAllHeroes();
        }

        public void EnsureDirectBattleTestState()
        {
            EnsurePrototypeData();

            if (selectedHeroIds.Count == 3 && activeHeroesRuntime.Count == 3)
            {
                if (currentBattleIndex <= 0)
                {
                    currentBattleIndex = 1;
                }

                InitializeCurrentRunDeckIfNeeded();
                InitializeRelicPool();
                Debug.Log("BattleScene received selected heroes: " + string.Join(", ", selectedHeroIds));
                return;
            }

            ResetRunState();
            SetSelectedHeroes(new List<HeroId>
            {
                HeroId.Capybara,
                HeroId.Sloth,
                HeroId.Panda
            });
            InitializeCurrentRunDeckIfNeeded();
            currentBattleIndex = 1;
            bossBattleStarted = false;
            runWon = false;
            runLost = false;
            selectedMapNodeType = MapNodeType.None;
            supportNodeUsedThisStage = false;
            pendingBetweenBattleRecovery = false;
            InitializeRelicPool();

            Debug.Log("BattleScene direct open fallback party: Capybara, Sloth, Panda");
        }

        public void SetSelectedHeroes(List<HeroId> selectedHeroes)
        {
            EnsurePrototypeData();
            selectedHeroIds = new List<HeroId>(selectedHeroes);
            activeHeroesRuntime = BuildRuntimeHeroes(selectedHeroIds);
            Debug.Log("Selected heroes stored: " + string.Join(", ", selectedHeroIds));
        }

        public void PrepareEncounterDeck()
        {
            EnsurePrototypeData();
            InitializeCurrentRunDeckIfNeeded();
            deckManager.SetStartingDeck(new List<RuntimeCardState>(currentRunDeck));
            Debug.Log("Run deck cards after filtering: " + string.Join(", ", currentRunDeck.Select(card => card.DisplayName)));
        }

        public void InitializeCurrentRunDeckIfNeeded()
        {
            EnsurePrototypeData();

            if (currentRunDeck != null && currentRunDeck.Count > 0)
            {
                return;
            }

            if (selectedHeroIds.Count == 0)
            {
                SetSelectedHeroes(new List<HeroId>
                {
                    HeroId.Capybara,
                    HeroId.Sloth,
                    HeroId.Panda
                });
            }

            currentRunDeck = DeckBuilder.BuildStartingDeck(cardDatabase.cards, selectedHeroIds, progressionManager);
            ApplyStartingDeckProgressionBonuses();
            Debug.Log("Initialized run deck. Count: " + currentRunDeck.Count);
        }

        public void AdvanceToNextEncounter()
        {
            if (runWon)
            {
                return;
            }

            if (!bossBattleStarted)
            {
                if (currentBattleIndex < totalNormalBattlesBeforeBoss)
                {
                    currentBattleIndex++;
                }
                else
                {
                    bossBattleStarted = true;
                    AutoSaveCurrentRun(mapSceneName);
                }
            }
        }

        public List<MapNodeType> GetAvailableMapNodes()
        {
            List<MapNodeType> nodes = new List<MapNodeType>();

            if (runWon)
            {
                return nodes;
            }

            if (currentBattleIndex >= totalNormalBattlesBeforeBoss)
            {
                if (!bossBattleStarted)
                {
                    nodes.Add(MapNodeType.Boss);
                }

                return nodes;
            }

            nodes.Add(MapNodeType.Battle);

            if (!supportNodeUsedThisStage)
            {
                nodes.Add(MapNodeType.Treasure);
                nodes.Add(MapNodeType.Campfire);
                nodes.Add(MapNodeType.Shop);
            }

            return nodes;
        }

        public void SelectMapNode(MapNodeType nodeType)
        {
            selectedMapNodeType = nodeType;
            Debug.Log("Selected node: " + nodeType);
        }

        public void StartNormalBattleFromMap()
        {
            SelectMapNode(MapNodeType.Battle);

            if (pendingBetweenBattleRecovery)
            {
                RecoverHeroesForNextBattle();
                pendingBetweenBattleRecovery = false;
            }

            supportNodeUsedThisStage = false;
            currentBattleIndex = Mathf.Clamp(currentBattleIndex + 1, 1, totalNormalBattlesBeforeBoss);
            bossBattleStarted = false;
            AutoSaveCurrentRun(battleSceneName);
            LoadConfiguredScene(battleSceneName);
        }

        public void StartBossBattleFromMap()
        {
            SelectMapNode(MapNodeType.Boss);

            if (pendingBetweenBattleRecovery)
            {
                RecoverHeroesForNextBattle();
                pendingBetweenBattleRecovery = false;
            }

            supportNodeUsedThisStage = false;
            bossBattleStarted = true;
            AutoSaveCurrentRun(battleSceneName);
            LoadConfiguredScene(battleSceneName);
        }

        public void ResolveTreasureNode(CardData rewardCard)
        {
            SelectMapNode(MapNodeType.Treasure);
            supportNodeUsedThisStage = true;

            if (rewardCard != null)
            {
                AddRewardCard(rewardCard);
            }

            AutoSaveCurrentRun(mapSceneName);
        }

        public bool ResolveTreasureRelicNode(RelicId relicId)
        {
            MarkTreasureNodeResolved();
            return GainRelic(relicId);
        }

        public void ResolveCampfireNode()
        {
            SelectMapNode(MapNodeType.Campfire);
            RecoverHeroesForNextBattle();
            pendingBetweenBattleRecovery = false;
            supportNodeUsedThisStage = true;
            AutoSaveCurrentRun(mapSceneName);
        }

        public void ResolveCampfireUpgradeNode()
        {
            SelectMapNode(MapNodeType.Campfire);
            pendingBetweenBattleRecovery = false;
            supportNodeUsedThisStage = true;
            AutoSaveCurrentRun(mapSceneName);
        }

        public void RecoverHeroesForNextBattle()
        {
            foreach (RuntimeHeroState hero in activeHeroesRuntime)
            {
                if (hero == null || hero.heroData == null)
                {
                    continue;
                }

                hero.block = 0;
                hero.statuses.ClearAll();

                if (hero.IsAlive)
                {
                    int healAmount = Mathf.CeilToInt(hero.MaxHp * 0.3f);
                    hero.Heal(healAmount);
                }
                else
                {
                    hero.currentHp = Mathf.Max(1, Mathf.CeilToInt(hero.MaxHp * 0.25f));
                    hero.isDown = false;
                }
            }
        }

        public void MarkRunWon()
        {
            runWon = true;
            runLost = false;
            AutoSaveCurrentRun(heroSelectionSceneName);
        }

        public void MarkRunLost()
        {
            runLost = true;
            runWon = false;
            AutoSaveCurrentRun(heroSelectionSceneName);
        }

        public string GetRunProgressLabel()
        {
            if (runWon)
            {
                return "Run won!";
            }

            if (bossBattleStarted)
            {
                return "Boss Battle";
            }

            return $"Battle {Mathf.Max(1, currentBattleIndex)}/{totalNormalBattlesBeforeBoss}";
        }

        public void EnsurePrototypeData()
        {
            if (heroDatabase == null)
            {
                heroDatabase = ScriptableObject.CreateInstance<HeroDatabase>();
            }

            if (heroDatabase.heroes == null)
            {
                heroDatabase.heroes = new List<HeroData>();
            }

            if (heroDatabase.heroes.Count == 0)
            {
                heroDatabase.heroes = CreatePrototypeHeroes();
            }

            if (cardDatabase == null)
            {
                cardDatabase = ScriptableObject.CreateInstance<CardDatabase>();
            }

            if (cardDatabase.cards == null)
            {
                cardDatabase.cards = new List<CardData>();
            }

            if (cardDatabase.cards.Count == 0 || cardDatabase.cards.Count < 20)
            {
                cardDatabase.cards = CreatePrototypeCards();
            }

            NormalizePrototypeCardFlags();

            if (relicDatabase == null)
            {
                relicDatabase = new List<RelicData>();
            }

            if (relicDatabase.Count == 0)
            {
                relicDatabase = CreatePrototypeRelics();
            }

            if (progressionManager != null)
            {
                progressionManager.Initialize(heroDatabase, cardDatabase);
            }
        }

        private void ResetRunState()
        {
            selectedHeroIds = new List<HeroId>();
            activeHeroesRuntime = new List<RuntimeHeroState>();
            currentRunDeck = new List<RuntimeCardState>();
            currentBattleIndex = 0;
            bossBattleStarted = false;
            runWon = false;
            runLost = false;
            selectedMapNodeType = MapNodeType.None;
            supportNodeUsedThisStage = false;
            pendingBetweenBattleRecovery = false;
            ownedRelics = new List<RelicId>();
            availableRelicsPool = new List<RelicId>();
            gold = 100;
            runProgressSummaries.Clear();

            if (deckManager != null)
            {
                deckManager.SetStartingDeck(new List<RuntimeCardState>());
            }
        }

        private void InitializeRelicPool()
        {
            EnsurePrototypeData();

            if (availableRelicsPool == null)
            {
                availableRelicsPool = new List<RelicId>();
            }

            if (availableRelicsPool.Count > 0 || relicDatabase.Count == 0)
            {
                return;
            }

            availableRelicsPool = relicDatabase
                .Where(relic => relic != null && relic.relicId != RelicId.None && !ownedRelics.Contains(relic.relicId))
                .Select(relic => relic.relicId)
                .ToList();
        }

        private void ApplyIronSnack()
        {
            foreach (RuntimeHeroState hero in activeHeroesRuntime)
            {
                if (hero == null)
                {
                    continue;
                }

                hero.GainMaxHp(5);
            }

            Debug.Log("Iron Snack: all active heroes gained +5 max HP and +5 current HP.");
        }

        private List<RuntimeHeroState> BuildRuntimeHeroes(IEnumerable<HeroId> heroIds)
        {
            List<RuntimeHeroState> heroes = new List<RuntimeHeroState>();
            foreach (HeroId heroId in heroIds)
            {
                HeroData heroData = heroDatabase.GetHero(heroId);
                if (heroData == null)
                {
                    continue;
                }

                RuntimeHeroState runtimeHero = new RuntimeHeroState
                {
                    heroData = heroData
                };
                runtimeHero.ResetForBattle();
                runtimeHero.bonusMaxHp = progressionManager != null ? progressionManager.GetPermanentMaxHpBonus(heroId) : 0;
                runtimeHero.currentHp = runtimeHero.MaxHp;
                heroes.Add(runtimeHero);
            }

            return heroes;
        }

        private List<HeroData> CreatePrototypeHeroes()
        {
            return new List<HeroData>
            {
                CreateHero(HeroId.Capybara, "Capybara", HeroClass.Swordsman, 42, "Balanced attacker with guard skills."),
                CreateHero(HeroId.Koala, "Koala", HeroClass.Thief, 34, "Fast strikes, weak effects, card draw."),
                CreateHero(HeroId.Sloth, "Sloth", HeroClass.Healer, 36, "Healing and support."),
                CreateHero(HeroId.Panda, "Panda", HeroClass.Tank, 50, "Block, taunt, protection."),
                CreateHero(HeroId.Kangaroo, "Kangaroo", HeroClass.Fighter, 40, "Combo damage and strength.")
            };
        }

        private List<CardData> CreatePrototypeCards()
        {
            return new List<CardData>
            {
                CreateCard("swift_slash", "Swift Slash", "Deal 8 damage.", HeroId.Capybara, CardType.Attack, TargetType.Enemy, 1, damage: 8, upgradedDamage: 11),
                CreateCard("guard_stance", "Guard Stance", "Gain 8 block.", HeroId.Capybara, CardType.Skill, TargetType.Self, 1, block: 8, upgradedBlock: 12),
                CreateCard("pommel_tap", "Pommel Tap", "Deal 5 damage. Apply 1 Stun.", HeroId.Capybara, CardType.Attack, TargetType.Enemy, 1, damage: 5, stunAmount: 1, upgradedDamage: 7, upgradedStunAmount: 1),
                CreateCard("rally_cut", "Rally Cut", "Deal 10 damage. Gain 1 Strength.", HeroId.Capybara, CardType.Attack, TargetType.Enemy, 2, damage: 10, upgradedDamage: 14, isStarterCard: false),
                CreateCard("shadow_strike", "Shadow Strike", "Deal 7 damage. Apply 1 Weak.", HeroId.Koala, CardType.Attack, TargetType.Enemy, 1, damage: 7, weakAmount: 1, upgradedDamage: 10, upgradedWeakAmount: 2),
                CreateCard("smoke_step", "Smoke Step", "Gain 6 block and draw 1 card.", HeroId.Koala, CardType.Skill, TargetType.Self, 1, block: 6, drawAmount: 1, upgradedBlock: 9, upgradedDrawAmount: 1),
                CreateCard("muzzle_trick", "Muzzle Trick", "Apply 1 Silence. Draw 1 card.", HeroId.Koala, CardType.Skill, TargetType.Enemy, 1, drawAmount: 1, silenceAmount: 1, upgradedDrawAmount: 1, upgradedSilenceAmount: 2),
                CreateCard("silent_pounce", "Silent Pounce", "Deal 6 damage. Apply 1 Silence.", HeroId.Koala, CardType.Attack, TargetType.Enemy, 1, damage: 6, silenceAmount: 1, upgradedDamage: 8, upgradedSilenceAmount: 2, isStarterCard: false),
                CreateCard("staff_tap", "Staff Tap", "Deal 5 damage.", HeroId.Sloth, CardType.Attack, TargetType.Enemy, 1, damage: 5, upgradedDamage: 8),
                CreateCard("soothing_light", "Soothing Light", "Heal 8 HP.", HeroId.Sloth, CardType.Skill, TargetType.Ally, 1, heal: 8, upgradedHeal: 12),
                CreateCard("quiet_blessing", "Quiet Blessing", "Heal 6 HP.", HeroId.Sloth, CardType.Skill, TargetType.Ally, 1, heal: 6, upgradedHeal: 9),
                CreateCard("deep_rest", "Deep Rest", "Heal all allies for 6 HP.", HeroId.Sloth, CardType.Skill, TargetType.AllAllies, 2, heal: 6, upgradedHeal: 9, isStarterCard: false),
                CreateCard("shield_bash", "Shield Bash", "Deal 6 damage and gain 4 block.", HeroId.Panda, CardType.Attack, TargetType.Enemy, 1, damage: 6, block: 4, upgradedDamage: 9, upgradedBlock: 7),
                CreateCard("barkskin_guard", "Barkskin Guard", "Gain 16 block. Gain 1 Taunt.", HeroId.Panda, CardType.Skill, TargetType.Self, 2, block: 16, tauntAmount: 1, upgradedBlock: 22, upgradedTauntAmount: 2),
                CreateCard("guardian_roar", "Guardian Roar", "All allies gain 8 block. Panda gains 1 Taunt.", HeroId.Panda, CardType.Skill, TargetType.AllAllies, 2, block: 8, upgradedBlock: 12, isStarterCard: false),
                CreateCard("power_combo", "Power Combo", "Deal 12 damage.", HeroId.Kangaroo, CardType.Attack, TargetType.Enemy, 2, damage: 12, upgradedDamage: 16),
                CreateCard("battle_focus", "Battle Focus", "Gain 2 Strength.", HeroId.Kangaroo, CardType.Skill, TargetType.Self, 1, strengthAmount: 2, upgradedStrengthAmount: 3),
                CreateCard("momentum_kick", "Momentum Kick", "Deal 5 damage. If this is the second Attack this turn, deal 5 extra damage.", HeroId.Kangaroo, CardType.Attack, TargetType.Enemy, 1, damage: 5, upgradedDamage: 7, isStarterCard: false),
                CreateCard("snack_time", "Snack Time", "Draw 1 card.", HeroId.Neutral, CardType.Skill, TargetType.None, 1, drawAmount: 1, upgradedDrawAmount: 2),
                CreateCard("quick_guard", "Quick Guard", "Gain 5 block.", HeroId.Neutral, CardType.Skill, TargetType.Self, 1, block: 5, upgradedBlock: 8)
            };
        }

        private List<RelicData> CreatePrototypeRelics()
        {
            return new List<RelicData>
            {
                CreateRelic(RelicId.BambooCharm, "Bamboo Charm", "First Attack card played each battle deals +3 damage.", "Common"),
                CreateRelic(RelicId.CozyLeaf, "Cozy Leaf", "At battle start, all active heroes gain 5 block.", "Common"),
                CreateRelic(RelicId.LuckyPaw, "Lucky Paw", "Reward card screen shows 4 choices instead of 3.", "Uncommon"),
                CreateRelic(RelicId.IronSnack, "Iron Snack", "All current active heroes gain +5 max HP and +5 current HP when gained.", "Uncommon"),
                CreateRelic(RelicId.SlothTea, "Sloth Tea", "First healing card played each battle heals +5 extra.", "Common"),
                CreateRelic(RelicId.PandaEmblem, "Panda Emblem", "If Panda is active, Panda starts each battle with 10 extra block.", "Uncommon"),
                CreateRelic(RelicId.ThiefsBell, "Thief's Bell", "If Koala is active, the first Koala card played each turn costs 0.", "Rare"),
                CreateRelic(RelicId.KangarooWraps, "Kangaroo Wraps", "After playing 2 Attack cards in the same turn, deal 4 bonus damage to a random living enemy.", "Rare")
            };
        }

        private HeroData CreateHero(HeroId heroId, string heroName, HeroClass heroClass, int maxHp, string description)
        {
            HeroData hero = ScriptableObject.CreateInstance<HeroData>();
            hero.heroId = heroId;
            hero.heroName = heroName;
            hero.heroClass = heroClass;
            hero.maxHp = maxHp;
            hero.description = description;
            return hero;
        }

        private RelicData CreateRelic(RelicId relicId, string relicName, string description, string rarity)
        {
            RelicData relic = ScriptableObject.CreateInstance<RelicData>();
            relic.relicId = relicId;
            relic.relicName = relicName;
            relic.description = description;
            relic.rarity = rarity;
            return relic;
        }

        private CardData CreateCard(
            string cardId,
            string cardName,
            string description,
            HeroId ownerHeroId,
            CardType cardType,
            TargetType targetType,
            int cost,
            int damage = 0,
            int block = 0,
            int heal = 0,
            int drawAmount = 0,
            int strengthAmount = 0,
            int weakAmount = 0,
            int vulnerableAmount = 0,
            int bleedAmount = 0,
            int poisonAmount = 0,
            int tauntAmount = 0,
            int stunAmount = 0,
            int silenceAmount = 0,
            int upgradedDamage = 0,
            int upgradedBlock = 0,
            int upgradedHeal = 0,
            int upgradedDrawAmount = 0,
            int upgradedStrengthAmount = 0,
            int upgradedWeakAmount = 0,
            int upgradedVulnerableAmount = 0,
            int upgradedBleedAmount = 0,
            int upgradedPoisonAmount = 0,
            int upgradedTauntAmount = 0,
            int upgradedStunAmount = 0,
            int upgradedSilenceAmount = 0,
            bool isStarterCard = true)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.cardId = cardId;
            card.cardName = cardName;
            card.description = description;
            card.isStarterCard = isStarterCard;
            card.ownerHeroId = ownerHeroId;
            card.cardType = cardType;
            card.targetType = targetType;
            card.cost = cost;
            card.damage = damage;
            card.block = block;
            card.heal = heal;
            card.drawAmount = drawAmount;
            card.strengthAmount = strengthAmount;
            card.weakAmount = weakAmount;
            card.vulnerableAmount = vulnerableAmount;
            card.bleedAmount = bleedAmount;
            card.poisonAmount = poisonAmount;
            card.tauntAmount = tauntAmount;
            card.stunAmount = stunAmount;
            card.silenceAmount = silenceAmount;
            card.upgradedDamage = upgradedDamage;
            card.upgradedBlock = upgradedBlock;
            card.upgradedHeal = upgradedHeal;
            card.upgradedDrawAmount = upgradedDrawAmount;
            card.upgradedStrengthAmount = upgradedStrengthAmount;
            card.upgradedWeakAmount = upgradedWeakAmount;
            card.upgradedVulnerableAmount = upgradedVulnerableAmount;
            card.upgradedBleedAmount = upgradedBleedAmount;
            card.upgradedPoisonAmount = upgradedPoisonAmount;
            card.upgradedTauntAmount = upgradedTauntAmount;
            card.upgradedStunAmount = upgradedStunAmount;
            card.upgradedSilenceAmount = upgradedSilenceAmount;
            return card;
        }

        private void LoadConfiguredScene(string sceneName)
        {
#if UNITY_EDITOR
            string editorScenePath = $"Assets/PawSlayers/Scenes/{sceneName}.unity";
            if (System.IO.File.Exists(editorScenePath))
            {
                EditorSceneManager.LoadSceneInPlayMode(editorScenePath, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(sceneName);
        }

        public void LoadSceneByName(string sceneName)
        {
            LoadConfiguredScene(sceneName);
        }

        public bool HasSavedRun()
        {
            return runSaveManager != null && runSaveManager.HasSavedRun();
        }

        public bool ContinueSavedRun()
        {
            return runSaveManager != null && runSaveManager.ContinueSavedRun();
        }

        public void DeleteCurrentRunSave()
        {
            runSaveManager?.DeleteCurrentRunSave();
        }

        public void AutoSaveCurrentRun(string sceneNameOverride = null)
        {
            runSaveManager?.SaveCurrentRun(sceneNameOverride);
        }

        private void ApplyStartingDeckProgressionBonuses()
        {
            if (progressionManager == null || currentRunDeck == null)
            {
                return;
            }

            foreach (HeroId heroId in selectedHeroIds)
            {
                if (!progressionManager.HasLevelFourStarterUpgrade(heroId))
                {
                    continue;
                }

                RuntimeCardState cardToUpgrade = currentRunDeck
                    .Where(card => card != null && card.OwnerHeroId == heroId && card.baseCard != null && (card.baseCard.isStarterCard || PrototypeStarterCardIds.Contains(card.baseCard.cardId)) && !card.isUpgraded && card.CanUpgrade)
                    .OrderBy(_ => UnityEngine.Random.value)
                    .FirstOrDefault();

                if (cardToUpgrade == null)
                {
                    continue;
                }

                cardToUpgrade.Upgrade();
                Debug.Log($"{heroId} Level 4 bonus: {cardToUpgrade.DisplayName} starts upgraded.");
            }
        }

        private void NormalizePrototypeCardFlags()
        {
            if (cardDatabase == null || cardDatabase.cards == null)
            {
                return;
            }

            foreach (CardData card in cardDatabase.cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.cardId))
                {
                    continue;
                }

                if (PrototypeStarterCardIds.Contains(card.cardId))
                {
                    card.isStarterCard = true;
                }
            }
        }

        private void GrantXpToSelectedHeroes(int amount)
        {
            if (progressionManager == null || amount <= 0 || selectedHeroIds.Count == 0)
            {
                return;
            }

            RecordProgressionResults(progressionManager.AddXpToHeroes(selectedHeroIds, amount));
        }

        private void RecordProgressionResults(List<HeroXpGainResult> results)
        {
            foreach (HeroXpGainResult result in results)
            {
                if (!runProgressSummaries.TryGetValue(result.heroId, out RunHeroProgressSummary summary))
                {
                    summary = new RunHeroProgressSummary
                    {
                        heroId = result.heroId,
                        heroName = result.heroName,
                        startLevel = result.oldLevel,
                        endLevel = result.newLevel
                    };
                    runProgressSummaries[result.heroId] = summary;
                }

                summary.totalXpGained += result.xpGained;
                summary.endLevel = Mathf.Max(summary.endLevel, result.newLevel);

                foreach (string unlockedCardId in result.unlockedCardIds)
                {
                    string unlockedCardName = progressionManager.GetCardName(unlockedCardId);
                    string message = $"{summary.heroName} unlocked {unlockedCardName}!";
                    if (!summary.unlockedMessages.Contains(message))
                    {
                        summary.unlockedMessages.Add(message);
                    }
                }

                foreach (string unlockedPerkId in result.unlockedPerkIds)
                {
                    string message = $"{summary.heroName} unlocked a perk!";
                    if (!summary.unlockedMessages.Contains(message))
                    {
                        summary.unlockedMessages.Add(message);
                    }
                }
            }
        }

        public RunSaveData BuildRunSaveData(string sceneNameOverride = null)
        {
            EnsurePrototypeData();
            RunSaveData saveData = new RunSaveData
            {
                hasActiveRun = selectedHeroIds.Count > 0 && !runWon && !runLost,
                runWon = runWon,
                runLost = runLost,
                selectedHeroIds = new List<HeroId>(selectedHeroIds),
                ownedRelics = new List<RelicId>(ownedRelics),
                gold = gold,
                currentBattleIndex = currentBattleIndex,
                totalNormalBattlesBeforeBoss = totalNormalBattlesBeforeBoss,
                bossAvailable = currentBattleIndex >= totalNormalBattlesBeforeBoss,
                bossStarted = bossBattleStarted,
                supportNodeUsedThisStage = supportNodeUsedThisStage,
                pendingBetweenBattleRecovery = pendingBetweenBattleRecovery,
                currentSceneName = string.IsNullOrWhiteSpace(sceneNameOverride) ? mapSceneName : sceneNameOverride,
                currentNodeType = selectedMapNodeType.ToString(),
                mapStep = currentBattleIndex,
                savedAtUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            foreach (RuntimeHeroState hero in activeHeroesRuntime)
            {
                if (hero == null || hero.heroData == null)
                {
                    continue;
                }

                saveData.heroes.Add(new HeroRuntimeSaveData
                {
                    heroId = hero.heroData.heroId,
                    currentHp = hero.currentHp,
                    maxHp = hero.MaxHp,
                    block = hero.block,
                    isDown = hero.isDown,
                    statuses = BuildStatusSaveData(hero.statuses)
                });
            }

            foreach (RuntimeCardState card in currentRunDeck.Where(card => card != null && card.baseCard != null))
            {
                saveData.currentRunDeck.Add(new CardInstanceSaveData
                {
                    cardId = card.baseCard.cardId,
                    ownerHeroId = card.OwnerHeroId,
                    isUpgraded = card.isUpgraded,
                    isTemporary = false
                });
            }

            Debug.Log("Hero HP saved: " + string.Join(", ", saveData.heroes.Select(hero => $"{hero.heroId}:{hero.currentHp}/{hero.maxHp}")));
            return saveData;
        }

        public void ApplyRunSaveData(RunSaveData data)
        {
            if (data == null)
            {
                return;
            }

            EnsurePrototypeData();
            ResetRunState();

            selectedHeroIds = new List<HeroId>(data.selectedHeroIds ?? new List<HeroId>());
            activeHeroesRuntime = BuildRuntimeHeroes(selectedHeroIds);
            currentBattleIndex = data.currentBattleIndex;
            totalNormalBattlesBeforeBoss = data.totalNormalBattlesBeforeBoss > 0 ? data.totalNormalBattlesBeforeBoss : 3;
            bossBattleStarted = data.bossStarted;
            runWon = data.runWon;
            runLost = data.runLost;
            supportNodeUsedThisStage = data.supportNodeUsedThisStage;
            pendingBetweenBattleRecovery = data.pendingBetweenBattleRecovery;
            gold = data.gold;
            ownedRelics = new List<RelicId>(data.ownedRelics ?? new List<RelicId>());

            if (!string.IsNullOrWhiteSpace(data.currentNodeType) && Enum.TryParse(data.currentNodeType, out MapNodeType parsedNode))
            {
                selectedMapNodeType = parsedNode;
            }

            RestoreSavedHeroes(data);
            RestoreSavedDeck(data);
            InitializeRelicPool();
            PrepareEncounterDeck();

            Debug.Log("Current run deck count loaded: " + currentRunDeck.Count);
            Debug.Log("Relic count loaded: " + ownedRelics.Count);
            Debug.Log("Gold loaded: " + gold);
            Debug.Log("Selected heroes loaded: " + string.Join(", ", selectedHeroIds));
            Debug.Log("Hero HP loaded: " + string.Join(", ", activeHeroesRuntime.Where(hero => hero != null && hero.heroData != null).Select(hero => $"{hero.heroData.heroId}:{hero.currentHp}/{hero.MaxHp}")));
        }

        private void RestoreSavedHeroes(RunSaveData data)
        {
            foreach (HeroRuntimeSaveData savedHero in data.heroes)
            {
                RuntimeHeroState runtimeHero = GetHeroState(savedHero.heroId);
                if (runtimeHero == null || runtimeHero.heroData == null)
                {
                    continue;
                }

                runtimeHero.bonusMaxHp = Mathf.Max(0, savedHero.maxHp - runtimeHero.heroData.maxHp);
                runtimeHero.currentHp = Mathf.Clamp(savedHero.currentHp, 0, runtimeHero.MaxHp);
                runtimeHero.block = savedHero.block;
                runtimeHero.isDown = savedHero.isDown || runtimeHero.currentHp <= 0;
                ApplyStatusSaveData(runtimeHero.statuses, savedHero.statuses);
            }
        }

        private void RestoreSavedDeck(RunSaveData data)
        {
            currentRunDeck = new List<RuntimeCardState>();
            foreach (CardInstanceSaveData savedCard in data.currentRunDeck)
            {
                if (savedCard == null || savedCard.isTemporary || string.IsNullOrWhiteSpace(savedCard.cardId))
                {
                    continue;
                }

                CardData cardDefinition = cardDatabase.cards.FirstOrDefault(card => card != null && card.cardId == savedCard.cardId);
                if (cardDefinition == null)
                {
                    Debug.LogWarning("Missing card definition: " + savedCard.cardId);
                    continue;
                }

                RuntimeCardState runtimeCard = RuntimeCardState.Create(cardDefinition);
                runtimeCard.isUpgraded = savedCard.isUpgraded;
                currentRunDeck.Add(runtimeCard);
            }
        }

        private StatusEffectsSaveData BuildStatusSaveData(StatusEffectState statuses)
        {
            if (statuses == null)
            {
                return new StatusEffectsSaveData();
            }

            return new StatusEffectsSaveData
            {
                weak = statuses.weak,
                vulnerable = statuses.vulnerable,
                strength = statuses.strength,
                bleed = statuses.bleed,
                poison = statuses.poison,
                taunt = statuses.taunt,
                stun = statuses.stun,
                silence = statuses.silence
            };
        }

        private void ApplyStatusSaveData(StatusEffectState target, StatusEffectsSaveData source)
        {
            if (target == null || source == null)
            {
                return;
            }

            target.weak = source.weak;
            target.vulnerable = source.vulnerable;
            target.strength = source.strength;
            target.bleed = source.bleed;
            target.poison = source.poison;
            target.taunt = source.taunt;
            target.stun = source.stun;
            target.silence = source.silence;
        }
    }
}
