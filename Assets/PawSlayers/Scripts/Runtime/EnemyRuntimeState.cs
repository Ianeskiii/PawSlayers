using System;

namespace PawSlayers
{
    [Serializable]
    public class EnemyRuntimeState
    {
        public string enemyId;
        public string enemyName;
        public int maxHp;
        public int currentHp;
        public int attackDamage;

        public bool IsAlive
        {
            get { return currentHp > 0; }
        }

        public string IntentText
        {
            get { return IsAlive ? $"Attack {attackDamage}" : "Defeated"; }
        }

        public void Reset()
        {
            currentHp = maxHp;
        }

        public void TakeDamage(int damage)
        {
            currentHp = Math.Max(0, currentHp - Math.Max(0, damage));
        }
    }
}
