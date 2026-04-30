using System;
using System.Collections.Generic;

namespace PawSlayers
{
    [Serializable]
    public class TreasureRewardChoice
    {
        public TreasureRewardType rewardType;
        public string title;
        public string description;
        public int goldAmount;
        public List<RelicData> relicChoices = new List<RelicData>();
    }
}
