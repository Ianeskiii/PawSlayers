# Paw Slayers Prototype Setup

This folder contains a beginner-friendly prototype for the 3-active-heroes-per-run deck system.

## What it creates

- `HeroSelection` scene
- `Battle` scene
- 5 hero data assets
- 12 card data assets
- simple UI prefabs for hero buttons, hero panels, and cards
- a `RunManager` that persists from hero selection into battle

## One-time setup in Unity

1. Open the project in Unity.
2. Wait for scripts to compile.
3. In the top menu, click `Tools > Paw Slayers > Generate Prototype Setup`.
4. Open the scene `Assets/PawSlayers/Scenes/HeroSelection.unity`.
5. Press Play.

## Controls in Play Mode

- Select exactly 3 heroes, then click `Start Run`
- In battle:
  - Click cards to play them
  - Click `Draw Card` to draw
  - Click `End Turn` to discard hand and draw 5 new cards
  - Click `Kill Hero 1`, `Kill Hero 2`, or `Kill Hero 3` to test disabled cards
  - Click `Heal All` to restore the team
  - Click `Win Battle` to open reward cards

## Keyboard shortcuts in Battle

- `1`, `2`, `3` = kill hero slot 1, 2, or 3
- `H` = heal all heroes
- `D` = draw a card
- `W` = win battle

## Important scripts

- `Assets/PawSlayers/Scripts/Runtime/RunManager.cs`
- `Assets/PawSlayers/Scripts/Runtime/DeckManager.cs`
- `Assets/PawSlayers/Scripts/UI/HeroSelectionManager.cs`
- `Assets/PawSlayers/Scripts/UI/BattleUIManager.cs`
- `Assets/PawSlayers/Scripts/UI/RewardCardManager.cs`
- `Assets/PawSlayers/Scripts/Editor/PawSlayersPrototypeSetupEditor.cs`

## If you want to edit heroes or cards later

- Hero assets live in `Assets/PawSlayers/Data/Heroes`
- Card assets live in `Assets/PawSlayers/Data/Cards`
- Databases live in `Assets/PawSlayers/Data`

If you rerun `Generate Prototype Setup`, the scenes and prefabs created by this prototype will be rebuilt.
