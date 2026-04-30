using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PawSlayers
{
    [CreateAssetMenu(menuName = "Paw Slayers/Hero Database", fileName = "HeroDatabase")]
    public class HeroDatabase : ScriptableObject
    {
        public List<HeroData> heroes = new List<HeroData>();

        public HeroData GetHero(HeroId heroId)
        {
            return heroes.FirstOrDefault(hero => hero != null && hero.heroId == heroId);
        }
    }
}
