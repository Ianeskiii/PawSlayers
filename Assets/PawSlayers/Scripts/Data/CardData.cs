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
        public bool taunt;
        public Sprite cardArt;
    }
}
