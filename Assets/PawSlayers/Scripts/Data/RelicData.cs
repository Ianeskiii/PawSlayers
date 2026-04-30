using UnityEngine;

namespace PawSlayers
{
    [CreateAssetMenu(menuName = "Paw Slayers/Relic Data", fileName = "RelicData")]
    public class RelicData : ScriptableObject
    {
        public RelicId relicId;
        public string relicName;
        [TextArea(2, 4)]
        public string description;
        public string rarity;
        public Sprite icon;
    }
}
