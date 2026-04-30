using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PawSlayers
{
    public class TreasureManager : MonoBehaviour
    {
        public RunManager runManager;
        public CardDatabase cardDatabase;

        public void Initialize(RunManager manager, CardDatabase database)
        {
            runManager = manager;
            cardDatabase = database;
        }

        public List<TreasureRewardChoice> GenerateRewardChoices(int count)
        {
            List<TreasureRewardChoice> choices = new List<TreasureRewardChoice>();
            List<TreasureRewardType> availableTypes = GetAvailableRewardTypes();

            foreach (TreasureRewardType rewardType in availableTypes.OrderBy(_ => Random.value).Take(count))
            {
                TreasureRewardChoice choice = BuildChoice(rewardType);
                if (choice != null)
                {
                    choices.Add(choice);
                }
            }

            while (choices.Count < count)
            {
                TreasureRewardChoice goldChoice = BuildChoice(TreasureRewardType.Gold);
                if (goldChoice == null)
                {
                    break;
                }

                choices.Add(goldChoice);
            }

            return choices;
        }

        public List<CardData> GetCardRewardChoices(int count)
        {
            if (cardDatabase == null || runManager == null)
            {
                return new List<CardData>();
            }

            return cardDatabase
                .GetEligibleCards(runManager.SelectedHeroIds)
                .Where(card => card != null && card.cardType != CardType.Status)
                .OrderBy(_ => Random.value)
                .Take(count)
                .ToList();
        }

        public List<RuntimeCardState> GetUpgradeChoices()
        {
            return runManager == null
                ? new List<RuntimeCardState>()
                : runManager.GetUpgradeableCards();
        }

        public void ApplyGoldReward(int amount)
        {
            if (runManager == null)
            {
                return;
            }

            runManager.MarkTreasureNodeResolved();
            runManager.AddGold(amount);
        }

        public bool ApplyRelicReward(RelicId relicId)
        {
            if (runManager == null)
            {
                return false;
            }

            runManager.MarkTreasureNodeResolved();
            return runManager.GainRelic(relicId);
        }

        public void ApplyCardReward(CardData card)
        {
            if (runManager == null || card == null)
            {
                return;
            }

            runManager.MarkTreasureNodeResolved();
            runManager.AddRewardCard(card);
        }

        public bool ApplyUpgradeReward(RuntimeCardState card)
        {
            if (runManager == null || card == null)
            {
                return false;
            }

            bool upgraded = runManager.UpgradeCard(card);
            if (upgraded)
            {
                runManager.MarkTreasureNodeResolved();
            }

            return upgraded;
        }

        public void ApplyHealReward()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.MarkTreasureNodeResolved();
            runManager.ApplyTreasureHealReward();
        }

        private List<TreasureRewardType> GetAvailableRewardTypes()
        {
            List<TreasureRewardType> types = new List<TreasureRewardType>
            {
                TreasureRewardType.Gold,
                TreasureRewardType.Heal
            };

            if (runManager != null && runManager.GetAvailableRelicChoices(1).Count > 0)
            {
                types.Add(TreasureRewardType.Relic);
            }

            if (GetCardRewardChoices(1).Count > 0)
            {
                types.Add(TreasureRewardType.Card);
            }

            if (GetUpgradeChoices().Count > 0)
            {
                types.Add(TreasureRewardType.Upgrade);
            }

            return types.Distinct().ToList();
        }

        private TreasureRewardChoice BuildChoice(TreasureRewardType rewardType)
        {
            TreasureRewardChoice choice = new TreasureRewardChoice
            {
                rewardType = rewardType
            };

            switch (rewardType)
            {
                case TreasureRewardType.Relic:
                    List<RelicData> relicChoices = runManager != null ? runManager.GetAvailableRelicChoices(3) : new List<RelicData>();
                    if (relicChoices.Count == 0)
                    {
                        return null;
                    }

                    choice.title = "Ancient Relic";
                    choice.description = "Choose 1 relic.";
                    choice.relicChoices = relicChoices;
                    break;
                case TreasureRewardType.Gold:
                    choice.goldAmount = Random.Range(40, 81);
                    choice.title = "Gold Cache";
                    choice.description = $"Gain {choice.goldAmount} gold.";
                    break;
                case TreasureRewardType.Card:
                    if (GetCardRewardChoices(1).Count == 0)
                    {
                        return null;
                    }

                    choice.title = "Card Stash";
                    choice.description = "Choose 1 card to add to your deck.";
                    break;
                case TreasureRewardType.Upgrade:
                    if (GetUpgradeChoices().Count == 0)
                    {
                        return null;
                    }

                    choice.title = "Upgrade Scroll";
                    choice.description = "Upgrade 1 card in your deck.";
                    break;
                case TreasureRewardType.Heal:
                    choice.title = "Healing Fruit";
                    choice.description = "Recover party HP.";
                    break;
                default:
                    return null;
            }

            return choice;
        }
    }
}
