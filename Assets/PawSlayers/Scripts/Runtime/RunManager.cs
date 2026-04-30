using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace PawSlayers
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("Data")]
        public HeroDatabase heroDatabase;
        public CardDatabase cardDatabase;
        public List<RelicData> relicDatabase = new List<RelicData>();

        [Header("Scenes")]
        public string heroSelectionSceneName = "HeroSelection";
        public string battleSceneName = "Battle";
        public string mapSceneName = "Map";

        [Header("Run State")]
        [SerializeField] private List<HeroId> selectedHeroIds = new List<HeroId>();
        [SerializeField] private List<RuntimeHeroState> activeHeroesRuntime = new List<RuntimeHeroState>();
        [SerializeField] private List<RuntimeCardState> currentRunDeck = new List<RuntimeCardState>();
        [SerializeField] private int currentBattleIndex;
        [SerializeField] private int totalNormalBattlesBeforeBoss = 3;
        [SerializeField] private bool bossBattleStarted;
        [SerializeField] private bool runWon;
        [SerializeField] private MapNodeType selectedMapNodeType = MapNodeType.None;
        [SerializeField] private bool supportNodeUsedThisStage;
        [SerializeField] private bool pendingBetweenBattleRecovery;
        [SerializeField] private List<RelicId> ownedRelics = new List<RelicId>();
        [SerializeField] private List<RelicId> availableRelicsPool = new List<RelicId>();
        [SerializeField] private int gold;

        private DeckManager deckManager;

        public List<HeroId> SelectedHeroIds => selectedHeroIds;
        public List<RuntimeHeroState> ActiveHeroesRuntime => activeHeroesRuntime;
        public List<RuntimeCardState> CurrentRunDeck => currentRunDeck;
        public int CurrentBattleIndex => currentBattleIndex;
        public int TotalNormalBattlesBeforeBoss => totalNormalBattlesBeforeBoss;
        public bool BossBattleStarted => bossBattleStarted;
        public bool RunWon => runWon;
        public bool IsBossBattle => bossBattleStarted;
        public MapNodeType SelectedMapNodeType => selectedMapNodeType;
        public bool SupportNodeUsedThisStage => supportNodeUsedThisStage;
        public bool PendingBetweenBattleRecovery => pendingBetweenBattleRecovery;
        public List<RelicId> OwnedRelics => ownedRelics;
        public int Gold => gold;
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

            EnsurePrototypeData();
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
            selectedMapNodeType = MapNodeType.None;
            supportNodeUsedThisStage = false;
            pendingBetweenBattleRecovery = false;
            InitializeRelicPool();
            PrepareEncounterDeck();

            Debug.Log("Loading BattleScene with selected heroes: " + string.Join(", ", selectedHeroIds));
            LoadConfiguredScene(battleSceneName);
        }

        public void ResetRunAndReturnToSelection()
        {
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
                .OrderBy(_ => Random.value)
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

            return true;
        }

        public void AddGold(int amount)
        {
            gold += Mathf.Max(0, amount);
            Debug.Log("Current gold: " + gold);
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
            return true;
        }

        public void MarkTreasureNodeResolved()
        {
            SelectMapNode(MapNodeType.Treasure);
            supportNodeUsedThisStage = true;
        }

        public void MarkShopNodeResolved()
        {
            SelectMapNode(MapNodeType.Shop);
            supportNodeUsedThisStage = true;
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

            currentRunDeck = DeckBuilder.BuildStartingDeck(cardDatabase.cards, selectedHeroIds);
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
        }

        public void ResolveCampfireUpgradeNode()
        {
            SelectMapNode(MapNodeType.Campfire);
            pendingBetweenBattleRecovery = false;
            supportNodeUsedThisStage = true;
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

            if (cardDatabase.cards.Count == 0 || cardDatabase.cards.Count < 15)
            {
                cardDatabase.cards = CreatePrototypeCards();
            }

            if (relicDatabase == null)
            {
                relicDatabase = new List<RelicData>();
            }

            if (relicDatabase.Count == 0)
            {
                relicDatabase = CreatePrototypeRelics();
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
            selectedMapNodeType = MapNodeType.None;
            supportNodeUsedThisStage = false;
            pendingBetweenBattleRecovery = false;
            ownedRelics = new List<RelicId>();
            availableRelicsPool = new List<RelicId>();
            gold = 100;

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
                CreateCard("shadow_strike", "Shadow Strike", "Deal 7 damage. Apply 1 Weak.", HeroId.Koala, CardType.Attack, TargetType.Enemy, 1, damage: 7, weakAmount: 1, upgradedDamage: 10, upgradedWeakAmount: 2),
                CreateCard("smoke_step", "Smoke Step", "Gain 6 block and draw 1 card.", HeroId.Koala, CardType.Skill, TargetType.Self, 1, block: 6, drawAmount: 1, upgradedBlock: 9, upgradedDrawAmount: 1),
                CreateCard("muzzle_trick", "Muzzle Trick", "Apply 1 Silence. Draw 1 card.", HeroId.Koala, CardType.Skill, TargetType.Enemy, 1, drawAmount: 1, silenceAmount: 1, upgradedDrawAmount: 1, upgradedSilenceAmount: 2),
                CreateCard("staff_tap", "Staff Tap", "Deal 5 damage.", HeroId.Sloth, CardType.Attack, TargetType.Enemy, 1, damage: 5, upgradedDamage: 8),
                CreateCard("soothing_light", "Soothing Light", "Heal 8 HP.", HeroId.Sloth, CardType.Skill, TargetType.Ally, 1, heal: 8, upgradedHeal: 12),
                CreateCard("quiet_blessing", "Quiet Blessing", "Heal 6 HP.", HeroId.Sloth, CardType.Skill, TargetType.Ally, 1, heal: 6, upgradedHeal: 9),
                CreateCard("shield_bash", "Shield Bash", "Deal 6 damage and gain 4 block.", HeroId.Panda, CardType.Attack, TargetType.Enemy, 1, damage: 6, block: 4, upgradedDamage: 9, upgradedBlock: 7),
                CreateCard("barkskin_guard", "Barkskin Guard", "Gain 16 block. Gain 1 Taunt.", HeroId.Panda, CardType.Skill, TargetType.Self, 2, block: 16, tauntAmount: 1, upgradedBlock: 22, upgradedTauntAmount: 2),
                CreateCard("power_combo", "Power Combo", "Deal 12 damage.", HeroId.Kangaroo, CardType.Attack, TargetType.Enemy, 2, damage: 12, upgradedDamage: 16),
                CreateCard("battle_focus", "Battle Focus", "Gain 2 Strength.", HeroId.Kangaroo, CardType.Skill, TargetType.Self, 1, strengthAmount: 2, upgradedStrengthAmount: 3),
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
            int upgradedSilenceAmount = 0)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.cardId = cardId;
            card.cardName = cardName;
            card.description = description;
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
    }
}
