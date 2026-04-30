using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PawSlayers
{
    [CreateAssetMenu(menuName = "Paw Slayers/Card Database", fileName = "CardDatabase")]
    public class CardDatabase : ScriptableObject
    {
        public List<CardData> cards = new List<CardData>();

        public List<CardData> GetEligibleCards(List<HeroId> selectedHeroes)
        {
            return cards.Where(card =>
                card != null &&
                (card.ownerHeroId == HeroId.Neutral || selectedHeroes.Contains(card.ownerHeroId))).ToList();
        }
    }
}
