using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroSelectionCardView : MonoBehaviour
    {
        public Text heroNameText;
        public Text heroClassText;
        public Text descriptionText;
        public Text progressionText;
        public Text lockStateText;
        public Image portraitImage;
        public Image selectionOutline;
        public Image backgroundImage;
        public Button button;

        private HeroData heroData;
        private HeroSelectionManager selectionManager;
        private HeroProgressionManager progressionManager;

        public void Setup(HeroData data, HeroSelectionManager manager, HeroProgressionManager progression, bool isSelected)
        {
            heroData = data;
            selectionManager = manager;
            progressionManager = progression;

            heroNameText.text = data.heroName;
            heroClassText.text = data.heroClass.ToString();
            descriptionText.text = data.description;
            EnsureOptionalTexts();

            if (descriptionText != null)
            {
                descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            portraitImage.sprite = data.portrait;
            portraitImage.enabled = true;
            portraitImage.color = data.portrait != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
            RefreshProgression(progressionManager);
            SetSelected(isSelected);
        }

        public void RefreshProgression(HeroProgressionManager progression)
        {
            progressionManager = progression;
            EnsureOptionalTexts();

            if (progressionText != null)
            {
                progressionText.text = progressionManager != null
                    ? progressionManager.GetProgressLabel(heroData.heroId)
                    : "Level 1 - 0/25 XP";
            }

            bool isUnlocked = progressionManager == null || progressionManager.IsHeroUnlocked(heroData.heroId);
            SetLocked(!isUnlocked);
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionOutline != null)
            {
                selectionOutline.color = isSelected ? new Color(0.2f, 0.9f, 0.4f, 1f) : new Color(0f, 0f, 0f, 0f);
            }

            if (backgroundImage != null)
            {
                if (button != null && !button.interactable)
                {
                    backgroundImage.color = new Color(0.65f, 0.65f, 0.65f, 1f);
                }
                else
                {
                    backgroundImage.color = isSelected ? new Color(0.86f, 0.95f, 0.83f, 1f) : new Color(0.93f, 0.88f, 0.76f, 1f);
                }
            }
        }

        public void SetLocked(bool isLocked)
        {
            EnsureOptionalTexts();

            if (lockStateText != null)
            {
                lockStateText.text = isLocked ? "Locked" : string.Empty;
            }

            if (backgroundImage != null)
            {
                if (isLocked)
                {
                    backgroundImage.color = new Color(0.65f, 0.65f, 0.65f, 1f);
                }
            }

            if (button != null)
            {
                button.interactable = !isLocked;
            }

            if (portraitImage != null)
            {
                portraitImage.color = isLocked
                    ? new Color(0.45f, 0.45f, 0.45f, 1f)
                    : (heroData != null && heroData.portrait != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f));
            }
        }

        private void OnClicked()
        {
            if (heroData != null && selectionManager != null)
            {
                selectionManager.ToggleHeroSelection(heroData.heroId);
            }
        }

        private void EnsureOptionalTexts()
        {
            if (progressionText == null)
            {
                progressionText = CreateText("ProgressionText", new Vector2(12f, -84f), new Vector2(220f, 22f), 15, FontStyle.Bold, TextAnchor.UpperLeft);
            }

            if (lockStateText == null)
            {
                lockStateText = CreateText("LockStateText", new Vector2(12f, -108f), new Vector2(220f, 22f), 15, FontStyle.Bold, TextAnchor.UpperLeft);
                lockStateText.color = new Color(0.45f, 0.1f, 0.1f, 1f);
            }
        }

        private Text CreateText(string objectName, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(transform, false);
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
