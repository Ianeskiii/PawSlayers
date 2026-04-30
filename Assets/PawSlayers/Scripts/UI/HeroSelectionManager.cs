using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroSelectionManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public RunManager runManager;
        public Transform heroCardContainer;
        public HeroSelectionCardView heroCardPrefab;

        [Header("UI")]
        public Text selectedCountText;
        public Text infoMessageText;
        public Button startRunButton;

        private readonly List<HeroId> selectedHeroIds = new List<HeroId>();
        private readonly Dictionary<HeroId, HeroSelectionCardView> cardViews = new Dictionary<HeroId, HeroSelectionCardView>();

        private void Start()
        {
            if (runManager == null)
            {
                runManager = RunManager.Instance;
            }

            if (runManager == null)
            {
                GameObject runManagerObject = new GameObject("RunManager");
                runManager = runManagerObject.AddComponent<RunManager>();
                runManagerObject.AddComponent<DeckManager>();
            }

            runManager.EnsurePrototypeData();

            if (runManager.heroDatabase == null)
            {
                if (infoMessageText != null)
                {
                    infoMessageText.text = "RunManager or HeroDatabase is missing.";
                }

                if (startRunButton != null)
                {
                    startRunButton.interactable = false;
                }

                return;
            }

            BuildRosterUi();
            RefreshUi();
        }

        public void ToggleHeroSelection(HeroId heroId)
        {
            if (selectedHeroIds.Contains(heroId))
            {
                selectedHeroIds.Remove(heroId);
                SetInfoMessage(string.Empty);
            }
            else
            {
                if (selectedHeroIds.Count >= 3)
                {
                    SetInfoMessage("Max 3 heroes per run.");
                    RefreshUi();
                    return;
                }

                selectedHeroIds.Add(heroId);
                SetInfoMessage(string.Empty);
            }

            RefreshUi();
        }

        public void StartRun()
        {
            if (selectedHeroIds.Count != 3 || runManager == null)
            {
                return;
            }

            Debug.Log("Selected hero count: " + selectedHeroIds.Count);
            Debug.Log("Selected hero IDs: " + string.Join(", ", selectedHeroIds));
            Debug.Log("Loading BattleScene");
            runManager.StartRunWithSelection(new List<HeroId>(selectedHeroIds));
        }

        private void BuildRosterUi()
        {
            cardViews.Clear();

            foreach (Transform child in heroCardContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (HeroData hero in runManager.heroDatabase.heroes)
            {
                if (hero == null || hero.heroId == HeroId.Neutral)
                {
                    continue;
                }

                HeroSelectionCardView cardView = heroCardPrefab != null
                    ? Instantiate(heroCardPrefab, heroCardContainer)
                    : null;

                if (cardView == null)
                {
                    continue;
                }

                cardView.Setup(hero, this, false);
                cardViews[hero.heroId] = cardView;
            }

            startRunButton.onClick.RemoveAllListeners();
            startRunButton.onClick.AddListener(StartRun);
        }

        private void RefreshUi()
        {
            selectedCountText.text = $"Selected: {selectedHeroIds.Count}/3";
            startRunButton.interactable = selectedHeroIds.Count == 3;
            Debug.Log("Selected hero count: " + selectedHeroIds.Count);

            foreach (KeyValuePair<HeroId, HeroSelectionCardView> pair in cardViews)
            {
                pair.Value.SetSelected(selectedHeroIds.Contains(pair.Key));
            }
        }

        private void SetInfoMessage(string message)
        {
            if (infoMessageText != null)
            {
                infoMessageText.text = message;
            }
        }
    }
}
