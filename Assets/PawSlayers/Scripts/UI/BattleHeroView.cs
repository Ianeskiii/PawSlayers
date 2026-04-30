using System;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class BattleHeroView : MonoBehaviour
    {
        public Text heroNameText;
        public Text heroClassText;
        public Text hpText;
        public Text blockText;
        public Text stateText;
        public Image portraitImage;
        public Image backgroundImage;
        public Button button;
        public Outline highlightOutline;

        private RuntimeHeroState heroState;
        private Action<RuntimeHeroState> onClicked;

        public RuntimeHeroState HeroState => heroState;

        public void Setup(Action<RuntimeHeroState> clickAction)
        {
            onClicked = clickAction;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        public void Refresh(RuntimeHeroState heroState)
        {
            if (heroState == null || heroState.heroData == null)
            {
                return;
            }

            this.heroState = heroState;

            heroNameText.text = heroState.heroData.heroName;
            heroClassText.text = heroState.heroData.heroClass.ToString();
            hpText.text = $"HP: {heroState.currentHp}/{heroState.heroData.maxHp}";
            blockText.text = $"Block: {heroState.block}";
            stateText.text = $"Status: {(heroState.IsAlive ? "Alive" : "Down")}";

            portraitImage.sprite = heroState.heroData.portrait;
            portraitImage.enabled = true;
            portraitImage.color = heroState.heroData.portrait != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

            if (backgroundImage != null)
            {
                backgroundImage.color = heroState.IsAlive ? new Color(0.86f, 0.93f, 0.86f, 1f) : new Color(0.55f, 0.55f, 0.55f, 1f);
            }
        }

        public void SetTargetHighlight(bool isHighlighted)
        {
            if (highlightOutline != null)
            {
                highlightOutline.enabled = isHighlighted;
            }
        }

        private void HandleClick()
        {
            onClicked?.Invoke(heroState);
        }
    }
}
