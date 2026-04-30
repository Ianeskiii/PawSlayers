using System.Collections.Generic;
using UnityEngine;

namespace PawSlayers
{
    public class DeckManager : MonoBehaviour
    {
        [SerializeField] private List<CardData> runDeck = new List<CardData>();
        [SerializeField] private List<CardData> discardPile = new List<CardData>();
        [SerializeField] private List<CardData> drawPile = new List<CardData>();
        [SerializeField] private List<CardData> hand = new List<CardData>();

        public IReadOnlyList<CardData> RunDeck => runDeck;
        public IReadOnlyList<CardData> DiscardPile => discardPile;
        public IReadOnlyList<CardData> DrawPile => drawPile;
        public IReadOnlyList<CardData> Hand => hand;

        public void SetStartingDeck(List<CardData> cards)
        {
            runDeck = new List<CardData>(cards);
            discardPile.Clear();
            hand.Clear();
            drawPile = new List<CardData>(cards);
            Shuffle(drawPile);
        }

        public List<CardData> DrawCards(int amount)
        {
            List<CardData> drawn = new List<CardData>();
            for (int index = 0; index < amount; index++)
            {
                if (drawPile.Count == 0)
                {
                    ReshuffleDiscardIntoDraw();
                }

                if (drawPile.Count == 0)
                {
                    break;
                }

                CardData card = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(card);
                drawn.Add(card);
            }

            return drawn;
        }

        public void DiscardCard(CardData card)
        {
            if (card == null)
            {
                return;
            }

            if (hand.Remove(card))
            {
                discardPile.Add(card);
            }
        }

        public void DiscardHand()
        {
            while (hand.Count > 0)
            {
                CardData card = hand[0];
                hand.RemoveAt(0);
                discardPile.Add(card);
            }
        }

        public void ReshuffleDiscardIntoDraw()
        {
            if (discardPile.Count == 0)
            {
                return;
            }

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            Shuffle(drawPile);
        }

        public void AddCardToDeck(CardData card)
        {
            if (card == null)
            {
                return;
            }

            runDeck.Add(card);
            discardPile.Add(card);
        }

        private void Shuffle(List<CardData> cards)
        {
            for (int index = 0; index < cards.Count; index++)
            {
                int swapIndex = Random.Range(index, cards.Count);
                (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
            }
        }
    }
}
