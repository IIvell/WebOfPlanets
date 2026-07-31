# Izvještaj — analiza Assets/ prije reorganizacije (2026-07-27)

Faza 1, ništa nije dirano. Izvor: Unity AssetDatabase preko unityMCP (714 datoteka, 74 foldera).

## 1. Inventura

| Vrsta | Broj | Napomena |
|---|---|---|
| Modeli (.fbx) | 449 | golema većina su uvezeni kitovi |
| Skripte (.cs) | 89 | 88 runtime + 1 editor; sve u `namespace WebOfPlanets` |
| ScriptableObjecti (.asset) | 51 | + 7 URP settings + 5 TMP |
| Teksture (.png/.jpg/.jpeg) | 56 | |
| Materijali (.mat) | 18 | |
| Sceni (.unity) | 1 | SampleScene (jedina, u buildu) |
| Shaderi | 19 + 4 .cginc + 1 .hlsl | svi u TextMesh Pro (paket) |
| Audio / Animacije | 0 / 0 | zvuk je proceduralan (AudioSynth), VFX runtime-generiran |
| Prefabi (.prefab) | **0** | folder `Prefabs/` uopće ne sadrži prefabe — samo modele! |
| Dokumenti (.md) | 7 | svi u rootu Assets/ |
| Ostalo | .zip (1!), .inputactions, .ttf, screenshotovi | |

## 2. Moje vs. uvezeno

**Moje:** `Scripts/` (89), `Editor/`, `Scenes/`, `Scriptable Objects/` (51), `Materials/` (4), `System/Settings/` (URP), planet-materijali i `resource_bits_texture.png` u `Prefabs/Materials/`, `Planet.fbx`, `fridge.fbx`, Grab-Bot modeli (`Prefabs/Character/`), .md dokumenti, screenshotovi.

