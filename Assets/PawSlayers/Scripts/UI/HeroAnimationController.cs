using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class HeroAnimationController : MonoBehaviour
    {
        [Header("Targets")]
        public Image heroImage;
        public UISpriteSheetAnimator spriteAnimator;

        [Header("Idle")]
        public Sprite idleSprite;
        public Sprite[] idleFrames;
        public float idleFps = 18;

        [Header("Swift Slash")]
        public Sprite[] swiftSlashFrames;
        public float swiftSlashFps = 18;

        public bool IsPlaying => spriteAnimator != null && spriteAnimator.IsPlaying;

        private HeroId currentHeroId = HeroId.Neutral;
        private bool idleLoopActive;
        private bool missingIdleAnimationWarned;

        private void Awake()
        {
            if (heroImage == null)
            {
                heroImage = GetComponent<Image>();
            }

            if (spriteAnimator == null)
            {
                spriteAnimator = GetComponent<UISpriteSheetAnimator>();
            }

            if (spriteAnimator == null)
            {
                spriteAnimator = gameObject.AddComponent<UISpriteSheetAnimator>();
            }

            spriteAnimator.targetImage = heroImage;
            spriteAnimator.playOnAwake = false;
            spriteAnimator.loop = false;
        }

        public void SetHeroId(HeroId heroId)
        {
            currentHeroId = heroId;
        }

        public void SetIdleSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            idleSprite = sprite;

            if (spriteAnimator != null)
            {
                spriteAnimator.idleSprite = sprite;
                if (!spriteAnimator.IsPlaying)
                {
                    StartIdleAnimation();
                }
            }
            else if (heroImage != null)
            {
                heroImage.sprite = sprite;
            }
        }

        public bool PlayCardAnimation(CardAnimationType animationType)
        {
            switch (animationType)
            {
                case CardAnimationType.SwiftSlash:
                    return PlaySwiftSlash();
                default:
                    ReturnToIdle();
                    return false;
            }
        }

        public bool PlaySwiftSlash()
        {
            if (spriteAnimator == null)
            {
                Debug.LogWarning("Swift Slash animation frames missing. Using fallback.");
                ReturnToIdle();
                return false;
            }

            if (swiftSlashFrames == null || swiftSlashFrames.Length == 0)
            {
                Debug.LogWarning("Swift Slash animation frames missing. Using fallback.");
                ReturnToIdle();
                return false;
            }

            idleLoopActive = false;
            Debug.Log("Playing Swift Slash animation.");
            return spriteAnimator.PlayOnce(swiftSlashFrames, swiftSlashFps, idleSprite, () =>
            {
                Debug.Log("Swift Slash animation finished.");
                StartIdleAnimation();
            });
        }

        public void ReturnToIdle()
        {
            StartIdleAnimation();
        }

        private void StartIdleAnimation()
        {
            if (spriteAnimator == null)
            {
                if (heroImage != null && idleSprite != null)
                {
                    heroImage.sprite = idleSprite;
                }

                return;
            }

            spriteAnimator.idleSprite = idleSprite;

            if (currentHeroId == HeroId.Capybara)
            {
                if (idleFrames != null && idleFrames.Length > 0)
                {
                    idleLoopActive = true;
                    spriteAnimator.PlayLoop(idleFrames, idleFps, idleSprite);
                    return;
                }

                if (!missingIdleAnimationWarned)
                {
                    missingIdleAnimationWarned = true;
                    Debug.LogWarning("Capybara idle animation frames missing. Using static idle sprite.");
                }
            }

            idleLoopActive = false;
            if (spriteAnimator != null)
            {
                spriteAnimator.RestoreIdle();
            }
            else if (heroImage != null && idleSprite != null)
            {
                heroImage.sprite = idleSprite;
            }
        }
    }
}
