using System.Collections.Generic;
using System.Linq;

namespace PawSlayers
{
    public static class DeckBuilder
    {
        public static List<CardData> BuildStartingDeck(IEnumerable<CardData> allCards, List<HeroId> selectedHeroes)
        {
            return allCards
                .Where(card => card != null && (card.ownerHeroId == HeroId.Neutral || selectedHeroes.Contains(card.ownerHeroId)))
                .ToList();
        }
    }
}
