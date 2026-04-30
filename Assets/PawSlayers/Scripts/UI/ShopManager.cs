using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PawSlayers
{
    public class ShopManager : MonoBehaviour
    {
        public const int CardCost = 50;
        public const int RelicCost = 120;
        public const int RemoveCardCost = 75;
        public const int HealCost = 60;
        public const int UpgradeCardCost = 90;

        public RunManager runManager;
        public CardDatabase cardDatabase;

        public void Initialize(RunManager manager, CardDatabase database)
        {
            runManager = manager;
            cardDatabase = database;
        }

        public List<ShopOffer> GenerateOffers()
        {
            List<ShopOffer> offers = new List<ShopOffer>();

            List<CardData> cardChoices = GetEligibleShopCards(2);
            foreach (CardData card in cardChoices)
            {
                offers.Add(new ShopOffer
                {
                    offerType = ShopOfferType.Card,
                    title = "Buy Card: " + card.cardName,
                    description = "Add " + card.cardName + " to your deck.",
                    cost = CardCost,
                    cardData = card
                });
            }

            RelicData relic = GetShopRelic();
            offers.Add(new ShopOffer
            {
                offerType = ShopOfferType.Relic,
                title = relic != null ? "Buy Relic: " + relic.relicName : "Buy Relic",
                description = relic != null ? relic.description : "No relics available.",
                cost = RelicCost,
                relicData = relic,
                isPurchased = relic == null
            });

            offers.Add(new ShopOffer
            {
                offerType = ShopOfferType.RemoveCard,
                title = "Remove Card",
                description = "Remove 1 card from your deck.",
                cost = RemoveCardCost
            });

            offers.Add(new ShopOffer
            {
                offerType = ShopOfferType.Heal,
                title = "Heal Party",
                description = "Recover party HP.",
                cost = HealCost
            });

            bool hasUpgradeable = runManager != null && runManager.GetUpgradeableCards().Count > 0;
            offers.Add(new ShopOffer
            {
                offerType = ShopOfferType.UpgradeCard,
                title = "Upgrade Card",
                description = hasUpgradeable ? "Upgrade 1 card in your deck." : "No cards available to upgrade.",
                cost = UpgradeCardCost,
                isPurchased = !hasUpgradeable
            });

            return offers;
        }

        public List<CardData> GetEligibleShopCards(int count)
        {
            if (cardDatabase == null || runManager == null)
            {
                return new List<CardData>();
            }

            return cardDatabase.GetEligibleCards(runManager.SelectedHeroIds, runManager.ProgressionManager)
                .Where(card => card != null && card.cardType != CardType.Status)
                .OrderBy(_ => Random.value)
                .Take(count)
                .ToList();
        }

        public RelicData GetShopRelic()
        {
            if (runManager == null)
            {
                return null;
            }

            return runManager.GetAvailableRelicChoices(1).FirstOrDefault();
        }

        public bool TryBuyCard(ShopOffer offer, out string message)
        {
            if (offer == null || offer.cardData == null)
            {
                message = "No card available.";
                return false;
            }

            if (!runManager.TrySpendGold(offer.cost))
            {
                message = "Not enough gold.";
                return false;
            }

            runManager.AddRewardCard(offer.cardData);
            offer.isPurchased = true;
            message = "Bought " + offer.cardData.cardName + ".";
            Debug.Log("Bought card: " + offer.cardData.cardName);
            Debug.Log("Current deck count: " + runManager.CurrentRunDeck.Count);
            return true;
        }

        public bool TryBuyRelic(ShopOffer offer, out string message)
        {
            if (offer == null || offer.relicData == null)
            {
                message = "No relics available.";
                return false;
            }

            if (!runManager.TrySpendGold(offer.cost))
            {
                message = "Not enough gold.";
                return false;
            }

            bool gained = runManager.GainRelic(offer.relicData.relicId);
            if (!gained)
            {
                runManager.AddGold(offer.cost);
                message = "Relic already owned.";
                return false;
            }

            offer.isPurchased = true;
            message = "Bought relic: " + offer.relicData.relicName + ".";
            Debug.Log("Bought relic: " + offer.relicData.relicName);
            return true;
        }

        public bool TryBuyHeal(ShopOffer offer, out string message)
        {
            if (offer == null)
            {
                message = "Heal unavailable.";
                return false;
            }

            if (!runManager.TrySpendGold(offer.cost))
            {
                message = "Not enough gold.";
                return false;
            }

            runManager.ApplyShopHealReward();
            offer.isPurchased = true;
            message = "Party healed.";
            Debug.Log("Healed party.");
            return true;
        }

        public bool TryBuyUpgrade(ShopOffer offer, RuntimeCardState card, out string message)
        {
            if (offer == null || card == null)
            {
                message = "No cards available to upgrade.";
                return false;
            }

            if (!runManager.TrySpendGold(offer.cost))
            {
                message = "Not enough gold.";
                return false;
            }

            bool upgraded = runManager.UpgradeCard(card);
            if (!upgraded)
            {
                runManager.AddGold(offer.cost);
                message = "No cards available to upgrade.";
                return false;
            }

            offer.isPurchased = true;
            message = "Upgraded " + card.baseCard.cardName + " to " + card.DisplayName + ".";
            Debug.Log("Upgraded card: " + card.DisplayName);
            return true;
        }

        public bool TryBuyRemoveCard(ShopOffer offer, RuntimeCardState card, out string message)
        {
            if (offer == null || card == null)
            {
                message = "No card selected.";
                return false;
            }

            if (runManager.CurrentRunDeck.Count <= 5)
            {
                message = "Deck is too small to remove more cards.";
                return false;
            }

            if (!runManager.TrySpendGold(offer.cost))
            {
                message = "Not enough gold.";
                return false;
            }

            bool removed = runManager.RemoveCardFromRunDeck(card);
            if (!removed)
            {
                runManager.AddGold(offer.cost);
                message = "Deck is too small to remove more cards.";
                return false;
            }

            offer.isPurchased = true;
            message = "Removed " + card.DisplayName + ".";
            Debug.Log("Removed card: " + card.DisplayName);
            return true;
        }
    }
}
