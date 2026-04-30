using System;
using System.Collections.Generic;

namespace PawSlayers
{
    [Serializable]
    public class HeroProgressionState
    {
        public HeroId heroId;
        public string heroName;
        public int level = 1;
        public int xp;
        public bool isUnlocked = true;
        public List<string> unlockedCardIds = new List<string>();
        public List<string> unlockedPerkIds = new List<string>();
    }

    [Serializable]
    public class HeroProgressionSaveData
    {
        public List<HeroProgressionState> heroes = new List<HeroProgressionState>();
    }

    [Serializable]
    public class HeroXpGainResult
    {
        public HeroId heroId;
        public string heroName;
        public int xpGained;
        public int oldLevel;
        public int newLevel;
        public List<string> unlockedCardIds = new List<string>();
        public List<string> unlockedPerkIds = new List<string>();
    }

    [Serializable]
    public class RunHeroProgressSummary
    {
        public HeroId heroId;
        public string heroName;
        public int totalXpGained;
        public int startLevel;
        public int endLevel;
        public List<string> unlockedMessages = new List<string>();
    }
}
