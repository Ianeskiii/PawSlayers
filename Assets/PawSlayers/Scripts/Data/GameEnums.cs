using System;

namespace PawSlayers
{
    public enum HeroId
    {
        Neutral = 0,
        Capybara = 1,
        Koala = 2,
        Sloth = 3,
        Panda = 4,
        Kangaroo = 5
    }

    public enum HeroClass
    {
        None = 0,
        Swordsman = 1,
        Thief = 2,
        Healer = 3,
        Tank = 4,
        Fighter = 5
    }

    public enum CardType
    {
        Attack = 0,
        Skill = 1,
        Status = 2
    }

    public enum TargetType
    {
        Enemy = 0,
        Ally = 1,
        Self = 2,
        AllEnemies = 3,
        AllAllies = 4,
        None = 5
    }

    public enum MapNodeType
    {
        None = 0,
        Battle = 1,
        Treasure = 2,
        Campfire = 3,
        Boss = 4
    }

    public enum RelicId
    {
        None = 0,
        BambooCharm = 1,
        CozyLeaf = 2,
        LuckyPaw = 3,
        IronSnack = 4,
        SlothTea = 5,
        PandaEmblem = 6,
        ThiefsBell = 7,
        KangarooWraps = 8
    }

    public enum TreasureRewardType
    {
        Relic = 0,
        Gold = 1,
        Card = 2,
        Upgrade = 3,
        Heal = 4
    }

    [Serializable]
    public class RuntimeHeroState
    {
        public HeroData heroData;
        public int currentHp;
        public int block;
        public bool isDown;
        public int bonusMaxHp;
        public StatusEffectState statuses = new StatusEffectState();

        public int MaxHp
        {
            get { return heroData == null ? 0 : heroData.maxHp + bonusMaxHp; }
        }

        public bool IsAlive
        {
            get { return !isDown && currentHp > 0; }
        }

        public void ResetForBattle()
        {
            if (heroData == null)
            {
                return;
            }

            currentHp = MaxHp;
            block = 0;
            isDown = false;
            statuses.ClearAll();
        }

        public void GainBlock(int amount)
        {
            block += Math.Max(0, amount);
        }

        public void Heal(int amount)
        {
            if (heroData == null || amount <= 0)
            {
                return;
            }

            currentHp = Math.Min(MaxHp, currentHp + amount);
            if (currentHp > 0)
            {
                isDown = false;
            }
        }

        public void GainMaxHp(int amount)
        {
            if (heroData == null || amount <= 0)
            {
                return;
            }

            bonusMaxHp += amount;
            currentHp = Math.Min(MaxHp, currentHp + amount);
            if (currentHp > 0)
            {
                isDown = false;
            }
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

        public void TakeDamage(int amount)
        {
            int remainingDamage = Math.Max(0, amount);

            if (block > 0)
            {
                int absorbed = Math.Min(block, remainingDamage);
                block -= absorbed;
                remainingDamage -= absorbed;
            }

            if (remainingDamage <= 0)
            {
                return;
            }

            currentHp = Math.Max(0, currentHp - remainingDamage);
            if (currentHp == 0)
            {
                isDown = true;
            }
        }

        public void TakeDirectDamageIgnoringBlock(int amount)
        {
            int finalDamage = Math.Max(0, amount);
            currentHp = Math.Max(0, currentHp - finalDamage);
            if (currentHp == 0)
            {
                isDown = true;
            }
        }

        public void TickStartOfTurnStatuses(Action<string> logAction = null)
        {
            if (statuses.bleed > 0 && currentHp > 0)
            {
                int bleedAmount = statuses.bleed;
                TakeDirectDamageIgnoringBlock(bleedAmount);
                logAction?.Invoke($"{heroData.heroName} took {bleedAmount} Bleed damage.");
                statuses.bleed = Math.Max(0, statuses.bleed - 1);
            }
        }

        public void TickEndOfTurnStatuses(Action<string> logAction = null)
        {
            if (statuses.poison > 0 && currentHp > 0)
            {
                int poisonAmount = statuses.poison;
                TakeDirectDamageIgnoringBlock(poisonAmount);
                logAction?.Invoke($"{heroData.heroName} took {poisonAmount} Poison damage.");
                statuses.poison = Math.Max(0, statuses.poison - 1);
            }

            statuses.TickEndOfTurnCore();
        }

        public void TickTaunt(Action<string> logAction = null)
        {
            if (statuses.taunt <= 0)
            {
                return;
            }

            statuses.taunt = Math.Max(0, statuses.taunt - 1);
            if (statuses.taunt == 0)
            {
                logAction?.Invoke($"{heroData.heroName}'s Taunt faded.");
            }
        }
    }
}
