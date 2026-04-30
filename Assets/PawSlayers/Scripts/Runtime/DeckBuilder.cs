using System.Collections.Generic;
using System.Linq;

namespace PawSlayers
{
    public static class DeckBuilder
    {
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

        public static List<RuntimeCardState> BuildStartingDeck(IEnumerable<CardData> allCards, List<HeroId> selectedHeroes, HeroProgressionManager progressionManager = null)
        {
            return allCards
                .Where(card => card != null && (card.isStarterCard || StarterCardIds.Contains(card.cardId)))
                .Where(card => card.ownerHeroId == HeroId.Neutral || selectedHeroes.Contains(card.ownerHeroId))
                .Where(card => progressionManager == null || progressionManager.IsCardUnlocked(card))
                .Select(RuntimeCardState.Create)
                .ToList();
        }
    }
}
