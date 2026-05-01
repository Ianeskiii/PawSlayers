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
        public int block;
        public bool isBoss;
        public int currentPhase = 1;
        public bool phaseTwoTriggered;
        public int patternStep;
        public string intentName;
        public string intentDescription;
        public bool intentIsAttack = true;
        public UnityEngine.Sprite battleSprite;
        public StatusEffectState statuses = new StatusEffectState();

        public bool IsAlive
        {
            get { return currentHp > 0; }
        }

        public string IntentText
        {
            get
            {
                if (!IsAlive)
                {
                    return "Defeated";
                }

                if (!string.IsNullOrWhiteSpace(intentName))
                {
                    return intentName;
                }

                return $"Attack {attackDamage}";
            }
        }

        public string PhaseLabel
        {
            get { return isBoss ? $"Phase {currentPhase}" : string.Empty; }
        }

        public void Reset()
        {
            currentHp = maxHp;
            block = 0;
            statuses.ClearAll();
        }

        public void TakeDamage(int damage)
        {
            int remainingDamage = Math.Max(0, damage);

            if (block > 0)
            {
                int absorbed = Math.Min(block, remainingDamage);
                block -= absorbed;
                remainingDamage -= absorbed;
            }

            currentHp = Math.Max(0, currentHp - remainingDamage);
        }

        public void GainBlock(int amount)
        {
            block += Math.Max(0, amount);
        }

        public void ClearBlock()
        {
            block = 0;
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            currentHp = Math.Min(maxHp, currentHp + amount);
        }

        public void AddWeak(int amount) => statuses.AddWeak(amount);
        public void AddVulnerable(int amount) => statuses.AddVulnerable(amount);
        public void AddStrength(int amount) => statuses.AddStrength(amount);
        public void AddBleed(int amount) => statuses.AddBleed(amount);
        public void AddPoison(int amount) => statuses.AddPoison(amount);
        public void AddTaunt(int amount) => statuses.AddTaunt(amount);
        public void AddStun(int amount) => statuses.AddStun(amount);
        public void AddSilence(int amount) => statuses.AddSilence(amount);

        public string GetStatusSummaryText()
        {
            return statuses.GetStatusSummaryText();
        }

        public void TakeDirectDamageIgnoringBlock(int damage)
        {
            currentHp = Math.Max(0, currentHp - Math.Max(0, damage));
        }

        public void TickStartOfTurnStatuses(Action<string> logAction = null)
        {
            if (statuses.bleed > 0 && currentHp > 0)
            {
                int bleedAmount = statuses.bleed;
                TakeDirectDamageIgnoringBlock(bleedAmount);
                logAction?.Invoke($"{enemyName} took {bleedAmount} Bleed damage.");
                statuses.bleed = Math.Max(0, statuses.bleed - 1);
            }
        }

        public void TickEndOfTurnStatuses(Action<string> logAction = null)
        {
            if (statuses.poison > 0 && currentHp > 0)
            {
                int poisonAmount = statuses.poison;
                TakeDirectDamageIgnoringBlock(poisonAmount);
                logAction?.Invoke($"{enemyName} took {poisonAmount} Poison damage.");
                statuses.poison = Math.Max(0, statuses.poison - 1);
            }

            statuses.TickEndOfTurnCore();
        }
    }
}
