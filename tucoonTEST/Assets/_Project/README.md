# Supermarket Idle Tycoon

Unity: 6000.3.17f1

This folder contains project-owned code, scenes, configuration, and content for the Supermarket Idle Tycoon test task. Imported third-party asset packs remain outside this folder and must be treated as read-only dependencies.

## Folder Responsibilities

- `Art`: Project-owned visual assets, material instances, model exports, prefab variants, and VFX built specifically for the game.
- `Audio`: Project-owned music and SFX references or edited audio assets.
- `Data`: ScriptableObjects and other authored configuration data.
- `Fonts`: Project-owned font assets.
- `Scenes`: Project-owned Unity scenes.
- `Scripts/Core`: Application flow, bootstrapping, shared game state contracts, and high-level orchestration.
- `Scripts/Economy`: Currency, profit, pricing, resource, and reward logic.
- `Scripts/Building`: Object creation, buildable definitions, placement flow, shelf/register/business-area expansion logic.
- `Scripts/AI`: Customer and staff behavior code, navigation-facing logic, and automation agents.
- `Scripts/Progression`: Level, location, unlock, upgrade, and automation progression rules.
- `Scripts/Save`: Native Unity/C# save data models, serializers, and persistence services.
- `Scripts/UI`: Main menu, settings, loading screen, HUD, and other interface controllers.
- `Scripts/Utilities`: Small reusable helpers that do not belong to a specific gameplay domain.
- `UI`: Project-owned UI sprites, icons, and UI prefabs.
- `Editor`: Project-owned Unity editor utilities.

## Third-Party Asset Policy

Third-party visual assets are treated as read-only dependencies.

Game-specific prefabs, prefab variants, material instances, scene placements, configurations, and edited resources should be created under `Assets/_Project` rather than modifying source asset-pack files.
