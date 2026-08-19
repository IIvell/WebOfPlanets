---
tags: [dizajn, moj-projekt, planeti]
---

# Organski planet (Organic)

Tip s **obnovljivim** resursima — jedini gdje se resursi vraćaju sami.

- **Resursi:**
  - **Wood** (gustoća 0.1–0.2) — mining verzija traži **Axe** (klasa Woodcutting), 3 s, **regenerira se za 10 s**.
  - **Plant** (gustoća 0.2–0.4) — čisti pickup (instant), **regenerira se za 5 s**.
- Regeneracija = objekt ostaje, samo je privremeno neuberiv; nema nestajanja.
- Bez hazarda (samo mobovi). Materijal: `M_Planet_Organic.mat`.
- Plant je i **maintenance trošak** Ore Collectora — organski planeti ostaju relevantni i kasnije.

## GDD vs. implementacija

GDD-ov "ciklički rast" **je implementiran** — kao `regenerationTime` na resursu. Ljekovite biljke i smola (sekundarni resursi) nisu.

## Povezano

- [[Tipovi planeta]] · [[Resursi]] · [[Alati]] · [[Progresija kroz Hub]] (prag 2: Wood 5 + Plant 4)

## Moguća potpitanja

- *„Zašto se baš organski resursi regeneriraju?"* → tematski (rast) i ekonomski: obnovljivi izvor za trajne troškove (maintenance strojeva), dok su rudarski resursi konačni.
