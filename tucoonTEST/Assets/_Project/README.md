# Supermarket Idle Tycoon

A compact 3D idle-tycoon vertical slice designed to demonstrate the full progression loop in roughly 10-15 minutes. Project-owned code, scenes, configuration, wrappers, tests, and UI live under `Assets/_Project`. Imported asset packs remain read-only.

## Test-task checklist

| Requirement | Implementation |
| --- | --- |
| Game Core | Bootstrap composition, application context, scene/session lifecycle, pause, save lifecycle |
| Building system | Data-driven definitions, stable BuildSpot IDs, validation, upgrades, restoration, expansion gates |
| Customer AI | One pooled FSM covering spawn, shelf reservation, shopping, checkout queue, payment, exit |
| Economy | Non-negative wallet, validated spending, customer income multipliers, UI events |
| Save | Native JSON/System.IO, backup, version migration, corruption recovery, reset |
| Progression | Five levels with visible unlocks, profit growth, traffic growth, automation, milestone popup |
| Main Menu | Continue/Play, Settings, Quit |
| Settings | Master, Music, SFX, Fullscreen, Back |
| Loading | `LoadSceneAsync` with progress feedback |
| In-game UI | Money, level/XP, rating, objective, pause, contextual building and employee panels |
| Visual consistency | Casual low-poly store, coherent UI palette, URP-compatible project-owned material replacements |
| Native Unity only | Unity AI Navigation, ObjectPool, SceneManager, JsonUtility, coroutines; no gameplay middleware |

The requested editor is **Unity 6000.3.17f1**. This checkout is currently saved by **6000.3.13f1** because 6000.3.17f1 is not installed in the verification environment. Do not edit `ProjectVersion.txt` by hand. Open, upgrade, validate, and save once in 6000.3.17f1 before submission.

## Run the demo

1. Open the project in Unity.
2. Let package import and script compilation complete.
3. Run `Tools > Supermarket Tycoon > Build / Repair Playable Demo` once if generated content needs repair.
4. Press Play. `Bootstrap` is the configured Play Mode start scene.

A fresh game starts with $150. Buy the $50 Shelf and $100 Checkout to activate the customer loop.

## Scene flow

```text
Bootstrap
  -> AppBootstrapper creates ApplicationContext
  -> MainMenu (Play/Continue, Settings, Quit)
  -> Loading overlay + SceneManager.LoadSceneAsync
  -> Game (new session composition and restored save state)
  -> Pause -> Main Menu or Resume
```

## Gameplay loop

```text
Buy Shelf + Checkout
  -> customers shop and queue
  -> payments add money and XP
  -> objectives grant compact rewards
  -> upgrade stations and hire Cashier
  -> unlock and purchase physical expansion
  -> add revenue capacity and second Checkout
  -> unlock Premium Shelf and VIP customers
  -> reach Store Established while play continues
```

## Progression and unlocks

| Level | Cumulative XP | Customer cap | Meaningful unlocks |
| --- | ---: | ---: | --- |
| 1 | 0 | 3 | Starter Shelf and Checkout; establish the first working store loop |
| 2 | 100 | 4 | Second Shelf and Cashier automation |
| 3 | 300 | 6 | Purchasable Store Expansion, Expansion Shelf, second Checkout, stronger Cashier |
| 4 | 650 | 8 | Premium Shelf, VIP profile, level-three station upgrades, stronger Cashier |
| 5 | 1100 | 10 | Maximum customer flow, final upgrades, `STORE ESTABLISHED` milestone; gameplay continues |

Station upgrades visibly scale the wrapper and change capacity, income multiplier, shopping duration, or checkout duration. Ratings also affect traffic: below 3.0 slows arrivals by 15%, while 4.0+ speeds them by 10%.

## Objectives

`ObjectiveConfig` owns the ordered tutorial path:

