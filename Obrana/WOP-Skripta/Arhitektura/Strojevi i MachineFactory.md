---
tags: [arhitektura, moj-projekt]
---

# Strojevi i MachineFactory

Dio [[Arhitektura projekta]]. Mapa: `Assets/_Project/Scripts/Machines/` (11 datoteka).

## Anatomija stroja — tri komada, ne jedan

| Komad | Što je | Gdje |
|---|---|---|
| **Ponašanje** | `MonoBehaviour` (npr. `SmelterMachine`) | u datoteci svog `*MachineData` |
| **Podaci** | `*MachineData : MachineData : QuickSlotItem : ScriptableObject` | tip u `Machines/`, instanca u `Data/Machines/` |
| **Spawn** | poziv kroz `MachineFactory` | `MachineFactory.cs` |

Ako stroj treba preživjeti save → još i `Kind` konstanta te capture/restore u [[Save-load sustav|SaveSystemu]].

Tipovi strojeva: Collector, Storage, Smelter, Extractor, Uplink, Teleporter (+ TwoWayGate), Totem, Computer.

## MachineFactory — jedan izvor istine

Statični factory za sve svjetske objekte (strojevi, totemi, markeri, mobovi). Drži **jedinu** tablicu fallback boja i default scale-ova po tipu.

**Zašto je to važno — stvarni defekt:** ranije su `MachinePlacer` i `SaveSystem` držali **svaki svoju kopiju** tablice boja. Kopije su divergirale i dvosmjerni teleporter se nakon loada vraćao u boji običnog. Fix je bio spajanje u jednu tablicu (`PLAN-KOD §1`).

> Ovo je najbolji konkretan primjer koji imam za pitanje *„zašto duplikacija podataka nije samo estetski problem"*.

Factory ujedno rješava **prizemljenje na sferu**: objekt se spusti tako da mu dno stvarne geometrije sjedne na točku na površini, neovisno o tome gdje je pivot prefaba — nužno jer su planeti kugle, a modeli dolaze iz raznih paketa s različitim pivotima.

## Zašto statični factory, a ne prefab instanciranje svugdje

- Pozivatelja je puno i različitih (`SaveSystem`, `PlanetConnection`, `ConnectionManager`, `HubBase`, `EnemyMobSpawner`, `RespawnTotem`, `ComputerMachine`, `GameManager`) — bez factoryja svaki bi ponavljao logiku skaliranja, boje i prizemljenja.
- Load i ručno postavljanje idu **istim putem** → stroj postavljen rukom i stroj iz savea su identični. Ista filozofija kao rebuild u [[Save-load sustav|save/loadu]].

## Moguća potpitanja

- *„Zašto MonoBehaviour živi u datoteci svog SO-a?"* → konsolidacija malih datoteka (srpanj 2026.). Ograničenje: **klasa koju scena/asset referencira preko GUID-a mora imati datoteku svog imena**, inače referenca pukne. Zato su svi `*MachineData` zadržali svoje datoteke.
- *„Kako biste dodali novi stroj?"* → MonoBehaviour + `*MachineData` tip i `.asset` + spawn put u factoryju (+ save, ako treba). Vidi tri komada gore.
- *„Što je fallback boja?"* → obojena kocka kad stroj nema prefab; razvojni placeholder koji je preživio jer je koristan za brzo prototipiranje.
