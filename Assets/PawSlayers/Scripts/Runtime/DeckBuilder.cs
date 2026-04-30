using System.Collections.Generic;
using System.Linq;

namespace PawSlayers
{
    public static class DeckBuilder
    {
        public static List<RuntimeCardState> BuildStartingDeck(IEnumerable<CardData> allCards, List<HeroId> selectedHeroes, HeroProgressionManager progressionManager = null)
        {
            return allCards
                .Where(card => card != null && card.isStarterCard)
                .Where(card => card.ownerHeroId == HeroId.Neutral || selectedHeroes.Contains(card.ownerHeroId))
                .Where(card => progressionManager == null || progressionManager.IsCardUnlocked(card))
                .Select(RuntimeCardState.Create)
                .ToList();
        }
    }
}
