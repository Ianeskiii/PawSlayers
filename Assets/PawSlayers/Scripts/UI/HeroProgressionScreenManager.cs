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
        public Transform heroListContainer;
        public Button backButton;
        public Button resetProgressionDebugButton;
        public Text infoMessageText;

        private void Start()
        {
            runManager = ResolveRunManager();
            runManager.EnsurePrototypeData();

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
            GameObject panel = CreatePanel($"{hero.heroName}Entry", heroListContainer as RectTransform, new Color(0.93f, 0.91f, 0.84f, 1f));
            LayoutElement layoutElement = panel.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 170f;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 170f);

            if (progress != null && !progress.isUnlocked)
            {
                panel.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1f);
            }

            CreateText("HeroName", rect, new Vector2(16f, -14f), new Vector2(420f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft).text =
                $"{hero.heroName} - {hero.heroClass}";

            int level = progress != null ? progress.level : 1;
            int xp = progress != null ? progress.xp : 0;
            int nextTarget = progressionManager != null ? progressionManager.GetNextLevelXpTarget(hero.heroId) : 25;
            string unlockedText = progress != null && progress.isUnlocked ? "Unlocked" : "Locked";

            CreateText("LevelText", rect, new Vector2(16f, -46f), new Vector2(320f, 22f), 18, FontStyle.Bold, TextAnchor.UpperLeft).text = $"Level {level}";
            CreateText("XpText", rect, new Vector2(16f, -70f), new Vector2(320f, 22f), 17, FontStyle.Normal, TextAnchor.UpperLeft).text = level >= 5 ? $"XP: {xp} / Max" : $"XP: {xp} / {nextTarget}";
            CreateText("UnlockedText", rect, new Vector2(16f, -94f), new Vector2(320f, 22f), 17, FontStyle.Normal, TextAnchor.UpperLeft).text = unlockedText;
            CreateText("DescriptionText", rect, new Vector2(16f, -118f), new Vector2(520f, 38f), 16, FontStyle.Italic, TextAnchor.UpperLeft).text = hero.description;

            string cardsSummary = progressionManager != null ? progressionManager.GetUnlockedCardsSummary(hero.heroId) : "Unlocked Cards: None yet";
            string perkSummary = progressionManager != null ? progressionManager.GetPerkSummary(hero.heroId) : "Perk: Locked until Level 5";

            CreateText("CardsText", rect, new Vector2(580f, -20f), new Vector2(640f, 48f), 16, FontStyle.Normal, TextAnchor.UpperLeft).text = cardsSummary;
            CreateText("PerkText", rect, new Vector2(580f, -74f), new Vector2(640f, 28f), 16, FontStyle.Normal, TextAnchor.UpperLeft).text = perkSummary;
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

            if (titleText == null)
            {
                titleText = CreateText("Title", root, new Vector2(24f, -24f), new Vector2(700f, 40f), 34, FontStyle.Bold, TextAnchor.UpperLeft);
                titleText.text = "Hero Progression";
            }

            if (infoMessageText == null)
            {
                infoMessageText = CreateText("InfoMessage", root, new Vector2(24f, -74f), new Vector2(700f, 28f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
                infoMessageText.color = new Color(0.55f, 0.16f, 0.16f, 1f);
            }

            if (heroListContainer == null)
            {
                GameObject listObject = CreateUiObject("HeroListContainer", root, Vector2.zero);
                RectTransform listRect = listObject.GetComponent<RectTransform>();
                listRect.anchorMin = new Vector2(0f, 0f);
                listRect.anchorMax = new Vector2(1f, 1f);
                listRect.offsetMin = new Vector2(24f, 96f);
                listRect.offsetMax = new Vector2(-24f, -108f);

                VerticalLayoutGroup layout = listObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                heroListContainer = listObject.transform;
            }

            if (backButton == null)
            {
                backButton = CreateButton("BackButton", root, "Back to Main Menu", new Vector2(240f, 54f));
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

        private Button CreateButton(string objectName, RectTransform parent, string label, Vector2 size)
        {
            GameObject buttonObject = CreateUiObject(objectName, parent, size);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.36f, 0.55f, 0.31f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.46f, 0.65f, 0.41f, 1f);
            colors.pressedColor = new Color(0.26f, 0.45f, 0.21f, 1f);
            button.colors = colors;

            Text labelText = CreateText("Label", buttonObject.GetComponent<RectTransform>(), Vector2.zero, size, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(labelText.rectTransform, 0f);
            labelText.text = label;
            return button;
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
