using System;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class CardView : MonoBehaviour
    {
        private static readonly Color AttackBannerColor = new Color(0.72f, 0.31f, 0.22f, 1f);
        private static readonly Color SkillBannerColor = new Color(0.24f, 0.50f, 0.42f, 1f);
        private static readonly Color StatusBannerColor = new Color(0.45f, 0.39f, 0.57f, 1f);
        private static readonly Color NeutralAccentColor = new Color(0.66f, 0.58f, 0.43f, 1f);
        private static readonly Color CapybaraAccentColor = new Color(0.58f, 0.38f, 0.24f, 1f);
        private static readonly Color KoalaAccentColor = new Color(0.47f, 0.42f, 0.58f, 1f);
        private static readonly Color SlothAccentColor = new Color(0.36f, 0.57f, 0.39f, 1f);
        private static readonly Color PandaAccentColor = new Color(0.64f, 0.59f, 0.36f, 1f);
        private static readonly Color KangarooAccentColor = new Color(0.74f, 0.42f, 0.20f, 1f);
        private static readonly Color ParchmentColor = new Color(0.96f, 0.93f, 0.84f, 1f);
        private static readonly Color StatusCardColor = new Color(0.83f, 0.81f, 0.88f, 1f);
        private static readonly Color DisabledCardColor = new Color(0.58f, 0.58f, 0.58f, 1f);
        private static readonly Color DisabledOverlayColor = new Color(0.10f, 0.10f, 0.12f, 0.42f);
        private static readonly Color SelectedOutlineColor = new Color(0.95f, 0.80f, 0.27f, 1f);
        private static readonly Color AffordableCostColor = new Color(0.20f, 0.40f, 0.70f, 1f);
        private static readonly Color LowEnergyCostColor = new Color(0.37f, 0.45f, 0.58f, 1f);
        private static readonly Color UpgradedBadgeColor = new Color(0.72f, 0.56f, 0.16f, 1f);
        private static readonly Color ReasonTextColor = new Color(1f, 0.95f, 0.95f, 1f);
        private static readonly Color WarningTextColor = new Color(0.80f, 0.50f, 0.16f, 1f);

        public Text cardNameText;
        public Text ownerText;
        public Text typeText;
        public Text costText;
        public Text descriptionText;
        public Text disabledReasonText;
        public Text upgradedLabelText;
        public Text artLabelText;
        public Image artImage;
        public Image backgroundImage;
        public Image costBadgeImage;
        public Image disabledOverlayImage;
        public Image headerBannerImage;
        public Image ownerAccentImage;
        public Image artFrameImage;
        public Button button;
        public CanvasGroup canvasGroup;
        public Outline selectionOutline;

        private CardData cardData;
        private RuntimeCardState runtimeCard;
        private Action<CardData> onClicked;
        private Action<RuntimeCardState> onRuntimeClicked;
        private Color baseBackgroundColor = ParchmentColor;
        private Color baseCostBadgeColor = AffordableCostColor;
        private bool isDisabled;
        private bool showEnergyWarning;

        public CardData CardData => cardData;
        public RuntimeCardState RuntimeCard => runtimeCard;

        public void Setup(CardData data, Action<CardData> clickAction)
        {
            cardData = data;
            runtimeCard = null;
            onClicked = clickAction;
            onRuntimeClicked = null;

            EnsureComponentReferences();

            if (data == null)
            {
                ApplyMissingDisplay();
                return;
            }

            ApplyDisplay(
                data.GetDisplayName(false),
                data.ownerHeroId,
                data.cardType,
                data.cost,
                data.BuildDescription(false),
                false,
                data.cardArt);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }

            SetDisabled(false, string.Empty);
            SetAffordable(true);
            SetSelected(false);
        }

        public void Setup(RuntimeCardState data, Action<RuntimeCardState> clickAction, string descriptionOverride = null)
        {
            runtimeCard = data;
            cardData = data != null ? data.baseCard : null;
            onRuntimeClicked = clickAction;
            onClicked = null;

            EnsureComponentReferences();

            if (data == null)
            {
                ApplyMissingDisplay();
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

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }

            SetDisabled(false, string.Empty);
            SetAffordable(true);
            SetSelected(false);
        }

        public void SetDisabled(bool disabled, string disabledReason)
        {
            isDisabled = disabled;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = disabled ? 0.62f : 1f;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = disabled ? DisabledCardColor : baseBackgroundColor;
            }

            if (disabledOverlayImage != null)
            {
                disabledOverlayImage.enabled = disabled;
                disabledOverlayImage.color = DisabledOverlayColor;
            }

            if (disabledReasonText != null)
            {
                disabledReasonText.text = disabled ? disabledReason : string.Empty;
                disabledReasonText.color = disabled ? ReasonTextColor : WarningTextColor;
            }

            if (button != null)
            {
                button.interactable = !disabled;
            }

            ApplyCostBadgeState();
        }

        public void SetAffordable(bool affordable, string warningText = "Not enough energy")
        {
            showEnergyWarning = !affordable;

            if (!isDisabled && disabledReasonText != null)
            {
                disabledReasonText.text = affordable ? string.Empty : warningText;
                disabledReasonText.color = WarningTextColor;
            }

            ApplyCostBadgeState();
        }

        public void SetSelected(bool selected)
        {
            if (selectionOutline != null)
            {
                selectionOutline.enabled = selected;
                selectionOutline.effectColor = SelectedOutlineColor;
                selectionOutline.effectDistance = new Vector2(4f, -4f);
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = selected ? new Vector3(1.03f, 1.03f, 1f) : Vector3.one;
            }
        }

        private void ApplyDisplay(string displayName, HeroId ownerHeroId, CardType cardType, int cost, string description, bool upgraded, Sprite cardArt)
        {
            baseBackgroundColor = cardType == CardType.Status ? StatusCardColor : ParchmentColor;
            baseCostBadgeColor = GetCostBadgeColor(cardType);

            if (cardNameText != null)
            {
                cardNameText.text = displayName;
                cardNameText.color = new Color(0.18f, 0.14f, 0.11f, 1f);
            }

            if (ownerText != null)
            {
                ownerText.text = BuildOwnerLine(ownerHeroId);
                ownerText.color = new Color(0.32f, 0.27f, 0.22f, 1f);
            }

            if (typeText != null)
            {
                typeText.text = cardType.ToString().ToUpperInvariant();
                typeText.color = new Color(0.98f, 0.96f, 0.90f, 1f);
            }

            if (costText != null)
            {
                costText.text = cost.ToString();
                costText.color = Color.white;
            }

            if (descriptionText != null)
            {
                descriptionText.text = description;
                descriptionText.color = new Color(0.24f, 0.20f, 0.17f, 1f);
            }

            if (upgradedLabelText != null)
            {
                upgradedLabelText.text = upgraded ? "UPGRADED" : string.Empty;
                upgradedLabelText.color = upgraded ? UpgradedBadgeColor : Color.clear;
            }

            if (artImage != null)
            {
                artImage.sprite = cardArt;
                artImage.color = cardArt != null
                    ? Color.white
                    : new Color(0.84f, 0.82f, 0.76f, 1f);
            }

            if (artLabelText != null)
            {
                artLabelText.text = cardArt == null ? GetArtPlaceholderLabel(cardType) : string.Empty;
                artLabelText.color = new Color(0.29f, 0.26f, 0.22f, 0.92f);
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = baseBackgroundColor;
            }

            if (headerBannerImage != null)
            {
                headerBannerImage.color = GetBannerColor(cardType);
            }

            if (ownerAccentImage != null)
            {
                ownerAccentImage.color = GetOwnerAccentColor(ownerHeroId);
            }

            if (artFrameImage != null)
            {
                artFrameImage.color = new Color(0.72f, 0.64f, 0.50f, 1f);
            }

            ApplyCostBadgeState();
            ConfigureReadableText();
        }

        private void ApplyMissingDisplay()
        {
            if (cardNameText != null)
            {
                cardNameText.text = "Missing Card";
            }

            if (descriptionText != null)
            {
                descriptionText.text = "Card data is missing.";
            }
        }

        private void EnsureComponentReferences()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            EnsureLayout();
            EnsureSelectionOutline();
            EnsureHeaderBanner();
            EnsureOwnerAccent();
            EnsureCostBadge();
            EnsureArtFrame();
            EnsureArtLabel();
            EnsureUpgradeLabel();
            EnsureDisabledOverlay();
        }

        private void EnsureLayout()
        {
            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(236f, 336f);
            }

            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = 236f;
                layout.preferredHeight = 336f;
                layout.minWidth = 220f;
                layout.minHeight = 320f;
            }

            SetRect(cardNameText, new Vector2(62f, -12f), new Vector2(154f, 34f), TextAnchor.UpperLeft);
            SetRect(ownerText, new Vector2(16f, -50f), new Vector2(202f, 18f), TextAnchor.UpperLeft);
            SetRect(typeText, new Vector2(16f, -74f), new Vector2(110f, 18f), TextAnchor.MiddleCenter);
            SetRect(descriptionText, new Vector2(16f, -210f), new Vector2(204f, 88f), TextAnchor.UpperLeft);
            SetRect(disabledReasonText, new Vector2(18f, -302f), new Vector2(200f, 28f), TextAnchor.MiddleCenter);
        }

        private void EnsureSelectionOutline()
        {
            if (selectionOutline != null)
            {
                return;
            }

            selectionOutline = GetComponent<Outline>();
            if (selectionOutline == null)
            {
                selectionOutline = gameObject.AddComponent<Outline>();
            }

            selectionOutline.effectColor = SelectedOutlineColor;
            selectionOutline.effectDistance = new Vector2(4f, -4f);
            selectionOutline.enabled = false;
        }

        private void EnsureHeaderBanner()
        {
            if (headerBannerImage == null)
            {
                headerBannerImage = FindOrCreateImage("HeaderBanner", transform, new Color(0.27f, 0.27f, 0.27f, 1f));
            }

            RectTransform rect = headerBannerImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(10f, -86f);
            rect.offsetMax = new Vector2(-10f, -56f);
            headerBannerImage.transform.SetSiblingIndex(0);
        }

        private void EnsureOwnerAccent()
        {
            if (ownerAccentImage == null)
            {
                ownerAccentImage = FindOrCreateImage("OwnerAccent", transform, NeutralAccentColor);
            }

            RectTransform rect = ownerAccentImage.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(10f, 0f);
            ownerAccentImage.transform.SetSiblingIndex(0);
        }

        private void EnsureCostBadge()
        {
            if (costBadgeImage == null)
            {
                costBadgeImage = FindOrCreateImage("CostBadge", transform, AffordableCostColor);
            }

            RectTransform badgeRect = costBadgeImage.rectTransform;
            badgeRect.anchorMin = new Vector2(0f, 1f);
            badgeRect.anchorMax = new Vector2(0f, 1f);
            badgeRect.pivot = new Vector2(0f, 1f);
            badgeRect.anchoredPosition = new Vector2(12f, -12f);
            badgeRect.sizeDelta = new Vector2(42f, 42f);

            if (costText != null)
            {
                costText.transform.SetParent(costBadgeImage.transform, false);
                StretchFull(costText.rectTransform, 0f);
                costText.alignment = TextAnchor.MiddleCenter;
            }
        }

        private void EnsureArtFrame()
        {
            if (artFrameImage == null)
            {
                artFrameImage = FindOrCreateImage("ArtFrame", transform, new Color(0.72f, 0.64f, 0.50f, 1f));
            }

            RectTransform frameRect = artFrameImage.rectTransform;
            frameRect.anchorMin = new Vector2(0f, 1f);
            frameRect.anchorMax = new Vector2(0f, 1f);
            frameRect.pivot = new Vector2(0f, 1f);
            frameRect.anchoredPosition = new Vector2(16f, -102f);
            frameRect.sizeDelta = new Vector2(204f, 94f);

            if (artImage == null)
            {
                artImage = FindOrCreateImage("Art", artFrameImage.transform, new Color(0.84f, 0.82f, 0.76f, 1f));
            }
            else
            {
                artImage.transform.SetParent(artFrameImage.transform, false);
            }

            RectTransform artRect = artImage.rectTransform;
            StretchFull(artRect, 6f);
        }

        private void EnsureArtLabel()
        {
            Transform parent = artFrameImage != null ? artFrameImage.transform : transform;
            if (artLabelText == null)
            {
                artLabelText = FindOrCreateText("ArtLabel", parent, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            }

            StretchFull(artLabelText.rectTransform, 8f);
            artLabelText.color = new Color(0.29f, 0.26f, 0.22f, 0.92f);
        }

        private void EnsureUpgradeLabel()
        {
            if (upgradedLabelText == null)
            {
                upgradedLabelText = FindOrCreateText("UpgradedLabel", transform, 11, FontStyle.Bold, TextAnchor.UpperRight);
            }

            SetRect(upgradedLabelText, new Vector2(132f, -14f), new Vector2(84f, 18f), TextAnchor.UpperRight);
            upgradedLabelText.color = UpgradedBadgeColor;
        }

        private void EnsureDisabledOverlay()
        {
            if (disabledOverlayImage == null)
            {
                disabledOverlayImage = FindOrCreateImage("DisabledOverlay", transform, DisabledOverlayColor);
            }

            StretchFull(disabledOverlayImage.rectTransform, 0f);
            disabledOverlayImage.transform.SetAsLastSibling();
            disabledOverlayImage.enabled = false;

            if (disabledReasonText != null)
            {
                disabledReasonText.transform.SetParent(disabledOverlayImage.transform, false);
                RectTransform reasonRect = disabledReasonText.rectTransform;
                reasonRect.anchorMin = new Vector2(0f, 0f);
                reasonRect.anchorMax = new Vector2(1f, 0f);
                reasonRect.pivot = new Vector2(0.5f, 0f);
                reasonRect.anchoredPosition = new Vector2(0f, 14f);
                reasonRect.sizeDelta = new Vector2(-18f, 40f);
            }
        }

        private void ConfigureReadableText()
        {
            ConfigureText(cardNameText, 15, 22);
            ConfigureText(ownerText, 12, 14);
            ConfigureText(typeText, 12, 13);
            ConfigureText(descriptionText, 12, 16);
            ConfigureText(disabledReasonText, 12, 15);
            ConfigureText(upgradedLabelText, 10, 11);
            ConfigureText(artLabelText, 13, 18);
        }

        private void ConfigureText(Text text, int minSize, int maxSize)
        {
            if (text == null)
            {
                return;
            }

            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void ApplyCostBadgeState()
        {
            if (costBadgeImage != null)
            {
                costBadgeImage.color = showEnergyWarning && !isDisabled ? LowEnergyCostColor : baseCostBadgeColor;
            }
        }

        private string BuildOwnerLine(HeroId ownerHeroId)
        {
            if (ownerHeroId == HeroId.Neutral)
            {
                return "Neutral";
            }

            HeroClass heroClass = GetHeroClass(ownerHeroId);
            return heroClass == HeroClass.None
                ? ownerHeroId.ToString()
                : ownerHeroId + " - " + heroClass;
        }

        private string GetArtPlaceholderLabel(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Attack:
                    return "ATTACK";
                case CardType.Skill:
                    return "SKILL";
                default:
                    return "STATUS";
            }
        }

        private Color GetBannerColor(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Attack:
                    return AttackBannerColor;
                case CardType.Skill:
                    return SkillBannerColor;
                default:
                    return StatusBannerColor;
            }
        }

        private Color GetCostBadgeColor(CardType cardType)
        {
            switch (cardType)
            {
                case CardType.Attack:
                    return new Color(0.73f, 0.30f, 0.22f, 1f);
                case CardType.Skill:
                    return AffordableCostColor;
                default:
                    return new Color(0.43f, 0.39f, 0.54f, 1f);
            }
        }

        private Color GetOwnerAccentColor(HeroId ownerHeroId)
        {
            switch (ownerHeroId)
            {
                case HeroId.Capybara:
                    return CapybaraAccentColor;
                case HeroId.Koala:
                    return KoalaAccentColor;
                case HeroId.Sloth:
                    return SlothAccentColor;
                case HeroId.Panda:
                    return PandaAccentColor;
                case HeroId.Kangaroo:
                    return KangarooAccentColor;
                default:
                    return NeutralAccentColor;
            }
        }

        private HeroClass GetHeroClass(HeroId ownerHeroId)
        {
            switch (ownerHeroId)
            {
                case HeroId.Capybara:
                    return HeroClass.Swordsman;
                case HeroId.Koala:
                    return HeroClass.Thief;
                case HeroId.Sloth:
                    return HeroClass.Healer;
                case HeroId.Panda:
                    return HeroClass.Tank;
                case HeroId.Kangaroo:
                    return HeroClass.Fighter;
                default:
                    return HeroClass.None;
            }
        }

        private Image FindOrCreateImage(string objectName, Transform parent, Color color)
        {
            Transform existing = parent.Find(objectName);
            Image image = existing != null ? existing.GetComponent<Image>() : null;
            if (image != null)
            {
                return image;
            }

            GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            image = imageObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text FindOrCreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            Transform existing = parent.Find(objectName);
            Text text = existing != null ? existing.GetComponent<Text>() : null;
            if (text != null)
            {
                return text;
            }

            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.black;
            return text;
        }

        private void SetRect(Text text, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
        {
            if (text == null)
            {
                return;
            }

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = anchor == TextAnchor.UpperRight || anchor == TextAnchor.MiddleRight
                ? new Vector2(1f, 1f)
                : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            text.alignment = anchor;
        }

        private void StretchFull(RectTransform rectTransform, float padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
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
