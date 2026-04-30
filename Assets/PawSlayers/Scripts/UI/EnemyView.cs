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

        public void Refresh(EnemyRuntimeState enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemyNameText.text = enemy.enemyName;
            hpText.text = $"HP: {enemy.currentHp}/{enemy.maxHp}";
            intentText.text = enemy.IntentText;

            if (backgroundImage != null)
            {
                backgroundImage.color = enemy.IsAlive
                    ? new Color(0.93f, 0.84f, 0.84f, 1f)
                    : new Color(0.55f, 0.55f, 0.55f, 1f);
            }
        }
    }
}
