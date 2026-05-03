using System;
using UnityEngine;

namespace PawSlayers
{
    [Serializable]
    public class RuntimeCardState
    {
        public string runtimeId;
        public CardData baseCard;
        public bool isUpgraded;

        public string DisplayName => baseCard == null ? "Missing Card" : baseCard.GetDisplayName(isUpgraded);
        public string Description => baseCard == null ? string.Empty : baseCard.BuildDescription(isUpgraded);
        public HeroId OwnerHeroId => baseCard == null ? HeroId.Neutral : baseCard.ownerHeroId;
        public CardType CardType => baseCard == null ? CardType.Skill : baseCard.cardType;
        public CardAnimationType AnimationType => baseCard == null ? CardAnimationType.None : baseCard.animationType;
        public TargetType TargetType => baseCard == null ? TargetType.None : baseCard.targetType;
        public int Cost => baseCard == null ? 0 : baseCard.cost;
        public int Damage => baseCard == null ? 0 : baseCard.GetDamage(isUpgraded);
        public int Block => baseCard == null ? 0 : baseCard.GetBlock(isUpgraded);
        public int Heal => baseCard == null ? 0 : baseCard.GetHeal(isUpgraded);
        public int DrawAmount => baseCard == null ? 0 : baseCard.GetDrawAmount(isUpgraded);
        public int StrengthAmount => baseCard == null ? 0 : baseCard.GetStrengthAmount(isUpgraded);
        public int WeakAmount => baseCard == null ? 0 : baseCard.GetWeakAmount(isUpgraded);
        public int VulnerableAmount => baseCard == null ? 0 : baseCard.GetVulnerableAmount(isUpgraded);
        public int BleedAmount => baseCard == null ? 0 : baseCard.GetBleedAmount(isUpgraded);
        public int PoisonAmount => baseCard == null ? 0 : baseCard.GetPoisonAmount(isUpgraded);
        public int TauntAmount => baseCard == null ? 0 : baseCard.GetTauntAmount(isUpgraded);
        public int StunAmount => baseCard == null ? 0 : baseCard.GetStunAmount(isUpgraded);
        public int SilenceAmount => baseCard == null ? 0 : baseCard.GetSilenceAmount(isUpgraded);
        public Sprite CardArt => baseCard == null ? null : baseCard.cardArt;
        public bool CanUpgrade => baseCard != null && !isUpgraded && baseCard.HasUpgrade();

        public static RuntimeCardState Create(CardData baseCard)
        {
            return new RuntimeCardState
            {
                runtimeId = Guid.NewGuid().ToString("N"),
                baseCard = baseCard,
                isUpgraded = false
            };
        }

        public bool Upgrade()
        {
            if (!CanUpgrade)
            {
                return false;
            }

            isUpgraded = true;
            return true;
        }

        public string BuildUpgradePreview()
        {
            if (baseCard == null)
            {
                return string.Empty;
            }

            return $"Current: {baseCard.BuildDescription(isUpgraded)}\nUpgrade: {baseCard.BuildDescription(true)}";
        }
    }
}
