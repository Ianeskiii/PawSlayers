using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroProgressionScreenManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public RunManager runManager;

        [Header("UI")]
        public RectTransform screenRoot;
        public Text titleText;
        public Text subtitleText;
        public Transform heroListContainer;
        public Button backButton;
        public Button resetProgressionDebugButton;
        public Text infoMessageText;

        private void Start()
        {
            runManager = ResolveRunManager();
            runManager.EnsurePrototypeData();

            Debug.Log("Hero Progression opened.");
            EnsureRuntimeUi();
            BindButtons();
            RefreshUi();
        }

        public void BackToMainMenu()
        {
            if (runManager == null)
            {
                return;
            }

            Debug.Log("Back clicked.");
            runManager.LoadSceneByName(runManager.mainMenuSceneName);
        }

        public void ResetProgressionDebug()
        {
            if (runManager == null)
            {
                return;
            }

            runManager.ResetHeroProgressionForTesting();
            SetInfo("DEBUG: Hero progression reset.");
            RefreshUi();
        }

        private RunManager ResolveRunManager()
        {
            if (runManager != null)
            {
                return runManager;
            }

            runManager = RunManager.Instance;
            if (runManager != null)
            {
                return runManager;
            }

            GameObject runManagerObject = new GameObject("RunManager");
            runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();
            return runManager;
        }

        private void BindButtons()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(BackToMainMenu);
            }

            if (resetProgressionDebugButton != null)
            {
                resetProgressionDebugButton.onClick.RemoveAllListeners();
                resetProgressionDebugButton.onClick.AddListener(ResetProgressionDebug);
            }
        }

        private void RefreshUi()
        {
            if (heroListContainer == null || runManager == null || runManager.heroDatabase == null)
            {
                return;
            }

            foreach (Transform child in heroListContainer)
            {
                Destroy(child.gameObject);
            }

            HeroProgressionManager progressionManager = runManager.ProgressionManager;

            foreach (HeroData hero in runManager.heroDatabase.heroes.Where(hero => hero != null && hero.heroId != HeroId.Neutral))
            {
                HeroProgressionState progress = progressionManager != null ? progressionManager.GetProgress(hero.heroId) : null;
                CreateHeroProgressEntry(hero, progress, progressionManager);
            }
        }

        private void CreateHeroProgressEntry(HeroData hero, HeroProgressionState progress, HeroProgressionManager progressionManager)
        {
            GameObject panel = CreatePanel($"{hero.heroName}Entry", heroListContainer as RectTransform, new Color(0.92f, 0.88f, 0.79f, 1f));
            LayoutElement layoutElement = panel.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 210f;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 210f);

            if (progress != null && !progress.isUnlocked)
            {
                panel.GetComponent<Image>().color = new Color(0.62f, 0.62f, 0.62f, 1f);
            }

            Text heroName = CreateText("HeroName", rect, new Vector2(18f, -16f), new Vector2(440f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            heroName.text = $"{hero.heroName} - {hero.heroClass}";
            heroName.color = new Color(0.18f, 0.15f, 0.12f, 1f);

            int level = progress != null ? progress.level : 1;
            int xp = progress != null ? progress.xp : 0;
            int nextTarget = progressionManager != null ? progressionManager.GetNextLevelXpTarget(hero.heroId) : 25;
            string unlockedText = progress != null && progress.isUnlocked ? "Unlocked" : "Locked";

            Text levelText = CreateText("LevelText", rect, new Vector2(18f, -50f), new Vector2(180f, 22f), 18, FontStyle.Bold, TextAnchor.UpperLeft);
            levelText.text = $"Level {level}";
            levelText.color = new Color(0.17f, 0.28f, 0.17f, 1f);

            Text xpText = CreateText("XpText", rect, new Vector2(18f, -76f), new Vector2(260f, 22f), 17, FontStyle.Normal, TextAnchor.UpperLeft);
            xpText.text = level >= 5 ? $"XP: {xp} / Max" : $"XP: {xp} / {nextTarget}";
            xpText.color = new Color(0.18f, 0.35f, 0.53f, 1f);

            CreateXpBar(rect, xp, level, nextTarget);

            Text unlockedStateText = CreateText("UnlockedText", rect, new Vector2(18f, -114f), new Vector2(220f, 22f), 17, FontStyle.Bold, TextAnchor.UpperLeft);
            unlockedStateText.text = unlockedText;
            unlockedStateText.color = progress != null && progress.isUnlocked
                ? new Color(0.22f, 0.46f, 0.27f, 1f)
                : new Color(0.55f, 0.15f, 0.15f, 1f);

            Text descText = CreateText("DescriptionText", rect, new Vector2(18f, -142f), new Vector2(440f, 40f), 16, FontStyle.Italic, TextAnchor.UpperLeft);
            descText.text = hero.description;
            descText.color = new Color(0.28f, 0.25f, 0.22f, 1f);

            string cardsSummary = progressionManager != null ? progressionManager.GetLevelThreeUnlockSummary(hero.heroId) : "Unlocked Card: Locked until Level 3";
            string perkSummary = progressionManager != null ? progressionManager.GetPerkSummary(hero.heroId) : "Level 5 Perk: Locked until Level 5";

            Text cardsText = CreateText("CardsText", rect, new Vector2(520f, -24f), new Vector2(620f, 58f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            cardsText.text = cardsSummary;
            cardsText.color = new Color(0.18f, 0.15f, 0.12f, 1f);
            Text perkText = CreateText("PerkText", rect, new Vector2(520f, -92f), new Vector2(620f, 64f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            perkText.text = perkSummary;
            perkText.color = new Color(0.22f, 0.22f, 0.26f, 1f);
        }

        private void EnsureRuntimeUi()
        {
            RectTransform root = screenRoot;
            if (root == null)
            {
                root = transform as RectTransform;
            }

            if (root == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvas = canvasObject.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                    CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920f, 1080f);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                }

                GameObject rootObject = CreateUiObject("HeroProgressionRoot", canvas.transform as RectTransform, Vector2.zero);
                root = rootObject.GetComponent<RectTransform>();
                StretchFull(root, 24f);
            }

            screenRoot = root;
            EnsureBackground(root);

            if (titleText == null)
            {
                titleText = CreateText("Title", root, new Vector2(28f, -28f), new Vector2(700f, 40f), 38, FontStyle.Bold, TextAnchor.UpperLeft);
                titleText.text = "Hero Progression";
                titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            }

            if (subtitleText == null)
            {
                subtitleText = CreateText("Subtitle", root, new Vector2(30f, -74f), new Vector2(760f, 28f), 20, FontStyle.Italic, TextAnchor.UpperLeft);
                subtitleText.text = "Level up heroes and unlock new cards.";
                subtitleText.color = new Color(0.84f, 0.86f, 0.80f, 1f);
            }

            if (infoMessageText == null)
            {
                infoMessageText = CreateText("InfoMessage", root, new Vector2(30f, -108f), new Vector2(700f, 28f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
                infoMessageText.color = new Color(0.86f, 0.56f, 0.46f, 1f);
            }

            if (heroListContainer == null)
            {
                GameObject panel = CreatePanel("ListPanel", root, new Color(0.18f, 0.16f, 0.13f, 0.72f));
                RectTransform panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(1f, 1f);
                panelRect.offsetMin = new Vector2(24f, 96f);
                panelRect.offsetMax = new Vector2(-24f, -108f);

                GameObject viewport = CreatePanel("Viewport", panelRect, new Color(0f, 0f, 0f, 0.08f));
                RectTransform viewportRect = viewport.GetComponent<RectTransform>();
                StretchFull(viewportRect, 16f);
                viewport.AddComponent<Mask>().showMaskGraphic = false;

                ScrollRect scrollRect = panel.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.viewport = viewportRect;

                GameObject listObject = CreateUiObject("HeroListContainer", viewportRect, Vector2.zero);
                RectTransform listRect = listObject.GetComponent<RectTransform>();
                listRect.anchorMin = new Vector2(0f, 1f);
                listRect.anchorMax = new Vector2(1f, 1f);
                listRect.pivot = new Vector2(0.5f, 1f);
                listRect.anchoredPosition = Vector2.zero;
                listRect.sizeDelta = new Vector2(-12f, 0f);

                VerticalLayoutGroup layout = listObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                ContentSizeFitter fitter = listObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.content = listRect;

                heroListContainer = listObject.transform;
            }

            if (backButton == null)
            {
                backButton = CreateButton("BackButton", root, "Back", new Vector2(180f, 54f));
                RectTransform rect = backButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(24f, 24f);
            }

            if (resetProgressionDebugButton == null)
            {
                resetProgressionDebugButton = CreateButton("ResetProgressionDebugButton", root, "DEBUG Reset Hero Progression", new Vector2(320f, 54f));
                RectTransform rect = resetProgressionDebugButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-24f, 24f);
            }

            titleText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            subtitleText.color = new Color(0.84f, 0.86f, 0.80f, 1f);
            StyleButton(backButton, new Color(0.47f, 0.35f, 0.18f, 1f), false);
            StyleButton(resetProgressionDebugButton, new Color(0.52f, 0.22f, 0.20f, 1f), true);
        }

        private void SetInfo(string message)
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = message;
            }
        }

        private GameObject CreateUiObject(string objectName, RectTransform parent, Vector2 size)
        {
            GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return gameObject;
        }

        private GameObject CreatePanel(string objectName, RectTransform parent, Color color)
        {
            GameObject panel = CreateUiObject(objectName, parent, Vector2.zero);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private void EnsureBackground(RectTransform root)
        {
            Image image = root.GetComponent<Image>();
            if (image == null)
            {
                image = root.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.10f, 0.15f, 0.12f, 1f);
        }

        private void CreateXpBar(RectTransform parent, int xp, int level, int nextTarget)
        {
            GameObject barRoot = CreateUiObject("XpBarRoot", parent, new Vector2(260f, 10f));
            Image bg = barRoot.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.20f, 0.16f, 1f);
            RectTransform rect = barRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(18f, -102f);

            GameObject fill = CreateUiObject("XpBarFill", rect, Vector2.zero);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.40f, 0.72f, 0.56f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = GetXpFill(level, xp, nextTarget);
        }

        private float GetXpFill(int level, int xp, int nextTarget)
        {
            int previousThreshold = 0;
            switch (Mathf.Clamp(level, 1, 5))
            {
                case 2: previousThreshold = 25; break;
                case 3: previousThreshold = 60; break;
                case 4: previousThreshold = 110; break;
                case 5: previousThreshold = 180; break;
            }

            if (level >= 5 || nextTarget <= previousThreshold)
            {
                return 1f;
            }

            return Mathf.Clamp01((float)(xp - previousThreshold) / (nextTarget - previousThreshold));
        }

        private Button CreateButton(string objectName, RectTransform parent, string label, Vector2 size)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent, size);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.47f, 0.35f, 0.18f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.46f, 0.65f, 0.41f, 1f);
            colors.pressedColor = new Color(0.26f, 0.45f, 0.21f, 1f);
            button.colors = colors;

            Text labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), Vector2.zero, size, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(labelText.rectTransform, 0f);
            labelText.text = label;
            labelText.color = new Color(0.98f, 0.95f, 0.86f, 1f);
            return button;
        }

        private void StyleButton(Button button, Color normalColor, bool isDanger)
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
            colors.highlightedColor = normalColor * 1.12f;
            colors.pressedColor = normalColor * 0.9f;
            colors.disabledColor = new Color(0.40f, 0.40f, 0.40f, 0.85f);
            button.colors = colors;

            Text label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = isDanger
                    ? new Color(1f, 0.93f, 0.90f, 1f)
                    : new Color(0.98f, 0.95f, 0.86f, 1f);
            }
        }

        private Text CreateText(string objectName, RectTransform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(objectName, parent, size);
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

        private void StretchFull(RectTransform rectTransform, float padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
