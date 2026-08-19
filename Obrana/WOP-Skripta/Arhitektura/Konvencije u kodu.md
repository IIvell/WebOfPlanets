---
tags: [arhitektura, moj-projekt]
---

# Konvencije u kodu

Dio [[Arhitektura projekta]]. Ovo su pravila kojih se držim kroz cijeli projekt — dobra su za pitanje *„kako osiguravate konzistentnost?"*.

## Namespace

**`WebOfPlanets` za svaku runtime skriptu, bez iznimke.** Mape grupiraju datoteke, ne stvaraju podnamespace. Razlog: pod-namespaceovi bi tražili `using` na vrhu svake datoteke, a projekt nema [[Assembly-CSharp (bez asmdefova)|assembly granice]] koju bi oni odražavali.

## Jezik

Komentari i log poruke su na **hrvatskom**. Pri uređivanju datoteke pratim jezik okoline i ne prevodim postojeće komentare.

## Stil komentara

Blok komentar **iznad klase** koji objašnjava **zašto** je dizajn takav — često s referencom na audit, ispravljen defekt ili `GDD` / `PLAN-KOD §n`. To su **nosive odluke**: čitaju se prije refaktoriranja i nadopunjuju, a ne brišu.

## Serijalizirana polja

```csharp
[Header("Rudarenje")]
[Tooltip("Koliko sekundi traje jedno rudarenje.")]
[SerializeField] private float miningTime = 2f;
```

`private` + `[SerializeField]` = vidljivo u Inspectoru, ali nedostupno drugom kodu (enkapsulacija ostaje) — bolje od `public`.

> ⚠️ **Preimenovanje serijaliziranog polja tiho resetira vrijednost** iz scene na default iz koda, jer se serijalizacija veže na *ime*. `PlayerController` dokumentira slučaj gdje je to učinjeno namjerno i upozorava da se stara imena ne vraćaju. (Rješenje kad se mora preimenovati: `[FormerlySerializedAs("staroIme")]`.)

## Singletoni

```csharp
public static X Instance { get; private set; }   // set u Awake, očišćen u OnDestroy
```

16 klasa ih koristi (`GameManager`, `InventorySystem`, `HubStorage`, većina UI-ja). Statika koja preživi domain reload resetira se preko `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` — vidi [[Runtime bootstrap pattern]].

## GameKeys

**Nikad hardkodirana tipka u gameplay skripti.** Sve tipke izvan `.inputactions` asseta su u `Game/GameKeys.cs`, zajedno s tekstom za ekran s kontrolama. Ta je konsolidacija zamijenila tipke raštrkane po 14+ skripti — prije nje ekran s kontrolama je lagao čim bi se tipka promijenila.

`.inputactions` asset pokriva samo Movement / Jump / MouseLook; ostalo ide kroz `GameKeys.WasPressed(...)`. Vidi [[Input System u mom projektu]].

## TestingMode

`GameManager.TestingMode` je **jedan prekidač** koji sve troškove (crafting, veze, teleporti, hub pragovi, održavanje) učini besplatnima. Svaki novi trošak mora ići kroz njega — inače testiranje kasnih faza igre traje sat vremena.

`GameManager.IsPlaying` gejta gameplay input i **null-safe** je kad GameManager ne postoji u sceni.

## Imenovanje asseta

`M_*` materijali, `T_*` teksture, `*MachineData` SO tipovi, `*UI` ekrani.

## Moguća potpitanja

- *„Zašto hrvatski komentari?"* → rad se brani na hrvatskom; konzistentnost je važnija od jezika.
- *„Provjeravate li konvencije automatski?"* → ne, nema linter ni testove; postoje audit alati u `Assets/Editor/` za konkretne provjere (npr. collideri).