**Uvezeno (kitovi / download):** `Prefabs/Space kit/` (153, Kenney-stil), `Prefabs/Forest/` (106), `Prefabs/Graveyard/` (92), `Prefabs/Magma/` (15), `Prefabs/Gas/` (14, Sketchfab s `source/`+`textures/` strukturom), `Prefabs/Materials/` FBX pack resursa (80+ Copper/Gold/Iron/Fuel...), `Prefabs/Planets/` (Jupiter, Qo'noS), `Prefabs/Textures/` (Ice/Stone/Volcanic), `Prefabs/Tools/Drill/`, `Prefabs/Resources/EnemyMobAlien.fbx`, `TextMesh Pro/` (paket-esencijali).

## 3. Nalazi

### Duplikati (identičan sadržaj, MD5)
- `Prefabs/Resources/EnemyMobAlien.fbx` == `Prefabs/Space kit/alien.fbx` — kopija je **namjerna** (runtime load iz Resources po imenu). Ostaje.
- Kolizije imena bez duplikata sadržaja: `Untitled_2_DefaultMaterial_*.png` postoji i u `Gas/Cylinder/` i u `Gas/Tanker/` (različit sadržaj); `MaterialScene.fbx` (Planets) vs `MaterialScene(Det4).fbx` (Magma); `Ice_*.mat` u `Prefabs/Materials/` zrcali `Ice_*.png` u `Prefabs/Textures/Ice planet/`.

### Neiskorišteno (464 datoteke nedosegnute iz scene / Resources / settingsa)
Ništa se ne briše — samo popis. Metoda: reverse-dependency od SampleScene + svi Resources folderi + URP settings + .inputactions.

- **Kitovi — koristi se šačica, ostatak leži:** Forest 101/106 nekorišteno (koriste se samo `Grass_2_C_Singlesided_Color1`, `Rock_2_E`, `Rock_3_F`, `Tree_4_C` + tekstura), Graveyard 89/92 (koriste se `border-pillar`, `pillar-obelisk` + colormap), Space kit 143/153 (koriste se `chimney_detailed`, `craft_miner`, `gate_complex`, `gate_simple`, `machine_barrel(+Large)`, `machine_generator(+Large)`, `machine_wireless`, `rover`), Prefabs/Materials 80/91 FBX-ova nekorišteno (koriste se `Chest`, `Computer`, `Iron_Bar`, `Pickaxe`).
- **Moje nekorišteno:** 7 Grab-Bot dijelova (koristi se samo `Grab-Bot COMPLETE.fbx`), `Ice_*.mat` (5, materijali nazvani po teksturnim mapama — vjerojatno ostatak importa), `Jupiter.fbx`, `Qo'noS.fbx`, `MaterialScene.fbx`, dio QonoS/Planeta tekstura, `venus-surface1.jpeg`, `volcanic.fbx` (source).
- **Smeće:** `Prefabs/Gas/Mask/source/model.zip` — ZIP u Assets/ (Unity ga ne koristi).

### Datoteke u rootu Assets/
7 × .md (`GDD.md`, `PLAN.md`, `PLAN-KOD-2026-07-25.md`, `AUDIT-2026-07-14.md`, `AUDIT-KOD-2026-07-24.md`, `GROUNDING-2026-07-16.md`, `DEVLOG.md`) + `Planet.fbx` (koristi se!). Napomena: CLAUDE.md pod "Unresolved" kaže da GDD/PLAN-KOD nisu u repou — jesu, u rootu Assets/.

### Loše / nekonzistentno imenovanje
- `Prefabs/` ne sadrži prefabe nego modele; `Prefabs/Materials/` sadrži FBX modele *game-resursa* — dvostruko krivo ime.
- Razmaci i znakovi: `Space kit/`, `Ice planet/`, `Scriptable Objects/`, `gas cylinder.fbx`, `Qo'noS.fbx` (apostrof!), `NormalMap (4).png`, `Untitled 2.fbx`, `grab-bot base with wheel.fbx` vs `Grab-Bot COMPLETE.fbx` (miješan case).
- Bez prefiksa T_/M_/SFX_ (osim Drill tekstura koje ih već imaju).
- Skripte: **čiste** — imena datoteka = imena klasa, sve u `namespace WebOfPlanets`; višeklasne datoteke su dokumentirana konsolidacija iz CLAUDE.md (ne dirati). `PlayerInputActions.cs` je generiran.

## 4. Predložena nova struktura

```
Assets/
  _Project/
    Scripts/        <- Assets/Scripts/* (postojeći domenski podfolderi — vidi pitanje 2)
    Scenes/         <- Assets/Scenes/SampleScene.unity
    Data/           <- Assets/Scriptable Objects/*  (Resources/ podfolderi ostaju Resources!)
    Art/
      Materials/    <- Assets/Materials/* + Planet_*.mat + VolcanicHazard.mat
      Textures/     <- resource_bits_texture.png
      Models/       <- Planet.fbx, fridge.fbx, Prefabs/Character/*
    Settings/       <- Assets/System/Settings/* (URP; GUID-reference iz Project Settings prežive AssetDatabase move)
    Docs/           <- 7 root .md + Screenshots/ (ili izvan Assets — vidi ispod)
  ThirdParty/
    SpaceKit/       <- Prefabs/Space kit/
    Forest/         <- Prefabs/Forest/
    Graveyard/      <- Prefabs/Graveyard/
    Magma/          <- Prefabs/Magma/
    Gas/            <- Prefabs/Gas/
    ResourceModels/ <- Prefabs/Materials/ (FBX pack; .mat idu u _Project/Art/Materials)
    PlanetModels/   <- Prefabs/Planets/
    PlanetTextures/ <- Prefabs/Textures/
    Drill/          <- Prefabs/Tools/Drill/
  Editor/           NE DIRA SE
  Prefabs/Resources/ NE DIRA SE (runtime load po imenu; 1 datoteka)
  TextMesh Pro/     NE DIRA SE (paket-esencijali s vlastitim Resources)
```

Prazni folderi (`Prefabs/`, `Audio/`, `Animations/`, `Sprites/`, `VFX/`) se **ne kreiraju** — nema sadržaja za njih (0 prefaba, 0 audio/anim asseta).

## 5. Tablica staro → novo

| Staro | Novo | Napomena |
|---|---|---|
| Assets/Scripts/** | Assets/_Project/Scripts/** | bez promjene sadržaja |
| Assets/Scenes/SampleScene.unity | Assets/_Project/Scenes/SampleScene.unity | provjeriti Build Settings nakon |
| Assets/Scriptable Objects/** | Assets/_Project/Data/** | interna struktura identična; `Resources/` i `Machines/Resources/` zadržavaju ime i sadržaj (Resources.Load radi iz bilo kojeg foldera imena Resources) |
| Assets/Materials/Brown∣Grey∣Red∣GrabBot.mat | _Project/Art/Materials/M_Brown∣M_Grey∣M_Red∣M_GrabBot.mat | rename siguran (GUID ostaje) |
| Prefabs/Materials/Planet_*.mat, VolcanicHazard.mat | _Project/Art/Materials/M_Planet_*.mat, M_VolcanicHazard.mat | |
| Prefabs/Materials/resource_bits_texture.png | _Project/Art/Textures/T_ResourceBits.png | |
| Assets/Planet.fbx | _Project/Art/Models/Planet.fbx | |
| Prefabs/fridge.fbx | _Project/Art/Models/Fridge.fbx | |
| Prefabs/Character/*.fbx | _Project/Art/Models/Character/ + PascalCase (GrabBotComplete.fbx…) | |
| Assets/System/Settings/** | _Project/Settings/** | |
| Root *.md + Screenshots/ | _Project/Docs/ | ili izvan Assets |
| Prefabs/Space kit∣Forest∣Graveyard∣Magma∣Gas∣Materials∣Planets∣Textures∣Tools/Drill | Assets/ThirdParty/… (tablica gore) | sadržaj se NE dira, samo lokacija foldera |
| Editor/, TextMesh Pro/, Prefabs/Resources/ | bez promjene | posebna pravila |

**Namjerno se NE radi:**
- Rename ijednog ScriptableObject .asset-a — SaveSystem ih rješava **po imenu** iz Resources; rename bi slomio postojeće save datoteke.
- Rename sadržaja kitova (tvoje pravilo + GUID reference iz Planets.asset/scene ionako prežive).
- Diranje `PlayerInputActions.cs` — ali nakon micanja treba u `.inputactions` importeru ažurirati putanju generirane klase (inače regeneracija piše na staru putanju).
- Brisanje bilo čega (uklj. `model.zip` i 5 `Ice_*.mat` — samo označeno).

**Nakon Faze 2 još:** ažurirati CLAUDE.md (Layout sekcija + "Unresolved" — GDD/PLAN-KOD postoje), `read_console`, otvoriti scenu, provjeriti Missing reference i Build Settings.

## 6. FAZA 2 — IZVRŠENO (2026-07-27)

Sve premješteno kroz `AssetDatabase.MoveAsset`/`RenameAsset` (GUID-ovi očuvani): 15 folder-moveova,
27 file-moveova, 12 preimenovanja — **0 grešaka**. Odluke: Scripts zadržao postojeće domene,
namespace ostao flat `WebOfPlanets`, docs u `_Project/Docs/`.

**Verifikacija:** konzola 0 errora / 0 warninga; scena učitana s nove putanje, validate: 0 missing
skripti, 0 slomljenih prefaba; Build Settings automatski ažuriran (GUID); `Resources.Load` testiran
(`Recipes/Axe`, `Mining_ore`, `NetworkComputer`, `RespawnTotem`, `EnemyMobAlien` — sve radi, 267
asseta dosegljivo). `.inputactions` ima `generateWrapperCode: 0` (ručna generacija) — putanju nije
trebalo dirati. CLAUDE.md ažuriran (Layout, putanje, Gotchas, Unresolved).

**Ostavljeno za ručnu odluku:**
- Prazni folderi `Assets/System/` i `Assets/Prefabs/Tools/` (ništa nisam brisao — obriši ih ručno u Editoru ako želiš).
- 464 neiskorištena asseta iz §3 (uglavnom kitovi) — kandidati za brisanje/izdvajanje, samo popisani.
- `model.zip` u `ThirdParty/Gas/Mask/source/` — ne pripada u Assets.
- 5 `Ice_*.mat` premješteno u `ThirdParty/PlanetTextures/Ice planet/Materials/` — i dalje nekorišteni.
- `Prefabs/Resources/` i `TextMesh Pro/` namjerno netaknuti.

## 7. Otvorena pitanja iz Faze 1 (riješeno u chatu)

1. **Kreni s Fazom 2?**
2. **Scripts podfolderi:** zadržati postojeće domene (Audio, Crafting, Enemies, Events, Game, Interaction, Inventory, Machines, Planet, Player, Tools, UI, Vfx) ili prepakirati u Player/Enemies/UI/Managers/Systems/Utils? Postojeća podjela je granularnija i CLAUDE.md je dokumentira.
3. **Namespace:** CLAUDE.md izričito propisuje flat `WebOfPlanets` za sve ("Keep it that way"). Tvoja uputa traži namespace po folderu. Flat = 0 izmjena koda; po folderu = izmjena svih 89 datoteka + `using`-ovi (serijalizacija ne strada, ali krši projektnu konvenciju).
4. **Docs/Screenshots:** u `_Project/Docs/` (kroz AssetDatabase, sve po pravilima) ili potpuno izvan Assets/ (čišći build, ali file-system move)?
