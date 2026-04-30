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
        Skill = 1
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

    [Serializable]
    public class RuntimeHeroState
    {
        public HeroData heroData;
        public int currentHp;
        public int block;
        public bool isDown;

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

            currentHp = heroData.maxHp;
            block = 0;
            isDown = false;
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

            currentHp = Math.Min(heroData.maxHp, currentHp + amount);
            if (currentHp > 0)
            {
                isDown = false;
            }
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
    }
}
