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

        [Header("Swift Slash")]
        public Sprite[] swiftSlashFrames;
        public float swiftSlashFps = 24;

        public bool IsPlaying => spriteAnimator != null && spriteAnimator.IsPlaying;

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
                    spriteAnimator.RestoreIdle();
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

            Debug.Log("Playing Swift Slash animation.");
            return spriteAnimator.PlayOnce(swiftSlashFrames, swiftSlashFps, idleSprite, () =>
            {
                Debug.Log("Swift Slash animation finished.");
            });
        }

        public void ReturnToIdle()
        {
            if (spriteAnimator != null)
            {
                spriteAnimator.idleSprite = idleSprite;
                spriteAnimator.RestoreIdle();
            }
            else if (heroImage != null && idleSprite != null)
            {
                heroImage.sprite = idleSprite;
            }
        }
    }
}