1. Build a Shelf
2. Build a Checkout
3. Serve 5 Customers
4. Upgrade a Shelf
5. Reach Level 2
6. Hire a Cashier
7. Serve 15 Customers
8. Reach Level 3
9. Unlock Store Expansion
10. Build the second Checkout
11. Reach Rating 4.0
12. Reach Level 5

`ObjectiveService` receives explicit domain/application events, pays rewards through `EconomyService` and `ProgressionService`, and advances without UI-owned business rules.

## Architecture

```text
AppBootstrapper
  -> ApplicationContext
     -> SceneFlowService / AudioService / SettingsService / repositories
     -> GameSceneEntryPoint (composition root only)
        -> Wallet -> EconomyService
        -> ProgressionService / StoreRatingService
        -> BuildingService -> BuildingUpgradeService -> StationRegistry
        -> StoreExpansionService
        -> EmployeeService ---------------------------> StationRegistry
        -> CustomerSpawner -> ObjectPool<CustomerAgent>
           -> shared Customer FSM -> ShelfStation / CheckoutStation
           -> CustomerVisualSelector -> 5 visual roots
        -> ObjectiveService / StatisticsService / OfflineIncomeService
        -> GameSessionEventCoordinator
        -> GameSaveCoordinator
        -> focused UI controllers -> presentation-only views
```

Plain C# services own rules and accept explicit constructor dependencies. MonoBehaviours adapt those services to scene objects, NavMesh movement, pooling, Unity UI, and lifecycle callbacks. `GameSessionEventCoordinator` translates gameplay events between focused systems, leaving `GameSceneEntryPoint` responsible for construction and disposal. There is no service locator, mutable global gameplay state, or giant `GameManager`.

Dependency direction is presentation -> application services -> domain state. Persistence is behind `ISaveRepository`; wallet access is behind `IWallet`. Views expose commands and rendering only. Subscriptions are removed in `Dispose`/`OnDestroy`.

## Building extensibility

`BuildingDefinition` provides a stable ID, display name, type, wrapper prefab, price, required player level, base capacity, and `BuildingLevelDefinition[]`. Each level can configure cost, player requirement, capacity, income multiplier, interaction duration, and visual scale.

To add a building:

1. Create a project-owned station wrapper prefab.
2. Create a `BuildingDefinition` with a unique stable ID and level data.
3. Add and configure a `BuildSpot` with its own stable ID.
4. Optionally set a required expansion ID.

No central purchase or save code needs modification. Shelf and Checkout behavior is discovered from focused station components on the wrapper.

## Store expansion

`StoreExpansionDefinition`, `StoreExpansionSpot`, and `StoreExpansionService` model business areas independently of buildings. The authored area requires level 3 and $500. Before purchase, a bright physical barrier and lock prompt close the rear floor. Purchase removes the NavMesh obstacle, reveals green developed-floor trim, and activates three gated spots:

- Expansion Shelf, available at level 3
- Second Checkout, available at level 3
- Premium Shelf, available at level 4

To add an expansion, create a definition with a stable ID, configure another `StoreExpansionSpot`, and assign that ID to its gated BuildSpots.

## Automation

The Cashier unlocks at level 2. `EmployeeService` persists its level and updates `StationRegistry`, so its speed multiplier affects existing and future Checkouts.

| Cashier level | Cost | Player level | Checkout speed bonus |
| --- | ---: | ---: | ---: |
| 1 | $300 | 2 | 20% |
| 2 | $450 | 3 | 35% |
| 3 | $700 | 4 | 50% |

## Customer AI

Every behavior profile and visual uses one `CustomerAgent` and one state machine:

```text
Spawn -> Find/reserve Shelf -> Move -> Shop -> Release Shelf
      -> Find/join Checkout -> Follow queue -> Pay -> Exit -> Pool
```

Shelf reservations prevent over-capacity use. Checkout owns queue order and positions. A full or missing station causes retry/wait behavior rather than a duplicate AI branch. Queue patience can route a customer to Exit without payment. Pause stops movement and state timers. Pool return releases both shelf and checkout ownership.

