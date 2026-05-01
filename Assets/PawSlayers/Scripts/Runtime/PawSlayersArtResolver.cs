using System.Collections.Generic;
using UnityEngine;

namespace PawSlayers
{
    public static class PawSlayersArtResolver
    {
        private static readonly HashSet<string> MissingWarnings = new HashSet<string>();

        public static Sprite GetHeroPortraitSprite(HeroData heroData)
        {
            if (heroData == null)
            {
                return null;
            }

            if (heroData.portraitSprite != null)
            {
                return heroData.portraitSprite;
            }

            if (heroData.portrait != null)
            {
                return heroData.portrait;
            }

            string resourcePath = "Art/HeroPortraits/" + BuildHeroFileStem(heroData) + "_portrait";
            return LoadSpriteOrWarn(
                "hero-portrait-" + heroData.heroId,
                resourcePath,
                "Missing hero portrait for " + heroData.heroName + ". Using placeholder."
            );
        }

        public static Sprite GetHeroBattleSprite(HeroData heroData)
        {
            if (heroData == null)
            {
                return null;
            }

            if (heroData.battleSprite != null)
            {
                return heroData.battleSprite;
            }

            string resourcePath = "Art/Heroes/" + BuildHeroFileStem(heroData) + "_battle";
            return LoadSpriteOrWarn(
                "hero-battle-" + heroData.heroId,
                resourcePath,
                "Missing hero battle sprite for " + heroData.heroName + ". Using placeholder."
            );
        }

        public static Sprite GetEnemyBattleSprite(EnemyArtDatabase enemyArtDatabase, string enemyId, bool isBoss)
        {
            if (enemyArtDatabase != null)
            {
                Sprite databaseSprite = enemyArtDatabase.GetBattleSprite(enemyId);
                if (databaseSprite != null)
                {
                    return databaseSprite;
                }
            }

            string folder = isBoss ? "Art/Bosses/" : "Art/Enemies/";
            return LoadSpriteOrWarn(
                "enemy-battle-" + enemyId,
                folder + enemyId + "_battle",
                "Missing enemy sprite for " + enemyId + ". Using placeholder."
            );
        }

        private static string BuildHeroFileStem(HeroData heroData)
        {
            return heroData.heroId.ToString().ToLowerInvariant() + "_" +
                   heroData.heroClass.ToString().ToLowerInvariant();
        }

        private static Sprite LoadSpriteOrWarn(string warningKey, string resourcePath, string warningMessage)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                return sprite;
            }

            if (MissingWarnings.Add(warningKey))
            {
                Debug.LogWarning(warningMessage + " Checked Resources path: " + resourcePath);
            }

            return null;
        }
    }
}