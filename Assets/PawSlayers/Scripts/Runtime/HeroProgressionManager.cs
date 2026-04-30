using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PawSlayers
{
    public class HeroProgressionManager : MonoBehaviour
    {
        private const string SaveKey = "PawSlayers_HeroProgression";
        private static readonly int[] LevelThresholds = { 0, 25, 60, 110, 180 };
        private static readonly HashSet<string> StarterCardIds = new HashSet<string>
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

        private readonly Dictionary<HeroId, string> level3UnlockCardIds = new Dictionary<HeroId, string>
        {
            { HeroId.Capybara, "rally_cut" },
            { HeroId.Koala, "silent_pounce" },
            { HeroId.Sloth, "deep_rest" },
            { HeroId.Panda, "guardian_roar" },
            { HeroId.Kangaroo, "momentum_kick" }
        };

        private readonly Dictionary<HeroId, string> level5PerkIds = new Dictionary<HeroId, string>
        {
            { HeroId.Capybara, "capybara_level5_perk" },
            { HeroId.Koala, "koala_level5_perk" },
            { HeroId.Sloth, "sloth_level5_perk" },
            { HeroId.Panda, "panda_level5_perk" },
            { HeroId.Kangaroo, "kangaroo_level5_perk" }
        };

        private HeroDatabase heroDatabase;
        private CardDatabase cardDatabase;
        private HeroProgressionSaveData saveData = new HeroProgressionSaveData();

        public void Initialize(HeroDatabase database, CardDatabase cards)
        {
            heroDatabase = database;
            cardDatabase = cards;
            LoadProgression();
            EnsureRosterEntries();
            SaveProgression();
        }

        public void LoadProgression()
        {
            string json = PlayerPrefs.GetString(SaveKey, string.Empty);
            saveData = string.IsNullOrWhiteSpace(json)
                ? new HeroProgressionSaveData()
                : JsonUtility.FromJson<HeroProgressionSaveData>(json);

            if (saveData == null)
            {
                saveData = new HeroProgressionSaveData();
            }

            if (saveData.heroes == null)
            {
                saveData.heroes = new List<HeroProgressionState>();
            }
        }

        public void SaveProgression()
        {
            if (saveData == null)
            {
                saveData = new HeroProgressionSaveData();
            }

            string json = JsonUtility.ToJson(saveData);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public void ResetProgressionForTesting()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            saveData = new HeroProgressionSaveData();
            EnsureRosterEntries();
            SaveProgression();
            Debug.Log("Hero progression reset.");
        }

        public void UnlockAllHeroes()
        {
            EnsureRosterEntries();
            foreach (HeroProgressionState hero in saveData.heroes)
            {
                hero.isUnlocked = true;
            }

            SaveProgression();
            Debug.Log("All heroes unlocked.");
        }

        public void PrintProgression()
        {
            EnsureRosterEntries();
            foreach (HeroProgressionState hero in saveData.heroes.OrderBy(hero => hero.heroId))
            {
                Debug.Log($"{hero.heroName}: Level {hero.level}, XP {hero.xp}, Unlocked {hero.isUnlocked}, Cards [{string.Join(", ", hero.unlockedCardIds)}], Perks [{string.Join(", ", hero.unlockedPerkIds)}]");
            }
        }

        public HeroProgressionState GetProgress(HeroId heroId)
        {
            EnsureRosterEntries();
            return saveData.heroes.FirstOrDefault(hero => hero.heroId == heroId);
        }

        public bool IsHeroUnlocked(HeroId heroId)
        {
            HeroProgressionState hero = GetProgress(heroId);
            return hero != null && hero.isUnlocked;
        }

        public bool IsCardUnlocked(CardData card)
        {
            if (card == null)
            {
                return false;
            }

            if (card.ownerHeroId == HeroId.Neutral || card.isStarterCard || StarterCardIds.Contains(card.cardId))
            {
                return true;
            }

            HeroProgressionState hero = GetProgress(card.ownerHeroId);
            return hero != null && hero.unlockedCardIds.Contains(card.cardId);
        }

        public List<CardData> FilterUnlockedCards(IEnumerable<CardData> cards, List<HeroId> selectedHeroes)
        {
            return cards
                .Where(card => card != null)
                .Where(card => card.ownerHeroId == HeroId.Neutral || selectedHeroes.Contains(card.ownerHeroId))
                .Where(IsCardUnlocked)
                .ToList();
        }

        public int GetPermanentMaxHpBonus(HeroId heroId)
        {
            HeroProgressionState hero = GetProgress(heroId);
            return hero != null && hero.level >= 2 ? 5 : 0;
        }

        public bool HasLevelFourStarterUpgrade(HeroId heroId)
        {
            HeroProgressionState hero = GetProgress(heroId);
            return hero != null && hero.level >= 4;
        }

        public bool HasLevelFivePerk(HeroId heroId)
        {
            HeroProgressionState hero = GetProgress(heroId);
            return hero != null && hero.level >= 5;
        }

        public string GetProgressLabel(HeroId heroId)
        {
            HeroProgressionState hero = GetProgress(heroId);
            if (hero == null)
            {
                return "Level 1 - 0/25 XP";
            }

            if (hero.level >= LevelThresholds.Length)
            {
                return $"Level {hero.level} - Max";
            }

            int nextThreshold = LevelThresholds[Mathf.Clamp(hero.level, 0, LevelThresholds.Length - 1)];
            return $"Level {hero.level} - {hero.xp}/{nextThreshold} XP";
        }

        public List<HeroXpGainResult> AddXpToHeroes(IEnumerable<HeroId> heroIds, int xpAmount)
        {
            EnsureRosterEntries();
            List<HeroXpGainResult> results = new List<HeroXpGainResult>();
            int clampedXp = Mathf.Max(0, xpAmount);

            foreach (HeroId heroId in heroIds.Distinct())
            {
                if (heroId == HeroId.Neutral)
                {
                    continue;
                }

                HeroProgressionState hero = GetProgress(heroId);
                if (hero == null)
                {
                    continue;
                }

                HeroXpGainResult result = new HeroXpGainResult
                {
                    heroId = hero.heroId,
                    heroName = hero.heroName,
                    xpGained = clampedXp,
                    oldLevel = hero.level
                };

                hero.xp = Mathf.Min(LevelThresholds[LevelThresholds.Length - 1], hero.xp + clampedXp);
                hero.level = GetLevelForXp(hero.xp);
                result.newLevel = hero.level;

                if (result.oldLevel < 3 && result.newLevel >= 3)
                {
                    string unlockedCardId = UnlockLevelThreeCard(hero.heroId);
                    if (!string.IsNullOrWhiteSpace(unlockedCardId))
                    {
                        result.unlockedCardIds.Add(unlockedCardId);
                    }
                }

                if (result.oldLevel < 5 && result.newLevel >= 5)
                {
                    string perkId = UnlockLevelFivePerk(hero.heroId);
                    if (!string.IsNullOrWhiteSpace(perkId))
                    {
                        result.unlockedPerkIds.Add(perkId);
                    }
                }

                results.Add(result);

                if (result.newLevel > result.oldLevel)
                {
                    Debug.Log($"{hero.heroName} reached Level {result.newLevel}!");
                }
            }

            SaveProgression();
            return results;
        }

        public string GetCardName(string cardId)
        {
            if (cardDatabase == null || string.IsNullOrWhiteSpace(cardId))
            {
                return cardId;
            }

            CardData card = cardDatabase.cards.FirstOrDefault(entry => entry != null && entry.cardId == cardId);
            return card != null ? card.cardName : cardId;
        }

        private void EnsureRosterEntries()
        {
            if (heroDatabase == null || heroDatabase.heroes == null)
            {
                return;
            }

            foreach (HeroData heroData in heroDatabase.heroes)
            {
                if (heroData == null || heroData.heroId == HeroId.Neutral)
                {
                    continue;
                }

                HeroProgressionState existing = saveData.heroes.FirstOrDefault(hero => hero.heroId == heroData.heroId);
                if (existing == null)
                {
                    existing = new HeroProgressionState
                    {
                        heroId = heroData.heroId,
                        heroName = heroData.heroName,
                        isUnlocked = true,
                        level = 1,
                        xp = 0
                    };
                    saveData.heroes.Add(existing);
                }
                else
                {
                    existing.heroName = heroData.heroName;
                }

                existing.level = GetLevelForXp(existing.xp);
            }
        }

        private int GetLevelForXp(int xp)
        {
            int level = 1;
            for (int index = 0; index < LevelThresholds.Length; index++)
            {
                if (xp >= LevelThresholds[index])
                {
                    level = index + 1;
                }
            }

            return Mathf.Clamp(level, 1, 5);
        }

        private string UnlockLevelThreeCard(HeroId heroId)
        {
            HeroProgressionState hero = GetProgress(heroId);
            if (hero == null || !level3UnlockCardIds.TryGetValue(heroId, out string cardId))
            {
                return string.Empty;
            }

            if (!hero.unlockedCardIds.Contains(cardId))
            {
                hero.unlockedCardIds.Add(cardId);
                Debug.Log($"{hero.heroName} unlocked {GetCardName(cardId)}.");
            }

            return cardId;
        }

        private string UnlockLevelFivePerk(HeroId heroId)
        {
            HeroProgressionState hero = GetProgress(heroId);
            if (hero == null || !level5PerkIds.TryGetValue(heroId, out string perkId))
            {
                return string.Empty;
            }

            if (!hero.unlockedPerkIds.Contains(perkId))
            {
                hero.unlockedPerkIds.Add(perkId);
                Debug.Log($"{hero.heroName} unlocked a passive perk.");
            }

            return perkId;
        }
    }
}
