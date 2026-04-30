using System.Collections.Generic;
using System.Linq;

namespace PawSlayers
{
    public static class CardRules
    {
        public static bool CanPlayCard(RuntimeCardState card, List<HeroId> selectedHeroes, List<RuntimeHeroState> activeHeroes, out string reason)
        {
            reason = string.Empty;

            if (card == null || card.baseCard == null)
            {
                reason = "Missing card";
                return false;
            }

            if (card.CardType == CardType.Status)
            {
                reason = "Unplayable";
                return false;
            }

            if (card.OwnerHeroId == HeroId.Neutral)
            {
                return true;
            }

            if (!selectedHeroes.Contains(card.OwnerHeroId))
            {
                reason = "Hero not selected";
                return false;
            }

            RuntimeHeroState owner = activeHeroes.FirstOrDefault(hero => hero.heroData != null && hero.heroData.heroId == card.OwnerHeroId);
            if (owner == null)
            {
                reason = "Hero missing";
                return false;
            }

            if (!owner.IsAlive)
            {
                reason = owner.heroData.heroName + " is down";
                return false;
            }

            if (owner.statuses.stun > 0)
            {
                reason = owner.heroData.heroName + " is stunned";
                return false;
            }

            if (owner.statuses.silence > 0 && card.CardType == CardType.Skill)
            {
                reason = owner.heroData.heroName + " is silenced";
                return false;
            }

            return true;
        }
    }
}
