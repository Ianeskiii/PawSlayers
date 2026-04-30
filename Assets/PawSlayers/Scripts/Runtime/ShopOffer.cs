using System;

namespace PawSlayers
{
    [Serializable]
    public class ShopOffer
    {
        public ShopOfferType offerType;
        public string title;
        public string description;
        public int cost;
        public bool isPurchased;
        public CardData cardData;
        public RelicData relicData;
    }
}
