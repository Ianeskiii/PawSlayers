using System;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class CardView : MonoBehaviour
    {
        public Text cardNameText;
        public Text ownerText;
        public Text typeText;
        public Text costText;
        public Text descriptionText;
        public Text disabledReasonText;
        public Image artImage;
        public Image backgroundImage;
        public Button button;
        public CanvasGroup canvasGroup;

        private CardData cardData;
        private Action<CardData> onClicked;

        public CardData CardData => cardData;

        public void Setup(CardData data, Action<CardData> clickAction)
        {
            cardData = data;
            onClicked = clickAction;

            cardNameText.text = data.cardName;
            ownerText.text = data.ownerHeroId == HeroId.Neutral ? "Neutral" : data.ownerHeroId.ToString();
            typeText.text = data.cardType.ToString();
            costText.text = data.cost.ToString();
            descriptionText.text = data.description;
            artImage.sprite = data.cardArt;
            artImage.enabled = true;
            artImage.color = data.cardArt != null ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1f);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        public void SetPlayable(bool isPlayable, string disabledReason)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isPlayable ? 1f : 0.55f;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isPlayable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            }

            if (disabledReasonText != null)
            {
                disabledReasonText.text = isPlayable ? string.Empty : disabledReason;
            }

            if (button != null)
            {
                button.interactable = isPlayable;
            }
        }

        private void HandleClick()
        {
            onClicked?.Invoke(cardData);
        }
    }
}
