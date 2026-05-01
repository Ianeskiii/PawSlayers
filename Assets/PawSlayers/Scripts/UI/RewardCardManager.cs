using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class RewardCardManager : MonoBehaviour
    {
        private static readonly Color RewardOverlayColor = new Color(0f, 0f, 0f, 0.74f);
        private static readonly Color RewardPanelColor = new Color(0.96f, 0.92f, 0.84f, 1f);
        private static readonly Color RewardInfoColor = new Color(0.33f, 0.28f, 0.22f, 1f);
        private static readonly Color RewardAccentColor = new Color(0.74f, 0.56f, 0.18f, 1f);

        public RunManager runManager;
        public BattleUIManager battleUiManager;
        public CardDatabase cardDatabase;
        public GameObject rewardPanel;
        public Transform rewardContainer;
        public CardView rewardCardPrefab;
        public Text rewardTitleText;
        public Text rewardInfoText;
        public Button continueButton;

        private bool rewardChosen;

        public void ShowRewards()
        {
            EnsureRuntimeUi();

            if (runManager == null)
            {
                runManager = RunManager.Instance;
            }

            if (cardDatabase == null && runManager != null)
            {
                cardDatabase = runManager.cardDatabase;
            }

            if (rewardPanel == null || rewardContainer == null || rewardTitleText == null || cardDatabase == null || runManager == null)
            {
                return;
            }

            StyleRewardPanel();
            rewardChosen = false;
            ConfigureContinueButton();
            runManager.EnsurePrototypeData();

            foreach (Transform child in rewardContainer)
            {
                Destroy(child.gameObject);
            }

            int rewardChoiceCount = runManager.HasRelic(RelicId.LuckyPaw) ? 4 : 3;
            List<CardData> eligibleCards = cardDatabase.GetEligibleCards(runManager.SelectedHeroIds, runManager.ProgressionManager);
            if (eligibleCards.Count == 0 && runManager.cardDatabase != null)
            {
                eligibleCards = runManager.cardDatabase.GetEligibleCards(runManager.SelectedHeroIds, null)
                    .Where(card => card != null && card.cardType != CardType.Status)
                    .ToList();
                Debug.LogWarning("Reward pool fallback used because filtered eligible cards were empty.");
            }

            List<CardData> choices = eligibleCards.OrderBy(_ => Random.value).Take(rewardChoiceCount).ToList();

            if (choices.Count == 0)
            {
                if (rewardTitleText != null)
                {
                    rewardTitleText.text = "Choose 1 Reward Card";
                }

                if (rewardInfoText != null)
                {
                    rewardInfoText.text = "No reward cards available.";
                }

                if (continueButton != null)
                {
                    continueButton.gameObject.SetActive(true);
                    continueButton.interactable = true;
                }

                rewardPanel.SetActive(true);
                Debug.LogWarning("Reward screen opened with no reward choices.");
                return;
            }

            foreach (CardData choice in choices)
            {
                CardView view = rewardCardPrefab != null
                    ? Instantiate(rewardCardPrefab, rewardContainer)
                    : CreateRuntimeRewardCardView(rewardContainer);

                view.Setup(choice, SelectReward);
                view.SetDisabled(false, string.Empty);
            }

            rewardTitleText.text = "Choose 1 Reward Card";

            if (rewardInfoText != null)
            {
                rewardInfoText.text = "Add one card to your deck.";
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
                continueButton.interactable = false;
            }

            rewardPanel.SetActive(true);
        }

        public void SelectReward(CardData selectedCard)
        {
            if (rewardChosen || runManager == null || selectedCard == null)
            {
                return;
            }

            rewardChosen = true;
            runManager.AddRewardCard(selectedCard);

            if (rewardInfoText != null)
            {
                rewardInfoText.text = $"Added {selectedCard.cardName} to deck.";
            }

            LockRewardChoices(selectedCard);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = true;
            }
        }

        public void ContinueAfterReward()
        {
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }

            if (runManager != null && !runManager.RunWon)
            {
                runManager.EnterMapAfterBattleReward();
            }
        }

        private void ConfigureContinueButton()
        {
            if (continueButton == null)
            {
                return;
            }

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(ContinueAfterReward);
            StyleButton(continueButton, RewardAccentColor);
        }

        private void EnsureRuntimeUi()
        {
            if (rewardPanel == null)
            {
                return;
            }

            Transform rewardBox = rewardPanel.transform.childCount > 0 ? rewardPanel.transform.GetChild(0) : null;
            if (rewardBox == null)
            {
                return;
            }

            if (rewardTitleText == null)
            {
                rewardTitleText = FindOrCreateText(rewardBox, "RewardTitle", new Vector2(20f, -20f), new Vector2(420f, 34f), 30, FontStyle.Bold, TextAnchor.UpperLeft);
            }

            if (rewardInfoText == null)
            {
                rewardInfoText = FindOrCreateText(rewardBox, "RewardInfo", new Vector2(20f, -58f), new Vector2(560f, 28f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            }

            if (rewardContainer == null)
            {
                Transform existing = rewardBox.Find("RewardContainer");
                rewardContainer = existing;
            }

            if (continueButton == null)
            {
                Transform existing = rewardBox.Find("ContinueButton");
                if (existing != null)
                {
                    continueButton = existing.GetComponent<Button>();
                }
                else
                {
                    continueButton = CreateButton("ContinueButton", rewardBox, "Continue", new Vector2(180f, 48f));
                    RectTransform continueRect = continueButton.GetComponent<RectTransform>();
                    continueRect.anchorMin = new Vector2(1f, 0f);
                    continueRect.anchorMax = new Vector2(1f, 0f);
                    continueRect.pivot = new Vector2(1f, 0f);
                    continueRect.anchoredPosition = new Vector2(-20f, 20f);
                    continueRect.sizeDelta = new Vector2(180f, 48f);
                }
            }
        }

        private void StyleRewardPanel()
        {
            if (rewardPanel != null)
            {
                Image overlay = rewardPanel.GetComponent<Image>();
                if (overlay != null)
                {
                    overlay.color = RewardOverlayColor;
                }
            }

            Transform rewardBox = rewardPanel != null && rewardPanel.transform.childCount > 0 ? rewardPanel.transform.GetChild(0) : null;
            if (rewardBox != null)
            {
                Image boxImage = rewardBox.GetComponent<Image>();
                if (boxImage != null)
                {
                    boxImage.color = RewardPanelColor;
                }
            }

            if (rewardTitleText != null)
            {
                rewardTitleText.color = new Color(0.18f, 0.15f, 0.12f, 1f);
            }

            if (rewardInfoText != null)
            {
                rewardInfoText.color = RewardInfoColor;
            }

            if (continueButton != null)
            {
                StyleButton(continueButton, RewardAccentColor);
            }
        }

        private void LockRewardChoices(CardData selectedCard)
        {
            if (rewardContainer == null)
            {
                return;
            }

            foreach (Transform child in rewardContainer)
            {
                CardView view = child.GetComponent<CardView>();
                if (view == null)
                {
                    continue;
                }

                bool isSelected = view.CardData == selectedCard;
                view.SetSelected(isSelected);
                if (!isSelected)
                {
                    view.SetDisabled(true, string.Empty);
                }
            }
        }

        private CardView CreateRuntimeRewardCardView(Transform parent)
        {
            GameObject root = new GameObject("RewardCardView", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 250f;
            layout.preferredHeight = 320f;

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            Button button = root.AddComponent<Button>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();

            CardView view = root.AddComponent<CardView>();
            view.backgroundImage = background;
            view.button = button;
            view.canvasGroup = canvasGroup;
            view.cardNameText = CreateText("CardName", root.transform, new Vector2(10f, -10f), new Vector2(180f, 40f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            view.costText = CreateText("CostText", root.transform, new Vector2(198f, -10f), new Vector2(40f, 28f), 22, FontStyle.Bold, TextAnchor.UpperRight);
            view.ownerText = CreateText("OwnerText", root.transform, new Vector2(10f, -54f), new Vector2(220f, 22f), 16, FontStyle.Italic, TextAnchor.UpperLeft);
            view.typeText = CreateText("TypeText", root.transform, new Vector2(10f, -78f), new Vector2(220f, 22f), 16, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject art = new GameObject("Art", typeof(RectTransform));
            art.transform.SetParent(root.transform, false);
            Image artImage = art.AddComponent<Image>();
            artImage.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            RectTransform artRect = art.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0f, 1f);
            artRect.anchorMax = new Vector2(0f, 1f);
            artRect.pivot = new Vector2(0f, 1f);
            artRect.anchoredPosition = new Vector2(20f, -108f);
            artRect.sizeDelta = new Vector2(210f, 90f);
            view.artImage = artImage;

            view.descriptionText = CreateText("Description", root.transform, new Vector2(10f, -208f), new Vector2(230f, 74f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            view.disabledReasonText = CreateText("DisabledReason", root.transform, new Vector2(10f, -286f), new Vector2(230f, 28f), 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            view.disabledReasonText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
            return view;
        }

        private Text FindOrCreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Text existingText = existing.GetComponent<Text>();
                if (existingText != null)
                {
                    return existingText;
                }
            }

            return CreateText(name, parent, anchoredPosition, size, fontSize, fontStyle, alignment);
        }

        private Button CreateButton(string name, Transform parent, string label, Vector2 size)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            Image image = buttonObject.GetComponent<Image>();
            image.color = RewardAccentColor;

            Button button = buttonObject.GetComponent<Button>();
            StyleButton(button, RewardAccentColor);

            Text labelText = CreateText("Label", buttonObject.transform, Vector2.zero, size, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(labelText.rectTransform, 0f);
            labelText.text = label;
            labelText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            return button;
        }

        private void StyleButton(Button button, Color normalColor)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = normalColor;
            }

            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = normalColor * 1.08f;
            colors.pressedColor = normalColor * 0.9f;
            colors.disabledColor = new Color(0.42f, 0.42f, 0.42f, 0.85f);
            button.colors = colors;
        }

        private void StretchFull(RectTransform rectTransform, float padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }

        private Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.black;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }
    }
}
