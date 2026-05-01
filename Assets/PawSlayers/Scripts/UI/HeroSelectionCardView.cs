using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroSelectionCardView : MonoBehaviour
    {
        public Text heroNameText;
        public Text heroClassText;
        public Text descriptionText;
        public Text levelText;
        public Text progressionText;
        public Text lockStateText;
        public Text selectedBadgeText;
        public Image portraitImage;
        public Image selectionOutline;
        public Image backgroundImage;
        public Image xpBarFillImage;
        public Image lockOverlayImage;
        public Button button;

        private HeroData heroData;
        private HeroSelectionManager selectionManager;
        private HeroProgressionManager progressionManager;

        public void Setup(HeroData data, HeroSelectionManager manager, HeroProgressionManager progression, bool isSelected)
        {
            heroData = data;
            selectionManager = manager;
            progressionManager = progression;
            EnsureLayout();

            if (heroNameText != null)
            {
                heroNameText.text = data.heroName;
                heroNameText.color = new Color(0.18f, 0.15f, 0.12f, 1f);
            }

            if (heroClassText != null)
            {
                heroClassText.text = data.heroClass.ToString();
                heroClassText.color = new Color(0.35f, 0.28f, 0.20f, 1f);
            }

            if (descriptionText != null)
            {
                descriptionText.text = data.description;
                descriptionText.color = new Color(0.26f, 0.24f, 0.20f, 1f);
            }

            EnsureOptionalTexts();

            if (descriptionText != null)
            {
                descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
                descriptionText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (portraitImage != null)
            {
                Sprite portraitSprite = PawSlayersArtResolver.GetHeroPortraitSprite(data);
                portraitImage.sprite = portraitSprite;
                portraitImage.enabled = true;
                portraitImage.preserveAspect = true;
                portraitImage.color = portraitSprite != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }

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

            if (levelText != null)
            {
                HeroProgressionState progress = progressionManager != null ? progressionManager.GetProgress(heroData.heroId) : null;
                int level = progress != null ? progress.level : 1;
                levelText.text = $"Level {level}";
            }

            if (xpBarFillImage != null)
            {
                HeroProgressionState progress = progressionManager != null ? progressionManager.GetProgress(heroData.heroId) : null;
                int xp = progress != null ? progress.xp : 0;
                int currentLevel = progress != null ? progress.level : 1;
                int previousThreshold = GetPreviousThreshold(currentLevel);
                int nextThreshold = GetNextThreshold(currentLevel);
                float fill = nextThreshold <= previousThreshold
                    ? 1f
                    : Mathf.Clamp01((float)(xp - previousThreshold) / (nextThreshold - previousThreshold));
                xpBarFillImage.fillAmount = fill;
            }

            bool isUnlocked = progressionManager == null || progressionManager.IsHeroUnlocked(heroData.heroId);
            SetLocked(!isUnlocked);
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionOutline != null)
            {
                selectionOutline.color = isSelected ? new Color(0.98f, 0.82f, 0.28f, 1f) : new Color(0f, 0f, 0f, 0f);
            }

            if (backgroundImage != null)
            {
                if (button != null && !button.interactable)
                {
                    backgroundImage.color = new Color(0.58f, 0.58f, 0.58f, 1f);
                }
                else
                {
                    backgroundImage.color = isSelected ? new Color(0.98f, 0.94f, 0.82f, 1f) : new Color(0.92f, 0.88f, 0.78f, 1f);
                }
            }

            if (selectedBadgeText != null)
            {
                selectedBadgeText.text = isSelected ? "Selected" : string.Empty;
            }

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = isSelected ? new Vector3(1.02f, 1.02f, 1f) : Vector3.one;
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
                    backgroundImage.color = new Color(0.58f, 0.58f, 0.58f, 1f);
                }
            }

            if (button != null)
            {
                button.interactable = !isLocked;
            }

            if (lockOverlayImage != null)
            {
                lockOverlayImage.enabled = isLocked;
            }

            if (portraitImage != null)
            {
                Sprite portraitSprite = heroData != null ? PawSlayersArtResolver.GetHeroPortraitSprite(heroData) : null;
                portraitImage.color = isLocked
                    ? new Color(0.45f, 0.45f, 0.45f, 1f)
                    : (portraitSprite != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f));
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
                progressionText = CreateText("ProgressionText", new Vector2(132f, -110f), new Vector2(230f, 22f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
                progressionText.color = new Color(0.18f, 0.35f, 0.53f, 1f);
            }

            if (lockStateText == null)
            {
                lockStateText = CreateText("LockStateText", new Vector2(132f, -136f), new Vector2(220f, 22f), 15, FontStyle.Bold, TextAnchor.UpperLeft);
                lockStateText.color = new Color(0.45f, 0.1f, 0.1f, 1f);
            }

            if (levelText == null)
            {
                levelText = CreateText("LevelText", new Vector2(132f, -84f), new Vector2(120f, 22f), 15, FontStyle.Bold, TextAnchor.UpperLeft);
                levelText.color = new Color(0.18f, 0.24f, 0.18f, 1f);
            }

            if (selectedBadgeText == null)
            {
                selectedBadgeText = CreateText("SelectedBadgeText", new Vector2(258f, -14f), new Vector2(100f, 20f), 13, FontStyle.Bold, TextAnchor.UpperRight);
                selectedBadgeText.color = new Color(0.72f, 0.52f, 0.08f, 1f);
            }

            if (xpBarFillImage == null)
            {
                GameObject xpBarRoot = CreatePanel("XpBarRoot", new Vector2(132f, -164f), new Vector2(220f, 10f), new Color(0.24f, 0.22f, 0.18f, 1f));
                GameObject fill = CreatePanel("XpBarFill", Vector2.zero, Vector2.zero, new Color(0.42f, 0.72f, 0.55f, 1f), xpBarRoot.transform);
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                xpBarFillImage = fill.GetComponent<Image>();
                xpBarFillImage.type = Image.Type.Filled;
                xpBarFillImage.fillMethod = Image.FillMethod.Horizontal;
                xpBarFillImage.fillAmount = 0f;
            }

            if (lockOverlayImage == null)
            {
                GameObject overlay = CreatePanel("LockOverlay", new Vector2(0f, 0f), Vector2.zero, new Color(0.08f, 0.08f, 0.08f, 0.35f));
                RectTransform overlayRect = overlay.GetComponent<RectTransform>();
                overlayRect.anchorMin = Vector2.zero;
                overlayRect.anchorMax = Vector2.one;
                overlayRect.offsetMin = Vector2.zero;
                overlayRect.offsetMax = Vector2.zero;
                overlay.transform.SetAsLastSibling();
                lockOverlayImage = overlay.GetComponent<Image>();
                lockOverlayImage.enabled = false;

                if (selectedBadgeText != null)
                {
                    selectedBadgeText.transform.SetAsLastSibling();
                }

                if (lockStateText != null)
                {
                    lockStateText.transform.SetAsLastSibling();
                }
            }
        }

        private void EnsureLayout()
        {
            RectTransform rootRect = transform as RectTransform;
            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(390f, 210f);
            }

            if (selectionOutline != null)
            {
                RectTransform outlineRect = selectionOutline.rectTransform;
                outlineRect.anchorMin = Vector2.zero;
                outlineRect.anchorMax = Vector2.one;
                outlineRect.offsetMin = new Vector2(-3f, -3f);
                outlineRect.offsetMax = new Vector2(3f, 3f);
            }

            if (portraitImage != null)
            {
                RectTransform portraitRect = portraitImage.rectTransform;
                portraitRect.anchorMin = new Vector2(0f, 1f);
                portraitRect.anchorMax = new Vector2(0f, 1f);
                portraitRect.pivot = new Vector2(0f, 1f);
                portraitRect.anchoredPosition = new Vector2(18f, -20f);
                portraitRect.sizeDelta = new Vector2(96f, 96f);
            }

            if (heroNameText != null)
            {
                heroNameText.rectTransform.anchoredPosition = new Vector2(132f, -18f);
                heroNameText.rectTransform.sizeDelta = new Vector2(220f, 26f);
            }

            if (heroClassText != null)
            {
                heroClassText.rectTransform.anchoredPosition = new Vector2(132f, -48f);
                heroClassText.rectTransform.sizeDelta = new Vector2(220f, 22f);
            }

            if (descriptionText != null)
            {
                descriptionText.rectTransform.anchoredPosition = new Vector2(132f, -142f);
                descriptionText.rectTransform.sizeDelta = new Vector2(230f, 44f);
            }
        }

        private int GetPreviousThreshold(int currentLevel)
        {
            switch (Mathf.Clamp(currentLevel, 1, 5))
            {
                case 2: return 25;
                case 3: return 60;
                case 4: return 110;
                case 5: return 180;
                default: return 0;
            }
        }

        private int GetNextThreshold(int currentLevel)
        {
            switch (Mathf.Clamp(currentLevel, 1, 5))
            {
                case 1: return 25;
                case 2: return 60;
                case 3: return 110;
                case 4: return 180;
                default: return 180;
            }
        }

        private GameObject CreatePanel(string objectName, Vector2 anchoredPosition, Vector2 size, Color color, Transform overrideParent = null)
        {
            GameObject panelObject = new GameObject(objectName, typeof(RectTransform));
            panelObject.transform.SetParent(overrideParent != null ? overrideParent : transform, false);
            Image image = panelObject.AddComponent<Image>();
            image.color = color;

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            if (overrideParent == null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = size;
            }

            return panelObject;
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
