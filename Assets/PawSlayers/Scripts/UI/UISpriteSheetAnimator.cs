using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class UISpriteSheetAnimator : MonoBehaviour
    {
        public Image targetImage;
        public Sprite idleSprite;
        public Sprite[] frames;
        public float framesPerSecond = 30f;
        public bool playOnAwake;
        public bool loop;

        private Coroutine playRoutine;

        public bool IsPlaying => playRoutine != null;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        private void Start()
        {
            if (playOnAwake)
            {
                PlayOnce();
            }
            else
            {
                RestoreIdle();
            }
        }

        public bool PlayOnce(Sprite[] overrideFrames = null, float overrideFps = -1f, Sprite overrideIdleSprite = null, Action onComplete = null)
        {
            return PlayInternal(overrideFrames, overrideFps, overrideIdleSprite, false, onComplete);
        }

        public bool PlayLoop(Sprite[] overrideFrames = null, float overrideFps = -1f, Sprite overrideIdleSprite = null)
        {
            return PlayInternal(overrideFrames, overrideFps, overrideIdleSprite, true, null);
        }

        public void StopPlayback()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }
        }

        private bool PlayInternal(Sprite[] overrideFrames, float overrideFps, Sprite overrideIdleSprite, bool shouldLoop, Action onComplete)
        {
            if (targetImage == null)
            {
                return false;
            }

            Sprite[] framesToPlay = overrideFrames != null && overrideFrames.Length > 0 ? overrideFrames : frames;
            if (framesToPlay == null || framesToPlay.Length == 0)
            {
                if (overrideIdleSprite != null)
                {
                    idleSprite = overrideIdleSprite;
                }

                RestoreIdle();
                onComplete?.Invoke();
                return false;
            }

            if (overrideIdleSprite != null)
            {
                idleSprite = overrideIdleSprite;
            }

            float fps = overrideFps > 0f ? overrideFps : Mathf.Max(1f, framesPerSecond);

            StopPlayback();

            playRoutine = StartCoroutine(PlayRoutine(framesToPlay, fps, shouldLoop, onComplete));
            return true;
        }

        public void RestoreIdle()
        {
            if (targetImage == null)
            {
                return;
            }

            if (idleSprite != null)
            {
                targetImage.sprite = idleSprite;
            }
        }

        private IEnumerator PlayRoutine(Sprite[] framesToPlay, float fps, bool shouldLoop, Action onComplete)
        {
            float frameDuration = 1f / Mathf.Max(1f, fps);

            do
            {
                for (int index = 0; index < framesToPlay.Length; index++)
                {
                    if (targetImage != null && framesToPlay[index] != null)
                    {
                        targetImage.sprite = framesToPlay[index];
                    }

                    yield return new WaitForSeconds(frameDuration);
                }
            }
            while (shouldLoop || loop);

            playRoutine = null;
            RestoreIdle();
            onComplete?.Invoke();
        }
    }
}
