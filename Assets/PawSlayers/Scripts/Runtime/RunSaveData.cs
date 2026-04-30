using System;
using System.Collections.Generic;

namespace PawSlayers
{
    [Serializable]
    public class RunSaveData
    {
        public bool hasActiveRun;
        public bool runWon;
        public bool runLost;
        public List<HeroId> selectedHeroIds = new List<HeroId>();
        public List<HeroRuntimeSaveData> heroes = new List<HeroRuntimeSaveData>();
        public List<CardInstanceSaveData> currentRunDeck = new List<CardInstanceSaveData>();
        public List<RelicId> ownedRelics = new List<RelicId>();
        public int gold;
        public int currentBattleIndex;
        public int totalNormalBattlesBeforeBoss;
        public bool bossAvailable;
        public bool bossStarted;
        public bool supportNodeUsedThisStage;
        public bool pendingBetweenBattleRecovery;
        public string currentSceneName;
        public string currentNodeType;
        public int mapStep;
        public long savedAtUnixTime;
    }

    [Serializable]
    public class HeroRuntimeSaveData
    {
        public HeroId heroId;
        public int currentHp;
        public int maxHp;
        public int block;
        public bool isDown;
        public StatusEffectsSaveData statuses = new StatusEffectsSaveData();
    }

    [Serializable]
    public class CardInstanceSaveData
    {
        public string cardId;
        public HeroId ownerHeroId;
        public bool isUpgraded;
        public bool isTemporary;
    }

    [Serializable]
    public class StatusEffectsSaveData
    {
        public int weak;
        public int vulnerable;
        public int strength;
        public int bleed;
        public int poison;
        public int taunt;
        public int stun;
        public int silence;
    }
}
