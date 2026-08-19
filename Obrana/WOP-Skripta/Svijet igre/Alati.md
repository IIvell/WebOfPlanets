---
tags: [dizajn, moj-projekt, resursi]
---

# Alati

5 alata (`ToolData` SO u `Data/Tools/`), dvije klase: `ToolClass { Mining, Woodcutting }`.

| Alat | Klasa | Tier | Brzina | Trajnost | Otključan |
|---|---|---|---|---|---|
| Pickaxe | Mining | 1 | 2× | 100 | od starta |
| Axe | Woodcutting | 1 | 2× | 100 | od starta |
| Drill | Mining | 2 | 3× | 150 | prag 2 |
| Rune Drill | Mining | 3 | 5× | 300 | prag 3 |
| Eternal Pickaxe | Mining | 1 | 3× | **0 = beskonačno** | prag 4 |

- **Tier gating:** resurs prolazi ako je opremljen točan alat ili ista klasa + `miningTier ≥` traženi. Rune traži tier 3 → ni Drill (2) ni Eternal Pickaxe (1) ne otvaraju rune.
- **Brzina:** `stvarno vrijeme = miningTime / speedMultiplier` → [[Resursi]].
- **Trajnost:** alat se troši korištenjem; popravak **ne postoji** (GDD ga predviđa) — samo nova izrada ili Eternal Pickaxe (nagrada praga 4: trajnost zauvijek, ali tier 1).

## Povezano

- [[Resursi]] · [[Progresija kroz Hub]] · [[Rudarski planet (Mining)]] · [[Vulkanski planet (Volcanic)]]

## Moguća potpitanja

- *„Čemu dvije klase alata?"* → sprječava da jedan alat radi sve: drvo traži Axe (Woodcutting), kamen/ruda/rune Mining klasu — igrač nosi više alata u hotbaru.
- *„Zašto Eternal Pickaxe nije najbolji alat?"* → svjestan trade-off: beskonačna trajnost, ali tier 1 i 3× — udobnost za svakodnevno rudarenje, Rune Drill ostaje nužan za endgame.
