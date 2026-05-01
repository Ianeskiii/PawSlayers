using System;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class EnemyView : MonoBehaviour
    {
        public Text enemyNameText;
        public Text hpText;
        public Text blockText;
        public Text phaseText;
        public Text intentText;
        public Text intentDescriptionText;
        public Text statusText;
        public Image spriteImage;
        public Image backgroundImage;
        public Image hpBarFillImage;
        public Image dimOverlayImage;
        public Button button;
        public Outline highlightOutline;

        private EnemyRuntimeState enemyState;
        private Action<EnemyRuntimeState> onClicked;

        public EnemyRuntimeState EnemyState => enemyState;

        public void Setup(Action<EnemyRuntimeState> clickAction)
        {
            onClicked = clickAction;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        public void Refresh(EnemyRuntimeState enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemyState = enemy;
            if (enemyNameText != null)
            {
                enemyNameText.text = enemy.enemyName;
            }

            if (hpText != null)
            {
                hpText.text = $"HP: {enemy.currentHp}/{enemy.maxHp}";
            }

            if (blockText != null)
            {
                blockText.text = $"Block: {enemy.block}";
            }

            if (phaseText != null)
            {
                phaseText.text = enemy.isBoss ? enemy.PhaseLabel : string.Empty;
            }

            if (intentText != null)
            {
                intentText.text = $"Intent: {enemy.IntentText}";
            }

            if (intentDescriptionText != null)
            {
                intentDescriptionText.text = enemy.IsAlive ? enemy.intentDescription : string.Empty;
            }

            if (statusText != null)
            {
                statusText.text = enemy.statuses.HasAnyActiveStatus()
                    ? enemy.GetStatusSummaryText()
                    : string.Empty;
            }

            if (spriteImage != null)
            {
                spriteImage.sprite = enemy.battleSprite;
                spriteImage.enabled = true;
                spriteImage.preserveAspect = true;
                spriteImage.color = enemy.battleSprite != null ? Color.white : new Color(0.70f, 0.70f, 0.74f, 1f);

                RectTransform spriteRect = spriteImage.rectTransform;
                if (spriteRect != null)
                {
                    spriteRect.sizeDelta = enemy.isBoss ? new Vector2(108f, 108f) : new Vector2(88f, 88f);
                }
            }

            if (backgroundImage != null)
            {
                if (!enemy.IsAlive)
                {
                    backgroundImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                }
                else if (enemy.isBoss)
                {
                    backgroundImage.color = new Color(0.48f, 0.23f, 0.23f, 1f);
                }
                else
                {
                    backgroundImage.color = new Color(0.48f, 0.37f, 0.30f, 1f);
                }
            }

            if (hpBarFillImage != null)
            {
                hpBarFillImage.fillAmount = enemy.maxHp <= 0 ? 0f : Mathf.Clamp01((float)enemy.currentHp / enemy.maxHp);
            }

            if (dimOverlayImage != null)
            {
                dimOverlayImage.enabled = !enemy.IsAlive;
            }

            if (enemyNameText != null)
            {
                enemyNameText.color = new Color(0.98f, 0.95f, 0.88f, 1f);
            }

            if (hpText != null)
            {
                hpText.color = new Color(1f, 0.86f, 0.86f, 1f);
            }

            if (blockText != null)
            {
                blockText.color = new Color(0.86f, 0.90f, 0.98f, 1f);
            }

            if (phaseText != null)
            {
                phaseText.color = enemy.isBoss
                    ? new Color(1f, 0.80f, 0.60f, 1f)
                    : new Color(0.90f, 0.88f, 0.82f, 1f);
            }

            if (intentText != null)
            {
                intentText.color = new Color(1f, 0.92f, 0.74f, 1f);
            }

            if (intentDescriptionText != null)
            {
                intentDescriptionText.color = new Color(0.96f, 0.94f, 0.89f, 1f);
            }

            if (statusText != null)
            {
                statusText.color = new Color(0.88f, 0.90f, 0.95f, 1f);
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
            onClicked?.Invoke(enemyState);
        }
    }
}
