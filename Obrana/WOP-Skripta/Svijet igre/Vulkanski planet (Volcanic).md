---
tags: [dizajn, moj-projekt, planeti]
---

# Vulkanski planet (Volcanic)

Najopasniji i najrjeđe posjećivan tip — kasna igra (endgame resurs Rune).

- **Resurs:** **Rune** — najrjeđi u igri: gustoća **0.05–0.1** (ostali 0.2–0.4), `pickupChance = 0` → **uvijek mining verzija**: 6 s, traži **Rune Drill** (Mining tier 3, otključava se tek na pragu 3). Ne regenerira se.
- **Lava zone** (`VolcanicHazardSpawner`): **2–5 zona** po planetu, radijus 4–10, **15 dmg/s** (tick 1 s, zajednički tick — preklop zona ne duplicira štetu), ukopane u površinu (`surfaceOffset -1`) i **kruže** po velikoj kružnici brzinom 2–5 m/s.
- **Nestabilan** → veze uz njega degradiraju 1.5×/2× brže.
- Materijal: `M_Planet_Volcanic.mat`; zone `M_VolcanicHazard.mat`.

## Dizajnerska uloga

Dvostruka brava na endgame: (1) Rune traži alat s praga 3, (2) planet fizički kažnjava boravak. Pragovi 4 i 5 traže Rune → igrač **mora** svladati najopasniji tip da završi igru.

## GDD vs. implementacija

GDD-ove "opasne zone" **jesu implementirane** — i još se kreću, što GDD ne spominje. Magma/pepeo/obsidijan kao resursi ne postoje (samo Rune). "Visoka nestabilnost" je implementirana kao množitelj degradacije veza; kvarenje strojeva nije.

## Povezano

- [[Tipovi planeta]] · [[Resursi]] · [[Alati]] · [[Progresija kroz Hub]] (prag 4: Rune 4, prag 5: Rune 6) · [[Cilj i kraj igre]]

## Moguća potpitanja

- *„Zašto se lava zone kreću?"* → statične zone igrač jednom nauči i ignorira; kretanje traži stalnu pozornost → survival napetost i na kratkim posjetima.
- *„Što ako partija nema vulkanski planet?"* → teoretski moguće (uniformni random, ≈0.6%) — poznato ograničenje; rješenje bi bilo garantirati bar jedan planet po tipu pri generaciji.
