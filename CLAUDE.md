# Web of Planets

Single-player 3D space survival/factory game. The player walks on spherical, procedurally
generated planets, mines resources, crafts machines, and links planets into a network of
degrading connections. Builds as a standalone PC game (URP, Unity 6.0).

## Environment

- Unity **6000.3.10f1** (revision `e35f0c77bd8e`)
- Render pipeline: **URP 17.3.0** — confirmed by `GraphicsSettings.asset` `m_CustomRenderPipeline`
  pointing at `Assets/_Project/Settings/PC_RPAsset.asset` (plus `Mobile_RPAsset`, `PC_Renderer`,
  `Mobile_Renderer`, `UniversalRenderPipelineGlobalSettings`)
- Input: **new Input System only** (`activeInputHandler: 1`, `com.unity.inputsystem` 1.18.0).
  Legacy `UnityEngine.Input` / `Input.GetKey` will not work.
- Scripting backend: Mono (default; IL2CPP set for Android), API compatibility level `.NET Standard`
- Color space: Linear. Default resolution 1024x768. Product `Web of Planets`, version `0.1.0`.
- Also installed: AI Navigation 2.0.10, Timeline, uGUI, Visual Scripting, Test Framework 1.6.0,
  MCP for Unity (`com.coplaydev.unity-mcp`, git dependency, `#main`)
- Unity Editor connected when this file was written: **yes** (MCP bridge on port 6400, play mode active)

## Layout

Reorganized 2026-07-27 via AssetDatabase moves (GUIDs preserved) — see `REORG-IZVJESTAJ-2026-07-27.md`
in the project root for the full old→new table.

```
Assets/
  _Project/           All first-party content.
    Scripts/          All runtime C#. One folder per domain, namespace is flat (see Conventions).
      Audio/          AudioManager.cs (self-bootstrapping; AudioSynth procedural SFX in the same file)
      Crafting/       CraftingRecipe.cs (SO + CraftingSystem service in the same file)
      Enemies/        EnemyMob.cs (mob + spawner in the same file)
      Events/         GameEventBus.cs (static bus + event payload structs/enums)
      Game/           GameManager, GameState, GameKeys, HubProgress, SaveSystem.cs (single file since
                      2026-07-31, four partial blocks), SpaceSkybox.cs (+ SpaceSun/SunFollow)
      Interaction/    IInteractable + BaseInteractable and its subclasses, Interactor.cs (raycast
                      source + InteractableHighlight)
      Inventory/      InventorySystem, QuickSlotInventory, HubStorage, Item/InventoryItem/QuickSlotItem
      Machines/       Machine MonoBehaviours live in their *MachineData ScriptableObject's file
                      (consolidation 2026-07-31; CollectorMachine + ProductionMachine in MachineData.cs),
                      plus MachineFactory.cs (spawning + MachineBreakdown) and MachinePlacer (placement input)
      Planet/         Procedural planets, gravity (Attractor), surface placement,
                      ConnectionManager / PlanetConnection, resource spawning, volcanic hazards.
                      Planet textures are ASSETS (ThirdParty/PlanetModels + PlanetTextures), not code —
                      the procedural PlanetTextureUtil.cs was deleted in July 2026.
      Player/         PlayerController, PlayerCamera, PlayerHealth
        Input/        PlayerInputActions.cs — GENERATED from the .inputactions asset. Do not hand-edit.
      Tools/          Tool + ToolData-style SOs (GasMaskData + GasMaskVisual, NetworkMapDeviceData),
                      PlayerToolSystem
      UI/             One *UI MonoBehaviour per screen/overlay (uGUI + TextMesh Pro), UiFocus,
                      UiTheme (static, null-safe skin iz Resources/UISprites — SunGraphica itch.io
                      pack; sprite-ove priprema Editor alat Tools/Web of Planets/Uvezi UI sprite-ove).
                      Exceptions since 2026-07-31: ItemInfoUI in CraftingUI.cs, HubProgressUI in
                      ComputerMenuUI.cs, VictoryUI in MainMenuUI.cs
      Vfx/            VfxManager.cs — runtime-generated particle systems, no VFX assets
                      (+ DisintegrationEffect in the same file)
    Scenes/           SampleScene.unity — the ONLY scene, and the only scene in the build
    Data/             ScriptableObjects (was "Scriptable Objects/"): Machines/ (+ Machines/Resources/),
                      Planets/, Resources/ (+ Resources/Recipes/), Tools/, Devices/
    Art/              Materials/ (M_* prefix), Textures/ (T_* prefix),
                      Models/ (Planet, Fridge, Character/GrabBot*)
    Settings/         URP pipeline assets, renderers, volume profiles (was System/Settings/)
    Docs/             GDD.md, PLAN*.md, AUDIT*.md, DEVLOG.md, GROUNDING*.md + Screenshots/
  ThirdParty/         Imported kits, content untouched: SpaceKit/, Forest/, Graveyard/, Magma/, Gas/,
                      ResourceModels/ (resource item FBX pack), PlanetModels/, PlanetTextures/, Drill/
  Editor/             ColliderAudit.cs — both audits' menu items + collider audit (NOT moved)
  Prefabs/Resources/  EnemyMobAlien.fbx — runtime-loaded by name; deliberately left in place
  TextMesh Pro/       TMP package assets (NOT moved — has its own Resources/)
```

