using System;
using UnityEngine;
using UnityEngine.UI;

namespace PawSlayers
{
    public class EnemyView : MonoBehaviour
    {
        public Text enemyNameText;
        public Text hpText;
        public Text intentText;
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
            enemyNameText.text = enemy.enemyName;
            hpText.text = $"HP: {enemy.currentHp}/{enemy.maxHp}";
            intentText.text = $"Intent: {enemy.IntentText}";

            if (backgroundImage != null)
            {
                backgroundImage.color = enemy.IsAlive
                    ? new Color(0.93f, 0.84f, 0.84f, 1f)
                    : new Color(0.55f, 0.55f, 0.55f, 1f);
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
