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
        public Image backgroundImage;
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

            if (backgroundImage != null)
            {
                if (!enemy.IsAlive)
                {
                    backgroundImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                }
                else if (enemy.isBoss)
                {
                    backgroundImage.color = new Color(0.77f, 0.9f, 0.79f, 1f);
                }
                else
                {
                    backgroundImage.color = new Color(0.93f, 0.84f, 0.84f, 1f);
                }
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
