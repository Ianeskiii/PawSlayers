using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PawSlayers
{
    [CreateAssetMenu(menuName = "Paw Slayers/Enemy Art Database", fileName = "EnemyArtDatabase")]
    public class EnemyArtDatabase : ScriptableObject
    {
        [Serializable]
        public class EnemyArtEntry
        {
            public string enemyId;
            public Sprite battleSprite;
        }

        public List<EnemyArtEntry> enemies = new List<EnemyArtEntry>();

        public Sprite GetBattleSprite(string enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId) || enemies == null)
            {
                return null;
            }

            EnemyArtEntry entry = enemies.FirstOrDefault(candidate =>
                candidate != null &&
                !string.IsNullOrWhiteSpace(candidate.enemyId) &&
                string.Equals(candidate.enemyId, enemyId, StringComparison.OrdinalIgnoreCase));

            return entry != null ? entry.battleSprite : null;
        }

        public void SetBattleSprite(string enemyId, Sprite sprite)
        {
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                return;
            }

            if (enemies == null)
            {
                enemies = new List<EnemyArtEntry>();
            }

            EnemyArtEntry entry = enemies.FirstOrDefault(candidate =>
                candidate != null &&
                !string.IsNullOrWhiteSpace(candidate.enemyId) &&
                string.Equals(candidate.enemyId, enemyId, StringComparison.OrdinalIgnoreCase));

            if (entry == null)
            {
                entry = new EnemyArtEntry { enemyId = enemyId };
                enemies.Add(entry);
            }

            entry.battleSprite = sprite;
        }
    }
}
