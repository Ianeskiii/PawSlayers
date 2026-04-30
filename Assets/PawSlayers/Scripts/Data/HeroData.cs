using UnityEngine;

namespace PawSlayers
{
    [CreateAssetMenu(menuName = "Paw Slayers/Hero Data", fileName = "HeroData")]
    public class HeroData : ScriptableObject
    {
        public HeroId heroId;
        public string heroName;
        public HeroClass heroClass;
        public int maxHp = 40;
        [TextArea(2, 4)]
        public string description;
        public Sprite portrait;
    }
}
