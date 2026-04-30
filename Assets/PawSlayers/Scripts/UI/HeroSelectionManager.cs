using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroSelectionManager : MonoBehaviour
    {
        [Header("Dependencies")]
        public RunManager runManager;
        public Transform heroButtonContainer;
        public HeroSelectionButtonView heroButtonPrefab;

        [Header("UI")]
        public Text selectedCountText;
        public Text infoMessageText;
        public Button startRunButton;

        private readonly List<HeroId> selectedHeroIds = new List<HeroId>();
        private readonly Dictionary<HeroId, HeroSelectionButtonView> buttonViews = new Dictionary<HeroId, HeroSelectionButtonView>();

        private void Start()
        {
            if (runManager == null)
            {
                runManager = RunManager.Instance;
            }

            if (runManager == null || runManager.heroDatabase == null)
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
                    SetInfoMessage("You already selected 3 heroes. Deselect one first.");
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

            runManager.StartRunWithSelection(new List<HeroId>(selectedHeroIds));
        }

        private void BuildRosterUi()
        {
            buttonViews.Clear();

            foreach (Transform child in heroButtonContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (HeroData hero in runManager.heroDatabase.heroes)
            {
                if (hero == null || hero.heroId == HeroId.Neutral)
                {
                    continue;
                }

                HeroSelectionButtonView buttonView = Instantiate(heroButtonPrefab, heroButtonContainer);
                buttonView.Setup(hero, this, false);
                buttonViews[hero.heroId] = buttonView;
            }

            startRunButton.onClick.RemoveAllListeners();
            startRunButton.onClick.AddListener(StartRun);
        }

        private void RefreshUi()
        {
            selectedCountText.text = $"Selected: {selectedHeroIds.Count}/3";
            startRunButton.interactable = selectedHeroIds.Count == 3;

            foreach (KeyValuePair<HeroId, HeroSelectionButtonView> pair in buttonViews)
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
