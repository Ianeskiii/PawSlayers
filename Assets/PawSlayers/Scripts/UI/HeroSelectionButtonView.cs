using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroSelectionButtonView : MonoBehaviour
    {
        public Text heroNameText;
        public Text heroClassText;
        public Text descriptionText;
        public Image portraitImage;
        public Image selectionOutline;
        public Button button;

        private HeroData heroData;
        private HeroSelectionManager selectionManager;

        public void Setup(HeroData data, HeroSelectionManager manager, bool isSelected)
        {
            heroData = data;
            selectionManager = manager;

            heroNameText.text = data.heroName;
            heroClassText.text = data.heroClass.ToString();
            descriptionText.text = data.description;
            portraitImage.sprite = data.portrait;
            portraitImage.enabled = true;
            portraitImage.color = data.portrait != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
            SetSelected(isSelected);
        }

        public void SetSelected(bool isSelected)
        {
            if (selectionOutline != null)
            {
                selectionOutline.color = isSelected ? new Color(0.2f, 0.9f, 0.4f, 1f) : new Color(0f, 0f, 0f, 0f);
            }
        }

        private void OnClicked()
        {
            if (heroData != null && selectionManager != null)
            {
                selectionManager.ToggleHeroSelection(heroData.heroId);
            }
        }
    }
}
