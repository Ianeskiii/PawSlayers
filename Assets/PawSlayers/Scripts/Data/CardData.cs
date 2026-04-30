using System.Collections.Generic;
using UnityEngine;

namespace PawSlayers
{
    [CreateAssetMenu(menuName = "Paw Slayers/Card Data", fileName = "CardData")]
    public class CardData : ScriptableObject
    {
        public string cardId;
        public string cardName;
        [TextArea(2, 5)]
        public string description;
        public bool isStarterCard = true;
        public HeroId ownerHeroId;
        public CardType cardType;
        public TargetType targetType;
        public int cost;
        public int damage;
        public int block;
        public int heal;
        public int drawAmount;
        public int strengthAmount;
        public int weakAmount;
        public int vulnerableAmount;
        public int bleedAmount;
        public int poisonAmount;
        public int tauntAmount;
        public int stunAmount;
        public int silenceAmount;
        public int upgradedDamage;
        public int upgradedBlock;
        public int upgradedHeal;
        public int upgradedDrawAmount;
        public int upgradedStrengthAmount;
        public int upgradedWeakAmount;
        public int upgradedVulnerableAmount;
        public int upgradedBleedAmount;
        public int upgradedPoisonAmount;
        public int upgradedTauntAmount;
        public int upgradedStunAmount;
        public int upgradedSilenceAmount;
        public Sprite cardArt;

        public string GetDisplayName(bool isUpgraded)
        {
            return isUpgraded ? cardName + "+" : cardName;
        }

        public int GetDamage(bool isUpgraded)
        {
            return isUpgraded && upgradedDamage > 0 ? upgradedDamage : damage;
        }

        public int GetBlock(bool isUpgraded)
        {
            return isUpgraded && upgradedBlock > 0 ? upgradedBlock : block;
        }

        public int GetHeal(bool isUpgraded)
        {
            return isUpgraded && upgradedHeal > 0 ? upgradedHeal : heal;
        }

        public int GetDrawAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedDrawAmount > 0 ? upgradedDrawAmount : drawAmount;
        }

        public int GetStrengthAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedStrengthAmount > 0 ? upgradedStrengthAmount : strengthAmount;
        }

        public int GetWeakAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedWeakAmount > 0 ? upgradedWeakAmount : weakAmount;
        }

        public bool HasUpgrade()
        {
            return
                upgradedDamage > damage ||
                upgradedBlock > block ||
                upgradedHeal > heal ||
                upgradedDrawAmount > drawAmount ||
                upgradedStrengthAmount > strengthAmount ||
                upgradedWeakAmount > weakAmount ||
                upgradedVulnerableAmount > vulnerableAmount ||
                upgradedBleedAmount > bleedAmount ||
                upgradedPoisonAmount > poisonAmount ||
                upgradedTauntAmount > tauntAmount ||
                upgradedStunAmount > stunAmount ||
                upgradedSilenceAmount > silenceAmount;
        }

        public int GetVulnerableAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedVulnerableAmount > 0 ? upgradedVulnerableAmount : vulnerableAmount;
        }

        public int GetBleedAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedBleedAmount > 0 ? upgradedBleedAmount : bleedAmount;
        }

        public int GetPoisonAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedPoisonAmount > 0 ? upgradedPoisonAmount : poisonAmount;
        }

        public int GetTauntAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedTauntAmount > 0 ? upgradedTauntAmount : tauntAmount;
        }

        public int GetStunAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedStunAmount > 0 ? upgradedStunAmount : stunAmount;
        }

        public int GetSilenceAmount(bool isUpgraded)
        {
            return isUpgraded && upgradedSilenceAmount > 0 ? upgradedSilenceAmount : silenceAmount;
        }

        public string BuildDescription(bool isUpgraded)
        {
            List<string> parts = new List<string>();

            if (GetDamage(isUpgraded) > 0)
            {
                parts.Add($"Deal {GetDamage(isUpgraded)} damage.");
            }

            if (GetBlock(isUpgraded) > 0)
            {
                parts.Add($"Gain {GetBlock(isUpgraded)} block.");
            }

            if (GetHeal(isUpgraded) > 0)
            {
                parts.Add($"Heal {GetHeal(isUpgraded)} HP.");
            }

            if (GetDrawAmount(isUpgraded) > 0)
            {
                parts.Add($"Draw {GetDrawAmount(isUpgraded)} card{(GetDrawAmount(isUpgraded) == 1 ? string.Empty : "s")}.");
            }

            if (GetStrengthAmount(isUpgraded) > 0)
            {
                parts.Add($"Gain {GetStrengthAmount(isUpgraded)} Strength.");
            }

            if (GetWeakAmount(isUpgraded) > 0)
            {
                parts.Add($"Apply {GetWeakAmount(isUpgraded)} Weak.");
            }

            if (GetVulnerableAmount(isUpgraded) > 0)
            {
                parts.Add($"Apply {GetVulnerableAmount(isUpgraded)} Vulnerable.");
            }

            if (GetBleedAmount(isUpgraded) > 0)
            {
                parts.Add($"Apply {GetBleedAmount(isUpgraded)} Bleed.");
            }

            if (GetPoisonAmount(isUpgraded) > 0)
            {
                parts.Add($"Apply {GetPoisonAmount(isUpgraded)} Poison.");
            }

            if (GetTauntAmount(isUpgraded) > 0)
            {
                parts.Add($"Gain {GetTauntAmount(isUpgraded)} Taunt.");
            }

            if (GetStunAmount(isUpgraded) > 0)
            {
                parts.Add($"Apply {GetStunAmount(isUpgraded)} Stun.");
            }

            if (GetSilenceAmount(isUpgraded) > 0)
            {
                parts.Add($"Apply {GetSilenceAmount(isUpgraded)} Silence.");
            }

            if (parts.Count == 0)
            {
                return description;
            }

            string generatedDescription = string.Join(" ", parts);

            if (cardId == "rally_cut" || cardId == "guardian_roar" || cardId == "momentum_kick")
            {
                return description;
            }

            return generatedDescription;
        }
    }
}
