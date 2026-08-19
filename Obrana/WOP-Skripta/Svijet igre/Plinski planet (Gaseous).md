---
tags: [dizajn, moj-projekt, planeti]
---

# Plinski planet (Gaseous)

Prvi od dva **nestabilna** tipa — opasnost je pasivna i stalna: otrovna atmosfera.

- **Resurs:** **Gas** (gustoća 0.2–0.4) — pickup, **regenerira se za 8 s**.
- **Otrovna atmosfera** (`GasPlanetAtmosphere`, klasa u `Planet.cs`):
  - **5 dmg/s**, tick svake 1 s, **grace period 3 s** nakon slijetanja,
  - aktivna samo ako planet nije Hub i tip je Gaseous,
  - poništava ju **gas maska** (`GasMaskData.IsWorn()`), otključava se na pragu 2; nosi se tipkom P na slotu, ne troši se,
  - samobootstrap preko `[RuntimeInitializeOnLoadMethod]` — bez editiranja scene.
- **Nestabilan** → veze uz njega degradiraju 1.5×/2× brže → [[Tipovi planeta]].
- Materijal: `M_Planet_Gaseous.mat`.

## GDD vs. implementacija

GDD za plinske planete predviđa *plutanje u atmosferi* i nestabilno tlo — umjesto toga implementirana je **otrovna atmosfera + gas maska**, mehanika koje u GDD-u nema. Primjer svjesne zamjene skupe mehanike jeftinijom s istim efektom (planet je "uvjetovan opremom").

## Povezano

- [[Tipovi planeta]] · [[Resursi]] · [[Progresija kroz Hub]] (prag 3 i 5 traže Gas) · [[Survival žanr]]

## Moguća potpitanja

- *„Kako igrač preživi na plinskom planetu?"* → prvi posjeti: brzi ulazak-izlazak unutar grace perioda / uz štetu; trajno: gas maska (prag 2, recept Metal Ingot 2 + Plant 3).
- *„Zašto atmosfera umjesto plutanja iz GDD-a?"* → plutanje bi tražilo novi sustav kretanja; atmosfera daje isti dizajnerski cilj (planet s preduvjetom) uz mali trošak implementacije.
