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
        public Outline selectionOutline;

        private CardData cardData;
        private Action<CardData> onClicked;

        public CardData CardData => cardData;

        public void Setup(CardData data, Action<CardData> clickAction)
        {
            cardData = data;
            onClicked = clickAction;

            cardNameText.text = data.cardName;
            ownerText.text = data.ownerHeroId == HeroId.Neutral ? "Owner: Neutral" : "Owner: " + data.ownerHeroId;
            typeText.text = "Type: " + data.cardType;
            costText.text = data.cost.ToString();
            descriptionText.text = data.description;

            ConfigureReadableText();

            artImage.sprite = data.cardArt;
            artImage.enabled = true;
            artImage.color = data.cardArt != null ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1f);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            SetSelected(false);
        }

        public void SetDisabled(bool isDisabled, string disabledReason)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isDisabled ? 0.55f : 1f;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isDisabled ? new Color(0.55f, 0.55f, 0.55f, 1f) : Color.white;
            }

            if (disabledReasonText != null)
            {
                disabledReasonText.text = isDisabled ? disabledReason : string.Empty;
            }

            if (button != null)
            {
                button.interactable = !isDisabled;
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionOutline != null)
            {
                selectionOutline.enabled = isSelected;
            }
        }

        private void ConfigureReadableText()
        {
            if (cardNameText != null)
            {
                cardNameText.resizeTextForBestFit = true;
                cardNameText.resizeTextMinSize = 14;
                cardNameText.resizeTextMaxSize = 22;
                cardNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
                cardNameText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (descriptionText != null)
            {
                descriptionText.resizeTextForBestFit = true;
                descriptionText.resizeTextMinSize = 12;
                descriptionText.resizeTextMaxSize = 15;
                descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            }
        }

        private void HandleClick()
        {
            onClicked?.Invoke(cardData);
        }
    }
}
