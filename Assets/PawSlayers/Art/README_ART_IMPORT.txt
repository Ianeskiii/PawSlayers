Paw Slayers Art Import Notes

Recommended PNG import settings:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Pixels Per Unit: default is fine for UI
- Mesh Type: Full Rect
- Filter Mode: Bilinear or Point, depending on your style
- Compression: None or High Quality

Supported art folders:
- Assets/PawSlayers/Art/Heroes
- Assets/PawSlayers/Art/HeroPortraits
- Assets/PawSlayers/Art/Enemies
- Assets/PawSlayers/Art/Bosses
- Assets/PawSlayers/Art/Placeholders

Expected hero battle file names:
- capybara_swordsman_battle.png
- koala_thief_battle.png
- sloth_healer_battle.png
- panda_tank_battle.png
- kangaroo_fighter_battle.png

Expected hero portrait file names:
- capybara_swordsman_portrait.png
- koala_thief_portrait.png
- sloth_healer_portrait.png
- panda_tank_portrait.png
- kangaroo_fighter_portrait.png

Expected enemy file names:
- sporeling_battle.png
- fungus_brute_battle.png
- batty_battle.png
- cave_rat_battle.png
- thorn_sprite_battle.png
- moss_troll_battle.png
- crystal_slime_battle.png
- bandit_crow_battle.png
- old_treant_battle.png
- briar_king_battle.png

Two easy ways to use art:
1. Manual assignment:
   - Drag hero portraits and battle sprites into each HeroData asset.
   - Drag enemy sprites into the EnemyArtDatabase asset.

2. Resources auto-load:
   - Put copies of the PNGs into:
     - Assets/PawSlayers/Resources/Art/Heroes
     - Assets/PawSlayers/Resources/Art/HeroPortraits
     - Assets/PawSlayers/Resources/Art/Enemies
     - Assets/PawSlayers/Resources/Art/Bosses
   - Then run:
     Tools > Paw Slayers > Assign Art From Resources

If art is missing, the prototype will keep using placeholder panels.
