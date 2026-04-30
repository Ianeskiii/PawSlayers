using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PawSlayers
{
    public class RunManager : MonoBehaviour
    {
        public static RunManager Instance { get; private set; }

        [Header("Data")]
        public HeroDatabase heroDatabase;
        public CardDatabase cardDatabase;

        [Header("Scenes")]
        public string heroSelectionSceneName = "HeroSelection";
        public string battleSceneName = "Battle";

        [Header("Run State")]
        [SerializeField] private List<HeroId> selectedHeroIds = new List<HeroId>();
        [SerializeField] private List<RuntimeHeroState> activeHeroesRuntime = new List<RuntimeHeroState>();

        private DeckManager deckManager;

        public List<HeroId> SelectedHeroIds => selectedHeroIds;
        public List<RuntimeHeroState> ActiveHeroesRuntime => activeHeroesRuntime;
        public List<CardData> RunDeck => deckManager == null ? new List<CardData>() : deckManager.RunDeck.ToList();
        public List<CardData> DiscardPile => deckManager == null ? new List<CardData>() : deckManager.DiscardPile.ToList();
        public List<CardData> DrawPile => deckManager == null ? new List<CardData>() : deckManager.DrawPile.ToList();
        public List<CardData> Hand => deckManager == null ? new List<CardData>() : deckManager.Hand.ToList();

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
            SetSelectedHeroes(selectedHeroes);
            BuildAndShuffleRunDeck();
            SceneManager.LoadScene(battleSceneName);
        }

        public void ResetRunAndReturnToSelection()
        {
            selectedHeroIds.Clear();
            activeHeroesRuntime.Clear();
            deckManager.SetStartingDeck(new List<CardData>());
            SceneManager.LoadScene(heroSelectionSceneName);
        }

        public RuntimeHeroState GetHeroState(HeroId heroId)
        {
            return activeHeroesRuntime.FirstOrDefault(hero => hero.heroData != null && hero.heroData.heroId == heroId);
        }

        public bool CanPlayCard(CardData card, out string reason)
        {
            return CardRules.CanPlayCard(card, selectedHeroIds, activeHeroesRuntime, out reason);
        }

        public List<CardData> DrawCards(int amount)
        {
            return deckManager.DrawCards(amount);
        }

        public void DiscardCard(CardData card)
        {
            deckManager.DiscardCard(card);
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
            if (card == null || deckManager == null)
            {
                return;
            }

            deckManager.AddCardToDeck(card);
        }

        public void HealAllHeroes()
        {
            foreach (RuntimeHeroState hero in activeHeroesRuntime)
            {
                hero?.Heal(999);
                if (hero != null)
                {
                    hero.block = 0;
                }
            }
        }

        public void EnsureDirectBattleTestState()
        {
            EnsurePrototypeData();

            if (selectedHeroIds.Count == 3 && activeHeroesRuntime.Count == 3)
            {
                return;
            }

            SetSelectedHeroes(new List<HeroId>
            {
                HeroId.Capybara,
                HeroId.Sloth,
                HeroId.Panda
            });
        }

        public void SetSelectedHeroes(List<HeroId> selectedHeroes)
        {
            EnsurePrototypeData();
            selectedHeroIds = new List<HeroId>(selectedHeroes);
            activeHeroesRuntime = BuildRuntimeHeroes(selectedHeroIds);
        }

        public void BuildAndShuffleRunDeck()
        {
            EnsurePrototypeData();
            List<CardData> startingDeck = DeckBuilder.BuildStartingDeck(cardDatabase.cards, selectedHeroIds);
            deckManager.SetStartingDeck(startingDeck);
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

            if (cardDatabase.cards.Count == 0)
            {
                cardDatabase.cards = CreatePrototypeCards();
            }
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
                CreateHero(HeroId.Capybara, "Capybara", HeroClass.Swordsman, 42, "Front-line swordsman with steady offense and defense."),
                CreateHero(HeroId.Koala, "Koala", HeroClass.Thief, 34, "Fast thief who chips enemies and sets up tricky turns."),
                CreateHero(HeroId.Sloth, "Sloth", HeroClass.Healer, 36, "Slow but reliable support healer."),
                CreateHero(HeroId.Panda, "Panda", HeroClass.Tank, 50, "Heavy tank who stacks block and protects the team."),
                CreateHero(HeroId.Kangaroo, "Kangaroo", HeroClass.Fighter, 40, "Aggressive fighter with strong combo hits.")
            };
        }

        private List<CardData> CreatePrototypeCards()
        {
            return new List<CardData>
            {
                CreateCard("swift_slash", "Swift Slash", "Deal 8 damage.", HeroId.Capybara, CardType.Attack, TargetType.Enemy, 1, damage: 8),
                CreateCard("guard_stance", "Guard Stance", "Gain 8 block.", HeroId.Capybara, CardType.Skill, TargetType.Self, 1, block: 8),
                CreateCard("shadow_strike", "Shadow Strike", "Deal 7 damage. Apply Weak later.", HeroId.Koala, CardType.Attack, TargetType.Enemy, 1, damage: 7, weakAmount: 1),
                CreateCard("smoke_step", "Smoke Step", "Gain 6 block and draw 1 card.", HeroId.Koala, CardType.Skill, TargetType.Self, 1, block: 6, drawAmount: 1),
                CreateCard("staff_tap", "Staff Tap", "Deal 5 damage.", HeroId.Sloth, CardType.Attack, TargetType.Enemy, 1, damage: 5),
                CreateCard("soothing_light", "Soothing Light", "Heal 8 HP.", HeroId.Sloth, CardType.Skill, TargetType.Ally, 1, heal: 8),
                CreateCard("shield_bash", "Shield Bash", "Deal 6 damage and gain 4 block.", HeroId.Panda, CardType.Attack, TargetType.Enemy, 1, damage: 6, block: 4),
                CreateCard("barkskin_guard", "Barkskin Guard", "Gain 16 block. Taunt later.", HeroId.Panda, CardType.Skill, TargetType.Self, 2, block: 16, taunt: true),
                CreateCard("power_combo", "Power Combo", "Deal 12 damage.", HeroId.Kangaroo, CardType.Attack, TargetType.Enemy, 2, damage: 12),
                CreateCard("battle_focus", "Battle Focus", "Gain 2 Strength later.", HeroId.Kangaroo, CardType.Skill, TargetType.Self, 1, strengthAmount: 2),
                CreateCard("snack_time", "Snack Time", "Draw 1 card.", HeroId.Neutral, CardType.Skill, TargetType.None, 1, drawAmount: 1),
                CreateCard("quick_guard", "Quick Guard", "Gain 5 block.", HeroId.Neutral, CardType.Skill, TargetType.Self, 1, block: 5)
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
            bool taunt = false)
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
            card.taunt = taunt;
            return card;
        }
    }
}