## Assemblies

There are **no `.asmdef` files in this project**. Everything under `Assets/_Project/Scripts/` compiles into
`Assembly-CSharp`, and `Assets/Editor/` into `Assembly-CSharp-Editor`.

Consequences:

- Any runtime script can reference any other runtime script — no assembly boundary to violate.
- `Assets/Editor/` code can see runtime code, but **not the reverse**. Runtime scripts must wrap
  any `UnityEditor` usage in `#if UNITY_EDITOR`. (`_Project/Scripts/Planet/SurfaceAudit.cs` already does.)
- Do not add an `.asmdef` casually — introducing one anywhere under `Assets/_Project/Scripts/` will split
  the assembly and break the currently-unrestricted cross-folder references.
- There is **no test assembly** and no `Tests/` folder, despite `com.unity.test-framework` being
  installed. There are no automated tests to run.

## Key systems

- **World generation** — `Planet/PlanetCreator.cs`. Spawns `startingPlanets` (default 30) via a
  chained spawn: each new planet anchors to a randomly chosen already-spawned planet within
  connection range, so the potential-connection graph is connected by construction. Planet types:
  `Mining, Organic, Ice, Volcanic, Gaseous`. `Planet.Start` raises `OnPlanetDiscovered`, which is
  what triggers hazard and mob spawning — including on save load.
- **Gravity & surface** — `Planet/Attractor.cs` pulls bodies toward planet centers;
  `PlayerController` keeps the capsule locked to the surface within `surfaceSkin` and releases
  the lock above `ungroundHeight`. `Planet/SurfacePlacement.cs` + `SphericalUV.cs` handle
  placing objects on a sphere.
- **Connection network** — `Planet/ConnectionManager.cs` + `PlanetConnection.cs`. Three tiers
  (weak/mid/strong) with different cost, lifespan, and thickness; connections degrade over time
  and faster when an endpoint is an unstable (volcanic/gaseous) planet. A spanning tree is always
  built so every planet is reachable from the hub; extra links respect `maxPotentialPerPlanet` (3).
- **Machines** — every machine is a MonoBehaviour in `Machines/` paired with a
  `*MachineData` ScriptableObject in `Assets/_Project/Data/Machines/`. `MachineFactory` is the
  single static spawner and the **only** table of fallback colors and default scales — do not
  re-introduce local copies (they previously diverged). `MachinePlacer` handles placement input.
- **Event bus** — `Events/GameEventBus.cs`, a static class of `Action<T>` events plus `Raise*`
  helpers, with payload structs in `EventTypes.cs`. Several events are intentionally reserved for
  future features and have no publisher/subscriber yet; the file header says explicitly not to
  delete them.
- **Save/load** — `Game/SaveSystem.cs` (one file since 2026-07-31; four `partial` blocks inside:
  core / Dto / Capture / Restore). Single JSON slot at `Application.persistentDataPath/webofplanets_save.json`.
  Load does **not** reload the scene: the procedural world is torn down in place and rebuilt through
  the same code paths as world-gen. Assets are resolved by type + name from `Resources`.
- **Runtime bootstrap instead of scene edits** — this is the project's dominant architectural
  pattern (~20 scripts). `AudioManager`, `VfxManager`, `MainMenuUI`, `VictoryUI`, `SpaceSun`,
  `SpaceSkybox`, `EnemyMobSpawner`, `GasMaskVisual` and more create/configure themselves via
  `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`. (`GrabBotSkin` and the planet-texture
  generators used this pattern too, before being replaced by asset textures in July 2026.)
  Comments state the reason explicitly: to avoid editing the scene YAML. Static state is likewise
  reset via `RuntimeInitializeLoadType.SubsystemRegistration`. **Prefer adding a new system this
  way over modifying `SampleScene.unity`.** It's also why save-load must not reload the scene.

