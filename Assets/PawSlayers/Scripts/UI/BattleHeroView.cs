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
        public Text statusText;
        public Text tauntText;
        public Image portraitImage;
        public Image backgroundImage;
        public Image hpBarFillImage;
        public Image dimOverlayImage;
        public Button button;
        public Outline highlightOutline;
        public HeroAnimationController heroAnimationController;

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
            hpText.text = $"HP: {heroState.currentHp}/{heroState.MaxHp}";
            blockText.text = $"Block: {heroState.block}";
            stateText.text = $"Status: {(heroState.IsAlive ? "Alive" : "Down")}";
            if (tauntText != null)
            {
                tauntText.text = heroState.statuses.taunt > 0 ? $"Taunt {heroState.statuses.taunt}" : string.Empty;
            }

            if (statusText != null)
            {
                statusText.text = heroState.statuses.HasAnyActiveStatus()
                    ? heroState.GetStatusSummaryText()
                    : string.Empty;
            }

            if (portraitImage != null)
            {
                Sprite heroSprite = PawSlayersArtResolver.GetHeroBattleSprite(heroState.heroData);
                portraitImage.enabled = true;
                portraitImage.preserveAspect = true;
                portraitImage.color = heroSprite != null ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

                if (heroAnimationController != null)
                {
                    heroAnimationController.heroImage = portraitImage;
                    heroAnimationController.SetHeroId(heroState.heroData.heroId);
                    heroAnimationController.SetIdleSprite(heroSprite);
                }
                else
                {
                    portraitImage.sprite = heroSprite;
                }
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = heroState.IsAlive ? new Color(0.92f, 0.88f, 0.78f, 1f) : new Color(0.42f, 0.42f, 0.42f, 1f);
            }

            if (hpBarFillImage != null)
            {
                hpBarFillImage.fillAmount = heroState.MaxHp <= 0 ? 0f : Mathf.Clamp01((float)heroState.currentHp / heroState.MaxHp);
            }

            if (dimOverlayImage != null)
            {
                dimOverlayImage.enabled = !heroState.IsAlive;
            }

            if (heroNameText != null)
            {
                heroNameText.color = new Color(0.17f, 0.14f, 0.11f, 1f);
            }

            if (heroClassText != null)
            {
                heroClassText.color = new Color(0.36f, 0.28f, 0.20f, 1f);
            }

            if (hpText != null)
            {
                hpText.color = new Color(0.68f, 0.14f, 0.14f, 1f);
            }

            if (blockText != null)
            {
                blockText.color = new Color(0.25f, 0.38f, 0.57f, 1f);
            }

            if (stateText != null)
            {
                stateText.color = heroState.IsAlive
                    ? new Color(0.18f, 0.46f, 0.21f, 1f)
                    : new Color(0.45f, 0.15f, 0.15f, 1f);
            }

            if (statusText != null)
            {
                statusText.color = new Color(0.24f, 0.22f, 0.20f, 1f);
            }
        }

        public void SetTargetHighlight(bool isHighlighted)
        {
            if (highlightOutline != null)
            {
                highlightOutline.enabled = isHighlighted;
            }
        }

        public bool PlayCardAnimation(CardAnimationType animationType)
        {
            if (heroAnimationController == null)
            {
                return false;
            }

            return heroAnimationController.PlayCardAnimation(animationType);
        }

        private void HandleClick()
        {
            onClicked?.Invoke(heroState);
        }
    }
}
