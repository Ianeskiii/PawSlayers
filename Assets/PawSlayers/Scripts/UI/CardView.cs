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
        private RuntimeCardState runtimeCard;
        private Action<CardData> onClicked;
        private Action<RuntimeCardState> onRuntimeClicked;

        public CardData CardData => cardData;
        public RuntimeCardState RuntimeCard => runtimeCard;

        public void Setup(CardData data, Action<CardData> clickAction)
        {
            cardData = data;
            runtimeCard = null;
            onClicked = clickAction;
            onRuntimeClicked = null;

            ApplyDisplay(
                data.GetDisplayName(false),
                data.ownerHeroId,
                data.cardType,
                data.cost,
                data.BuildDescription(false),
                false,
                data.cardArt);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            SetSelected(false);
        }

        public void Setup(RuntimeCardState data, Action<RuntimeCardState> clickAction, string descriptionOverride = null)
        {
            runtimeCard = data;
            cardData = data != null ? data.baseCard : null;
            onRuntimeClicked = clickAction;
            onClicked = null;

            if (data == null)
            {
                return;
            }

            ApplyDisplay(
                data.DisplayName,
                data.OwnerHeroId,
                data.CardType,
                data.Cost,
                string.IsNullOrWhiteSpace(descriptionOverride) ? data.Description : descriptionOverride,
                data.isUpgraded,
                data.CardArt);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
            SetSelected(false);
        }

        private void ApplyDisplay(string displayName, HeroId ownerHeroId, CardType cardType, int cost, string description, bool isUpgraded, Sprite cardArt)
        {
            cardNameText.text = displayName;
            ownerText.text = ownerHeroId == HeroId.Neutral ? "Owner: Neutral" : "Owner: " + ownerHeroId;
            typeText.text = isUpgraded ? "Type: " + cardType + "  UPGRADED" : "Type: " + cardType;
            costText.text = cost.ToString();
            descriptionText.text = description;
            ConfigureReadableText();

            artImage.sprite = cardArt;
            artImage.enabled = true;
            artImage.color = cardArt != null ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1f);
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
            if (runtimeCard != null)
            {
                onRuntimeClicked?.Invoke(runtimeCard);
                return;
            }

            onClicked?.Invoke(cardData);
        }
    }
}
