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
        public EnemyArtDatabase enemyArtDatabase;
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
        public Text runProgressText;
        public Text goldText;
        public Text relicsText;
        public Text drawPileText;
        public Text discardPileText;
        public Text battleLogText;
        public Text debugToggleText;
        public Button drawButton;
        public Button endTurnButton;
        public Button killHero1Button;
        public Button killHero2Button;
        public Button killHero3Button;
        public Button healAllButton;
        public Button winBattleButton;
        public GameObject runWonPanel;
        public Text runWonText;
        public Button returnToSelectionButton;
        public Button debugToggleButton;
        public GameObject debugPanelRoot;
        public Transform debugButtonsContainer;
        public Text combatLogTitleText;

        private readonly List<BattleHeroView> heroViews = new List<BattleHeroView>();
        private readonly List<EnemyView> enemyViews = new List<EnemyView>();
        private readonly List<CardView> handViews = new List<CardView>();
        private readonly List<EnemyRuntimeState> enemies = new List<EnemyRuntimeState>();

        private const int MaxEnergy = 3;
        private const string WoundCardId = "status_wound";
        private const int BriarKingPhaseTwoThresholdHp = 110;

        private int currentEnergy;
        private bool battleEnded;
        private RuntimeCardState selectedCard;
        private bool enemyTurnActive;
        private bool bambooCharmUsedThisBattle;
        private bool slothTeaUsedThisBattle;
        private bool thiefsBellUsedThisTurn;
        private int kangarooWrapsAttackCardsThisTurn;
        private bool capybaraLevelFiveUsedThisBattle;
        private bool koalaLevelFiveUsedThisBattle;
        private bool slothLevelFiveUsedThisBattle;
        private int kangarooLevelFiveAttackCardsThisTurn;

        private void Start()
        {
            EnsureRunManager();
            EnsureRuntimeUi();
            BindButtons();
            InitializeEncounterFlow();
        }

        private void OnEnable()
        {
            BindButtons();
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
            TickHeroEndOfTurnStatuses();
            CheckBattleState();

            if (battleEnded)
            {
                RefreshAllUi();
                return;
            }

            enemyTurnActive = true;
            RefreshTopBar();
            RunEnemyTurn();
            enemyTurnActive = false;
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
            enemyTurnActive = false;
            CleanupTemporaryBattleCards();
            AddLog("Battle won.");

            if (runManager.IsBossBattle)
            {
                runManager.GrantBossBattleXp();
                runManager.MarkRunWon();
                ShowRunWon();
                return;
            }

            runManager.GrantNormalBattleXp();
            RefreshAllUi();

            if (rewardCardManager != null)
            {
                rewardCardManager.ShowRewards();
            }

            if (endTurnButton != null)
            {
                endTurnButton.interactable = false;
            }
        }

        public void ContinueToNextBattle()
        {
            if (runManager == null || runManager.RunWon)
            {
                return;
            }

            runManager.RecoverHeroesForNextBattle();
            runManager.AdvanceToNextEncounter();
            StartEncounter();
        }

        public void ForceBossBattleDebug()
        {
            if (runManager == null)
            {
                return;
            }

            AddLog("Debug: loading boss battle.");
            runManager.StartBossBattleFromMap();
        }

        public void DamageBossHalfDebug()
        {
            EnemyRuntimeState boss = GetBossEnemy();
            if (boss == null || !boss.IsAlive)
            {
                AddLog("Debug: no living boss.");
                return;
            }

            int targetHp = Mathf.Max(1, BriarKingPhaseTwoThresholdHp);
            boss.currentHp = Mathf.Min(boss.currentHp, targetHp);
            UpdateBossPhaseIfNeeded(boss);
            AddLog("Debug: boss reduced to phase threshold.");
            RefreshAllUi();
        }

        public void KillBossDebug()
        {
            EnemyRuntimeState boss = GetBossEnemy();
            if (boss == null || !boss.IsAlive)
            {
                AddLog("Debug: no living boss.");
                return;
            }

            boss.currentHp = 0;
            boss.block = 0;
            AddLog("Debug: boss defeated.");
            RefreshAllUi();
            CheckBattleState();
        }

        public void AddWeakToEnemy1Debug() => ApplyStatusToEnemyDebug(0, enemy => enemy.AddWeak(1), "Enemy 1 gained 1 Weak.");
        public void AddVulnerableToEnemy1Debug() => ApplyStatusToEnemyDebug(0, enemy => enemy.AddVulnerable(1), "Enemy 1 gained 1 Vulnerable.");
        public void AddPoisonToEnemy1Debug() => ApplyStatusToEnemyDebug(0, enemy => enemy.AddPoison(3), "Enemy 1 gained 3 Poison.");
        public void StunEnemy1Debug() => ApplyStatusToEnemyDebug(0, enemy => enemy.AddStun(1), "Enemy 1 gained 1 Stun.");
        public void SilenceEnemy1Debug() => ApplyStatusToEnemyDebug(0, enemy => enemy.AddSilence(1), "Enemy 1 gained 1 Silence.");
        public void AddStrengthToHero1Debug() => ApplyStatusToHeroDebug(0, hero => hero.AddStrength(2), "Hero 1 gained 2 Strength.");
        public void AddBleedToHero1Debug() => ApplyStatusToHeroDebug(0, hero => hero.AddBleed(2), "Hero 1 gained 2 Bleed.");
        public void StunHero1Debug() => ApplyStatusToHeroDebug(0, hero => hero.AddStun(1), "Hero 1 gained 1 Stun.");
        public void SilenceHero1Debug() => ApplyStatusToHeroDebug(0, hero => hero.AddSilence(1), "Hero 1 gained 1 Silence.");

        public void ClearAllStatusesDebug()
        {
            foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime)
            {
                hero?.statuses.ClearAll();
            }

            foreach (EnemyRuntimeState enemy in enemies)
            {
                enemy?.statuses.ClearAll();
            }

            AddLog("All statuses cleared.");
            RefreshAllUi();
        }

        private void InitializeEncounterFlow()
        {
            runManager.EnsureDirectBattleTestState();
            Debug.Log("BattleScene received selected heroes: " + string.Join(", ", runManager.SelectedHeroIds));
            StartEncounter();
        }

        private void StartEncounter()
        {
            battleEnded = false;
            enemyTurnActive = false;
            ClearCardSelection();
            ResetRelicStateForBattle();

            if (rewardCardManager != null && rewardCardManager.rewardPanel != null)
            {
                rewardCardManager.rewardPanel.SetActive(false);
            }

            CreateEncounterForCurrentRun();
            Debug.Log($"Selected heroes: {runManager.SelectedHeroIds.Count}");
            Debug.Log($"Enemies spawned: {enemies.Count}");

            runManager.PrepareEncounterDeck();
            Debug.Log($"Run deck cards: {runManager.CurrentRunDeck.Count}");
            Debug.Log($"Draw pile count: {runManager.DrawPile.Count}");

            BuildHeroViews();
            BuildEnemyViews();

            if (runWonPanel != null)
            {
                runWonPanel.SetActive(false);
            }

            AddLog(GetEncounterStartLog());
            StartPlayerTurn(true);
            Debug.Log($"Starting hand: {runManager.Hand.Count}");
        }

        private string GetEncounterStartLog()
        {
            if (runManager.IsBossBattle)
            {
                return "Boss Battle: Briar King";
            }

            return $"Battle {runManager.CurrentBattleIndex} started.";
        }

        private void StartPlayerTurn(bool isBattleStart = false)
        {
            enemyTurnActive = false;

            if (!isBattleStart)
            {
                TickHeroStartOfTurnStatuses();
                CheckBattleState();
                if (battleEnded)
                {
                    RefreshAllUi();
                    return;
                }
            }

            ResetHeroBlock();
            currentEnergy = MaxEnergy;
            thiefsBellUsedThisTurn = false;
            kangarooWrapsAttackCardsThisTurn = 0;
            kangarooLevelFiveAttackCardsThisTurn = 0;

            if (isBattleStart)
            {
                ApplyBattleStartRelics();
            }

            ClearCardSelection();
            RefreshEnemyIntents();
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

        private void ResetRelicStateForBattle()
        {
            bambooCharmUsedThisBattle = false;
            slothTeaUsedThisBattle = false;
            thiefsBellUsedThisTurn = false;
            kangarooWrapsAttackCardsThisTurn = 0;
            capybaraLevelFiveUsedThisBattle = false;
            koalaLevelFiveUsedThisBattle = false;
            slothLevelFiveUsedThisBattle = false;
            kangarooLevelFiveAttackCardsThisTurn = 0;
        }

        private void ApplyBattleStartRelics()
        {
            if (runManager.HasRelic(RelicId.CozyLeaf))
            {
                foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime.Where(hero => hero != null && hero.IsAlive))
                {
                    hero.GainBlock(5);
                }

                AddLog("Cozy Leaf: all heroes gained 5 block.");
                Debug.Log("Cozy Leaf: all heroes gained 5 block.");
            }

            if (runManager.HasRelic(RelicId.PandaEmblem))
            {
                RuntimeHeroState panda = runManager.GetHeroState(HeroId.Panda);
                if (panda != null && panda.IsAlive)
                {
                    panda.GainBlock(10);
                    AddLog("Panda Emblem: Panda gained 10 block.");
                    Debug.Log("Panda Emblem: Panda gained 10 block.");
                }
            }

            if (runManager.ProgressionManager != null &&
                runManager.ProgressionManager.HasLevelFivePerk(HeroId.Panda))
            {
                RuntimeHeroState panda = runManager.GetHeroState(HeroId.Panda);
                if (panda != null && panda.IsAlive)
                {
                    panda.GainBlock(5);
                    AddLog("Panda passive: Panda gained 5 block.");
                }
            }
        }

        private void CreateEncounterForCurrentRun()
        {
            enemies.Clear();

            if (runManager.IsBossBattle)
            {
                enemies.Add(CreateBossEnemy());
                return;
            }

            switch (Mathf.Max(1, runManager.CurrentBattleIndex))
            {
                case 1:
                    enemies.Add(CreateEnemy("sporeling", "Sporeling", 50, 6));
                    enemies.Add(CreateEnemy("fungus_brute", "Fungus Brute", 78, 12));
                    enemies.Add(CreateEnemy("batty", "Batty", 40, 8));
                    break;
                case 2:
                    enemies.Add(CreateEnemy("cave_rat", "Cave Rat", 45, 7));
                    enemies.Add(CreateEnemy("thorn_sprite", "Thorn Sprite", 55, 9));
                    enemies.Add(CreateEnemy("moss_troll", "Moss Troll", 90, 14));
                    break;
                default:
                    enemies.Add(CreateEnemy("crystal_slime", "Crystal Slime", 65, 10));
                    enemies.Add(CreateEnemy("bandit_crow", "Bandit Crow", 60, 11));
                    enemies.Add(CreateEnemy("old_treant", "Old Treant", 110, 15));
                    break;
            }
        }

        private EnemyRuntimeState CreateEnemy(string enemyId, string enemyName, int maxHp, int attackDamage)
        {
            EnemyRuntimeState enemy = new EnemyRuntimeState
            {
                enemyId = enemyId,
                enemyName = enemyName,
                maxHp = maxHp,
                currentHp = maxHp,
                attackDamage = attackDamage,
                battleSprite = PawSlayersArtResolver.GetEnemyBattleSprite(enemyArtDatabase != null ? enemyArtDatabase : runManager != null ? runManager.enemyArtDatabase : null, enemyId, false),
                intentName = "Attack",
                intentDescription = $"Deal {attackDamage} damage to one hero."
            };

            UpdateEnemyIntent(enemy);
            return enemy;
        }

        private EnemyRuntimeState CreateBossEnemy()
        {
            EnemyRuntimeState boss = new EnemyRuntimeState
            {
                enemyId = "briar_king",
                enemyName = "Briar King",
                maxHp = 220,
                currentHp = 220,
                attackDamage = 16,
                battleSprite = PawSlayersArtResolver.GetEnemyBattleSprite(enemyArtDatabase != null ? enemyArtDatabase : runManager != null ? runManager.enemyArtDatabase : null, "briar_king", true),
                isBoss = true,
                currentPhase = 1,
                phaseTwoTriggered = false,
                patternStep = 0
            };

            UpdateBossIntent(boss);
            return boss;
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

                EnsureHeroViewDisplayFields(heroView);
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
                EnemyView enemyView = CanUseConfiguredEnemyViewPrefab()
                    ? Instantiate(enemyViewPrefab, enemyContainer)
                    : CreateRuntimeEnemyView(enemyContainer);

                EnsureEnemyViewDisplayFields(enemyView);
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

            foreach (RuntimeCardState card in runManager.Hand)
            {
                CardView cardView = cardViewPrefab != null
                    ? Instantiate(cardViewPrefab, handContainer)
                    : CreateRuntimeCardView(handContainer);

                EnsureCardViewInteractive(cardView);
                cardView.Setup(card, OnCardClicked);

                bool isDisabled = IsCardDisabled(card, out string disabledReason);
                cardView.SetDisabled(isDisabled, disabledReason);
                cardView.SetAffordable(GetModifiedCardCost(card, false) <= currentEnergy);
                cardView.SetSelected(selectedCard == card);
                handViews.Add(cardView);
            }
        }

        private void OnCardClicked(RuntimeCardState card)
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

            int modifiedCost = GetModifiedCardCost(card, false);
            if (modifiedCost > currentEnergy)
            {
                AddLog("Not enough energy.");
                return;
            }

            selectedCard = card;

            if (card.TargetType == TargetType.None)
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

        private void ResolveCardPlay(RuntimeCardState card, RuntimeHeroState chosenHeroTarget, EnemyRuntimeState chosenEnemyTarget)
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

            int modifiedCost = GetModifiedCardCost(card, true);
            if (modifiedCost > currentEnergy)
            {
                AddLog("Not enough energy.");
                return;
            }

            RuntimeHeroState ownerHero = GetOwnerHero(card);
            RuntimeHeroState heroTarget = ResolveHeroTarget(card, ownerHero, chosenHeroTarget);
            EnemyRuntimeState enemyTarget = ResolveEnemyTarget(card, chosenEnemyTarget);

            currentEnergy -= modifiedCost;

            string sourceName = ownerHero != null ? ownerHero.heroData.heroName : "Neutral";
            string targetName = enemyTarget != null
                ? enemyTarget.enemyName
                : heroTarget != null
                    ? heroTarget.heroData.heroName
                    : "no target";

            AddLog($"{sourceName} used {card.DisplayName} on {targetName}.");

            int damageAmount = GetModifiedDamage(card, card.Damage);
            if (card.baseCard != null &&
                card.baseCard.cardId == "momentum_kick" &&
                kangarooWrapsAttackCardsThisTurn == 1)
            {
                int bonusDamage = card.isUpgraded ? 7 : 5;
                damageAmount += bonusDamage;
                AddLog($"Momentum Kick bonus: +{bonusDamage} damage.");
            }

            if (damageAmount > 0 && card.TargetType == TargetType.AllEnemies)
            {
                foreach (EnemyRuntimeState enemy in enemies.Where(enemy => enemy.IsAlive))
                {
                    ApplyAttackDamageToEnemy(ownerHero, enemy, damageAmount);
                }
            }
            else if (damageAmount > 0 && enemyTarget != null)
            {
                ApplyAttackDamageToEnemy(ownerHero, enemyTarget, damageAmount);
            }

            if (card.Block > 0 && card.TargetType == TargetType.AllAllies)
            {
                foreach (RuntimeHeroState ally in runManager.ActiveHeroesRuntime.Where(hero => hero.IsAlive))
                {
                    ally.GainBlock(card.Block);
                    AddLog($"{ally.heroData.heroName} gained {card.Block} block.");
                }
            }
            else if (card.Block > 0 && heroTarget != null)
            {
                heroTarget.GainBlock(card.Block);
                AddLog($"{heroTarget.heroData.heroName} gained {card.Block} block.");
            }

            int healAmount = GetModifiedHeal(card, card.Heal);
            if (healAmount > 0 && card.TargetType == TargetType.AllAllies)
            {
                foreach (RuntimeHeroState ally in runManager.ActiveHeroesRuntime.Where(hero => hero.IsAlive))
                {
                    int beforeHp = ally.currentHp;
                    ally.Heal(healAmount);
                    int healedAmount = ally.currentHp - beforeHp;
                    AddLog($"{ally.heroData.heroName} healed {healedAmount} HP.");
                }
            }
            else if (healAmount > 0 && heroTarget != null)
            {
                int beforeHp = heroTarget.currentHp;
                heroTarget.Heal(healAmount);
                int healedAmount = heroTarget.currentHp - beforeHp;
                AddLog($"{heroTarget.heroData.heroName} healed {healedAmount} HP.");
            }

            if (card.DrawAmount > 0)
            {
                int drawn = runManager.DrawCards(card.DrawAmount).Count;
                AddLog($"Player drew {drawn} cards.");
            }

            ApplySpecialCardEffects(card, ownerHero);

            ApplyCardStatuses(card, ownerHero, chosenHeroTarget, chosenEnemyTarget);

            if (card.isUpgraded)
            {
                AddLog("Played upgraded card values: " + card.Description);
            }

            HandleRelicsAfterCardPlayed(card);

            runManager.DiscardCard(card);
            ClearCardSelection();
            RefreshAllUi();
            CheckBattleState();
        }

        private RuntimeHeroState ResolveHeroTarget(RuntimeCardState card, RuntimeHeroState ownerHero, RuntimeHeroState chosenHeroTarget)
        {
            if (card.TargetType == TargetType.Self)
            {
                return ownerHero;
            }

            if (card.TargetType == TargetType.Ally)
            {
                return chosenHeroTarget ?? runManager.ActiveHeroesRuntime.FirstOrDefault(hero => hero.IsAlive);
            }

            if (card.TargetType == TargetType.AllAllies)
            {
                return ownerHero ?? runManager.ActiveHeroesRuntime.FirstOrDefault(hero => hero.IsAlive);
            }

            return ownerHero ?? chosenHeroTarget;
        }

        private void ApplySpecialCardEffects(RuntimeCardState card, RuntimeHeroState ownerHero)
        {
            if (card == null || card.baseCard == null || ownerHero == null)
            {
                return;
            }

            switch (card.baseCard.cardId)
            {
                case "rally_cut":
                    ownerHero.AddStrength(1);
                    AddLog($"{ownerHero.heroData.heroName} gained 1 Strength.");
                    break;
                case "guardian_roar":
                    ownerHero.AddTaunt(card.isUpgraded ? 2 : 1);
                    AddLog($"{ownerHero.heroData.heroName} gained {(card.isUpgraded ? 2 : 1)} Taunt.");
                    break;
            }
        }

        private EnemyRuntimeState ResolveEnemyTarget(RuntimeCardState card, EnemyRuntimeState chosenEnemyTarget)
        {
            if (card.TargetType == TargetType.Enemy || card.TargetType == TargetType.AllEnemies)
            {
                return chosenEnemyTarget ?? enemies.FirstOrDefault(enemy => enemy.IsAlive);
            }

            return null;
        }

        private int GetModifiedCardCost(RuntimeCardState card, bool applyChanges)
        {
            int modifiedCost = card.Cost;

            if (runManager.HasRelic(RelicId.ThiefsBell) &&
                card.OwnerHeroId == HeroId.Koala &&
                !thiefsBellUsedThisTurn &&
                runManager.GetHeroState(HeroId.Koala)?.IsAlive == true)
            {
                modifiedCost = 0;

                if (applyChanges)
                {
                    thiefsBellUsedThisTurn = true;
                    AddLog("Thief's Bell: Koala card cost reduced to 0.");
                    Debug.Log("Thief's Bell: Koala card cost reduced to 0.");
                }
            }

            if (runManager.ProgressionManager != null &&
                runManager.ProgressionManager.HasLevelFivePerk(HeroId.Koala) &&
                card.OwnerHeroId == HeroId.Koala &&
                card.CardType == CardType.Skill &&
                !koalaLevelFiveUsedThisBattle &&
                runManager.GetHeroState(HeroId.Koala)?.IsAlive == true)
            {
                modifiedCost = 0;

                if (applyChanges)
                {
                    koalaLevelFiveUsedThisBattle = true;
                    AddLog("Koala passive: first Thief Skill cost reduced to 0.");
                }
            }

            return modifiedCost;
        }

        private int GetModifiedDamage(RuntimeCardState card, int baseDamage)
        {
            int modifiedDamage = baseDamage;

            if (modifiedDamage > 0 &&
                card.CardType == CardType.Attack &&
                runManager.HasRelic(RelicId.BambooCharm) &&
                !bambooCharmUsedThisBattle)
            {
                modifiedDamage += 3;
                bambooCharmUsedThisBattle = true;
                AddLog("Bamboo Charm: +3 damage applied.");
                Debug.Log("Bamboo Charm: +3 damage applied.");
            }

            if (modifiedDamage > 0 &&
                card.CardType == CardType.Attack &&
                card.OwnerHeroId == HeroId.Capybara &&
                runManager.ProgressionManager != null &&
                runManager.ProgressionManager.HasLevelFivePerk(HeroId.Capybara) &&
                !capybaraLevelFiveUsedThisBattle)
            {
                modifiedDamage += 2;
                capybaraLevelFiveUsedThisBattle = true;
                AddLog("Capybara passive: +2 damage applied.");
            }

            return modifiedDamage;
        }

        private int GetModifiedHeal(RuntimeCardState card, int baseHeal)
        {
            int modifiedHeal = baseHeal;

            if (modifiedHeal > 0 &&
                runManager.HasRelic(RelicId.SlothTea) &&
                !slothTeaUsedThisBattle)
            {
                modifiedHeal += 5;
                slothTeaUsedThisBattle = true;
                AddLog("Sloth Tea: +5 heal applied.");
                Debug.Log("Sloth Tea: +5 heal applied.");
            }

            if (modifiedHeal > 0 &&
                card.OwnerHeroId == HeroId.Sloth &&
                runManager.ProgressionManager != null &&
                runManager.ProgressionManager.HasLevelFivePerk(HeroId.Sloth) &&
                !slothLevelFiveUsedThisBattle)
            {
                modifiedHeal += 3;
                slothLevelFiveUsedThisBattle = true;
                AddLog("Sloth passive: +3 heal applied.");
            }

            return modifiedHeal;
        }

        private void HandleRelicsAfterCardPlayed(RuntimeCardState card)
        {
            if (card.CardType != CardType.Attack)
            {
                return;
            }

            kangarooWrapsAttackCardsThisTurn++;

            if (card.OwnerHeroId == HeroId.Kangaroo &&
                runManager.ProgressionManager != null &&
                runManager.ProgressionManager.HasLevelFivePerk(HeroId.Kangaroo))
            {
                kangarooLevelFiveAttackCardsThisTurn++;
                if (kangarooLevelFiveAttackCardsThisTurn >= 2)
                {
                    kangarooLevelFiveAttackCardsThisTurn = 0;
                    RuntimeHeroState kangaroo = runManager.GetHeroState(HeroId.Kangaroo);
                    if (kangaroo != null && kangaroo.IsAlive)
                    {
                        kangaroo.AddStrength(1);
                        AddLog("Kangaroo passive: gained 1 Strength.");
                    }
                }
            }

            if (!runManager.HasRelic(RelicId.KangarooWraps) || kangarooWrapsAttackCardsThisTurn < 2)
            {
                return;
            }

            kangarooWrapsAttackCardsThisTurn = 0;

            List<EnemyRuntimeState> livingEnemies = enemies.Where(enemy => enemy.IsAlive).ToList();
            if (livingEnemies.Count == 0)
            {
                return;
            }

            EnemyRuntimeState target = livingEnemies[Random.Range(0, livingEnemies.Count)];
            ApplyDamageToEnemy(target, 4);
            AddLog($"Kangaroo Wraps: bonus damage triggered on {target.enemyName}.");
            Debug.Log("Kangaroo Wraps: bonus damage triggered.");
        }

        private void RunEnemyTurn()
        {
            foreach (EnemyRuntimeState enemy in enemies.Where(enemy => enemy.IsAlive).ToList())
            {
                enemy.TickStartOfTurnStatuses(AddLog);
                UpdateBossPhaseIfNeeded(enemy);
                if (!enemy.IsAlive)
                {
                    continue;
                }

                if (enemy.statuses.stun > 0)
                {
                    AddLog($"{enemy.enemyName} is stunned and skips its turn.");
                    enemy.TickEndOfTurnStatuses(AddLog);
                    UpdateEnemyIntent(enemy);
                    continue;
                }

                if (enemy.isBoss)
                {
                    RunBossTurn(enemy);
                }
                else
                {
                    RunNormalEnemyTurn(enemy);
                }

                if (runManager.ActiveHeroesRuntime.All(hero => !hero.IsAlive))
                {
                    break;
                }
            }

            RefreshEnemyIntents();
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
                enemyTurnActive = false;
                CleanupTemporaryBattleCards();
                runManager.GrantRunLossXp();
                runManager.MarkRunLost();
                AddLog("Battle lost.");
                AddLog("Run lost.");
                ShowRunLost();
            }

            if (battleEnded && endTurnButton != null)
            {
                endTurnButton.interactable = false;
            }
        }

        private void ShowRunWon()
        {
            enemyTurnActive = false;
            AddLog("Run won!");
            ShowBattleEndPanel(runManager.GetRunSummaryText(true));
        }

        private void RefreshTopBar()
        {
            if (titleText != null)
            {
                titleText.text = "Paw Slayers";
            }

            if (turnText != null)
            {
                turnText.text = battleEnded
                    ? "Turn: Battle Ended"
                    : enemyTurnActive
                        ? "Turn: Enemy Turn"
                        : "Turn: Player Turn";
            }

            if (energyText != null)
            {
                energyText.text = $"Energy: {currentEnergy}/{MaxEnergy}";
            }

            if (runProgressText != null)
            {
                runProgressText.text = runManager.GetRunProgressLabel();
            }

            if (goldText != null)
            {
                goldText.text = $"Gold: {runManager.Gold}";
            }

            if (relicsText != null)
            {
                relicsText.text = runManager.OwnedRelics.Count > 0
                    ? $"Relics: {runManager.OwnedRelics.Count}"
                    : "Relics: No relics";
            }

            if (drawPileText != null)
            {
                drawPileText.text = $"Draw: {runManager.DrawPile.Count}";
            }

            if (discardPileText != null)
            {
                discardPileText.text = $"Discard: {runManager.DiscardPile.Count}";
            }

            if (endTurnButton != null)
            {
                bool rewardOpen = rewardCardManager != null && rewardCardManager.rewardPanel != null && rewardCardManager.rewardPanel.activeSelf;
                endTurnButton.interactable = !battleEnded && !enemyTurnActive && !rewardOpen;
            }
        }

        private bool IsCardDisabled(RuntimeCardState card, out string reason)
        {
            if (!runManager.CanPlayCard(card, out reason))
            {
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private bool IsValidHeroTarget(RuntimeCardState card, RuntimeHeroState hero)
        {
            if (card == null || hero == null || !hero.IsAlive)
            {
                return false;
            }

            RuntimeHeroState ownerHero = GetOwnerHero(card);
            switch (card.TargetType)
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

        private bool IsValidEnemyTarget(RuntimeCardState card, EnemyRuntimeState enemy)
        {
            if (card == null || enemy == null || !enemy.IsAlive)
            {
                return false;
            }

            return card.TargetType == TargetType.Enemy || card.TargetType == TargetType.AllEnemies;
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

        private RuntimeHeroState GetOwnerHero(RuntimeCardState card)
        {
            if (card == null || card.OwnerHeroId == HeroId.Neutral)
            {
                return null;
            }

            return runManager.GetHeroState(card.OwnerHeroId);
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

            string[] lines = battleLogText.text
                .Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Take(8)
                .ToArray();
            battleLogText.text = string.Join("\n", lines);
        }

        private void EnsureRunManager()
        {
            if (RunManager.Instance != null && runManager != RunManager.Instance)
            {
                runManager = RunManager.Instance;
            }

            if (runManager == null)
            {
                runManager = RunManager.Instance;
            }

            if (runManager != null)
            {
                if (enemyArtDatabase == null)
                {
                    enemyArtDatabase = runManager.enemyArtDatabase;
                }

                return;
            }

            GameObject runManagerObject = new GameObject("RunManager");
            runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();

            if (enemyArtDatabase == null && runManager != null)
            {
                enemyArtDatabase = runManager.enemyArtDatabase;
            }
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

            if (returnToSelectionButton != null)
            {
                returnToSelectionButton.onClick.RemoveAllListeners();
                returnToSelectionButton.onClick.AddListener(ReturnToHeroSelection);
            }

            if (debugToggleButton != null)
            {
                debugToggleButton.onClick.RemoveAllListeners();
                debugToggleButton.onClick.AddListener(ToggleDebugPanel);
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

        private void TickHeroStartOfTurnStatuses()
        {
            foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime)
            {
                if (hero == null || hero.heroData == null)
                {
                    continue;
                }

                hero.TickTaunt(AddLog);
                hero.TickStartOfTurnStatuses(AddLog);
                if (!hero.IsAlive)
                {
                    AddLog($"{hero.heroData.heroName} is down.");
                }
            }
        }

        private void TickHeroEndOfTurnStatuses()
        {
            foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime)
            {
                if (hero == null || hero.heroData == null)
                {
                    continue;
                }

                hero.TickEndOfTurnStatuses(AddLog);
                if (!hero.IsAlive)
                {
                    AddLog($"{hero.heroData.heroName} is down.");
                }
            }
        }

        private void RefreshEnemyIntents()
        {
            foreach (EnemyRuntimeState enemy in enemies)
            {
                UpdateEnemyIntent(enemy);
            }
        }

        private int ApplyAttackerModifiers(int baseDamage, int strengthAmount, int weakAmount)
        {
            int modifiedDamage = Mathf.Max(0, baseDamage + strengthAmount);

            if (weakAmount > 0)
            {
                modifiedDamage = Mathf.FloorToInt(modifiedDamage * 0.75f);
                AddLog("Weak reduced damage.");
            }

            return modifiedDamage;
        }

        private int ApplyTargetVulnerable(int damage, int vulnerableAmount)
        {
            int modifiedDamage = Mathf.Max(0, damage);

            if (vulnerableAmount > 0)
            {
                modifiedDamage = Mathf.CeilToInt(modifiedDamage * 1.5f);
                AddLog("Vulnerable increased damage.");
            }

            return modifiedDamage;
        }

        private int ApplyAttackDamageToEnemy(RuntimeHeroState attacker, EnemyRuntimeState target, int baseDamage)
        {
            if (target == null || !target.IsAlive)
            {
                return 0;
            }

            int finalDamage = ApplyAttackerModifiers(baseDamage, attacker != null ? attacker.statuses.strength : 0, attacker != null ? attacker.statuses.weak : 0);
            finalDamage = ApplyTargetVulnerable(finalDamage, target.statuses.vulnerable);
            ApplyDamageToEnemy(target, finalDamage);
            return finalDamage;
        }

        private int ApplyAttackDamageToHero(EnemyRuntimeState attacker, RuntimeHeroState target, int baseDamage)
        {
            if (target == null || !target.IsAlive)
            {
                return 0;
            }

            int finalDamage = ApplyAttackerModifiers(baseDamage, attacker != null ? attacker.statuses.strength : 0, attacker != null ? attacker.statuses.weak : 0);
            finalDamage = ApplyTargetVulnerable(finalDamage, target.statuses.vulnerable);
            target.TakeDamage(finalDamage);
            return finalDamage;
        }

        private List<RuntimeHeroState> GetHeroTargetsForCard(RuntimeCardState card, RuntimeHeroState ownerHero, RuntimeHeroState chosenHeroTarget)
        {
            if (card == null)
            {
                return new List<RuntimeHeroState>();
            }

            if (card.TargetType == TargetType.AllAllies)
            {
                return runManager.ActiveHeroesRuntime.Where(hero => hero != null && hero.IsAlive).ToList();
            }

            RuntimeHeroState target = ResolveHeroTarget(card, ownerHero, chosenHeroTarget);
            return target != null ? new List<RuntimeHeroState> { target } : new List<RuntimeHeroState>();
        }

        private List<EnemyRuntimeState> GetEnemyTargetsForCard(RuntimeCardState card, EnemyRuntimeState chosenEnemyTarget)
        {
            if (card == null)
            {
                return new List<EnemyRuntimeState>();
            }

            if (card.TargetType == TargetType.AllEnemies)
            {
                return enemies.Where(enemy => enemy.IsAlive).ToList();
            }

            EnemyRuntimeState target = ResolveEnemyTarget(card, chosenEnemyTarget);
            return target != null ? new List<EnemyRuntimeState> { target } : new List<EnemyRuntimeState>();
        }

        private void ApplyCardStatuses(RuntimeCardState card, RuntimeHeroState ownerHero, RuntimeHeroState chosenHeroTarget, EnemyRuntimeState chosenEnemyTarget)
        {
            foreach (RuntimeHeroState heroTarget in GetHeroTargetsForCard(card, ownerHero, chosenHeroTarget))
            {
                ApplyStatusesToHero(heroTarget, card);
            }

            foreach (EnemyRuntimeState enemyTarget in GetEnemyTargetsForCard(card, chosenEnemyTarget))
            {
                ApplyStatusesToEnemy(enemyTarget, card);
            }
        }

        private void ApplyStatusesToHero(RuntimeHeroState target, RuntimeCardState card)
        {
            if (target == null || card == null)
            {
                return;
            }

            ApplyStatusAmounts(
                target.heroData.heroName,
                card.WeakAmount,
                card.VulnerableAmount,
                card.StrengthAmount,
                card.BleedAmount,
                card.PoisonAmount,
                card.TauntAmount,
                card.StunAmount,
                card.SilenceAmount,
                target.AddWeak,
                target.AddVulnerable,
                target.AddStrength,
                target.AddBleed,
                target.AddPoison,
                target.AddTaunt,
                target.AddStun,
                target.AddSilence);
        }

        private void ApplyStatusesToEnemy(EnemyRuntimeState target, RuntimeCardState card)
        {
            if (target == null || card == null)
            {
                return;
            }

            ApplyStatusAmounts(
                target.enemyName,
                card.WeakAmount,
                card.VulnerableAmount,
                card.StrengthAmount,
                card.BleedAmount,
                card.PoisonAmount,
                card.TauntAmount,
                card.StunAmount,
                card.SilenceAmount,
                target.AddWeak,
                target.AddVulnerable,
                target.AddStrength,
                target.AddBleed,
                target.AddPoison,
                target.AddTaunt,
                target.AddStun,
                target.AddSilence);
        }

        private void ApplyStatusAmounts(
            string targetName,
            int weakAmount,
            int vulnerableAmount,
            int strengthAmount,
            int bleedAmount,
            int poisonAmount,
            int tauntAmount,
            int stunAmount,
            int silenceAmount,
            System.Action<int> addWeak,
            System.Action<int> addVulnerable,
            System.Action<int> addStrength,
            System.Action<int> addBleed,
            System.Action<int> addPoison,
            System.Action<int> addTaunt,
            System.Action<int> addStun,
            System.Action<int> addSilence)
        {
            if (weakAmount > 0)
            {
                addWeak(weakAmount);
                AddLog($"{targetName} gained {weakAmount} Weak.");
            }

            if (vulnerableAmount > 0)
            {
                addVulnerable(vulnerableAmount);
                AddLog($"{targetName} gained {vulnerableAmount} Vulnerable.");
            }

            if (strengthAmount > 0)
            {
                addStrength(strengthAmount);
                AddLog($"{targetName} gained {strengthAmount} Strength.");
            }

            if (bleedAmount > 0)
            {
                addBleed(bleedAmount);
                AddLog($"{targetName} gained {bleedAmount} Bleed.");
            }

            if (poisonAmount > 0)
            {
                addPoison(poisonAmount);
                AddLog($"{targetName} gained {poisonAmount} Poison.");
            }

            if (tauntAmount > 0)
            {
                addTaunt(tauntAmount);
                AddLog($"{targetName} gained {tauntAmount} Taunt.");
            }

            if (stunAmount > 0)
            {
                addStun(stunAmount);
                AddLog($"{targetName} gained {stunAmount} Stun.");
            }

            if (silenceAmount > 0)
            {
                addSilence(silenceAmount);
                AddLog($"{targetName} gained {silenceAmount} Silence.");
            }
        }

        private void ApplyDamageToEnemy(EnemyRuntimeState enemy, int damage)
        {
            if (enemy == null || !enemy.IsAlive || damage <= 0)
            {
                return;
            }

            enemy.TakeDamage(damage);
            UpdateBossPhaseIfNeeded(enemy);
        }

        private void RunNormalEnemyTurn(EnemyRuntimeState enemy)
        {
            if (enemy == null || !enemy.IsAlive)
            {
                return;
            }

            bool useBasicAttack = enemy.statuses.silence > 0 && !enemy.intentIsAttack;
            if (useBasicAttack)
            {
                AddLog($"{enemy.enemyName} is silenced and uses a basic attack.");
                RunEnemyBasicAttack(enemy);
                enemy.TickEndOfTurnStatuses(AddLog);
                AdvanceEnemyPattern(enemy);
                return;
            }

            ExecuteNormalEnemyPattern(enemy);
            enemy.TickEndOfTurnStatuses(AddLog);
            AdvanceEnemyPattern(enemy);
        }

        private void RunEnemyBasicAttack(EnemyRuntimeState enemy)
        {
            RuntimeHeroState targetHero = GetPreferredHeroTarget();
            if (targetHero == null)
            {
                return;
            }

            int finalDamage = ApplyAttackDamageToHero(enemy, targetHero, enemy.attackDamage);
            AddLog($"{enemy.enemyName} attacked {targetHero.heroData.heroName} for {finalDamage}.");

            if (!targetHero.IsAlive)
            {
                AddLog($"{targetHero.heroData.heroName} is down.");
            }
        }

        private void RunBossTurn(EnemyRuntimeState boss)
        {
            if (boss == null || !boss.IsAlive)
            {
                return;
            }

            UpdateBossPhaseIfNeeded(boss);

            if (!boss.IsAlive)
            {
                return;
            }

            bool useBasicAttack = boss.statuses.silence > 0 && !boss.intentIsAttack;
            if (useBasicAttack)
            {
                AddLog($"{boss.enemyName} is silenced and uses a basic attack.");
                RunEnemyBasicAttack(boss);
                boss.TickEndOfTurnStatuses(AddLog);
                boss.patternStep = (boss.patternStep + 1) % 3;
                UpdateBossIntent(boss);
                return;
            }

            if (boss.currentPhase == 1)
            {
                RunBossPhaseOneStep(boss);
            }
            else
            {
                RunBossPhaseTwoStep(boss);
            }

            boss.TickEndOfTurnStatuses(AddLog);
            boss.patternStep = (boss.patternStep + 1) % 3;
            UpdateBossIntent(boss);
        }

        private void RunBossPhaseOneStep(EnemyRuntimeState boss)
        {
            switch (boss.patternStep)
            {
                case 0:
                    AddLog("Briar King used Thorn Lash.");
                    DamageRandomHero(14, "Briar King");
                    break;
                case 1:
                    AddLog("Briar King used Briar Guard.");
                    boss.GainBlock(18);
                    break;
                default:
                    AddLog("Briar King used Root Snare.");
                    DamageAllLivingHeroes(8, "Briar King");
                    foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime.Where(hero => hero.IsAlive))
                    {
                        hero.AddWeak(1);
                        AddLog($"{hero.heroData.heroName} gained 1 Weak.");
                    }
                    AddTemporaryWoundToDiscard();
                    AddLog("A Wound was added to your discard pile.");
                    break;
            }
        }

        private void RunBossPhaseTwoStep(EnemyRuntimeState boss)
        {
            switch (boss.patternStep)
            {
                case 0:
                    AddLog("Briar King used Thornstorm.");
                    DamageAllLivingHeroes(10, "Briar King");
                    foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime.Where(hero => hero.IsAlive))
                    {
                        hero.AddBleed(1);
                        AddLog($"{hero.heroData.heroName} gained 1 Bleed.");
                    }
                    break;
                case 1:
                    AddLog("Briar King used Crushing Vines.");
                    RuntimeHeroState target = DamageRandomHero(22, "Briar King");
                    if (target != null && target.IsAlive)
                    {
                        target.AddStun(1);
                        AddLog($"{target.heroData.heroName} gained 1 Stun.");
                    }
                    break;
                default:
                    AddLog("Briar King used Regrowth.");
                    boss.Heal(18);
                    boss.GainBlock(10);
                    break;
            }
        }

        private void UpdateBossPhaseIfNeeded(EnemyRuntimeState enemy)
        {
            if (enemy == null || !enemy.isBoss || !enemy.IsAlive || enemy.phaseTwoTriggered || enemy.currentHp > BriarKingPhaseTwoThresholdHp)
            {
                return;
            }

            enemy.phaseTwoTriggered = true;
            enemy.currentPhase = 2;
            enemy.patternStep = 0;
            enemy.ClearBlock();
            enemy.GainBlock(20);
            AddLog("Briar King enrages!");
            UpdateBossIntent(enemy);
        }

        private void UpdateBossIntent(EnemyRuntimeState boss)
        {
            if (boss == null)
            {
                return;
            }

            boss.intentIsAttack = false;

            if (boss.currentPhase == 1)
            {
                switch (boss.patternStep)
                {
                    case 0:
                        boss.intentName = "Thorn Lash";
                        boss.intentDescription = "Deal 14 damage to one random active hero.";
                        boss.intentIsAttack = true;
                        break;
                    case 1:
                        boss.intentName = "Briar Guard";
                        boss.intentDescription = "Gain 18 block.";
                        break;
                    default:
                        boss.intentName = "Root Snare";
                        boss.intentDescription = "Deal 8 damage to all active heroes, apply 1 Weak, and add 1 Wound to discard.";
                        boss.intentIsAttack = true;
                        break;
                }

                return;
            }

            switch (boss.patternStep)
            {
                case 0:
                    boss.intentName = "Thornstorm";
                    boss.intentDescription = "Deal 10 damage to all active heroes and apply 1 Bleed.";
                    boss.intentIsAttack = true;
                    break;
                case 1:
                    boss.intentName = "Crushing Vines";
                    boss.intentDescription = "Deal 22 damage to one random active hero and apply 1 Stun.";
                    boss.intentIsAttack = true;
                    break;
                default:
                    boss.intentName = "Regrowth";
                    boss.intentDescription = "Heal 18 HP and gain 10 block.";
                        break;
            }
        }

        private void ExecuteNormalEnemyPattern(EnemyRuntimeState enemy)
        {
            switch (enemy.enemyId)
            {
                case "sporeling":
                    if (enemy.patternStep == 0)
                    {
                        RuntimeHeroState sporeTarget = GetPreferredHeroTarget();
                        if (sporeTarget != null)
                        {
                            sporeTarget.AddWeak(1);
                            AddLog("Sporeling used Spore Puff.");
                            AddLog($"{sporeTarget.heroData.heroName} gained 1 Weak.");
                        }
                    }
                    else
                    {
                        AddLog("Sporeling used Nip.");
                        RunEnemyBasicAttack(enemy);
                    }
                    break;
                case "fungus_brute":
                    if (enemy.patternStep == 0)
                    {
                        AddLog("Fungus Brute used Heavy Slam.");
                        RunEnemyBasicAttack(enemy);
                    }
                    else
                    {
                        AddLog("Fungus Brute used Harden.");
                        enemy.GainBlock(10);
                    }
                    break;
                case "batty":
                    if (enemy.patternStep == 0)
                    {
                        AddLog("Batty used Swoop.");
                        RunEnemyBasicAttack(enemy);
                    }
                    else
                    {
                        RuntimeHeroState battyTarget = GetPreferredHeroTarget();
                        if (battyTarget != null)
                        {
                            AddLog("Batty used Shriek.");
                            battyTarget.AddVulnerable(1);
                            battyTarget.AddSilence(1);
                            AddLog($"{battyTarget.heroData.heroName} gained 1 Vulnerable.");
                            AddLog($"{battyTarget.heroData.heroName} gained 1 Silence.");
                        }
                    }
                    break;
                case "cave_rat":
                    if (enemy.patternStep == 0)
                    {
                        AddLog("Cave Rat used Bite.");
                        RunEnemyBasicAttack(enemy);
                    }
                    else
                    {
                        RuntimeHeroState ratTarget = GetPreferredHeroTarget();
                        if (ratTarget != null)
                        {
                            AddLog("Cave Rat used Dirty Scratch.");
                            int finalDamage = ApplyAttackDamageToHero(enemy, ratTarget, 4);
                            AddLog($"{enemy.enemyName} attacked {ratTarget.heroData.heroName} for {finalDamage}.");
                            ratTarget.AddBleed(2);
                            AddLog($"{ratTarget.heroData.heroName} gained 2 Bleed.");
                            if (!ratTarget.IsAlive)
                            {
                                AddLog($"{ratTarget.heroData.heroName} is down.");
                            }
                        }
                    }
                    break;
                case "thorn_sprite":
                    if (enemy.patternStep == 0)
                    {
                        RuntimeHeroState spriteTarget = GetPreferredHeroTarget();
                        if (spriteTarget != null)
                        {
                            AddLog("Thorn Sprite used Thorn Shot.");
                            int finalDamage = ApplyAttackDamageToHero(enemy, spriteTarget, 8);
                            AddLog($"{enemy.enemyName} attacked {spriteTarget.heroData.heroName} for {finalDamage}.");
                            spriteTarget.AddVulnerable(1);
                            AddLog($"{spriteTarget.heroData.heroName} gained 1 Vulnerable.");
                            if (!spriteTarget.IsAlive)
                            {
                                AddLog($"{spriteTarget.heroData.heroName} is down.");
                            }
                        }
                    }
                    else if (enemy.patternStep == 1)
                    {
                        RuntimeHeroState poisonTarget = GetPreferredHeroTarget();
                        if (poisonTarget != null)
                        {
                            AddLog("Thorn Sprite used Poison Dust.");
                            poisonTarget.AddPoison(3);
                            AddLog($"{poisonTarget.heroData.heroName} gained 3 Poison.");
                        }
                    }
                    else
                    {
                        RuntimeHeroState dazeTarget = GetPreferredHeroTarget();
                        if (dazeTarget != null)
                        {
                            AddLog("Thorn Sprite used Dazing Dust.");
                            dazeTarget.AddStun(1);
                            AddLog($"{dazeTarget.heroData.heroName} gained 1 Stun.");
                        }
                    }
                    break;
                case "moss_troll":
                    if (enemy.patternStep == 0)
                    {
                        AddLog("Moss Troll used Smash.");
                        RunEnemyBasicAttack(enemy);
                    }
                    else
                    {
                        AddLog("Moss Troll used Regenerate.");
                        enemy.Heal(10);
                    }
                    break;
                case "crystal_slime":
                    if (enemy.patternStep == 0)
                    {
                        AddLog("Crystal Slime used Slam.");
                        RunEnemyBasicAttack(enemy);
                    }
                    else
                    {
                        AddLog("Crystal Slime used Shimmer.");
                        enemy.GainBlock(8);
                    }
                    break;
                case "bandit_crow":
                    if (enemy.patternStep == 0)
                    {
                        AddLog("Bandit Crow used Peck.");
                        RunEnemyBasicAttack(enemy);
                    }
                    else
                    {
                        AddLog("Bandit Crow used Steal Focus.");
                        foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime.Where(hero => hero.IsAlive))
                        {
                            hero.AddWeak(1);
                            AddLog($"{hero.heroData.heroName} gained 1 Weak.");
                        }
                    }
                    break;
                case "old_treant":
                    if (enemy.patternStep == 0)
                    {
                        AddLog("Old Treant used Branch Slam.");
                        RunEnemyBasicAttack(enemy);
                    }
                    else if (enemy.patternStep == 1)
                    {
                        RuntimeHeroState treantTarget = GetPreferredHeroTarget();
                        if (treantTarget != null)
                        {
                            AddLog("Old Treant used Root Bind.");
                            treantTarget.AddWeak(1);
                            treantTarget.AddVulnerable(1);
                            AddLog($"{treantTarget.heroData.heroName} gained 1 Weak.");
                            AddLog($"{treantTarget.heroData.heroName} gained 1 Vulnerable.");
                        }
                    }
                    else
                    {
                        AddLog("Old Treant used Bark Up.");
                        enemy.GainBlock(15);
                    }
                    break;
                default:
                    RunEnemyBasicAttack(enemy);
                    break;
            }
        }

        private void AdvanceEnemyPattern(EnemyRuntimeState enemy)
        {
            if (enemy == null || enemy.isBoss)
            {
                return;
            }

            int patternLength = 2;
            if (enemy.enemyId == "thorn_sprite" || enemy.enemyId == "old_treant")
            {
                patternLength = 3;
            }

            enemy.patternStep = (enemy.patternStep + 1) % patternLength;
            UpdateEnemyIntent(enemy);
        }

        private void UpdateEnemyIntent(EnemyRuntimeState enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (enemy.isBoss)
            {
                UpdateBossIntent(enemy);
                return;
            }

            enemy.intentIsAttack = false;

            switch (enemy.enemyId)
            {
                case "sporeling":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Spore Puff";
                        enemy.intentDescription = "Apply 1 Weak to a random hero.";
                    }
                    else
                    {
                        enemy.intentName = "Nip";
                        enemy.intentDescription = "Attack 6.";
                        enemy.intentIsAttack = true;
                    }
                    break;
                case "fungus_brute":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Heavy Slam";
                        enemy.intentDescription = "Attack 12.";
                        enemy.intentIsAttack = true;
                    }
                    else
                    {
                        enemy.intentName = "Harden";
                        enemy.intentDescription = "Gain 10 block.";
                    }
                    break;
                case "batty":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Swoop";
                        enemy.intentDescription = "Attack 8.";
                        enemy.intentIsAttack = true;
                    }
                    else
                    {
                        enemy.intentName = "Shriek";
                        enemy.intentDescription = "Apply 1 Vulnerable and 1 Silence.";
                    }
                    break;
                case "cave_rat":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Bite";
                        enemy.intentDescription = "Attack 7.";
                        enemy.intentIsAttack = true;
                    }
                    else
                    {
                        enemy.intentName = "Dirty Scratch";
                        enemy.intentDescription = "Attack 4 and apply 2 Bleed.";
                        enemy.intentIsAttack = true;
                    }
                    break;
                case "thorn_sprite":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Thorn Shot";
                        enemy.intentDescription = "Attack 8 and apply 1 Vulnerable.";
                        enemy.intentIsAttack = true;
                    }
                    else if (enemy.patternStep == 1)
                    {
                        enemy.intentName = "Poison Dust";
                        enemy.intentDescription = "Apply 3 Poison.";
                    }
                    else
                    {
                        enemy.intentName = "Dazing Dust";
                        enemy.intentDescription = "Apply 1 Stun.";
                    }
                    break;
                case "moss_troll":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Smash";
                        enemy.intentDescription = "Attack 14.";
                        enemy.intentIsAttack = true;
                    }
                    else
                    {
                        enemy.intentName = "Regenerate";
                        enemy.intentDescription = "Heal 10 HP.";
                    }
                    break;
                case "crystal_slime":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Slam";
                        enemy.intentDescription = "Attack 10.";
                        enemy.intentIsAttack = true;
                    }
                    else
                    {
                        enemy.intentName = "Shimmer";
                        enemy.intentDescription = "Gain 8 block.";
                    }
                    break;
                case "bandit_crow":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Peck";
                        enemy.intentDescription = "Attack 9.";
                        enemy.intentIsAttack = true;
                    }
                    else
                    {
                        enemy.intentName = "Steal Focus";
                        enemy.intentDescription = "Apply 1 Weak to all heroes.";
                    }
                    break;
                case "old_treant":
                    if (enemy.patternStep == 0)
                    {
                        enemy.intentName = "Branch Slam";
                        enemy.intentDescription = "Attack 15.";
                        enemy.intentIsAttack = true;
                    }
                    else if (enemy.patternStep == 1)
                    {
                        enemy.intentName = "Root Bind";
                        enemy.intentDescription = "Apply 1 Weak and 1 Vulnerable.";
                    }
                    else
                    {
                        enemy.intentName = "Bark Up";
                        enemy.intentDescription = "Gain 15 block.";
                    }
                    break;
                default:
                    enemy.intentName = "Attack";
                    enemy.intentDescription = $"Attack {enemy.attackDamage}.";
                    enemy.intentIsAttack = true;
                    break;
            }
        }

        private RuntimeHeroState DamageRandomHero(int damage, string attackerName)
        {
            RuntimeHeroState hero = GetPreferredHeroTarget();
            if (hero == null)
            {
                return null;
            }

            EnemyRuntimeState enemyAttacker = enemies.FirstOrDefault(enemy => enemy.IsAlive && enemy.enemyName == attackerName);
            if (enemyAttacker != null)
            {
                damage = ApplyAttackDamageToHero(enemyAttacker, hero, damage);
            }
            else
            {
                hero.TakeDamage(damage);
            }
            AddLog($"{attackerName} attacked {hero.heroData.heroName} for {damage}.");

            if (!hero.IsAlive)
            {
                AddLog($"{hero.heroData.heroName} is down.");
            }

            return hero;
        }

        private void DamageAllLivingHeroes(int damage, string attackerName)
        {
            foreach (RuntimeHeroState hero in runManager.ActiveHeroesRuntime.Where(hero => hero.IsAlive).ToList())
            {
                int finalDamage = damage;
                EnemyRuntimeState enemyAttacker = enemies.FirstOrDefault(enemy => enemy.IsAlive && enemy.enemyName == attackerName);
                if (enemyAttacker != null)
                {
                    finalDamage = ApplyAttackDamageToHero(enemyAttacker, hero, damage);
                }
                else
                {
                    hero.TakeDamage(finalDamage);
                }
                AddLog($"{attackerName} attacked {hero.heroData.heroName} for {finalDamage}.");

                if (!hero.IsAlive)
                {
                    AddLog($"{hero.heroData.heroName} is down.");
                }
            }
        }

        private RuntimeHeroState GetRandomLivingHero()
        {
            List<RuntimeHeroState> livingHeroes = runManager.ActiveHeroesRuntime.Where(hero => hero.IsAlive).ToList();
            if (livingHeroes.Count == 0)
            {
                return null;
            }

            return livingHeroes[Random.Range(0, livingHeroes.Count)];
        }

        private RuntimeHeroState GetPreferredHeroTarget()
        {
            List<RuntimeHeroState> tauntingHeroes = runManager.ActiveHeroesRuntime
                .Where(hero => hero.IsAlive && hero.statuses.taunt > 0)
                .ToList();

            if (tauntingHeroes.Count > 0)
            {
                return tauntingHeroes[Random.Range(0, tauntingHeroes.Count)];
            }

            return GetRandomLivingHero();
        }

        private EnemyRuntimeState GetBossEnemy()
        {
            return enemies.FirstOrDefault(enemy => enemy != null && enemy.isBoss);
        }

        private void ApplyStatusToEnemyDebug(int index, System.Action<EnemyRuntimeState> applyAction, string logMessage)
        {
            if (index < 0 || index >= enemies.Count || enemies[index] == null)
            {
                return;
            }

            applyAction?.Invoke(enemies[index]);
            AddLog(logMessage);
            RefreshAllUi();
        }

        private void ApplyStatusToHeroDebug(int index, System.Action<RuntimeHeroState> applyAction, string logMessage)
        {
            if (index < 0 || index >= runManager.ActiveHeroesRuntime.Count || runManager.ActiveHeroesRuntime[index] == null)
            {
                return;
            }

            applyAction?.Invoke(runManager.ActiveHeroesRuntime[index]);
            AddLog(logMessage);
            RefreshAllUi();
        }

        private void AddTemporaryWoundToDiscard()
        {
            RuntimeCardState wound = CreateWoundCard();
            runManager.AddTemporaryCardToDiscard(wound);
        }

        private RuntimeCardState CreateWoundCard()
        {
            CardData woundData = ScriptableObject.CreateInstance<CardData>();
            woundData.hideFlags = HideFlags.HideAndDontSave;
            woundData.cardId = WoundCardId;
            woundData.cardName = "Wound";
            woundData.description = "Unplayable. Clutters your deck.";
            woundData.ownerHeroId = HeroId.Neutral;
            woundData.cardType = CardType.Status;
            woundData.targetType = TargetType.None;
            woundData.cost = 0;
            return RuntimeCardState.Create(woundData);
        }

        private void CleanupTemporaryBattleCards()
        {
            runManager.RemoveTemporaryCards(WoundCardId);
        }

        private void ShowRunLost()
        {
            ShowBattleEndPanel(runManager.GetRunSummaryText(false));
        }

        private void ShowBattleEndPanel(string message)
        {
            if (runWonPanel != null)
            {
                runWonPanel.SetActive(true);
            }

            if (runWonText != null)
            {
                runWonText.text = message;
            }

            if (returnToSelectionButton != null)
            {
                returnToSelectionButton.gameObject.SetActive(true);
                returnToSelectionButton.interactable = true;
            }

            RefreshAllUi();
        }

        private void ReturnToHeroSelection()
        {
            if (runManager != null)
            {
                runManager.ResetRunAndReturnToSelection();
            }
        }

        private void ToggleDebugPanel()
        {
            if (debugPanelRoot == null)
            {
                return;
            }

            bool newState = !debugPanelRoot.activeSelf;
            debugPanelRoot.SetActive(newState);

            if (debugToggleText != null)
            {
                debugToggleText.text = newState ? "Hide Debug" : "Debug";
            }
        }

        private Button CreateDebugActionButton(string name, Transform parent, string label, UnityEngine.Events.UnityAction callback)
        {
            Button button = CreateButton(name, parent, label);
            if (button != null && callback != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(callback);
            }

            return button;
        }

        private bool CanUseConfiguredEnemyViewPrefab()
        {
            return enemyViewPrefab != null &&
                enemyViewPrefab.enemyNameText != null &&
                enemyViewPrefab.hpText != null &&
                enemyViewPrefab.blockText != null &&
                enemyViewPrefab.phaseText != null &&
                enemyViewPrefab.intentText != null &&
                enemyViewPrefab.intentDescriptionText != null;
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

            if (titleText != null && heroContainer != null && enemyContainer != null && handContainer != null && runProgressText != null && relicsText != null && rewardCardManager != null && rewardCardManager.continueButton != null && returnToSelectionButton != null)
            {
                rewardCardManager.battleUiManager = this;
                EnsureBattleScenePolish(canvas.transform);
                return;
            }

            foreach (Transform child in canvas.transform)
            {
                Destroy(child.gameObject);
            }

            GameObject root = CreatePanel("BattleRoot", canvas.transform, new Color(0.12f, 0.16f, 0.14f, 1f));
            StretchFull(root.GetComponent<RectTransform>(), 18f);

            GameObject topBar = CreatePanel("TopBar", root.transform, new Color(0.20f, 0.17f, 0.13f, 0.92f));
            RectTransform topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 1f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.pivot = new Vector2(0.5f, 1f);
            topBarRect.anchoredPosition = Vector2.zero;
            topBarRect.sizeDelta = new Vector2(0f, 88f);

            runProgressText = CreateText("RunProgressText", topBar.transform, new Vector2(20f, -18f), new Vector2(280f, 26f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            runProgressText.color = new Color(0.98f, 0.92f, 0.75f, 1f);
            titleText = CreateText("Title", topBar.transform, new Vector2(0f, -16f), new Vector2(380f, 32f), 32, FontStyle.Bold, TextAnchor.UpperCenter);
            titleText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            goldText = CreateText("GoldText", topBar.transform, new Vector2(-310f, -18f), new Vector2(130f, 24f), 18, FontStyle.Bold, TextAnchor.UpperRight);
            goldText.rectTransform.anchorMin = new Vector2(1f, 1f);
            goldText.rectTransform.anchorMax = new Vector2(1f, 1f);
            goldText.rectTransform.pivot = new Vector2(1f, 1f);
            goldText.rectTransform.anchoredPosition = new Vector2(-310f, -18f);
            goldText.color = new Color(0.95f, 0.84f, 0.44f, 1f);
            relicsText = CreateText("RelicsText", topBar.transform, new Vector2(-170f, -18f), new Vector2(150f, 24f), 18, FontStyle.Bold, TextAnchor.UpperRight);
            relicsText.rectTransform.anchorMin = new Vector2(1f, 1f);
            relicsText.rectTransform.anchorMax = new Vector2(1f, 1f);
            relicsText.rectTransform.pivot = new Vector2(1f, 1f);
            relicsText.rectTransform.anchoredPosition = new Vector2(-170f, -18f);
            relicsText.color = new Color(0.78f, 0.64f, 0.92f, 1f);
            debugToggleButton = CreateButton("DebugToggleButton", topBar.transform, "Debug");
            RectTransform debugToggleRect = debugToggleButton.GetComponent<RectTransform>();
            debugToggleRect.anchorMin = new Vector2(1f, 1f);
            debugToggleRect.anchorMax = new Vector2(1f, 1f);
            debugToggleRect.pivot = new Vector2(1f, 1f);
            debugToggleRect.anchoredPosition = new Vector2(-20f, -16f);
            debugToggleRect.sizeDelta = new Vector2(120f, 42f);
            debugToggleText = debugToggleButton.GetComponentInChildren<Text>();

            turnText = CreateText("TurnText", root.transform, new Vector2(24f, -108f), new Vector2(240f, 28f), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            turnText.color = new Color(0.96f, 0.94f, 0.89f, 1f);

            GameObject fieldArea = CreateUiObject("FieldArea", root.transform, Vector2.zero);
            RectTransform fieldRect = fieldArea.GetComponent<RectTransform>();
            fieldRect.anchorMin = new Vector2(0f, 0f);
            fieldRect.anchorMax = new Vector2(1f, 1f);
            fieldRect.offsetMin = new Vector2(22f, 296f);
            fieldRect.offsetMax = new Vector2(-22f, -118f);

            GameObject heroPanel = CreatePanel("HeroPanel", fieldArea.transform, new Color(0.94f, 0.90f, 0.82f, 0.96f));
            RectTransform heroPanelRect = heroPanel.GetComponent<RectTransform>();
            heroPanelRect.anchorMin = new Vector2(0f, 0f);
            heroPanelRect.anchorMax = new Vector2(0.29f, 1f);
            heroPanelRect.offsetMin = Vector2.zero;
            heroPanelRect.offsetMax = new Vector2(-14f, 0f);
            Text heroesLabel = CreateText("HeroesLabel", heroPanel.transform, new Vector2(14f, -12f), new Vector2(240f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            heroesLabel.text = "Active Heroes";
            heroContainer = CreateLayoutContainer("HeroContainer", heroPanel.transform, true, new Vector2(12f, 12f), new Vector2(-12f, -48f));

            Text versusText = CreateText("VersusText", fieldArea.transform, new Vector2(0f, -120f), new Vector2(120f, 44f), 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            versusText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            versusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            versusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            versusText.rectTransform.anchoredPosition = new Vector2(0f, -120f);
            versusText.text = "VS";
            versusText.color = new Color(0.87f, 0.75f, 0.45f, 0.75f);

            GameObject enemyPanel = CreatePanel("EnemyPanel", fieldArea.transform, new Color(0.21f, 0.17f, 0.15f, 0.96f));
            RectTransform enemyPanelRect = enemyPanel.GetComponent<RectTransform>();
            enemyPanelRect.anchorMin = new Vector2(0.71f, 0f);
            enemyPanelRect.anchorMax = new Vector2(1f, 1f);
            enemyPanelRect.offsetMin = new Vector2(14f, 0f);
            enemyPanelRect.offsetMax = Vector2.zero;
            Text enemiesLabel = CreateText("EnemiesLabel", enemyPanel.transform, new Vector2(14f, -12f), new Vector2(240f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            enemiesLabel.text = "Enemies";
            enemiesLabel.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            enemyContainer = CreateLayoutContainer("EnemyContainer", enemyPanel.transform, true, new Vector2(12f, 12f), new Vector2(-12f, -48f));

            GameObject bottomBar = CreatePanel("BottomBar", root.transform, new Color(0.20f, 0.17f, 0.13f, 0.92f));
            RectTransform bottomBarRect = bottomBar.GetComponent<RectTransform>();
            bottomBarRect.anchorMin = new Vector2(0f, 0f);
            bottomBarRect.anchorMax = new Vector2(1f, 0f);
            bottomBarRect.pivot = new Vector2(0.5f, 0f);
            bottomBarRect.anchoredPosition = Vector2.zero;
            bottomBarRect.sizeDelta = new Vector2(0f, 262f);

            energyText = CreateText("EnergyText", bottomBar.transform, new Vector2(20f, -18f), new Vector2(160f, 34f), 26, FontStyle.Bold, TextAnchor.UpperLeft);
            energyText.color = new Color(0.60f, 0.84f, 1f, 1f);
            drawPileText = CreateText("DrawPileText", bottomBar.transform, new Vector2(20f, -58f), new Vector2(120f, 22f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            drawPileText.color = new Color(0.93f, 0.93f, 0.98f, 1f);
            discardPileText = CreateText("DiscardPileText", bottomBar.transform, new Vector2(20f, -82f), new Vector2(120f, 22f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            discardPileText.color = new Color(0.93f, 0.93f, 0.98f, 1f);

            endTurnButton = CreateButton("EndTurnButton", bottomBar.transform, "End Turn");
            RectTransform endTurnRect = endTurnButton.GetComponent<RectTransform>();
            endTurnRect.anchorMin = new Vector2(0f, 0f);
            endTurnRect.anchorMax = new Vector2(0f, 0f);
            endTurnRect.pivot = new Vector2(0f, 0f);
            endTurnRect.anchoredPosition = new Vector2(20f, 18f);
            endTurnRect.sizeDelta = new Vector2(180f, 50f);

            GameObject handPanel = CreatePanel("HandPanel", bottomBar.transform, new Color(0.95f, 0.93f, 0.87f, 0.98f));
            RectTransform handPanelRect = handPanel.GetComponent<RectTransform>();
            handPanelRect.anchorMin = new Vector2(0f, 0f);
            handPanelRect.anchorMax = new Vector2(1f, 1f);
            handPanelRect.offsetMin = new Vector2(220f, 18f);
            handPanelRect.offsetMax = new Vector2(-360f, -18f);
            CreateText("HandLabel", handPanel.transform, new Vector2(14f, -12f), new Vector2(200f, 26f), 22, FontStyle.Bold, TextAnchor.UpperLeft).text = "Hand";
            handContainer = CreateLayoutContainer("HandContainer", handPanel.transform, false, new Vector2(10f, 12f), new Vector2(-10f, -44f));

            GameObject logPanel = CreatePanel("LogPanel", bottomBar.transform, new Color(0.16f, 0.20f, 0.24f, 0.96f));
            RectTransform logRect = logPanel.GetComponent<RectTransform>();
            logRect.anchorMin = new Vector2(1f, 0f);
            logRect.anchorMax = new Vector2(1f, 1f);
            logRect.pivot = new Vector2(1f, 0.5f);
            logRect.anchoredPosition = new Vector2(-20f, 0f);
            logRect.sizeDelta = new Vector2(320f, -36f);
            combatLogTitleText = CreateText("CombatLogTitle", logPanel.transform, new Vector2(14f, -12f), new Vector2(180f, 24f), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            combatLogTitleText.text = "Combat Log";
            combatLogTitleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            battleLogText = CreateText("BattleLog", logPanel.transform, new Vector2(14f, -42f), new Vector2(292f, 180f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
            battleLogText.color = new Color(0.92f, 0.94f, 0.97f, 1f);

            debugPanelRoot = CreatePanel("DebugPanel", root.transform, new Color(0.17f, 0.20f, 0.18f, 0.98f));
            GameObject debugPanel = debugPanelRoot;
            RectTransform debugRect = debugPanel.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(1f, 1f);
            debugRect.anchorMax = new Vector2(1f, 1f);
            debugRect.pivot = new Vector2(1f, 1f);
            debugRect.anchoredPosition = new Vector2(-20f, -92f);
            debugRect.sizeDelta = new Vector2(220f, 430f);
            Text debugLabel = CreateText("DebugLabel", debugPanel.transform, new Vector2(12f, -12f), new Vector2(180f, 28f), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            debugLabel.text = "Debug Tools";
            debugLabel.color = new Color(0.98f, 0.95f, 0.86f, 1f);

            debugButtonsContainer = CreateLayoutContainer("DebugButtons", debugPanel.transform, true, new Vector2(12f, 12f), new Vector2(-12f, -48f));
            Transform debugButtons = debugButtonsContainer;
            drawButton = CreateDebugActionButton("DrawButton", debugButtons, "Draw Card", DrawOneCard);
            killHero1Button = CreateDebugActionButton("KillHero1Button", debugButtons, "Kill Hero 1", () => MarkHeroDownByIndex(0));
            killHero2Button = CreateDebugActionButton("KillHero2Button", debugButtons, "Kill Hero 2", () => MarkHeroDownByIndex(1));
            killHero3Button = CreateDebugActionButton("KillHero3Button", debugButtons, "Kill Hero 3", () => MarkHeroDownByIndex(2));
            healAllButton = CreateDebugActionButton("HealAllButton", debugButtons, "Heal All", HealAllHeroes);
            winBattleButton = CreateDebugActionButton("WinBattleButton", debugButtons, "Win Battle", WinBattle);
            debugPanelRoot.SetActive(false);

            if (rewardCardManager == null)
            {
                rewardCardManager = gameObject.GetComponent<RewardCardManager>();
                if (rewardCardManager == null)
                {
                    rewardCardManager = gameObject.AddComponent<RewardCardManager>();
                }
            }

            rewardCardManager.battleUiManager = this;

            GameObject rewardPanel = CreatePanel("RewardPanel", root.transform, new Color(0f, 0f, 0f, 0.7f));
            StretchFull(rewardPanel.GetComponent<RectTransform>(), 0f);
            rewardPanel.SetActive(false);

            GameObject rewardBox = CreatePanel("RewardBox", rewardPanel.transform, new Color(0.97f, 0.95f, 0.88f, 1f));
            RectTransform rewardBoxRect = rewardBox.GetComponent<RectTransform>();
            rewardBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            rewardBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            rewardBoxRect.pivot = new Vector2(0.5f, 0.5f);
            rewardBoxRect.sizeDelta = new Vector2(900f, 520f);
            rewardBoxRect.anchoredPosition = Vector2.zero;

            rewardCardManager.rewardTitleText = CreateText("RewardTitle", rewardBox.transform, new Vector2(20f, -20f), new Vector2(400f, 32f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            rewardCardManager.rewardInfoText = CreateText("RewardInfo", rewardBox.transform, new Vector2(20f, -58f), new Vector2(500f, 24f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            rewardCardManager.rewardContainer = CreateLayoutContainer("RewardContainer", rewardBox.transform, false, new Vector2(20f, 90f), new Vector2(-20f, -100f));
            rewardCardManager.rewardPanel = rewardPanel;
            rewardCardManager.rewardCardPrefab = cardViewPrefab;

            rewardCardManager.continueButton = CreateButton("ContinueButton", rewardBox.transform, "Continue");
            RectTransform continueRect = rewardCardManager.continueButton.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(1f, 0f);
            continueRect.anchorMax = new Vector2(1f, 0f);
            continueRect.pivot = new Vector2(1f, 0f);
            continueRect.anchoredPosition = new Vector2(-20f, 20f);
            continueRect.sizeDelta = new Vector2(180f, 48f);
            rewardCardManager.continueButton.gameObject.SetActive(false);

            runWonPanel = CreatePanel("RunWonPanel", root.transform, new Color(0f, 0f, 0f, 0.7f));
            StretchFull(runWonPanel.GetComponent<RectTransform>(), 0f);
            runWonPanel.SetActive(false);

            GameObject runWonBox = CreatePanel("RunWonBox", runWonPanel.transform, new Color(0.97f, 0.95f, 0.88f, 1f));
            RectTransform runWonRect = runWonBox.GetComponent<RectTransform>();
            runWonRect.anchorMin = new Vector2(0.5f, 0.5f);
            runWonRect.anchorMax = new Vector2(0.5f, 0.5f);
            runWonRect.pivot = new Vector2(0.5f, 0.5f);
            runWonRect.sizeDelta = new Vector2(760f, 360f);
            runWonRect.anchoredPosition = Vector2.zero;
            runWonText = CreateText("RunWonText", runWonBox.transform, new Vector2(40f, -40f), new Vector2(680f, 220f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            returnToSelectionButton = CreateButton("ReturnToSelectionButton", runWonBox.transform, "Return to Hero Selection");
            RectTransform returnRect = returnToSelectionButton.GetComponent<RectTransform>();
            returnRect.anchorMin = new Vector2(0.5f, 0f);
            returnRect.anchorMax = new Vector2(0.5f, 0f);
            returnRect.pivot = new Vector2(0.5f, 0f);
            returnRect.anchoredPosition = new Vector2(0f, 20f);
            returnRect.sizeDelta = new Vector2(260f, 52f);
            returnToSelectionButton.gameObject.SetActive(false);
        }

        private void EnsureBattleScenePolish(Transform canvasTransform)
        {
            if (canvasTransform == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = "Paw Slayers";
                titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            }

            if (turnText != null)
            {
                turnText.color = new Color(0.96f, 0.94f, 0.89f, 1f);
            }

            if (runProgressText != null)
            {
                runProgressText.color = new Color(0.98f, 0.92f, 0.75f, 1f);
            }

            if (relicsText != null)
            {
                relicsText.color = new Color(0.78f, 0.64f, 0.92f, 1f);
            }

            if (goldText == null && titleText != null)
            {
                goldText = CreateText("GoldText", titleText.transform.parent, new Vector2(-310f, -18f), new Vector2(130f, 24f), 18, FontStyle.Bold, TextAnchor.UpperRight);
                goldText.rectTransform.anchorMin = new Vector2(1f, 1f);
                goldText.rectTransform.anchorMax = new Vector2(1f, 1f);
                goldText.rectTransform.pivot = new Vector2(1f, 1f);
                goldText.rectTransform.anchoredPosition = new Vector2(-310f, -18f);
                goldText.color = new Color(0.95f, 0.84f, 0.44f, 1f);
            }

            if (drawPileText == null && energyText != null)
            {
                drawPileText = CreateText("DrawPileText", energyText.transform.parent, new Vector2(20f, -58f), new Vector2(120f, 20f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
                drawPileText.color = new Color(0.93f, 0.93f, 0.98f, 1f);
            }

            if (discardPileText == null && energyText != null)
            {
                discardPileText = CreateText("DiscardPileText", energyText.transform.parent, new Vector2(20f, -82f), new Vector2(120f, 20f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
                discardPileText.color = new Color(0.93f, 0.93f, 0.98f, 1f);
            }

            if (combatLogTitleText == null && battleLogText != null)
            {
                combatLogTitleText = CreateText("CombatLogTitle", battleLogText.transform.parent, new Vector2(12f, -12f), new Vector2(180f, 24f), 20, FontStyle.Bold, TextAnchor.UpperLeft);
                combatLogTitleText.text = "Combat Log";
            }

            if (debugPanelRoot == null)
            {
                Transform debugTransform = canvasTransform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DebugPanel");
                if (debugTransform != null)
                {
                    debugPanelRoot = debugTransform.gameObject;
                    debugPanelRoot.SetActive(false);
                }
            }

            if (endTurnButton == null)
            {
                Transform endTurnTransform = canvasTransform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "EndTurnButton");
                if (endTurnTransform != null)
                {
                    endTurnButton = endTurnTransform.GetComponent<Button>();
                }
            }

            if (drawButton == null)
            {
                Transform drawTransform = canvasTransform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DrawButton");
                if (drawTransform != null)
                {
                    drawButton = drawTransform.GetComponent<Button>();
                }
            }

            if (debugToggleButton == null && titleText != null)
            {
                debugToggleButton = CreateButton("DebugToggleButton", titleText.transform.parent, "Debug");
                RectTransform debugToggleRect = debugToggleButton.GetComponent<RectTransform>();
                debugToggleRect.anchorMin = new Vector2(1f, 1f);
                debugToggleRect.anchorMax = new Vector2(1f, 1f);
                debugToggleRect.pivot = new Vector2(1f, 1f);
                debugToggleRect.anchoredPosition = new Vector2(-20f, -16f);
                debugToggleRect.sizeDelta = new Vector2(120f, 42f);
                debugToggleText = debugToggleButton.GetComponentInChildren<Text>();
            }

            if (battleLogText != null)
            {
                battleLogText.color = new Color(0.92f, 0.94f, 0.97f, 1f);
            }

            if (heroContainer != null)
            {
                VerticalLayoutGroup heroLayout = heroContainer.GetComponent<VerticalLayoutGroup>();
                if (heroLayout != null)
                {
                    heroLayout.spacing = 12f;
                }
            }

            if (enemyContainer != null)
            {
                VerticalLayoutGroup enemyLayout = enemyContainer.GetComponent<VerticalLayoutGroup>();
                if (enemyLayout != null)
                {
                    enemyLayout.spacing = 12f;
                }
            }

            if (handContainer != null)
            {
                HorizontalLayoutGroup handLayout = handContainer.GetComponent<HorizontalLayoutGroup>();
                if (handLayout != null)
                {
                    handLayout.spacing = 12f;
                    handLayout.childAlignment = TextAnchor.LowerCenter;
                }
            }
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

        private void EnsureHeroViewDisplayFields(BattleHeroView view)
        {
            if (view == null)
            {
                return;
            }

            LayoutElement layout = view.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = Mathf.Max(layout.preferredHeight, 192f);
            }

            RectTransform rect = view.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.y < 192f)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 192f);
            }

            if (view.statusText == null)
            {
                view.statusText = CreateText("StatusText", view.transform, new Vector2(12f, -144f), new Vector2(230f, 34f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
            }

            if (view.tauntText == null)
            {
                view.tauntText = CreateText("TauntText", view.transform, new Vector2(150f, -98f), new Vector2(90f, 20f), 14, FontStyle.Bold, TextAnchor.UpperRight);
                view.tauntText.color = new Color(0.66f, 0.42f, 0.12f, 1f);
            }

            if (view.portraitImage == null)
            {
                GameObject portrait = CreatePanel("Portrait", view.transform, new Color(0.75f, 0.75f, 0.75f, 1f));
                RectTransform portraitRect = portrait.GetComponent<RectTransform>();
                portraitRect.anchorMin = new Vector2(1f, 1f);
                portraitRect.anchorMax = new Vector2(1f, 1f);
                portraitRect.pivot = new Vector2(1f, 1f);
                portraitRect.anchoredPosition = new Vector2(-14f, -14f);
                portraitRect.sizeDelta = new Vector2(62f, 62f);
                view.portraitImage = portrait.GetComponent<Image>();
            }

            if (view.portraitImage != null)
            {
                RectTransform portraitRect = view.portraitImage.rectTransform;
                portraitRect.anchorMin = new Vector2(1f, 1f);
                portraitRect.anchorMax = new Vector2(1f, 1f);
                portraitRect.pivot = new Vector2(1f, 1f);
                portraitRect.anchoredPosition = new Vector2(-14f, -14f);
                portraitRect.sizeDelta = new Vector2(78f, 78f);
                view.portraitImage.preserveAspect = true;
            }

            EnsureBarVisuals(view.transform, ref view.hpBarFillImage, "HpBarFill", new Vector2(12f, -72f), new Vector2(170f, 12f), new Color(0.78f, 0.18f, 0.18f, 1f), new Color(0.22f, 0.14f, 0.14f, 1f));
            EnsureDimOverlay(view.transform, ref view.dimOverlayImage, "DimOverlay");
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

        private void EnsureEnemyViewDisplayFields(EnemyView view)
        {
            if (view == null)
            {
                return;
            }

            LayoutElement layout = view.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = Mathf.Max(layout.preferredHeight, 224f);
            }

            RectTransform rect = view.GetComponent<RectTransform>();
            if (rect != null && rect.sizeDelta.y < 224f)
            {
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 224f);
            }

            if (view.blockText == null)
            {
                view.blockText = CreateText("BlockText", view.transform, new Vector2(12f, -66f), new Vector2(220f, 22f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            }

            if (view.phaseText == null)
            {
                view.phaseText = CreateText("PhaseText", view.transform, new Vector2(12f, -90f), new Vector2(220f, 22f), 18, FontStyle.Bold, TextAnchor.UpperLeft);
            }

            if (view.intentDescriptionText == null)
            {
                view.intentDescriptionText = CreateText("IntentDescriptionText", view.transform, new Vector2(12f, -138f), new Vector2(220f, 32f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            }

            if (view.statusText == null)
            {
                view.statusText = CreateText("StatusText", view.transform, new Vector2(12f, -172f), new Vector2(220f, 32f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
            }

            if (view.spriteImage == null)
            {
                GameObject spriteObject = CreatePanel("EnemySprite", view.transform, new Color(0.70f, 0.70f, 0.74f, 1f));
                RectTransform spriteRect = spriteObject.GetComponent<RectTransform>();
                spriteRect.anchorMin = new Vector2(1f, 1f);
                spriteRect.anchorMax = new Vector2(1f, 1f);
                spriteRect.pivot = new Vector2(1f, 1f);
                spriteRect.anchoredPosition = new Vector2(-14f, -14f);
                spriteRect.sizeDelta = new Vector2(88f, 88f);
                view.spriteImage = spriteObject.GetComponent<Image>();
            }

            if (view.spriteImage != null)
            {
                RectTransform spriteRect = view.spriteImage.rectTransform;
                spriteRect.anchorMin = new Vector2(1f, 1f);
                spriteRect.anchorMax = new Vector2(1f, 1f);
                spriteRect.pivot = new Vector2(1f, 1f);
                spriteRect.anchoredPosition = new Vector2(-14f, -14f);
                spriteRect.sizeDelta = new Vector2(88f, 88f);
                view.spriteImage.preserveAspect = true;
            }

            EnsureBarVisuals(view.transform, ref view.hpBarFillImage, "HpBarFill", new Vector2(12f, -42f), new Vector2(180f, 12f), new Color(0.82f, 0.22f, 0.22f, 1f), new Color(0.20f, 0.12f, 0.12f, 1f));
            EnsureDimOverlay(view.transform, ref view.dimOverlayImage, "DimOverlay");
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

            EnsureCardVisuals(view);
        }

        private void EnsureCardVisuals(CardView view)
        {
            if (view == null)
            {
                return;
            }

            LayoutElement layout = view.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = 220f;
                layout.preferredHeight = 318f;
            }

            RectTransform rect = view.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(220f, 318f);
            }

            if (view.backgroundImage != null)
            {
                view.backgroundImage.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            }

            if (view.costBadgeImage == null)
            {
                GameObject badge = CreatePanel("CostBadge", view.transform, new Color(0.23f, 0.42f, 0.72f, 1f));
                RectTransform badgeRect = badge.GetComponent<RectTransform>();
                badgeRect.anchorMin = new Vector2(0f, 1f);
                badgeRect.anchorMax = new Vector2(0f, 1f);
                badgeRect.pivot = new Vector2(0f, 1f);
                badgeRect.anchoredPosition = new Vector2(10f, -10f);
                badgeRect.sizeDelta = new Vector2(42f, 42f);
                view.costBadgeImage = badge.GetComponent<Image>();

                if (view.costText != null)
                {
                    view.costText.transform.SetParent(badge.transform, false);
                    RectTransform costRect = view.costText.rectTransform;
                    StretchFull(costRect, 0f);
                    view.costText.alignment = TextAnchor.MiddleCenter;
                    view.costText.color = Color.white;
                }
            }

            if (view.upgradedLabelText == null)
            {
                view.upgradedLabelText = CreateText("UpgradedLabel", view.transform, new Vector2(126f, -12f), new Vector2(84f, 22f), 12, FontStyle.Bold, TextAnchor.UpperRight);
                view.upgradedLabelText.color = new Color(0.64f, 0.45f, 0.08f, 1f);
            }

            if (view.disabledOverlayImage == null)
            {
                GameObject overlay = CreatePanel("DisabledOverlay", view.transform, new Color(0.12f, 0.12f, 0.14f, 0.35f));
                StretchFull(overlay.GetComponent<RectTransform>(), 0f);
                overlay.transform.SetAsLastSibling();
                view.disabledOverlayImage = overlay.GetComponent<Image>();
                view.disabledOverlayImage.enabled = false;

                if (view.disabledReasonText != null)
                {
                    view.disabledReasonText.transform.SetParent(overlay.transform, false);
                    RectTransform reasonRect = view.disabledReasonText.rectTransform;
                    reasonRect.anchorMin = new Vector2(0f, 0f);
                    reasonRect.anchorMax = new Vector2(1f, 0f);
                    reasonRect.pivot = new Vector2(0.5f, 0f);
                    reasonRect.anchoredPosition = new Vector2(0f, 14f);
                    reasonRect.sizeDelta = new Vector2(-16f, 36f);
                    view.disabledReasonText.color = new Color(1f, 0.95f, 0.95f, 1f);
                }
            }
        }

        private void EnsureBarVisuals(Transform parent, ref Image fillImage, string name, Vector2 anchoredPosition, Vector2 size, Color fillColor, Color backgroundColor)
        {
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                return;
            }

            GameObject barRoot = CreatePanel(name + "Root", parent, backgroundColor);
            RectTransform bgRect = barRoot.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.pivot = new Vector2(0f, 1f);
            bgRect.anchoredPosition = anchoredPosition;
            bgRect.sizeDelta = size;

            GameObject fill = CreatePanel(name, barRoot.transform, fillColor);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            StretchFull(fillRect, 0f);
            fillImage = fill.GetComponent<Image>();
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
        }

        private void EnsureDimOverlay(Transform parent, ref Image overlayImage, string name)
        {
            if (overlayImage != null)
            {
                return;
            }

            GameObject overlay = CreatePanel(name, parent, new Color(0f, 0f, 0f, 0.32f));
            StretchFull(overlay.GetComponent<RectTransform>(), 0f);
            overlay.transform.SetAsLastSibling();
            overlayImage = overlay.GetComponent<Image>();
            overlayImage.enabled = false;
        }

        private BattleHeroView CreateRuntimeHeroView(Transform parent)
        {
            GameObject root = CreatePanel("BattleHeroView", parent, new Color(0.92f, 0.88f, 0.78f, 1f));
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 192f;
            Button button = root.AddComponent<Button>();
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            BattleHeroView view = root.AddComponent<BattleHeroView>();
            view.backgroundImage = root.GetComponent<Image>();
            view.button = button;
            view.highlightOutline = outline;
            view.heroNameText = CreateText("HeroName", root.transform, new Vector2(14f, -14f), new Vector2(190f, 26f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            view.heroClassText = CreateText("HeroClass", root.transform, new Vector2(14f, -40f), new Vector2(190f, 20f), 17, FontStyle.Italic, TextAnchor.UpperLeft);
            view.hpText = CreateText("HpText", root.transform, new Vector2(14f, -90f), new Vector2(180f, 20f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            view.blockText = CreateText("BlockText", root.transform, new Vector2(14f, -114f), new Vector2(120f, 20f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            view.tauntText = CreateText("TauntText", root.transform, new Vector2(146f, -114f), new Vector2(90f, 20f), 14, FontStyle.Bold, TextAnchor.UpperRight);
            view.tauntText.color = new Color(0.66f, 0.42f, 0.12f, 1f);
            view.stateText = CreateText("StateText", root.transform, new Vector2(14f, -136f), new Vector2(160f, 20f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            view.statusText = CreateText("StatusText", root.transform, new Vector2(14f, -158f), new Vector2(228f, 28f), 13, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject portrait = CreatePanel("Portrait", root.transform, new Color(0.75f, 0.75f, 0.75f, 1f));
            RectTransform portraitRect = portrait.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(1f, 1f);
            portraitRect.anchorMax = new Vector2(1f, 1f);
            portraitRect.pivot = new Vector2(1f, 1f);
            portraitRect.anchoredPosition = new Vector2(-14f, -14f);
            portraitRect.sizeDelta = new Vector2(78f, 78f);
            view.portraitImage = portrait.GetComponent<Image>();
            view.portraitImage.preserveAspect = true;
            EnsureBarVisuals(root.transform, ref view.hpBarFillImage, "HpBarFill", new Vector2(14f, -66f), new Vector2(160f, 12f), new Color(0.78f, 0.18f, 0.18f, 1f), new Color(0.22f, 0.14f, 0.14f, 1f));
            EnsureDimOverlay(root.transform, ref view.dimOverlayImage, "DimOverlay");
            return view;
        }

        private EnemyView CreateRuntimeEnemyView(Transform parent)
        {
            GameObject root = CreatePanel("EnemyView", parent, new Color(0.48f, 0.37f, 0.30f, 1f));
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredHeight = 224f;
            Button button = root.AddComponent<Button>();
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.9f, 0.2f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            EnemyView view = root.AddComponent<EnemyView>();
            view.backgroundImage = root.GetComponent<Image>();
            view.button = button;
            view.highlightOutline = outline;
            view.enemyNameText = CreateText("EnemyName", root.transform, new Vector2(14f, -14f), new Vector2(220f, 24f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            view.enemyNameText.color = new Color(0.98f, 0.95f, 0.88f, 1f);
            view.hpText = CreateText("HpText", root.transform, new Vector2(14f, -90f), new Vector2(220f, 20f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            view.hpText.color = new Color(0.98f, 0.95f, 0.88f, 1f);
            view.blockText = CreateText("BlockText", root.transform, new Vector2(14f, -114f), new Vector2(220f, 20f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            view.blockText.color = new Color(0.90f, 0.92f, 0.98f, 1f);
            view.phaseText = CreateText("PhaseText", root.transform, new Vector2(14f, -136f), new Vector2(220f, 20f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            view.phaseText.color = new Color(0.98f, 0.83f, 0.60f, 1f);
            view.intentText = CreateText("IntentText", root.transform, new Vector2(14f, -160f), new Vector2(220f, 20f), 16, FontStyle.Bold, TextAnchor.UpperLeft);
            view.intentText.color = new Color(1f, 0.92f, 0.74f, 1f);
            view.intentDescriptionText = CreateText("IntentDescriptionText", root.transform, new Vector2(14f, -184f), new Vector2(220f, 24f), 13, FontStyle.Normal, TextAnchor.UpperLeft);
            view.intentDescriptionText.color = new Color(0.96f, 0.94f, 0.89f, 1f);
            view.statusText = CreateText("StatusText", root.transform, new Vector2(14f, -206f), new Vector2(220f, 18f), 12, FontStyle.Normal, TextAnchor.UpperLeft);
            view.statusText.color = new Color(0.88f, 0.90f, 0.95f, 1f);

            GameObject sprite = CreatePanel("EnemySprite", root.transform, new Color(0.70f, 0.70f, 0.74f, 1f));
            RectTransform spriteRect = sprite.GetComponent<RectTransform>();
            spriteRect.anchorMin = new Vector2(1f, 1f);
            spriteRect.anchorMax = new Vector2(1f, 1f);
            spriteRect.pivot = new Vector2(1f, 1f);
            spriteRect.anchoredPosition = new Vector2(-14f, -14f);
            spriteRect.sizeDelta = new Vector2(88f, 88f);
            view.spriteImage = sprite.GetComponent<Image>();
            view.spriteImage.preserveAspect = true;

            EnsureBarVisuals(root.transform, ref view.hpBarFillImage, "HpBarFill", new Vector2(14f, -66f), new Vector2(170f, 12f), new Color(0.82f, 0.22f, 0.22f, 1f), new Color(0.20f, 0.12f, 0.12f, 1f));
            EnsureDimOverlay(root.transform, ref view.dimOverlayImage, "DimOverlay");
            return view;
        }

        private CardView CreateRuntimeCardView(Transform parent)
        {
            GameObject root = CreatePanel("CardView", parent, new Color(0.96f, 0.93f, 0.84f, 1f));
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 220f;
            layout.preferredHeight = 318f;

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
            view.cardNameText = CreateText("CardName", root.transform, new Vector2(60f, -10f), new Vector2(148f, 34f), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            view.costText = CreateText("CostText", root.transform, Vector2.zero, new Vector2(42f, 42f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            view.ownerText = CreateText("OwnerText", root.transform, new Vector2(12f, -48f), new Vector2(196f, 20f), 14, FontStyle.Italic, TextAnchor.UpperLeft);
            view.typeText = CreateText("TypeText", root.transform, new Vector2(12f, -68f), new Vector2(196f, 20f), 14, FontStyle.Bold, TextAnchor.UpperLeft);
            view.upgradedLabelText = CreateText("UpgradedLabel", root.transform, new Vector2(130f, -12f), new Vector2(74f, 18f), 11, FontStyle.Bold, TextAnchor.UpperRight);
            view.upgradedLabelText.color = new Color(0.64f, 0.45f, 0.08f, 1f);

            GameObject art = CreatePanel("Art", root.transform, new Color(0.82f, 0.82f, 0.82f, 1f));
            RectTransform artRect = art.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0f, 1f);
            artRect.anchorMax = new Vector2(0f, 1f);
            artRect.pivot = new Vector2(0f, 1f);
            artRect.anchoredPosition = new Vector2(16f, -94f);
            artRect.sizeDelta = new Vector2(188f, 92f);
            view.artImage = art.GetComponent<Image>();

            GameObject costBadge = CreatePanel("CostBadge", root.transform, new Color(0.23f, 0.42f, 0.72f, 1f));
            RectTransform badgeRect = costBadge.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(10f, -10f);
            badgeRect.sizeDelta = new Vector2(42f, 42f);
            view.costBadgeImage = costBadge.GetComponent<Image>();
            view.costText.transform.SetParent(costBadge.transform, false);
            StretchFull(view.costText.rectTransform, 0f);
            view.costText.color = Color.white;

            view.descriptionText = CreateText("Description", root.transform, new Vector2(12f, -194f), new Vector2(196f, 80f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
            view.disabledReasonText = CreateText("DisabledReason", root.transform, new Vector2(10f, -286f), new Vector2(196f, 24f), 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            view.disabledReasonText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
            GameObject overlay = CreatePanel("DisabledOverlay", root.transform, new Color(0.12f, 0.12f, 0.14f, 0.35f));
            StretchFull(overlay.GetComponent<RectTransform>(), 0f);
            overlay.transform.SetAsLastSibling();
            view.disabledOverlayImage = overlay.GetComponent<Image>();
            view.disabledOverlayImage.enabled = false;
            view.disabledReasonText.transform.SetParent(overlay.transform, false);
            RectTransform reasonRect = view.disabledReasonText.rectTransform;
            reasonRect.anchorMin = new Vector2(0f, 0f);
            reasonRect.anchorMax = new Vector2(1f, 0f);
            reasonRect.pivot = new Vector2(0.5f, 0f);
            reasonRect.anchoredPosition = new Vector2(0f, 16f);
            reasonRect.sizeDelta = new Vector2(-16f, 32f);
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
            GameObject buttonObject = CreatePanel(name, parent, new Color(0.39f, 0.30f, 0.16f, 1f));
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(180f, 48f);
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 180f;
            layout.preferredHeight = 48f;

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.39f, 0.30f, 0.16f, 1f);
            colors.highlightedColor = new Color(0.51f, 0.39f, 0.20f, 1f);
            colors.pressedColor = new Color(0.29f, 0.22f, 0.12f, 1f);
            colors.disabledColor = new Color(0.28f, 0.28f, 0.28f, 0.9f);
            button.colors = colors;

            Text labelText = CreateText("Label", buttonObject.transform, new Vector2(20f, -10f), new Vector2(140f, 24f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            labelText.text = label;
            labelText.color = new Color(0.99f, 0.96f, 0.89f, 1f);
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
