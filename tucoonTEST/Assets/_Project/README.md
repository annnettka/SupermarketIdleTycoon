# Supermarket Idle Tycoon

Target Unity version: **6000.3.17f1**  
Version currently declared by this checkout: **6000.3.13f1**

This project is a compact 3D idle-tycoon vertical slice built for a 10-15 minute progression run. Project-owned code, scenes, data, wrappers, tests, and UI live under `Assets/_Project`. Imported packages outside that folder are read-only dependencies.

## Run the demo

1. Open the project and let Unity finish importing and compiling.
2. Run `Tools > Supermarket Tycoon > Build / Repair Playable Demo` once.
3. Press Play. Bootstrap is configured as the Play Mode start scene and loads Main Menu asynchronously.

The builder is idempotent and recreates only project-owned generated content. A fresh game starts with $150: build the $50 Shelf and $100 Checkout to start customer traffic.

## Progression

Progression uses data from `ProgressionConfig` and caps at level 5.

| Level | XP threshold | Main unlocks |
| --- | ---: | --- |
| 1 | 0 | First Shelf and Checkout |
| 2 | 100 | Second Shelf and Cashier |
| 3 | 300 cumulative | Store Expansion and second Checkout |
| 4 | 650 cumulative | VIP customers and Premium Shelf |
| 5 | 1100 cumulative | Maximum customer flow and Store Established milestone |

The active-customer cap grows through 3, 4, 6, 8, and 10. Level-up notifications name the new unlocks, and the HUD always shows the active objective.

## Building Upgrades

Every `BuildingDefinition` contains `BuildingLevelDefinition[]`. Levels configure upgrade cost, required player level, station capacity, income multiplier, interaction duration, and presentation scale. `BuildingUpgradeService` is the only upgrade use case: it validates state, progression, and funds before spending and applying the level through `BuildingService`.

Selecting a built station opens its contextual panel. Shelf panels compare income and capacity; Checkout panels compare service time and queue capacity. Level 3 stations show `MAX LEVEL`.

To add an upgrade, append a level to the building asset. No `BuildingService` code change is required.

## Store Expansion

`StoreExpansionDefinition`, `StoreExpansionSpot`, and `StoreExpansionService` model future expansion zones. The first zone requires level 3 and $500. Before purchase, a visible barrier and lock prompt divide the store. Purchase removes the barrier, reveals the developed floor treatment, and enables expansion-gated BuildSpots for the second Checkout and Premium Shelf.

To add an expansion, create a definition with a stable ID, add a `StoreExpansionSpot`, and put that ID on gated BuildSpots.

## Automation

The Cashier employee unlocks at level 2. `EmployeeService` persists the employee level and applies its multiplier through `StationRegistry`, including Checkouts built later.

| Cashier level | Cost | Required player level | Checkout speed bonus |
| --- | ---: | ---: | ---: |
| 1 | $300 | 2 | 20% |
| 2 | $450 | 3 | 35% |
| 3 | $700 | 4 | 50% |

The employee uses a project-owned wrapper around the existing character asset. Add future employees as definitions and focused services; employee UI must request the service use case rather than mutate save state.

## Customer Profiles

The pooled customer agent and existing FSM are shared by every profile. `CustomerProfileDefinition` configures movement, shopping time, payment, patience, spawn weight, required level, and a material-property tint.

- Normal: standard values and 15 seconds of queue patience.
- Impatient: 18% faster, 8 seconds of patience, and 15% less payment.
- VIP: unlocks at level 4, moves slightly slower, waits 20 seconds, and pays 2x.

Add a customer type by creating another profile asset and adding it to `GameSceneEntryPoint`. The FSM does not need to be duplicated.

## Store Rating

Rating starts at 3.0 and is clamped to 1-5. A completed transaction adds 0.03, an impatient departure removes 0.12, and a store upgrade adds 0.05. Ratings below 3.0 make spawn intervals 15% longer; ratings of 4.0 or above make them 10% shorter. Rating is visible in the HUD and saved.

## Objectives

