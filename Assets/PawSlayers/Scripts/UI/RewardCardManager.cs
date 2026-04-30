using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class RewardCardManager : MonoBehaviour
    {
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
                    rewardTitleText.text = "Choose 1 reward card";
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

            rewardTitleText.text = "Choose 1 reward card";

            if (rewardInfoText != null)
            {
                rewardInfoText.text = "Choose one card to add to your run deck.";
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
                rewardInfoText.text = $"Selected: {selectedCard.cardName}";
            }

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
        }

        private CardView CreateRuntimeRewardCardView(Transform parent)
        {
            GameObject root = new GameObject("RewardCardView", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            LayoutElement layout = root.AddComponent<LayoutElement>();
            layout.preferredWidth = 250f;
            layout.preferredHeight = 320f;

            Image background = root.AddComponent<Image>();
            background.color = Color.white;
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
