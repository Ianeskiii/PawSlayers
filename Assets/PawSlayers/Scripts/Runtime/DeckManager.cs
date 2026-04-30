using System.Collections.Generic;
using UnityEngine;

namespace PawSlayers
{
    public class DeckManager : MonoBehaviour
    {
        [SerializeField] private List<RuntimeCardState> runDeck = new List<RuntimeCardState>();
        [SerializeField] private List<RuntimeCardState> discardPile = new List<RuntimeCardState>();
        [SerializeField] private List<RuntimeCardState> drawPile = new List<RuntimeCardState>();
        [SerializeField] private List<RuntimeCardState> hand = new List<RuntimeCardState>();

        public IReadOnlyList<RuntimeCardState> RunDeck => runDeck;
        public IReadOnlyList<RuntimeCardState> DiscardPile => discardPile;
        public IReadOnlyList<RuntimeCardState> DrawPile => drawPile;
        public IReadOnlyList<RuntimeCardState> Hand => hand;

        public void SetStartingDeck(List<RuntimeCardState> cards)
        {
            runDeck = new List<RuntimeCardState>(cards);
            discardPile.Clear();
            hand.Clear();
            drawPile = new List<RuntimeCardState>(cards);
            Shuffle(drawPile);
        }

        public List<RuntimeCardState> DrawCards(int amount)
        {
            List<RuntimeCardState> drawn = new List<RuntimeCardState>();
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

                RuntimeCardState card = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(card);
                drawn.Add(card);
            }

            return drawn;
        }

        public void DiscardCard(RuntimeCardState card)
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
                RuntimeCardState card = hand[0];
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

        public void AddCardToDeck(RuntimeCardState card)
        {
            if (card == null)
            {
                return;
            }

            runDeck.Add(card);
            discardPile.Add(card);
        }

        public void AddCardToDiscard(RuntimeCardState card)
        {
            if (card == null)
            {
                return;
            }

            discardPile.Add(card);
        }

        public void RemoveCardsWhere(System.Predicate<RuntimeCardState> match)
        {
            if (match == null)
            {
                return;
            }

            runDeck.RemoveAll(match);
            discardPile.RemoveAll(match);
            drawPile.RemoveAll(match);
            hand.RemoveAll(match);
        }

        private void Shuffle(List<RuntimeCardState> cards)
        {
            for (int index = 0; index < cards.Count; index++)
            {
                int swapIndex = Random.Range(index, cards.Count);
                (cards[index], cards[swapIndex]) = (cards[swapIndex], cards[index]);
            }
        }
    }
}