Behavior profiles are data assets:

- Normal: standard speed/value, 15 seconds patience.
- Impatient: 18% faster, 20% shorter shopping, 15% lower payment, 8 seconds patience.
- VIP: unlocks at level 4, 2x payment, 20 seconds patience, gold presentation tint.

To add a profile, create a `CustomerProfileDefinition`, configure weights/requirements/multipliers, and add it to the entry-point profile list. The FSM stays unchanged.

## Customer visual variation

The project-owned `Customer.prefab` wrapper contains `CustomerVisualSelector` configuration for five distinct Hodaart character roots: Character 01 through Character 05. Pool creation instantiates the wrapper once; each spawn activates one eligible root and disables the others. Behavior never references visual IDs. Profile tint is applied with `MaterialPropertyBlock`, making VIPs immediately recognizable without cloning materials.

To add a visual:

1. Nest a visual prefab under the project-owned Customer wrapper.
2. Add a unique `CustomerVisualVariant` entry and optional required level.
3. Leave movement, profile, pooling, and FSM code unchanged.

## Save architecture

Gameplay data is readable JSON at `Application.persistentDataPath/supermarket-save.json`; the prior valid file is retained as `supermarket-save.backup.json`. Native `JsonUtility` and `System.IO` are the only persistence dependencies.

Save version 2 persists:

- money, current level, and XP
- stable BuildSpot ID, BuildingDefinition ID, and building level for every purchase
- purchased expansion IDs
- Cashier level
- store rating
- current objective index and progress
- lifetime served/lost, earned money, buildings, and upgrades
- last-save UTC ticks and pending offline income

Version-1 saves migrate in place: building levels default to 1, rating to 3.0, new collections/statistics are initialized, and invalid numeric values are clamped. Corrupt input is copied to the backup path before a fresh in-memory state is returned. Reset Progress removes gameplay save and backup while settings remain separate. Authoritative building/expansion snapshots live in services so Unity teardown order cannot erase a final save.

## Offline income and stats

Offline earnings are capped at 120 minutes and estimate throughput from Shelf capacity, Checkout capacity, base payment, average Shelf multiplier, Cashier speed, and 35% offline efficiency. Collection transfers the pending amount exactly once. The Pause menu shows customers served/lost, total earned, buildings purchased, and upgrades purchased.

## Generated content and materials

`SupermarketTycoonDemoBuilder` idempotently creates or repairs three project scenes, six building definitions, three profile assets, objectives, employee/expansion configuration, wrapper prefabs, NavMesh data, UI, and visual progression. `DemoVerificationTool` validates required assets, scene references, stable IDs, six BuildSpots, five Checkout queue points, five customer visuals, missing scripts, and unsupported materials.

`MaterialRepairTool` creates project-owned URP-compatible replacements for known Grocery and SimpleFX materials. Source assets are never edited.

## Third-party assets used read-only

- Store environment and stations: `Assets/Gridness Studios/Grocery Store Pack Lite`
- Character visuals: `Assets/Hodaart/HodaartLowPolyCharacterCollection3`
- UI sprites: `Assets/HONETi/mobile_cartoon_GUI`
- Purchase particles: `Assets/SimpleFX`
- Audio: `Assets/Cartoon Game Sound 2.0`
- Navigation: official `com.unity.ai.navigation`

No Zenject, DOTween, Odin, Easy Save, third-party DI, save, AI, pathfinding, or gameplay framework is used.

## Tests and validation

EditMode tests cover:

- wallet overspend prevention
- XP carry and level cap
- building level/funds validation and successful spending
- upgrade spending and maximum level
- JSON round trip, version-1 migration, corrupt-save backup/fallback, reset deletion
- objective reward and advance
- rating bounds and traffic thresholds
- customer payment multiplier
- one-time offline collection

Run from `Window > General > Test Runner`, or use Unity batch mode. Run `Tools > Supermarket Tycoon > Validate Playable Demo` for static scene/data/prefab checks.
