# Supermarket Idle Tycoon

Target Unity version: **6000.3.17f1**
Version currently declared by this checkout: **6000.3.13f1**

This is a small playable 3D idle-tycoon vertical slice. Project-owned code, scenes, data, wrappers, tests, and UI live under `Assets/_Project`. Imported packs outside this folder are read-only dependencies.

## Run the demo

1. Open the project in Unity.
2. Wait for script compilation to finish with no project-owned errors.
3. Run `Tools > Supermarket Tycoon > Build / Repair Playable Demo` once. The builder is idempotent and only recreates project-owned content.
4. Press Play. The project configures `Assets/_Project/Scenes/Bootstrap.unity` as the Play Mode start scene even when MainMenu or Game is open for editing.

The Bootstrap scene loads MainMenu asynchronously. Select Play or Continue to enter the supermarket. Build the shelf for $50 and checkout for $100; customers then shop and pay automatically. Use the top-right pause button to resume, change settings, reset progress, or return to Main Menu.

## Architecture

The runtime uses a pragmatic modular architecture inside one assembly. Plain C# services own game rules and state; MonoBehaviours adapt those services to scenes, navigation, and UI. ScriptableObjects contain configuration only.

```text
Bootstrap
   |
AppBootstrapper (composition root)
   |
ApplicationContext
   +-- SceneFlowService
   +-- AudioService
   +-- SettingsService
   +-- SaveRepository
             |
             v
         Game Scene
             |
      GameSceneEntryPoint
             |
         Game Session
     /       |        \
 Wallet  Buildings  Customers (FSM + pool)
             |        |
        Stations <--- Queue
             |        |
             +--> Checkout --> Economy --> Progression
```

`AppBootstrapper` explicitly constructs application-wide services. A loaded scene exposes one `SceneEntryPoint`; `SceneFlowService` injects `ApplicationContext` at the scene boundary. `GameSceneEntryPoint` then constructs the session-scoped wallet, progression, economy, station registry, building service, customer spawner, save coordinator, and UI controllers. No service locator or public global manager is used.

## Modules

- `Scripts/Core`: application context, game configuration, pause state, and composition support.
- `Scripts/Economy`: wallet invariants and transaction income.
- `Scripts/Buildings`: data-driven definitions, build spots, purchase validation, shelf stations, and station registry.
- `Scripts/Customers`: customer configuration, pooled agents, runtime context, and finite state machine.
- `Scripts/Checkout`: checkout queue and queue-point ownership.
- `Scripts/Progression`: XP thresholds, level changes, and unlock events.
- `Scripts/Save`: JSON data, repositories, settings persistence, and save coordination.
- `Scripts/SceneFlow`: asynchronous scene loading and scene entry points.
- `Scripts/UI`: view components and controllers for menu, HUD, settings, pause, loading, and income feedback.
- `Scripts/Audio`: centralized UI/build/income/level-up cues.
- `Editor`: idempotent project builder and asset wiring.
- `Tests/EditMode`: wallet, progression, and JSON round-trip tests.

## Gameplay loop

The player starts with $150. Build spots call `BuildingService`, which validates level, occupancy, configuration, and funds before spending. Once at least one `ShelfStation` and one `CheckoutStation` are registered, `CustomerSpawner` begins producing up to six pooled customers.

```text
Spawn -> reserve shelf -> walk -> shop -> release shelf
      -> join checkout -> advance in queue -> pay
      -> award money and XP -> exit -> return to pool
```

The customer state machine uses separate state classes. Missing shelves/checkouts, full queues, vanished destinations, pause, and missing NavMesh placement result in waiting or retrying instead of exceptions.

`CheckoutStation` owns an ordered customer list and three authored queue transforms. Each customer asks for its current position every tick, so the line advances automatically when the first customer completes payment or leaves.

## Save data

Gameplay is stored as readable JSON at:

```text
Application.persistentDataPath/supermarket-save.json
```

A previous valid file is retained as `supermarket-save.backup.json`. Corrupt data is backed up and replaced with a safe fresh state. The format stores:

```text
SaveVersion
Money
CurrentLevel
CurrentXp
BuiltBuildings[]
  BuildSpotId
  BuildingDefinitionId
```

Settings use `supermarket-settings.json` and are not deleted by Reset Progress. Saves occur after purchases, completed customer transactions/progression changes, application pause, application quit, and before returning to Main Menu.

## Extending buildings

1. Create a project-owned wrapper prefab under `Art/Prefabs/Buildings`; nest a third-party visual instead of editing its source.
2. Add logical station/interaction transforms and components to the wrapper.
3. Create a `BuildingDefinition` asset with a stable unique ID, type, prefab, cost, required level, and capacity.
4. Add a `BuildSpot` with a stable spot ID and reference the new definition.

`BuildingService` remains the purchase boundary. New UI buttons should request the use case through it rather than changing the wallet or instantiating prefabs directly.

## Extending customer behavior

Implement another `ICustomerState` in `Scripts/Customers/States`, transition to it from an existing state, and keep scene dependencies in `CustomerRuntimeContext`. States should reserve shared resources on entry and release them on every completion/cancellation path. New visual animation behavior belongs in a presentation adapter, not in the state rules.

## Third-party assets used read-only

- Grocery environment and visuals: `Assets/Gridness Studios/Grocery Store Pack Lite`
- Customer visual and existing animator: `Assets/Hodaart/HodaartLowPolyCharacterCollection3`
- Mobile UI sprites: `Assets/HONETi/mobile_cartoon_GUI`
- Build feedback: `Assets/SimpleFX`
- Audio cues: `Assets/Cartoon Game Sound 2.0`
- Navigation: official `com.unity.ai.navigation` package

The builder loads these exact paths and falls back to clean native Unity primitives/colors when an optional visual cannot be loaded. It never writes into source pack folders.

## Generated content

The builder creates or repairs:

- `Scenes/Bootstrap.unity`, `Scenes/MainMenu.unity`, `Scenes/Game.unity`
- `Data/ScriptableObjects/GameConfig.asset`, `CustomerConfig.asset`, `ProgressionConfig.asset`
- Four `BuildingDefinition` assets for level 1-3 spots
- Wrapper prefabs for shelf, checkout, customer, floating income, and purchase FX
- Project-owned materials, Main Menu, loading overlay, HUD, pause/settings/reset UI
- Four build spots, customer spawn/exit, queue points, NavMeshSurface, and baked NavMesh data
- Build Settings with Bootstrap, MainMenu, and Game at indices 0-2 while preserving unrelated existing scenes afterward

## Controls

- Mouse/touch: menu, settings, build spots, and pause controls.
- `PLAY`/`CONTINUE`: load the game asynchronously.
- `II`: pause.
- `RESET PROGRESS`: confirmation-gated gameplay reset; audio/display settings remain intact.