`ObjectiveConfig` holds the ordered, data-driven milestone sequence. `ObjectiveService` consumes explicit gameplay events for building, upgrades, customers, income, levels, employees, expansions, and rating. Rewards are applied through `EconomyService` and `ProgressionService`, then the next goal appears automatically.

The authored sequence covers the first Shelf and Checkout, five served customers, a Shelf upgrade, level 2, Cashier hire, $500 earned, Store Expansion, 25 served customers, rating 4.0, and level 5.

Add an objective by appending an `ObjectiveDefinition` to `ObjectiveConfig`; select a supported type, target, optional stable ID filter, and reward.

## Offline Income

Saves store UTC ticks and any uncollected amount. On return, `OfflineIncomeService` calculates at most 120 minutes without simulating customers:

```text
estimated customers per minute
* base customer payment
* average shelf income multiplier
* cashier speed multiplier
* minutes away
* 0.35 offline efficiency
```

The estimate is limited by both Shelf and Checkout capacity. Income is moved to the wallet only when `COLLECT` is pressed, and pending income becomes zero immediately so it cannot be collected twice.

## Stats

The Pause menu exposes lifetime Customers Served, Customers Lost, Total Money Earned, Buildings Purchased, and Upgrades Purchased. `StatisticsService` owns these counters and the save coordinator persists a copy.

## Save Migration

Gameplay is readable JSON at `Application.persistentDataPath/supermarket-save.json`; the previous valid file is retained as `supermarket-save.backup.json`. Save version 2 adds:

```text
BuildingLevel
PurchasedExpansionIds[]
CashierLevel
StoreRating
CurrentObjectiveIndex / CurrentObjectiveProgress
LifetimeStats
LastSaveUtcTicks
PendingOfflineIncome
```

Version 1 files retain money, XP, level, and built stations. Missing building levels become 1, rating becomes 3.0, collections and stats are initialized, and invalid numeric values are clamped. Migration never deletes an existing save. Reset Progress explicitly deletes gameplay data while retaining settings.

## Architecture

```text
AppBootstrapper
  -> ApplicationContext (scene flow, audio, settings, repositories)
     -> GameSceneEntryPoint (session composition root)
        -> Wallet -> EconomyService
        -> ProgressionService -> StoreRatingService
        -> BuildingService -> BuildingUpgradeService -> StationRegistry
        -> StoreExpansionService
        -> EmployeeService ---------------------------> StationRegistry
        -> CustomerSpawner -> pooled CustomerAgent -> shared FSM
                              -> Shelf / Checkout queue
        -> ObjectiveService / StatisticsService / OfflineIncomeService
        -> GameSaveCoordinator
        -> focused UI controllers and views
```

Services are plain C# objects with explicit constructor dependencies and events. MonoBehaviours adapt them to scenes, navigation, pooled agents, and Unity UI. There is no service locator, mutable global state, or monolithic game manager.

## Generated content

The builder creates or repairs the three project scenes, gameplay configuration assets, five building definitions, three customer profiles, employee/objective/expansion definitions, project-owned wrapper prefabs, NavMesh data, expansion visuals, and all HUD/pause/settings/stats/offline panels. Checkout wrappers author five queue positions.

Known third-party Grocery and SimpleFX materials are copied into project-owned URP-compatible replacements by `MaterialRepairTool`; source assets remain untouched.

## Third-party assets used read-only

- Grocery environment and stations: `Assets/Gridness Studios/Grocery Store Pack Lite`
- Customer and Cashier visuals: `Assets/Hodaart/HodaartLowPolyCharacterCollection3`
- UI sprites: `Assets/HONETi/mobile_cartoon_GUI`
- Purchase feedback: `Assets/SimpleFX`
- Audio cues: `Assets/Cartoon Game Sound 2.0`
- Navigation: official `com.unity.ai.navigation` package

## Tests

EditMode coverage includes wallet invariants, XP overflow and level cap, save round-trip and version-1 migration, objective rewards, rating bounds/traffic thresholds, and customer-payment multipliers.
