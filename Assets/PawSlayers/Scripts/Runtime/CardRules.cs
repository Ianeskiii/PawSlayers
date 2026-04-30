using System.Collections.Generic;
using System.Linq;

namespace PawSlayers
{
    public static class CardRules
    {
        public static bool CanPlayCard(CardData card, List<HeroId> selectedHeroes, List<RuntimeHeroState> activeHeroes, out string reason)
        {
            reason = string.Empty;

            if (card == null)
            {
                reason = "Missing card";
                return false;
            }

            if (card.ownerHeroId == HeroId.Neutral)
            {
                return true;
            }

            if (!selectedHeroes.Contains(card.ownerHeroId))
            {
                reason = "Hero not selected";
                return false;
            }

            RuntimeHeroState owner = activeHeroes.FirstOrDefault(hero => hero.heroData != null && hero.heroData.heroId == card.ownerHeroId);
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

            return true;
        }
    }
}
