#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PawSlayers.EditorTools
{
    public static class PawSlayersPrototypeSetupEditor
    {
        private const string RootFolder = "Assets/PawSlayers";
        private const string DataFolder = RootFolder + "/Data";
        private const string HeroDataFolder = DataFolder + "/Heroes";
        private const string CardDataFolder = DataFolder + "/Cards";
        private const string EnemyArtDatabasePath = DataFolder + "/EnemyArtDatabase.asset";
        private const string PrefabFolder = RootFolder + "/Prefabs";
        private const string SceneFolder = RootFolder + "/Scenes";
        private const string ArtFolder = RootFolder + "/Art";
        private const string ResourcesFolder = RootFolder + "/Resources";
        private const string ResourcesArtFolder = ResourcesFolder + "/Art";
        private const string HeroArtFolder = ArtFolder + "/Heroes";
        private const string HeroPortraitArtFolder = ArtFolder + "/HeroPortraits";
        private const string EnemyArtFolder = ArtFolder + "/Enemies";
        private const string BossArtFolder = ArtFolder + "/Bosses";
        private const string PlaceholderArtFolder = ArtFolder + "/Placeholders";

        [MenuItem("Tools/Paw Slayers/Generate Prototype Setup")]
        public static void GeneratePrototypeSetup()
        {
            EnsureFolders();

            HeroDatabase heroDatabase = CreateHeroDatabase();
            CardDatabase cardDatabase = CreateCardDatabase();
            EnemyArtDatabase enemyArtDatabase = CreateEnemyArtDatabase();

            HeroSelectionCardView heroCardPrefab = CreateHeroSelectionCardPrefab();
            BattleHeroView heroViewPrefab = CreateBattleHeroViewPrefab();
            EnemyView enemyViewPrefab = CreateEnemyViewPrefab();
            CardView cardViewPrefab = CreateCardViewPrefab();

            CreateMainMenuScene(heroDatabase, cardDatabase);
            CreateHeroSelectionScene(heroDatabase, cardDatabase, heroCardPrefab);
            CreateBattleScene(heroDatabase, cardDatabase, enemyArtDatabase, cardViewPrefab, heroViewPrefab, enemyViewPrefab);
            CreateMapScene(cardViewPrefab);
            CreateHeroProgressionScene(heroDatabase, cardDatabase);
            AddScenesToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Paw Slayers", "Prototype setup generated.\n\nScenes:\n- MainMenu\n- HeroSelection\n- Battle\n- Map\n- HeroProgression", "Nice");
        }

        [MenuItem("Tools/Paw Slayers/Assign Art From Resources")]
        public static void AssignArtFromResources()
        {
            EnsureFolders();

            EnemyArtDatabase enemyArtDatabase = CreateEnemyArtDatabase();
            AssignHeroSpritesFromProjectFolders();

            AssignEnemyResourceSprite(enemyArtDatabase, "sporeling", "Assets/PawSlayers/Resources/Art/Enemies/sporeling_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "fungus_brute", "Assets/PawSlayers/Resources/Art/Enemies/fungus_brute_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "batty", "Assets/PawSlayers/Resources/Art/Enemies/batty_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "cave_rat", "Assets/PawSlayers/Resources/Art/Enemies/cave_rat_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "thorn_sprite", "Assets/PawSlayers/Resources/Art/Enemies/thorn_sprite_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "moss_troll", "Assets/PawSlayers/Resources/Art/Enemies/moss_troll_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "crystal_slime", "Assets/PawSlayers/Resources/Art/Enemies/crystal_slime_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "bandit_crow", "Assets/PawSlayers/Resources/Art/Enemies/bandit_crow_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "old_treant", "Assets/PawSlayers/Resources/Art/Enemies/old_treant_battle.png");
            AssignEnemyResourceSprite(enemyArtDatabase, "briar_king", "Assets/PawSlayers/Resources/Art/Bosses/briar_king_battle.png");

            EditorUtility.SetDirty(enemyArtDatabase);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Art assignment pass finished.");
        }

        [MenuItem("Tools/Paw Slayers/Validate Art Assignments")]
        public static void ValidateArtAssignments()
        {
            string[] heroGuids = AssetDatabase.FindAssets("t:HeroData");
            Debug.Log("Validate Art Assignments: found HeroData assets = " + heroGuids.Length);

            foreach (string heroGuid in heroGuids)
            {
                string heroAssetPath = AssetDatabase.GUIDToAssetPath(heroGuid);
                HeroData heroData = AssetDatabase.LoadAssetAtPath<HeroData>(heroAssetPath);

                if (heroData == null)
                {
                    Debug.LogWarning("Validate Art Assignments: failed to load HeroData at " + heroAssetPath);
                    continue;
                }

                Debug.Log(
                    $"HeroData asset found: {heroAssetPath}\n" +
                    $"- Hero: {heroData.heroName} ({heroData.heroId})\n" +
                    $"- battleSprite: {(heroData.battleSprite != null ? AssetDatabase.GetAssetPath(heroData.battleSprite) : "EMPTY")}\n" +
                    $"- portraitSprite: {(heroData.portraitSprite != null ? AssetDatabase.GetAssetPath(heroData.portraitSprite) : "EMPTY")}\n" +
                    $"- portrait: {(heroData.portrait != null ? AssetDatabase.GetAssetPath(heroData.portrait) : "EMPTY")}");
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "PawSlayers");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder, "Scenes");
            EnsureFolder(DataFolder, "Heroes");
            EnsureFolder(DataFolder, "Cards");
            EnsureFolder(RootFolder, "Scripts");
            EnsureFolder(RootFolder, "Art");
            EnsureFolder(RootFolder, "Resources");
            EnsureFolder(ResourcesFolder, "Art");
            EnsureFolder(ArtFolder, "Heroes");
            EnsureFolder(ArtFolder, "HeroPortraits");
            EnsureFolder(ArtFolder, "Enemies");
            EnsureFolder(ArtFolder, "Bosses");
            EnsureFolder(ArtFolder, "Placeholders");
            EnsureFolder(ResourcesArtFolder, "Heroes");
            EnsureFolder(ResourcesArtFolder, "HeroPortraits");
            EnsureFolder(ResourcesArtFolder, "Enemies");
            EnsureFolder(ResourcesArtFolder, "Bosses");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static HeroDatabase CreateHeroDatabase()
        {
            List<HeroData> heroAssets = new List<HeroData>
            {
                CreateOrUpdateHero("Capybara", HeroId.Capybara, HeroClass.Swordsman, 42, "Balanced attacker with guard skills."),
                CreateOrUpdateHero("Koala", HeroId.Koala, HeroClass.Thief, 34, "Fast strikes, weak effects, card draw."),
                CreateOrUpdateHero("Sloth", HeroId.Sloth, HeroClass.Healer, 36, "Healing and support."),
                CreateOrUpdateHero("Panda", HeroId.Panda, HeroClass.Tank, 50, "Block, taunt, protection."),
                CreateOrUpdateHero("Kangaroo", HeroId.Kangaroo, HeroClass.Fighter, 40, "Combo damage and strength.")
            };

            HeroDatabase database = LoadOrCreateAsset<HeroDatabase>(DataFolder + "/HeroDatabase.asset");
            database.heroes = heroAssets;
            EditorUtility.SetDirty(database);
            return database;
        }

        private static HeroData CreateOrUpdateHero(string heroName, HeroId heroId, HeroClass heroClass, int maxHp, string description)
        {
            string path = $"{HeroDataFolder}/{heroName}.asset";
            HeroData hero = LoadOrCreateAsset<HeroData>(path);
            hero.heroName = heroName;
            hero.heroId = heroId;
            hero.heroClass = heroClass;
            hero.maxHp = maxHp;
            hero.description = description;
            EditorUtility.SetDirty(hero);
            return hero;
        }

        private static CardDatabase CreateCardDatabase()
        {
            List<CardData> cardAssets = new List<CardData>
            {
                CreateOrUpdateCard("swift_slash", "Swift Slash", "Deal 8 damage.", HeroId.Capybara, CardType.Attack, TargetType.Enemy, 1, damage: 8, upgradedDamage: 11),
                CreateOrUpdateCard("guard_stance", "Guard Stance", "Gain 8 block.", HeroId.Capybara, CardType.Skill, TargetType.Self, 1, block: 8, upgradedBlock: 12),
                CreateOrUpdateCard("pommel_tap", "Pommel Tap", "Deal 5 damage. Apply 1 Stun.", HeroId.Capybara, CardType.Attack, TargetType.Enemy, 1, damage: 5, stunAmount: 1, upgradedDamage: 7, upgradedStunAmount: 1),
                CreateOrUpdateCard("shadow_strike", "Shadow Strike", "Deal 7 damage. Apply 1 Weak.", HeroId.Koala, CardType.Attack, TargetType.Enemy, 1, damage: 7, weakAmount: 1, upgradedDamage: 10, upgradedWeakAmount: 2),
                CreateOrUpdateCard("smoke_step", "Smoke Step", "Gain 6 block. Draw 1 card.", HeroId.Koala, CardType.Skill, TargetType.Self, 1, block: 6, drawAmount: 1, upgradedBlock: 9, upgradedDrawAmount: 1),
                CreateOrUpdateCard("muzzle_trick", "Muzzle Trick", "Apply 1 Silence. Draw 1 card.", HeroId.Koala, CardType.Skill, TargetType.Enemy, 1, drawAmount: 1, silenceAmount: 1, upgradedDrawAmount: 1, upgradedSilenceAmount: 2),
                CreateOrUpdateCard("staff_tap", "Staff Tap", "Deal 5 damage.", HeroId.Sloth, CardType.Attack, TargetType.Enemy, 1, damage: 5, upgradedDamage: 8),
                CreateOrUpdateCard("soothing_light", "Soothing Light", "Heal 8 HP.", HeroId.Sloth, CardType.Skill, TargetType.Ally, 1, heal: 8, upgradedHeal: 12),
                CreateOrUpdateCard("quiet_blessing", "Quiet Blessing", "Heal 6 HP.", HeroId.Sloth, CardType.Skill, TargetType.Ally, 1, heal: 6, upgradedHeal: 9),
                CreateOrUpdateCard("shield_bash", "Shield Bash", "Deal 6 damage. Gain 4 block.", HeroId.Panda, CardType.Attack, TargetType.Enemy, 1, damage: 6, block: 4, upgradedDamage: 9, upgradedBlock: 7),
                CreateOrUpdateCard("barkskin_guard", "Barkskin Guard", "Gain 16 block. Gain 1 Taunt.", HeroId.Panda, CardType.Skill, TargetType.Self, 2, block: 16, tauntAmount: 1, upgradedBlock: 22, upgradedTauntAmount: 2),
                CreateOrUpdateCard("power_combo", "Power Combo", "Deal 12 damage.", HeroId.Kangaroo, CardType.Attack, TargetType.Enemy, 2, damage: 12, upgradedDamage: 16),
                CreateOrUpdateCard("battle_focus", "Battle Focus", "Gain 2 Strength.", HeroId.Kangaroo, CardType.Skill, TargetType.Self, 1, strengthAmount: 2, upgradedStrengthAmount: 3),
                CreateOrUpdateCard("snack_time", "Snack Time", "Draw 1 card.", HeroId.Neutral, CardType.Skill, TargetType.None, 1, drawAmount: 1, upgradedDrawAmount: 2),
                CreateOrUpdateCard("quick_guard", "Quick Guard", "Gain 5 block.", HeroId.Neutral, CardType.Skill, TargetType.Self, 1, block: 5, upgradedBlock: 8)
            };

            CardDatabase database = LoadOrCreateAsset<CardDatabase>(DataFolder + "/CardDatabase.asset");
            database.cards = cardAssets;
            EditorUtility.SetDirty(database);
            return database;
        }

        private static EnemyArtDatabase CreateEnemyArtDatabase()
        {
            EnemyArtDatabase database = LoadOrCreateAsset<EnemyArtDatabase>(EnemyArtDatabasePath);
            if (database.enemies == null)
            {
                database.enemies = new List<EnemyArtDatabase.EnemyArtEntry>();
            }

            EnsureEnemyArtEntry(database, "sporeling");
            EnsureEnemyArtEntry(database, "fungus_brute");
            EnsureEnemyArtEntry(database, "batty");
            EnsureEnemyArtEntry(database, "cave_rat");
            EnsureEnemyArtEntry(database, "thorn_sprite");
            EnsureEnemyArtEntry(database, "moss_troll");
            EnsureEnemyArtEntry(database, "crystal_slime");
            EnsureEnemyArtEntry(database, "bandit_crow");
            EnsureEnemyArtEntry(database, "old_treant");
            EnsureEnemyArtEntry(database, "briar_king");
            EditorUtility.SetDirty(database);
            return database;
        }

        private static CardData CreateOrUpdateCard(
            string cardId,
            string cardName,
            string description,
            HeroId ownerHeroId,
            CardType cardType,
            TargetType targetType,
            int cost,
            int damage = 0,
            int block = 0,
            int heal = 0,
            int drawAmount = 0,
            int strengthAmount = 0,
            int weakAmount = 0,
            int vulnerableAmount = 0,
            int bleedAmount = 0,
            int poisonAmount = 0,
            int tauntAmount = 0,
            int stunAmount = 0,
            int silenceAmount = 0,
            int upgradedDamage = 0,
            int upgradedBlock = 0,
            int upgradedHeal = 0,
            int upgradedDrawAmount = 0,
            int upgradedStrengthAmount = 0,
            int upgradedWeakAmount = 0,
            int upgradedVulnerableAmount = 0,
            int upgradedBleedAmount = 0,
            int upgradedPoisonAmount = 0,
            int upgradedTauntAmount = 0,
            int upgradedStunAmount = 0,
            int upgradedSilenceAmount = 0)
        {
            string path = $"{CardDataFolder}/{cardName.Replace(" ", string.Empty)}.asset";
            CardData card = LoadOrCreateAsset<CardData>(path);
            card.cardId = cardId;
            card.cardName = cardName;
            card.description = description;
            card.ownerHeroId = ownerHeroId;
            card.cardType = cardType;
            card.targetType = targetType;
            card.cost = cost;
            card.damage = damage;
            card.block = block;
            card.heal = heal;
            card.drawAmount = drawAmount;
            card.strengthAmount = strengthAmount;
            card.weakAmount = weakAmount;
            card.vulnerableAmount = vulnerableAmount;
            card.bleedAmount = bleedAmount;
            card.poisonAmount = poisonAmount;
            card.tauntAmount = tauntAmount;
            card.stunAmount = stunAmount;
            card.silenceAmount = silenceAmount;
            card.upgradedDamage = upgradedDamage;
            card.upgradedBlock = upgradedBlock;
            card.upgradedHeal = upgradedHeal;
            card.upgradedDrawAmount = upgradedDrawAmount;
            card.upgradedStrengthAmount = upgradedStrengthAmount;
            card.upgradedWeakAmount = upgradedWeakAmount;
            card.upgradedVulnerableAmount = upgradedVulnerableAmount;
            card.upgradedBleedAmount = upgradedBleedAmount;
            card.upgradedPoisonAmount = upgradedPoisonAmount;
            card.upgradedTauntAmount = upgradedTauntAmount;
            card.upgradedStunAmount = upgradedStunAmount;
            card.upgradedSilenceAmount = upgradedSilenceAmount;
            EditorUtility.SetDirty(card);
            return card;
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureEnemyArtEntry(EnemyArtDatabase database, string enemyId)
        {
            if (database == null || string.IsNullOrWhiteSpace(enemyId))
            {
                return;
            }

            if (database.enemies.Exists(entry => entry != null && entry.enemyId == enemyId))
            {
                return;
            }

            database.enemies.Add(new EnemyArtDatabase.EnemyArtEntry { enemyId = enemyId });
        }

        private static Sprite LoadSpriteAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return null;
            }

            Debug.LogWarning("Texture found but sprite missing at path: " + assetPath + ". Attempting to switch importer to Sprite (2D and UI).");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("Failed to get TextureImporter for path: " + assetPath);
                return null;
            }

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }

            sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning("Sprite import conversion did not produce a Sprite at path: " + assetPath);
            }
            else
            {
                Debug.Log("Sprite import conversion succeeded: " + assetPath);
            }

            return sprite;
        }

        private static void AssignHeroSpritesFromProjectFolders()
        {
            string[] heroGuids = AssetDatabase.FindAssets("t:HeroData");
            Debug.Log("Assign Art From Resources: found HeroData assets = " + heroGuids.Length);

            foreach (string heroGuid in heroGuids)
            {
                string heroAssetPath = AssetDatabase.GUIDToAssetPath(heroGuid);
                HeroData heroData = AssetDatabase.LoadAssetAtPath<HeroData>(heroAssetPath);

                if (heroData == null)
                {
                    Debug.LogWarning("Assign Art From Resources: failed to load HeroData at " + heroAssetPath);
                    continue;
                }

                Debug.Log("HeroData asset found: " + heroAssetPath);

                string heroStem = heroData.heroId.ToString().ToLowerInvariant() + "_" + heroData.heroClass.ToString().ToLowerInvariant();
                Sprite battleSprite = TryLoadHeroSprite(
                    heroData,
                    "battle",
                    new[]
                    {
                        $"{HeroArtFolder}/{heroStem}_battle.png",
                        $"{ResourcesArtFolder}/Heroes/{heroStem}_battle.png"
                    });

                Sprite portraitSprite = TryLoadHeroSprite(
                    heroData,
                    "portrait",
                    new[]
                    {
                        $"{HeroPortraitArtFolder}/{heroStem}_portrait.png",
                        $"{ResourcesArtFolder}/HeroPortraits/{heroStem}_portrait.png"
                    });

                heroData.battleSprite = battleSprite;
                heroData.portraitSprite = portraitSprite;
                heroData.portrait = portraitSprite;

                EditorUtility.SetDirty(heroData);

                Debug.Log(battleSprite != null
                    ? "Assigned hero battle sprite: " + heroData.heroName + " -> " + AssetDatabase.GetAssetPath(battleSprite)
                    : "Failed to assign hero battle sprite: " + heroData.heroName);

                Debug.Log(portraitSprite != null
                    ? "Assigned hero portrait sprite: " + heroData.heroName + " -> " + AssetDatabase.GetAssetPath(portraitSprite)
                    : "Failed to assign hero portrait sprite: " + heroData.heroName);
            }
        }

        private static Sprite TryLoadHeroSprite(HeroData heroData, string spriteKind, IEnumerable<string> candidatePaths)
        {
            foreach (string candidatePath in candidatePaths)
            {
                Debug.Log($"Checking {spriteKind} sprite path for {heroData.heroName}: {candidatePath}");
                Sprite sprite = LoadSpriteAsset(candidatePath);
                if (sprite != null)
                {
                    Debug.Log($"Found {spriteKind} sprite for {heroData.heroName}: {candidatePath}");
                    return sprite;
                }
            }

            Debug.LogWarning($"Missing {spriteKind} sprite for {heroData.heroName} after checking all candidate paths.");
            return null;
        }

        private static void AssignEnemyResourceSprite(EnemyArtDatabase database, string enemyId, string assetPath)
        {
            if (database == null)
            {
                return;
            }

            Sprite sprite = LoadSpriteAsset(assetPath);
            database.SetBattleSprite(enemyId, sprite);
            Debug.Log(sprite != null
                ? "Assigned enemy sprite: " + enemyId
                : "Missing enemy sprite: " + enemyId);
        }

        private static HeroSelectionCardView CreateHeroSelectionCardPrefab()
        {
            string path = PrefabFolder + "/HeroSelectionCard.prefab";
            DeleteAssetIfExists(path);

            GameObject root = CreateUiObject("HeroSelectionCard", null, new Vector2(390f, 210f));
            Image rootImage = root.AddComponent<Image>();
            rootImage.color = new Color(0.92f, 0.88f, 0.78f, 1f);
            Button button = root.AddComponent<Button>();

            GameObject outlineObject = CreateUiObject("SelectionOutline", root.transform, Vector2.zero);
            StretchFull(outlineObject.GetComponent<RectTransform>(), 0f);
            Image outlineImage = outlineObject.AddComponent<Image>();
            outlineImage.color = new Color(0f, 0f, 0f, 0f);

            GameObject portraitObject = CreateUiObject("Portrait", root.transform, new Vector2(96f, 96f));
            SetAnchoredRect(portraitObject.GetComponent<RectTransform>(), new Vector2(18f, -20f), new Vector2(96f, 96f), TextAnchor.UpperLeft);
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            portraitImage.preserveAspect = true;

            Text heroName = CreateText("HeroName", root.transform, new Vector2(132f, -18f), new Vector2(220f, 26f), 24, FontStyle.Bold, TextAnchor.UpperLeft);
            Text heroClass = CreateText("HeroClass", root.transform, new Vector2(132f, -48f), new Vector2(220f, 22f), 18, FontStyle.Italic, TextAnchor.UpperLeft);
            Text description = CreateText("Description", root.transform, new Vector2(132f, -142f), new Vector2(230f, 44f), 15, FontStyle.Normal, TextAnchor.UpperLeft);

            HeroSelectionCardView view = root.AddComponent<HeroSelectionCardView>();
            view.heroNameText = heroName;
            view.heroClassText = heroClass;
            view.descriptionText = description;
            view.portraitImage = portraitImage;
            view.selectionOutline = outlineImage;
            view.backgroundImage = rootImage;
            view.button = button;

            HeroSelectionCardView prefab = SavePrefab<HeroSelectionCardView>(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static BattleHeroView CreateBattleHeroViewPrefab()
        {
            string path = PrefabFolder + "/BattleHeroView.prefab";
            DeleteAssetIfExists(path);

            GameObject root = CreateUiObject("BattleHeroView", null, new Vector2(260f, 178f));
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.86f, 0.93f, 0.86f, 1f);

            GameObject portraitObject = CreateUiObject("Portrait", root.transform, new Vector2(70f, 70f));
            SetAnchoredRect(portraitObject.GetComponent<RectTransform>(), new Vector2(12f, -12f), new Vector2(70f, 70f), TextAnchor.UpperLeft);
            Image portraitImage = portraitObject.AddComponent<Image>();
            portraitImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            portraitImage.preserveAspect = true;

            Text heroName = CreateText("HeroName", root.transform, new Vector2(92f, -12f), new Vector2(150f, 26f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            Text heroClass = CreateText("HeroClass", root.transform, new Vector2(92f, -38f), new Vector2(150f, 24f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            Text hpText = CreateText("HpText", root.transform, new Vector2(12f, -92f), new Vector2(160f, 20f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            Text blockText = CreateText("BlockText", root.transform, new Vector2(12f, -112f), new Vector2(120f, 20f), 16, FontStyle.Normal, TextAnchor.UpperLeft);
            Text stateText = CreateText("StateText", root.transform, new Vector2(150f, -112f), new Vector2(90f, 20f), 16, FontStyle.Bold, TextAnchor.UpperRight);
            Text statusText = CreateText("StatusText", root.transform, new Vector2(12f, -136f), new Vector2(230f, 30f), 14, FontStyle.Normal, TextAnchor.UpperLeft);

            BattleHeroView view = root.AddComponent<BattleHeroView>();
            view.heroNameText = heroName;
            view.heroClassText = heroClass;
            view.hpText = hpText;
            view.blockText = blockText;
            view.stateText = stateText;
            view.statusText = statusText;
            view.portraitImage = portraitImage;
            view.backgroundImage = background;

            BattleHeroView prefab = SavePrefab<BattleHeroView>(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static CardView CreateCardViewPrefab()
        {
            string path = PrefabFolder + "/CardView.prefab";
            DeleteAssetIfExists(path);

            GameObject root = CreateUiObject("CardView", null, new Vector2(236f, 336f));
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.96f, 0.93f, 0.84f, 1f);
            Button button = root.AddComponent<Button>();
            CanvasGroup canvasGroup = root.AddComponent<CanvasGroup>();
            Outline outline = root.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.80f, 0.27f, 1f);
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = false;

            GameObject ownerAccentObject = CreateUiObject("OwnerAccent", root.transform, Vector2.zero);
            Image ownerAccent = ownerAccentObject.AddComponent<Image>();
            ownerAccent.color = new Color(0.66f, 0.58f, 0.43f, 1f);
            RectTransform ownerAccentRect = ownerAccentObject.GetComponent<RectTransform>();
            ownerAccentRect.anchorMin = new Vector2(0f, 0f);
            ownerAccentRect.anchorMax = new Vector2(0f, 1f);
            ownerAccentRect.pivot = new Vector2(0f, 0.5f);
            ownerAccentRect.anchoredPosition = Vector2.zero;
            ownerAccentRect.sizeDelta = new Vector2(10f, 0f);

            GameObject headerObject = CreateUiObject("HeaderBanner", root.transform, Vector2.zero);
            Image headerBanner = headerObject.AddComponent<Image>();
            headerBanner.color = new Color(0.24f, 0.50f, 0.42f, 1f);
            RectTransform headerRect = headerObject.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(10f, -86f);
            headerRect.offsetMax = new Vector2(-10f, -56f);

            GameObject costBadgeObject = CreateUiObject("CostBadge", root.transform, new Vector2(42f, 42f));
            Image costBadge = costBadgeObject.AddComponent<Image>();
            costBadge.color = new Color(0.20f, 0.40f, 0.70f, 1f);
            SetAnchoredRect(costBadgeObject.GetComponent<RectTransform>(), new Vector2(12f, -12f), new Vector2(42f, 42f), TextAnchor.UpperLeft);

            Text costText = CreateText("CostText", costBadgeObject.transform, Vector2.zero, new Vector2(42f, 42f), 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(costText.rectTransform, 0f);
            costText.color = Color.white;

            Text cardName = CreateText("CardName", root.transform, new Vector2(62f, -12f), new Vector2(154f, 34f), 20, FontStyle.Bold, TextAnchor.UpperLeft);
            Text upgradedLabel = CreateText("UpgradedLabel", root.transform, new Vector2(132f, -14f), new Vector2(84f, 18f), 11, FontStyle.Bold, TextAnchor.UpperRight);
            upgradedLabel.color = new Color(0.72f, 0.56f, 0.16f, 1f);

            Text ownerText = CreateText("Owner", root.transform, new Vector2(16f, -50f), new Vector2(202f, 18f), 14, FontStyle.Italic, TextAnchor.UpperLeft);
            Text typeText = CreateText("Type", root.transform, new Vector2(16f, -74f), new Vector2(110f, 18f), 13, FontStyle.Bold, TextAnchor.MiddleCenter);
            typeText.color = new Color(0.98f, 0.96f, 0.90f, 1f);

            GameObject artFrameObject = CreateUiObject("ArtFrame", root.transform, new Vector2(204f, 94f));
            Image artFrame = artFrameObject.AddComponent<Image>();
            artFrame.color = new Color(0.72f, 0.64f, 0.50f, 1f);
            SetAnchoredRect(artFrameObject.GetComponent<RectTransform>(), new Vector2(16f, -102f), new Vector2(204f, 94f), TextAnchor.UpperLeft);

            GameObject artObject = CreateUiObject("Art", artFrameObject.transform, new Vector2(192f, 82f));
            StretchFull(artObject.GetComponent<RectTransform>(), 6f);
            Image artImage = artObject.AddComponent<Image>();
            artImage.color = new Color(0.84f, 0.82f, 0.76f, 1f);

            Text artLabel = CreateText("ArtLabel", artFrameObject.transform, Vector2.zero, new Vector2(180f, 48f), 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            StretchFull(artLabel.rectTransform, 8f);

            Text descriptionText = CreateText("Description", root.transform, new Vector2(16f, -210f), new Vector2(204f, 88f), 14, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject disabledOverlayObject = CreateUiObject("DisabledOverlay", root.transform, Vector2.zero);
            Image disabledOverlay = disabledOverlayObject.AddComponent<Image>();
            disabledOverlay.color = new Color(0.10f, 0.10f, 0.12f, 0.42f);
            StretchFull(disabledOverlayObject.GetComponent<RectTransform>(), 0f);
            disabledOverlay.enabled = false;

            Text disabledReasonText = CreateText("DisabledReason", disabledOverlayObject.transform, new Vector2(18f, -302f), new Vector2(200f, 28f), 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            disabledReasonText.color = new Color(1f, 0.95f, 0.95f, 1f);
            RectTransform disabledReasonRect = disabledReasonText.rectTransform;
            disabledReasonRect.anchorMin = new Vector2(0f, 0f);
            disabledReasonRect.anchorMax = new Vector2(1f, 0f);
            disabledReasonRect.pivot = new Vector2(0.5f, 0f);
            disabledReasonRect.anchoredPosition = new Vector2(0f, 14f);
            disabledReasonRect.sizeDelta = new Vector2(-18f, 40f);

            CardView view = root.AddComponent<CardView>();
            view.cardNameText = cardName;
            view.ownerText = ownerText;
            view.typeText = typeText;
            view.costText = costText;
            view.descriptionText = descriptionText;
            view.disabledReasonText = disabledReasonText;
            view.upgradedLabelText = upgradedLabel;
            view.artLabelText = artLabel;
            view.artImage = artImage;
            view.backgroundImage = background;
            view.costBadgeImage = costBadge;
            view.disabledOverlayImage = disabledOverlay;
            view.headerBannerImage = headerBanner;
            view.ownerAccentImage = ownerAccent;
            view.artFrameImage = artFrame;
            view.button = button;
            view.canvasGroup = canvasGroup;
            view.selectionOutline = outline;

            CardView prefab = SavePrefab<CardView>(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static EnemyView CreateEnemyViewPrefab()
        {
            string path = PrefabFolder + "/EnemyView.prefab";
            DeleteAssetIfExists(path);

            GameObject root = CreateUiObject("EnemyView", null, new Vector2(260f, 206f));
            Image background = root.AddComponent<Image>();
            background.color = new Color(0.93f, 0.84f, 0.84f, 1f);

            Text enemyName = CreateText("EnemyName", root.transform, new Vector2(12f, -12f), new Vector2(220f, 24f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            Text hpText = CreateText("HpText", root.transform, new Vector2(12f, -42f), new Vector2(220f, 22f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            Text blockText = CreateText("BlockText", root.transform, new Vector2(12f, -66f), new Vector2(220f, 22f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
            Text phaseText = CreateText("PhaseText", root.transform, new Vector2(12f, -90f), new Vector2(220f, 22f), 18, FontStyle.Bold, TextAnchor.UpperLeft);
            Text intentText = CreateText("IntentText", root.transform, new Vector2(12f, -114f), new Vector2(220f, 22f), 18, FontStyle.Bold, TextAnchor.UpperLeft);
            Text intentDescriptionText = CreateText("IntentDescriptionText", root.transform, new Vector2(12f, -138f), new Vector2(220f, 32f), 15, FontStyle.Normal, TextAnchor.UpperLeft);
            Text statusText = CreateText("StatusText", root.transform, new Vector2(12f, -172f), new Vector2(220f, 28f), 14, FontStyle.Normal, TextAnchor.UpperLeft);

            EnemyView view = root.AddComponent<EnemyView>();
            view.enemyNameText = enemyName;
            view.hpText = hpText;
            view.blockText = blockText;
            view.phaseText = phaseText;
            view.intentText = intentText;
            view.intentDescriptionText = intentDescriptionText;
            view.statusText = statusText;
            view.backgroundImage = background;

            GameObject spriteObject = CreateUiObject("EnemySprite", root.transform, new Vector2(88f, 88f));
            SetAnchoredRect(spriteObject.GetComponent<RectTransform>(), new Vector2(160f, -12f), new Vector2(88f, 88f), TextAnchor.UpperLeft);
            Image spriteImage = spriteObject.AddComponent<Image>();
            spriteImage.color = new Color(0.75f, 0.75f, 0.78f, 1f);
            spriteImage.preserveAspect = true;
            view.spriteImage = spriteImage;

            EnemyView prefab = SavePrefab<EnemyView>(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateHeroSelectionScene(HeroDatabase heroDatabase, CardDatabase cardDatabase, HeroSelectionCardView heroCardPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "HeroSelection";

            Canvas canvas = CreateCanvas();
            CreateEventSystem();

            GameObject runManagerObject = new GameObject("RunManager");
            RunManager runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();
            runManager.heroDatabase = heroDatabase;
            runManager.cardDatabase = cardDatabase;
            runManager.mainMenuSceneName = "MainMenu";
            runManager.heroSelectionSceneName = "HeroSelection";
            runManager.battleSceneName = "Battle";
            runManager.mapSceneName = "Map";
            runManager.heroProgressionSceneName = "HeroProgression";

            GameObject rootPanel = CreatePanel("SelectionRoot", canvas.transform, new Color(0.10f, 0.15f, 0.12f, 1f));
            StretchFull(rootPanel.GetComponent<RectTransform>(), 20f);

            HeroSelectionManager selectionManager = rootPanel.AddComponent<HeroSelectionManager>();
            selectionManager.runManager = runManager;
            selectionManager.heroCardPrefab = heroCardPrefab;

            EditorSceneManager.SaveScene(scene, SceneFolder + "/HeroSelection.unity");
        }

        private static void CreateMainMenuScene(HeroDatabase heroDatabase, CardDatabase cardDatabase)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MainMenu";

            Canvas canvas = CreateCanvas();
            CreateEventSystem();

            GameObject runManagerObject = new GameObject("RunManager");
            RunManager runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();
            runManager.heroDatabase = heroDatabase;
            runManager.cardDatabase = cardDatabase;
            runManager.mainMenuSceneName = "MainMenu";
            runManager.heroSelectionSceneName = "HeroSelection";
            runManager.battleSceneName = "Battle";
            runManager.mapSceneName = "Map";
            runManager.heroProgressionSceneName = "HeroProgression";

            GameObject rootPanel = CreatePanel("MainMenuRoot", canvas.transform, new Color(0.10f, 0.15f, 0.12f, 1f));
            StretchFull(rootPanel.GetComponent<RectTransform>(), 20f);

            MainMenuManager menuManager = rootPanel.AddComponent<MainMenuManager>();
            menuManager.runManager = runManager;

            EditorSceneManager.SaveScene(scene, SceneFolder + "/MainMenu.unity");
        }

        private static void CreateBattleScene(HeroDatabase heroDatabase, CardDatabase cardDatabase, EnemyArtDatabase enemyArtDatabase, CardView cardViewPrefab, BattleHeroView heroViewPrefab, EnemyView enemyViewPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Battle";

            Canvas canvas = CreateCanvas();
            CreateEventSystem();

            GameObject rootPanel = CreatePanel("BattleRoot", canvas.transform, new Color(0.88f, 0.93f, 0.96f, 1f));
            StretchFull(rootPanel.GetComponent<RectTransform>(), 20f);

            Text titleText = CreateText("Title", rootPanel.transform, new Vector2(20f, -20f), new Vector2(600f, 40f), 30, FontStyle.Bold, TextAnchor.UpperLeft);
            titleText.text = "Paw Slayers - Battle Prototype";
            Text turnText = CreateText("TurnText", rootPanel.transform, new Vector2(20f, -62f), new Vector2(220f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
            Text energyText = CreateText("EnergyText", rootPanel.transform, new Vector2(260f, -62f), new Vector2(220f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft);

            GameObject fieldArea = CreateUiObject("FieldArea", rootPanel.transform, Vector2.zero);
            RectTransform fieldRect = fieldArea.GetComponent<RectTransform>();
            fieldRect.anchorMin = new Vector2(0f, 0f);
            fieldRect.anchorMax = new Vector2(1f, 1f);
            fieldRect.offsetMin = new Vector2(20f, 290f);
            fieldRect.offsetMax = new Vector2(-240f, -120f);

            GameObject heroPanel = CreatePanel("HeroPanel", fieldArea.transform, new Color(0.85f, 0.91f, 0.84f, 1f));
            RectTransform heroPanelRect = heroPanel.GetComponent<RectTransform>();
            heroPanelRect.anchorMin = new Vector2(0f, 0f);
            heroPanelRect.anchorMax = new Vector2(0.48f, 1f);
            heroPanelRect.offsetMin = Vector2.zero;
            heroPanelRect.offsetMax = new Vector2(-10f, 0f);
            CreateText("HeroesLabel", heroPanel.transform, new Vector2(12f, -12f), new Vector2(240f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft).text = "Heroes";

            GameObject heroContainer = CreateUiObject("HeroContainer", heroPanel.transform, Vector2.zero);
            RectTransform heroRect = heroContainer.GetComponent<RectTransform>();
            heroRect.anchorMin = new Vector2(0f, 0f);
            heroRect.anchorMax = new Vector2(1f, 1f);
            heroRect.offsetMin = new Vector2(12f, 12f);
            heroRect.offsetMax = new Vector2(-12f, -48f);
            VerticalLayoutGroup heroLayout = heroContainer.AddComponent<VerticalLayoutGroup>();
            heroLayout.spacing = 10f;
            heroLayout.childControlWidth = true;
            heroLayout.childControlHeight = false;
            heroLayout.childForceExpandWidth = true;
            heroLayout.childForceExpandHeight = false;

            GameObject enemyPanel = CreatePanel("EnemyPanel", fieldArea.transform, new Color(0.94f, 0.85f, 0.85f, 1f));
            RectTransform enemyPanelRect = enemyPanel.GetComponent<RectTransform>();
            enemyPanelRect.anchorMin = new Vector2(0.52f, 0f);
            enemyPanelRect.anchorMax = new Vector2(1f, 1f);
            enemyPanelRect.offsetMin = new Vector2(10f, 0f);
            enemyPanelRect.offsetMax = Vector2.zero;
            CreateText("EnemiesLabel", enemyPanel.transform, new Vector2(12f, -12f), new Vector2(240f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft).text = "Enemies";

            GameObject enemyContainer = CreateUiObject("EnemyContainer", enemyPanel.transform, Vector2.zero);
            RectTransform enemyRect = enemyContainer.GetComponent<RectTransform>();
            enemyRect.anchorMin = new Vector2(0f, 0f);
            enemyRect.anchorMax = new Vector2(1f, 1f);
            enemyRect.offsetMin = new Vector2(12f, 12f);
            enemyRect.offsetMax = new Vector2(-12f, -48f);
            VerticalLayoutGroup enemyLayout = enemyContainer.AddComponent<VerticalLayoutGroup>();
            enemyLayout.spacing = 10f;
            enemyLayout.childControlWidth = true;
            enemyLayout.childControlHeight = false;
            enemyLayout.childForceExpandWidth = true;
            enemyLayout.childForceExpandHeight = false;

            GameObject debugPanel = CreatePanel("DebugPanel", rootPanel.transform, new Color(0.82f, 0.82f, 0.82f, 1f));
            RectTransform debugRect = debugPanel.GetComponent<RectTransform>();
            debugRect.anchorMin = new Vector2(1f, 0.5f);
            debugRect.anchorMax = new Vector2(1f, 0.5f);
            debugRect.pivot = new Vector2(1f, 0.5f);
            debugRect.anchoredPosition = new Vector2(-20f, 0f);
            debugRect.sizeDelta = new Vector2(200f, 420f);
            CreateText("DebugLabel", debugPanel.transform, new Vector2(12f, -12f), new Vector2(180f, 28f), 22, FontStyle.Bold, TextAnchor.UpperLeft).text = "Debug";

            GameObject buttonRow = CreateUiObject("DebugButtons", debugPanel.transform, Vector2.zero);
            RectTransform buttonRect = buttonRow.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.offsetMin = new Vector2(12f, 12f);
            buttonRect.offsetMax = new Vector2(-12f, -48f);
            VerticalLayoutGroup buttonLayout = buttonRow.AddComponent<VerticalLayoutGroup>();
            buttonLayout.spacing = 10f;
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = false;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = false;

            Button drawButton = CreateButton("DrawButton", buttonRow.transform, Vector2.zero, new Vector2(120f, 44f), "Draw Card", TextAnchor.MiddleLeft).GetComponent<Button>();
            Button endTurnButton = CreateButton("EndTurnButton", buttonRow.transform, Vector2.zero, new Vector2(120f, 44f), "End Turn", TextAnchor.MiddleLeft).GetComponent<Button>();
            Button kill1Button = CreateButton("KillHero1Button", buttonRow.transform, Vector2.zero, new Vector2(120f, 44f), "Kill Hero 1", TextAnchor.MiddleLeft).GetComponent<Button>();
            Button kill2Button = CreateButton("KillHero2Button", buttonRow.transform, Vector2.zero, new Vector2(120f, 44f), "Kill Hero 2", TextAnchor.MiddleLeft).GetComponent<Button>();
            Button kill3Button = CreateButton("KillHero3Button", buttonRow.transform, Vector2.zero, new Vector2(120f, 44f), "Kill Hero 3", TextAnchor.MiddleLeft).GetComponent<Button>();
            Button healButton = CreateButton("HealAllButton", buttonRow.transform, Vector2.zero, new Vector2(120f, 44f), "Heal All", TextAnchor.MiddleLeft).GetComponent<Button>();
            Button winButton = CreateButton("WinBattleButton", buttonRow.transform, Vector2.zero, new Vector2(120f, 44f), "Win Battle", TextAnchor.MiddleLeft).GetComponent<Button>();

            GameObject handPanel = CreatePanel("HandPanel", rootPanel.transform, new Color(0.95f, 0.93f, 0.87f, 1f));
            RectTransform handPanelRect = handPanel.GetComponent<RectTransform>();
            handPanelRect.anchorMin = new Vector2(0f, 0f);
            handPanelRect.anchorMax = new Vector2(1f, 0f);
            handPanelRect.pivot = new Vector2(0.5f, 0f);
            handPanelRect.offsetMin = new Vector2(20f, 20f);
            handPanelRect.offsetMax = new Vector2(-20f, 260f);
            CreateText("HandLabel", handPanel.transform, new Vector2(12f, -12f), new Vector2(200f, 28f), 24, FontStyle.Bold, TextAnchor.UpperLeft).text = "Hand";

            GameObject handContainer = CreateUiObject("HandContainer", handPanel.transform, Vector2.zero);
            RectTransform handRect = handContainer.GetComponent<RectTransform>();
            handRect.anchorMin = new Vector2(0f, 0f);
            handRect.anchorMax = new Vector2(1f, 1f);
            handRect.offsetMin = new Vector2(12f, 12f);
            handRect.offsetMax = new Vector2(-12f, -48f);
            HorizontalLayoutGroup handLayout = handContainer.AddComponent<HorizontalLayoutGroup>();
            handLayout.spacing = 10f;
            handLayout.childControlWidth = false;
            handLayout.childControlHeight = false;
            handLayout.childForceExpandWidth = false;
            handLayout.childForceExpandHeight = false;

            GameObject logPanel = CreatePanel("LogPanel", rootPanel.transform, new Color(0.85f, 0.89f, 0.94f, 1f));
            RectTransform logRect = logPanel.GetComponent<RectTransform>();
            logRect.anchorMin = new Vector2(0f, 0f);
            logRect.anchorMax = new Vector2(0f, 0f);
            logRect.pivot = new Vector2(0f, 0f);
            logRect.anchoredPosition = new Vector2(20f, 260f);
            logRect.sizeDelta = new Vector2(520f, 130f);
            Text battleLog = CreateText("BattleLog", logPanel.transform, new Vector2(12f, -12f), new Vector2(496f, 106f), 16, FontStyle.Normal, TextAnchor.UpperLeft);

            GameObject rewardPanel = CreatePanel("RewardPanel", rootPanel.transform, new Color(0f, 0f, 0f, 0.7f));
            RectTransform rewardRect = rewardPanel.GetComponent<RectTransform>();
            rewardRect.anchorMin = new Vector2(0f, 0f);
            rewardRect.anchorMax = new Vector2(1f, 1f);
            rewardRect.offsetMin = Vector2.zero;
            rewardRect.offsetMax = Vector2.zero;

            GameObject rewardBox = CreatePanel("RewardBox", rewardPanel.transform, new Color(0.97f, 0.95f, 0.88f, 1f));
            RectTransform rewardBoxRect = rewardBox.GetComponent<RectTransform>();
            rewardBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            rewardBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            rewardBoxRect.pivot = new Vector2(0.5f, 0.5f);
            rewardBoxRect.sizeDelta = new Vector2(760f, 420f);
            rewardBoxRect.anchoredPosition = Vector2.zero;

            Text rewardTitle = CreateText("RewardTitle", rewardBox.transform, new Vector2(20f, -20f), new Vector2(300f, 32f), 28, FontStyle.Bold, TextAnchor.UpperLeft);
            GameObject rewardContainer = CreateUiObject("RewardContainer", rewardBox.transform, Vector2.zero);
            RectTransform rewardContainerRect = rewardContainer.GetComponent<RectTransform>();
            rewardContainerRect.anchorMin = new Vector2(0f, 0f);
            rewardContainerRect.anchorMax = new Vector2(1f, 1f);
            rewardContainerRect.offsetMin = new Vector2(20f, 20f);
            rewardContainerRect.offsetMax = new Vector2(-20f, -70f);
            HorizontalLayoutGroup rewardLayout = rewardContainer.AddComponent<HorizontalLayoutGroup>();
            rewardLayout.spacing = 12f;
            rewardLayout.childControlWidth = false;
            rewardLayout.childControlHeight = false;
            rewardLayout.childForceExpandWidth = false;
            rewardLayout.childForceExpandHeight = false;
            rewardPanel.SetActive(false);

            GameObject managerObject = new GameObject("BattleManagers");
            BattleUIManager battleUiManager = managerObject.AddComponent<BattleUIManager>();
            RewardCardManager rewardCardManager = managerObject.AddComponent<RewardCardManager>();
            DebugBattleControls debugControls = managerObject.AddComponent<DebugBattleControls>();
            GameObject runManagerObject = new GameObject("RunManager");
            RunManager runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();
            runManager.heroDatabase = heroDatabase;
            runManager.cardDatabase = cardDatabase;
            runManager.enemyArtDatabase = enemyArtDatabase;
            runManager.mainMenuSceneName = "MainMenu";
            runManager.heroSelectionSceneName = "HeroSelection";
            runManager.battleSceneName = "Battle";
            runManager.mapSceneName = "Map";
            runManager.heroProgressionSceneName = "HeroProgression";

            battleUiManager.runManager = runManager;
            battleUiManager.enemyArtDatabase = enemyArtDatabase;
            battleUiManager.heroViewPrefab = heroViewPrefab;
            battleUiManager.enemyViewPrefab = enemyViewPrefab;
            battleUiManager.cardViewPrefab = cardViewPrefab;
            battleUiManager.rewardCardManager = rewardCardManager;
            battleUiManager.drawButton = drawButton;
            battleUiManager.endTurnButton = endTurnButton;
            battleUiManager.killHero1Button = kill1Button;
            battleUiManager.killHero2Button = kill2Button;
            battleUiManager.killHero3Button = kill3Button;
            battleUiManager.healAllButton = healButton;
            battleUiManager.winBattleButton = winButton;
            battleUiManager.battleLogText = battleLog;
            battleUiManager.handContainer = handContainer.transform;
            battleUiManager.heroContainer = heroContainer.transform;
            battleUiManager.enemyContainer = enemyContainer.transform;

            rewardCardManager.runManager = runManager;
            rewardCardManager.cardDatabase = cardDatabase;
            rewardCardManager.rewardCardPrefab = cardViewPrefab;
            rewardCardManager.rewardPanel = rewardPanel;
            rewardCardManager.rewardContainer = rewardContainer.transform;
            rewardCardManager.rewardTitleText = rewardTitle;

            debugControls.battleUiManager = battleUiManager;
            debugControls.runManager = runManager;

            UnityEventTools.AddIntPersistentListener(kill1Button.onClick, debugControls.KillHeroSlot, 0);
            UnityEventTools.AddIntPersistentListener(kill2Button.onClick, debugControls.KillHeroSlot, 1);
            UnityEventTools.AddIntPersistentListener(kill3Button.onClick, debugControls.KillHeroSlot, 2);
            UnityEventTools.AddPersistentListener(healButton.onClick, debugControls.HealAllHeroes);
            UnityEventTools.AddPersistentListener(winButton.onClick, debugControls.WinBattle);

            EditorSceneManager.SaveScene(scene, SceneFolder + "/Battle.unity");
        }

        private static void CreateMapScene(CardView cardViewPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Map";

            CreateCanvas();
            CreateEventSystem();

            GameObject managerObject = new GameObject("MapManagers");
            MapManager mapManager = managerObject.AddComponent<MapManager>();
            mapManager.cardViewPrefab = cardViewPrefab;

            EditorSceneManager.SaveScene(scene, SceneFolder + "/Map.unity");
        }

        private static void CreateHeroProgressionScene(HeroDatabase heroDatabase, CardDatabase cardDatabase)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "HeroProgression";

            Canvas canvas = CreateCanvas();
            CreateEventSystem();

            GameObject runManagerObject = new GameObject("RunManager");
            RunManager runManager = runManagerObject.AddComponent<RunManager>();
            runManagerObject.AddComponent<DeckManager>();
            runManager.heroDatabase = heroDatabase;
            runManager.cardDatabase = cardDatabase;
            runManager.mainMenuSceneName = "MainMenu";
            runManager.heroSelectionSceneName = "HeroSelection";
            runManager.battleSceneName = "Battle";
            runManager.mapSceneName = "Map";
            runManager.heroProgressionSceneName = "HeroProgression";

            GameObject rootPanel = CreatePanel("HeroProgressionRoot", canvas.transform, new Color(0.10f, 0.15f, 0.12f, 1f));
            StretchFull(rootPanel.GetComponent<RectTransform>(), 20f);

            HeroProgressionScreenManager progressionManager = rootPanel.AddComponent<HeroProgressionScreenManager>();
            progressionManager.runManager = runManager;

            EditorSceneManager.SaveScene(scene, SceneFolder + "/HeroProgression.unity");
        }

        private static void AddScenesToBuildSettings()
        {
            string mainMenuPath = SceneFolder + "/MainMenu.unity";
            string heroSelectionPath = SceneFolder + "/HeroSelection.unity";
            string battlePath = SceneFolder + "/Battle.unity";
            string mapPath = SceneFolder + "/Map.unity";
            string heroProgressionPath = SceneFolder + "/HeroProgression.unity";

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(heroSelectionPath, true),
                new EditorBuildSettingsScene(battlePath, true),
                new EditorBuildSettingsScene(mapPath, true),
                new EditorBuildSettingsScene(heroProgressionPath, true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreateUiObject(name, parent, Vector2.zero);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static GameObject CreateButton(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string label, TextAnchor anchor)
        {
            GameObject buttonObject = CreateUiObject(name, parent, size);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();

            if (anchor == TextAnchor.LowerRight)
            {
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y);
            }

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.36f, 0.55f, 0.31f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.46f, 0.65f, 0.41f, 1f);
            colors.pressedColor = new Color(0.26f, 0.45f, 0.21f, 1f);
            button.colors = colors;

            Text labelText = CreateText("Label", buttonObject.transform, Vector2.zero, size, 20, FontStyle.Bold, TextAnchor.MiddleCenter);
            labelText.text = label;
            StretchFull(labelText.rectTransform, 0f);
            return buttonObject;
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            GameObject textObject = CreateUiObject(name, parent, size);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.alignment = alignment;
            text.color = Color.black;

            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent, Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return gameObject;
        }

        private static void StretchFull(RectTransform rectTransform, float padding)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(padding, padding);
            rectTransform.offsetMax = new Vector2(-padding, -padding);
        }

        private static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
        {
            rectTransform.sizeDelta = size;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = anchor == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private static T SavePrefab<T>(GameObject source, string path) where T : Component
        {
            PrefabUtility.SaveAsPrefabAsset(source, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static void UpsertScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            for (int index = 0; index < scenes.Count; index++)
            {
                if (scenes[index].path == path)
                {
                    scenes[index] = new EditorBuildSettingsScene(path, true);
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
#endif
