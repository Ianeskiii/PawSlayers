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
        public Text upgradedLabelText;
        public Image artImage;
        public Image backgroundImage;
        public Image costBadgeImage;
        public Image disabledOverlayImage;
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
            if (upgradedLabelText != null)
            {
                upgradedLabelText.text = isUpgraded ? "UPGRADED" : string.Empty;
            }
            ConfigureReadableText();

            artImage.sprite = cardArt;
            artImage.enabled = true;
            artImage.color = cardArt != null ? Color.white : new Color(0.82f, 0.82f, 0.82f, 1f);

            if (backgroundImage != null)
            {
                backgroundImage.color = cardType == CardType.Status
                    ? new Color(0.76f, 0.76f, 0.80f, 1f)
                    : new Color(0.96f, 0.93f, 0.84f, 1f);
            }

            if (costBadgeImage != null)
            {
                costBadgeImage.color = cardType == CardType.Attack
                    ? new Color(0.77f, 0.22f, 0.22f, 1f)
                    : cardType == CardType.Skill
                        ? new Color(0.23f, 0.42f, 0.72f, 1f)
                        : new Color(0.42f, 0.42f, 0.52f, 1f);
            }
        }

        public void SetDisabled(bool isDisabled, string disabledReason)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = isDisabled ? 0.55f : 1f;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isDisabled ? new Color(0.58f, 0.58f, 0.58f, 1f) : backgroundImage.color;
            }

            if (disabledReasonText != null)
            {
                disabledReasonText.text = isDisabled ? disabledReason : string.Empty;
            }

            if (disabledOverlayImage != null)
            {
                disabledOverlayImage.enabled = isDisabled;
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

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = isSelected ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
                rect.anchoredPosition = isSelected
                    ? new Vector2(rect.anchoredPosition.x, 10f)
                    : new Vector2(rect.anchoredPosition.x, 0f);
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