## Scenes

- `Assets/_Project/Scenes/SampleScene.unity` — the single scene and the only entry in
  `EditorBuildSettings`. There is no separate bootstrap or menu scene; the main menu is a
  runtime-created overlay (`MainMenuUI`).
- The Input System project-wide actions asset is wired through
  `EditorBuildSettings.m_configObjects["com.unity.input.settings.actions"]`.

## Conventions

- Namespace is **`WebOfPlanets` for every runtime script**, regardless of folder. Folders group
  files; they do not create sub-namespaces. Keep it that way.
- Comments and log messages are written in **Croatian**. Match the surrounding language when
  editing a file; do not translate existing comments.
- Comment style: a block comment above the class explaining *why* the design is the way it is,
  often referencing a past audit, defect fix, or an external design doc (`GDD`, `PLAN-KOD §n`).
  These are load-bearing decisions — read them before refactoring, and add to them rather than
  stripping them.
- Tunables are `[SerializeField] private` with `[Tooltip]`, grouped under `[Header]`. Scene-serialized
  values override code defaults, so **renaming a serialized field silently resets it** — `PlayerController`
  documents a case where this was done deliberately and warns against restoring the old names.
- Singletons use `public static X Instance { get; private set; }` assigned in `Awake`, cleared in
  `OnDestroy`. Statics that survive domain reload are reset with
  `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- Data-driven content lives in ScriptableObjects under `Assets/_Project/Data/`. Anything
  `SaveSystem` must resolve by name has to be reachable from a `Resources` folder.
- Keyboard keys outside the `.inputactions` asset are centralized in `Game/GameKeys.cs`, together
  with their display strings for the controls screen. **Never hardcode a `Key` in a gameplay
  script** — add it to `GameKeys` (this consolidation replaced keys hardcoded across 14+ scripts).
  The `.inputactions` asset only covers Movement / Jump / MouseLook.
- `GameManager.TestingMode` is the single switch that makes all resource costs free (crafting,
  connections, teleports, hub thresholds, maintenance). Route new costs through it.
- `GameManager.IsPlaying` gates gameplay input and is null-safe when no GameManager is in the scene.

## Working in this project

- Use `UnityEngine.InputSystem` APIs. `GameKeys.WasPressed(GameKeys.Interact)` /
  `GameKeys.IsPressed(...)` for one-off keys, the generated `PlayerInputActions` for movement/look.
- Write shaders and materials for **URP**, not Built-in. Lit shader is `Universal Render Pipeline/Lit`.
- Never edit or delete `.meta` files by hand — their GUIDs are what scenes and prefabs reference.
  Delete an asset together with its `.meta`, never one alone.
- `.unity`, `.prefab`, `.asset`, and `.mat` files are YAML but are not safe to hand-edit. Go through
  the Editor or unityMCP (`manage_scene`, `manage_prefabs`, `manage_scriptable_object`,
  `manage_material`, `manage_gameobject`).
- After creating or editing any script, call `read_console` (unityMCP) and confirm compilation
  succeeded. Unity compiles asynchronously — a successful write tool result means nothing about
  whether the code builds.
- New runtime scripts go in the matching `Assets/_Project/Scripts/<Domain>/` folder with
  `namespace WebOfPlanets`. New editor-only scripts go in `Assets/Editor/`.
- Adding a machine means three things, not one: the MonoBehaviour (in the matching `*MachineData`
  file in `Machines/`), a `*MachineData` ScriptableObject type + `.asset` instance, and a spawn
  path via `MachineFactory` (plus a `Kind` constant and capture/restore handling in `SaveSystem`
  if it should persist).
- Adding a persisted field means touching the Dto, Capture, and Restore partial blocks in
  `SaveSystem.cs` together. Old save files must still load.
- There are no unit tests. Verification is done by entering play mode and reading the console;
  the `Assets/Editor/*Audit*` menu items and `Planet/SurfaceAudit.cs` are the project's
  existing sanity-check tooling.
- unityMCP is available. Prefer it over guessing at scene contents: `find_gameobjects` /
  `manage_scene` to inspect, `read_console` to verify, `manage_editor` for play mode.

## Gotchas

- **No assembly definitions** — so a stray `using UnityEditor;` in a runtime script compiles in the
  Editor and breaks the player build. Always guard with `#if UNITY_EDITOR`.
- **Save load rebuilds in place, it does not reload the scene.** Anything that assumes a fresh
  scene, or that only initializes in `Awake` of a scene object, will be wrong after load.
- **`MachineFactory` owns machine colors and scales.** Two earlier copies of that table diverged
  and caused a shipped defect (two-way teleporters loading in the wrong color).
- **Reserved events in `GameEventBus` look dead but are not.** They are a deliberate, documented
  interface for planned features. Don't prune them as unused code.
- **`Assets/_Project/Scripts/Player/Input/PlayerInputActions.cs` is generated.** Edit the `.inputactions`
  asset and let Unity regenerate. Note: `generateWrapperCode` is 0 in the importer — the wrapper
  was generated manually; regeneration must be triggered explicitly.
- Folder names with spaces still exist (`Assets/ThirdParty/PlanetTextures/Ice planet/`,
  `Assets/TextMesh Pro/`) — quote paths in scripts and shell commands.
- **Not every class has its own file** (small-file consolidation, July 2026; extended 2026-07-31 —
  47 scripts are GUID-referenced from the scene or `.asset` files and keep their names, the rest
  were merged): `GameState` lives in `GameManager.cs`; `IInteractable`, `ConnectionInteractable`,
  `PotentialConnectionInteractable`, `ItemInteractable` in `BaseInteractable.cs`; `QuickSlotItem`,
  `InventoryItem` in `Item.cs`; `ConnectionRequirement` in `ConnectionManager.cs`;
  `VolcanicHazardOrbit`/`Zone` in `VolcanicHazardSpawner.cs`; event payload structs/enums in
  `GameEventBus.cs`; `CraftingSystem` in `CraftingRecipe.cs`; `EnemyMobSpawner` in `EnemyMob.cs`;
  audit menu items in `ColliderAudit.cs`. Since 2026-07-31 additionally: every machine MonoBehaviour
  in its `*MachineData` file (`CollectorMachine` + `ProductionMachine` in `MachineData.cs`,
  `MachineBreakdown` in `MachineFactory.cs`); `ItemInfoUI` in `CraftingUI.cs`; `HubProgressUI` in
  `ComputerMenuUI.cs`; `VictoryUI` in `MainMenuUI.cs`; `InteractableHighlight` in `Interactor.cs`;
  `DisintegrationEffect` in `VfxManager.cs`; `AudioSynth` in `AudioManager.cs`; `GasMaskVisual` in
  `GasMaskData.cs`; `SpaceSun`/`SunFollow` in `SpaceSkybox.cs`; `SphericalUV` in
  `SurfacePlacement.cs`; `ResourcePlacement` in `ResourceSpawnManager.cs`; `PlanetTeleporter` in
  `PlanetCreator.cs`; `GasPlanetAtmosphere` in `Planet.cs`; SaveSystem Dto/Capture/Restore partials
  in `SaveSystem.cs`. Only classes NOT referenced by GUID from scenes/prefabs/assets may be merged
  like this — anything serialized (all `*MachineData`, scene-attached MonoBehaviours) must keep a
  file matching its class name, or the reference breaks.
- Mining progress in flight and resource regeneration timers are deliberately not saved; a resource
  mid-regeneration comes back visible after load. This is a known simplification, not a bug.
- Some `Machines/` ScriptableObjects live under a nested `Resources/` folder
  (`_Project/Data/Machines/Resources/`) specifically so `SaveSystem` can resolve them by name.
  Same for `_Project/Data/Resources/` (resource SOs + `Recipes/`) and `Assets/Prefabs/Resources/`
  (EnemyMobAlien.fbx). **Never rename assets inside these folders** — runtime lookup and old save
  files resolve them by name. They may be moved only if the folder keeps the name `Resources` and
  the internal structure stays identical.

## Unresolved

- ~~Comments reference an external design doc as `GDD` and a code plan as `PLAN-KOD §1/§3`.~~
  Resolved 2026-07-27: both were in the Assets/ root all along — now at `Assets/_Project/Docs/GDD.md`
  and `Assets/_Project/Docs/PLAN-KOD-2026-07-25.md`.
- `companyName` is still `DefaultCompany` and target platform settings are largely at defaults —
  unclear whether PC-only is intentional or just not configured yet.
