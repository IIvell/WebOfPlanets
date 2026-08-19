---
tags: [arhitektura, moj-projekt]
---

# Struktura mapa i asseta

Dio [[Arhitektura projekta]]. Projekt je reorganiziran 27. 7. 2026. kroz `AssetDatabase` (GUID-ovi sačuvani, ništa se nije razbilo).

## Glavno pravilo

**`Assets/_Project/` = sve moje. `Assets/ThirdParty/` = sve tuđe, netaknuto.**

Podvlaka u `_Project` gura mapu na vrh Project prozora. Tuđi paketi se ne diraju da se mogu re-importati preko postojećih.

```
Assets/
  _Project/
    Scripts/     67 skripti, jedna mapa po domeni
    Scenes/      SampleScene.unity — jedina scena
    Data/        ScriptableObjecti (Machines, Planets, Resources, Tools, Devices)
    Art/         Materials (M_*), Textures (T_*), Models
    Settings/    URP pipeline asseti, rendereri, volume profili
    Docs/        GDD.md, PLAN-KOD, auditi, devlog
  ThirdParty/    SpaceKit, Forest, Graveyard, Magma, Gas, PlanetModels, PlanetTextures, Drill
  Editor/        ColliderAudit.cs — editor-only alati
  Prefabs/Resources/  EnemyMobAlien.fbx (učitava se po imenu u runtimeu)
```

## Domene skripti

`Audio`, `Crafting`, `Enemies`, `Events`, `Game`, `Interaction`, `Inventory`, `Machines`, `Planet`, `Player`, `Tools`, `UI`, `Vfx`.

Najveće: **UI (17)**, **Planet (12)**, **Machines (11)**.

Mape **samo grupiraju** — namespace je svugdje `WebOfPlanets` (vidi [[Konvencije u kodu]]).

## Zašto `Resources/` mape postoje

`Data/Machines/Resources/`, `Data/Resources/`, `Assets/Prefabs/Resources/` — [[Save-load sustav]] razrješava assete **po tipu + imenu** iz `Resources`. Zato:

> **Nikad ne preimenuj asset unutar `Resources/` mape** — stari save fajlovi ga traže po starom imenu i load pukne.

## Moguća potpitanja

- *„Zašto ne koristite Resources za sve?"* → `Resources` se cijeli pakira u build i učitava u memoriju; koristi se samo za ono što save mora naći po imenu. Modernije bi bilo Addressables.
- *„Što je `_` u `_Project`?"* → konvencija sortiranja, nema tehničko značenje.
